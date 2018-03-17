using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using SadConsole.Input;
using SadConsole.Themes;
using Microsoft.Xna.Framework.Input;
using FieryOpal.Src.Ui.Windows;

namespace FieryOpal.Src.Ui.Dialogs
{
    public abstract  class OpalDialog : OpalConsoleWindow
    {
        public static ButtonTheme DialogButtonTheme = new ButtonTheme()
        {
            Normal = new Cell(Palette.Ui["DefaultForeground"], Palette.Ui["DefaultBackground"]),
            MouseOver = new Cell(Palette.Ui["InfoMessage"], Palette.Ui["DefaultBackground"]),
            MouseClicking = new Cell(Palette.Ui["BoringMessage"], Palette.Ui["DefaultBackground"]),
            Focused = new Cell(Palette.Ui["BoringMessage"], Palette.Ui["DefaultBackground"]),
            Disabled = new Cell(Palette.Ui["DefaultBackground"], Palette.Ui["BoringMessage"])
        };

        protected static Palette DefaultPalette = new Palette(
            new[] {
                new Tuple<string, Color>("Light", new Color(255, 255, 255)),
                new Tuple<string, Color>("ShadeLight", new Color(191, 191, 191)),
                new Tuple<string, Color>("ShadeDark", new Color(77, 77, 77)),
                new Tuple<string, Color>("Dark", new Color(51, 51, 51)),
            }
        );

        protected static List<OpalDialog> activeDialogs = new List<OpalDialog>();

        private static Point _MakeSize = new Point(1, 1);
        private static string _MakeCaption = "Untitled Dialog";

        public static int CurrentDialogCount => activeDialogs.Count;

        protected OpalDialog()
            : this(_MakeSize.X, _MakeSize.Y, _MakeCaption, "")
        {

        }

        protected virtual void PrintText(string text)
        {
            Print(1, 1, new ColoredString(text, Palette.Ui["DefaultForeground"], Palette.Ui["DefaultBackground"]));
        }

        public OpalDialog(int width, int height, string caption, string text) : base(width, height, caption)
        {
            Hide();  // Starts shown
            Clear();
            PrintText(text);

            CloseOnESC = false;
        }

        public override void Show(bool modal)
        {
            base.Show(modal);
            if (!activeDialogs.Contains(this)) activeDialogs.Add(this);
        }

        public override void Hide()
        {
            base.Hide();
            if (activeDialogs.Contains(this)) activeDialogs.Remove(this);
        }

        public override bool ProcessMouse(MouseConsoleState state)
        {
            var mouse = state.Mouse;
            mouse.ScreenPosition -= TextSurface.Font.Size; // Adjust for border
            return base.ProcessMouse(new MouseConsoleState(state.Console, mouse));
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            return base.ProcessKeyboard(info);
        }

        public static void Update(GameTime gt)
        {
            var dialogs = activeDialogs.ToList();
            foreach (var d in dialogs)
            {
                d.ProcessKeyboard(Global.KeyboardState);
                d.Update(gt.ElapsedGameTime);
            }
        }

        public static void Draw(GameTime gt)
        {
            foreach (var d in activeDialogs)
            {
                d.Draw(gt.ElapsedGameTime);
            }
        }

        public static T Make<T>(string caption, string text)
            where T : OpalDialog, new()
        {
            return Make<T>(caption, text, new Point(Program.Width / 2 - 2, Program.Height - Program.Height / 4 - 2));
        }

        public static T Make<T>(string caption, string text, Point size)
            where T : OpalDialog, new()
        {
            _MakeSize = size;
            _MakeCaption = caption;
            T dialog = new T()
            {
                Position = new Point(Program.Width / 2 - size.X / 2, Program.Height / 2 - size.Y / 2)
            };
            dialog.PrintText(text);
            return dialog;
        }

        protected virtual void BindKeys()
        {
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Escape, Keybind.KeypressState.Press, "Close this dialog"), (info) => Hide());
        }

        public static void LendKeyboardFocus<T>(T d)
            where T : OpalDialog
        {
            Keybind.PushState();
            d.BindKeys();
            d.Closed += (e, eh) =>
            {
                Keybind.PopState();
            };
        }
    }
}
