using FieryOpal.Src.Actors;
using FieryOpal.Src.Lib;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using Microsoft.Xna.Framework;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    class GrasslandsTerrainGenerator : BiomeTerrainGenerator
    {

        protected GrasslandsTerrainGenerator(WorldTile worldPos) : base(worldPos) { }

        private void PlaceShrub(OpalLocalMap m, int x, int y)
        {
            var bush = new Sapling();
            bush.ChangeLocalMap(m, new Point(x, y));
        }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            float[,] shrubNoise = Noise.Calc2D(
                Region.WorldPosition.X * m.Width,
                Region.WorldPosition.Y * m.Height,
                m.Width,
                m.Height,
                .023f,
                8,
                .93f
            );

            Workspace.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<GrassSkeleton>());

                if (shrubNoise[x, y] >= .5f)
                    PlaceShrub(s, x, y);

                return false;
            });
        }
    }
}
