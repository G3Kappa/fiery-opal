using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    // Terrain types: moss, mud, rock, grass

    public class DryLeavesSkeleton : DirtSkeleton
    {
        public override string DefaultName => "Dry Leaves";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DryLeavesForeground"], Palette.Terrain["DryLeavesBackground"], 236);
    }

    public class WoodlandTerrainGenerator : BiomeTerrainGenerator
    {
        protected WoodlandTerrainGenerator(Point worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<DryLeavesSkeleton>());
                return false;
            });
        }
    }
}
