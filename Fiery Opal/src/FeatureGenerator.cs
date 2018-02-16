using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src
{
    public interface IOpalFeatureGenerator
    {
        OpalTile Get(int x, int y);
        void Generate(OpalLocalMap m);
    }

    public class BasicTerrainGenerator : IOpalFeatureGenerator
    {
        public void Generate(OpalLocalMap m)
        {
        }

        public OpalTile Get(int x, int y)
        {
            return OpalTile.DungeonWall;
        }
    }
}
