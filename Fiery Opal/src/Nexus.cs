using FieryOpal.Src.Multiplayer;
using FieryOpal.Src;
using FieryOpal.Src.Actors;
using FieryOpal.Src.Quests;
using FieryOpal.Src.Procedural;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Dialogs;
using FieryOpal.Src.Ui.Windows;
using Microsoft.Xna.Framework;
using System;
using FieryOpal.Src.Actors.Items;

namespace FieryOpal
{
    public static class Nexus
    {

        public static int Width { get; private set; }
        public static int Height { get; private set; }

        static GameWindowManager gameWindowManager;
        static MainMenuWindowManager mainMenuWindowManager;
        static WindowManager currentWM;

        public static FontConfigInfo Fonts { get; private set; }
        public static LocalizationInfo Locale { get; private set; }
        public static KeybindConfigInfo Keys { get; private set; }
        public static PaletteConfigInfo PaletteInfo { get; private set; }
        public static OpalGame GameInstance { get; private set; }
        public static TurnTakingActor Player => GameInstance.Player;
        public static DebugCLI DebugCLI { get; private set; }
        public static QuestManager Quests { get; private set; }

        public static InitConfigInfo InitInfo { get; set; }

        public static OpalServer GameServer { get; set; }
        public static OpalClient GameClient { get; set; }

        public static DayNightCycleManager DayNightCycle { get; private set; }

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
            Keybind.PushState();

            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        private static void Update(GameTime time)
        {
            OpalDialog.Update(time);
            Keybind.Update();
            currentWM.Update(time);
        }


        private static void Draw(GameTime time)
        {
            currentWM.Draw(time);
            OpalDialog.Draw(time);
        }

        private static void CreatePaths()
        {
            System.IO.Directory.CreateDirectory("./save");
            System.IO.Directory.CreateDirectory("./cfg");
            System.IO.Directory.CreateDirectory("./cfg/log");
            System.IO.Directory.CreateDirectory("./cfg/locale");
            System.IO.Directory.CreateDirectory("./cfg/scripts");
            System.IO.Directory.CreateDirectory("./cfg/keybinds");
            System.IO.Directory.CreateDirectory("./cfg/palettes");
            System.IO.Directory.CreateDirectory("./gfx");
            System.IO.Directory.CreateDirectory("./gfx/extra");
            System.IO.Directory.CreateDirectory("./gfx/shaders");
            System.IO.Directory.CreateDirectory("./sfx");
            System.IO.Directory.CreateDirectory("./sfx/soundtrack");
            System.IO.Directory.CreateDirectory("./sfx/effects");
        }

        private static void Init()
        {
            var fpsCounter = new SadConsole.Game.FPSCounterComponent(SadConsole.Game.Instance);
            SadConsole.Game.Instance.Components.Add(fpsCounter);

            Fonts = Util.LoadDefaultFontConfig();
            Keys = Util.LoadDefaultKeyConfig();
            Locale = Util.LoadDefaultLocalizationConfig(InitInfo);
            PaletteInfo = Util.LoadDefaultPaletteConfig();
            Palette.LoadDefaults(PaletteInfo);
            ShaderManager.LoadContent(SadConsole.Game.Instance.Content);

            SadConsole.Game.Instance.TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 1000 / InitInfo.FPSCap);

            InitInfo.RngSeed = InitInfo.RngSeed ?? Util.Rng.Next();
            Util.SeedRng(InitInfo.RngSeed.Value);

            DebugCLI = OpalDialog.Make<DebugCLI>("CLI", "", new Point((int)(Width * .4f), 4));
            DebugCLI.Position = new Point(0, 0);

            TypeConversionHelper<object>.RegisterDefaultConversions();
            TileSkeleton.PreloadAllSkeletons();
            OpalActorBase.PreloadActorClasses("");
            OpalActorBase.PreloadActorClasses("Animals");
            OpalActorBase.PreloadActorClasses("Decorations");
            OpalActorBase.PreloadActorClasses("Environment");
            OpalActorBase.PreloadActorClasses("Items");
            OpalActorBase.PreloadActorClasses("Items.Weapons");

            mainMenuWindowManager = new MainMenuWindowManager(Width, Height);
            currentWM = mainMenuWindowManager;

            mainMenuWindowManager.NewGameStarted += (info) =>
            {
                currentWM = gameWindowManager;
                gameWindowManager.Show();

                World world = new World(InitInfo.WorldWidth, InitInfo.WorldHeight);
                world.Generate();
                GameInstance = new OpalGame(world);
                DayNightCycle = new DayNightCycleManager(1200);
                gameWindowManager = new GameWindowManager(Width, Height, GameInstance);
                OpalLocalMap startingMap = GameInstance.World.RegionAt(Util.Rng.Next(GameInstance.World.Width), Util.Rng.Next(GameInstance.World.Height)).LocalMap;
                Player.ChangeLocalMap(startingMap, new Point(startingMap.Width / 2, startingMap.Height / 2));
                Quests = new QuestManager(Player);

                GameInstance.TurnManager.TurnEnded += (_, __) => { Quests.UpdateProgress(); };

                GameInstance.TurnManager.BeginTurn(GameInstance.CurrentMap);
                Util.LogText(Util.Str("WelcomeMessage"), false);
            };

            LuaVM.Init();
            mainMenuWindowManager.Show();
        }
    }
}
