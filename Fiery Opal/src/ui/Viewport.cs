using FieryOpal.src.lib;
using FieryOpal.Src.Procedural;
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
        private string CurrentMapName = "";

        public LocalMapViewport(OpalLocalMap target, Rectangle view_area)
        {
            ViewArea = view_area;
            Target = target;
            Nexus.Player.MapChanged += (e, eh) => {
                LastKnownPos.Clear();
                CurrentMapName = Target.Name;
            };
        }

        private void PrintFog(SadConsole.Console surface, Point p)
        {
            surface.SetCell(p.X, p.Y, new Cell(Target.FogColor, Target.FogColor, ' '));
        }

        public void Print(SadConsole.Console surf, TileMemory fog = null)
        {
            Print(surf, Rectangle.Empty, fog);
        }

        public override void Print(SadConsole.Console surface, Rectangle targetArea, TileMemory fog = null)
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

                if (!fog.KnowsOf(tuple.Item2))
                {
                    PrintFog(surface, pos + targetArea.Location);
                    continue;
                }
                else if (!fog.CanSee(tuple.Item2))
                {
                    surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, new Cell(Palette.Ui["UnseenTileForeground"], Palette.Ui["UnseenTileBackground"], tuple.Item1.Graphics.Glyph));
                }
                else
                {
                    surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, tuple.Item1.Graphics);
                }
            }

            var actors = Target.ActorsWithin(ViewArea).ToList();
            foreach (var k in LastKnownPos.Keys)
            {
                if(!(k as OpalActorBase)?.IsDead ?? true)
                    actors.Add(k);
            }
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

                    surface.SetForeground(targetArea.X + p.X, targetArea.Y + p.Y, act.Graphics.Foreground);
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
