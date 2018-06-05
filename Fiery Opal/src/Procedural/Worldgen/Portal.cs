using Microsoft.Xna.Framework;

namespace FieryOpal.Src.Procedural.Worldgen
{
    public struct Portal
    {
        public DungeonInstance FromInstance;
        public Point FromPos;
        public DungeonInstance ToInstance;
        public Point ToPos;

        public Portal Invert()
        {
            return new Portal()
            {
                FromInstance = ToInstance,
                FromPos = ToPos,
                ToInstance = FromInstance,
                ToPos = FromPos
            };
        }
    }


}
