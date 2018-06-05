using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class SnowSkeleton : DirtSkeleton
    {
        public override string DefaultName => "SnowGround";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["SnowForeground"], Palette.Terrain["SnowBackground"], '.');
    }
}
