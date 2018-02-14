using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.UI
{
    public class WindowManager
    {
        public MessagePipeline<OpalConsoleWindow> InternalMessagePipeline { get; protected set; }

        public int Width { get; }
        public int Height { get; }

        public virtual void Update(GameTime gameTime) {
            InternalMessagePipeline.Broadcast(null, new Func<OpalConsoleWindow, string>(w => { w.Update(gameTime.ElapsedGameTime); return "Update"; }));
        }
        public virtual void Draw(GameTime gameTime) {
            InternalMessagePipeline.Broadcast(null, new Func<OpalConsoleWindow, string>(w => { w.Draw(gameTime.ElapsedGameTime); return "Draw"; }));
        }

        public bool RegisterWindow(OpalConsoleWindow w)
        {
            bool ret = InternalMessagePipeline.Subscribe(w);
            if (!ret) return false;
            InternalMessagePipeline.Unicast(null, w.Handle, new Func<OpalConsoleWindow, string>(ocw => { ocw.OnWindowManagerRegistration(this); return "OnWindowManagerRegistration"; }));
            return true;
        }

        public bool UnregisterWindow(OpalConsoleWindow w)
        {
            bool ret = InternalMessagePipeline.Unsubscribe(w);
            if (!ret) return false;
            InternalMessagePipeline.Unicast(null, w.Handle, new Func<OpalConsoleWindow, string>(ocw => { ocw.OnWindowManagerUnregistration(this); return "OnWindowManagerUnregistration"; }));
            return true;
        }

        public WindowManager(int w, int h)
        {
            Width = w;
            Height = h;

            InternalMessagePipeline = new MessagePipeline<OpalConsoleWindow>();
        }
    }

    public class MainGameWindowManager : WindowManager
    {
        protected OpalConsoleWindow GameWindow, GameWindow2, InfoWindow, LogWindow;

        public MainGameWindowManager(int w, int h) : base(w, h)
        {
            OpalLocalMap map = new OpalLocalMap(w * 2, h * 2);
            OpalGame g = new OpalGame(new Viewport(map, new Rectangle(0, 0, w - w / 4, h - h / 4)));

            GameWindow = new OpalGameWindow((w - w / 4) / 2, h - h / 4, g);
            GameWindow2 = new OpalGameWindow((w - w / 4) / 2, h - h / 4, g, new Viewport(map, new Rectangle(0, 0, w - w / 4, h - h / 4)));
            GameWindow2.Position = new Point((w - w / 4) / 2, 0);

            InfoWindow = new OpalInfoWindow(w / 4, h - h / 4);
            InfoWindow.Position = new Point(w - w / 4, 0);

            LogWindow = new OpalLogWindow(w, h / 4);
            LogWindow.Position = new Point(0, h - h / 4);

            RegisterWindow(InfoWindow);
            RegisterWindow(LogWindow);
            RegisterWindow(GameWindow);
            RegisterWindow(GameWindow2);

            map.Generate(new BasicTerrainGenerator());
            ((OpalLogWindow)LogWindow).Log(new ColoredString("Map successfully generated."), true);

        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            var game_window = (OpalGameWindow)GameWindow;
            var fp_view = ((RaycastViewport)(game_window.Game.Viewport));

            if (SadConsole.Global.KeyboardState.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.D))
            {
                fp_view.Rotate((float)Math.PI / 10);
            }
            if (SadConsole.Global.KeyboardState.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.A))
            {
                fp_view.Rotate(-(float)Math.PI / 10);
            }
            if (SadConsole.Global.KeyboardState.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Up))
            {
                game_window.Game.Player.Move(new Point(0, -1));
            }
            if (SadConsole.Global.KeyboardState.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Down))
            {
                game_window.Game.Player.Move(new Point(0, 1));
            }
            if (SadConsole.Global.KeyboardState.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Left))
            {
                game_window.Game.Player.Move(new Point(-1, 0));
            }
            if (SadConsole.Global.KeyboardState.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Right))
            {
                game_window.Game.Player.Move(new Point(1, 0));
            }
        }
    }
}
