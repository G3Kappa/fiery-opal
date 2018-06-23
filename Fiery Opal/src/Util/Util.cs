using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static FieryOpal.Src.Procedural.GenUtil;

namespace FieryOpal.Src
{
    public static partial class Util
    {
        public static Random Rng { get; private set; } = new Random();

        private static double framerate = 0;
        public static double Framerate => framerate;

        public static T Choose<T>(IList<T> from)
        {
            return from.Count == 0 ? default(T) : from[Rng.Next(from.Count)];
        }

        public static IEnumerable<T> ChooseN<T>(IList<T> from, int n)
        {
            if (n >= from.Count) throw new ArgumentException();

            foreach (var t in from.OrderBy(t => Rng.Next()))
            {
                if (n-- > 0) yield return t;
                else break;
            }
        }

        public static void SeedRng(int seed)
        {
            Rng = new Random(seed);
        }

        public static bool? RandomTernary()
        {
            var r = Rng.NextDouble();
            if (r < .33) return true;
            if (r < .66) return false;
            return new bool?();
        }

        public static void UpdateFramerate(int millis)
        {
            framerate = (1000d / millis);
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
            for (int i = 2; i <= n_steps; ++i)
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

        public static T LoadContent<T>(string name)
        {
            return SadConsole.Game.Instance.Content.Load<T>(name);
        }

        public static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return
              assembly.GetTypes()
                      .Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal))
                      .ToArray();
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
            if (c.GetLength(0) != other.GetLength(0) || c.GetLength(1) != other.GetLength(1))
            {
                throw new ArgumentException("Both arrays must have the same size.");
            }
            int W = c.GetLength(0), H = c.GetLength(1);
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; ++y)
                {
                    other[x, y] = c[x, y];
                }
            }
        }

        public static void SlideAcross(this IEnumerable<MatrixReplacement> mr, OpalLocalMap tiles, Point stride, MRRule zero, MRRule one, int epochs = 1, bool break_early = true, bool shuffle = false, bool randomize_order = false)
        {
            List<int> done = new List<int>();
            for (int k = 0; k < epochs; ++k)
            {
                int i = -1;
                foreach (var m in mr)
                {
                    if (done.Contains(++i)) continue;
                    if (!m.SlideAcross(tiles, stride, zero, one, shuffle: shuffle, randomize_order: randomize_order) && break_early)
                    {
                        done.Add(i);
                        Util.LogText(String.Format("MatrixReplacement[].SlideAcross: Matrix done in {0} out of {1} epochs.", k + 1, epochs), true);
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

        public static IEnumerable<Tuple<T, T>> Pairs<T>(this IEnumerable<T> enumerable)
        {
            var list = enumerable.ToList();
            for (int i = 1; i < list.Count; ++i)
            {
                yield return new Tuple<T, T>(list[i - 1], list[i]);
            }
        }

        static float[] dt(float[] f, int n)
        {
            float[] d = new float[n];
            int[] v = new int[n];
            float[] z = new float[n + 1];
            int k = 0;
            v[0] = 0;
            z[0] = float.NegativeInfinity;
            z[1] = float.PositiveInfinity;
            for (int q = 1; q <= n - 1; q++)
            {
                float s = ((f[q] + q * q) - (f[v[k]] + v[k] * v[k])) / (2 * q - 2 * v[k]);
                while (s <= z[k])
                {
                    k--;
                    s = ((f[q] + q * q) - (f[v[k]] + v[k] * v[k])) / (2 * q - 2 * v[k]);
                }
                k++;
                v[k] = q;
                z[k] = s;
                z[k + 1] = float.PositiveInfinity;
            }

            k = 0;
            for (int q = 0; q <= n - 1; q++)
            {
                while (z[k + 1] < q)
                    k++;
                d[q] = (q - v[k]) * (q - v[k]) + f[v[k]];
            }

            return d;
        }

        /* dt of 2d function using squared distance */
        static void dt(ref float[,] im)
        {
            int width = im.GetLength(0);
            int height = im.GetLength(1);
            float[] f = new float[Math.Max(width, height)];

            // transform along columns
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    f[y] = im[x, y];
                }
                float[] d = dt(f, height);
                for (int y = 0; y < height; y++)
                {
                    im[x, y] = d[y];
                }
            }

            // transform along rows
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    f[x] = im[x, y];
                }
                float[] d = dt(f, width);

                for (int x = 0; x < width; x++)
                {
                    im[x, y] = d[x];
                }
            }
        }

        public static float[,] DistanceTransform(this bool[,] arr)
        {
            float[,] im = new float[arr.GetLength(0), arr.GetLength(1)];
            int m = arr.GetLength(0) * arr.GetLength(1);
            for (int i = 0; i < arr.GetLength(0); ++i)
            {
                for (int j = 0; j < arr.GetLength(1); ++j)
                {
                    im[i, j] = arr[i, j] ? m : 0f;
                }
            }
            dt(ref im);
            return im;
        }

        public static float[,] DistanceTransform(this float[,] arr, Func<int, int, float> threshold)
        {
            float[,] im = new float[arr.GetLength(0), arr.GetLength(1)];
            int m = arr.GetLength(0) * arr.GetLength(1);
            for (int i = 0; i < arr.GetLength(0); ++i)
            {
                for (int j = 0; j < arr.GetLength(1); ++j)
                {
                    im[i, j] = arr[i, j] > threshold(i, j) ? m : 0f;
                }
            }
            dt(ref im);
            return im;
        }

        public static float[,] Normalize(this float[,] arr, float factor)
        {
            float[,] ret = new float[arr.GetLength(0), arr.GetLength(1)];
            arr.DeepClone(ref ret);

            for (int x = 0; x < arr.GetLength(0); ++x)
            {
                for (int y = 0; y < arr.GetLength(1); ++y)
                {
                    ret[x, y] = arr[x, y] / factor;
                }
            }

            return ret;
        }

        public static float[,] Pow(this float[,] arr, float factor)
        {
            float[,] ret = new float[arr.GetLength(0), arr.GetLength(1)];
            arr.DeepClone(ref ret);

            for (int x = 0; x < arr.GetLength(0); ++x)
            {
                for (int y = 0; y < arr.GetLength(1); ++y)
                {
                    ret[x, y] = (float)Math.Pow(arr[x, y], factor);
                }
            }

            return ret;
        }

        public static IEnumerable<Tuple<T, Point>> ElementsWithinRect<T>(this T[,] arr, Rectangle? R, bool yield_null = false)
        {
            Rectangle r;
            if (!R.HasValue)
            {
                r = new Rectangle(0, 0, arr.GetLength(0), arr.GetLength(1));
            }
            else r = R.Value;

            for (int x = r.X; x < r.Width + r.X; ++x)
            {
                for (int y = r.Y; y < r.Height + r.Y; ++y)
                {
                    if (Util.OOB(x, y, arr.GetLength(0), arr.GetLength(1))) continue;

                    T t = arr[x, y];
                    if ((!yield_null && t != null) || yield_null)
                    {
                        yield return new Tuple<T, Point>(t, new Point(x, y));
                    }
                }
            }
        }

        public static void ForEach<T>(this IEnumerable<T> en, Action<T> act)
        {
            foreach (var e in en.ToList()) act(e);
        }

        public static Color ChangeValue(this Color c, int r = -1, int g = -1, int b = -1, int a = -1)
        {
            return new Color((byte)(r >= 0 ? r : c.R), (byte)(g >= 0 ? g : c.G), (byte)(b >= 0 ? b : c.B), (byte)(a >= 0 ? a : c.A));
        }
    }
}
