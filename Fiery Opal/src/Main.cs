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

        public static int Width { get; private set; } = 180;
        public static int Height { get; private set; } = 80;

        static MainGameWindowManager mainGameWindowManager;
        static World world;

        public static FontConfigInfo Fonts { get; private set; }
        public static LocalizationInfo LocalizationInfo { get; private set; }

        static void Main(string[] args)
        {
            CreatePaths();
            InitConfigInfo initInfo = Util.LoadDefaultInitConfig();
            Width = initInfo.ProgramWidth;
            Height = initInfo.ProgramHeight;

            LocalizationInfo = new LocalizationLoader().LoadFile(initInfo.Locale);

            world = new World(initInfo.WorldWidth, initInfo.WorldHeight);
            world.Generate();

            Keybind.PushState();
            SadConsole.Game.Create(initInfo.DefaultFontPath, Width, Height);
            
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
            System.IO.Directory.CreateDirectory("./cfg/locale");
        }

        private static void Init()
        {
            Fonts = Util.LoadDefaultFontConfig();

            OpalGame g = new OpalGame(world);
            mainGameWindowManager = new MainGameWindowManager(Width, Height, g);
            Util.Log(Util.Localize("WelcomeMessage"), false);
        }
    }
}
