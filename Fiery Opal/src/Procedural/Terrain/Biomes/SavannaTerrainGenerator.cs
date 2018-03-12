using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class DryGrassSkeleton : GrassSkeleton
    {
        public override string DefaultName => "Dry Grass";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DryGrassForeground"], Palette.Terrain["DryGrassBackground"], ';');
    }

    public class SavannaTerrainGenerator : BiomeTerrainGenerator
    {
        protected SavannaTerrainGenerator(Point worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<DryGrassSkeleton>());
                return false;
            });
        }
    }
}
