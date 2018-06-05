using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class SandSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: false,
                is_natural: true
            );
        public override string DefaultName => "Sand";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["SandForeground"], Palette.Terrain["SandBackground"], 247);
    }
}
