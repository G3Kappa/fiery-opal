using FieryOpal.Src.Actors;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using static FieryOpal.Src.Procedural.GenUtil;
using Microsoft.Xna.Framework.Input;

namespace FieryOpal.Src
{
    public static partial class Util
    {
        public static Random GlobalRng { get; } = new Random(0);

        private static double framerate = 0;
        public static double Framerate => framerate;

        public static T Choose<T>(IList<T> from)
        {
            return from[GlobalRng.Next(from.Count)];
        }

        public static IEnumerable<T> ChooseN<T>(IList<T> from, int n)
        {
            if (n >= from.Count) throw new ArgumentException();

            foreach (var t in from.OrderBy(t => GlobalRng.Next()))
            {
                if (n-- > 0) yield return t;
                else break;
            }
        }

        public static bool? RandomTernary()
        {
            var r = GlobalRng.NextDouble();
            if (r < .33) return true;
            if (r < .66) return false;
            return new bool?();
        }

        public static void Update(GameTime gt)
        {
            framerate = (1 / gt.ElapsedGameTime.TotalSeconds);
        }

        public static bool OOB(int x, int y, int w, int h, int min_x = 0, int min_y = 0)
        {
            return (x < min_x || y < min_y || x >= w || y >= h);
        }

        public static T GetEnumValueFromName<T>(string s)
            where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum) return default(T);
            foreach (var val in Enum.GetValues(typeof(T)))
            {
                if (Enum.GetName(typeof(T), val).ToLower().Equals(s.ToLower())) return (T)val;
            }
            return default(T);
        }

        // Rounds f to the nearest fraction of the form 1/n up to n_steps
        public static float Step(float f, int n_steps)
        {
            Tuple<float, int> best_delta = new Tuple<float, int>(1000000f, 0);
            for(int i = 2; i <= n_steps; ++i)
            {
                float n = 1f / i;
                var delta = Math.Max(f, n) - Math.Min(n, f);
                if (delta <= best_delta.Item1)
                {
                    best_delta = new Tuple<float, int>(delta, i);
                }
                else break;
            }

            return 1f / best_delta.Item2;
        }
    }

    public static partial class Extensions
    {
        public static string Fmt(this string s, params object[] args)
        {
            return String.Format(s, args);
        }

        public static void DeepClone<T>(this T[,] c, ref T[,] other)
        {
            if(c.GetLength(0) != other.GetLength(0) || c.GetLength(1) != other.GetLength(1))
            {
                throw new ArgumentException("Both arrays must have the same size.");
            }
            int W = c.GetLength(0), H = c.GetLength(1);
            for(int x = 0; x < W; x++)
            {
                for(int y = 0; y < H; ++y)
                {
                    other[x, y] = c[x, y];
                }
            }
        }

        public static void SlideAcross(this IEnumerable<MatrixReplacement> mr, OpalLocalMap tiles, Point stride, MRRule zero, MRRule one, int epochs=1, bool break_early=true, bool shuffle = false, bool randomize_order = false)
        {
            List<int> done = new List<int>();
            for(int k = 0; k < epochs; ++k)
            {
                int i = -1;
                foreach (var m in mr)
                {
                    if (done.Contains(++i)) continue;
                    if (!m.SlideAcross(tiles, stride, zero, one, shuffle: shuffle, randomize_order: randomize_order) && break_early)
                    {
                        done.Add(i);
                        Util.Log(String.Format("MatrixReplacement[].SlideAcross: Matrix done in {0} out of {1} epochs.", k + 1, epochs), true);
                    }
                }
            }
        }

        public static T MaxBy<T>(this IEnumerable<T> list, Func<T, float> selector)
        {
            var max = list.Max(selector);
            return list.First(x => selector(x) == max);
        }

        public static T MinBy<T>(this IEnumerable<T> list, Func<T, float> selector)
        {
            var min = list.Min(selector);
            return list.First(x => selector(x) == min);
        }
    }
}
