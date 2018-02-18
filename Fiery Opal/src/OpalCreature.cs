using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FieryOpal.src
{
    public interface IOpalGameActor
    {
        Point LocalPosition { get; }

        OpalLocalMap Map { get; }
        ColoredGlyph Graphics { get; set; }
        ColoredGlyph FirstPersonGraphics { get; set; }
        Vector2 FirstPersonScale { get; set; }
        float FirstPersonVerticalOffset { get; set; }
        bool Visible { get; set; }

        void Update(TimeSpan delta);

        bool Move(Point rel, bool absolute);

        /// <summary>
        /// Removes the actor from the current map and spawns it at the given coordinates on another map.
        /// </summary>
        /// <param name="new_map">The new OpalLocalMap.</param>
        /// <param name="new_spawn">The spawn coordinates.</param>
        /// <returns></returns>
        bool ChangeLocalMap(OpalLocalMap new_map, Point new_spawn);

        bool OnBump(IOpalGameActor other);
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

    public class OpalActorBase : IPipelineSubscriber<OpalActorBase>, IOpalGameActor
    {
        public Guid Handle { get; }

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


        public OpalActorBase()
        {
            Handle = Guid.NewGuid();
            Graphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, '@'));
            FirstPersonGraphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, '@'));
        }

        public void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalActorBase, string> msg, bool is_broadcast)
        {

        }

        public void Update(TimeSpan delta)
        {
            if (Map == null) return;
        }

        public bool Move(Point p, bool absolute = false)
        {
            p = new Point((absolute ? 0 : LocalPosition.X) + p.X, (absolute ? 0 : LocalPosition.Y) + p.Y);

            if (map == null)
            {
                localPosition = p;
                return true;
            }

            var actors_there = Map.ActorsAt(p.X, p.Y);
            if (actors_there.Count() > 0)
            {
                bool can_pass_through = true;
                foreach (var actor in actors_there)
                {
                    if (!actor.OnBump(this)) can_pass_through = false;
                }
                if (!can_pass_through) return false;
            }

            var tile = Map.TileAt(p.X, p.Y);
            if (tile == null) return false; //TODO: ChangeLocalMap?
            bool ret = !tile.Properties.BlocksMovement;
            if (ret)
            {
                var oldPos = localPosition;
                localPosition = p;
                map.NotifyActorMoved(this, oldPos);
            }
            return ret;
        }

        public bool ChangeLocalMap(OpalLocalMap new_map, Point new_spawn)
        {
            var tile = new_map.TileAt(new_spawn.X, new_spawn.Y);
            if (tile == null) return false;
            bool ret = !tile.Properties.BlocksMovement;
            if (ret)
            {
                map = new_map;
                map.Actors.Add(this);
                localPosition = new_spawn;
                map.NotifyActorMoved(this, new Point(-1, -1));
            }
            return ret;
        }

        public virtual bool OnBump(IOpalGameActor other) { return false; }

        public void Kill()
        {
            if (Map == null) return;
            is_dead = true;
            Map.Actors.Remove(this);
            Map.NotifyActorMoved(this, new Point(-2, -2));
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

        public override bool OnBump(IOpalGameActor other)
        {
            return !BlocksMovement;
        }
    }

    public class Plant : DecorationBase
    {
        public override bool BlocksMovement => true;
        protected static Color[] PossibleColors;
        protected static int[] PossibleGlyphs;

        public Plant()
        {
            PossibleGlyphs = new[] { 23, 244 };
            PossibleColors = new[] { Color.LawnGreen, Color.LimeGreen, Color.SpringGreen };

            float random_variance = (.5f - (float)Util.GlobalRng.NextDouble()) * 2; // -1 to 1
            FirstPersonVerticalOffset = 1f + random_variance / 2;
            FirstPersonScale = new Vector2(1.0f, 2f + random_variance);

            SetGraphics();
        }

        protected void SetGraphics()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(PossibleColors[Util.GlobalRng.Next(PossibleColors.Length)], Color.Transparent, PossibleGlyphs[Util.GlobalRng.Next(PossibleGlyphs.Length)]));
        }
    }

    public class Sapling : Plant
    {
        public override bool BlocksMovement => false;

        public Sapling()
        {
            PossibleGlyphs = new[] { 231, 252, 245 };
            
            float random_variance = (.5f - (float)Util.GlobalRng.NextDouble()) * 2; // -1 to 1
            FirstPersonVerticalOffset = 1.5f + random_variance / 2;
            FirstPersonScale = new Vector2(1.0f, 3f + random_variance);

            SetGraphics();
        }
    }

    public class Flower : Plant
    {
        public override bool BlocksMovement => false;

        public Flower()
        {
            PossibleGlyphs = new[] { 42 };
            PossibleColors = new[] { Color.CornflowerBlue, Color.MediumVioletRed, Color.Violet, Color.Goldenrod };

            FirstPersonVerticalOffset = 1.25f;
            FirstPersonScale = new Vector2(2.0f, 2.5f);

            SetGraphics();
        }
    }
}
