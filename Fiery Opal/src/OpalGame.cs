using FieryOpal.src.actors;
using FieryOpal.src.ui;
using Microsoft.Xna.Framework;
using System;

namespace FieryOpal.src
{
    // Here the pipeline is used to control the game remotely and to let it propagate messages to the parent window(s).
    public class OpalGame : IPipelineSubscriber<OpalGame>
    {
        public MessagePipeline<OpalConsoleWindow> InternalMessagePipeline { get; protected set; }
        public Guid Handle { get; }

        public TurnTakingActor Player = new Humanoid();
        public OpalLocalMap CurrentMap { get; set; }

        public TurnManager TurnManager { get; private set; }

        public OpalGame(OpalLocalMap startingMap)
        {
            Handle = Guid.NewGuid();
            CurrentMap = startingMap;

            Player.ChangeLocalMap(CurrentMap, new Point(CurrentMap.Width / 2, CurrentMap.Height / 2));
            CurrentMap.AddActor(Player);
            InternalMessagePipeline = new MessagePipeline<OpalConsoleWindow>();
            TurnManager = new TurnManager();
        }

        public virtual void Update(TimeSpan delta)
        {
            CurrentMap.Update(delta);
        }

        public virtual void Draw(TimeSpan delta)
        {
            // Tell any subscribed OpalGameWindow to render the viewport.
            InternalMessagePipeline.Broadcast(null, new Func<OpalConsoleWindow, string>(
                cw => 
                {
                    OpalGameWindow w = cw as OpalGameWindow;
                    if (Player != null)
                    {
                        w.Viewport.ViewArea = new Rectangle(Player.LocalPosition.X - w.Width / 2,
                                                            Player.LocalPosition.Y - w.Height / 2, 
                                                            w.Width, 
                                                            w.Height);
                    }
                    w.Viewport.Print(w, new Rectangle(new Point(0, 0), new Point(w.Width, w.Height)));
                    return "ViewportRefresh";
                }
                ));
        }
        public void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalGame, string> msg, bool is_broadcast)
        {
            string performed_action = msg(this);
            switch(performed_action)
            {
                case "RequestInfo": // Is a FWD from an OpalInfoWindow connected to a MessagePipeline<OpalConsoleWindow>
                    var info_pipeline = MessagePipeline<OpalConsoleWindow>.GetPipeline(pipeline_handle);
                    // Using a Forward in order to pass our Handle despite not being an OpalConsoleWindow
                    info_pipeline.Forward<OpalConsoleWindow>(pipeline_handle, Handle, sender_handle, new Func<OpalConsoleWindow, string>(
                        w => 
                        {
                            OpalInfoWindow info_window = (OpalInfoWindow)w;
                            OpalInfoWindow.GameInfo info = new OpalInfoWindow.GameInfo
                            {
                                Player = Player,
                                CurrentTurnTime = TurnManager.CurrentTime
                            };

                            info_window.ReceiveInfoUpdateFromGame(Handle, ref info);
                            return "ServeInfo";
                        }
                        ));
                    break;
                case "MapRefreshed":
                    TurnManager.ResetAccumulator();
                    break;
                case "PlayerInputHandled":
                    TurnManager.BeginTurn(CurrentMap, Player.Handle);
                    break;
                default:
                    break;
            }
        }
    }
}
