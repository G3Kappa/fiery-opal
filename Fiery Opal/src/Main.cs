using System;
using SadConsole;
using Console = SadConsole.Console;
using Microsoft.Xna.Framework;
using FieryOpal.src.UI;

namespace FieryOpal
{
    class Program
    {

        public const int Width = 80;
        public const int Height = 25;

        static MainGameWindowManager mainGameWindowManager;

        static void Main(string[] args)
        {
            SadConsole.Game.Create("Taffer.font", Width, Height);
            
            SadConsole.Game.OnInitialize = Init;
            SadConsole.Game.OnUpdate = Update;
            SadConsole.Game.OnDraw = Draw;

            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        private static void Update(GameTime time)
        {
            mainGameWindowManager.Update(time);
        }

        private static void Draw(GameTime time)
        {
            mainGameWindowManager.Draw(time);
        }

        private static void Init()
        {
            mainGameWindowManager = new MainGameWindowManager(Width, Height);
        }
    }
}
