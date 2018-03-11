﻿using FieryOpal.src.procgen;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.src
{
    public delegate bool OpalLocalMapIterator(OpalLocalMap self, int x, int y, OpalTile t);

    public class ViewportFog
    {
        protected HashSet<Point> Seen = new HashSet<Point>();
        protected HashSet<Point> Known = new HashSet<Point>();
        protected bool IsDisabled = false;

        public void See(Point p)
        {
            if (!Seen.Contains(p)) Seen.Add(p);
        }

        public void Learn(Point p)
        {
            if (!Known.Contains(p)) Known.Add(p);
        }

        public void Unsee(Point p)
        {
            if (Seen.Contains(p)) Seen.Remove(p);
        }

        public void Forget(Point p)
        {
            if (Known.Contains(p)) Known.Remove(p);
        }

        public void UnseeEverything()
        {
            Seen.Clear();
        }

        public void ForgetEverything()
        {
            Known.Clear();
        }

        public bool CanSee(Point p)
        {
            if (IsDisabled) return true;
            return Seen.Contains(p);
        }

        public bool KnowsOf(Point p)
        {
            if (IsDisabled) return true;
            return Known.Contains(p);
        }

        public void Disable()
        {
            IsDisabled = true;
        }

        public void Enable()
        {
            IsDisabled = false;
        }

        public void Toggle()
        {
            IsDisabled = !IsDisabled;
        }

        public bool IsEnabled => !IsDisabled;
    }

    public class OpalLocalMap
    {
        protected OpalTile[,] TerrainGrid { get; private set; }
        protected List<IOpalGameActor> Actors { get; private set; }

        public int Width { get; }
        public int Height { get; }

        public Color SkyColor { get; set; }
        public Color FogColor { get; set; }
        public ViewportFog Fog { get; set; }

        public WorldTile ParentRegion;

        public OpalLocalMap(int width, int height)
        {
            TerrainGrid = new OpalTile[width, height];
            Actors = new List<IOpalGameActor>();
            Width = width;
            Height = height;
            SkyColor = Color.DeepSkyBlue;
            FogColor = Color.DarkSlateGray;
            Fog = new ViewportFog();
        }

        public bool AddActor(IOpalGameActor actor)
        {
            if (Actors.Contains(actor)) return false;
            Actors.Add(actor);
            NotifyActorMoved(actor, new Point(-1, -1));
            return true;
        }

        public void AddActors(IEnumerable<IOpalGameActor> actors)
        {
            foreach(var actor in actors)
            {
                AddActor(actor);
            }
        }

        public bool RemoveActor(IOpalGameActor actor)
        {
            if (!Actors.Contains(actor)) return false;
            Actors.Remove(actor);
            NotifyActorMoved(actor, new Point(-2, -2));
            return true;
        }

        public void RemoveAllActors()
        {
            foreach(var actor in Actors.ToList())
            {
                RemoveActor(actor);
            }
        }

        public void RemoveActors(IEnumerable<IOpalGameActor> actors)
        {
            foreach (var actor in actors)
            {
                RemoveActor(actor);
            }
        }

        public virtual void Generate(params IOpalFeatureGenerator[] generators)
        {
            Iter((self, x, y, t) =>
            {
                self.TileAt(x, y)?.Dispose();
                return false;
            });

            actorsAtHashmap = new Dictionary<Point, List<IOpalGameActor>>();
            foreach (var gen in generators)
            {
                gen.Generate(this);
                Iter((self, x, y, t) =>
                {
                    OpalTile output = gen.Get(x, y);
                    if (output != null)
                    {
                        self.TerrainGrid[x, y] = output;
                    }
                    IDecoration decor = gen.GetDecoration(x, y);
                    if(decor != null)
                    {
                        if(!decor.ChangeLocalMap(self, new Point(x, y)))
                        {
                            Util.Log(String.Format("Decoration spawned at invalid location! ({0}, {1})", x, y), true);
                        }
                    }

                    return false;
                });
                // Generators often instantiate their own OpalLocalMaps,
                // and they yield tiles by copying them, so they need
                // a way to dispose of the allocated resources once the
                // current call to Generate is no longer relevant.
                // If they don't dispose of these resources, the function
                // getFirstFreeId() of OpalLocalTile will take longer
                // and longer as new maps are generated.
                gen.Dispose();
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
            HashSet<Point> processed = new HashSet<Point>();

            OpalTile replace_me = TileAt(x, y);
            if (replace_me == null) yield break;

            do
            {
                foreach (var n in Neighbours(x, y).Where(t => t.Item1 == replace_me && !processed.Contains(t.Item2)))
                {
                    neighbours.Push(n.Item2);
                }

                if(newTile != null) SetTile(x, y, newTile); // Flood fill is still useful even without actually applying it.
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
            return TerrainGrid[x, y];
        }

        public bool SetTile(int x, int y, OpalTile t)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return false;
            }
            TerrainGrid[x, y] = t;
            return true;
        }

        public IEnumerable<Tuple<OpalTile, Point>> Neighbours(int x, int y)
        {
            return TilesWithin(new Rectangle(x - 1, y - 1, 3, 3)).Where(t => t.Item2 != new Point(x, y));
        }

        protected Dictionary<Point, List<IOpalGameActor>> actorsAtHashmap = new Dictionary<Point, List<IOpalGameActor>>();

        public void NotifyActorMoved(IOpalGameActor actor, Point oldPos)
        {
            if(oldPos == new Point(-2, -2)) /* Actor died */
            {
                actorsAtHashmap[actor.LocalPosition].Remove(actor);
                return;
            }

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

        public IEnumerable<Tuple<OpalTile, Point>> TilesWithin(Rectangle? R, bool yield_null = false)
        {
            Rectangle r;
            if (!R.HasValue)
            {
                r = new Rectangle(0, 0, Width, Height);
            }
            else r = R.Value;

            for (int x = r.X; x < r.Width + r.X; ++x)
            {
                for (int y = r.Y; y < r.Height + r.Y; ++y)
                {
                    OpalTile t = TileAt(x, y);
                    if ((!yield_null && t != null) || yield_null)
                    {
                        yield return new Tuple<OpalTile, Point>(t, new Point(x, y));
                    }
                }
            }
        }

        public IEnumerable<IOpalGameActor> ActorsWithin(Rectangle? R)
        {
            Rectangle r;
            if (!R.HasValue)
            {
                foreach (var actor in Actors) yield return actor;
                yield break;
            }
            else r = R.Value;

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
                        if (!include_all_decorations && act is IDecoration && dist > r1 / 2.0f) continue;

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


        public float[,] CalcDistanceField(Func<Point, Point, float> D, Func<OpalTile, float> f)
        {
            // TODO: Use proper algorithm

            float[,] distfield = new float[Width, Height];
            var seeds = TilesWithin(new Rectangle(0, 0, Width, Height)).Where(t => f(t.Item1) == 0);
            float max_dist = 0;
            Iter((self, x, y, t) =>
            {
                float min_dist = float.MaxValue;
                float f_val = f(t);

                distfield[x, y] = 1f;
                return false; // TODO: REENABLE

                if (f_val > 0)
                {
                    foreach (var s in seeds)
                    {
                        float dist = D(s.Item2, new Point(x, y));
                        if (dist < min_dist)
                        {
                            min_dist = dist;
                        }
                        if(dist > max_dist)
                        {
                            max_dist = dist;
                        }
                    }
                    distfield[x, y] = (min_dist * f_val);
                }
                else distfield[x, y] = 0;
                return false;
            });
            Iter((self, x, y, t) =>
            {
                distfield[x, y] /= max_dist;
                return false;
            });

            return distfield;
        }
    }

}
