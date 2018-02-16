using System;
using SadConsole;
using Microsoft.Xna.Framework;
using FieryOpal.src.ui;
using FieryOpal.src;

namespace FieryOpal
{
    class Program
    {

        public const int Width = 160;
        public const int Height = 80;

        static MainGameWindowManager mainGameWindowManager;

        public static Font Font;
        public static Font FPFont;
        public static Font HDFont;

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
            Keybind.Update();
            mainGameWindowManager.Update(time);
        }

        private static void Draw(GameTime time)
        {
            mainGameWindowManager.Draw(time);
        }

        private static void Init()
        {
            Font = Global.LoadFont("Taffer.font").GetFont(Font.FontSizes.One);
            FPFont = Global.LoadFont("Kein.font").GetFont(Font.FontSizes.One);
            HDFont = Global.LoadFont("HD.font").GetFont(Font.FontSizes.One);

            OpalLocalMap map = new OpalLocalMap(Width / 2, Height / 2);
            OpalGame g = new OpalGame(map);

            mainGameWindowManager = new MainGameWindowManager(Width, Height, g);
        }
    }
}
