using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace FieryOpal.src.procgen.Terrain
{
    public abstract class BiomeTerrainGenerator : TerrainGeneratorBase
    {
        protected static Dictionary<BiomeType, List<Type>> GeneratorTypes = new Dictionary<BiomeType, List<Type>>();

        protected BiomeTerrainGenerator(Point worldPosition) : base(worldPosition)
        {
        }

        public static void RegisterType<T>(BiomeType biome)
            where T : BiomeTerrainGenerator
        {
            if (!GeneratorTypes.ContainsKey(biome)) GeneratorTypes[biome] = new List<Type>();
            GeneratorTypes[biome].Add(typeof(T));
        }

        public static BiomeTerrainGenerator Make(BiomeType biome, Point worldPos)
        {
            if (!GeneratorTypes.ContainsKey(biome)) return null;
            Type T = Util.Choose(GeneratorTypes[biome]);
            var ctor = T.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Point) }, null);
            return ctor.Invoke(new object[] { worldPos }) as BiomeTerrainGenerator;
        }
    }
}
