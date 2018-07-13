using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public class DoorSkeleton : BrickWallSkeleton
    {
        public override string DefaultName => "Door";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DoorForeground"], Palette.Terrain["DoorBackground"], 197);
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: true,
                is_natural: false,
                movement_penalty: 0f,
                ceiling_graphics: DefaultGraphics
            );

        public override OpalTile Make(int id)
        {
            return new DoorTile(id, this, DefaultName, DefaultProperties, DefaultGraphics);
        }
    }
}
