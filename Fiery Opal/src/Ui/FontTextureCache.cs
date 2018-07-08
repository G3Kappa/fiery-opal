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
            public Dictionary<ulong, RecoloredGlyph> RecoloredGlyphPixels;
            public List<byte> LumaSortedGlyphs;

            public CachedFont(Font f, Color[] fontImagePixels)
            {
                Font = f;
                FontImagePixels = fontImagePixels;
                GlyphPixels = new Dictionary<byte, Color[,]>();
                RecoloredGlyphPixels = new Dictionary<ulong, RecoloredGlyph>();
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

            var cached = CachedFonts[f.Name];
            if (!cached.GlyphPixels.ContainsKey(glyph))
            {
                cached.GlyphPixels[glyph] = ObtainGlyphPixels(cached, glyph);
            }

            cached.GlyphPixels[glyph].DeepClone(ref pixels);
        }

        public static Color[,] MakeLabel(Font f, string text, Color fg, Color bg)
        {
            Color[,] ret = new Color[text.Length * f.Size.X, f.Size.Y];
            for (int i = 0; i < text.Length; i++)
            {
                Color[,] pixels = GetRecoloredPixels(f, (byte)text[i], fg, bg);
                for (int x = 0; x < f.Size.X; x++)
                {
                    for (int y = 0; y < f.Size.Y; y++)
                    {
                        ret[i * f.Size.X + x, y] = pixels[x, y];
                    }
                }
            }
            return ret;
        }

        public static Color[,] MakeHealthBar(Font f, float pct)
        {
            const int LENGTH = 20;

            Color[,] ret = new Color[LENGTH * f.Size.X, f.Size.Y];

            Color bg = new Color(0, 0, 0, 64);
            for (int i = 0; i < LENGTH; i++)
            {
                char glyph = (i / (float)LENGTH) <= pct ? (char)177 : ' ';
                Color fg;
                if (pct > .75f) fg = Color.Lerp(Color.Green, Color.Yellow, (1 - pct) * 4);
                else if (pct > .5f) fg = Color.Lerp(Color.Yellow, Color.Red, (1 - pct) * 2);
                else fg = Color.Red;

                Color[,] pixels = GetRecoloredPixels(f, (byte)glyph, fg, bg);
                for (int x = 0; x < f.Size.X; x++)
                {
                    for (int y = 0; y < f.Size.Y; y++)
                    {
                        ret[i * f.Size.X + x, y] = pixels[x, y];
                    }
                }
            }
            return ret;
        }

        public static Color[,] MakeShadow(Font f, byte glyph = 7, Color? fg = null)
        {
            return GetCachedRecolor(f, glyph, fg ?? Color.White, Color.Transparent).Pixels;
        }

        public static Color[,] MakeInteractionMarker(Font f, byte glyph = 25, Color? fg = null)
        {
            return GetCachedRecolor(f, glyph, fg ?? Color.White, Color.Transparent).Pixels;
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

            var cf = CachedFonts[f.Name];
            return cf.LumaSortedGlyphs[(int)(brightness * cf.LumaSortedGlyphs.Count)];
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

            ulong key = (ulong)(glyph * 17 + newForeground.PackedValue * 17 + newBackground.PackedValue * 17);
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

            var rg = new CachedFont.RecoloredGlyph(glyph, newForeground.PackedValue, newBackground.PackedValue);
            cf.RecoloredGlyphPixels[key] = rg;
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

            rg.Pixels = pixels2d;
            return rg;
        }

        public static void CollectGarbage()
        {
            var now = DateTime.Now;

            var m_fore = Palette.Ui["BoringMessage"];
            var m_back = Palette.Ui["DefaultBackground"];

            foreach (CachedFont cf in CachedFonts.Values)
            {
                List<ulong> to_remove = new List<ulong>();
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
