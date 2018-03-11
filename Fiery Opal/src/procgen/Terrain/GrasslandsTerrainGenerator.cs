using FieryOpal.src.actors;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.procgen.Terrain
{
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

            float[,] shrubNoise = Simplex.Noise.Calc2D(
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
                s.SetTile(x, y, OpalTile.Grass);

                if(shrubNoise[x, y] >= .5f)
                    PlaceShrub(s, x, y);

                return false;
            });
        }
    }
}
