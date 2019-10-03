using FieryOpal.Src.Actors;
using FieryOpal.Src.Actors.Environment;
using FieryOpal.Src.Audio;
using FieryOpal.Src.Procedural;
using FieryOpal.Src.Procedural.Terrain.Biomes;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace FieryOpal.Src
{
    public delegate bool OpalLocalMapIterator(OpalLocalMap self, int x, int y, OpalTile t);


    [Serializable]
    public class OpalLocalMap : IDisposable
    {
        protected OpalTile[,] TerrainGrid { get; private set; }
        public List<IOpalGameActor> Actors { get; private set; }

        public int Width { get; }
        public int Height { get; }
        public string Name { get; set; }
        public string DataFolder { get; set; }
        public bool Indoors { get; set; } = false;

        public Color FogColor { get; set; }
        public TileSkeleton CeilingTile { get; set; }

        public List<ILocalFeatureGenerator> FeatureGenerators { get; private set; } = new List<ILocalFeatureGenerator>();

        public SFXManager.SoundTrackType SoundTrack { get; set; } = SFXManager.SoundTrackType.None;
        public List<Tuple<SFXManager.SoundEffectType, float>> SoundEffects { get; private set; } = new List<Tuple<SFXManager.SoundEffectType, float>>();

        public WorldTile ParentRegion;
        public LightingManager Lighting { get; private set; }

        public delegate void ActorSpawnedEventHandler(OpalLocalMap sender, IOpalGameActor args);
        public event ActorSpawnedEventHandler ActorSpawned;
        public delegate void ActorDespawnedEventHandler(OpalLocalMap sender, IOpalGameActor args);
        public event ActorDespawnedEventHandler ActorDespawned;

        public float AmbientLightIntensity { get; set; }

        public OpalLocalMap(int width, int height, WorldTile parent, string name)
        {
            TerrainGrid = new OpalTile[width, height];
            Actors = new List<IOpalGameActor>();
            Width = width;
            Height = height;
            FogColor = Palette.Ui["UnknownTileBackground"];
            ParentRegion = parent;
            Name = name;
            CeilingTile = null;
            Lighting = new LightingManager(this);
            AmbientLightIntensity = 1f;
        }

        public float[,] DistanceTransform(Func<Tuple<OpalTile, Point>, bool> predicate)
        {
            bool[,] tiles = new bool[Width, Height];
            Iter((s, x, y, t) =>
            {
                tiles[x, y] = predicate(new Tuple<OpalTile, Point>(t, new Point(x, y)));
                return false;
            });
            return tiles.DistanceTransform().Normalize((float)Math.Sqrt(Width * Width + Height * Height));
        }

        private Object actorsLock = new Object();
        public IOpalGameActor FindActorByHandle(Guid handle)
        {
            lock (actorsLock)
            {
                foreach (var a in Actors)
                {
                    if (a.Handle == handle) return a;
                }
            }
            return null;
        }

        public bool Spawn(IOpalGameActor actor)
        {
            lock (actorsLock)
            {
                if (Actors.Contains(actor)) return false;
                Actors.Add(actor);
                NotifyActorMoved(actor, new Point(-1, -1));
                ActorSpawned?.Invoke(this, actor);
            }
            return true;
        }

        public void SpawnMany(IEnumerable<IOpalGameActor> actors)
        {
            lock (actorsLock)
            {
                foreach (var actor in actors)
                {
                    Spawn(actor);
                }
            }
        }

        public bool Despawn(IOpalGameActor actor, bool invokeChangeLocalMap=true)
        {
            lock (actorsLock)
            {
                if (!Actors.Contains(actor)) return false;
                if(invokeChangeLocalMap)
                {
                    actor.ChangeLocalMap(null, null);
                    return true;
                }
                NotifyActorMoved(actor, new Point(-2, -2));
                ActorDespawned?.Invoke(this, actor);
            }
            return true;
        }

        public void DespawnAll()
        {
            lock (actorsLock)
            {
                foreach (var actor in Actors.ToList())
                {
                    Despawn(actor);
                }
            }
        }

        public void DespawnMany(IEnumerable<IOpalGameActor> actors)
        {
            lock (actorsLock)
            {
                foreach (var actor in actors)
                {
                    Despawn(actor);
                }
            }
        }

        public void CallLocalGenerator(ILocalFeatureGenerator gen, bool remember = true)
        {
            if (remember) FeatureGenerators.Add(gen);
            gen.Generate(this);
            Iter((self, x, y, t) =>
            {
                OpalTile output = gen.Get(x, y);
                if (output != null)
                {
                    t = self.TerrainGrid[x, y] = output;
                    if (output.Properties.IsBlock || output.Properties.Fertility == 0)
                    {
                        foreach (var act in ActorsAt(x, y).Where(a => a is DecorationBase).ToList())
                        {
                            if (!(act is Plant) && output.Properties.Fertility == 0 && !output.Properties.IsBlock) continue;
                            (act as DecorationBase).Kill();
                        }
                    }
                }
                IDecoration decor = gen.GetDecoration(x, y);
                if (decor != null)
                {
                    if (t.Properties.IsBlock)
                    {
                        Util.LogText(String.Format("Decoration spawned at invalid location! ({0}, {1})", x, y), true);
                    }
                    else decor.ChangeLocalMap(self, new Point(x, y));
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

        public virtual void GenerateAnew(params ILocalFeatureGenerator[] generators)
        {
            var actors = actorsAtHashmap.SelectMany(a => a.Value).Where(a => a is TurnTakingActor).ToList();
            FeatureGenerators.Clear();
            Iter((self, x, y, t) =>
            {
                self.TileAt(x, y)?.Dispose();
                return false;
            });

            Actors = new List<IOpalGameActor>();
            Lighting = new LightingManager(this);
            actorsAtHashmap = new Dictionary<Point, List<IOpalGameActor>>();
            foreach (var gen in generators)
            {
                CallLocalGenerator(gen);
            }
            CallLocalGenerator(new BiomeTransitioner(ParentRegion));

            foreach (var a in actors)
            {
                a.ChangeLocalMap(this, a.LocalPosition, !(this is IDecoration));
            }

            Lighting.Update();
        }

        public void GenerateAnew()
        {
            GenerateAnew(FeatureGenerators.ToArray());
        }

        public void GenerateWorldFeatures()
        {
            if (ParentRegion == null) return;
            foreach (var gen in ParentRegion.FeatureGenerators)
            {
                gen.GenerateLocal(this);
            }
        }

        public virtual void Iter(OpalLocalMapIterator iter, Rectangle? area = null)
        {
            for (int x = 0; x < (area?.Width ?? Width); ++x)
            {
                for (int y = 0; y < (area?.Height ?? Height); ++y)
                {
                    int X = (area?.X ?? 0) + x;
                    int Y = (area?.Y ?? 0) + y;

                    OpalTile t = TileAt(X, Y);
                    if (iter(this, X, Y, t)) break;
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

                if (newTile != null) SetTile(x, y, newTile); // Flood fill is still useful even without actually applying it.
                processed.Add(new Point(x, y));

                yield return processed.Last();
                if (neighbours.Count == 0) break;

                Point newXY = neighbours.Pop();
                x = newXY.X;
                y = newXY.Y;
            }
            while (true);
        }

        public Rectangle? FindArea(Point size, Point stride, Predicate<Tuple<OpalTile, Point>> pred)
        {
            for(int x = 0; x < Width; x += stride.X)
            {
                for (int y = 0; y < Height; y += stride.Y)
                {
                    var r = new Rectangle(x, y, size.X, size.Y);
                    if (TilesWithin(r).All(t => pred(t)))
                    {
                        return r;
                    }
                }
            }
            return null;
        }

        public void DrawLine(Point start, Point end, OpalTile newTile, int thickness = 1, bool killDecorations = false)
        {
            DrawShape(Util.BresenhamLine(start, end, thickness), newTile, killDecorations);
        }

        public void DrawDisc(Point center, int radius, OpalTile newTile, bool killDecorations = false)
        {
            DrawShape(Util.Disc(center, radius), newTile, killDecorations);
        }

        public void DrawCurve(Point p1, Point p2, Point p3, Point p4, OpalTile newTile, int thickness = 1, int n = 5, bool killDecorations = false)
        {
            DrawShape(Util.CubicBezier(p1, p2, p3, p4, thickness: thickness, n: n), newTile, killDecorations);
        }

        public void DrawShape(IEnumerable<Point> points, OpalTile brush, bool killDecorations = false)
        {
            foreach (var p in points)
            {
                SetTile(p.X, p.Y, brush);
                if (killDecorations)
                {
                    ActorsAt(p.X, p.Y)
                        .Where(a => typeof(IDecoration).IsAssignableFrom(a.GetType()))
                        .ForEach(a => (a as OpalActorBase).Kill());
                }
            }
        }

        private double SFXCooldown = 0f;
        private DateTime SFXLastPlayedAt = DateTime.Now;
        public void Update(TimeSpan delta)
        {
            lock (actorsLock)
            {
                foreach (var actor in Actors)
                {
                    actor.Update(delta);
                }
            }

            if (SoundEffects.Count > 0)
            {
                if ((DateTime.Now - SFXLastPlayedAt).TotalMilliseconds >= SFXCooldown)
                {
                    SFXCooldown = 0f;

                    // Roll a random SFX and then roll against its probability value
                    var sfx = Util.Choose(SoundEffects);
                    if (Util.Rng.NextDouble() < sfx.Item2)
                    {
                        float volumeVariance = (float)Util.Rng.NextDouble() / 3f + .33f;
                        float pitchVariance = (float)Util.Rng.NextDouble() - .5f;
                        float panVariance = (float)Util.Rng.NextDouble() - .5f;

                        SFXManager.PlayFX(sfx.Item1, volumeVariance, pitchVariance, panVariance, false, false);

                        // To prevent spam and earrape, 5-10 seconds must pass before each ambient SFX can be played again.
                        SFXCooldown = Util.Rng.Next(5000, 10000);
                        SFXLastPlayedAt = DateTime.Now;

                        Util.LogText("OpalLocalMap.Update: Played SFX \"{0}\".".Fmt(Enum.GetName(typeof(SFXManager.SoundEffectType), sfx.Item1)), true);
                    }
                }
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

        public OpalTile TileAt(Point p)
        {
            return TileAt(p.X, p.Y);
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

        public IEnumerable<Tuple<OpalTile, Point>> Neighbours(int x, int y, bool cardinal = false, bool yield_null = false)
        {
            if (!cardinal) return TilesWithin(new Rectangle(x - 1, y - 1, 3, 3), yield_null).Where(t => t.Item2 != new Point(x, y));
            return TilesWithin(new Rectangle(x - 1, y - 1, 3, 3), yield_null).Where(t => Math.Abs(t.Item2.X - x) + Math.Abs(t.Item2.Y - y) == 1);
        }

        protected Dictionary<Point, List<IOpalGameActor>> actorsAtHashmap = new Dictionary<Point, List<IOpalGameActor>>();

        public void NotifyActorMoved(IOpalGameActor actor, Point oldPos)
        {
            if (oldPos == new Point(-2, -2)) /* Actor died */
            {
                actorsAtHashmap[actor.LocalPosition].Remove(actor);
                Actors.Remove(actor);
                return;
            }

            if (oldPos != new Point(-1, -1) && actorsAtHashmap.ContainsKey(oldPos) /* Actor is from this map */ )
            {
                actorsAtHashmap[oldPos].Remove(actor);
                if (actorsAtHashmap[oldPos].Count == 0)
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
            lock (actorsLock)
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
            }
            return new List<IOpalGameActor>();
        }

        public IEnumerable<IOpalGameActor> ActorsAt(Point p)
        {
            return ActorsAt(p.X, p.Y);
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
                lock (actorsLock)
                {
                    foreach (var actor in Actors) yield return actor;
                }
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
            foreach (var t in TilesWithin(new Rectangle(x - r1, y - r1, r1 * 2, r1 * 2)))
            {
                int dist = t.Item2.FastDist(center);
                if (dist <= r1 && dist > r2)
                {
                    yield return t;
                }
            }
        }

        public IEnumerable<IOpalGameActor> ActorsWithinRing(int x, int y, int r1, int r2, bool include_all_decorations = false)
        {
            Point center = new Point(x, y);
            foreach (var t in TilesWithin(new Rectangle(x - r1, y - r1, r1 * 2, r1 * 2)))
            {
                double dist = t.Item2.FastDist(new Point(x, y));
                if (dist <= r1 && dist > r2)
                {
                    var actors = ActorsAt(t.Item2.X, t.Item2.Y).ToList();
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

        public bool IsTileAcessible(Point p)
        {
            var t = TileAt(p);
            return (!t?.Properties.BlocksMovement ?? false)
                     && !ActorsAt(p.X, p.Y)
                     .Any(
                        a => (!(a as OpalActorBase)?.IgnoresCollision ?? false)
                     );
        }

        public Point FirstAccessibleTileInLine(Point lineStart, Point lineEnd, bool ignoreDecorations = true)
        {
            foreach(Point p in Util.BresenhamLine(lineStart, lineEnd, 1))
            {
                if (IsTileAcessible(p)) return p;
            }

            Util.LogText("No accessible tile between {0} and {1}!".Fmt(lineStart, lineEnd), true);
            return new Point(0, 0);
        }

        public Point FirstAccessibleTileAround(Point xy)
        {
            int r = 1;
            List<Tuple<OpalTile, Point>> tiles_in_ring;
            
            if (IsTileAcessible(xy)) return xy;

            do
            {
                tiles_in_ring = TilesWithinRing(xy.X, xy.Y, r, r - 1)
                    .Where(t => IsTileAcessible(t.Item2)).ToList();

                if (r >= Width / 2)
                {
                    Util.LogText("No accessible tile around {0}!".Fmt(xy), true);
                    return new Point(0, 0);
                }

                r++;
            }
            while (tiles_in_ring.Count == 0);

            return tiles_in_ring.First().Item2;
        }

        public void Dispose()
        {
            Iter((s, x, y, t) =>
            {
                t?.Dispose();
                return false;
            });
        }
    }

    public static class OpalTileExtensions
    {
        public static TileSkeleton[,] GetSkeletons(this OpalTile[,] grid)
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);
            TileSkeleton[,] ret = new TileSkeleton[w, h];
            for (int x = 0; x < w; ++x)
                for (int y = 0; y < h; ++y)
                    ret[x, y] = grid[x, y]?.Skeleton ?? null;
            return ret;
        }

        public static T[] Flatten<T>(this T[,] grid)
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);
            T[] ret = new T[w * h];
            for (int x = 0; x < w; ++x)
                for (int y = 0; y < h; ++y)
                    ret[y * w + x] = grid[x, y];

            return ret;
        }

        public static T[,] Unflatten<T>(this T[] arr, int w, int h)
        {
            if (w * h != arr.Length) throw new ArgumentException();

            T[,] ret = new T[w, h];
            for (int x = 0; x < w; ++x)
                for (int y = 0; y < h; ++y)
                    ret[x, y] = arr[y * w + x];

            return ret;
        }
    }

}
