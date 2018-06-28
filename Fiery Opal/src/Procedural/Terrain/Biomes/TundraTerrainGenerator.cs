using FieryOpal.Src.Actors;
using FieryOpal.Src.Lib;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using Microsoft.Xna.Framework;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class TundraTerrainGenerator : BiomeTerrainGenerator
    {
        protected TundraTerrainGenerator(WorldTile worldPos) : base(worldPos) { }

        private void PlaceShrub(OpalLocalMap m, int x, int y)
        {
            var bush = new RedSapling();
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
                .026f,
                8,
                .86f
            );

            float[,] dirtNoise = Noise.Calc2D(
                Region.WorldPosition.X * m.Width,
                Region.WorldPosition.Y * m.Height,
                m.Width,
                m.Height,
                .06f,
                4,
                1f
            );

            float[,] wallNoise = Noise.Calc2D(
                (Region.ParentWorld.Width + Region.WorldPosition.X) * m.Width,
                (Region.ParentWorld.Height + Region.WorldPosition.Y) * m.Height,
                m.Width,
                m.Height,
                .008f,
                3,
                1f
            );

            float[,] wallDt = wallNoise
                .DistanceTransform((x, y) => wallNoise[x, y] < .8f && wallNoise[x, y] >= .65f ? 1f : 0f)
                .Normalize(m.Width * m.Height)
                .Pow(.5f);

            Workspace.Iter((s, x, y, t) =>
            {
                if (wallNoise[x, y] < .8f && wallNoise[x, y] >= .65f)
                {
                    s.SetTile(x, y, OpalTile.GetRefTile<NaturalWallSkeleton>());
                }
                else if (wallDt[x, y] + (dirtNoise[x, y] - .5) / 4 < .1f)
                {
                    s.SetTile(x, y, OpalTile.GetRefTile<DirtSkeleton>());

                    if (wallDt[x, y] + (dirtNoise[x, y] - .5) / 4 < .06f && Util.Rng.NextDouble() >= shrubNoise[x, y])
                        PlaceShrub(s, x, y);
                }
                else
                {
                    s.SetTile(x, y, OpalTile.GetRefTile<SnowSkeleton>());
                }

                return false;
            });
        }
    }
}
