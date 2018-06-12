using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace FieryOpal.Src.Procedural.Terrain.Dungeons
{
    /// <summary>
    /// Generates an entrance vault for a dungeon.
    /// </summary>
    class SakhalTombGenerator : BuildingGeneratorBase
    {
        public SakhalTombGenerator()
        {
        }

        private bool ValidTile(OpalLocalMap m, Tuple<OpalTile, Point> t)
        {
            return
                !Util.OOB(t.Item2.X, t.Item2.Y, m.Width - 20, m.Height - 20, 20, 20)
                && m.TilesWithinRing(t.Item2.X, t.Item2.Y, 5, 0)
                .All(x => !x.Item1.Properties.BlocksMovement);
        }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);
        }
    }
}
