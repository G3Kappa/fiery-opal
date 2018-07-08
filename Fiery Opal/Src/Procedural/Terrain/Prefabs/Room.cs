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
        public Point DoorWall { get; }

        public RoomPrefab(Point p, int w = 0, int h = 0, Point? doorWall = null) : base(p, w, h)
        {
            Materials["Floor"] = TileSkeleton.Get<ConstructedFloorSkeleton>();
            Materials["Wall"] = TileSkeleton.Get<ConstructedWallSkeleton>();
            Materials["Door"] = TileSkeleton.Get<DoorSkeleton>();
            DoorWall = doorWall ?? Point.Zero;
        }

        public override void Generate()
        {
            base.Generate();

            OpalTile wallTile = TileFactory.Make(Materials["Wall"].Make);
            OpalTile floorTile = TileFactory.Make(Materials["Floor"].Make);

            Workspace.Iter((s, x, y, t) =>
            {
                if (
                    x == 0
                    || y == 0
                    || x == Width - 1
                    || y == Height - 1
                )
                {
                    s.SetTile(x, y, wallTile);
                }
                else
                {
                    s.SetTile(x, y, floorTile);
                }
                return false;
            });

            if (DoorWall == Point.Zero) return;

            Point dw;
            if(DoorWall.X < 0)
            {
                dw = new Point(0, Height / 2);
            }
            else if(DoorWall.X > 0)
            {
                dw = new Point(Width - 1, Height / 2);
            }
            else if (DoorWall.Y < 0)
            {
                dw = new Point(Width / 2, 0);
            }
            else
            {
                dw = new Point(Width / 2, Height - 1);
            }

            Workspace.SetTile(dw.X, dw.Y, TileFactory.Make(Materials["Door"].Make));
        }
    }

    public class HomePrefabDecorator : PrefabDecorator
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

        public override void Decorate(OpalLocalMap pfWorkspace)
        {
            base.Decorate(pfWorkspace);
            OpalTile dbg = TileFactory.Make(TileSkeleton.Get<DebugFloorSkeleton>().Make);

            new[] { MarkVases }.SlideAcross(
                pfWorkspace,
                new Point(1),
                new MRRule(t => t != dbg && (!t?.Properties.BlocksMovement ?? false), () => Util.Rng.NextDouble() > .84 ? dbg : null),
                new MRRule(t => t?.Properties.BlocksMovement ?? false, () => null),
                10,
                true,
                true,
                true
            );
        }
    }
}
