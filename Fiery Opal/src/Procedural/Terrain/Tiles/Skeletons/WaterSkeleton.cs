using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class WaterSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: false,
                is_natural: true,
                movement_penalty: 1.0f
            );
        public override string DefaultName => "Water";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["WaterForeground"], Palette.Terrain["WaterBackground"], 247);
    }
}
