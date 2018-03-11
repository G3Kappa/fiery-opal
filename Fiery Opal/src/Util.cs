using FieryOpal.src.actors;
using FieryOpal.src.ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Linq;
using System.Collections.Generic;
using static FieryOpal.src.procgen.GenUtil;
using System.Text;
using SadConsole.Surfaces;
using Microsoft.Xna.Framework.Input;

namespace FieryOpal.src
{
    public static class Util
    {
        public static readonly MessagePipeline<OpalConsoleWindow> GlobalLogPipeline = new MessagePipeline<OpalConsoleWindow>();

        public static Random GlobalRng { get; } = new Random(0);

        private static double framerate = 0;
        public static double Framerate => framerate;

        public static List<OkCancelDialog> Dialogs { get; } = new List<OkCancelDialog>();

        public static void Log(ColoredString msg, bool debug)
        {
            GlobalLogPipeline.BroadcastLogMessage(null, msg, debug);
        }

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

        public static void Log(String msg, bool debug, Color? fg = null, Color? bg = null)
        {
            Color fore = fg.HasValue ? fg.Value : (debug ? Palette.Ui["BoringMessage"] : Palette.Ui["DefaultForeground"]);
            Color back = bg.HasValue ? bg.Value : Palette.Ui["DefaultBackground"];

            GlobalLogPipeline.BroadcastLogMessage(null, new ColoredString(msg, fore, back), debug);
        }

        public static Point NormalizedStep(Vector2 v)
        {
            return new Point((v.X > 0 ? 1 : (v.X < 0 ? -1 : 0)), (v.Y > 0 ? 1 : (v.Y < 0 ? -1 : 0)));
        }

        public static Point RandomUnitPoint(bool xy=true)
        {
            if(xy)
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

        public static PlayerActionsKeyConfiguration LoadDefaultKeyconfig()
        {
            var cfg = new PlayerActionsKeyConfiguration();
            cfg.AssignKey(PlayerAction.Wait, new Keybind.KeybindInfo(Keys.OemPeriod, Keybind.KeypressState.Press, "Player: Wait"));

            cfg.AssignKey(PlayerAction.MoveU, new Keybind.KeybindInfo(Keys.W, Keybind.KeypressState.Press, "Player: Walk forwards"));
            cfg.AssignKey(PlayerAction.MoveD, new Keybind.KeybindInfo(Keys.S, Keybind.KeypressState.Press, "Player: Walk backwards"));
            cfg.AssignKey(PlayerAction.MoveL, new Keybind.KeybindInfo(Keys.A, Keybind.KeypressState.Press, "Player: Strafe left"));
            cfg.AssignKey(PlayerAction.MoveR, new Keybind.KeybindInfo(Keys.D, Keybind.KeypressState.Press, "Player: Strafe right"));

            cfg.AssignKey(PlayerAction.TurnL, new Keybind.KeybindInfo(Keys.Q, Keybind.KeypressState.Press, "Player: Turn left"));
            cfg.AssignKey(PlayerAction.TurnR, new Keybind.KeybindInfo(Keys.E, Keybind.KeypressState.Press, "Player: Turn right"));

            cfg.AssignKey(PlayerAction.Interact, new Keybind.KeybindInfo(Keys.Space, Keybind.KeypressState.Press, "Player: Interact"));
            cfg.AssignKey(PlayerAction.OpenInventory, new Keybind.KeybindInfo(Keys.I, Keybind.KeypressState.Press, "Player: Open inventory"));

            return cfg;
        }

        public static bool OOB(int x, int y, int w, int h, int min_x = 0, int min_y = 0)
        {
            return (x < min_x || y < min_y || x >= w || y >= h);
        }
    }

    /// <summary>
    /// Used to speed up the retrieval of colored glyph textures in first person mode.
    /// These are much larger than the minimap characters, and recoloring them every time
    /// is incredibly wasteful. A simple garbage collector runs every once in a while and 
    /// removes cached objects that weren't hit enough times to justify their caching.
    /// 
    /// GC properties are defined as static readonly fields.
    /// </summary>
    public static class FontTextureCache
    {
        public static readonly TimeSpan GC_FREQUENCY = new TimeSpan(0, 0, 10);
        public static readonly TimeSpan HIT_DELTA_TOO_HIGH = new TimeSpan(0, 0, 1);
        public static readonly float HIT_COUNT_TOO_LOW = .45f; // % of frames elapsed between each garbage collection

