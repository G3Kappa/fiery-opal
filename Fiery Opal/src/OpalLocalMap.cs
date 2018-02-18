using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.src
{
    public delegate bool OpalLocalMapIterator(OpalLocalMap self, int x, int y, OpalTile t);

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
            SkyColor = Color.DeepSkyBlue;
        }

        public virtual void Generate(params IOpalFeatureGenerator[] generators)
        {
            actorsAtHashmap = new Dictionary<Point, List<IOpalGameActor>>();
            foreach (var gen in generators)
            {
                gen.Generate(this);
                Iter((self, x, y, t) =>
                {
                    OpalTile output = gen.Get(x, y);
                    if (output != null)
                    {
                        self.TerrainGrid[x, y] = output.Id;
                    }
                    IDecoration decor = gen.GetDecoration(x, y);
                    if(decor != null)
                    {
                        if(!decor.ChangeLocalMap(self, new Point(x, y)))
                        {
                            Util.Log(String.Format("Decoration spawned at invalid location! ({0}, {1})", x, y), true, Color.Red, Color.Black);
                        }
                    }

                    return false;
                });
            }
        }

        public virtual void Iter(OpalLocalMapIterator iter)
        {
            for (int x = 0; x < Width; ++x)
            {
                for (int y = 0; y < Height; ++y)
                {
                    OpalTile t = TileAt(x, y);
                    if (iter(this, x, y, t)) break;
                }
            }
        }

        public IEnumerable<Point> FloodFill(int x, int y, OpalTile newTile)
        {
            Stack<Point> neighbours = new Stack<Point>();
            List<Point> processed = new List<Point>();

            OpalTile replace_me = TileAt(x, y);
            if (replace_me == null) yield break;

            do
            {
                foreach (var n in Neighbours(x, y).Where(t => t.Item1 == replace_me && !processed.Contains(t.Item2)))
                {
                    neighbours.Push(n.Item2);
                }

                SetTile(x, y, newTile);
                processed.Add(new Point(x, y));

                yield return processed.Last();
                if (neighbours.Count == 0) break;

                Point newXY = neighbours.Pop();
                x = newXY.X;
                y = newXY.Y;
            }
            while (true);
        }

        public IEnumerable<Point> DrawLine(Point start, Point end, OpalTile newTile, int thickness = 1)
        {
            var original_start = new Point(start.X, start.Y);

            int dx = Math.Abs(end.X - start.X), sx = start.X < end.X ? 1 : -1;
            int dy = Math.Abs(end.Y - start.Y), sy = start.Y < end.Y ? 1 : -1;
            int err = (dx > dy ? dx : -dy) / 2, e2;
            while(true)
            {
                SetTile(start.X, start.Y, newTile);
                yield return start;
                if (start.X == end.X && start.Y == end.Y) break;
                e2 = err;
                if (e2 > -dx) { err -= dy; start.X += sx; }
                if (e2 < dy) { err += dx; start.Y += sy; }
            }
            if(thickness > 1)
            {
                if (dx > dy)
                {
                    foreach (var p in DrawLine(original_start + new Point(0, 1), end + new Point(0, 1), newTile, thickness - 1))
                        yield return p;
                }
                else
                {
                    foreach (var p in DrawLine(original_start + new Point(1, 0), end + new Point(1, 0), newTile, thickness - 1))
                        yield return p;
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

        public bool SetTile(int x, int y, OpalTile t)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return false;
            }
            TerrainGrid[x, y] = t.Id;
            return true;
        }

        public IEnumerable<Tuple<OpalTile, Point>> Neighbours(int x, int y)
        {
            return TilesWithin(new Rectangle(x - 1, y - 1, 3, 3)).Where(t => t.Item2 != new Point(x, y));
        }

        protected Dictionary<Point, List<IOpalGameActor>> actorsAtHashmap = new Dictionary<Point, List<IOpalGameActor>>();

        public void NotifyActorMoved(IOpalGameActor actor, Point oldPos)
        {
            if(oldPos != new Point(-1, -1) /* Actor is from another map */ && actorsAtHashmap.ContainsKey(oldPos))
            {
                actorsAtHashmap[oldPos].Remove(actor);
                if(actorsAtHashmap[oldPos].Count == 0)
                {
                    actorsAtHashmap.Remove(oldPos);
                }
            }
            if (!actorsAtHashmap.ContainsKey(actor.LocalPosition))
            {
                actorsAtHashmap[actor.LocalPosition] = new List<IOpalGameActor>();
            }
            actorsAtHashmap[actor.LocalPosition].Add(actor);
        }

        public IEnumerable<IOpalGameActor> ActorsAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return new List<IOpalGameActor>();
            }
            var p = new Point(x, y);
            if (actorsAtHashmap.ContainsKey(p))
            {
                return actorsAtHashmap[p];
            }
            return new List<IOpalGameActor>();
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

        public IEnumerable<Tuple<OpalTile, Point>> TilesWithinRing(int x, int y, int r1, int r2)
        {
            Point center = new Point(x, y);
            foreach(var t in TilesWithin(new Rectangle(x - r1, y - r1, r1 * 2, r1 * 2)))
            {
                double dist = Math.Sqrt(Math.Pow(t.Item2.X - x, 2) + Math.Pow(t.Item2.Y - y, 2));
                if(dist <= r1 && dist > r2) yield return t;
            }
        }

        public IEnumerable<IOpalGameActor> ActorsWithinRing(int x, int y, int r1, int r2, bool include_all_decorations = false)
        {
            Point center = new Point(x, y);
            foreach (var t in TilesWithin(new Rectangle(x - r1, y - r1, r1 * 2, r1 * 2)))
            {
                double dist = Math.Sqrt(Math.Pow(t.Item2.X - x, 2) + Math.Pow(t.Item2.Y - y, 2));
                var actors = ActorsAt(t.Item2.X, t.Item2.Y);
                if (dist <= r1 && dist > r2 && actors.Count() > 0)
                {
                    foreach (var act in actors)
                    {
                        // No need to display hundreds of decorations, only the closest ones will suffice.
                        // This is an optimization for the RaycastViewport.
                        if (!include_all_decorations && act is IDecoration && dist > r1 / 3.0f) continue;

                        yield return act;
                    }
                }
            }
        }

        public Point FirstAccessibleTileAround(Point xy)
        {
            int r = 0;
            IEnumerable<Tuple<OpalTile, Point>> tiles_in_ring;

            do
            {
                tiles_in_ring = TilesWithinRing(xy.X, xy.Y, ++r, r - 1)
                    .Where(
                    t => !t.Item1.Properties.BlocksMovement
                         && !ActorsAt(t.Item2.X, t.Item2.Y)
                         .Any(
                         a => !(a is IDecoration) || (a as IDecoration).BlocksMovement
                         )
                    );
            }

            while (tiles_in_ring.Count() == 0);

            return tiles_in_ring.First().Item2;
        }
    }

}
