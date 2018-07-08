using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace FieryOpal.Src
{
    public static partial class Util
    {
        public static Point NormalizedStep(Vector2 v)
        {
            return new Point((v.X > 0 ? 1 : (v.X < 0 ? -1 : 0)), (v.Y > 0 ? 1 : (v.Y < 0 ? -1 : 0)));
        }

        public static Point RandomUnitPoint(bool xy = true, bool xor=false)
        {
            if(!xor)
            {
                if (xy)
                {
                    return new Point(Util.Rng.Next(3) - 1, Util.Rng.Next(3) - 1);
                }
                bool x = Util.Rng.NextDouble() < .5f;
                return new Point(x ? Util.Rng.Next(3) - 1 : 0, !x ? Util.Rng.Next(3) - 1 : 0);
            }

            Point r;
            do
            {
                r = RandomUnitPoint(xy, false);
            } while (r == Point.Zero || (Math.Abs(r.X) + Math.Abs(r.Y)) == 2);
            return r;
        }

        public static Vector2 Orthogonal(this Vector2 v)
        {
            return new Vector2(-v.Y, v.X);
        }

        public static Point NormalizedStep(Point p)
        {
            return NormalizedStep(new Vector2(p.X, p.Y));
        }

        public static IEnumerable<Point> BresenhamLine(Point start, Point end, int thickness = 1)
        {
            var original_start = new Point(start.X, start.Y);
            int dx = Math.Abs(end.X - start.X), sx = start.X < end.X ? 1 : -1;
            int dy = Math.Abs(end.Y - start.Y), sy = start.Y < end.Y ? 1 : -1;

            int err = (dx > dy ? dx : -dy) / 2, e2;
            while (true)
            {
                yield return start;
                if (start.X == end.X && start.Y == end.Y) break;
                e2 = err;
                if (e2 > -dx) { err -= dy; start.X += sx; }
                if (e2 < dy) { err += dx; start.Y += sy; }
            }
            if (thickness > 1)
            {
                if (dx > dy)
                {
                    foreach (var p in BresenhamLine(original_start + new Point(0, 1), end + new Point(0, 1), thickness - 1))
                        yield return p;
                }
                else
                {
                    foreach (var p in BresenhamLine(original_start + new Point(1, 0), end + new Point(1, 0), thickness - 1))
                        yield return p;
                }
            }
        }

        public static IEnumerable<Point> Disc(Point p, int r)
        {
            for (int x = -r; x < r; ++x)
            {
                for (int y = -r; y < r; ++y)
                {
                    Point q = new Point(x + p.X, y + p.Y);
                    if (q.SquaredEuclidianDistance(p) <= r * r)
                    {
                        yield return q;
                    }
                }
            }
        }

        public static IEnumerable<Point> CubicBezier(Point p1, Point p2, Point p3, Point p4, int thickness = 1, int n = 20)
        {
            List<Point> pts = new List<Point>();
            for (int i = 0; i <= n; ++i)
            {
                double t = (double)i / n;
                double a = Math.Pow(1 - t, 3);
                double b = 3 * t * Math.Pow(1 - t, 2);
                double c = 3 * Math.Pow(t, 2) * (1 - t);
                double d = Math.Pow(t, 3);

                Point p = new Point(
                    (int)(a * p1.X + b * p2.X + c * p3.X + d * p4.X),
                    (int)(a * p1.Y + b * p2.Y + c * p3.Y + d * p4.Y)
                );
                pts.Add(p);
            }
            for (int i = 0; i < n; ++i)
            {
                foreach (Point p in BresenhamLine(pts[i], pts[i + 1], thickness))
                    yield return p;
            }

            if (thickness > 1)
            {
                int dx = Math.Abs(p4.X - p1.X);
                int dy = Math.Abs(p4.Y - p1.Y);

                Point q = new Point(1, 0);
                if (dx > dy)
                    q = new Point(0, 1);

                foreach (Point p in CubicBezier(p1 + q, p2 + q, p3 + q, p4 + q, thickness - 1, n))
                    yield return p;
            }
        }

        public static IEnumerable<Point> Displace(IEnumerable<Point> list, int noiseW, int noiseH, float noiseS, float amplitude)
        {
            float[,] noise = Lib.Noise.Calc2D(0, 0, noiseW, noiseH, noiseS);
            foreach (Point p in list)
            {
                float n = noise[p.X % noiseW, p.Y % noiseH];
                yield return p +
                    new Point(
                        (int)(Math.Cos(n * 2 * Math.PI - Math.PI) * amplitude),
                        (int)(Math.Sin(n * 2 * Math.PI - Math.PI) * amplitude)
                    );
            }
        }
    }

    public static partial class Extensions
    {
        public static Rectangle Intersection(this Rectangle r, Rectangle other)
        {
            if (!r.Intersects(other)) return new Rectangle(0, 0, 0, 0);
            return new Rectangle(
                Math.Max(r.Left, other.Left),
                Math.Max(r.Top, other.Top),
                Math.Min(r.Right, other.Right) - Math.Max(r.Left, other.Left),
                Math.Min(r.Bottom, other.Bottom) - Math.Max(r.Top, other.Top)
            );
        }

        public static Vector2 ToUnit(this Vector2 v)
        {
            return new Vector2(UnitX(v), UnitY(v));
        }

        public static Vector2 ChangeXY(this Vector2 v, Func<float, float> new_x, Func<float, float> new_y)
        {
            return new Vector2(new_x(v.X), new_y(v.Y));
        }

        public static Vector2 ChangeX(this Vector2 v, Func<float, float> new_x)
        {
            return ChangeXY(v, new_x, (y) => y);
        }

        public static Vector2 MaxComponents(this Vector2 v)
        {
            bool xy = Math.Abs(v.X) > Math.Abs(v.Y);

            return new Vector2(xy ? v.X : 0, xy ? 0 : v.Y);
        }

        public static Vector2 Abs(this Vector2 v)
        {
            return new Vector2(Math.Abs(v.X), Math.Abs(v.Y));
        }

        public static Point ToRoundedPoint(this Vector2 v)
        {
            float fx = v.X - (int)v.X;
            float fy = v.Y - (int)v.Y;

            return new Point(
                fx >= .5f ? (int)(v.X + 1) : (int)v.X,
                fy >= .5f ? (int)(v.Y + 1) : (int)v.Y
            );
        }

        public static Vector2 ChangeY(this Vector2 v, Func<float, float> new_y)
        {
            return ChangeXY(v, (x) => x, new_y);
        }

        public static int UnitX(this Vector2 v)
        {
            return v.X < -.5f ? -1 : v.X > .5f ? 1 : 0;
        }

        public static int UnitY(this Vector2 v)
        {
            return v.Y < -.5f ? -1 : v.Y > .5f ? 1 : 0;
        }

        public static double Dist(this Vector2 v, Vector2 w)
        {
            return Math.Sqrt(Math.Pow(w.X - v.X, 2) + Math.Pow(w.Y - v.Y, 2));
        }

        public static double Dist(this Vector2 v, Point w)
        {
            return Math.Sqrt(Math.Pow(w.X - v.X, 2) + Math.Pow(w.Y - v.Y, 2));
        }

        public static double Dist(this Point v, Point w)
        {
            return Math.Sqrt(Math.Pow(w.X - v.X, 2) + Math.Pow(w.Y - v.Y, 2));
        }

        public static double Dist(this Point v, Vector2 w)
        {
            return Math.Sqrt(Math.Pow(w.X - v.X, 2) + Math.Pow(w.Y - v.Y, 2));
        }

        public static double ManhattanDist(this Point v, Point w)
        {
            return Math.Abs(w.X - v.X) + Math.Abs(w.Y - v.Y);
        }

        public static double ManhattanDist(this Vector2 v, Vector2 w)
        {
            return Math.Abs(w.X - v.X) + Math.Abs(w.Y - v.Y);
        }

        /// <summary>
        /// Generalizes Manhattan distance (p=1) and Euclidean distance (p=2).
        /// </summary>
        /// <returns></returns>
        public static double MinkowskiDist(this Point v, Point w, float p)
        {
            p = (float)Math.Pow(2, p);
            return Math.Pow(Math.Pow(Math.Abs(w.X - v.X), p) + Math.Pow(Math.Abs(w.Y - v.Y), p), 1 / p);
        }


        /// <summary>
        /// Generalizes Manhattan distance (p=1) and Euclidean distance (p=2).
        /// </summary>
        /// <returns></returns>
        public static double MinkowskiDist(this Vector2 v, Vector2 w, float p)
        {
            p = (float)Math.Pow(2, p);
            return Math.Pow(Math.Pow(Math.Abs(w.X - v.X), p) + Math.Pow(Math.Abs(w.Y - v.Y), p), 1 / p);
        }

        public static double ChebyshevDistance(this Point v, Point w)
        {
            return Math.Max(Math.Abs(v.X - w.X), Math.Abs(v.Y - w.Y));
        }

        public static double ChebyshevDistance(this Vector2 v, Vector2 w)
        {
            return Math.Max(Math.Abs(v.X - w.X), Math.Abs(v.Y - w.Y));
        }

        public static double OctagonalDist(this Point v, Point w)
        {
            return OctagonalDist(v.ToVector2(), w.ToVector2());
        }

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static double OctagonalDist(this Vector2 v, Vector2 w)
        {
            float dx = Math.Abs(v.X - w.X);
            float dy = Math.Abs(v.Y - w.Y);

            float min, max;
            if (dx < dy)
            {
                min = dx;
                max = dy;
            }
            else
            {
                min = dy;
                max = dx;
            }

            return 0.41 * min + 0.941246 * max;
        }

        // http://flipcode.com/archives/Fast_Approximate_Distance_Functions.shtml
        public static int FastDist(this Point v, Point w)
        {
            int dx = Math.Abs(v.X - w.X);
            int dy = Math.Abs(v.Y - w.Y);

            int min, max;
            if (dx < dy)
            {
                min = dx;
                max = dy;
            }
            else
            {
                min = dy;
                max = dx;
            }

            int approx = (max * 1007) + (min * 441);
            if(max < (min << 4))
            {
                approx -= max * 40;
            }

            // add 512 for proper rounding
            return ((approx + 512) >> 10);
        }

        public static float SquaredEuclidianDistance(this Point p, Point q)
        {
            return (q.X - p.X) * (q.X - p.X) + (q.Y - p.Y) * (q.Y - p.Y);
        }

        public static float SquaredEuclidianDistance(this Vector2 p, Vector2 q)
        {
            return (q.X - p.X) * (q.X - p.X) + (q.Y - p.Y) * (q.Y - p.Y);
        }

        public static float SquaredEuclidianDistance(this int p, int q)
        {
            return (q - p) * (q - p);
        }

        public static double Quantize(this double d, int steps)
        {
            return ((int)(d * steps)) / (float)steps;
        }

        public static float Quantize(this float d, int steps)
        {
            return (float)Quantize((double)d, steps);
        }

        public static Point RotateCW(this Point p)
        {
            return new Point(p.Y, -p.X);
        }

        public static Point RotateCCW(this Point p)
        {
            return new Point(-p.Y, p.X);
        }
    }
}
