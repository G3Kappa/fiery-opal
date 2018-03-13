using FieryOpal.Src;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src.Ui.Dialogs
{
    public class WorldMapScrollDialog : ViewportScrollDialog<WorldMapViewport>
    {
        private void MoveCursor(int x, int y)
        {
            Point p = Viewport.CursorPosition + new Point(x, y);
            if (p.X < 0 || p.Y < 0) return;
            if (p.X >= Viewport.TargetWidth || p.Y >= Viewport.TargetHeight) return;

            Viewport.CursorPosition = p;
        }

        protected override void BindKeys()
        {
            base.BindKeys();
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Up, Keybind.KeypressState.Press, "Map: Move cursor up"), (i) => { MoveCursor(0, -1); });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Left, Keybind.KeypressState.Press, "Map: Move cursor left"), (i) => { MoveCursor(-1, 0); });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Down, Keybind.KeypressState.Press, "Map: Move cursor down"), (i) => { MoveCursor(0, 1); });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Right, Keybind.KeypressState.Press, "Map: Move cursor right"), (i) => { MoveCursor(1, 0); });
        }
    }
}
