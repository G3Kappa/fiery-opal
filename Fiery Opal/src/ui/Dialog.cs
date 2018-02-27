using Microsoft.Xna.Framework;
using SadConsole;
using SadConsole.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SadConsole.Input;
using SadConsole.Themes;

namespace FieryOpal.src.ui
{
    public class OpalDialog : OpalConsoleWindow
    { 
        public enum OpalDialogResult
        {
            STILL_OPEN = -1,
            CLOSED = 0,
            CANCEL = 1,
            OK = 2
        }

        public OpalDialogResult Result { get; protected set; }

        protected Button OkButton, CancelButton;
        protected static ButtonTheme DialogButtonTheme = new ButtonTheme()
        {
            Normal = new Cell(Palette.Ui["DefaultForeground"], Palette.Ui["DefaultBackground"]),
            MouseOver = new Cell(Palette.Ui["InfoMessage"], Palette.Ui["DefaultBackground"]),
            MouseClicking = new Cell(Palette.Ui["BoringMessage"], Palette.Ui["DefaultBackground"]),
            Focused = new Cell(Palette.Ui["BoringMessage"], Palette.Ui["DefaultBackground"]),
            Disabled = new Cell(Palette.Ui["DefaultBackground"], Palette.Ui["BoringMessage"])
        };

        protected OpalDialog(int width, int height, string caption, string text, string ok_text = "Ok", string cancel_text = "Cancel") : base(width, height, caption)
        {
            OkButton = new Button(ok_text.Length + 4);
            OkButton.Position = new Point(width - (ok_text.Length + 7), height - 4);
            OkButton.Text = ok_text;
            OkButton.Theme = DialogButtonTheme;
            OkButton.Click += (btn, args) =>
            {
                Result = OpalDialogResult.OK;
                Hide();
            };
            Add(OkButton);

            CancelButton = new Button(cancel_text.Length + 4);
            CancelButton.Position = new Point(width - (cancel_text.Length + 4 + ok_text.Length + 8), height - 4);
            CancelButton.Text = cancel_text;
            CancelButton.Theme = DialogButtonTheme;
            CancelButton.Click += (btn, args) =>
            {
                Result = OpalDialogResult.CANCEL;
                Hide();
            };
            Add(CancelButton);
            FocusedControl = CancelButton; // Force redraw with new theme

            Result = OpalDialogResult.STILL_OPEN;
            Closed += (e, f) =>
            {
                if (Result == OpalDialogResult.STILL_OPEN) Result = OpalDialogResult.CLOSED;
            };

            Hide(); // Starts shown but Show() should show it
            Clear();
            Print(0, 0, new ColoredString(text, Palette.Ui["DefaultForeground"], Palette.Ui["DefaultBackground"]));
        }

        public override bool ProcessMouse(MouseConsoleState state)
        {
            var mouse = state.Mouse;
            mouse.ScreenPosition -= TextSurface.Font.Size; // Adjust for border
            return base.ProcessMouse(new MouseConsoleState(state.Console, mouse));
        }

        protected static List<OpalDialog> activeDialogs = new List<OpalDialog>();

        public static void Show(string caption, string text, Action<OpalDialogResult> onResult, string ok_text = "Ok", string cancel_text = "Cancel")
        {
            OpalDialog dialog = new OpalDialog(Program.Width / 2 - 2, Program.Height / 2 - 2, caption, text, ok_text, cancel_text);
            dialog.Position = new Point(1, Program.Height / 8);
            dialog.Show();
            activeDialogs.Add(dialog);
            dialog.Closed += (e, f) => { onResult(dialog.Result); activeDialogs.Remove(dialog); };
        }

        public static void Draw(GameTime gt)
        {
            foreach(var d in activeDialogs)
            {
                d.Draw(gt.ElapsedGameTime);
            }
        }

        public static int CurrentDialogCount => activeDialogs.Count;
    }
}
