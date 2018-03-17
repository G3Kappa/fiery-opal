using FieryOpal.Src.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System.Text.RegularExpressions;

namespace FieryOpal.Src.Ui.Windows
{
    public class MainGameWindowManager : WindowManager
    {
        protected OpalGameWindow FirstPersonWindow, TopDownWindow;
        protected OpalInfoWindow InfoWindow;
        protected OpalLogWindow LogWindow;

        protected OpalGame Game;

        public MainGameWindowManager(int w, int h, OpalGame g) : base(w, h)
        {
            
            Game = g;

            Vector2 font_ratio = Program.Fonts.MainFont.Size.ToVector2() / Program.Fonts.FirstPersonViewportFont.Size.ToVector2();

            Vector2 tdPos = new Vector2(0, .8f);
            Vector2 tdSize = new Vector2(.4f, .8f);

            Vector2 fpPos = new Vector2(.0f, 0) * font_ratio;
            Vector2 fpSize = new Vector2(1f, .8f) * font_ratio;

            Vector2 infoPos = new Vector2(.0f, 0);
            Vector2 infoSize = new Vector2(.2f, .8f);

            Vector2 logPos = new Vector2(0f, .8f);
            Vector2 logSize = new Vector2(1f, .2f);


            FirstPersonWindow = new OpalGameWindow((int)(fpSize.X * w), (int)(fpSize.Y * h), g, new RaycastViewport(g.CurrentMap, new Rectangle(0, 0, (int)(fpSize.X * w), (int)(fpSize.Y * h)), g.Player), Program.Fonts.FirstPersonViewportFont);
            FirstPersonWindow.Position = new Point((int)(fpPos.X * w), (int)(fpPos.Y * h));

            TopDownWindow = new OpalGameWindow((int)(tdSize.X * w), (int)(tdSize.Y * h), g, new LocalMapViewport(g.CurrentMap, new Rectangle(0, 0, (int)(tdSize.X * w), (int)(tdSize.Y * h))));
            TopDownWindow.Position = new Point((int)(tdPos.X * w), (int)(tdPos.Y * h));

            InfoWindow = new OpalInfoWindow((int)(infoSize.X * w), (int)(infoSize.Y * h));
            InfoWindow.Position = new Point((int)(infoPos.X * w), (int)(infoPos.Y * h));

            LogWindow = new OpalLogWindow((int)(logSize.X * w), (int)(logSize.Y * h));
            LogWindow.Position = new Point((int)(logPos.X * w), (int)(logPos.Y * h));

            RegisterWindow(InfoWindow);
            InfoWindow.Show();

            RegisterWindow(LogWindow);
            Util.GlobalLogPipeline.Subscribe(LogWindow); // So that this window can receive logs from anywhere
            LogWindow.Show();

            RegisterWindow(FirstPersonWindow);
            FirstPersonWindow.Show();
            RegisterWindow(TopDownWindow);
            TopDownWindow.Show();

            Game.Player.Brain = new PlayerControlledAI(Game.Player, Program.Keys.GetPlayerKeybinds());
            (Game.Player.Brain as PlayerControlledAI).InternalMessagePipeline.Subscribe(Game);
            (Game.Player.Brain as PlayerControlledAI).BindKeys();

            // CTRL+F1: Log window toggles debug mode. If compiling a debug assembly, DBG: messages can be hidden and shown at will.
            // Under release mode, DBG: messages will not be logged at all. It is still possible to enable debug logging, but it will
            // only log debug messages for as long as debug logging is enabled, and discard anything else.
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.F1, Keybind.KeypressState.Press, "Toggle debug logging", ctrl: true), (info) => {
                LogWindow.DebugMode = !LogWindow.DebugMode;
                LogWindow.Log(
                    new ColoredString("--" + (LogWindow.DebugMode ? "Enabled " : "Disabled") + " debug logging.", 
                        Palette.Ui["DebugMessage"], 
                        Palette.Ui["DefaultBackground"]),
                    false);
            });

#if DEBUG
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.F2, Keybind.KeypressState.Press, "Debug: Toggle fog", ctrl: true), (info) => {
                SeeEverything();
                LogWindow.Log(
                    new ColoredString("--" + (Game.Player.Brain.TileMemory.IsEnabled ? "Enabled " : "Disabled") + " fog.",
                        Palette.Ui["DebugMessage"],
                        Palette.Ui["DefaultBackground"]),
                    false);
            });

            // Add suppression rules for unneeded messages when debugging.
            LogWindow.AddSuppressionRule(new Regex("FontGC: .*?"));
            LogWindow.AddSuppressionRule(new Regex("MatrixReplacement.*?"));
            LogWindow.AddSuppressionRule(new Regex("INCLUDED: .*?"));
#endif
        }

        public override void Update(GameTime gameTime)
        {
            Game.Update(gameTime.ElapsedGameTime);
            base.Update(gameTime);
        }

        private void SeeEverything()
        {
            Game.Player.Brain.TileMemory.Toggle();
        }

        public override void Draw(GameTime gameTime)
        {
            Game.Draw(gameTime.ElapsedGameTime);
            base.Draw(gameTime);
        }
    }
}
