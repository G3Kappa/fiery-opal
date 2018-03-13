using SadConsole;
using Microsoft.Xna.Framework;
using FieryOpal.Src.Ui;
using FieryOpal.Src;
using FieryOpal.Src.Procedural;
using FieryOpal.src;
using System.Collections.Generic;
using System.Linq;
using System;

namespace FieryOpal
{
    class Program
    {

        public const int Width = 180;
        public const int Height = 80;

        static MainGameWindowManager mainGameWindowManager;

        public static FontConfigInfo Fonts;

        static void Main(string[] args)
        {
            CreatePaths();

            Keybind.PushState();
            SadConsole.Game.Create("gfx/Taffer.font", Width, Height);
            
            SadConsole.Game.OnInitialize = Init;
            SadConsole.Game.OnUpdate = Update;
            SadConsole.Game.OnDraw = Draw;

            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
            Keybind.PopState();
        }

        private static void Update(GameTime time)
        {
            Keybind.Update();
            Util.Update(time);
            mainGameWindowManager.Update(time);
            OpalDialog.Update(time);
        }

        private static void Draw(GameTime time)
        {
            mainGameWindowManager.Draw(time);
            OpalDialog.Draw(time);
        }

        private static void CreatePaths()
        {
            System.IO.Directory.CreateDirectory("./save");
            System.IO.Directory.CreateDirectory("./cfg");
        }

        private static void Init()
        {
            Fonts = Util.LoadDefaultFontConfig();

            World w = new World(100, 100);
            w.Generate();
            OpalGame g = new OpalGame(w);

            mainGameWindowManager = new MainGameWindowManager(Width, Height, g);
            Util.Log("Welcome to Fiery Opal!", false);
        }
    }
}
