using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class SeasonalForestTerrainGenerator : BiomeTerrainGenerator
    {
        protected SeasonalForestTerrainGenerator(WorldTile worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<DryLeavesSkeleton>());
                return false;
            });
        }
    }
}
