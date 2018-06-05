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
            var neighbours = t.ParentWorld.RegionsWithin(
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
                Util.Log("WorldFeatureGenerator: Could not place village.", true);
                yield break;
            }
            yield return c;
        }

        protected override void GenerateLocal(OpalLocalMap m, WorldTile parent)
        {

        }
    }
}
