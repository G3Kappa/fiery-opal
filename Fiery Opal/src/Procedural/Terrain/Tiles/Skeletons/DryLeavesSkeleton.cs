using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class DryLeavesSkeleton : DirtSkeleton
    {
        public override string DefaultName => "DryLeaves";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DryLeavesForeground"], Palette.Terrain["DryLeavesBackground"], 236);
    }
}
