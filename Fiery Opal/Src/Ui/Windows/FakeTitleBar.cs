using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SadConsole;
using System;

namespace FieryOpal.Src.Ui.Windows
{
    public class FakeTitleBar : OpalConsoleWindow
    {
        public void UpdateCaption(string newCaption)
        {
            ColoredString barL =
                "{0:LCYAN} {1:WHITE} ".FmtC(Palette.Ui["WHITE"], Palette.Ui["DGRAY"],
                (char)7, newCaption);

            ColoredString barR =
                " {0:WHITE}{1:WHITE}{2:RED}".FmtC(Palette.Ui["WHITE"], Palette.Ui["DGRAY"],
                '_', (char)254, 'x');

            ColoredString barC =
                "{0:LGRAY}".Repeat(Width - barL.Count - barR.Count).FmtC(Palette.Ui["WHITE"], Palette.Ui["DGRAY"],
                '=');

            Print(0, 0, barL + barC + barR);
        }

        public FakeTitleBar(int width, string caption, Font f) : base(width + 2, 1, caption, f)
        {
            Borderless = true;
            UpdateCaption(caption);
        }
    }

}
