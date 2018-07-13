using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class TiledFloor : TileSkeleton
    {
        public override string DefaultName => "TiledFloor";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["TiledFloorForeground"], Palette.Terrain["TiledFloorBackground"], '+');
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: false,
                is_natural: false,
                movement_penalty: -.1f, // Slightly favor constructed flooring over natural terrain
                ceiling_graphics: DefaultGraphics
            );
    }
}
