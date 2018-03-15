using FieryOpal.Src.Procedural;
using Microsoft.Xna.Framework;
using SadConsole;

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

        public LocalMapViewport(OpalLocalMap target, Rectangle view_area)
        {
            ViewArea = view_area;
            Target = target;
        }

        private void PrintFog(SadConsole.Console surface, Point p)
        {
            surface.SetCell(p.X, p.Y, new Cell(Target.FogColor, Target.FogColor, ' '));
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

            var actors = Target.ActorsWithin(ViewArea);
            foreach (var act in actors)
            {
                if (!act.Visible) continue;
                Point pos = act.LocalPosition - new Point(ViewArea.X, ViewArea.Y);
                if (pos.X >= targetArea.Width || pos.Y >= targetArea.Height)
                {
                    continue;
                }
                if (!fog.KnowsOf(act.LocalPosition))
                {
                    continue;
                }
                else if(act is DecorationBase && !fog.CanSee(act.LocalPosition))
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

    public class WorldMapViewport : Viewport
    {
        public World Target;
        public Point CursorPosition = new Point();
        public Cell Cursor = new Cell(Color.Red, Color.Transparent, 'X');

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
            var regions = Target.RegionsWithin(ViewArea);
            foreach(var tuple in regions)
            {
                WorldTile t = tuple.Item1;
                if (t == null) continue;

                Point pos = tuple.Item2 - ViewArea.Location;
                if (targetArea.X + pos.X >= targetArea.Width || targetArea.Y + pos.Y >= targetArea.Height) continue;

                surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, t.Graphics);
                if(tuple.Item2 == CursorPosition)
                {
                    surface.SetForeground(targetArea.X + pos.X, targetArea.Y + pos.Y, Cursor.Foreground);
                    surface.SetGlyph(targetArea.X + pos.X, targetArea.Y + pos.Y, Cursor.Glyph);
                }
            }
        }
    }
}
