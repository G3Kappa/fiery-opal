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
    public struct RoomComplexLayoutPiece
    {
        public OrthogonalConvexHull Hull;
        public bool IsRoom;
        public bool IsCorridor => !IsRoom;
    }

    public class RoomComplexPrefab : Prefab
    {
        public RoomComplexPrefab(Point p, int w = 0, int h = 0, Point? cellSz = null, bool? outdoors = null) : base(p, w, h)
        {
            CellSize = cellSz ?? new Point(3);
            Width = (w == 0 ? Util.Rng.Next(4, 9) * CellSize.X : w);
            Height = (h == 0 ? Util.Rng.Next(4, 9) * CellSize.Y : h);

            Outdoors = outdoors ?? Util.CoinToss();
            if(Outdoors)
            {
                CellSize += new Point(2);
                if (Width / CellSize.X <= 1 || Height / CellSize.Y <= 1) CellSize -= new Point(2);
            }

            Width = Width - Width % CellSize.X;
            Height = Height - Height % CellSize.Y;
        }

        public Point CellSize { get; private set; } = new Point(3);
        public bool Outdoors { get; private set; }

        public static List<RoomComplexLayoutPiece> GenerateLayout(Point gridSz, Point squareSz, out Point startPos)
        {
            bool[,] grid = new bool[gridSz.X, gridSz.Y];

            startPos = Point.Zero;
            Point startPosEndOffset = Point.Zero;
            switch(Util.Rng.Next(4))
            {
                case 0:
                    startPos = new Point(Util.Rng.Next(1, gridSz.X), 0);
                    break;
                case 1:
                    startPos = new Point(Util.Rng.Next(1, gridSz.X), gridSz.Y - 1);
                    startPosEndOffset = new Point(0, 1);
                    break;
                case 2:
                    startPos = new Point(0, Util.Rng.Next(1, gridSz.Y));
                    break;
                case 3:
                    startPos = new Point(gridSz.X - 1, Util.Rng.Next(1, gridSz.Y));
                    startPosEndOffset = new Point(1, 0);
                    break;
            }

            List<Point> visited = new List<Point>();
            Stack<Point> backtrack = new Stack<Point>();
            Point curPos = startPos;

            int corridorLength = 0;
            while(true)
            {
                visited.Add(curPos);
                grid[curPos.X, curPos.Y] = true;
                corridorLength++;

                List<Point> validNeighbours = new List<Point>();
                var almostValidNeighbours = new List<Tuple<bool, Point>>();
                foreach(var n in grid.Neighbours(curPos, true, false))
                {
                    if (n.Item1) continue;
                    if (corridorLength > 1 && Util.OOB(n.Item2.X, n.Item2.Y, gridSz.X - 1, gridSz.Y - 1, 1, 1))
                    {
                        if(Util.Rng.NextDouble() < .33f)
                        {
                            continue;
                        }
                    }
                    var nNeighbours = grid.Neighbours(n.Item2, false, false).Count(nn => nn.Item1);
                    if (nNeighbours <= 2 || (Util.CoinToss() && nNeighbours == 4))
                    {
                        almostValidNeighbours.Add(n);
                    }
                }

                foreach (var tup in almostValidNeighbours)
                {
                    var ab = grid.Neighbours(tup.Item2, false, false).Where(nn => nn.Item1).ToList();
                    if (ab.Count == 2 && ab[0].Item2.SquaredEuclidianDistance(ab[1].Item2) > 1) continue;
                    validNeighbours.Add(tup.Item2);
                }

                if(validNeighbours.Count == 0)
                {
                    if (backtrack.Count == 0) break;
                    curPos = backtrack.Pop();
                    continue;
                }

                curPos = Util.ChooseBias(validNeighbours, n => 1d / n.SquaredEuclidianDistance(curPos));
                backtrack.Push(curPos);
            }

            List<RoomComplexLayoutPiece> unmergedLayout = new List<RoomComplexLayoutPiece>();
            List<Rectangle> rects = new List<Rectangle>();
            for (int x = 0; x < gridSz.X; x++)
            {
                for (int y = 0; y < gridSz.Y; y++)
                {
                    var rect = new Rectangle(new Point(x, y) * squareSz, squareSz);
                    if(!unmergedLayout.Any(l => l.IsRoom == !grid[x, y] && l.Hull.AddRectangle(rect, force: grid[x, y])))
                    {
                        var newHull = new OrthogonalConvexHull();
                        newHull.AddRectangle(rect);

                        var newPiece = new RoomComplexLayoutPiece()
                        {
                            Hull = newHull,
                            IsRoom = !grid[x, y]
                        };

                        unmergedLayout.Add(newPiece);
                    }
                }
            }

            foreach (var a in unmergedLayout)
            {
                foreach (var b in unmergedLayout)
                {
                    if (ReferenceEquals(a, b) || !a.IsRoom || !b.IsRoom) continue;
                    a.Hull.TryMergeInPlace(b.Hull, false);
                }
            }

            startPos *= squareSz;
            startPos += (squareSz / new Point(2)) * (startPos.ToVector2().ToUnit().ToPoint()) + startPosEndOffset;
            return unmergedLayout.Where(l => l.Hull.TotalArea.Count > 0).ToList();
        }

        public override void Generate()
        {
            base.Generate();

            Point wh = new Point(Width, Height);

            var layout = GenerateLayout(wh / CellSize, CellSize, out Point startPos).ToList();

            if(!Outdoors)
            {
                foreach (RoomComplexLayoutPiece l in layout.Where(L => L.IsCorridor))
                {
                    var pf = new RoomPrefab(l.Hull);
                    pf.Materials["Floor"] = TileSkeleton.Get<TiledFloor>();
                    pf.Materials["Ceiling"] = TileSkeleton.Get<ConcreteFloorSkeleton>();
                    pf.Place(Workspace, null);
                }
            }

            List<Tuple<Point, DoorTile>> doors = new List<Tuple<Point, DoorTile>>();
            foreach (RoomComplexLayoutPiece l in layout.Where(L => L.IsRoom))
            {
                var pf = new RoomPrefab(l.Hull);
                if(!Outdoors) pf.Materials["Wall"] = pf.Materials["Floor"];
                pf.Materials["Ceiling"] = TileSkeleton.Get<ConcreteFloorSkeleton>();
                pf.Generate();
                // If this room is an interior door it will not get added
                // We have to "dig" it into the corridor walls
                doors.Add(pf.AddDoor(null, null, Outdoors ? null : this));
                if(Util.CoinToss())
                {
                    doors.Add(pf.AddDoor(null, null, Outdoors ? null : this));
                }
                pf.Place(Workspace, null);
            }


            if(!Outdoors)
            {
                var outerRoom = new RoomPrefab(Point.Zero, wh.X, wh.Y);
                outerRoom.Materials["Floor"] = null;
                outerRoom.Materials["Wall"] = TileSkeleton.Get<BrickWallSkeleton>();
                outerRoom.Place(Workspace, null);
            }

            foreach (var d in doors)
            {
                Workspace.SetTile(d.Item1.X, d.Item1.Y, d.Item2);
            }

            if(!Outdoors)
            {
                var outerDoor = TileFactory.Make(TileSkeleton.Get<DoorSkeleton>().Make);
                Workspace.SetTile(startPos.X, startPos.Y, outerDoor);
            }
        }
    }

}
