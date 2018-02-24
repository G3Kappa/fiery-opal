using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.procgen
{
    public class BasicBuildingGenerator : IOpalFeatureGenerator
    {
        protected OpalLocalMap Tiles;

        public void Generate(OpalLocalMap m)
        {
            Tiles = new OpalLocalMap(m.Width, m.Height);
            var map_rect = new Rectangle(0, 0, m.Width, m.Height);
            var building_rects = GenUtil.Partition(map_rect, .8f, .25f, 0f);

            Tiles.Iter((self, x, y, T) =>
            {
                self.SetTile(x, y, m.TileAt(x, y));
                return false;
            });

            map_rect = new Rectangle(map_rect.X + 1, map_rect.Y + 1, map_rect.Width - 1, map_rect.Height - 1);
            foreach (Rectangle R in building_rects)
            {
                var r = new Rectangle(R.X, R.Y, R.Width - (1 - R.Width % 2), R.Height - (1 - R.Height % 2));
                if (!map_rect.Contains(r)) continue;
                if (m.TilesWithin(r).Where(t => !t.Item1.Properties.BlocksMovement).Count() < r.Width * r.Height / 2) continue;

                Util.Log("Building designed!", true);
                var building = new BasicBuildingDesigner(r).Generate();
                foreach (var tuple in building)
                {
                    Tiles.SetTile(tuple.Item3.X, tuple.Item3.Y, tuple.Item1);
                    foreach (var act in tuple.Item2)
                    {
                        act.ChangeLocalMap(Tiles, tuple.Item3);
                    }
                }
            };
        }

        public OpalTile Get(int x, int y)
        {
            return (OpalTile)Tiles.TileAt(x, y); // Either previous references or already cloned by the building designers
        }

        public IDecoration GetDecoration(int x, int y)
        {
            return Tiles.ActorsAt(x, y).FirstOrDefault(t => t is IDecoration) as IDecoration;
        }
    }

}
