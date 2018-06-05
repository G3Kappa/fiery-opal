using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class GrassSkeleton : FertileSoilSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: base.DefaultProperties.IsBlock,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: .1f,
                fertility: .5f
            );
        public override string DefaultName => "Grass";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["GrassForeground"], Palette.Terrain["GrassBackground"], ',');
    }
}
