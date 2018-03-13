using Microsoft.Xna.Framework;
using SadConsole.Controls;

namespace FieryOpal.Src.Ui.Dialogs
{
    class DialogueDialog : OpalDialog
    {
        protected OpalDialog SelectedOption = null;
        public DialogueDialog() : base()
        {
        }

        public void AddOption(string text, OpalDialog d)
        {
            var optButton = new Button(text.Length + 4);
            optButton.Position = new Point(1, Height - 2 - Controls.Count * 2);
            optButton.Text = text;
            optButton.Theme = DialogButtonTheme;
            optButton.Click += (btn, args) =>
            {
                SelectedOption = d;
                ShowNext();
            };
            optButton.EndCharacterLeft = '>';
            optButton.EndCharacterRight = ' ';
            Add(optButton);
            FocusedControl = optButton; // Force redraw with new theme
        }

        private void ShowNext()
        {
            Hide();
            SelectedOption?.Show();
        }

        public override void Show(bool modal)
        {
            if (Controls.Count == 0)
            {
                Util.Log("Tried to display a DialogueDialog with no options!", true);
                return;
            }
            base.Show(modal);
        }
    }
}
