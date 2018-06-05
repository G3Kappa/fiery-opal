using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class FertileSoilSkeleton : DirtSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: base.DefaultProperties.IsBlock,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: 0f,
                fertility: 1f
            );
        public override string DefaultName => "FertileSoil";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["SoilForeground"], Palette.Terrain["SoilBackground"], '=');
    }
}
