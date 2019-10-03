using FieryOpal.Src.Actors;
using FieryOpal.Src.Actors.Items.Weapons;
using FieryOpal.Src.Ui.Dialogs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Linq;
using System.Threading;

namespace FieryOpal.Src.Ui.Windows
{
    public class GameWindowManager : WindowManager
    {
        public OpalGameWindow FirstPersonWindow, TopDownWindow;
        public OpalInfoWindow InfoWindow;
        public OpalLogWindow LogWindow;

        private FakeTitleBar TitleBar;

        protected OpalGame Game;

        private void CreateLayout(int w, int h, OpalGame g)
        {
            // FPVFont is smaller than the main font, so we need to multiply
            // the size of the raycast window by the correct amount as to fill
            // all the available space.
            // EDIT: FPVFont no longer exists, so this is no longer a ratio a the division is no longer required.
            // EDIT2: Now I done fucked up again and since the font on which size is based is the "Books" font (12x12),
            // the main font ends up being smaller (10x10). So we need a ratio again in order to fill up the missing space.
            Vector2 dfSz = new Vector2(Nexus.InitInfo.DefaultFontWidth, Nexus.InitInfo.DefaultFontHeight);
            Vector2 font_ratio = dfSz / Nexus.Fonts.MainFont.Size.ToVector2();

            float aspect_ratio = w / (float)h;

            // The layout is defined in the [0, 1] range.
            Vector2 tdPos = new Vector2(0, 0f);
            Vector2 tdSize = new Vector2(.5f, .5f * aspect_ratio) * font_ratio;

            Vector2 fpPos = new Vector2(.5f, 0) * dfSz;
            Vector2 fpSize = new Vector2(.5f, .5f * aspect_ratio) * dfSz;

            Vector2 infoPos = new Vector2(.75f, .5f * aspect_ratio) * font_ratio;
            Vector2 infoSize = new Vector2(.25f, 1 - .5f * aspect_ratio) * font_ratio;

            Vector2 logPos = new Vector2(0f, .5f * aspect_ratio) * font_ratio;
            Vector2 logSize = new Vector2(.75f, 1 - .5f * aspect_ratio) * font_ratio;

            h = h - 1;
            var raycastViewport = new RaycastViewport(
                g.CurrentMap,
                new Rectangle(0, 0, (int)(fpSize.X * w), (int)(fpSize.Y * h)),
                g.Player
            );

            var topdownViewport = new LocalMapViewport(
                g.CurrentMap,
                new Rectangle(0, 0, (int)(tdSize.X * w), (int)(tdSize.Y * h))
            );

            int parity = (int)(tdSize.X * w + 1) % 2;

            FirstPersonWindow = new OpalGameWindow(
                (int)(fpSize.X * w) + (int)(parity * dfSz.X), (int)(fpSize.Y * (h)),
                g,
                raycastViewport
            );
            FirstPersonWindow.Position = new Point((int)(fpPos.X * w) - (int)(parity * dfSz.X), 2 * (int)dfSz.Y + (int)(fpPos.Y * h));
            RegisterWindow(FirstPersonWindow);

            TopDownWindow = new OpalGameWindow(
                (int)(tdSize.X * w) - parity, (int)(tdSize.Y * h),
                g,
                topdownViewport
            );
            TopDownWindow.Position = new Point((int)(tdPos.X * w), 2 + (int)(tdPos.Y * h));
            RegisterWindow(TopDownWindow);

            InfoWindow = new OpalInfoWindow((int)(infoSize.X * w), (int)(infoSize.Y * h));
            InfoWindow.Position = new Point((int)(infoPos.X * w), 2 + (int)(infoPos.Y * h));
            RegisterWindow(InfoWindow);

            LogWindow = new OpalLogWindow((int)(logSize.X * w), (int)(logSize.Y * h));
            LogWindow.Position = new Point((int)(logPos.X * w), 2 + (int)(logPos.Y * h));
            LogWindow.LoadSuppressionRules(Nexus.InitInfo);
            RegisterWindow(LogWindow);

            TitleBar = new FakeTitleBar(w, "./FieryOpal", Nexus.Fonts.Spritesheets["Books"]);
            RegisterWindow(TitleBar);
            // So that this window can receive logs from anywhere
            Util.GlobalLogPipeline.Subscribe(LogWindow);

            Nexus.DialogRect = new Rectangle(
                0, 1,
                Width + 2, 
                (int)((tdSize.Y * h) / font_ratio.Y + 3)
            );
        }

