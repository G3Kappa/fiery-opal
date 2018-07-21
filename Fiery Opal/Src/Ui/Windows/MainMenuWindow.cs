using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Input;
using SadConsole.Renderers;
using SadConsole.Shapes;
using SadConsole.Surfaces;
using System;

namespace FieryOpal.Src.Ui.Windows
{
    public class MainMenuWindow : OpalConsoleWindow
    {
        public MainMenuWindow(int width, int height) : base(width - 2, height - 2, "Main Menu", Nexus.Fonts.Spritesheets["Books"])
        {
            Fill(Color.White, Color.Black, ' ');
            Print(1, 1, "Fiery Opal".ToColoredString(Palette.Ui["RED"]));
        }

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
        }

        public override void Show(bool modal)
        {
            base.Show(modal);
            Keybind.PushState();
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.N, Keybind.KeypressState.Press, "Main Menu: New Game"), (info) =>
            {
                NewGamePressed?.Invoke();
            });
        }

        public override void Hide()
        {
            base.Hide();
            Keybind.PopState();
        }

        public event Action NewGamePressed;
    }
}
