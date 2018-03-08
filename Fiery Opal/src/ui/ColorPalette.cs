using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.ui
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
            foreach(var t in colors)
            {
                Add(t.Item1, t.Item2);
            }
        }

        public bool Add(string name, Color c)
        {
            if(Dict.Keys.Count < Capacity)
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
            get => GetOrDefault(c, Color.Magenta);
        }

        public static Palette Ui = new Palette(new[] {
            new Tuple<string, Color>("DefaultForeground", Color.White),
            new Tuple<string, Color>("DefaultBackground", new Color(0, 0, 0)),

            new Tuple<string, Color>("UnseenTileForeground", Color.Gray),
            new Tuple<string, Color>("UnseenTileBackground", new Color(50, 50, 50)),

            new Tuple<string, Color>("DebugMessage",      Color.RoyalBlue),
            new Tuple<string, Color>("ErrorMessage",      Color.IndianRed),
            new Tuple<string, Color>("InfoMessage",       Color.Gold),
            new Tuple<string, Color>("BoringMessage",     Color.DarkGray),
        });

        public static Cell DefaultTextStyle = new Cell(
            Ui["DefaultForeground"],
            Ui["DefaultBackground"]
        );

        public static Palette Vegetation = new Palette(new[] {
            new Tuple<string, Color>("GenericPlant1", Color.LawnGreen),
            new Tuple<string, Color>("GenericPlant2", Color.LimeGreen),
            new Tuple<string, Color>("GenericPlant3", Color.SpringGreen),
            new Tuple<string, Color>("GenericPlant4", Color.SaddleBrown),

            new Tuple<string, Color>("Flower1", new Color(100, 210, 255)),
            new Tuple<string, Color>("Flower2", new Color(255, 100, 180)),
            new Tuple<string, Color>("Flower3", new Color(255, 180, 100)),
            new Tuple<string, Color>("Flower4", new Color(255, 255, 210)),
            new Tuple<string, Color>("Flower5", new Color(180, 100, 255)),
        });

        public static Palette Terrain = new Palette(new[] {
            new Tuple<string, Color>("DirtForeground", Color.SandyBrown),
            new Tuple<string, Color>("DirtBackground", new Color(128, 92, 70)),
            new Tuple<string, Color>("SoilForeground", Color.Gold),
            new Tuple<string, Color>("SoilBackground", new Color(128, 92, 70)),
            new Tuple<string, Color>("GrassForeground", Color.Green),
            new Tuple<string, Color>("GrassBackground", Color.LawnGreen),
            new Tuple<string, Color>("MediumGrassForeground", Color.LawnGreen),
            new Tuple<string, Color>("MediumGrassBackground", Color.Green),
            new Tuple<string, Color>("TallGrassForeground", Color.Green),
            new Tuple<string, Color>("TallGrassBackground", Color.ForestGreen),

            new Tuple<string, Color>("RockFloorForeground", Color.SlateGray),
            new Tuple<string, Color>("RockFloorBackground", Color.DarkSlateGray),
            new Tuple<string, Color>("RockWallForeground", Color.SlateGray),
            new Tuple<string, Color>("RockWallBackground", Color.DarkSlateGray),

            new Tuple<string, Color>("ConstructedWallForeground", Color.DarkGray),
            new Tuple<string, Color>("ConstructedWallBackground", Color.LightGray),
            new Tuple<string, Color>("ConstructedFloorForeground", Color.LightGray),
            new Tuple<string, Color>("ConstructedFloorBackground", Color.DarkGray),
            new Tuple<string, Color>("DoorForeground", Color.SandyBrown),
            new Tuple<string, Color>("DoorBackground", new Color(128, 92, 70)),
        });
    }
}
