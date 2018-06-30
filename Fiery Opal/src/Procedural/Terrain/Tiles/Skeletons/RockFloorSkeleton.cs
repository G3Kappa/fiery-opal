using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class RockFloorSkeleton : DirtSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: base.DefaultProperties.IsBlock,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: .1f,
                fertility: 0f
            );
        public override string DefaultName => "RockFloor";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["RockForeground"], Palette.Terrain["RockBackground"], '.');
    }
}
