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
        protected virtual Cell BaseGraphics => new Cell(Palette.Terrain["World_RiverForeground"], Color.Transparent, 247);
        public int Thickness { get; protected set; }

        protected Func<BiomeInfo, OpalTile> TileSelector;

        public RiverFeatureGenerator(Func<BiomeInfo, OpalTile> tiles)
        {
            Thickness = Util.Rng.Next(2, 8) * 2 - 1;
            TileSelector = tiles;
        }

        public override Cell OverrideGraphics(WorldTile region)
        {
            var edges = GetEdges(region).ToList();

            int glyph = BaseGraphics.Glyph;
            if (edges.Count == 1)
            {
                if (edges[0].X < 0) glyph = 181;
                else if (edges[0].X > 0) glyph = 198;
                else if (edges[0].Y < 0) glyph = 208;
                else if (edges[0].Y > 0) glyph = 210;
            }
            else if (edges.Count == 2)
            {
                var p1 = edges[0].X != 0 ? edges[0] : edges[1];
                var p2 = p1 == edges[0] ? edges[1] : edges[0];

                // -1, 0; 0, -1 ╝
                if (p1.X < 0 && p2.Y < 0) glyph = 188;
                // -1, 0; 0, 1 ╗
                else if (p1.X < 0 && p2.Y > 0) glyph = 187;
                // 1, 0; 0, -1 ╚
                else if (p1.X > 0 && p2.Y < 0) glyph = 200;
                // 1, 0; 0, 1 ╔
                else if (p1.X > 0 && p2.Y > 0) glyph = 201;
                // Vertical
                else if (p1.X == p2.X) glyph = 186;
                // Horizontal
                else if (p1.Y == p2.Y) glyph = 205;
            }
            else if (edges.Count == 3)
            {
                if (!edges.Contains(new Point(-1, 0))) glyph = 204;
                else if (!edges.Contains(new Point(1, 0))) glyph = 185;
                else if (!edges.Contains(new Point(0, -1))) glyph = 203;
                else if (!edges.Contains(new Point(0, 1))) glyph = 202;
            }
            else if (edges.Count == 4)
            {
                glyph = 206;
            }

            return new Cell(BaseGraphics.Foreground, region.Graphics.Background, glyph);
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
            return t.FeatureGenerators.Any(y => y.GetType() == this.GetType());
        }

        private IEnumerable<Point> Descend(World w)
        {
            Point p, q = new Point();
            int start_tries = 100;
            // Pick a (semi-)random point as seed for the river
            do
            {
                p = new Point(Util.Rng.Next(0, w.Width), Util.Rng.Next(0, w.Height));
            }
            while ((w.RegionAt(p.X, p.Y).GenInfo.Elevation <= .75f || !ValidRegion(w.RegionAt(p.X, p.Y))) && --start_tries > 0);
            if (start_tries < 0)
            {
                Util.LogText("WorldFeatureGenerator: Could not place river.", true);
                yield break;
            }
            yield return p;

            // Find the local minima of the distance transform of the world (sea/inland) from the starting point
            // and yield each traversed tile. Stop early if we reach the sea.
            var dt = w.SeaDT;
            do
            {
                var regions = dt.ElementsWithinRect(new Rectangle(p.X - 1, p.Y - 1, 3, 3)).Where(r => r.Item2.SquaredEuclidianDistance(p) < 2)
                    .Select(r => new Tuple<float, Point>(
                        (float)Math.Pow(dt[r.Item2.X, r.Item2.Y], 1 / Math.Pow(w.RegionAt(r.Item2.X, r.Item2.Y).GenInfo.Elevation, 3)),
                        r.Item2
                    )).ToList();

                // If any neighbour is a river, stop here and become an affluent.
                if (regions.Any(r => HasRiver(w.RegionAt(r.Item2.X, r.Item2.Y))))
                {
                    break;
                }

                q = regions.MinBy(r => r.Item1).Item2;
                if (q == p || !ValidRegion(w.RegionAt(q.X, q.Y))) break;
                yield return q;
                p = q;
            }
            while (true);
        }

        protected override IEnumerable<Point> MarkRegions(World w)
        {
            var points = Descend(w).ToList();
            var unique = points.Where(p => points.Count(q => q == p) == 1);
            if (unique.Count() > 3)
            {
                foreach (var p in unique) yield return p;
            }
            yield break;
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

        private IEnumerable<Point> GetEdges(WorldTile parent)
        {
            var p = parent.WorldPosition;
            var edges = parent.ParentWorld.RegionsWithinRect(
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
            );
            return edges;
        }

        protected override void GenerateLocal(OpalLocalMap m, WorldTile parent)
        {
            var edges = GetEdges(parent).ToList();

            var tileref = TileSelector(parent.Biome);
            Point center = new Point(
                m.Width / 2,
                m.Height / 2
            );

            if (edges.Count == 2)
            {
                Point p1 = NormalizeEdge(edges[0], m);
                Point p2 = NormalizeEdge(edges[1], m);
                m.DrawCurve(p1, center, center, p2, tileref, Thickness, 100, true);
            }
            else if (edges.Count > 0)
            {
                foreach (var edge in edges)
                {
                    Point norm = NormalizeEdge(edge, m);
                    m.DrawLine(norm, center - new Point(edge.X >= 0 ? Thickness / 2 : 0, edge.Y >= 0 ? Thickness / 2 : 0), tileref, Thickness, true);
                }
            }
        }
    }
}

