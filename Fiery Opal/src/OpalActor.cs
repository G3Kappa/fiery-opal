using FieryOpal.Src.Actors;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FieryOpal.Src
{
    public struct ActorIdentity
    {
        public string Name;

        public ActorIdentity(string name = "Nameless Actor")
        {
            Name = name;
        }
    }

    public interface ICustomSpritesheet
    {
        Font Spritesheet { get; }
    }

    public interface INamedObject
    {
        string Name { get; }
    }


    public delegate void ActorMovedEventHandler(IOpalGameActor a, Point oldPos, bool mapChanged=false);
    public delegate void MapChangedEventHandler(IOpalGameActor a, OpalLocalMap oldMap);
    public interface IOpalGameActor : ICustomSpritesheet, INamedObject
    {
        Guid Handle { get; }
        Point LocalPosition { get; }

        OpalLocalMap Map { get; }
        ColoredGlyph Graphics { get; set; }
        ColoredGlyph FirstPersonGraphics { get; set; }
        Vector2 FirstPersonScale { get; set; }
        float FirstPersonVerticalOffset { get; set; }
        bool Visible { get; set; }

        void Update(TimeSpan delta);

        bool CanMove { get; }
        bool MoveTo(Point rel, bool absolute);
        event ActorMovedEventHandler PositionChanged;
        event MapChangedEventHandler MapChanged;

        /// <summary>
        /// Removes the actor from the current map and spawns it at the given coordinates on another map.
        /// </summary>
        /// <param name="new_map">The new OpalLocalMap.</param>
        /// <param name="new_spawn">The spawn coordinates.</param>
        /// <returns></returns>
        bool ChangeLocalMap(OpalLocalMap new_map, Point new_spawn, bool check_tile = true);

        bool OnBump(IOpalGameActor other);
    }

    public interface IInspectable
    {
        string GetInspectDescription(IOpalGameActor observer);
    }

    public interface IDecoration : IOpalGameActor
    {
        bool BlocksMovement { get; }
    }

    public interface IInteractive : INamedObject
    {
        bool InteractWith(OpalActorBase actor);
    }

    public interface IInventoryHolder : IOpalGameActor
    {
        PersonalInventory Inventory { get; }
    }

    public interface IEquipmentUser : IOpalGameActor
    {
        PersonalEquipment Equipment { get; }
    }

    [Serializable]
    public class OpalActorBase : IPipelineSubscriber<OpalActorBase>, IOpalGameActor, IInspectable
    {

        private Point localPosition;
        public Point LocalPosition => localPosition;

        private OpalLocalMap map;
        public OpalLocalMap Map => map;

        private ColoredGlyph graphics;
        public ColoredGlyph Graphics { get => graphics; set => graphics = value; }
        private ColoredGlyph fp_graphics;
        public ColoredGlyph FirstPersonGraphics { get => fp_graphics; set => fp_graphics = value; }

        private Vector2 fp_scale = new Vector2(1f, 1f);
        public Vector2 FirstPersonScale { get => fp_scale; set => fp_scale = value; }
        private float fp_voff = 0;
        public float FirstPersonVerticalOffset { get => fp_voff; set => fp_voff = value; }

        private bool visible = true;
        public bool Visible { get => visible; set => visible = value; }

        private bool is_dead;
        public bool IsDead => is_dead;

        private bool can_move = true;
        public bool CanMove => can_move;

        private bool ignores_collision = false;
        public bool IgnoresCollision => ignores_collision;

        private bool is_flying = false;
        public bool IsFlying { get => is_flying; protected set => is_flying = value; }

        public Guid Handle { get; }
        public virtual Font Spritesheet => Nexus.Fonts.Spritesheets["Creatures"];

        public bool IsPlayer => (this as TurnTakingActor)?.Brain is PlayerControlledAI;

        public string Name { get; set; }

        public OpalActorBase()
        {
            Handle = Guid.NewGuid();
            Graphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, '@'));
            FirstPersonGraphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, '@'));
        }

        public virtual void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalActorBase, string> msg, bool is_broadcast)
        {

        }

        public virtual void Update(TimeSpan delta)
        {
            if (Map == null) return;
        }


        public bool MoveTo(Point p, bool absolute = false)
        {
            var newPos = new Point();
            var ret = CanMoveTo(p, ref newPos, absolute);
            var oldPos = localPosition;
            if (ret)
            {
                localPosition = newPos;
                map.NotifyActorMoved(this, oldPos);
                PositionChanged?.Invoke(this, oldPos);
            }
            else if (!absolute && Util.OOB(newPos.X, newPos.Y, map.Width, map.Height))
            {
                var curRegion = map.ParentRegion;
                var world = curRegion?.ParentWorld;
                if (world == null)
                {
                    if (IsPlayer)
                    {
                        Util.LogText("The void lies there.", false);
                    }
                    return false;
                }

                Point q = new Point();
                if (newPos.X < 0)
                {
                    q.X = -1;
                }
                if (newPos.Y < 0)
                {
                    q.Y = -1;
                }
                if (newPos.X >= map.Width)
                {
                    q.X = 1;
                }
                if (newPos.Y >= map.Height)
                {
                    q.Y = 1;
                }

                Point new_region_pos = curRegion.WorldPosition + q;
                var new_region = world.RegionAt(new_region_pos.X, new_region_pos.Y);

                if (new_region != null)
                {
                    Point new_spawn = new Point(LocalPosition.X, LocalPosition.Y);
                    if (q.X < 0)
                    {
                        new_spawn.X = map.Width - 1;
                    }
                    else
                    if (q.X > 0)
                    {
                        new_spawn.X = 0;
                    }
                    if (q.Y < 0)
                    {
                        new_spawn.Y = map.Height - 1;
                    }
                    else if (q.Y > 0)
                    {
                        new_spawn.Y = 0;
                    }

                    var t = new_region.LocalMap.TileAt(new_spawn);
                    if (!t.Properties.BlocksMovement || IgnoresCollision)
                    {
                        ChangeLocalMap(new_region.LocalMap, new_spawn);
                        PositionChanged?.Invoke(this, oldPos, true);
                    }
                    else if (IsPlayer)
                        Util.Log(Util.Str("Actor_CannotChangeRegion", t.Name).ToColoredString(Palette.Ui["BoringMessage"]), false);
                }
                else if (IsPlayer)
                    Util.Log(Util.Str("Actor_CannotChangeRegion", "the edge of the world").ToColoredString(Palette.Ui["BoringMessage"]), false);
            }

            if (!ret && IsPlayer)
            {
                var t = Map.TileAt(LocalPosition + p);
                if (t?.Properties.IsBlock ?? false)
                    Util.Log(Util.Str("Actor_BumpInto", t.Name).ToColoredString(Palette.Ui["BoringMessage"]), false);
            }

            return ret;
        }

        public bool CanMoveTo(Point p, ref Point newPos, bool absolute = false)
        {
            if (!CanMove) return false;
            Point new_p = new Point((absolute ? 0 : LocalPosition.X) + p.X, (absolute ? 0 : LocalPosition.Y) + p.Y);
            newPos = new_p;

            if (map == null)
            {
                return true;
            }

            if (IgnoresCollision && !Util.OOB(new_p.X, new_p.Y, Map.Width, Map.Height)) return true;

            var actors_there = Map.ActorsAt(new_p.X, new_p.Y);
            if (actors_there.Count() > 0)
            {
                bool can_pass_through = true;
                foreach (var actor in actors_there)
                {
                    if (!actor.OnBump(this)) can_pass_through = false;
                }
                if (!can_pass_through) return false;
            }

            var tile = Map.TileAt(new_p.X, new_p.Y);
            if (tile == null) return false;
            bool ret = !tile.Properties.BlocksMovement;
            if (ret)
            {
                // If moving diagonally by only one square
                if (Math.Abs(p.X) + Math.Abs(p.Y) == 2)
                {
                    // Check that you're not trying to squeeze through two walls
                    if ((Map.TileAt(new_p.X - p.X, new_p.Y)?.Properties.IsBlock ?? true)
                        && (Map.TileAt(new_p.X, new_p.Y - p.Y)?.Properties.IsBlock ?? true))
                    {
                        return false;
                    }
                }
            }
            return ret;
        }

        public bool ChangeLocalMap(OpalLocalMap new_map, Point new_spawn, bool check_tile = true)
        {
            var oldMap = map;
            oldMap?.Despawn(this);

            if (new_map == null)
            {
                MapChanged?.Invoke(this, oldMap);
                map = null;
                return true;
            }

            var tile = new_map.TileAt(new_spawn.X, new_spawn.Y);
            bool ret = tile != null && !tile.Properties.IsBlock
                && !new_map.ActorsAt(new_spawn.X, new_spawn.Y).Any(a =>
            {
                if (this is DecorationBase)
                {
                    return a is DecorationBase;
                }
                return a is OpalActorBase;
            });

            if (!ret && check_tile) new_spawn = new_map.FirstAccessibleTileAround(new_spawn, !(this is DecorationBase));

            map = new_map;
            localPosition = new_spawn;
            map.Spawn(this);
            MapChanged?.Invoke(this, oldMap);

            if (IsPlayer)
            {
                Soundtrack.Play(new_map.Soundtrack);
            }

            return true;
        }

        public event ActorMovedEventHandler PositionChanged;
        public event MapChangedEventHandler MapChanged;

        public virtual bool OnBump(IOpalGameActor other) { return IgnoresCollision; }

        public override bool Equals(object obj)
        {
            if (!typeof(OpalActorBase).IsAssignableFrom(obj.GetType())) return false;
            return (obj as OpalActorBase).Handle == Handle;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void Kill()
        {
            is_dead = true;
            Map?.Despawn(this);
        }

        public void SetCollision(bool c)
        {
            ignores_collision = !c;
        }

        public string GetInspectDescription(IOpalGameActor observer)
        {
            return "TODO: CHANGE ME";
        }

        private static Dictionary<string, Type> ActorClasses = new Dictionary<string, Type>();
        public static void PreloadActorClasses(string subNamespace)
        {

            Type[] typelist = Util.GetTypesInNamespace(Assembly.GetExecutingAssembly(), "FieryOpal.Src.Actors" + (subNamespace.Length == 0 ? "" : "." + subNamespace));
            foreach (Type t in typelist)
            {
                if (t.IsAbstract || !typeof(OpalActorBase).IsAssignableFrom(t)) continue;
                ActorClasses[t.Name.ToLower()] = t;
                Util.LogText("OpalActorBase.PreloadActorClasses: Preloaded {0}.".Fmt(t.Name), true, Palette.Ui["BoringMessage"]);
            }
        }

        public static OpalActorBase MakeFromClassName(string className)
        {
            if (ActorClasses.ContainsKey(className.ToLower()))
            {
                return Activator.CreateInstance(ActorClasses[className.ToLower()]) as OpalActorBase;
            }
            return null;
        }
    }
    public abstract class DecorationBase : OpalActorBase, IDecoration
    {
        public virtual bool BlocksMovement => false;
        public virtual bool DisplayAsBlock => false;

        public override bool OnBump(IOpalGameActor other)
        {
            return !BlocksMovement;
        }
    }
}
