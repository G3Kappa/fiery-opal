using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    // Terrain types: moss, mud, rock, grass

    public class MossSkeleton : DirtSkeleton
    {
        public override string DefaultName => "Moss Ground";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["MossGroundForeground"], Palette.Terrain["MossGroundBackground"], 177);
    }

    public class TropicalRainforestTerrainGenerator : BiomeTerrainGenerator
    {
        protected TropicalRainforestTerrainGenerator(WorldTile worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<MossSkeleton>());
                return false;
            });
        }
    }
}
