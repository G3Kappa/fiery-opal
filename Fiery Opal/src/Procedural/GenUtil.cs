using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural
{
    public static class GenUtil
    {
        public class Rectanglef
        {
            public Vector2 Location { get; set; }
            public Vector2 Size { get; set; }

            public float X => Location.X;
            public float Y => Location.Y;
            public float Width => Size.X;
            public float Height => Size.Y;

            public float Left => Location.X;
            public float Top => Location.Y;
            public float Right => Location.X + Size.X;
            public float Bottom => Location.Y + Size.Y;

            public static implicit operator Rectanglef(Rectangle r)
            {
                return new Rectanglef()
                {
                    Location = new Vector2(r.X, r.Y),
                    Size = new Vector2(r.Size.X, r.Size.Y)
                };
            }

            public static implicit operator Rectangle(Rectanglef r)
            {
                return new Rectangle()
                {
                    Location = new Point((int)r.X, (int)r.Y),
                    Size = new Point((int)r.Size.X, (int)r.Size.Y)
                };
            }

            public Rectanglef()
            {
                Location = Vector2.Zero;
                Size = Vector2.Zero;
            }

            public Rectanglef(float x, float y, float w, float h)
            {
                Location = new Vector2(x, y);
                Size = new Vector2(w, h);
            }
        }

        public static IEnumerable<Rectangle> SplitRect(Rectangle r, float pct)
        {
            bool ver = r.Width > r.Height; // Vertical cut?

            float r1W = (ver ? r.Width * pct : r.Width);
            float r1H = (ver ? r.Height : r.Height * pct);

            float r2W = (ver ? r.Width - r1W : r.Width);
            float r2H = (ver ? r.Height : r.Height - r1H);

            Rectanglef r1 = new Rectanglef(r.X, r.Y, r1W, r1H);
            Rectanglef r2 = new Rectanglef(r.X + (ver ? r1W : 0), r.Y + (ver ? 0 : r1H), r2W, r2H);

            yield return r1;
            yield return r2;
        }

        public static IEnumerable<Rectangle> Partition(Rectangle r, float min_size)
        {
            List<Rectangle> partitions = new List<Rectangle>();
            Stack<Rectanglef> stack = new Stack<Rectanglef>();

            Rectanglef I = r;

            do
            {
                bool ver = I.Width > I.Height; // Vertical cut?

                // If cutting vertically, the width of the first rectangle will be half the original size, and the height full.
                // If cutting horizontally, the height will be half and the width full.
                float r1W = (ver ? I.Width / 2f : I.Width);
                float r1H = (ver ? I.Height : I.Height / 2f);

                // r1 = Location of first rectangle and r1W r1H size
                Rectanglef r1 = new Rectanglef(I.X, I.Y, r1W, r1H);
                // r2 = Basically r1 but with X or Y incremented by the previously halved measure
                Rectanglef r2 = new Rectanglef(
                    I.X + (ver ? r1W : 0),
                    I.Y + (ver ? 0 : r1H),
                    (ver ? r1W : I.Width),
                    (ver ? I.Height : r1H)
                );
                // If either of the generated rectangles is too small, as in
                // if the width of rX divided by the original width is less than min_size
                if (ver ? (r1W / r.Width < min_size) : (r1H / r.Height < min_size))
                {
                    // Add the current rectangle to the list of returned partitions
                    partitions.Add(I);

                    // If there are no more rectangles on the stack, done
                    if (stack.Count == 0) break;
                    // Otherwise pop the stack and repeat
                    I = stack.Pop();
                    continue;
                }
                // Otherwise store r1 for later and try r2
                stack.Push(r1);
                I = r2;
            }
            while (true);

            return partitions;
        }

        public static IEnumerable<Tuple<IEnumerable<Point>, Point>> GetEnclosedAreasAndCentroids(OpalLocalMap tiles, Predicate<OpalTile> enclosed_tile)
        {
            Dictionary<Point, bool> AlreadyFilled = new Dictionary<Point, bool>();
            for (int x = 0; x < tiles.Width; ++x)
            {
                for (int y = 0; y < tiles.Height; ++y)
                {
                    Point p = new Point(x, y);
                    if (!enclosed_tile(tiles.TileAt(x, y)) || AlreadyFilled.ContainsKey(p) && AlreadyFilled[p]) continue;

                    var area = tiles.FloodFill(x, y, null);

                    // Centroid is the average of each point
                    // Instead of having it calculated elsewhere, we save one potentially wasteful loop by calculating it now.
                    Point centroid = new Point(0);
                    int count = 0;
                    foreach (Point q in area)
                    {
                        AlreadyFilled[q] = true;
                        centroid += q;
                        ++count;
                    }
                    centroid /= new Point(count);

                    yield return new Tuple<IEnumerable<Point>, Point>(area, centroid);
                }
            }
        }

        public static Rectangle GetEnclosingRectangle(IEnumerable<Point> area)
        {
            Point min = new Point(int.MaxValue, int.MaxValue), max = new Point(int.MinValue, int.MinValue);
            foreach (var p in area)
            {
                if (p.X < min.X)
                {
                    min.X = p.X;
                }
                else if (p.X > max.X)
                {
                    max.X = p.X;
                }
                if (p.Y < min.Y)
                {
                    min.Y = p.Y;
                }
                else if (p.Y > max.Y)
                {
                    max.Y = p.Y;
                }
            }
            return new Rectangle(min.X, min.Y, max.X - min.X + 1, max.Y - min.Y + 1);
        }

        public static void ConnectEnclosedAreas(OpalLocalMap tiles, IEnumerable<Tuple<IEnumerable<Point>, Point>> enclosedAreas, OpalTile pathTile, int minThickness, int maxThickness, int maxRadius, int maxConnections = 1)
        {
            var enclosed_areas = enclosedAreas.ToList();
            if (enclosed_areas.Count <= 1) return; // Done

            Dictionary<int, List<int>> connections = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> is_connected = new Dictionary<int, List<int>>();
            int remaining_connections = enclosed_areas.Count * maxConnections;
            int radius = 2; // Tiles

            for (int i = 0; i < enclosed_areas.Count; ++i)
            {
                connections[i] = new List<int>();
            }

            while (remaining_connections > 0 && radius <= maxRadius)
            {
                for (int i = 0; i < enclosed_areas.Count; ++i)
                {
                    if (connections[i].Count >= maxConnections) continue;
                    Point p1 = enclosed_areas[i].Item2;
                    for (int j = 0; j < enclosed_areas.Count; ++j)
                    {
                        if (i == j || connections[j].Count >= maxConnections || connections[j].Contains(i)) continue;
                        Point p2 = enclosed_areas[j].Item2;
                        if (p1.Dist(p2) <= radius)
                        {
                            connections[i].Add(j);
                            connections[j].Add(i);
                            // Now that an optimal path has been found, shuffle the centroids of i and j around
                            /*
                            enclosed_areas[i] = new Tuple<IEnumerable<Point>, Point>(
                                enclosed_areas[i].Item1,
                                enclosed_areas[i].Item2 + new Point(Util.Rng.Next(-radius / 4, radius / 4), Util.Rng.Next(-radius / 4, radius / 4))
                            );
                            enclosed_areas[j] = new Tuple<IEnumerable<Point>, Point>(
                                enclosed_areas[j].Item1,
                                enclosed_areas[j].Item2 + new Point(Util.Rng.Next(-radius / 4, radius / 4), Util.Rng.Next(-radius / 4, radius / 4))
                            );
                            */
                            remaining_connections--;
                            tiles.DrawLine(p1, p2, pathTile, thickness: Util.Rng.Next(minThickness, maxThickness));
                        }
                    }
                }
                radius++;
            }
        }

        public static IEnumerable<Point> MakeRockShape(Rectangle r)
        {
            // Use poisson to generate N points
            // Create circles of varying radii on each point
            // Iterate enclosing rectangle
            // If (x, y) / g falls within a circle, yield (x, y) + ofs

            Rectangle r2 = new Rectangle(r.Size / new Point(4) + r.Size / new Point(8), r.Size / new Point(2));
            int max_radius = Math.Min(r2.Width, r2.Height) / 2;
            int min_radius = Math.Min(r2.Width, r2.Height) / 3;

            var points = Lib.PoissonDiskSampler.SampleRectangle(r2.Location.ToVector2(), (r2.Location + r2.Size).ToVector2(), min_radius)
                .Select(p => new Tuple<Point, int, float>(p.ToPoint(), Util.Rng.Next(min_radius, max_radius + 1), (float)Util.Rng.NextDouble() / 2 + 1))
                .ToList();
            for (int x = 0; x < r.Width; ++x)
            {
                for (int y = 0; y < r.Height; ++y)
                {
                    var xy = new Point(x, y);
                    if (points.Any(p => new Point((int)(x / p.Item3 + r.Width / 8), (int)(y / p.Item3 + r.Height / 8)).Dist(p.Item1.ToVector2() / new Vector2(p.Item3)) <= p.Item2 / p.Item3))
                    {
                        yield return xy + r.Location;
                    }
                }
            }
            yield break;
        }

        public static IEnumerable<Point> WeightedRandomWalk(OpalLocalMap map, Point start, Func<Point, float> cost, Predicate<OpalTile> floor, Predicate<OpalTile> wall, OpalTile brush)
        {

            Point q = start, end, dir;
            do
            {
                dir = Util.RandomUnitPoint(false) * new Point(2);
            } while (dir == Point.Zero);

            while (map.Neighbours(q.X, q.Y, cardinal: true, yield_null: true).All(n => wall(n.Item1) || floor(n.Item1)))
            {
                end = q + dir;
                if (Util.CoinToss())
                {
                    if (cost(q + dir.RotateCCW()) < cost(q + dir.RotateCW()))
                    {
                        dir = dir.RotateCCW();
                    }
                    else dir = dir.RotateCW();
                }

                var line = Util.BresenhamLine(q, end).Where(t => wall(map.TileAt(t))).ToList();
                foreach (Point l in line) yield return l;

                map.DrawShape(line, brush);
                q = end;
            }
        }


        public class MRRule : Tuple<Predicate<OpalTile>, Func<OpalTile>>
        {
            public MRRule(Predicate<OpalTile> pred, Func<OpalTile> ret) : base(pred, ret) { }
        }

        public struct MatrixReplacement
        {
            public int[][,] Patterns;
            public int[][,] Replacements;

            public readonly int MatrixSize;
            public readonly string DebugName;

            public MatrixReplacement(int[,] pattern, int[,] replacement, string debug_name = "MatrixReplacement")
            {
                if (pattern.GetLength(0) != replacement.GetLength(0) || pattern.GetLength(0) != pattern.GetLength(1) || replacement.GetLength(0) != replacement.GetLength(1))
                {
                    throw new ArgumentException("Pattern and replacement must have the same size and both dimension must have the same size.");
                }

                Patterns = new int[4][,];
                Replacements = new int[4][,];
                MatrixSize = pattern.GetLength(0);
                DebugName = debug_name;

                Patterns[0] = pattern;
                Replacements[0] = replacement;
                for (int i = 1; i < 4; ++i)
                {
                    Patterns[i] = RotateMatrix(Patterns[i - 1], MatrixSize);
                    Replacements[i] = RotateMatrix(Replacements[i - 1], MatrixSize);
                }
            }

            public bool SlideAcross(OpalLocalMap tiles, Point stride, MRRule zero, MRRule one, bool shuffle = false, bool randomize_order = false)
            {
                bool at_least_one_match = false;
                List<Point> points = new List<Point>();
                for (int x = 0; x < tiles.Width; x += stride.X)
                {
                    for (int y = 0; y < tiles.Height; y += stride.Y)
                    {
                        points.Add(new Point(x, y));
                    }
                }

                if (randomize_order) points = points.OrderBy(x => Util.Rng.Next()).ToList();

                foreach (var p in points)
                {
                    bool matches = false;
                    Apply(tiles, p, zero, one, ref matches, shuffle);
                    if (!at_least_one_match && matches) at_least_one_match = true;
                }
                if (!at_least_one_match)
                {
                    Util.LogText(String.Format("MatrixReplacement.SlideAcross  : No matches for {0}.", DebugName), true);
                }
                return at_least_one_match;
            }

            public bool Matches(OpalLocalMap map, Point p, Predicate<OpalTile> zero, Predicate<OpalTile> one, ref int matrix_index, bool shuffle = false)
            {
                var indices = new Stack<int>();
                var arr = new[] { 0, 1, 2, 3 };
                if (shuffle)
                {
                    arr = arr.OrderBy(x => Util.Rng.Next()).ToArray();
                }
                for (int i = 0; i < 4; ++i)
                {
                    indices.Push(arr[i]);
                }

                /* For each rotated matrix */
                for (int i = indices.Pop(); indices.Count > 0; i = indices.Pop())
                {
                    var pattern = Patterns[i];
                    bool bad_pattern = false;
                    /* For each value in this pattern */
                    for (int x = 0; x < MatrixSize; ++x)
                    {
                        for (int y = 0; y < MatrixSize; ++y)
                        {
                            if (pattern[x, y] > 1) continue; // > 1 is catch all
                            bool bit = pattern[x, y] == 1;
                            var t = map.TileAt(p.X + x - (MatrixSize / 2), p.Y + y - (MatrixSize / 2));
                            if (t == null) continue;

                            if (bit == zero(t) || bit != one(t))
                            {
                                bad_pattern = true;
                                break;
                            }
                        }
                        if (bad_pattern) break;
                    }
                    if (bad_pattern) continue;
                    matrix_index = i;
                    return true;
                }
                return false;
            }

            public void Apply(OpalLocalMap map, Point p, MRRule zero, MRRule one, ref bool matches, bool shuffle = false)
            {
                if (one == null || zero == null) return;
                int matrix_index = 0;

                if (!(matches = Matches(map, p, zero.Item1, one.Item1, ref matrix_index, shuffle))) return;

                for (int x = 0; x < MatrixSize; ++x)
                {
                    for (int y = 0; y < MatrixSize; ++y)
                    {
                        bool bit = Replacements[matrix_index][x, y] == 1;
                        if (Replacements[matrix_index][x, y] <= 1) // > 1 is ignored
                        {
                            int xx = p.X + x - (MatrixSize / 2);
                            int yy = p.Y + y - (MatrixSize / 2);
                            OpalTile t = (bit ? one.Item2() : zero.Item2());
                            if(t != null) map.SetTile(xx, yy, t);
                        }
                    }
                }
            }

            static int[,] Invert(int[,] matrix, int n)
            {
                int[,] ret = new int[n, n];

                for (int i = 0; i < n; ++i)
                {
                    for (int j = 0; j < n; ++j)
                    {
                        if (matrix[i, j] <= 1)
                        {
                            ret[i, j] = 1 - matrix[i, j];
                        }
                        else ret[i, j] = matrix[i, j];
                    }
                }

                return ret;
            }

            static int[,] Mirror(int[,] matrix, int n, bool x)
            {
                int[,] ret = new int[n, n];

                for (int i = 0; i < n; ++i)
                {
                    for (int j = 0; j < n; ++j)
                    {
                        int ni = x ? n - i - 1 : i;
                        int nj = !x ? n - j - 1 : j;

                        if (matrix[ni, nj] <= 1)
                        {
                            ret[i, j] = 1 - matrix[ni, nj];
                        }
                        else ret[i, j] = matrix[ni, nj];
                    }
                }

                return ret;
            }

            public MatrixReplacement Inverted()
            {
                return new MatrixReplacement(Invert(Patterns[0], MatrixSize), Invert(Replacements[0], MatrixSize), DebugName + " (Inverted)");
            }

            public MatrixReplacement MirroredX()
            {
                return new MatrixReplacement(Mirror(Patterns[0], MatrixSize, true), Mirror(Replacements[0], MatrixSize, true), DebugName + " (Mirrored[X])");
            }

            public MatrixReplacement MirroredY()
            {
                return new MatrixReplacement(Mirror(Patterns[0], MatrixSize, false), Mirror(Replacements[0], MatrixSize, false), DebugName + " (Mirrored[Y])");
            }

            static int[,] RotateMatrix(int[,] matrix, int n)
            {
                int[,] ret = new int[n, n];

                for (int i = 0; i < n; ++i)
                {
                    for (int j = 0; j < n; ++j)
                    {
                        ret[i, j] = matrix[n - j - 1, i];
                    }
                }

                return ret;
            }

            public static MatrixReplacement NinetyDegCorners = new MatrixReplacement(
                new int[3, 3] {
                    { 0, 0, 0, },
                    { 0, 1, 1, },
                    { 0, 1, 1, },
                },
                new int[3, 3] {
                    { 0, 0, 0, },
                    { 0, 0, 1, },
                    { 0, 1, 1, },
                },
                "90° Corners"
            );

            public static MatrixReplacement SmallGaps = new MatrixReplacement(
                new int[3, 3] {
                    { 1, 0, 0, },
                    { 0, 0, 0, },
                    { 0, 0, 1, },
                },
                new int[3, 3] {
                    { 0, 0, 0, },
                    { 0, 0, 0, },
                    { 0, 0, 0, },
                },
                "Small Gaps"
            );

            public static MatrixReplacement JaggedSurface = new MatrixReplacement(
                new int[3, 3] {
                    { 0, 0, 0, },
                    { 0, 1, 0, },
                    { 2, 1, 2, },
                },
                new int[3, 3] {
                    { 0, 0, 0, },
                    { 0, 0, 0, },
                    { 2, 1, 2, },
                },
                "Jagged Surface"
            );

            public static MatrixReplacement LoneTile = new MatrixReplacement(
                new int[3, 3] {
                    { 0, 0, 0, },
                    { 0, 1, 0, },
                    { 0, 0, 2, },
                },
                new int[3, 3] {
                    { 0, 0, 0, },
                    { 0, 0, 0, },
                    { 0, 0, 0, },
                },
                "Lone Tile"
            );

            public static MatrixReplacement DiagonalGaps = new MatrixReplacement(
                new int[2, 2] {
                    { 0, 1, },
                    { 1, 0, },
                },
                new int[2, 2] {
                    { 0, 0, },
                    { 0, 0, },
                },
                "Diagonal Gaps"
            );

            public static MatrixReplacement ThinWalls = new MatrixReplacement(
                new int[4, 4] {
                    { 0, 2, 1, 0, },
                    { 0, 1, 1, 0, },
                    { 0, 1, 2, 0, },
                    { 2, 2, 2, 2, }
                },
                new int[4, 4] {
                    { 0, 0, 0, 0, },
                    { 0, 0, 0, 0, },
                    { 0, 0, 0, 0, },
                    { 2, 2, 2, 2, }
                },
                "Thin Walls"
            );

            public static MatrixReplacement[] CaveSystemRules = new[] {
                NinetyDegCorners,
                NinetyDegCorners.Inverted(),
                SmallGaps,
                JaggedSurface,
                JaggedSurface.Inverted(),
                DiagonalGaps,
                LoneTile,
                ThinWalls
            };
        }
    }

}