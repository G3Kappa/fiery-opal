using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class FrozenWaterSkeleton : WaterSkeleton
    {
        public override OpalTileProperties DefaultProperties => new OpalTileProperties(
            is_block: base.DefaultProperties.IsBlock,
            is_natural: base.DefaultProperties.IsNatural,
            movement_penalty: 0
        );

        public override string DefaultName => "FrozenWater";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["IceForeground"], Palette.Terrain["IceBackground"], 247);
    }

}
