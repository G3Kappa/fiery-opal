using FieryOpal.src.actors;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.procgen.Terrain
{
    public class OceanTerrainGenerator : BiomeTerrainGenerator
    {
        protected OceanTerrainGenerator(Point worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Tiles.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.Water);
                return false;
            });
        }
    }
}
