using FieryOpal.Src.Actors;
using FieryOpal.Src.Actors.Items.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System.Linq;

namespace FieryOpal.Src.Ui.Windows
{
    public class MainGameWindowManager : WindowManager
    {
        protected OpalGameWindow FirstPersonWindow, TopDownWindow;
        protected OpalInfoWindow InfoWindow;
        protected OpalLogWindow LogWindow;

        protected OpalGame Game;

        private void CreateLayout(int w, int h, OpalGame g)
        {
            // FPVFont is smaller than the main font, so we need to multiply
            // the size of the raycast window by the correct amount as to fill
            // all the available space.
            // EDIT: FPVFont no longer exists, so this is no longer a ratio a the division is no longer required.
            Vector2 font_ratio = Nexus.Fonts.MainFont.Size.ToVector2();

            // The layout is defined in the [0, 1] range.
            Vector2 tdPos = new Vector2(0, 0f);
            Vector2 tdSize = new Vector2(.4f, .7f);

            Vector2 fpPos = new Vector2(.4f, 0) * font_ratio;
            Vector2 fpSize = new Vector2(.4f, .7f) * font_ratio;

            Vector2 infoPos = new Vector2(.8f, 0);
            Vector2 infoSize = new Vector2(.2f, .7f);

            Vector2 logPos = new Vector2(0f, .7f);
            Vector2 logSize = new Vector2(1f, .3f);

            var raycastViewport = new RaycastViewport(
                g.CurrentMap,
                new Rectangle(0, 0, (int)(fpSize.X * w), (int)(fpSize.Y * h)),
                g.Player
            );

            var topdownViewport = new LocalMapViewport(
                g.CurrentMap,
                new Rectangle(0, 0, (int)(tdSize.X * w), (int)(tdSize.Y * h))
            );

            FirstPersonWindow = new OpalGameWindow(
                (int)(fpSize.X * w), (int)(fpSize.Y * h),
                g,
                raycastViewport,
                null
            );
            FirstPersonWindow.Position = new Point((int)(fpPos.X * w), (int)(fpPos.Y * h));
            RegisterWindow(FirstPersonWindow);

            TopDownWindow = new OpalGameWindow(
                (int)(tdSize.X * w), (int)(tdSize.Y * h),
                g,
                topdownViewport
            );
            TopDownWindow.Position = new Point((int)(tdPos.X * w), (int)(tdPos.Y * h));
            RegisterWindow(TopDownWindow);

            InfoWindow = new OpalInfoWindow((int)(infoSize.X * w), (int)(infoSize.Y * h));
            InfoWindow.Position = new Point((int)(infoPos.X * w), (int)(infoPos.Y * h));
            RegisterWindow(InfoWindow);

            LogWindow = new OpalLogWindow((int)(logSize.X * w), (int)(logSize.Y * h));
            LogWindow.Position = new Point((int)(logPos.X * w), (int)(logPos.Y * h));
            LogWindow.LoadSuppressionRules(Nexus.InitInfo);
            RegisterWindow(LogWindow);
            // So that this window can receive logs from anywhere
            Util.GlobalLogPipeline.Subscribe(LogWindow);
        }

        public MainGameWindowManager(int w, int h, OpalGame g) : base(w, h)
        {

            Game = g;

            CreateLayout(w, h, g);

            PlayerControlledAI player_brain = new PlayerControlledAI(Game.Player, Nexus.Keys.GetPlayerKeybinds());
            player_brain.InternalMessagePipeline.Subscribe(Game);
            player_brain.BindKeys();
            Game.Player.Brain = player_brain;

            // CTRL+F1: Log window toggles debug mode. If compiling a debug assembly, DBG: messages can be hidden and shown at will.
            // Under release mode, DBG: messages will not be logged at all. It is still possible to enable debug logging, but it will
            // only log debug messages for as long as debug logging is enabled, and discard anything else.
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.F1, Keybind.KeypressState.Press, "Toggle debug logging", ctrl: true), (info) =>
            {
                LogWindow.DebugMode = !LogWindow.DebugMode;
                LogWindow.Log(
                    ("--" + (LogWindow.DebugMode ? "Enabled " : "Disabled") + " debug logging.").ToColoredString(Palette.Ui["DebugMessage"]),
                    false
                );
            });

            InfoWindow.Show();
            FirstPersonWindow.Show();
            TopDownWindow.Show();
            LogWindow.Show();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (Game.Player != null)
            {
                TopDownWindow.Viewport.ViewArea = 
                FirstPersonWindow.Viewport.ViewArea = 
                    new Rectangle(
                        Game.Player.LocalPosition.X - TopDownWindow.Width / 2,
                        Game.Player.LocalPosition.Y - TopDownWindow.Height / 2,
                        TopDownWindow.Width,
                        TopDownWindow.Height
                    );
            }

            bool wasDirty = (FirstPersonWindow.Viewport as RaycastViewport)?.Dirty ?? false;
            if(wasDirty)
            {
                FirstPersonWindow.Viewport.Print(FirstPersonWindow, new Rectangle(new Point(0, 0), new Point(FirstPersonWindow.Width, FirstPersonWindow.Height)), Game.Player.Brain.TileMemory);
                TopDownWindow.Viewport.Print(TopDownWindow, new Rectangle(new Point(0, 0), new Point(TopDownWindow.Width, TopDownWindow.Height)), Game.Player.Brain.TileMemory);
            }

            var rc = FirstPersonWindow.Viewport as RaycastViewport;
            Global.DrawCalls.Add(new DrawCallTexture(rc.RenderSurface, FirstPersonWindow.Position.ToVector2()));
            var weaps = Game.Player.Equipment.GetContents().Where(i => i is Weapon).Select(i => i as Weapon);
            foreach (var weapon in weaps)
            {
                // TODO draw pixelwise
                var tex = weapon.ViewGraphics.AsTexture2D(rc.RenderSurface.Width);
                Global.DrawCalls.Add(new DrawCallTexture(tex, FirstPersonWindow.Position.ToVector2() + new Vector2(rc.RenderSurface.Width / 2 - tex.Width / 2 + weapon.ViewGraphics.Offset.X * tex.Width / 2, rc.RenderSurface.Height - tex.Height + weapon.ViewGraphics.Offset.Y * tex.Height / 2)));
            }

            Game.Draw(gameTime.ElapsedGameTime);
            base.Draw(gameTime);
        }
    }
}
