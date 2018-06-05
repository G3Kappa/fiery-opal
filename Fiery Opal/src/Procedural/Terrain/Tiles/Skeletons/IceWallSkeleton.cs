using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class IceWallSkeleton : NaturalWallSkeleton
    {
        public override OpalTileProperties DefaultProperties => new OpalTileProperties(
            is_block: true,
            is_natural: base.DefaultProperties.IsNatural
        );
        public override string DefaultName => "IceWall";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["IceForeground"], Palette.Terrain["IceBackground"], '/');
    }
}
