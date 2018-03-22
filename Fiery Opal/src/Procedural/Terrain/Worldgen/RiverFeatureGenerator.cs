using FieryOpal.Src.Procedural.Terrain.Biomes;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural.Worldgen
{
    public class RiverFeatureGenerator : WorldFeatureGenerator
    {
        private Cell BaseGraphics = new Cell(Palette.Terrain["World_RiverForeground"], Color.Transparent, '~');
        public int Thickness { get; }

        protected Func<BiomeInfo, OpalTile> TileSelector;

        public RiverFeatureGenerator(Func<BiomeInfo, OpalTile> tiles)
        {
            Thickness = Util.GlobalRng.Next(2, 8) * 2 - 1;
            TileSelector = tiles;
        }

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
                && !HasRiver(t);
        }

        private bool HasRiver(WorldTile t)
        {
            return t.FeatureGenerators.Any(y => y.GetType() == typeof(RiverFeatureGenerator));
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
                var regions = w.RegionsWithin(new Rectangle(p.X - 1, p.Y - 1, 3, 3)).Where(r => r.WorldPosition.SquaredEuclidianDistance(p) < 2);

                // If any neighbour is a river, stop here and become its affluent.
                if (regions.Any(x => HasRiver(x)))
                {
                    break;
                }

                q = regions.MinBy(x => x.GenInfo.Elevation + (ValidRegion(x) ? 0 : 1f)).WorldPosition;
                if (q == p || !ValidRegion(w.RegionAt(q.X, q.Y))) break;
                yield return q;
                p = q;
            }
            while (true);
        }

        private Point NormalizeEdge(Point edge, OpalLocalMap m)
        {
            Point norm = new Point();
            norm.X = edge.X == 0
                ? m.Width / 2 - Thickness / 2
                : (edge.X == -1 ? 0 : m.Width - 1);

            norm.Y = edge.Y == 0
                ? m.Height / 2 - Thickness / 2
                : (edge.Y == -1 ? 0 : m.Height - 1);

            return norm;
        }

        protected override void GenerateLocal(OpalLocalMap m, WorldTile parent)
        {
            var p = parent.WorldPosition;
            var edges = parent.ParentWorld.RegionsWithin(
                new Rectangle(p.X - 1, p.Y - 1, 3, 3)
            )
            .Where(
                // Isn't this tile
                r => r.WorldPosition != p
                // Is non-diagonally adjacent
                && r.WorldPosition.SquaredEuclidianDistance(p) < 2
                // And has a river tile
                && HasRiver(r)
            )
            .Select(
                r => r.WorldPosition - p
            ).ToList();

            var tileref = TileSelector(parent.Biome);
            Point center = new Point(
                m.Width / 2,
                m.Height / 2
            );

            if (edges.Count == 2)
            {
                Point p1 = NormalizeEdge(edges[0], m);
                Point p2 = NormalizeEdge(edges[1], m);
                m.DrawCurve(p1, center, center, p2, tileref, Thickness, 100);
            }
            else
            {
                foreach (var edge in edges)
                {
                    Point norm = NormalizeEdge(edge, m);
                    m.DrawLine(norm, center - new Point(edge.X >= 0 ? Thickness / 2 : 0, edge.Y >= 0 ? Thickness / 2 : 0), tileref, Thickness);
                }
            }
        }
    }
}

