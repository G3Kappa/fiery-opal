using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural.Worldgen
{
    public class RiverGenerator : WorldFeatureGenerator
    {
        private Cell BaseGraphics = new Cell(Palette.Terrain["World_RiverForeground"], Color.Transparent, '~');

        public override Cell OverrideGraphics(WorldTile region)
        {
            return new Cell(BaseGraphics.Foreground, region.Graphics.Background, BaseGraphics.Glyph);
        }

        private bool ValidRegion(WorldTile t)
        {
            return 
                (t.Biome.AverageTemperature <= BiomeHeatType.Hotter && t.Biome.AverageTemperature >= BiomeHeatType.Colder)
                && t.Biome.AverageHumidity >= BiomeMoistureType.Dry
                && !new[] { BiomeType.Sea, BiomeType.Ocean }.Contains(t.Biome.Type) 
                && t.FeatureGenerators.All(x => x.GetType() != typeof(RiverGenerator));
        }

        protected override IEnumerable<Point> MarkRegions(World w)
        {
            Point p, q = new Point();
            int start_tries = 100;
            // Pick a (semi-)random point as seed for the river
            do
            {
                p = new Point(Util.GlobalRng.Next(0, w.Width), Util.GlobalRng.Next(0, w.Height));
            }
            while ((w.RegionAt(p.X, p.Y).GenInfo.Elevation <= .75f || !ValidRegion(w.RegionAt(p.X, p.Y))) && --start_tries > 0);
            yield return p;

            // Find the local minima of the elevation map from the starting point
            // and yield each traversed tile.
            do
            {
                var regions = w.RegionsWithin(new Rectangle(p.X - 1, p.Y - 1, 3, 3)).Where(r => r.Item2.SquaredEuclidianDistance(p) < 2);

                // If any neighbour is a river, stop here and become its affluent.
                if (regions.Any(x => x.Item1.FeatureGenerators.Any(y => y.GetType() == typeof(RiverGenerator))))
                {
                    break;
                }

                q = regions.MinBy(x => x.Item1.GenInfo.Elevation + (ValidRegion(x.Item1) ? 0 : 1f)).Item2;
                if (q == p || !ValidRegion(w.RegionAt(q.X, q.Y))) break;
                yield return q;
                p = q;
            }
            while (true);
        }

        protected override void GenerateLocal(OpalLocalMap m, WorldTile parent)
        {
            var tileref = OpalTile.GetRefTile<FertileSoilSkeleton>();
            m.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, (OpalTile)tileref.Clone());
                return false;
            });
        }
    }
}

