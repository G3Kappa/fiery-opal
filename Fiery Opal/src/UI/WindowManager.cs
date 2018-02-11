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
        protected MessagePipeline<OpalConsoleWindow> InternalMessagePipeline;

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
            return InternalMessagePipeline.Subscribe(w);
        }

        public bool UnregisterWindow(OpalConsoleWindow w)
        {
            return InternalMessagePipeline.Unsubscribe(w);
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
            GameWindow = new OpalConsoleWindow(w - w / 4, h - h / 4, "Fiery Opal");

            InfoWindow = new OpalConsoleWindow(w / 4, h - h / 4, "Info");
            InfoWindow.Position = new Point(w - w / 4, 0);

            LogWindow = new OpalConsoleWindow(w, h / 4, "Log");
            LogWindow.Position = new Point(0, h - h / 4);

            RegisterWindow(GameWindow);
            RegisterWindow(InfoWindow);
            RegisterWindow(LogWindow);


            InternalMessagePipeline.Broadcast(null, new Func<OpalConsoleWindow, string>(ocw => { ocw.OnWindowManagerRegistration(this); return "OnWindowManagerRegistration"; }));
        }
    }
}
