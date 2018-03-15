using FieryOpal.Src.Lib;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Runtime.Serialization;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    // Terrain types: moss, mud, rock, grass

    public class IceSkeleton : NaturalFloorSkeleton
    {
        public override string DefaultName => "Ice Ground";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["IceForeground"], Palette.Terrain["IceBackground"], '.');
    }

    public class IceWallSkeleton : NaturalWallSkeleton
    {
        public override OpalTileProperties DefaultProperties => new OpalTileProperties(
            blocks_movement: true,
            is_natural: base.DefaultProperties.IsNatural
        );
        public override string DefaultName => "Ice Wall";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["IceForeground"], Palette.Terrain["IceBackground"], '/');
    }

    public class IceSheetTerrainGenerator : BiomeTerrainGenerator
    {
        protected IceSheetTerrainGenerator(Point worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            float[,] icebergNoise = Noise.Calc2D(
                WorldPosition.X * m.Width,
                WorldPosition.Y * m.Height,
                m.Width,
                m.Height,
                .023f,
                2,
                .8f
            );

            float[,] icebergMaskNoise = Noise.Calc2D(
                WorldPosition.X * m.Width,
                WorldPosition.Y * m.Height,
                m.Width,
                m.Height,
                .006f,
                3,
                1f
            );

            Tiles.Iter((s, x, y, t) =>
            {
                if(icebergNoise[x, y] * icebergMaskNoise[x, y] < .5f)
                {
                    s.SetTile(x, y, OpalTile.GetRefTile<IceSkeleton>());
                }
                else
                {
                    s.SetTile(x, y, OpalTile.GetRefTile<IceWallSkeleton>());
                }
                return false;
            });
        }
    }
}
