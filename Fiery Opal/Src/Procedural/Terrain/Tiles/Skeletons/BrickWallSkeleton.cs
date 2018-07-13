using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class BrickWallSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: true,
                is_natural: false,
                movement_penalty: 0f // Unneeded since it blocks movement
            );
        public override string DefaultName => "Brick Wall";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["BrickWallForeground"], Palette.Terrain["BrickWallBackground"], 176);
    }
}
