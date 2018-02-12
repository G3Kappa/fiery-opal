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
        protected OpalConsoleWindow GameWindow, InfoWindow, LogWindow;

        public MainGameWindowManager(int w, int h) : base(w, h)
        {
            GameWindow = new OpalGameWindow(w - w / 4, h - h / 4, new OpalGame());

            InfoWindow = new OpalConsoleWindow(w / 4, h - h / 4, "Info");
            InfoWindow.Position = new Point(w - w / 4, 0);

            LogWindow = new OpalLogWindow(w, h / 4);
            LogWindow.Position = new Point(0, h - h / 4);

            RegisterWindow(InfoWindow);
            RegisterWindow(LogWindow);
            RegisterWindow(GameWindow);
        }
    }
}
