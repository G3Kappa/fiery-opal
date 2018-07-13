using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class ConcreteFloorSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: false,
                is_natural: false,
                movement_penalty: -.1f // Unneeded since it blocks movement
            );
        public override string DefaultName => "ConcreteFloor";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["ConcreteFloorForeground"], Palette.Terrain["ConcreteFloorBackground"], 219);
    }
}
