using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static FieryOpal.Src.Procedural.GenUtil;

namespace FieryOpal.Src.Procedural
{

    public class BasicBuildingDesigner : BuildingDesigner
    {
        public BasicBuildingDesigner(Rectangle r) : base(r) { }
        public bool[,] GenerateBuildingMatrix()
        {
            bool[,] grid = new bool[3, 3];

            // 1. Start with a random grid square and fill it
            // 2. With probability numSquaresFilled/9 break the cycle
            // 3. Select a perpendicular, empty grid square with distance = 1 and fill it
            // 4. Go to 1
            // 5. If there are still empty squares surrounded by empty squares,
            // 6.   With probability (3 - Count) / 3 fill a random square
            Point square = new Point(Util.GlobalRng.Next(0, 3), Util.GlobalRng.Next(0, 3)), start = square;
            int numSquaresFilled = 0;
            do
            {
                grid[square.X, square.Y] = true;
                numSquaresFilled++;

                List<Point> choices = new List<Point>();
                bool canMoveLeft = square.X > 0 && !grid[square.X - 1, square.Y];
                if (canMoveLeft) choices.Add(new Point(square.X - 1, square.Y));

                bool canMoveRight = square.X < 2 && !grid[square.X + 1, square.Y];
                if (canMoveRight) choices.Add(new Point(square.X + 1, square.Y));

                bool canMoveUp = square.Y > 0 && !grid[square.X, square.Y - 1];
                if (canMoveUp) choices.Add(new Point(square.X, square.Y - 1));

                bool canMoveDown = square.Y < 2 && !grid[square.X, square.Y + 1];
                if (canMoveDown) choices.Add(new Point(square.X, square.Y + 1));

                if (numSquaresFilled == 9 || choices.Count == 0) break;
                square = choices[Util.GlobalRng.Next(choices.Count)];
            }
            while (Util.GlobalRng.NextDouble() > (numSquaresFilled / 9f));

            if (grid[1, 1]) return grid;

            bool aboveCenter = grid[1, 0];
            bool belowCenter = grid[1, 2];
            bool leftofCenter = grid[0, 1];
            bool rightofCenter = grid[2, 1];
            int count = new[] { aboveCenter, belowCenter, leftofCenter, rightofCenter }.Sum(b => b ? 1 : 0);

            // If three or more squares are perpendicularly next to the center square, it is impossible for there to be a solitary empty square
            if (count >= 3 || Util.GlobalRng.NextDouble() < count / 3) return grid;
            // If two squares are perpendicularly next to the center square, only one solitary tile exists and it lies in the opposite corner of the grid.
            if (count == 2)
            {
                if (aboveCenter && leftofCenter) grid[2, 2] = true;
                else if (aboveCenter && rightofCenter) grid[0, 2] = true;
                else if (belowCenter && leftofCenter) grid[2, 0] = true;
                else if (belowCenter && rightofCenter) grid[0, 0] = true;
            }
            // If only one square is perpendicularly next to the center, there can be three solitary tiles on the opposite side of the grid.
            else if (count == 1)
            {
                if (aboveCenter) grid[Util.GlobalRng.Next(0, 3), 2] = true;
                else if (belowCenter) grid[Util.GlobalRng.Next(0, 3), 0] = true;
                else if (leftofCenter) grid[2, Util.GlobalRng.Next(0, 3)] = true;
                else if (rightofCenter) grid[0, Util.GlobalRng.Next(0, 3)] = true;
            }
            // Otherwise, it is safe to assume that there was only one filled square and it was a corner square, in which case there are three solitary tiles on all other corners.
            else
            {
                // For now, let's not fill the square.
            }

            return grid;
        }

        public static MatrixReplacement FillSmallGaps = new MatrixReplacement(
            new int[3, 3] {
                    { 2, 1, 2, },
                    { 2, 0, 2, },
                    { 2, 1, 2, },
            },
            new int[3, 3] {
                    { 2, 1, 2, },
                    { 2, 1, 2, },
                    { 2, 1, 2, },
            },
            "Fill Small Gaps"
        );

        public static MatrixReplacement MakeWallsSide = new MatrixReplacement(
            new int[3, 3] {
                    { 2, 1, 2 },
                    { 2, 0, 2 },
                    { 2, 0, 2 },
            },
            new int[3, 3] {
                    { 2, 2, 2 },
                    { 2, 1, 2 },
                    { 2, 0, 2 },
            },
            "Make Walls (Side)"
        );

        public static MatrixReplacement MakeWallsDiag = new MatrixReplacement(
            new int[3, 3] {
                    { 1, 2, 2 },
                    { 2, 0, 2 },
                    { 2, 2, 0 },
            },
            new int[3, 3] {
                    { 2, 2, 2 },
                    { 2, 1, 2 },
                    { 2, 2, 0 },
            },
            "Make Walls (Diag)"
        );

