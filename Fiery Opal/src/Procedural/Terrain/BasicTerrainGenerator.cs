﻿using Microsoft.Xna.Framework;
using System.Linq;

namespace FieryOpal.Src.Procedural.Terrain
{
    public abstract class TerrainGeneratorBase : IOpalFeatureGenerator
    {
        protected OpalLocalMap Tiles; 
        protected Point WorldPosition;
        
        public TerrainGeneratorBase(Point worldPosition)
        {
            WorldPosition = worldPosition;
        }

        public void Dispose()
        {
            Tiles.Iter((self, x, y, T) =>
            {
                T?.Dispose();
                return false;
            });
        }

        public virtual void Generate(OpalLocalMap m)
        {
            Tiles = new OpalLocalMap(m.Width, m.Height);
        }

        public OpalTile Get(int x, int y)
        {
            return (OpalTile)(Tiles.TileAt(x, y)?.Clone() ?? OpalTile.GetRefTile<DirtSkeleton>().Clone()); // Since we don't alter individual tiles, we only need to clone them when requested.
        }

        public IDecoration GetDecoration(int x, int y)
        {
            return Tiles.ActorsAt(x, y).Where(a => a is IDecoration).FirstOrDefault() as IDecoration;
        }
    }
}