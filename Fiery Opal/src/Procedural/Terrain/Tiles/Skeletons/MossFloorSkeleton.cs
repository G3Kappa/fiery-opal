using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class MossFloorSkeleton : DirtSkeleton
    {
        public override string DefaultName => "MossGround";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["MossGroundForeground"], Palette.Terrain["MossGroundBackground"], 177);
    }
}
