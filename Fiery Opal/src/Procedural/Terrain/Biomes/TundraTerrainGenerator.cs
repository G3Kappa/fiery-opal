using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class TundraTerrainGenerator : BiomeTerrainGenerator
    {
        protected TundraTerrainGenerator(WorldTile worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Workspace.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<SnowSkeleton>());
                return false;
            });
        }
    }
}
