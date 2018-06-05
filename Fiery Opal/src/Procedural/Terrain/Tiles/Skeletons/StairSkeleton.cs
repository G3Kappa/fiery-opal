using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons
{
    public abstract class StairSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties => new OpalTileProperties(
            is_block: false,
            is_natural: false,
            movement_penalty: 0,
            fertility: 0
        );
        public override Cell DefaultGraphics => new Cell(Color.Red, Color.Blue, 'X');
        public override string DefaultName => "Stairs";

        public override OpalTile Make(int id)
        {
            return new StairTile(id, this, DefaultName, DefaultProperties, DefaultGraphics);
        }
    }

    public class DownstairSkeleton : StairSkeleton
    {
        public override OpalTileProperties DefaultProperties => base.DefaultProperties;
        public override Cell DefaultGraphics => new Cell(Color.White, Color.Black, '<');
        public override string DefaultName => "Downstairs";
    }

    public class UpstairSkeleton : StairSkeleton
    {
        public override OpalTileProperties DefaultProperties => base.DefaultProperties;
        public override Cell DefaultGraphics => new Cell(Color.White, Color.Black, '>');
        public override string DefaultName => "Upstairs";
    }
}
