using SadConsole;
using Microsoft.Xna.Framework;
using FieryOpal.Src;
using FieryOpal.Src.Procedural;
using FieryOpal.src;
using System.Collections.Generic;
using System.Linq;
using System;
using FieryOpal.Src.Ui.Dialogs;
using FieryOpal.Src.Ui.Windows;
using FieryOpal.Src.Ui;

namespace FieryOpal
{
    class Program
    {

        public static int Width { get; private set; }
        public static int Height { get; private set; }

        static MainGameWindowManager mainGameWindowManager;

        public static FontConfigInfo Fonts { get; private set; }
        public static LocalizationInfo Locale { get; private set; }
        public static KeybindConfigInfo Keys { get; private set; }
        public static PaletteConfigInfo PaletteInfo { get; private set; }

        private static InitConfigInfo InitInfo { get; set; }

        static void Main(string[] args)
        {
            CreatePaths();
            InitInfo = Util.LoadDefaultInitConfig();
            Width = InitInfo.ProgramWidth;
            Height = InitInfo.ProgramHeight;

            SadConsole.Game.Create(InitInfo.DefaultFontPath, Width, Height);
            SadConsole.Game.OnInitialize = Init;
            SadConsole.Game.OnUpdate = Update;
            SadConsole.Game.OnDraw = Draw;

            Keybind.PushState();
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
            System.IO.Directory.CreateDirectory("./gfx");
            System.IO.Directory.CreateDirectory("./gfx/extra");
        }

        private static void Init()
        {
            Fonts = Util.LoadDefaultFontConfig();
            Keys = Util.LoadDefaultKeyConfig();
            Locale = Util.LoadDefaultLocalizationConfig(InitInfo);
            PaletteInfo = Util.LoadDefaultPaletteConfig();
            Palette.LoadDefaults(PaletteInfo);

            World world = new World(InitInfo.WorldWidth, InitInfo.WorldHeight);
            world.Generate();
            OpalGame g = new OpalGame(world);
            mainGameWindowManager = new MainGameWindowManager(Width, Height, g);

            Util.Log(Util.Localize("WelcomeMessage"), false);
        }
    }
}
