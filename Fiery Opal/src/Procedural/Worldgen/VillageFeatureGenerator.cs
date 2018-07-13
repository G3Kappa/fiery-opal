using FieryOpal.Src.Actors;
using FieryOpal.Src.Procedural.Terrain.Prefabs;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural.Worldgen
{
    public class VillageFeatureGenerator : WorldFeatureGenerator
    {
        protected Cell BaseGraphics = new Cell(Palette.Terrain["World_ColonyForeground"], Color.Transparent, 159);

        public VillageFeatureGenerator()
        {
        }

        public override Cell OverrideGraphics(WorldTile region)
        {
            return new Cell(BaseGraphics.Foreground, region.Graphics.Background, BaseGraphics.Glyph);
        }

        protected bool ValidRegion(WorldTile t)
        {
            return t != null
                && (t.Biome.AverageTemperature < BiomeHeatType.Hottest && t.Biome.AverageTemperature > BiomeHeatType.Coldest)
                && !new[] { BiomeType.Sea, BiomeType.Ocean }.Contains(t.Biome.Type)
                && !VillagesNearby(t);
        }

        private bool VillagesNearby(WorldTile t)
        {
            var neighbours = t.ParentWorld.RegionsWithinRect(
                new Rectangle(t.WorldPosition - new Point(2), new Point(5))
            );
            return neighbours.Any(n => n.FeatureGenerators.Any(x => x is VillageFeatureGenerator));
        }

        protected override IEnumerable<Point> MarkRegions(World w)
        {
            Point c;
            int start_tries = 100;
            do
            {
                c = new Point(Util.Rng.Next(0, w.Width), Util.Rng.Next(0, w.Height));
            }
            while (!ValidRegion(w.RegionAt(c.X, c.Y)) && --start_tries > 0);
            if (start_tries < 0)
            {
                Util.LogText("WorldFeatureGenerator: Could not place village.", true);
                yield break;
            }
            yield return c;
        }

        protected override void GenerateLocal(OpalLocalMap m, WorldTile parent)
        {
            Point cellSz = new Point(3);

            var lots = GenUtil.SplitRect(
                new Rectangle(m.Width / 8, m.Height / 8, m.Width / 2 + m.Width / 4, m.Height / 2 + m.Height / 4),
                () => new Vector2(1f / (1.5f + (float)Util.Rng.NextDouble().Quantize(8) * 1.5f)),
                (cellSz.X + 3) * 3,
                (cellSz.Y + 3) * 3
            );

            foreach(var r in lots)
            {
                r.Inflate(-cellSz.X, -cellSz.Y);

                //if (Util.Rng.NextDouble() < .3f) continue;

                var R = r;
                R.Location += new Point(Util.Rng.Next(-cellSz.X / 2, cellSz.X / 2), Util.Rng.Next(-cellSz.Y / 2, cellSz.Y / 2));

                new RoomComplexPrefab(R.Location, R.Width, R.Height, cellSz).Place(m, null);
            }

        }
    }
}
