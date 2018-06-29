using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SadConsole;
using System;

namespace FieryOpal.Src.Ui.Windows
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
            var rcv = (Viewport as RaycastViewport);
            switch (performed_action)
            {
                case "RequestInfo": // Forward RequestInfo messages to any connected OpalGames. Pass pipeline handle as original sender to enable broadcast on the other end.
                    InternalMessagePipeline.BroadcastForward<OpalConsoleWindow>(pipeline_handle, sender_handle, new Func<OpalGame, string>(g => { return performed_action; }));
                    break;
                case "ToggleRaycastLabelView":
                    rcv?.ToggleLabels();
                    rcv?.FlagForRedraw();
                    if (rcv != null)
                    {
                        Util.LogBadge("FPV:",
                            "Named actor labels {0}".Fmt(rcv.DrawActorLabels ? "ON" : "OFF"),
                            false,
                            rcv.DrawActorLabels ? Palette.Ui["InfoMessage"] : Palette.Ui["ErrorMessage"]
                        );
                    }
                    break;
                case "FlagRaycastViewportForRedraw":
                case "UpdateRaycastWindowRotation":
                    rcv?.FlagForRedraw();
                    break;
                case "ToggleActorBoundaryBoxes":
                    rcv?.ToggleActorBoundaryBoxes();
                    rcv?.FlagForRedraw();
                    if(rcv != null)
                    {
                        Util.LogBadge("FPV:",
                            "Actor boundary boxes {0}".Fmt(rcv.DrawActorBoundaryBoxes ? "ON" : "OFF"),
                            false,
                            rcv.DrawActorBoundaryBoxes ? Palette.Ui["InfoMessage"] : Palette.Ui["ErrorMessage"]
                        );
                    }
                    break;
                case "ToggleTerrainGrid":
                    rcv?.ToggleTerrainGrid();
                    rcv?.FlagForRedraw();
                    if (rcv != null)
                    {
                        Util.LogBadge("FPV:",
                            "Terrain grid {0}".Fmt(rcv.DrawTerrainGrid ? "ON" : "OFF"),
                            false,
                            rcv.DrawTerrainGrid ? Palette.Ui["InfoMessage"] : Palette.Ui["ErrorMessage"]
                        );
                    }
                    break;
                case "ToggleAmbientShading":
                    rcv?.ToggleAmbientShading();
                    rcv?.FlagForRedraw();
                    if (rcv != null)
                    {
                        Util.LogBadge("FPV:",
                            "Ambient shading {0}".Fmt(rcv.DrawAmbientShading ? "ON" : "OFF"),
                            false,
                            rcv.DrawAmbientShading ? Palette.Ui["InfoMessage"] : Palette.Ui["ErrorMessage"]
                        );
                    }
                    break;
                case "ToggleLighting":
                    rcv?.FlagForRedraw();
                    if (rcv != null)
                    {
                        Game.CurrentMap.Lighting.ToggleEnabled();
                        Util.LogBadge("FPV:",
                            "Lighting system {0}".Fmt(Game.CurrentMap.Lighting.Enabled ? "ON" : "OFF"),
                            false,
                            Game.CurrentMap.Lighting.Enabled ? Palette.Ui["InfoMessage"] : Palette.Ui["ErrorMessage"]
                        );
                    }
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
            if(!(Viewport is RaycastViewport)) base.Draw(delta);
        }
    }

}
