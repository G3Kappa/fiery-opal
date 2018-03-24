using Microsoft.Xna.Framework;
using System.Linq;
using System.Collections.Generic;
using SadConsole;

namespace FieryOpal.Src.Procedural.Worldgen
{

    // WFGs must be called at runtime, when the actual map(s) is generated.
    // Therefore, they must be capable of storing a stateful representation
    // of the WorldTiles they intend to change, and how they intend to change
    // them, and this state will be used when each localmap is first generated.

    /*
        So we want a mapping of the form
        world_tile => f(local_map)
        Where f can modify the state of the local map.

        When a map is generated, it will look for its parent WorldTile
        and fetch any IWorldFeatureGenerator references it owns. Then, for
        each of those references, it will call ref.GenLocal(this, parent).

        At this point, the generator will check if it needs to
        modify the state of the parent WorldTile. If it doesn't, it will return
        an empty function, otherwise it will refer to the previously mentioned
        mapping and return the appropriate function which will then be called
        by the requesting LocalMap with itself as parameter.
    */

    public abstract class WorldFeatureGenerator
    {
        protected HashSet<Point> MarkedRegions { get; private set; } = new HashSet<Point>();
        public virtual Cell OverrideGraphics(WorldTile region)
        {
            return null;
        }

        protected abstract void GenerateLocal(OpalLocalMap m, WorldTile parent);

        public void GenerateLocal(OpalLocalMap m)
        {
            if ((m?.ParentRegion ?? null) == null) return;
            //if (!MarkedRegions.Contains(m.ParentRegion.WorldPosition)) return;
            GenerateLocal(m, m.ParentRegion);
        }

        protected abstract IEnumerable<Point> MarkRegions(World w);

        public IEnumerable<Point> GetMarkedRegions(World w)
        {
            return (MarkedRegions = new HashSet<Point>(MarkRegions(w)));
        }

        public virtual void Dispose()
        {
            MarkedRegions.Clear();
        }
    }
}

