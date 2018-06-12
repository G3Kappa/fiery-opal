using FieryOpal.Src;
using FieryOpal.Src.Actors;
using FieryOpal.Src.Procedural;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Dialogs;
using FieryOpal.Src.Ui.Windows;
using Microsoft.Xna.Framework;

namespace FieryOpal
{
    public static class Nexus
    {

        public static int Width { get; private set; }
        public static int Height { get; private set; }

        static MainGameWindowManager mainGameWindowManager;

        public static FontConfigInfo Fonts { get; private set; }
        public static LocalizationInfo Locale { get; private set; }
        public static KeybindConfigInfo Keys { get; private set; }
        public static PaletteConfigInfo PaletteInfo { get; private set; }
        public static OpalGame GameInstance { get; private set; }
        public static TurnTakingActor Player => GameInstance.Player;

        public static InitConfigInfo InitInfo { get; set; }

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
            Util.Update(time);
            OpalDialog.Update(time);
            Keybind.Update();
            mainGameWindowManager.Update(time);
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
            System.IO.Directory.CreateDirectory("./cfg/log");
            System.IO.Directory.CreateDirectory("./cfg/locale");
            System.IO.Directory.CreateDirectory("./cfg/scripts");
            System.IO.Directory.CreateDirectory("./cfg/keybinds");
            System.IO.Directory.CreateDirectory("./cfg/palettes");
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
            GameInstance = new OpalGame(world);
            mainGameWindowManager = new MainGameWindowManager(Width, Height, GameInstance);

            TypeConversionHelper<object>.RegisterDefaultConversions();
            TileSkeleton.PreloadAllSkeletons();
            OpalActorBase.PreloadActorClasses("");
            OpalActorBase.PreloadActorClasses("Animals");
            OpalActorBase.PreloadActorClasses("Decorations");
            LuaVM.Init();

            Util.Log(Util.Str("WelcomeMessage"), false);
        }
    }
}
