using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class NaturalFloorSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: false,
                is_natural: true,
                movement_penalty: 0f,
                fertility: 0f
            );
        public override string DefaultName => "RockFloor";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["RockFloorForeground"], Palette.Terrain["RockFloorBackground"], '.');
    }
}
