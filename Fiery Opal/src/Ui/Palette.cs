using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Ui
{
    public class Palette
    {

        protected Dictionary<string, uint> Dict = new Dictionary<string, uint>();
        public int Capacity { get; }

        public Palette(int capacity)
        {
            Capacity = capacity;
        }

        public Palette(IEnumerable<Tuple<string, Color>> colors)
        {
            Capacity = colors.Count();
            foreach (var t in colors)
            {
                Add(t.Item1, t.Item2);
            }
        }

        public bool Add(string name, Color c)
        {
            if (Dict.Keys.Count < Capacity)
            {
                if (Dict.ContainsKey(name)) return false;
                Dict[name] = c.PackedValue;
                return true;
            }
            return false;
        }

        public bool Remove(string name)
        {
            if (Dict.Keys.Count > 0)
            {
                if (!Dict.ContainsKey(name)) return false;
                Dict.Remove(name);
                return true;
            }
            return false;
        }

        public Color? Get(string name)
        {
            if (!Dict.ContainsKey(name)) return null;
            return new Color(Dict[name]);
        }

        public Color GetOrDefault(string name, Color def)
        {
            return Get(name) ?? def;
        }

        public Color this[string c]
        {
            get
            {
                Color ret = GetOrDefault(c, Color.Magenta);
                if (ret == Color.Magenta)
                {
                    if (c.Contains("Foreground")) return Color.Magenta;
                    else if (c.Contains("Background")) return Color.DarkMagenta;
                }
                return ret;
            }
        }

        private static Tuple<string, Color> KVPToTuple(KeyValuePair<string, Color> kvp)
        {
            return new Tuple<string, Color>(kvp.Key, kvp.Value);
        }

        public static void LoadDefaults(PaletteConfigInfo cfg)
        {
            Palette.Ui = new Palette(cfg.Ui.Select(KVPToTuple).ToList());
            Palette.Terrain = new Palette(cfg.Terrain.Select(KVPToTuple).ToList());
            Palette.Vegetation = new Palette(cfg.Vegetation.Select(KVPToTuple).ToList());
            Palette.Creatures = new Palette(cfg.Creatures.Select(KVPToTuple).ToList());
        }

        public static Cell DefaultTextStyle
        {
            get
            {
                if (Ui.Capacity > 0)
                {
                    return new Cell(
                        Ui["DefaultForeground"],
                        Ui["DefaultBackground"]
                    );
                }
                return new Cell(Color.White, Color.Black);
            }
        }

        public static Palette Ui = new Palette(0);
        public static Palette Vegetation = new Palette(0);
        public static Palette Terrain = new Palette(0);
        public static Palette Creatures = new Palette(0);
    }
}
