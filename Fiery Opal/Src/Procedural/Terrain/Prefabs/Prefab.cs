using FieryOpal.Src.Procedural.Terrain.Tiles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src.Procedural.Terrain.Prefabs
{
    public abstract class Prefab
    {
        public int Width { get; }
        public int Height { get; }

        public Point Location { get; }

        public Dictionary<string, TileSkeleton> Materials { get; }
        protected OpalLocalMap Workspace;

        public Prefab(Point p, int w = 0, int h = 0)
        {
            Width = w == 0 ? Util.Rng.Next(10, 20) : w;
            Height = h == 0 ? Util.Rng.Next(10, 20) : h;
            Materials = new Dictionary<string, TileSkeleton>();
            Location = p;
        }

        public virtual void Generate()
        {
            Workspace = new OpalLocalMap(Width, Height, null, "Prefab");
        }

        public void Place(OpalLocalMap m, PrefabDecorator decorator)
        {
            Generate();
            decorator.Decorate(Workspace);

            m.ActorsWithin(new Rectangle(Location, new Point(Width, Height))).Where(a => a is IDecoration).ForEach(a => m.Despawn(a));
            Workspace.Iter((w, x, y, t) =>
            {
                m.SetTile(x + Location.X, y + Location.Y, t);
                Workspace.ActorsAt(x, y).ForEach(a => a.ChangeLocalMap(m, new Point(x, y) + Location, false));
                return false;
            });
        }
    }

    public abstract class PrefabDecorator
    {
        public virtual void Decorate(OpalLocalMap pfWorkspace)
        {

        }
    }
}
