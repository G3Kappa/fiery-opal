using FieryOpal.Src.Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;

namespace FieryOpal.Src.Ui.Dialogs
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
            var noise = Noise.Calc1D(Util.GlobalRng.Next(0, 100), width, .25f);

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
}