        private static DateTime lastGarbageCollection = DateTime.Now;

        struct CachedFont
        {
            public class RecoloredGlyph
            {
                public byte Glyph = 0;
                public uint Foreground = 0;
                public uint Background = 0;

                public Color[,] Pixels;

                private int _hits;
                public int Hits
                {
                    get => _hits;
                    set
                    {
                        _hits = value;
                        var now = DateTime.Now;
                        HitDelta = now - LastHit;
                        LastHit = now;
                    }
                }
                public DateTime LastHit;
                public TimeSpan HitDelta;

                public RecoloredGlyph(byte glyph, uint fg, uint bg)
                {
                    Glyph = glyph;
                    Foreground = fg;
                    Background = bg;
                }
            }

            public Font Font;
            public Color[] FontImagePixels;
            public Dictionary<byte, Color[,]> GlyphPixels;
            public Dictionary<Tuple<byte, uint, uint>, RecoloredGlyph> RecoloredGlyphPixels;

            public CachedFont(Font f, Color[] fontImagePixels)
            {
                Font = f;
                FontImagePixels = fontImagePixels;
                GlyphPixels = new Dictionary<byte, Color[,]>();
                RecoloredGlyphPixels = new Dictionary<Tuple<byte, uint, uint>, RecoloredGlyph>();
            }
        }

        private static Dictionary<string, CachedFont> CachedFonts = new Dictionary<string, CachedFont>();

        private static Color[,] ObtainGlyphPixels(CachedFont f, byte glyph)
        {
            Color[,] pixels2d = new Color[f.Font.Size.X, f.Font.Size.Y];

            int cx = (glyph % 16) * f.Font.Size.X;
            int cy = (glyph / 16) * f.Font.Size.Y;
            
            for (int x = 0; x < f.Font.Size.X; x++)
            {
                for (int y = 0; y < f.Font.Size.Y; y++)
                {
                    int localindex = (cy + y) * f.Font.FontImage.Width + x + cx;

                    var p = f.FontImagePixels[localindex];
                    pixels2d[x, y] = p;
                }
            }

            return pixels2d;
        }

        public static void GetPixels(Font f, byte glyph, ref Color[,] pixels)
        {
            if(!CachedFonts.ContainsKey(f.Name))
            {
                Color[] data = new Color[f.FontImage.Width * f.FontImage.Height];
                f.FontImage.GetData(data);
                CachedFonts[f.Name] = new CachedFont(f, data);
            }

            if (!CachedFonts[f.Name].GlyphPixels.ContainsKey(glyph))
            {
                CachedFonts[f.Name].GlyphPixels[glyph] = ObtainGlyphPixels(CachedFonts[f.Name], glyph);
            }

            CachedFonts[f.Name].GlyphPixels[glyph].DeepClone(ref pixels);
        }

        public static Color[,] GetRecoloredPixels(Font f, byte glyph, Color newForeground, Color newBackground)
        {
            return GetCachedRecolor(f, glyph, newForeground, newBackground).Pixels;
        }

        static CachedFont.RecoloredGlyph GetCachedRecolor(Font f, byte glyph, Color newForeground, Color newBackground)
        {
            if (DateTime.Now - lastGarbageCollection >= GC_FREQUENCY)
            {
                CollectGarbage();
            }

            Tuple<byte, uint, uint> key = new Tuple<byte, uint, uint>(glyph, newForeground.PackedValue, newBackground.PackedValue);
            Color[,] pixels2d = new Color[f.Size.X, f.Size.Y];
            if (!CachedFonts.ContainsKey(f.Name) || !CachedFonts[f.Name].GlyphPixels.ContainsKey(glyph))
            {
                GetPixels(f, glyph, ref pixels2d);
                return GetCachedRecolor(f, glyph, newForeground, newBackground);
            }

            CachedFont cf = CachedFonts[f.Name];
            if (cf.RecoloredGlyphPixels.ContainsKey(key))
            {
                cf.RecoloredGlyphPixels[key].Hits++;
                return cf.RecoloredGlyphPixels[key];
            }

            cf.RecoloredGlyphPixels[key] = new CachedFont.RecoloredGlyph(glyph, newForeground.PackedValue, newBackground.PackedValue);
            for (int x = 0; x < f.Size.X; x++)
            {
                for (int y = 0; y < f.Size.Y; y++)
                {
                    // If gray: Lerp -the newbackground with the new foreground multiplied by the normalized grey value- by the alpha value.
                    // Otherwise: Retain sprite color (i.e. tree trunks)

                    var c = cf.GlyphPixels[glyph][x, y];
                    if(c.GetSaturation() == 0f)
                    {
                        float grey = c.R / 255f;
                        // grey = grey / 2f + .5f;
                        pixels2d[x, y] = Color.Lerp(newBackground, new Color((int)(newForeground.R * grey), (int)(newForeground.G * grey), (int)(newForeground.B * grey)), c.A / 255f);
                    }
                    else
                    {
                        pixels2d[x, y] = cf.GlyphPixels[glyph][x, y];
                    }
                }
            }

            cf.RecoloredGlyphPixels[key].Pixels = pixels2d;
            return cf.RecoloredGlyphPixels[key];
        }

