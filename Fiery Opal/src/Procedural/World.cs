using FieryOpal.Src.Lib;
using FieryOpal.Src.Procedural.Terrain;
using FieryOpal.Src.Procedural.Terrain.Biomes;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using FieryOpal.Src.Procedural.Worldgen;
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
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Woodland,            BiomeType.Savanna,              BiomeType.Desert },              //DRYER
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Desert },             //DRY
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.SeasonalForest, BiomeType.Grassland,      BiomeType.TemperateRainforest,        BiomeType.Savanna },             //WET
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.SeasonalForest,      BiomeType.TropicalRainforest, BiomeType.TropicalRainforest },  //WETTER
        { BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.TemperateRainforest,      BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest }   //WETTEST
        };

        private BiomeType GetBiomeType()
        {
            BiomeType baseType = BiomeTable[(int)AverageHumidity, (int)AverageTemperature];
            if (baseType == BiomeType.Ice) return baseType;

            switch (Elevation)
            {
                case BiomeElevationType.WayBelowSeaLevel:
                    return BiomeType.Ocean;
                case BiomeElevationType.BelowSeaLevel:
                    return BiomeType.Sea;
                case BiomeElevationType.AtSeaLevel:
                    return baseType;
                case BiomeElevationType.AboveSeaLevel:
                    return BiomeType.Mountain;
                case BiomeElevationType.WayAboveSeaLevel:
                    return BiomeType.Peak;
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

        private string _Name<T>(T t)
            where T : struct, IConvertible
        {
            return Enum.GetName(typeof(T), t);
        }

        public override string ToString()
        {
            string fmt = "BIOME: {0} | HEAT: {1} | MOISTURE: {2} | ELEVATION: {3}";
            return String.Format(fmt,
                _Name(Type), _Name(AverageTemperature),
                _Name(AverageHumidity), _Name(Elevation)
            );
        }
    }

    public struct WorldGenInfo
    {
        public float Elevation { get; }
        public float Temperature { get; }
        public float Moisture { get; }

        public WorldGenInfo(float temp, float moist, float elev)
        {
            Elevation = elev;
            Temperature = temp;
            Moisture = moist;
        }
    }

    public class WorldTile
    {
        public const int REGION_WIDTH = 60;
        public const int REGION_HEIGHT = 60;

        private static char GetGlyph(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    return (char)239;
                case BiomeType.Savanna:
                    return (char)231;
                case BiomeType.TropicalRainforest:
                    return (char)157;
                case BiomeType.Tundra:
                    return (char)177;
                case BiomeType.TemperateRainforest:
                    return (char)244;
                case BiomeType.Grassland:
                    return '"';
                case BiomeType.SeasonalForest:
                    return (char)5;
                case BiomeType.BorealForest:
                    return (char)24;
                case BiomeType.Woodland:
                    return (char)6;
                case BiomeType.Sea:
                case BiomeType.Ocean:
                    return (char)247;
                case BiomeType.Mountain:
                    return (char)127;
                case BiomeType.Peak:
                    return (char)30;

                case BiomeType.Ice:
                default:
                    return '/';
            }
        }

        private OpalLocalMap localMap = null;
        public OpalLocalMap LocalMap
        {
            get
            {
                bool first = localMap == null;
                var ret = (localMap = localMap ?? GenerateLocalMap());
                if (first) ret.GenerateWorldFeatures();
                return ret;
            }
        }

        private OpalLocalMap GenerateLocalMap()
        {
            var map = new OpalLocalMap(REGION_WIDTH, REGION_HEIGHT, this, Biome?.Type.ToString() ?? "Instance");
            TerrainGeneratorBase gen;
            if (Biome != null)
            {
                gen = BiomeTerrainGenerator.Make(Biome.Type, this);
            }
            else gen = new SimpleTerrainGenerator(this);

            map.GenerateAnew(gen);
            return map;
        }

        public Cell DefaultGraphics
        {
            get => new Cell(
                Palette.Terrain["Biome_" + Biome.Type.ToString() + "Foreground"],
                Palette.Terrain["Biome_" + Biome.Type.ToString() + "Background"],
                GetGlyph(Biome.Type)
            );
        }

        private Cell _graphics = null;
        public Cell Graphics
        {
            get { return _graphics ?? DefaultGraphics; }
            set { _graphics = value; }
        }
        public BiomeInfo Biome { get; set; }
        public World ParentWorld { get; }
        public Point WorldPosition { get; }
        public WorldGenInfo GenInfo { get; set; }

        public List<WorldFeatureGenerator> FeatureGenerators;

        public WorldTile(World parent, Point position)
        {
            ParentWorld = parent;
            WorldPosition = position;
            FeatureGenerators = new List<WorldFeatureGenerator>();
        }

        public override string ToString()
        {
            return String.Format("BIOME: {0}", Biome.ToString());
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
        public IEnumerable<WorldTile> RegionsWithin(Rectangle? R, bool yield_null = false)
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
                        yield return t;
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

        private IEnumerable<WorldFeatureGenerator> GetWFGs()
        {
            int n_rivers = Util.Rng.Next((int)(Width * Height * .006f));
            for (int i = 0; i < n_rivers; ++i)
                yield return new RiverFeatureGenerator(
                    (b) => b.AverageTemperature >= BiomeHeatType.Cold
                    ? OpalTile.GetRefTile<WaterSkeleton>()
                    : OpalTile.GetRefTile<FrozenWaterSkeleton>()
                );

            int n_colonies = Util.Rng.Next((int)(Width * Height * .0028f));
            for (int i = 0; i < n_colonies; ++i)
                yield return new ColonyFeatureGenerator();

            int n_villages = Util.Rng.Next((int)(Width * Height * .0035f));
            for (int i = 0; i < n_villages; ++i)
                yield return new VillageFeatureGenerator();

            int n_dungeons = Util.Rng.Next((int)(Width * Height * .0015f));
            for (int i = 0; i < n_dungeons; ++i)
                yield return new DungeonFeatureGenerator();
        }

        public void Generate()
        {
            var fElevMap = GenerateElevationMap();
            Apply(ref fElevMap, (x, y, f) => (float)Math.Pow(f, .9f));
            var fTempMap = GenerateTemperatureMap();
            Apply(ref fTempMap, (x, y, f) => .35f * f + .65f * (.99f - Math.Abs(y - .5f) * 2));
            var fRainMap = GenerateHumidityMap();
            Apply(ref fRainMap, (x, y, f) =>
                .25f * f
                + .5f * (1 - 2 * Math.Max(0, fElevMap[(int)(x * fElevMap.GetLength(0)), (int)(y * fElevMap.GetLength(1))] - .5f))
                + .25f * (1 - 1.75f * Math.Max(0, fTempMap[(int)(x * fTempMap.GetLength(0)), (int)(y * fTempMap.GetLength(1))] - .25f))
            );

            var tempMap = CastMap<BiomeHeatType>(fTempMap, (x, y, f) => f);
            var rainMap = CastMap<BiomeMoistureType>(fRainMap, (x, y, f) => f);
            var elevMap = CastMap<BiomeElevationType>(fElevMap, (x, y, f) => f);

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

            for (int x = 0; x < Width; ++x)
            {
                for (int y = 0; y < Height; ++y)
                {
                    var tile = Regions[x, y] = new WorldTile(this, new Point(x, y));
                    tile.Biome = new BiomeInfo(tempMap[x, y], rainMap[x, y], elevMap[x, y]);
                    tile.GenInfo = new WorldGenInfo(fTempMap[x, y], fRainMap[x, y], fElevMap[x, y]);
                }
            }
            // WFG here
            List<WorldFeatureGenerator> gens = new List<WorldFeatureGenerator>();
            gens.AddRange(GetWFGs());
            Dictionary<WorldFeatureGenerator, List<WorldTile>> regions = new Dictionary<WorldFeatureGenerator, List<WorldTile>>();
            foreach (var g in gens)
            {
                regions[g] = new List<WorldTile>();
                foreach (var t in g.GetMarkedRegions(this))
                {
                    var r = RegionAt(t.X, t.Y);

                    r.FeatureGenerators.Add(g);
                    regions[g].Add(r);
                }
            }

            foreach (var t in regions)
            {
                foreach (var r in t.Value)
                {
                    r.Graphics = t.Key.OverrideGraphics(r) ?? r.Graphics;
                }
            }
        }

        private float[,] GenerateTemperatureMap()
        {
            return Noise.Calc2D(
                Util.Rng.Next(1000),
                Util.Rng.Next(1000),
                Width,
                Height,
                .035f,
                4,
                1f);
        }

        private float[,] GenerateHumidityMap()
        {
            return Noise.Calc2D(
                Util.Rng.Next(1000),
                Util.Rng.Next(1000),
                Width,
                Height,
                .015f,
                8,
                .9f);
        }

        private float[,] GenerateElevationMap()
        {
            return Noise.Calc2D(
                Util.Rng.Next(1000),
                Util.Rng.Next(1000),
                Width,
                Height,
                .012f,
                7,
                .999f);
        }

        private void Apply(ref float[,] map, Func<float, float, float, float> f)
        {
            int w = map.GetLength(0);
            int h = map.GetLength(1);
            for (int i = 0; i < w; ++i)
                for (int j = 0; j < h; ++j)
                    map[i, j] = f(i / (float)w, j / (float)h, map[i, j]);
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
