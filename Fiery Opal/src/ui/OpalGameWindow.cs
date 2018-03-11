using FieryOpal.src.procgen;
using SadConsole;
using System;
using System.Collections.Generic;

namespace FieryOpal.src.ui
{
    public class OpalGameWindow : OpalConsoleWindow
    {
        public MessagePipeline<OpalGame> InternalMessagePipeline { get; protected set; }
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
                case "FlagRaycastViewportForRedraw":
                    if (!(Viewport is RaycastViewport)) break;
                    (Viewport as RaycastViewport).FlagForRedraw();
                    Invalidate();
                    break;
                case "UpdateRaycastWindowRotation":
                    if (!(Viewport is RaycastViewport)) break;
                    var v = (Viewport as RaycastViewport);
                    v.DirectionVector = Game.Player.LookingAt;
                    v.PlaneVector = Game.Player.LookingAt.Orthogonal();
                    v.FlagForRedraw();
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
    }

}
