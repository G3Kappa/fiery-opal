using FieryOpal.Src;
using FieryOpal.Src.Procedural;
using FieryOpal.Src.Procedural.Terrain;
using FieryOpal.Src.Procedural.Terrain.Biomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.Procedural.Terrain.Dungeons
{
    public class DungeonTerrainGenerator : TerrainGeneratorBase
    {
        public int Depth { get; }

        public DungeonTerrainGenerator(WorldTile region, int depth) : base(region)
        {
            Depth = depth;
        }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);
        }
    }
}
