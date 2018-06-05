using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class DirtSkeleton : NaturalFloorSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: base.DefaultProperties.IsBlock,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: .1f,
                fertility: .2f
            );
        public override string DefaultName => "Dirt";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DirtForeground"], Palette.Terrain["DirtBackground"], '.');
    }
}
