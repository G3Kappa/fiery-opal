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
        public struct NewGameInfo
        {

        }

        public MainMenuWindowManager(int w, int h) : base(w, h)
        {

        }

        public override void Show()
        {
            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(GameTime time)
        {
            base.Draw(time);
        }

        public event Action<NewGameInfo> NewGameStarted;
    }
}
