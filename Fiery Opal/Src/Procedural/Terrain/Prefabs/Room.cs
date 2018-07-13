using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FieryOpal.Src.Actors.Decorations;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using Microsoft.Xna.Framework;
using static FieryOpal.Src.Procedural.GenUtil;

namespace FieryOpal.Src.Procedural.Terrain.Prefabs
{
    public class RoomPrefab : Prefab
    {
        public OrthogonalConvexHull Hull { get; protected set; }

        public RoomPrefab(Point p, int w = 0, int h = 0) : base(p, w, h)
        {
            Materials["Floor"] = TileSkeleton.Get<WoodenFloorSkeleton>();
            Materials["Wall"] = TileSkeleton.Get<ConcreteWallSkeleton>();
            Materials["Door"] = TileSkeleton.Get<DoorSkeleton>();
            Materials["Ceiling"] = TileSkeleton.Get<TiledFloor>();
            Hull = new OrthogonalConvexHull();
            Hull.AddRectangle(new Rectangle(p, new Point(w, h)));
        }

        public RoomPrefab(OrthogonalConvexHull hull) : this(hull.Location, hull.Size.X, hull.Size.Y)
        {
            Hull = hull;
        }

        public override void Generate()
        {
            base.Generate();

            OpalTile wallTile = Materials["Wall"] != null ? TileFactory.Make(Materials["Wall"].Make) : null;
            OpalTile floorTile = Materials["Floor"] != null ? TileFactory.Make(Materials["Floor"].Make) : null;
            OpalTile ceilingTile = Materials["Ceiling"] != null ? TileFactory.Make(Materials["Ceiling"].Make) : null;
            if(floorTile != null) floorTile.Properties.CeilingGraphics = ceilingTile?.Graphics ?? null;
            if(wallTile != null) wallTile.Properties.CeilingGraphics = ceilingTile?.Graphics ?? null;

            foreach (Point wallPos in Hull.Perimeter)
            {
                Workspace.SetTile(wallPos.X - Hull.Location.X, wallPos.Y - Hull.Location.Y, wallTile);
            }

            foreach (Point floorPos in Hull.EnclosedArea)
            {
                Workspace.SetTile(floorPos.X - Hull.Location.X, floorPos.Y - Hull.Location.Y, floorTile);
            }
        }

        public Tuple<Point, DoorTile> AddDoor(Point? p = null, bool? open = null, RoomComplexPrefab parent=null)
        {
            if (!Generated) Generate();

            DoorTile doorTile = (DoorTile)(Materials["Door"] != null ? TileFactory.Make(Materials["Door"].Make) : null);
            if(doorTile != null)
            {
                doorTile.Toggle(open ?? Util.Rng.NextDouble() > .5f);
            }
            
            if (p == null)
            {
                Rectangle doorRoom;
                Point q = Point.Zero;
                do
                {
                    doorRoom = Util.Choose(Hull.Rectangles);
                    // If this is an open-type room (usually interiors), place the door right outside of the wall
                    bool outerDoor = !Materials["Wall"]?.DefaultProperties.IsBlock ?? true;

                    switch (Util.Rng.Next(4))
                    {
                        case 0:
                            p = doorRoom.Location + new Point(doorRoom.Width / 2, 0);
                            if (outerDoor && parent != null && doorRoom.Location.Y > 0) q = new Point(0, -1);
                            break;
                        case 1:
                            p = doorRoom.Location + new Point(doorRoom.Width / 2, doorRoom.Height - 1);
                            if (outerDoor && parent != null && doorRoom.Location.Y + doorRoom.Size.Y < parent.Height - 1) q = new Point(0, 1);
                            break;
                        case 2:
                            p = doorRoom.Location + new Point(0, doorRoom.Height / 2);
                            if (outerDoor && parent != null && doorRoom.Location.X > 0) q = new Point(-1, 0);
                            break;
                        case 3:
                            p = doorRoom.Location + new Point(doorRoom.Width - 1, doorRoom.Height / 2);
                            if (outerDoor && parent != null && doorRoom.Location.X + doorRoom.Size.X < parent.Width - 1) q = new Point(1, 0);
                            break;
                    }
                }
                while (!Hull.Perimeter.Contains(p.Value));
                p += q;
            }

            Workspace.SetTile(p.Value.X - Hull.Location.X, p.Value.Y - Hull.Location.Y, doorTile);
            return new Tuple<Point, DoorTile>(p.Value, doorTile);
        }
    }

