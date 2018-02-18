using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Linq;

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

            FirstPersonWindow = new OpalGameWindow(w, h * 2 - h / 2, g, new RaycastViewport(g.CurrentMap, new Rectangle(0, 0, w - w / 4, h - h / 4), g.Player, Program.HDFont), Program.FPFont);

            TopDownWindow = new OpalGameWindow((w) / 2, h - h / 4, g, new Viewport(g.CurrentMap, new Rectangle(0, 0, w - w / 4, h - h / 4)));
            TopDownWindow.Position = new Point((w) / 2, 0);

            InfoWindow = new OpalInfoWindow(w / 4, h - h / 4);
            InfoWindow.Position = new Point(w - w / 4, 0);

            LogWindow = new OpalLogWindow(w, h / 4);
            LogWindow.Position = new Point(0, h - h / 4);

            //RegisterWindow(InfoWindow);

            RegisterWindow(LogWindow);
            Util.GlobalLogPipeline.Subscribe(LogWindow); // So that this window can receive logs from anywhere

            RegisterWindow(FirstPersonWindow);
            RegisterWindow(TopDownWindow);

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.W, Keybind.KeypressState.Press), (info) => { MovePlayer(0, -1); });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.A, Keybind.KeypressState.Press), (info) => { MovePlayer(-1, 0); });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.S, Keybind.KeypressState.Press), (info) => { MovePlayer(0, 1); });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.D, Keybind.KeypressState.Press), (info) => { MovePlayer(1, 0); });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Q, Keybind.KeypressState.Press), (info) => { RotateFirstPersonViewport((float)Math.PI / 4); });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.E, Keybind.KeypressState.Press), (info) => { RotateFirstPersonViewport(-(float)Math.PI / 4); });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.R, Keybind.KeypressState.Press), (info) => { RegenMap(); });

            // CTRL+F1: Log window toggles debug mode. If compiling a debug assembly, DBG: messages can be hidden and shown at will.
            // Under release mode, DBG: messages will not be logged at all. It is still possible to enable debug logging, but it will
            // only log debug messages for as long as debug logging is enabled, and discard anything else.
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.F1, Keybind.KeypressState.Press, ctrl: true), (info) => {
                LogWindow.DebugMode = !LogWindow.DebugMode;
                LogWindow.Log(new ColoredString("--" + (LogWindow.DebugMode ? "Enabled " : "Disabled") + " debug logging.", Color.Gold, Color.Black), false);
            });

#if DEBUG
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.F, Keybind.KeypressState.Press), (info) => { DestroyWhateverLiesInFrontOfPlayer(); });
#endif
        }

        public override void Update(GameTime gameTime)
        {
            Game.Update(gameTime.ElapsedGameTime);
            base.Update(gameTime);
        }

        private void RegenMap()
        {
            Game.CurrentMap.Actors.Clear();
            Game.CurrentMap.Generate(new BasicTerrainGenerator(openness: 0.92f), new BasicTerrainDecorator());
            LogWindow.Log(new ColoredString("Map successfully generated."), true);
            Game.Player.ChangeLocalMap(Game.CurrentMap, Game.CurrentMap.FirstAccessibleTileAround(Game.Player.LocalPosition));

            var fp_view = (RaycastViewport)FirstPersonWindow.Viewport;
            fp_view.Dirty = true;
        }

        private void DestroyWhateverLiesInFrontOfPlayer()
        {
            var fp_view = (RaycastViewport)FirstPersonWindow.Viewport;
            var pos = Game.Player.LocalPosition + Util.NormalizedStep(fp_view.DirectionVector);
            var tile_in_front = Game.CurrentMap.TileAt(pos.X, pos.Y);
            if (tile_in_front != null && tile_in_front.Properties.BlocksMovement)
            {
                Game.CurrentMap.SetTile(pos.X, pos.Y, OpalTile.DebugGround);
            }
            var actors_in_front = Game.CurrentMap.ActorsAt(pos.X, pos.Y).ToList();
            foreach (var act in actors_in_front)
            {
                if (!(act is Plant)) continue;
                (act as Plant).Kill();
            }
            fp_view.Dirty = true;
        }

        /// <summary>
        /// Moves the player relative to where they are looking. Both parameters should be either 0, 1 or -1 and assume 0° rotation, i.e. (0, -1) is upwards.
        /// </summary>
        private void MovePlayer(int x, int y)
        {
            var fp_view = (RaycastViewport)FirstPersonWindow.Viewport;
            fp_view.Dirty = true;
            if (x == 0 && y == -1) Game.Player.Move(Util.NormalizedStep(fp_view.DirectionVector));
            else if (x == -1 && y == 0) Game.Player.Move(Util.NormalizedStep(-fp_view.PlaneVector));
            else if (x == 0 && y == 1) Game.Player.Move(Util.NormalizedStep(-fp_view.DirectionVector));
            else if (x == 1 && y == 0) Game.Player.Move(Util.NormalizedStep(fp_view.PlaneVector));
        }

        /// <summary>
        /// Rotates the FP viewport by a given angle in radians. +/-PI / 4 allows for eight-directional movement.
        /// </summary>
        private void RotateFirstPersonViewport(float angle)
        {
            var fp_view = (RaycastViewport)FirstPersonWindow.Viewport;
            fp_view.Rotate(angle);
        }

        public override void Draw(GameTime gameTime)
        {
            Game.Draw(gameTime.ElapsedGameTime);
            base.Draw(gameTime);
        }
    }
}
