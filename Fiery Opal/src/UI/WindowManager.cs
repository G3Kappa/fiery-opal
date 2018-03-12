using Microsoft.Xna.Framework;
using System;

namespace FieryOpal.Src.Ui
{
    public class WindowManager
    {
        public MessagePipeline<OpalConsoleWindow> InternalMessagePipeline { get; protected set; }

        public int Width { get; }
        public int Height { get; }

        public virtual void Update(GameTime gameTime) {
            HandleInput();
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

        public virtual void HandleInput() { }

        public WindowManager(int w, int h)
        {
            Width = w;
            Height = h;

            InternalMessagePipeline = new MessagePipeline<OpalConsoleWindow>();
        }
    }
}
