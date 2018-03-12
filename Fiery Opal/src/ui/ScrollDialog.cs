using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;

namespace FieryOpal.Src.Ui
{
    public class ScrollDialog : OpalDialog
    {
        protected new static Palette DefaultPalette = new Palette(
            new[] {
                new Tuple<string, Color>("Light", new Color(222, 221, 195)),
                new Tuple<string, Color>("ShadeLight", new Color(191, 171, 143)),
                new Tuple<string, Color>("ShadeDark", new Color(148, 134, 100)),
                new Tuple<string, Color>("Dark", new Color(102, 82, 51)),
            }
        );

        protected SadConsole.Console WriteableArea;

        public ScrollDialog() : base()
        {
            Borderless = true;
            WriteableArea = new SadConsole.Console(Width - 12, Height - 8);

            textSurface.DefaultBackground =
                Theme.FillStyle.Background = Color.Transparent;
            Clear();

            WriteableArea.Position = Position + new Point(8, 6);
        }

        protected override void BindKeys()
        {
            base.BindKeys();
        }

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
            PrintRoll(0, 0);
            PrintRoll(Width - 5, 0);
            PrintPage();
            WriteableArea.Draw(delta);
        }

        private void PrintRoll(int x, int y)
        {
            Cell style1 = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["ShadeDark"]);
            Cell style2 = new Cell(DefaultPalette["ShadeDark"], DefaultPalette["ShadeLight"]);
            Cell style3 = new Cell(DefaultPalette["ShadeDark"], DefaultPalette["Dark"]);

            for (int i = 0; i < 5; ++i)
            {
                VPrint(x + i, y + 2, " ".Repeat(Height - 4).ToColoredString(i != 1 ? style1 : style2));
            }
            Print(x, y + 1, " ".Repeat(5).ToColoredString(style3));
            Print(x + 1, y, " ".Repeat(3).ToColoredString(style3));
            Print(x + 1, y + 1, " ".ToColoredString(style1));

            Print(x, y + Height - 2, " ".Repeat(5).ToColoredString(style3));
            Print(x + 1, y + Height - 1, " ".Repeat(3).ToColoredString(style3));
            Print(x + 1, y + Height - 2, " ".ToColoredString(style1));
        }

        private ColoredString MakeBorder(int width)
        {
            ColoredGlyph[] glyphs = new ColoredGlyph[width];
            var noise = Simplex.Noise.Calc1D(Util.GlobalRng.Next(0, 100), width, .25f);

            for (int i = 0; i < width; ++i)
            {
                var color = noise[i] < .5f ? Color.Transparent : DefaultPalette["Light"];

                glyphs[i] = new ColoredGlyph(new Cell(color, color, ' '));
            }

            return new ColoredString(glyphs);
        }

        private ColoredString topBorder;
        ColoredString TopBorder
        {
            get
            {
                if (topBorder != null) return topBorder;
                topBorder = MakeBorder(Width - 10);
                return topBorder;
            }
        }

        private ColoredString botBorder;
        ColoredString BottomBorder
        {
            get
            {
                if (botBorder != null) return botBorder;
                botBorder = MakeBorder(Width - 10);
                return botBorder;
            }
        }

        private void PrintPage()
        {
            Cell style1 = new Cell(Color.Transparent, DefaultPalette["Light"]);

            Print(5, 2, TopBorder);
            for (int j = 1; j < Height - 5; ++j)
            {
                Print(5, 2 + j, " ".Repeat(Width - 10).ToColoredString(style1));
            }
            Print(5, 2 + Height - 5, BottomBorder);
        }

        protected override void PrintText(string text)
        {
            WriteableArea?.Print(0, 0, text.ToColoredString(DefaultPalette["ShadeDark"], DefaultPalette["Light"]));
        }
    }


    public class ViewportScrollDialog<T> : ScrollDialog
        where T : Viewport
    {
        public T Viewport;

        public ViewportScrollDialog() : base()
        {

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
            Viewport?.Print(WriteableArea, new Rectangle(new Point(0), new Point(WriteableArea.Width, WriteableArea.Height)));
        }
    }

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
