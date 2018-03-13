using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Ui.Dialogs
{
    public class ContextMenu<T> : OpalDialog
    {
        private List<Tuple<string, Action<T>, Keybind.KeybindInfo>> Actions;

        public Action<T> ChosenAction = null;
        public bool BindActions = true;

        public ContextMenu() : base()
        {
            Borderless = true;
            Actions = new List<Tuple<string, Action<T>, Keybind.KeybindInfo>>();

            textSurface.DefaultBackground = 
                Theme.FillStyle.Background = 
                DefaultPalette["ShadeDark"];
            Clear();
        }

        protected override void BindKeys()
        {
            base.BindKeys();
            if (!BindActions) return;
            foreach(var a in Actions)
            {
                Keybind.BindKey(a.Item3, (i) =>
                {
                    ChosenAction = a.Item2;
                    Hide();
                });
            }
        }

        private void PrintSideBorders()
        {
            Cell railStyle = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["ShadeDark"]);

            int leftRailGlyph = 221;
            int rightRailGlyph = 222;

            VPrint(0, 0, ((char)leftRailGlyph).Repeat(Height).ToColoredString(railStyle));
            VPrint(Width - 1, 0, ((char)rightRailGlyph).Repeat(Height).ToColoredString(railStyle));
        }

        private void PrintTopBottomBorders()
        {
            Cell borderStyle = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["ShadeDark"]);

            int fullBlockGlyph = 219;
            int leftTGlyph = 195;
            int rightTGlyph = 180;
            int hLineGlyph = 196;

            string top_border = "" + ((char)fullBlockGlyph) + ((char)leftTGlyph) + ((char)hLineGlyph).Repeat(Width - 4) + ((char)rightTGlyph) + ((char)fullBlockGlyph);
            string bottom_border = "" + ((char)fullBlockGlyph) + ((char)hLineGlyph).Repeat(Width - 2) + ((char)fullBlockGlyph);

            Print(0, Height - 1, bottom_border.ToColoredString(borderStyle));
            Print(0, 0, top_border.ToColoredString(borderStyle));

            Print(Width / 2 - Caption.Length / 2, 0, Caption.ToColoredString(borderStyle));
        }

        private void PrintActions()
        {
            string fmt = "{0} -> {1}";
            Cell textStyle = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["ShadeDark"]);

            int longest_key = Actions.Max(a => a.Item3.ToString().Length);

            for (int i = 0; i < Actions.Count; ++i)
            {
                string letter = Actions[i].Item3.ToString();
                var str = String.Format(fmt, letter.PadRight(longest_key, ' '), Actions[i].Item1);

                Print(2, i + 2, str.ToColoredString(textStyle));
            }
        }

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
            PrintSideBorders();
            PrintTopBottomBorders();
            PrintActions();
        }

        protected override void PrintText(string text)
        {
            return;
        }

        public void AddAction(string label, Action<T> action, Keybind.KeybindInfo kbinfo)
        {
            Actions.Add(new Tuple<string, Action<T>, Keybind.KeybindInfo>(label, action, kbinfo));
        }
    }
}
