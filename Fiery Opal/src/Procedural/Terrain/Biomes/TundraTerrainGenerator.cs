using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class SnowSkeleton : WaterSkeleton
    {
        public override string DefaultName => "Snow Ground";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["SnowForeground"], Palette.Terrain["SnowBackground"], '.');
    }

    public class TundraTerrainGenerator : BiomeTerrainGenerator
    {
        protected TundraTerrainGenerator(Point worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<SnowSkeleton>());
                return false;
            });
        }
    }
}
