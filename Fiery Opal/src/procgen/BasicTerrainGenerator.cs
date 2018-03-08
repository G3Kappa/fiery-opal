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
            return (OpalTile)(Tiles.TileAt(x, y)?.Clone() ?? OpalTile.Dirt.Clone()); // Since we don't alter individual tiles, we only need to clone them when requested.
        }

        public IDecoration GetDecoration(int x, int y)
        {
            return null;
        }
    }
}
