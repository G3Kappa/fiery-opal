using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class DirtSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: false,
                is_natural: true,
                movement_penalty: .1f,
                fertility: .2f
            );
        public override string DefaultName => "Dirt";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DirtForeground"], Palette.Terrain["DirtBackground"], 1);
    }
}
