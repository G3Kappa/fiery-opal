using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class DryGrassSkeleton : GrassSkeleton
    {
        public override string DefaultName => "DryGrass";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DryGrassForeground"], Palette.Terrain["DryGrassBackground"], ';');
    }
}