        public GameWindowManager(int w, int h, OpalGame g) : base(w, h)
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
        }

        public override void Show()
        {
            base.Show();

            InfoWindow.Show();
            FirstPersonWindow.Show();
            TopDownWindow.Show();
            LogWindow.Show();
            TitleBar.Show();
        }

        public override void Hide()
        {
            base.Hide();

            InfoWindow.Hide();
            FirstPersonWindow.Hide();
            TopDownWindow.Hide();
            LogWindow.Hide();
            TitleBar.Hide();
        }

        private string lastDialogTitle = "./FieryOpal";
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            string capt = (OpalDialog.ActiveDialog?.Caption ?? "");
            if (capt.Length > 0) capt = "/" + capt;

            if (capt != lastDialogTitle)
            {
                TitleBar.UpdateCaption("./FieryOpal{0}".Fmt(capt));
            }
        }


        public override void Draw(GameTime time)
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
            if (wasDirty)
            {
                FirstPersonWindow.Viewport.Print(FirstPersonWindow, new Rectangle(new Point(0, 0), new Point(FirstPersonWindow.Width, FirstPersonWindow.Height)), Game.Player.Brain.TileMemory);
                TopDownWindow.Viewport.Print(TopDownWindow, new Rectangle(new Point(0, 0), new Point(TopDownWindow.Width, TopDownWindow.Height)), Game.Player.Brain.TileMemory);
            }

            var rc = FirstPersonWindow.Viewport as RaycastViewport;
            Global.DrawCalls.Add(new DrawCallCustom(() =>
            {
                Global.SpriteBatch.End();
                ShaderManager.LightingShader.Parameters["LightMap"].SetValue(Game.CurrentMap.Lighting.LightMap);
                ShaderManager.LightingShader.Parameters["Projection"].SetValue(rc.ProjectionTexture);
                ShaderManager.LightingShader.Parameters["ViewDistance"].SetValue(Game.Player.ViewDistance / 64f);
                ShaderManager.LightingShader.Parameters["AmbientLightIntensity"].SetValue(Game.CurrentMap.AmbientLightIntensity);
                ShaderManager.LightingShader.Parameters["PlayerPosition"].SetValue((Game.Player.LocalPosition.ToVector2() / new Vector2(Game.CurrentMap.Width, Game.CurrentMap.Width)));
                ShaderManager.LightingShader.Parameters["SkyColor"].SetValue(Game.CurrentMap.Indoors ? Color.Black.ToVector4() : Nexus.DayNightCycle.GetSkyColor(.5f).ToVector4());
                Global.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, null, RasterizerState.CullNone, ShaderManager.LightingShader);
                Global.SpriteBatch.Draw(
                    rc.RenderSurface,
                    new Rectangle(FirstPersonWindow.Position, new Point(FirstPersonWindow.Width - 2, FirstPersonWindow.Height)),
                    null,
                    Color.White
                );
                Global.SpriteBatch.End();
                Global.SpriteBatch.Begin(SpriteSortMode.Immediate);
            }));

            var weaps = Game.Player.Equipment.GetContents().Where(i => i is Weapon).Select(i => i as Weapon);
            foreach (var weapon in weaps)
            {
                // TODO draw pixelwise
                var tex = weapon.ViewGraphics.AsTexture2D(rc.RenderSurface.Width);
                Global.DrawCalls.Add(new DrawCallCustom(() =>
                {
                    var ofs = new Vector2(
                        rc.RenderSurface.Width / 2 - tex.Width / 2 + weapon.ViewGraphics.Offset.X * tex.Width / 2, 
                        rc.RenderSurface.Height - tex.Height + weapon.ViewGraphics.Offset.Y * tex.Height / 2
                    );

                    Global.SpriteBatch.Draw(
                        tex,
                        FirstPersonWindow.Position.ToVector2() + ofs,
                        Game.Player.Map.Lighting.ApplyShading(Color.White, Game.Player.LocalPosition)
                    );
                }));
            }
            Game.Draw(time.ElapsedGameTime);
            base.Draw(time);
        }
    }
}
