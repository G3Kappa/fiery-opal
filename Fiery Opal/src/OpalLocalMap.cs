using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.src
{
    public class OpalLocalMap
    {
        protected int[,] TerrainGrid { get; private set; }
        public List<IOpalGameActor> Actors { get; private set; }

        public int Width { get; }
        public int Height { get; }

        public Color SkyColor { get; set; }

        public OpalLocalMap(int width, int height)
        {
            TerrainGrid = new int[width, height];
            Actors = new List<IOpalGameActor>();
            Width = width;
            Height = height;
            SkyColor = Color.MidnightBlue;
        }

        public virtual void Generate(params IOpalFeatureGenerator[] generators)
        {
            foreach (var gen in generators)
            {
                gen.Generate(this);
                for (int x = 0; x < Width; ++x)
                {
                    for (int y = 0; y < Height; ++y)
                    {
                        OpalTile output = gen.Get(x, y);
                        TerrainGrid[x, y] = output.Id;
                    }
                }
            }
        }

        public void Update(TimeSpan delta)
        {
            foreach (var actor in Actors)
            {
                actor.Update(delta);
            }
        }

        public OpalTile TileAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return null;
            }
            return OpalTile.FromId(TerrainGrid[x, y]);
        }

        public IEnumerable<IOpalGameActor> ActorsAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return new List<IOpalGameActor>();
            }
            return Actors.Where(act => act.LocalPosition == new Point(x, y));
        }

        public IEnumerable<Tuple<OpalTile, Point>> TilesWithin(Rectangle r)
        {
            for (int x = r.X; x < r.Width + r.X; ++x)
            {
                for (int y = r.Y; y < r.Height + r.Y; ++y)
                {
                    OpalTile t = TileAt(x, y);
                    if (t != null)
                    {
                        yield return new Tuple<OpalTile, Point>(t, new Point(x, y));
                    }
                }
            }
        }

        public IEnumerable<IOpalGameActor> ActorsWithin(Rectangle r)
        {
            for (int x = r.X; x < r.Width + r.X; ++x)
            {
                for (int y = r.Y; y < r.Height + r.Y; ++y)
                {
                    IEnumerable<IOpalGameActor> actors = ActorsAt(x, y);
                    foreach (var act in actors)
                    {
                        yield return act;
                    }
                }
            }
        }
    }

}
