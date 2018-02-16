using SadConsole;
using System;
using System.Collections.Generic;

namespace FieryOpal.src.ui
{
    public class OpalGameWindow : OpalConsoleWindow
    {
        public MessagePipeline<OpalGame> InternalMessagePipeline { get; protected set; }
        protected List<WindowManager> ConnectedWindowManagers = new List<WindowManager>();
        public OpalGame Game { get; protected set; }
        public Viewport Viewport { get; set; }

        public OpalGameWindow(int w, int h, OpalGame g, Viewport v, Font f = null) : base(w, h, "Fiery Opal", f)
        {
            Game = g;
            InternalMessagePipeline = new MessagePipeline<OpalGame>();
            InternalMessagePipeline.Subscribe(g);
            g.InternalMessagePipeline.Subscribe(this);
            Viewport = v;
        }

        public override void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalConsoleWindow, string> action, bool is_broadcast)
        {
            if (sender_handle == Handle) return;

            string performed_action = action(this);
            switch (performed_action)
            {
                case "RequestInfo": // Forward RequestInfo messages to any connected OpalGames. Pass pipeline handle as original sender to enable broadcast on the other end.
                    InternalMessagePipeline.BroadcastForward<OpalConsoleWindow>(pipeline_handle, sender_handle, new Func<OpalGame, string>(g => { return performed_action; }));
                    break;
                default:
                    break;
            }
        }

        public override void Update(TimeSpan delta)
        {
            base.Update(delta);
        }

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
        }

        public override void OnWindowManagerRegistration(WindowManager wm)
        {
            ConnectedWindowManagers.Add(wm);
        }

        public override void OnWindowManagerUnregistration(WindowManager wm)
        {
            ConnectedWindowManagers.Remove(wm);
        }

        public void Log(ColoredString msg, bool debug)
        {
            ConnectedWindowManagers.ForEach(wm => wm.InternalMessagePipeline.BroadcastLogMessage(this, msg, debug));
        }
    }

}
