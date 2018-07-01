using FieryOpal.Src.Procedural;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Ui
{
    public abstract class Viewport
    {
        public Rectangle ViewArea { get; set; }

        public abstract int TargetWidth { get; }
        public abstract int TargetHeight { get; }

        public abstract void Print(SadConsole.Console surface, Rectangle targetArea, TileMemory fog);
    }

    public class LocalMapViewport : Viewport
    {
        public OpalLocalMap Target { get; set; }

        public override int TargetWidth => Target?.Width ?? -1;
        public override int TargetHeight => Target?.Height ?? -1;

        private Dictionary<IOpalGameActor, Point> LastKnownPos = new Dictionary<IOpalGameActor, Point>();

        private OpalLocalMap lastTarget;
        public LocalMapViewport(OpalLocalMap target, Rectangle view_area)
        {
            ViewArea = view_area;
            Target = lastTarget = target;
        }

        private void PrintFog(SadConsole.Console surface, Point p)
        {
            surface.SetCell(p.X, p.Y, new Cell(Color.Transparent, Target.FogColor, ' '));
        }

        public void Print(SadConsole.Console surf, TileMemory fog = null)
        {
            Print(surf, Rectangle.Empty, fog);
        }

        private Cell ShadeCell(Cell c, Point p, bool gray=false)
        {
            Color bg = c.Background, fg = c.Foreground;

            fg = Target.Lighting.ApplyShading(fg, p);
            bg = Target.Lighting.ApplyShading(bg, p);

            if (gray)
            {
                var fgb = fg.GetBrightness();
                var bgb = bg.GetBrightness();

                fg = new Color(fgb, fgb, fgb);
                bg = new Color(bgb, bgb, bgb);
            }

            return new Cell(fg, bg, c.Glyph);
        }

        public override void Print(SadConsole.Console surface, Rectangle targetArea, TileMemory fog = null)
        {
            if(lastTarget != Target)
            {
                LastKnownPos.Clear();
                lastTarget = Target;
            }

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

                if (!fog.KnowsOf(tuple.Item2))
                {
                    PrintFog(surface, pos + targetArea.Location);
                    continue;
                }
                else if (!fog.CanSee(tuple.Item2) && !t.Properties.IsBlock)
                {
                    if (t is StairTile)
                    {
                        surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, new Cell(Palette.Ui["UnseenStairsForeground"], Palette.Ui["UnseenStairsBackground"], t.Graphics.Glyph));
                    }
                    else surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, ShadeCell(new Cell(Palette.Ui["UnseenTileForeground"], Palette.Ui["UnseenTileBackground"], tuple.Item1.Graphics.Glyph), tuple.Item2, gray: true));
                }
                else
                {
                    surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, ShadeCell(t.Graphics, tuple.Item2));
                }
            }

            var actors = Target.ActorsWithin(ViewArea).ToList();
            foreach (var k in LastKnownPos.Keys.ToList())
            {
                if (k.Map == Target) actors.Add(k);
                else LastKnownPos.Remove(k);
            }
            // Make sure that the player is always drawn last
            actors.Remove(Nexus.Player);
            actors.Add(Nexus.Player);
            foreach (var act in actors)
            {
                if (act == null) continue;
                Point vw = new Point(ViewArea.X, ViewArea.Y);

                bool canSee = fog.CanSee(act.LocalPosition);
                bool knowsOf = fog.KnowsOf(act.LocalPosition);

                // If we have seen this actor, but they're out of view, draw them at the last position we know of.
                if (LastKnownPos.ContainsKey(act) && !canSee)
                {
                    var p = LastKnownPos[act] - vw;
                    if (Util.OOB(p.X, p.Y, targetArea.Width, targetArea.Height)) continue;

                    surface.SetGlyph(targetArea.X + p.X, targetArea.Y + p.Y, act.Graphics.Glyph);
                    continue;
                }
                // Unexplored tiles
                else if (!knowsOf)
                {
                    continue;
                }
                else if (canSee)
                {
                    if (!act.Visible) continue;
                    LastKnownPos[act] = act.LocalPosition;

                    Point p = act.LocalPosition - vw;
                    if (Util.OOB(p.X, p.Y, targetArea.Width, targetArea.Height)) continue;

                    Color c = act.Graphics.Foreground;
                    if(act.LocalPosition != Nexus.Player.LocalPosition)
                    {
                        c = Target.Lighting.ApplyShading(act.Graphics.Foreground, act.LocalPosition);
                    }

                    surface.SetForeground(targetArea.X + p.X, targetArea.Y + p.Y, c);
                    surface.SetGlyph(targetArea.X + p.X, targetArea.Y + p.Y, act.Graphics.Glyph);
                }
            }
        }
    }

    public class WorldMapViewport : Viewport
    {
        public World Target;
        public Point CursorPosition = new Point();
        public Cell Cursor = new Cell(Color.Red, Color.Transparent, 'X');

        public Dictionary<Point, Cell> Markers = new Dictionary<Point, Cell>();

        public override int TargetWidth => Target?.Width ?? -1;
        public override int TargetHeight => Target?.Height ?? -1;

        public WorldMapViewport(World target, Rectangle view_area) : base()
        {
            ViewArea = view_area;
            Target = target;
        }

        public override void Print(SadConsole.Console surface, Rectangle targetArea, TileMemory fog = null)
        {
            surface.Clear();
            var regions = Target.RegionsWithinRect(ViewArea);
            foreach (var t in regions)
            {
                if (t == null) continue;

                Point pos = t.WorldPosition - ViewArea.Location;
                if (targetArea.X + pos.X >= targetArea.Width || targetArea.Y + pos.Y >= targetArea.Height) continue;

                surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, t.Graphics);

                if (Markers.ContainsKey(t.WorldPosition))
                {
                    var v = Markers[t.WorldPosition];
                    surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, v);
                }
                if (t.WorldPosition == CursorPosition)
                {
                    surface.SetForeground(targetArea.X + pos.X, targetArea.Y + pos.Y, Cursor.Foreground);
                    surface.SetGlyph(targetArea.X + pos.X, targetArea.Y + pos.Y, Cursor.Glyph);
                }
            }
        }
    }
}
