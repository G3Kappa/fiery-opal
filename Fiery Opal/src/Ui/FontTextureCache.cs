using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Ui
{
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
            public List<byte> LumaSortedGlyphs;

            public CachedFont(Font f, Color[] fontImagePixels)
            {
                Font = f;
                FontImagePixels = fontImagePixels;
                GlyphPixels = new Dictionary<byte, Color[,]>();
                RecoloredGlyphPixels = new Dictionary<Tuple<byte, uint, uint>, RecoloredGlyph>();
                LumaSortedGlyphs = new List<byte>(255);
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

        private static float CalcLuma(Color[,] pixels)
        {
            int w = pixels.GetLength(0);
            int h = pixels.GetLength(1);

            float sum = 0;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    sum += pixels[x, y].GetLuma() / 255f;
                }
            }
            sum /= w * h;
            return sum;
        }

        private static List<byte> SortGlyphs(Font f)
        {
            List<Tuple<float, byte>> list = new List<Tuple<float, byte>>();
            for (byte i = 0; i < 255; ++i)
            {
                var pixels = GetRecoloredPixels(f, i, Color.White, Color.Black);
                list.Add(new Tuple<float, byte>(CalcLuma(pixels), i));
            }
            return list.OrderBy(e => e.Item1).Select(e => e.Item2).ToList();
        }

        public static void GetPixels(Font f, byte glyph, ref Color[,] pixels)
        {
            if (!CachedFonts.ContainsKey(f.Name))
            {
                Color[] data = new Color[f.FontImage.Width * f.FontImage.Height];
                f.FontImage.GetData(data);
                var cached_font = new CachedFont(f, data);
                CachedFonts[f.Name] = cached_font;
                cached_font.LumaSortedGlyphs.AddRange(SortGlyphs(f));
            }

            if (!CachedFonts[f.Name].GlyphPixels.ContainsKey(glyph))
            {
                CachedFonts[f.Name].GlyphPixels[glyph] = ObtainGlyphPixels(CachedFonts[f.Name], glyph);
            }

            CachedFonts[f.Name].GlyphPixels[glyph].DeepClone(ref pixels);
        }

        public static byte GetGlyphByBrightness(Font f, float brightness)
        {
            if (brightness < 0 || brightness >= 1)
            {
                return (byte)' ';
            }
            if (!CachedFonts.ContainsKey(f.Name))
            {
                GetRecoloredPixels(f, 0, Color.White, Color.Black);
            }

            return CachedFonts[f.Name].LumaSortedGlyphs[(int)(brightness * CachedFonts[f.Name].LumaSortedGlyphs.Count)];
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
                    if (c.GetSaturation() == 0f)
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
                foreach (var key in cf.RecoloredGlyphPixels.Keys)
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

}
