using System.Linq;

namespace FieryOpal.Src.Procedural
{
    public class BasicBuildingGenerator : ILocalFeatureGenerator
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
            Tiles = new OpalLocalMap(m.Width, m.Height, null);
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
