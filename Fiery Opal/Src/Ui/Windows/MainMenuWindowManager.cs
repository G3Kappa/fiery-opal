using FieryOpal.Src.Actors;
using FieryOpal.Src.Actors.Items.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Linq;
using System.Threading;

namespace FieryOpal.Src.Ui.Windows
{
    public class MainMenuWindowManager : WindowManager
    {
        public MainMenuWindow MainMenu { get; }

        public MainMenuWindowManager(int w, int h) : base(w, h)
        {
            Vector2 fontRatio = Nexus.Fonts.MainFont.Size.ToVector2() / Nexus.Fonts.Spritesheets["Books"].Size.ToVector2();
            MainMenu = new MainMenuWindow(82, 52);
            //MainMenu.Position = new Point((int)((w / 2) * fontRatio.X) - 40, (int)((h / 2) * fontRatio.Y) - 25);
        }

        public bool InGame = false;

        public override void Show()
        {
            base.Show();

            MainMenu.SetLabels(InGame);
            MainMenu.Show();
        }

        public override void Hide()
        {
            base.Hide();

            MainMenu.Hide();
        }

        public override void HandleInput()
        {
            base.HandleInput();
            MainMenu.ProcessKeyboard(Global.KeyboardState);
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
            MainMenu.Update(time.ElapsedGameTime);
        }

        public override void Draw(GameTime time)
        {
            base.Draw(time);
            MainMenu.Draw(time.ElapsedGameTime);
        }
    }
}
