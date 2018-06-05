namespace FieryOpal.Src.Procedural
{
    public class TerrainDecoratorBase : ILocalFeatureGenerator
    {
        protected OpalLocalMap CurrentMap;

        public virtual void Dispose()
        {
            // Do nothing since we're not copying CurrentMap
        }

        public virtual void Generate(OpalLocalMap m)
        {
            CurrentMap = m;
        }

        public virtual OpalTile Get(int x, int y)
        {
            return null;
        }

        public virtual IDecoration GetDecoration(int x, int y)
        {
            return null;
        }
    }
}
