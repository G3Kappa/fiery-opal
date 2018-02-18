using FieryOpal.src.ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src
{
    public static class Util
    {
        public static readonly MessagePipeline<OpalConsoleWindow> GlobalLogPipeline = new MessagePipeline<OpalConsoleWindow>();

        public static Random GlobalRng { get; } = new Random(0);

        private static double framerate = 0;
        public static double Framerate => framerate;

        public static void Log(ColoredString msg, bool debug)
        {
            GlobalLogPipeline.BroadcastLogMessage(null, msg, debug);
        }

        public static void Update(GameTime gt)
        {
            framerate = (1 / gt.ElapsedGameTime.TotalSeconds);
        }

        public static void Log(String msg, bool debug, Color? fg = null, Color? bg = null)
        {
            Color fore = fg.HasValue ? fg.Value : Color.White;
            Color back = bg.HasValue ? bg.Value : Color.Black;

            GlobalLogPipeline.BroadcastLogMessage(null, new ColoredString(msg, fore, back), debug);
        }

        public static Point NormalizedStep(Vector2 v)
        {
            return new Point((v.X > 0 ? 1 : (v.X < 0 ? -1 : 0)), (v.Y > 0 ? 1 : (v.Y < 0 ? -1 : 0)));
        }

        public static Point NormalizedStep(Point p)
        {
            return NormalizedStep(new Vector2(p.X, p.Y));
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
                    pixels2d[x, y] = cf.GlyphPixels[glyph][x, y].A == 0 ? newBackground : newForeground;
                }
            }

            cf.RecoloredGlyphPixels[key].Pixels = pixels2d;
            return cf.RecoloredGlyphPixels[key];
        }

        public static void CollectGarbage()
        {
            var now = DateTime.Now;
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
                            ColoredString msg = new ColoredString("FontGC: Deallocated ")
                                + new ColoredString(((char)key.Item1).ToString(), new Color(key.Item2), new Color(key.Item3))
                                + new ColoredString(String.Format(". ({0}hits/{1}s, last hit {2}s ago)", 
                                cf.RecoloredGlyphPixels[key].Hits, 
                                Math.Round(time_elapsed.TotalSeconds, 2), 
                                Math.Round(cf.RecoloredGlyphPixels[key].HitDelta.TotalSeconds, 2)));
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
    }
}
