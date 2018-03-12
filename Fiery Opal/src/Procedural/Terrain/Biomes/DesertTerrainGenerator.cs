using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class SandSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: false,
                is_natural: true
            );
        public override string DefaultName => "Sand";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["SandForeground"], Palette.Terrain["SandBackground"], 247);
    }

    public class DesertTerrainGenerator : BiomeTerrainGenerator
    {
        protected DesertTerrainGenerator(Point worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<SandSkeleton>());
                return false;
            });
        }
    }
}
