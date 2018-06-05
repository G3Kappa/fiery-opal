using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class NaturalWallSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: true,
                is_natural: true,
                movement_penalty: 0f,
                fertility: 0f
            );
        public override string DefaultName => "RockWall";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["RockWallForeground"], Palette.Terrain["RockWallBackground"], 177);
    }
}
