using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;


namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class WaterSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: false,
                is_natural: true,
                movement_penalty: 1.0f
            );
        public override string DefaultName => "Water";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["WaterForeground"], Palette.Terrain["WaterBackground"], 247);
    }

    public class OceanTerrainGenerator : BiomeTerrainGenerator
    {
        protected OceanTerrainGenerator(WorldTile worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<WaterSkeleton>());
                return false;
            });
        }
    }
}
