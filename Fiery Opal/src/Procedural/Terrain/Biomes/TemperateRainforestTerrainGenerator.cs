using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using Microsoft.Xna.Framework;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class TemperateRainforestTerrainGenerator : BiomeTerrainGenerator
    {
        protected TemperateRainforestTerrainGenerator(WorldTile worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Workspace.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<GrassSkeleton>());
                return false;
            });
        }
    }
}
