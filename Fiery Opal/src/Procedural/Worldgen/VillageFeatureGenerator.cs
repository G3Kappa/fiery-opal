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
            int minSz = 10, maxSz = 15;

            var rooms = Lib.PoissonDiskSampler.SampleRectangle(new Vector2(maxSz, maxSz), new Vector2(m.Width - maxSz * 2, m.Height - maxSz * 2), maxSz + 3);
            HomePrefabDecorator roomDecorator = new HomePrefabDecorator();

            foreach(var r in rooms)
            {
                Point sz = new Point(Util.Rng.Next(minSz, maxSz), Util.Rng.Next(minSz, maxSz));
                Rectangle rect = new Rectangle(r.ToPoint(), sz);
                if (m.TilesWithin(rect).Count(t => t.Item1.Properties.IsBlock) < maxSz)
                {
                    new RoomPrefab(rect.Location, rect.Width, rect.Height, Util.RandomUnitPoint(false, true)).Place(m, roomDecorator);
                }
            }

            int population = Util.Rng.Next(5, 15);
            while(population --> 0)
            {
                var villager = new Humanoid();
                villager.ChangeLocalMap(m);
            }
        }
    }
}
