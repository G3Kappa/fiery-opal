using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class ConstructedWallSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: true,
                is_natural: false,
                movement_penalty: 0f // Unneeded since it blocks movement
            );
        public override string DefaultName => "ConstructedWall";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["ConstructedWallForeground"], Palette.Terrain["ConstructedWallBackground"], 176);
    }
}
