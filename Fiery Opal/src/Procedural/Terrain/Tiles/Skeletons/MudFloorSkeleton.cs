using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class MudFloorSkeleton : RockFloorSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: base.DefaultProperties.IsBlock,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: .3f,
                fertility: .75f
            );
        public override string DefaultName => "MudFloor";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DirtForeground"], Palette.Terrain["DirtBackground"], 247);
    }
}
