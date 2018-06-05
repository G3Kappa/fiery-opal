using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class DebugFloorSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: false,
                is_natural: false,
                movement_penalty: 0f,
                fertility: 0f
            );
        public override string DefaultName => "<DebugFloor>";
        private Cell graphics = new Cell(Palette.Terrain[""], Palette.Terrain[""], '?');
        public override Cell DefaultGraphics => graphics;

        public DebugFloorSkeleton SetGraphics(Cell c)
        {
            graphics = c;
            return this;
        }
    }

}
