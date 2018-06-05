using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Lib
{
    public class Voronoi
    {
        public static IEnumerable<IEnumerable<Point>> GetRegions(Rectangle r, IEnumerable<Point> seeds)
        {
            var _seeds = seeds.ToList();

            List<List<Point>> regions = new List<List<Point>>();
            for (int i = 0; i < _seeds.Count; ++i)
            {
                regions.Add(new List<Point>());
            }

            for (int x = 0; x < r.Width; ++x)
            {
                for (int y = 0; y < r.Height; ++y)
                {
                    var p = new Point(x, y);
                    int closest = 0;
                    float closest_dist = 1000000f;
                    for (int i = 0; i < _seeds.Count; ++i)
                    {
                        var dist = p.SquaredEuclidianDistance(_seeds[i]);
                        if (dist < closest_dist)
                        {
                            closest = i;
                            closest_dist = dist;
                        }
                    }
                    regions[closest].Add(p);
                }
            }

            return regions;
        }

    }
}
