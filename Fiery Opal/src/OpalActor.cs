﻿using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Linq;
using System.Collections.Generic;
using FieryOpal.src.ui;
using FieryOpal.src.actors;

namespace FieryOpal.src
{
    public struct ActorIdentity
    {
        public string Name;

        public ActorIdentity(string name = "Nameless Actor")
        {
            Name = name;
        }
    }

    public interface IOpalGameActor
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

        /// <summary>
        /// Removes the actor from the current map and spawns it at the given coordinates on another map.
        /// </summary>
        /// <param name="new_map">The new OpalLocalMap.</param>
        /// <param name="new_spawn">The spawn coordinates.</param>
        /// <returns></returns>
        bool ChangeLocalMap(OpalLocalMap new_map, Point new_spawn);

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

    public interface ILightSource : IOpalGameActor
    {
        Color LightColor { get; }
        float LightIntensity { get; }
        float LightRadius { get; }
    }

    public interface IInteractive : IOpalGameActor
    {
        bool InteractWith(OpalActorBase actor);
    }

    public interface IInventoryHolder : IOpalGameActor
    {
        PersonalInventory Inventory { get; }
    }

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
        private IOpalGameActor held_by = null;
        public bool CanMove => can_move && held_by == null;

        public Guid Handle { get; }
        

        public OpalActorBase()
        {
            Handle = Guid.NewGuid();
            Graphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, '@'));
            FirstPersonGraphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, '@'));
        }

        public virtual void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalActorBase, string> msg, bool is_broadcast)
        {

        }

        public void Update(TimeSpan delta)
        {
            if (Map == null) return;
        }

        public bool BlockMovement(IOpalGameActor holder)
        {
            if (holder == null) return false;
            held_by = holder;
            return true;
        }

        public bool ReleaseMovement(IOpalGameActor holder)
        {
            if (held_by != holder) return false;
            held_by = null;
            return true;
        }

        public bool MoveTo(Point p, bool absolute = false)
        {
            var newPos = new Point();
            var ret = CanMoveTo(p, ref newPos, absolute);
            if(ret)
            {
                var oldPos = localPosition;
                localPosition = newPos;
                map.NotifyActorMoved(this, oldPos);
            }
            else if(!absolute)
            {
                if (!Util.OOB(newPos.X, newPos.Y, map.Width, map.Height)) return ret;

                var curRegion = map.ParentRegion;
                var world = curRegion.ParentWorld;
                Point new_region_pos = curRegion.WorldPosition + p;
                var new_region = world.RegionAt(new_region_pos.X, new_region_pos.Y);

                if (new_region != null)
                {
                    Point new_spawn = new Point(LocalPosition.X, LocalPosition.Y);
                    if (p.X < 0)
                    {
                        new_spawn.X = map.Width - 1;
                    }
                    else
                    if (p.X > 0)
                    {
                        new_spawn.X = 0;
                    }
                    if (p.Y < 0)
                    {
                        new_spawn.Y = map.Height - 1;
                    }
                    else if (p.Y > 0)
                    {
                        new_spawn.Y = 0;
                    }

                    ChangeLocalMap(new_region.LocalMap, new_spawn);
                }
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
                localPosition = new_p;
                return true;
            }

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
            if (tile == null) return false; //TODO: ChangeLocalMap?
            bool ret = !tile.Properties.BlocksMovement;
            if (ret)
            {
                // If moving diagonally by only one square
                if (Math.Abs(p.X) + Math.Abs(p.Y) == 2)
                {
                    // Check that you're not trying to squeeze through two walls
                    if ((Map.TileAt(new_p.X - p.X, new_p.Y)?.Properties.BlocksMovement ?? true)
                        && (Map.TileAt(new_p.X, new_p.Y - p.Y)?.Properties.BlocksMovement ?? true))
                    {
                        return false;
                    }
                }
            }
            return ret;
        }

        public bool ChangeLocalMap(OpalLocalMap new_map, Point new_spawn)
        {
            var old_map = map;
            if (new_map == null)
            {
                if (map != null) map.RemoveActor(this);
                map = null;
                MapChanged?.Invoke(this, map);
                return true;
            }

            var tile = new_map.TileAt(new_spawn.X, new_spawn.Y);
            bool ret = tile == null || !tile.Properties.BlocksMovement;

            if(!ret) new_spawn = new_map.FirstAccessibleTileAround(new_spawn);

            map = new_map;
            localPosition = new_spawn;
            map.AddActor(this);
            MapChanged?.Invoke(this, map);
            return true;
        }

        public event Action<OpalActorBase, OpalLocalMap> MapChanged;

        public virtual bool OnBump(IOpalGameActor other) { return false; }

        public override bool Equals(object obj)
        {
            if (!(obj is OpalActorBase)) return false;
            return (obj as OpalActorBase).Handle == Handle;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void Kill()
        {
            if (Map == null) return;
            is_dead = true;
            Map.RemoveActor(this);
        }

        public string GetInspectDescription(IOpalGameActor observer)
        {
            return "TODO: CHANGE ME";
        }
    }
    public abstract class LightSourceBase : OpalActorBase, ILightSource
    {
        protected Color lightColor = Color.Gold;
        public Color LightColor => lightColor;
        public float LightIntensity => 1f;
        public float LightRadius => 3f;

        public LightSourceBase()
        {
            FirstPersonGraphics = Graphics = new ColoredGlyph(new Cell(Color.Gold, Color.Transparent, 'L'));
        }

        public void SetColor(Color c)
        {
            lightColor = c;
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
