using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.ui
{
    public class ColorPalette
    {
        protected Dictionary<string, uint> Palette = new Dictionary<string, uint>();
        public int Capacity { get; }

        public ColorPalette(int capacity)
        {
            Capacity = capacity;
        }

        public ColorPalette(IEnumerable<Tuple<string, Color>> colors)
        {
            Capacity = colors.Count();
            foreach(var t in colors)
            {
                Add(t.Item1, t.Item2);
            }
        }

        public bool Add(string name, Color c)
        {
            if(Palette.Keys.Count < Capacity)
            {
                if (Palette.ContainsKey(name)) return false;
                Palette[name] = c.PackedValue;
                return true;
            }
            return false;
        }

        public bool Remove(string name)
        {
            if (Palette.Keys.Count > 0)
            {
                if (!Palette.ContainsKey(name)) return false;
                Palette.Remove(name);
                return true;
            }
            return false;
        }

        public Color? Get(string name)
        {
            if (!Palette.ContainsKey(name)) return null;
            return new Color(Palette[name]);
        }

        public Color GetOrDefault(string name, Color def)
        {
            return Get(name) ?? def;
        }

        public static ColorPalette DefaultUiPalette = new ColorPalette(new[] {
            new Tuple<string, Color>("DefaultForeground", Color.White),
            new Tuple<string, Color>("DefaultBackground", Color.Black),

            new Tuple<string, Color>("DebugMessage",      Color.RoyalBlue),
            new Tuple<string, Color>("ErrorMessage",      Color.IndianRed),
            new Tuple<string, Color>("InfoMessage",       Color.Gold),
            new Tuple<string, Color>("BoringMessage",     Color.DarkGray),

        });
    }
}
