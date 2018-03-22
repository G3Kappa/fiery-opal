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

        public static Point RandomUnitPoint(bool xy = true)
        {
            if (xy)
            {
                return new Point(Util.GlobalRng.Next(3) - 1, Util.GlobalRng.Next(3) - 1);
            }
            bool x = Util.GlobalRng.NextDouble() < .5f;
            return new Point(x ? Util.GlobalRng.Next(3) - 1 : 0, !x ? Util.GlobalRng.Next(3) - 1 : 0);
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

        public static Vector2 ChangeY(this Vector2 v, Func<float, float> new_y)
        {
            return ChangeXY(v, (x) => x, new_y);
        }

        public static int UnitX(this Vector2 v)
        {
            return v.X < 0 ? -1 : v.X > 0 ? 1 : 0;
        }

        public static int UnitY(this Vector2 v)
        {
            return v.Y < 0 ? -1 : v.Y > 0 ? 1 : 0;
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

        public static float SquaredEuclidianDistance(this Point p, Point q)
        {
            return (q.X - p.X) * (q.X - p.X) + (q.Y - p.Y) * (q.Y - p.Y);
        }

        public static float SquaredEuclidianDistance(this int p, int q)
        {
            return (q - p) * (q - p);
        }

}
