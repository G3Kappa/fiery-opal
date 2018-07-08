using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class ConstructedFloorSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: false,
                is_natural: false,
                movement_penalty: -.1f, // Slightly favor constructed flooring over natural terrain
                has_roof: true
            );
        public override string DefaultName => "ConstructedFloor";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["ConstructedFloorForeground"], Palette.Terrain["ConstructedFloorBackground"], '+');
    }
}
