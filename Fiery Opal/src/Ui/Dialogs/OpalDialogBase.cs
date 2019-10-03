using FieryOpal.Src.Ui.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Input;
using SadConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Ui.Dialogs
{
    public abstract class OpalDialog : OpalConsoleWindow
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
                new Tuple<string, Color>("Light", Palette.Ui["WHITE"]),
                new Tuple<string, Color>("ShadeLight", Palette.Ui["LGRAY"]),
                new Tuple<string, Color>("ShadeDark", Palette.Ui["DGRAY"]),
                new Tuple<string, Color>("Dark", Palette.Ui["BLACK"]),
            }
        );

        protected static List<OpalDialog> activeDialogs = new List<OpalDialog>();

        private static Point _MakeSize = new Point(1, 1);
        private static string _MakeCaption = "Untitled Dialog";
        private static Font _MakeFont = null;

        public static int CurrentDialogCount => activeDialogs.Count;

        public static OpalDialog ActiveDialog => activeDialogs.Count > 0 ? activeDialogs.Last() : null;

        protected OpalDialog()
            : this(_MakeSize.X, _MakeSize.Y, _MakeCaption, "", _MakeFont)
        {

        }

        protected virtual void PrintText(string text)
        {
            Print(1, 1, new ColoredString(text, Palette.Ui["DefaultForeground"], Palette.Ui["DefaultBackground"]));
        }

        public OpalDialog(int width, int height, string caption, string text, Font f) : base(width, height, caption, f)
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
            return Make<T>(caption, text, new Point(-1, -1));
        }

        public static T Make<T>(string caption, string text, Point size, Font f=null, bool borderless=false)
            where T : OpalDialog, new()
        {
            f = f ?? Nexus.Fonts.Spritesheets["Books"];

            Point dfSz = new Point(Nexus.InitInfo.DefaultFontWidth, Nexus.InitInfo.DefaultFontHeight);
            Point fSz = f.Size;
            Vector2 fontRatio = dfSz.ToVector2() / (fSz).ToVector2();

            _MakeSize = (new Point(size.X > 0 ? size.X : Nexus.DialogRect.Width, size.Y > 0 ? size.Y : Nexus.DialogRect.Height).ToVector2() * fontRatio).ToPoint();
            _MakeCaption = caption;
            _MakeFont = f;
            if(!borderless && Nexus.DialogRect.Width != Nexus.Width && Nexus.DialogRect.Height != Nexus.Height)
            {
                _MakeSize -= new Point(2);
            }
            T dialog = new T()
            {
                Position = Nexus.DialogRect.Location
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
