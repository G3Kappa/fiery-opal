using System.Linq;

namespace FieryOpal.Src.Procedural
{
    public abstract class BuildingGeneratorBase : ILocalFeatureGenerator
    {
        protected OpalLocalMap Workspace;

        public void Dispose()
        {
            Workspace.Iter((self, x, y, T) =>
            {
                T?.Dispose();
                return false;
            });
        }

        public virtual void Generate(OpalLocalMap m)
        {
            Workspace = new OpalLocalMap(m.Width, m.Height, null, "BuildingGeneratorBase Workspace");
        }

        public OpalTile Get(int x, int y)
        {
            return Workspace.TileAt(x, y); // Either previous references or already cloned by the building designers
        }

        public IDecoration GetDecoration(int x, int y)
        {
            return Workspace.ActorsAt(x, y).FirstOrDefault(t => t is IDecoration) as IDecoration;
        }
    }

}
