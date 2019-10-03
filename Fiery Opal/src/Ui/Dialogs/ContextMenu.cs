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
            Actions = new List<Tuple<string, Action<T>, Keybind.KeybindInfo>>();

            textSurface.DefaultBackground =
                Theme.FillStyle.Background =
                DefaultPalette["Dark"];
            Clear();
        }

        protected override void BindKeys()
        {
            base.BindKeys();
            if (!BindActions) return;
            foreach (var a in Actions)
            {
                Keybind.BindKey(a.Item3, (i) =>
                {
                    ChosenAction = a.Item2;
                    Hide();
                });
            }
        }

        private void PrintActions()
        {
            Cell keyStyle = new Cell(DefaultPalette["Light"], DefaultPalette["Dark"]);
            Cell textStyle = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["Dark"]);

            int longest_key = Actions.Max(a => a.Item3.ToString().Length);

            for (int i = 0; i < Actions.Count; ++i)
            {
                string letter = Actions[i].Item3.ToString();
                var key = letter.PadRight(longest_key, ' ').ToColoredString(keyStyle);
                var txt = (" -> " + Actions[i].Item1).ToColoredString(textStyle);

                Print(2, i + 2, key + txt);
            }
        }

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
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