        public static void CollectGarbage()
        {
            var now = DateTime.Now;

            var m_fore = Palette.Ui["BoringMessage"];
            var m_back = Palette.Ui["DefaultBackground"];

            foreach (CachedFont cf in CachedFonts.Values)
            {
                List<Tuple<byte, uint, uint>> to_remove = new List<Tuple<byte, uint, uint>>();
                foreach(var key in cf.RecoloredGlyphPixels.Keys)
                {
                    // Update hit delta to reflect now - last hit;
                    cf.RecoloredGlyphPixels[key].Hits = cf.RecoloredGlyphPixels[key].Hits;
                    // If too much time passed since the last hit
                    if (cf.RecoloredGlyphPixels[key].HitDelta >= HIT_DELTA_TOO_HIGH)
                    {
                        // And this sprite didn't get enough hits since the previous GC pass
                        var time_elapsed = now - lastGarbageCollection;
                        if (cf.RecoloredGlyphPixels[key].Hits <= HIT_COUNT_TOO_LOW * Util.Framerate * time_elapsed.TotalSeconds)
                        {
                            to_remove.Add(key);
                            ColoredString msg = new ColoredString("FontGC: Deallocated ", m_fore, m_back)
                                + new ColoredString(((char)key.Item1).ToString(), new Color(key.Item2), new Color(key.Item3))
                                + new ColoredString(String.Format(". ({0}hits/{1}s, last hit {2}s ago)", 
                                cf.RecoloredGlyphPixels[key].Hits, 
                                Math.Round(time_elapsed.TotalSeconds, 2), 
                                Math.Round(cf.RecoloredGlyphPixels[key].HitDelta.TotalSeconds, 2)), m_fore, m_back);
                            Util.Log(msg, true);
                            continue;
                        }
                        // Reset the number of hits to 0 so that this will get deallocated during the next pass if it doesn't get displayed again
                        cf.RecoloredGlyphPixels[key].Hits = 0;
                    }
                }
                to_remove.ForEach(k => cf.RecoloredGlyphPixels.Remove(k));
            }
            lastGarbageCollection = /*"*/now/*"*/;
        }
    }

    public static class Extensions
    {
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

        public static void DeepClone(this Color[,] c, ref Color[,] other)
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

        public static string Repeat(this string s, int n)
        {
            StringBuilder sb = new StringBuilder();
            while (n-- > 0) sb.Append(s);
            return sb.ToString();
        }

        public static string Repeat(this char c, int n)
        {
            StringBuilder sb = new StringBuilder();
            while (n-- > 0) sb.Append(c);
            return sb.ToString();
        }

        private static readonly Encoding asciiEncoding = Encoding.GetEncoding(437);

        public static string ToAscii(this string dirty)
        {
            byte[] bytes = asciiEncoding.GetBytes(dirty);
            string clean = asciiEncoding.GetString(bytes);
            return clean;
        }

        public static ColoredString ToColoredString(this string s, Color? fg = null, Color? bg = null)
        {
            return new ColoredString(s, fg ?? Color.White, bg ?? Color.Transparent);
        }

        public static ColoredString ToColoredString(this string s, Cell c)
        {
            return new ColoredString(s, c);
        }

        public static ColoredString ToColoredString(this int glyph, Cell c)
        {
            return new ColoredString(((char)glyph).ToString(), c);
        }

        public static ColoredString ToColoredString(this char glyph, Cell c)
        {
            return new ColoredString(glyph.ToString(), c);
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

        public static string CapitalizeFirst(this string s)
        {
            return Char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
