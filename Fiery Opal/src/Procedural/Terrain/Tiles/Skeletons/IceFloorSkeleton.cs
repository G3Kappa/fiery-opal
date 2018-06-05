using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class IceFloorSkeleton : NaturalFloorSkeleton
    {
        public override string DefaultName => "IceGround";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["IceForeground"], Palette.Terrain["IceBackground"], '.');
    }
}
