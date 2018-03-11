using FieryOpal.src.actors;
using FieryOpal.src.procgen;
using FieryOpal.src.procgen.Terrain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace FieryOpal.src.ui
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
            Game.Player.Brain = new PlayerControlledAI(Game.Player, Util.LoadDefaultKeyconfig());
            (Game.Player.Brain as PlayerControlledAI).InternalMessagePipeline.Subscribe(Game);
            (Game.Player.Brain as PlayerControlledAI).BindKeys();

            FirstPersonWindow = new OpalGameWindow(w, h * 2 - h / 2, g, new RaycastViewport(g.CurrentMap, new Rectangle(0, 0, w - w / 4, h - h / 4), g.Player, Program.HDFont), Program.FPFont);

            TopDownWindow = new OpalGameWindow((w) / 3, h - h / 4, g, new LocalMapViewport(g.CurrentMap, new Rectangle(0, 0, w - w / 4, h - h / 4)));
            TopDownWindow.Position = new Point((w) / 2, 0);

            InfoWindow = new OpalInfoWindow(w / 6, h - h / 4);
            InfoWindow.Position = new Point(w / 2 + w / 3, 0);

            LogWindow = new OpalLogWindow(w, h / 4);
            LogWindow.Position = new Point(0, h - h / 4);

            RegisterWindow(InfoWindow);
            InfoWindow.Show();

            RegisterWindow(LogWindow);
            Util.GlobalLogPipeline.Subscribe(LogWindow); // So that this window can receive logs from anywhere
            LogWindow.Show();

            RegisterWindow(FirstPersonWindow);
            FirstPersonWindow.Show();
            RegisterWindow(TopDownWindow);
            TopDownWindow.Show();

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
                    new ColoredString("--" + (Game.CurrentMap.Fog.IsEnabled ? "Enabled " : "Disabled") + " fog.",
                        Palette.Ui["DebugMessage"],
                        Palette.Ui["DefaultBackground"]),
                    false);
            });

            // Add suppression rules for unneeded messages when debugging.
            LogWindow.AddSuppressionRule(new Regex("FontGC: .*?"));
            LogWindow.AddSuppressionRule(new Regex("MatrixReplacement.*?"));
#endif
        }

        public override void Update(GameTime gameTime)
        {
            Game.Update(gameTime.ElapsedGameTime);
            base.Update(gameTime);
        }

        private void SeeEverything()
        {
            Game.CurrentMap.Fog.Toggle();
        }

        public override void Draw(GameTime gameTime)
        {
            Game.Draw(gameTime.ElapsedGameTime);
            base.Draw(gameTime);
        }
    }
}
