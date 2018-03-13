﻿using Microsoft.Xna.Framework;
using System;

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
}