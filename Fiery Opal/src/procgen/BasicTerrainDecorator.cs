using FieryOpal.src.actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.procgen
{
    public class BasicTerrainDecorator : IOpalFeatureGenerator
    {
        float[,] Noise;
        float[,] DecorNoise;
        private OpalLocalMap CurrentMap;

        public void Generate(OpalLocalMap m)
        {
            Noise = Simplex.Noise.Calc2D(Util.GlobalRng.Next(0, 1000), Util.GlobalRng.Next(0, 1000), m.Width, m.Height, .035f);
            DecorNoise = Simplex.Noise.Calc2D(Util.GlobalRng.Next(0, 1000), Util.GlobalRng.Next(0, 1000), m.Width, m.Height, .05f);
            float[,] mask = Simplex.Noise.Calc2D(Util.GlobalRng.Next(0, 1000), Util.GlobalRng.Next(0, 1000), m.Width, m.Height, .0055f);
            float[,] distField = m.CalcDistanceField((p, q) => (float)p.Dist(q), (t) => t.Properties.BlocksMovement ? 0 : 1);
            m.Iter((self, x, y, t) =>
            {
                Noise[x, y] *= (mask[x, y] / 255f);
                Noise[x, y] /= 255f;
                DecorNoise[x, y] /= 255f;
                Noise[x, y] = (2 * (float)Util.GlobalRng.NextDouble() + 20 * Noise[x, y] + 50 * (float)Math.Pow(Math.Min(4 * distField[x, y], 0.999f), 1 / 2f)) / 72f;
                return false;
            });
            CurrentMap = m;
        }

        public OpalTile Get(int x, int y)
        {
            if (!(CurrentMap.TileAt(x, y).Skeleton is DirtSkeleton)) return null;
            if (DecorNoise[x, y] < .25) return null;

            return (OpalTile)new[] { OpalTile.Grass, OpalTile.MediumGrass, OpalTile.TallGrass }[(int)((DecorNoise[x, y] + Noise[x, y]) / 2f * 3)].Clone();
        }

        public IDecoration GetDecoration(int x, int y)
        {
            if (!(CurrentMap.TileAt(x, y).Skeleton is FertileSoilSkeleton)) return null;

            if ((DecorNoise[x, y] > .4f || (DecorNoise[x, y] + Noise[x, y]) / 2f > .8f) && Util.GlobalRng.NextDouble() > .4)
            {
                if ((2 * DecorNoise[x, y] + Noise[x, y]) / 3f > .75f)
                {
                    return new Plant();
                }
                IDecoration decoration;
                if (Util.GlobalRng.NextDouble() < .05)
                {
                    decoration = new Mushroom();
                }
                else
                {
                    decoration = new Sapling();
                }
                return decoration;
            }
            return null;
        }
    }
}