        protected override void GenerateOntoWorkspace()
        {
            // ---- Generate building rects from the grid
            bool[,] matrix = GenerateBuildingMatrix();
            Rectangle[,] building_rects = new Rectangle[3, 3];
            // Subpartition overlapping areas:
            // Take a building_rect and partition it in two. Move the first partition
            // one unit towards the other, making them intersect.
            // Store the intersection and use it to build internal walls.
            Rectangle[,] subpart_overlaps = new Rectangle[3, 3];
            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    building_rects[i, j] = new Rectangle(0, 0, 0, 0);
                    if (!matrix[i, j]) continue;
                    Point sz = new Point((int)(Workspace.Width / (Util.GlobalRng.NextDouble() * 3 + 1)), (int)(Workspace.Height / (Util.GlobalRng.NextDouble() * 3 + 1)));
                    Point p = new Point((i + 1) * Workspace.Width / 4, (j + 1) * Workspace.Height / 4);
                    p -= new Point(sz.X / 2, sz.Y / 2);

                    building_rects[i, j] = new Rectangle(p, sz); // GenUtil.Partition(new Rectangle(p, sz), 1, 1 / 2f, 0.0f).ElementAt(0);
                    var /*my_*/sides = GenUtil.SplitRect(building_rects[i, j], .5f).ToList();

                    if(sides[0].X < sides[1].X)
                    // Vertical cut
                    {
                        sides[1] = new Rectangle(sides[1].X - 1, sides[1].Y, sides[1].Width, sides[1].Height);
                    }
                    else
                    // Horizontal cut
                    {
                        sides[1] = new Rectangle(sides[1].X, sides[1].Y - 1, sides[1].Width, sides[1].Height);
                    }

                    subpart_overlaps[i, j] = sides[0].Intersection(sides[1]);
                }
            }
            /*
            Workspace.Iter((s, x, y, t) =>
            {
                int m_x = (int)(((float)x / s.Width) * 3);
                int m_y = (int)(((float)y / s.Height) * 3);
                var r = building_rects[m_x, m_y];
                s.SetTile(x, y, OpalTile.Dirt);
                if (!matrix[m_x, m_y]) return false;

                var p = new Point(x, y);
                if (building_rects[m_x, m_y].Contains(p))
                {
                    if(subpart_overlaps[m_x, m_y].Contains(p))
                    {
                        s.SetTile(x, y, OpalTile.ConstructedWall);
                    }
                    else s.SetTile(x, y, OpalTile.ConstructedFloor);
                }
                return false;
            });
            return;
            // ---- Apply MatrixReplacements to build walls and refine them.
            MRRule FloorToFloor = new MRRule(u => u == OpalTile.ConstructedFloor, OpalTile.ConstructedFloor);
            MRRule WallToWall = new MRRule(u => u == OpalTile.ConstructedWall, OpalTile.ConstructedWall);

            // Coin toss to see if this building has smooth angles
            if (Util.GlobalRng.NextDouble() < .5f)
            {
                MatrixReplacement.NinetyDegCorners.SlideAcross(
                    Workspace, new Point(1),
                    new MRRule(u => u != OpalTile.ConstructedFloor, OpalTile.Dirt),
                    FloorToFloor
                );
            }
            // Coin toss to see if 1-tile gaps are merged into the building
            if (Util.GlobalRng.NextDouble() < .5f)
            {
                FillSmallGaps.SlideAcross(
                    Workspace, new Point(1),
                    new MRRule(u => u != OpalTile.ConstructedFloor, OpalTile.ConstructedFloor),
                    new MRRule(u => u != OpalTile.Dirt, OpalTile.ConstructedFloor)
                );
            }
            // Nice matrix-driven external wall building
            new[] { MakeWallsSide, MakeWallsDiag }.SlideAcross(
                Workspace, new Point(1),
                FloorToFloor,
                new MRRule(u => u == null || u == OpalTile.Dirt, OpalTile.ConstructedWall),
                epochs: 1
            );

            // ---- Removing dirt but leaving an approximate circle to help this complex blend in with the host map
            Point center = new Point(Workspace.Width / 2, Workspace.Height / 2);
            var diameter = Math.Sqrt(Workspace.Width * Workspace.Height);
            float[,] removalNoise = Noise.Calc2D(0, 0, Workspace.Width, Workspace.Height, .05f);
            Workspace.Iter((s, x, y, t) =>
            {
                if (removalNoise[x, y] / 255f < new Point(x, y).Dist(center) / diameter)
                {
                    if (t == OpalTile.Dirt) s.SetTile(x, y, null);
                }
                return false;
            });
            return;
            // ---- Flood fill to get a list of enclosed rooms
            var rooms = GenUtil.GetEnclosedAreasAndCentroids(Workspace, t => t == OpalTile.ConstructedFloor);
            foreach(var room in rooms)
            {
                // Getting the rectangle that contains each room
                var rect = GenUtil.GetEnclosingRectangle(room.Item1);
                // Creating a new Room Designer to decorate this room
                var designer = new BasicRoomDesigner(rect);
                foreach (var tuple in designer.Generate())
                {
                    // Checking that the yielded decoration is actually inside the room
                    if (room.Item1.Contains(tuple.Item3))
                    {
                        if (tuple.Item1 != null) Workspace.SetTile(tuple.Item3.X, tuple.Item3.Y, tuple.Item1);
                        foreach (var act in tuple.Item2) act.ChangeLocalMap(Workspace, tuple.Item3);
                    }
                }
            }
            
            */
        }
    }
}