    public class RoomFurnisherPrefabDecorator : PrefabDecorator
    {
        private static MatrixReplacement MarkVases = new MatrixReplacement(
            new int[3, 3]
            {
                { 1, 0, 2 },
                { 1, 0, 0 },
                { 1, 0, 2 },
            },
            new int[3, 3]
            {
                { 2, 2, 2 },
                { 2, 0, 2 },
                { 2, 2, 2 },
            },
            "Mark vase spawns"
        );

        private static MatrixReplacement MarkClosets = new MatrixReplacement(
            new int[3, 3]
            {
                { 1, 0, 0 },
                { 1, 0, 0 },
                { 1, 0, 0 },
            },
            new int[3, 3]
            {
                { 2, 2, 2 },
                { 2, 0, 2 },
                { 2, 2, 2 },
            },
            "Mark closet spawns"
        );

        private static MatrixReplacement MarkTablesAndChairs5x5 = new MatrixReplacement(
            new int[5, 5]
            {
                { 1, 1, 1, 1, 1 },
                { 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0 },
            },
            new int[5, 5]
            {
                { 2, 2, 2, 2, 2 },
                { 2, 2, 1, 2, 2 },
                { 2, 1, 0, 1, 2 },
                { 2, 2, 1, 2, 2 },
                { 2, 2, 2, 2, 2 },
            },
            "Mark table and chair spawns (5x5)"
        );

        public override void Decorate(OpalLocalMap pfWorkspace)
        {
            base.Decorate(pfWorkspace);
            
            new[] { MarkTablesAndChairs5x5 }.SlideAcross(
                pfWorkspace,
                new Point(3),
                new MRRule(t => (!t?.Properties.BlocksMovement ?? false), (p) => 
                {
                    new Table().ChangeLocalMap(pfWorkspace, p);
                    return null;
                }),
                new MRRule(t => !typeof(DoorSkeleton).IsAssignableFrom(t.Skeleton.GetType()) && (t?.Properties.BlocksMovement ?? false), (p) =>
                {
                    if (Util.Rng.NextDouble() > .4f)
                    {
                        new Chair().ChangeLocalMap(pfWorkspace, p);
                    }
                    return null;
                }),
                1,
                true,
                true,
                true
            );

            new[] { MarkVases }.SlideAcross(
                pfWorkspace,
                new Point(1),
                new MRRule(t => (!t?.Properties.BlocksMovement ?? false), (p) =>
                {
                    if (Util.Rng.NextDouble() > .93)
                    {
                        new Vase().ChangeLocalMap(pfWorkspace, p);
                    }
                    return null;
                }),
                new MRRule(t => !typeof(DoorSkeleton).IsAssignableFrom(t.Skeleton.GetType()) && (t?.Properties.BlocksMovement ?? false), (_) => null),
                1,
                true,
                true,
                true
            );

            new[] { MarkClosets }.SlideAcross(
                pfWorkspace,
                new Point(1),
                new MRRule(t => (!t?.Properties.BlocksMovement ?? false), (p) =>
                {
                    if (Util.Rng.NextDouble() > .93)
                    {
                        new Closet().ChangeLocalMap(pfWorkspace, p);
                    }
                    return null;
                }),
                new MRRule(t => !typeof(DoorSkeleton).IsAssignableFrom(t.Skeleton.GetType()) && (t?.Properties.BlocksMovement ?? false), (_) => null),
                1,
                true,
                true,
                true
            );
        }
    }
}
