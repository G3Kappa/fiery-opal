using FieryOpal.Src.Actors;
using FieryOpal.Src.Lib;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class GrassSkeleton : FertileSoilSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: base.DefaultProperties.BlocksMovement,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: .1f,
                fertility: .5f
            );
        public override string DefaultName => "Grass";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["GrassForeground"], Palette.Terrain["GrassBackground"], ',');
    }

    class GrasslandsTerrainGenerator : BiomeTerrainGenerator
    {

        protected GrasslandsTerrainGenerator(Point worldPos) : base(worldPos) { }

        private void PlaceShrub(OpalLocalMap m, int x, int y)
        {
            var bush = new Sapling();
            bush.ChangeLocalMap(m, new Point(x, y));
        }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            float[,] shrubNoise = Noise.Calc2D(
                WorldPosition.X * m.Width, 
                WorldPosition.Y * m.Height,
                m.Width,
                m.Height,
                .023f,
                8,
                .93f
            );

            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<GrassSkeleton>());

                if(shrubNoise[x, y] >= .5f)
                    PlaceShrub(s, x, y);

                return false;
            });
        }
    }
}
