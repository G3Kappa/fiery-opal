using Microsoft.Xna.Framework;

namespace FieryOpal.src.ui
{
    public class Viewport
    {
        public OpalLocalMap Target { get; protected set; }

        public Rectangle ViewArea { get; set; }

        public Viewport(OpalLocalMap target, Rectangle view_area)
        {
            ViewArea = view_area;
            Target = target;
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
                surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, t.Graphics);
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
                surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, act.Graphics);
                // Replace background with original background
                surface.SetBackground(targetArea.X + pos.X, targetArea.Y + pos.Y, Target.TileAt(act.LocalPosition.X, act.LocalPosition.Y).Graphics.Background);
            }
        }
    }
}
