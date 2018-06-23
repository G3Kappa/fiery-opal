using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural.Worldgen
{
    class ColonyFeatureGenerator : VillageFeatureGenerator
    {
        public int Radius { get; }
        public float Density { get; }

        public ColonyFeatureGenerator()
        {
            Radius = Util.Rng.Next(2, 12);
            Density = Util.Rng.Next(20, 70) / 100f;
            BaseGraphics.Glyph = 158;
        }

        protected override IEnumerable<Point> MarkRegions(World w)
        {
            // Pick a starting point
            // Generate settlements around that point
            // More settlements near the center
            // Only settlements in valid tiles
            Point c;
            int start_tries = 100;
            do
            {
                c = new Point(Util.Rng.Next(0, w.Width), Util.Rng.Next(0, w.Height));
            }
            while (!ValidRegion(w.RegionAt(c.X, c.Y)) && --start_tries > 0);
            if (start_tries < 0)
            {
                Util.LogText("WorldFeatureGenerator: Could not place colony.", true);
                yield break;
            }

            var poisson =
                Lib.PoissonDiskSampler.SampleCircle(c.ToVector2(), Radius, Math.Max(Radius - Radius * Density, 2))
                .Select(v => v.ToPoint())
                .Where(p => ValidRegion(w.RegionAt(p.X, p.Y)));

            foreach (var p in poisson)
                yield return p;
        }

        protected override void GenerateLocal(OpalLocalMap m, WorldTile parent)
        {

        }
    }
}
