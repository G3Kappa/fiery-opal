using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;

namespace FieryOpal.src.ui
{
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

    public class Viewport
    {
        public OpalLocalMap Target { get; protected set; }

        public Rectangle ViewArea { get; set; }
        public ViewportFog Fog { get; set; }

        public Viewport(OpalLocalMap target, Rectangle view_area)
        {
            ViewArea = view_area;
            Target = target;
            Fog = new ViewportFog();
        }

        private void PrintFog(OpalConsoleWindow surface, Point p)
        {
            surface.SetCell(p.X, p.Y, new Cell(Target.FogColor, Target.FogColor, ' '));
        }

        public virtual void Print(OpalConsoleWindow surface, Rectangle targetArea)
        {
            surface.Clear();
            var tiles = Target.TilesWithin(ViewArea);
            foreach (var tuple in tiles)
            {
                OpalTile t = tuple.Item1;
                Point pos = tuple.Item2 - new Point(ViewArea.X, ViewArea.Y);
                if (pos.X >= targetArea.Width || pos.Y >= targetArea.Height)
                {
                    continue;
                }
                if (!Fog.KnowsOf(tuple.Item2))
                {
                    PrintFog(surface, pos + targetArea.Location);
                    continue;
                }
                else if (!Fog.CanSee(tuple.Item2))
                {
                    surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, new Cell(Palette.Ui["UnseenTileForeground"], Palette.Ui["UnseenTileBackground"], tuple.Item1.Graphics.Glyph));
                }
                else
                {
                    surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, tuple.Item1.Graphics);
                }
            }

            var actors = Target.ActorsWithin(ViewArea);
            foreach (var act in actors)
            {
                if (!act.Visible) continue;
                Point pos = act.LocalPosition - new Point(ViewArea.X, ViewArea.Y);
                if (pos.X >= targetArea.Width || pos.Y >= targetArea.Height)
                {
                    continue;
                }
                if (!Fog.KnowsOf(act.LocalPosition))
                {
                    continue;
                }
                else if(act is DecorationBase && !Fog.CanSee(act.LocalPosition))
                {
                    surface.SetGlyph(targetArea.X + pos.X, targetArea.Y + pos.Y, act.Graphics.Glyph);
                }
                else // Right now, creatures (but not decorations, assumedly unmoving) that move in known tiles are always seen by the player
                {
                    surface.SetForeground(targetArea.X + pos.X, targetArea.Y + pos.Y, act.Graphics.Foreground);
                    surface.SetGlyph(targetArea.X + pos.X, targetArea.Y + pos.Y, act.Graphics.Glyph);
                }
            }
        }
    }
}
