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
using System.Linq;
using System.Text.RegularExpressions;

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
        Hill,
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
        Bottom,
        WayWayBelowSeaLevel,
        WayBelowSeaLevel,
        BelowSeaLevel,
        AtSeaLevel,
        AboveSeaLevel,
        WayAboveSeaLevel,
        WayWayAboveSeaLevel,
        Top
    }

    [Serializable()]
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
                case BiomeElevationType.Bottom:
                    return BiomeType.Ocean;
                case BiomeElevationType.WayWayBelowSeaLevel:
                    return BiomeType.Ocean;
                case BiomeElevationType.WayBelowSeaLevel:
                    return BiomeType.Sea;
                case BiomeElevationType.BelowSeaLevel:
                    return baseType;
                case BiomeElevationType.AtSeaLevel:
                    return baseType;
                case BiomeElevationType.AboveSeaLevel:
                    return baseType;
                case BiomeElevationType.WayAboveSeaLevel:
                    return BiomeType.Hill;
                case BiomeElevationType.WayWayAboveSeaLevel:
                    return BiomeType.Mountain;
                case BiomeElevationType.Top:
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

    [Serializable()]
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

    [Serializable]
    public class WorldTile
    {
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
                case BiomeType.Hill:
                    return (char)239;
                case BiomeType.Mountain:
                    return (char)127;
                case BiomeType.Peak:
                    return (char)30;

                case BiomeType.Ice:
                default:
                    return '/';
            }
        }

        [NonSerialized()] // TODO: Serialize me
        private OpalLocalMap _localMap = null;
        public OpalLocalMap LocalMap
        {
            get
            {
                bool first = _localMap == null;
                var ret = (_localMap = _localMap ?? GenerateLocalMap());
                if (first) ret.GenerateWorldFeatures();
                return ret;
            }
        }

        private OpalLocalMap GenerateLocalMap()
        {
            var map = new OpalLocalMap(Nexus.InitInfo.RegionWidth, Nexus.InitInfo.RegionHeight, this, Biome?.Type.ToString() ?? "Instance");
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

        private SerializableCell _graphics = null;
        public Cell Graphics
        {
            get { return _graphics ?? DefaultGraphics; }
            set { _graphics = value; }
        }


        private BiomeInfo _biome;
        public BiomeInfo Biome { get => _biome; set => _biome = value; }

        private World _parent;
        public World ParentWorld => _parent;

        private SerializablePoint _wPos;
        public Point WorldPosition => _wPos;

        private WorldGenInfo _genInfo;
        public WorldGenInfo GenInfo { get => _genInfo; set => _genInfo = value; }


        [NonSerialized()] // TODO: Serialize me
        private List<WorldFeatureGenerator> _fGens = new List<WorldFeatureGenerator>();
        public List<WorldFeatureGenerator> FeatureGenerators => _fGens ?? (_fGens = new List<WorldFeatureGenerator>());

        public WorldTile(World parent, Point position)
        {
            _parent = parent;
            _wPos = position;
        }

        public override string ToString()
        {
            return String.Format("BIOME: {0}", Biome.ToString());
        }
    }

    [Serializable()]
    public class World : INamedObject
    {
        protected WorldTile[,] _regions;
        public WorldTile RegionAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
            return _regions[x, y];
        }

        [NonSerialized()]
        private float[,] m_SeaDT;
        public float[,] SeaDistanceTransform
        {
            get => m_SeaDT ?? (m_SeaDT = DistanceTransform(t => !new[] { BiomeType.Sea, BiomeType.Ocean }.Contains(t.Biome.Type)).Pow(.25f));
        }

        public float[,] DistanceTransform(Predicate<WorldTile> predicate)
        {
            bool[,] mask = new bool[Width, Height];

            for (int x = 0; x < Width; ++x)
            {
                for (int y = 0; y < Height; ++y)
                {
                    mask[x, y] = predicate(_regions[x, y]);
                }
            }

            return mask.DistanceTransform().Normalize((float)Math.Sqrt(Width * Width + Height * Height));
        }

        public IEnumerable<WorldTile> RegionsWithinRect(Rectangle? R, bool yield_null = false)
        {
            return _regions.ElementsWithinRect(R, yield_null).Select(t => t.Item1);
        }

        private int _w, _h;
        public int Width => _w;
        public int Height => _h;

        private string _name;
        public string Name { get => _name; private set => _name = value; }

        public World(int w, int h)
        {
            _regions = new WorldTile[w, h];
            _w = w;
            _h = h;
            Name = new WorldNameGenerator().GetName(null);
        }

        private IEnumerable<WorldFeatureGenerator> GetWFGs()
        {
            int n_rivers = Util.Rng.Next((int)(Width * Height * .006f)) + Util.Rng.Next(10, 30);
            for (int i = 0; i < n_rivers; ++i)
                yield return new RiverFeatureGenerator(
                    (b) => b.AverageTemperature >= BiomeHeatType.Cold
                    ? OpalTile.GetRefTile<WaterSkeleton>()
                    : OpalTile.GetRefTile<FrozenWaterSkeleton>()
                );

            int n_colonies = Util.Rng.Next((int)(Width * Height * 0));
            for (int i = 0; i < n_colonies; ++i)
                yield return new ColonyFeatureGenerator();

            int n_villages = Util.Rng.Next((int)(Width * Height * .0065f)) + Util.Rng.Next(10, 30);
            for (int i = 0; i < n_villages; ++i)
                yield return new VillageFeatureGenerator();

            int n_dungeons = Util.Rng.Next((int)(Width * Height * .0025f));
            for (int i = 0; i < n_dungeons; ++i)
                yield return new DungeonFeatureGenerator();
        }

        public void Generate()
        {
            var fElevMap = GenerateElevationMap();
            Apply(ref fElevMap, (x, y, f) => (float)Math.Pow((2 + f.ApplyContrast(.5f) + 1 * f + 3 * (float)Math.Pow(f, .5f) + 4 * (float)Math.Pow(f, 2f)) / 10f, 2f));
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
            BiomeTerrainGenerator.RegisterType<HillTerrainGenerator>(BiomeType.Hill);
            BiomeTerrainGenerator.RegisterType<MountainTerrainGenerator>(BiomeType.Mountain);
            BiomeTerrainGenerator.RegisterType<MountainTerrainGenerator>(BiomeType.Peak);

            for (int x = 0; x < Width; ++x)
            {
                for (int y = 0; y < Height; ++y)
                {
                    var tile = _regions[x, y] = new WorldTile(this, new Point(x, y));
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

        public void CreateSaveDataFolderStructure()
        {
            var path = $"./save/{Name}";

            int i = 2; string basePath = path;
            while(System.IO.Directory.Exists(path))
            {
                path = $"{basePath} ({i})";
            }
            DataFolder_Root = path;
            DataFolder_Regions = path + "/regions";
            DataFolder_Actors = path + "/actors";

            System.IO.Directory.CreateDirectory(DataFolder_Root);
            System.IO.Directory.CreateDirectory(DataFolder_Regions);
            System.IO.Directory.CreateDirectory(DataFolder_Actors);
        }

        public string DataFolder_Root { get; private set; }
        public string DataFolder_Regions { get; private set; }
        public string DataFolder_Actors { get; private set; }


        public void SaveToDisk(string fileName)
        {
            string fn = $"{DataFolder_Root}/{fileName}";
            Nexus.Serializer.SaveState(this, fn);
        }

        public void LoadFromDisk(string fileName)
        {
            string fn = $"{DataFolder_Root}/{fileName}";
            World state = (World)Nexus.Serializer.LoadState(fn);
            _regions = state._regions;
            _w = state._w;
            _h = state._h;
            _name = state._name;
        }

        private float[,] GenerateTemperatureMap()
        {
            return Noise.Calc2D(
                Util.Rng.Next(100000),
                Util.Rng.Next(100000),
                Width,
                Height,
                .035f,
                4,
                1f);
        }

        private float[,] GenerateHumidityMap()
        {
            return Noise.Calc2D(
                Util.Rng.Next(100000),
                Util.Rng.Next(100000),
                Width,
                Height,
                .015f,
                8,
                .9f);
        }

        private float[,] GenerateElevationMap()
        {
            return Noise.Calc2D(
                Util.Rng.Next(100000),
                Util.Rng.Next(100000),
                Width,
                Height,
                .014f,
                7,
                .999f
            );
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
                    ret[i, j] = values[((int)(normalize(i / (float)w, j / (float)h, map[i, j]) * values.Length)).Clamp(0, values.Length - 1)];

            return ret;
        }
    }
}
