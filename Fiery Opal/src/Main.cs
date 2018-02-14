using System;
using SadConsole;
using Console = SadConsole.Console;
using Microsoft.Xna.Framework;
using FieryOpal.src.UI;
using Microsoft.Xna.Framework.Graphics;

namespace FieryOpal
{
    class Program
    {

        public const int Width = 201;
        public const int Height = 120;

        static MainGameWindowManager mainGameWindowManager;

        public static Texture2D FontTexture;
        public static Font Font;

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
            FontTexture = SadConsole.Game.Instance.Content.Load<Texture2D>("taffer");
            Font = new FontMaster(FontTexture, 10, 10).GetFont(Font.FontSizes.One);
        }
    }
}
