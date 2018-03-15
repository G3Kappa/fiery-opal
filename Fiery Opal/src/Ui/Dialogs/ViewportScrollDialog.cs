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
    public class ViewportScrollDialog<T> : ScrollDialog
        where T : Viewport
    {
        public T Viewport;
        private TileMemory GodFog = new TileMemory();

        public ViewportScrollDialog() : base()
        {
            GodFog.Toggle();
        }

        private void ScrollViewArea(int x, int y)
        {
            Point p = new Point(x, y);
            Rectangle area = new Rectangle(Viewport.ViewArea.Location + p, Viewport.ViewArea.Size);

            if (area.X < 0 || area.Y < 0 || area.X + area.Width >= Viewport.TargetWidth + 1 || area.Y + area.Height >= Viewport.TargetHeight + 1) return;
            Viewport.ViewArea = area;
        }

        protected override void BindKeys()
        {
            base.BindKeys();
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.W, Keybind.KeypressState.Press, "Map: Scroll up"), (i) => { ScrollViewArea(0, -1); });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.A, Keybind.KeypressState.Press, "Map: Scroll left"), (i) => { ScrollViewArea(-1, 0); });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.S, Keybind.KeypressState.Press, "Map: Scroll down"), (i) => { ScrollViewArea(0, 1); });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.D, Keybind.KeypressState.Press, "Map: Scroll right"), (i) => { ScrollViewArea(1, 0); });
        }

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
            Viewport.ViewArea = new Rectangle(Viewport.ViewArea.Location, new Point(WriteableArea.Width, WriteableArea.Height));
            Viewport?.Print(WriteableArea, new Rectangle(new Point(0), new Point(WriteableArea.Width, WriteableArea.Height)), GodFog);
        }
    }

}
