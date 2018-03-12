using FieryOpal.Src.Procedural.Terrain.Biomes;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;

namespace FieryOpal.Src.Procedural
{
    public enum BiomeType
    {
        Desert,
        Savanna,
        TropicalRainforest,
        Grassland,
        Woodland,
        SeasonalForest,
        TemperateRainforest,
        BorealForest,
        Tundra,
        Ice,

        // Unrelated to the BiomeTable
        Sea,
        Ocean,
        Mountain,
        Peak,
    }

    public enum BiomeHeatType
    {
        Coldest,
        Colder,
        Cold,
        Hot,
        Hotter,
        Hottest
    }

    public enum BiomeMoistureType
    {
        Dryest,
        Dryer,
        Dry,
        Wet,
        Wetter,
        Wettest
    }

    public enum BiomeElevationType
    {
        WayBelowSeaLevel,
        BelowSeaLevel,
        AtSeaLevel,
        AboveSeaLevel,
        WayAboveSeaLevel
    }

    public class BiomeInfo
    {
        public BiomeHeatType AverageTemperature;
        public BiomeMoistureType AverageHumidity;
        public BiomeElevationType Elevation;

        // http://www.jgallant.com/procedurally-generating-wrapping-world-maps-in-unity-csharp-part-4/
        static BiomeType[,] BiomeTable = new BiomeType[6, 6] {   
        //COLDEST        //COLDER          //COLD                  //HOT                          //HOTTER                       //HOTTEST
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYEST
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYER
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.Woodland,     BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //DRY
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //WET
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.SeasonalForest,      BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest },  //WETTER
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.TemperateRainforest, BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest }   //WETTEST
        };

        private BiomeType GetBiomeType()
        {
            BiomeType baseType = BiomeTable[(int)AverageHumidity, (int)AverageTemperature];
            if (baseType == BiomeType.Ice) return baseType;

            switch(Elevation)
            {
                case BiomeElevationType.WayBelowSeaLevel:
                    return BiomeType.Ocean;
                case BiomeElevationType.BelowSeaLevel:
                    return BiomeType.Sea;
                case BiomeElevationType.AtSeaLevel:
                    return baseType;
                case BiomeElevationType.AboveSeaLevel:
                case BiomeElevationType.WayAboveSeaLevel:
                    return BiomeType.Mountain;
                default:
                    return BiomeType.Ice;
            }

        }

        public BiomeType Type => GetBiomeType();

        public BiomeInfo(BiomeHeatType heat, BiomeMoistureType moisture, BiomeElevationType elev)
        {
            AverageHumidity = moisture;
            AverageTemperature = heat;
            Elevation = elev;
        }
    }

    public class WorldTile
    {
        public const int REGION_WIDTH = 40;
        public const int REGION_HEIGHT = 40;

        public static Palette BiomePalette = new Palette(new[] {
            new Tuple<string, Color>(BiomeType.Ice.ToString(), new Color(255, 255, 255)),
            new Tuple<string, Color>(BiomeType.Desert.ToString(), new Color(238, 219, 116)),
            new Tuple<string, Color>(BiomeType.Savanna.ToString(), new Color(178, 209, 92)),
            new Tuple<string, Color>(BiomeType.TropicalRainforest.ToString(), new Color(67, 124, 0)),
            new Tuple<string, Color>(BiomeType.Tundra.ToString(), new Color(96, 131, 109)),
            new Tuple<string, Color>(BiomeType.TemperateRainforest.ToString(), new Color(32, 72, 35)),
            new Tuple<string, Color>(BiomeType.Grassland.ToString(), new Color(165, 225, 70)),
            new Tuple<string, Color>(BiomeType.SeasonalForest.ToString(), new Color(78, 99, 22)),
            new Tuple<string, Color>(BiomeType.BorealForest.ToString(), new Color(94, 116, 53)),
            new Tuple<string, Color>(BiomeType.Woodland.ToString(), new Color(139, 176, 79)),

            new Tuple<string, Color>(BiomeType.Sea.ToString(), new Color(15, 40, 90)),
            new Tuple<string, Color>(BiomeType.Ocean.ToString(), new Color(15, 30, 80)),
            new Tuple<string, Color>(BiomeType.Mountain.ToString(), new Color(140, 145, 150)),
            new Tuple<string, Color>(BiomeType.Peak.ToString(), new Color(170, 175, 180)),
        });

        private static char GetGlyph(BiomeType biome)
        {
            switch(biome)
            {
                case BiomeType.Desert:
                    return (char)239;
                case BiomeType.Savanna:
                    return (char)226;
                case BiomeType.TropicalRainforest:
                    return (char)157;
                case BiomeType.Tundra:
                    return (char)178;
                case BiomeType.TemperateRainforest:
                    return (char)244;
                case BiomeType.Grassland:
                    return (char)231;
                case BiomeType.SeasonalForest:
                    return (char)5;
                case BiomeType.BorealForest:
                    return (char)6;
                case BiomeType.Woodland:
                    return (char)24;
                case BiomeType.Sea:
                    return (char)247;
                case BiomeType.Ocean:
                    return (char)126;
                case BiomeType.Mountain:
                    return (char)127;
                case BiomeType.Peak:
                    return (char)30;

                case BiomeType.Ice:
                default:
                    return (char)219;
            }
        }


        private OpalLocalMap localMap = null;
        public OpalLocalMap LocalMap
        {
            get
            {
                return (localMap = localMap ?? GenerateLocalMap());
            }
        }

        private OpalLocalMap GenerateLocalMap()
        {
            var map = new OpalLocalMap(REGION_WIDTH, REGION_HEIGHT);
            map.Generate(BiomeTerrainGenerator.Make(Biome.Type, WorldPosition));
            map.ParentRegion = this;
            return map;
        }

        public Cell Graphics => new Cell(BiomePalette[Biome.Type.ToString()], Color.Lerp(new Color(222, 221, 195), Color.Lerp(BiomePalette[Biome.Type.ToString()], Color.Black, 0.5f), .75f), GetGlyph(Biome.Type));
        public BiomeInfo Biome { get; set; }
        public World ParentWorld { get; }
        public Point WorldPosition { get; }

        public WorldTile(World parent, Point position)
        {
            ParentWorld = parent;
            WorldPosition = position;
        }
    }

    public class World
    {
        protected WorldTile[,] Regions;
        public WorldTile RegionAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
            return Regions[x, y];
        }
        public IEnumerable<Tuple<WorldTile, Point>> RegionsWithin(Rectangle? R, bool yield_null = false)
        {
            Rectangle r;
            if (!R.HasValue)
            {
                r = new Rectangle(0, 0, Width, Height);
            }
            else r = R.Value;

            for (int x = r.X; x < r.Width + r.X; ++x)
            {
                for (int y = r.Y; y < r.Height + r.Y; ++y)
                {
                    WorldTile t = RegionAt(x, y);
                    if ((!yield_null && t != null) || yield_null)
                    {
                        yield return new Tuple<WorldTile, Point>(t, new Point(x, y));
                    }
                }
            }
        }

        public int Width { get; }
        public int Height { get; }

        public World(int w, int h)
        {
            Regions = new WorldTile[w, h];
            Width = w;
            Height = h;
        }

        public void Generate()
        {
            var heightmap = GenerateElevationMap();

            var tempMap = CastMap<BiomeHeatType>(GenerateTemperatureMap(), (x, y, f) => .25f * f + .75f * (.99f - Math.Abs(y - .5f) * 2));
            var rainMap = CastMap<BiomeMoistureType>(GenerateHumidityMap(), (x, y, f) => .6f * f + .4f * (1 - heightmap[(int)(x * heightmap.GetLength(0)), (int)(y * heightmap.GetLength(1))]));
            var elevMap = CastMap<BiomeElevationType>(heightmap, (x, y, f) => (1-(float)new Vector2(x, y).Dist(new Vector2(.5f, .5f))) * ((float)Math.Pow(f, .5f) -.1f));

            for (int x = 0; x < Width; ++x)
            {
                for(int y = 0; y < Height; ++y)
                {
                    var tile = Regions[x, y] = new WorldTile(this, new Point(x, y));
                    tile.Biome = new BiomeInfo(tempMap[x, y], rainMap[x, y], elevMap[x, y]);
                }
            }

            BiomeTerrainGenerator.RegisterType<OceanTerrainGenerator>(BiomeType.Ocean);
            BiomeTerrainGenerator.RegisterType<OceanTerrainGenerator>(BiomeType.Sea);
            BiomeTerrainGenerator.RegisterType<DesertTerrainGenerator>(BiomeType.Desert);
            BiomeTerrainGenerator.RegisterType<SavannaTerrainGenerator>(BiomeType.Savanna);
            BiomeTerrainGenerator.RegisterType<TropicalRainforestTerrainGenerator>(BiomeType.TropicalRainforest);
            BiomeTerrainGenerator.RegisterType<IceSheetTerrainGenerator>(BiomeType.Ice);
            BiomeTerrainGenerator.RegisterType<WoodlandTerrainGenerator>(BiomeType.Woodland);
            BiomeTerrainGenerator.RegisterType<TemperateRainforestTerrainGenerator>(BiomeType.TemperateRainforest);
            BiomeTerrainGenerator.RegisterType<SeasonalForestTerrainGenerator>(BiomeType.SeasonalForest);
            BiomeTerrainGenerator.RegisterType<BorealForestTerrainGenerator>(BiomeType.BorealForest);
            BiomeTerrainGenerator.RegisterType<TundraTerrainGenerator>(BiomeType.Tundra);
            BiomeTerrainGenerator.RegisterType<GrasslandsTerrainGenerator>(BiomeType.Grassland);
            BiomeTerrainGenerator.RegisterType<MountainTerrainGenerator>(BiomeType.Mountain);
            BiomeTerrainGenerator.RegisterType<MountainTerrainGenerator>(BiomeType.Peak);

        }

        private float[,] GenerateTemperatureMap()
        {
            return Simplex.Noise.Calc2D(
                Util.GlobalRng.Next(1000),
                Util.GlobalRng.Next(1000),
                Width,
                Height,
                .035f,
                2,
                1f);
        }

        private float[,] GenerateHumidityMap()
        {
            return Simplex.Noise.Calc2D(
                Util.GlobalRng.Next(1000),
                Util.GlobalRng.Next(1000),
                Width,
                Height,
                .033f,
                4,
                1f);
        }

        private float[,] GenerateElevationMap()
        {
            return Simplex.Noise.Calc2D(
                Util.GlobalRng.Next(1000),
                Util.GlobalRng.Next(1000),
                Width,
                Height,
                .05f,
                2,
                1);
        }

        private T[,] CastMap<T>(float[,] map, Func<float, float, float, float> normalize)
            where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum) throw new ArgumentException();

            int w = map.GetLength(0);
            int h = map.GetLength(1);
            T[,] ret = new T[w, h];
            T[] values = (T[])Enum.GetValues(typeof(T));

            for (int i = 0; i < w; ++i)
                for (int j = 0; j < h; ++j)
                    ret[i, j] = values[(int)(normalize(i / (float)w, j / (float)h, map[i, j]) * values.Length)];

            return ret;
        }
    }
}
