using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural
{
    public abstract class BuildingDesigner
    {
        protected OpalLocalMap Workspace;
        protected Point MapOffset;


        public BuildingDesigner(Rectangle area)
        {
            Workspace = new OpalLocalMap(area.Width, area.Height);
            MapOffset = area.Location;
        }

        protected abstract void GenerateOntoWorkspace();

        public IEnumerable<Tuple<OpalTile, IEnumerable<IOpalGameActor>, Point>> Generate()
        {
            GenerateOntoWorkspace();
            var ret = Workspace.TilesWithin(null, yield_null: true)
                .Select(t => new Tuple<OpalTile, IEnumerable<IOpalGameActor>, Point>(
                    (OpalTile)t.Item1?.Clone() ?? null,
                    Workspace.ActorsAt(t.Item2.X, t.Item2.Y).ToList(),
                    t.Item2 + MapOffset)
                 ).ToList();
            // Free resources
            Workspace.RemoveAllActors();
            Workspace = new OpalLocalMap(Workspace.Width, Workspace.Height);

            return ret;
        }
    }
}
