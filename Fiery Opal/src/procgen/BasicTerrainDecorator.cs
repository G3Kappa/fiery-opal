using FieryOpal.src.actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.procgen
{
    public class BasicTerrainDecorator : IOpalFeatureGenerator
    {
        private OpalLocalMap CurrentMap;

        public void Dispose()
        {
            // Do nothing since we're not copying CurrentMap
        }

        public void Generate(OpalLocalMap m)
        {
            CurrentMap = m;
        }

        public OpalTile Get(int x, int y)
        {
            return null;
        }

        public IDecoration GetDecoration(int x, int y)
        {
            return null;
        }
    }
}
