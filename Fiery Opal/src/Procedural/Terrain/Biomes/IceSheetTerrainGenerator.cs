using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Runtime.Serialization;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    // Terrain types: moss, mud, rock, grass

    [Serializable]
    public class IceSkeleton : WaterSkeleton
    {
        public override string DefaultName => "Ice Ground";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["IceForeground"], Palette.Terrain["IceBackground"], '/');
    }

    public class IceSheetTerrainGenerator : BiomeTerrainGenerator
    {
        protected IceSheetTerrainGenerator(Point worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<IceSkeleton>());
                return false;
            });
        }
    }
}
