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

        public void Dispose()
        {
            Tiles.Iter((self, x, y, T) =>
            {
                T?.Dispose();
                return false;
            });
        }

        public void Generate(OpalLocalMap m)
        {
            Tiles = new OpalLocalMap(m.Width, m.Height);
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
