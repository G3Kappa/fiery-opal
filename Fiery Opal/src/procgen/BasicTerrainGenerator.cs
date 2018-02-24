using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FieryOpal.src.procgen.GenUtil;

namespace FieryOpal.src.procgen
{
    public class BasicTerrainGenerator : IOpalFeatureGenerator
    {
        protected OpalLocalMap Tiles; // Used as wrapper for the extension methods

        public BasicTerrainGenerator()
        {
        }

        public void Generate(OpalLocalMap m)
        {
            Tiles = new OpalLocalMap(m.Width, m.Height);

            // Starting point: some random rectangles.
            var partitions = GenUtil.Partition(new Rectangle(0, 0, m.Width, m.Height), .85f, 1 / 10f, .33f);
            Tiles.Iter((s, x, y, t) =>
            {
                if (!partitions.Any(p => p.Contains(x, y)))
                {
                    s.SetTile(x, y, OpalTile.RockWall);
                    return false;
                }
                s.SetTile(x, y, OpalTile.RockFloor);
                return false;
            });
            // First step: Connect them with tunnels
            var areasAndCentroids = GenUtil.GetEnclosedAreasAndCentroids(Tiles, t => !t.Properties.BlocksMovement);
            GenUtil.ConnectEnclosedAreas(Tiles, areasAndCentroids, OpalTile.RockFloor, 3, 5, 32);
            // Second step: Let time do its thing
            MatrixReplacement.CaveSystemRules.SlideAcross(Tiles, new Point(1),
                new MRRule(u => u != null && !u.Properties.BlocksMovement, OpalTile.RockFloor),
                new MRRule(u => u != null && u.Properties.BlocksMovement, OpalTile.RockWall),
                epochs: 5,
                break_early: false
            );
            areasAndCentroids = GenUtil.GetEnclosedAreasAndCentroids(Tiles, t => !t.Properties.BlocksMovement);
            GenUtil.ConnectEnclosedAreas(Tiles, areasAndCentroids, OpalTile.RockFloor, 2, 4, 16);

            float[,] fertilityNoise = Simplex.Noise.Calc2D(Util.GlobalRng.Next(0, 1000), Util.GlobalRng.Next(0, 1000), m.Width, m.Height, .025f);
            Tiles.Iter((s, x, y, t) =>
            {
                fertilityNoise[x, y] /= 255f;
                if (t != OpalTile.RockFloor || fertilityNoise[x, y] <= .45) return false;
                if (fertilityNoise[x, y] >= .75) s.SetTile(x, y, OpalTile.FertileSoil);
                else s.SetTile(x, y, OpalTile.Dirt);
                return false;
            });
        }

        public OpalTile Get(int x, int y)
        {
            return (OpalTile)Tiles.TileAt(x, y).Clone(); // Since we don't alter individual tiles, we only need to clone them when requested.
        }

        public IDecoration GetDecoration(int x, int y)
        {
            return null;
        }
    }
}
