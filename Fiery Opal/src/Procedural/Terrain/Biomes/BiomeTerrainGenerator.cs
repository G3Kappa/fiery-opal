using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public abstract class BiomeTerrainGenerator : TerrainGeneratorBase
    {
        protected static Dictionary<BiomeType, List<Type>> GeneratorTypes = new Dictionary<BiomeType, List<Type>>();

        protected BiomeTerrainGenerator(WorldTile region) : base(region)
        {
        }

        public static void RegisterType<T>(BiomeType biome)
            where T : BiomeTerrainGenerator
        {
            if (!GeneratorTypes.ContainsKey(biome)) GeneratorTypes[biome] = new List<Type>();
            GeneratorTypes[biome].Add(typeof(T));
        }

        public static BiomeTerrainGenerator Make(BiomeType biome, WorldTile region)
        {
            if (!GeneratorTypes.ContainsKey(biome)) return null;
            Type T = Util.Choose(GeneratorTypes[biome]);
            var ctor = T.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(WorldTile) }, null);
            return ctor.Invoke(new object[] { region }) as BiomeTerrainGenerator;
        }
    }
}
