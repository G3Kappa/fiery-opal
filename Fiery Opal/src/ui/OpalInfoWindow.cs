using FieryOpal.src.actors;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;

namespace FieryOpal.src.ui
{

    public class OpalInfoWindow : OpalConsoleWindow
    {
        public struct GameInfo
        {
            public TurnTakingActor Player;
            public float CurrentTurnTime;
        }

        protected List<WindowManager> ConnectedWindowManagers = new List<WindowManager>();

        public OpalInfoWindow(int w, int h, Font f = null) : base(w, h, "Info", f)
        {
        }

        public override void Update(TimeSpan delta)
        {
            RequestInfo();
        }

        public override void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalConsoleWindow, string> action, bool is_broadcast)
        {
            if (sender_handle == Handle) return;
            string performed_action = action.Invoke(this);
        }

        public override void OnWindowManagerRegistration(WindowManager wm)
        {
            ConnectedWindowManagers.Add(wm);
        }

        public override void OnWindowManagerUnregistration(WindowManager wm)
        {
            ConnectedWindowManagers.Remove(wm);
        }

        public void RequestInfo()
        {
            ConnectedWindowManagers.ForEach(wm => wm.InternalMessagePipeline.Broadcast(this, new Func<OpalConsoleWindow, string>(
                w =>
                {
                    return "RequestInfo";
                }
                )));
        }

        protected ColoredString MakeHealthbar(int hp, int maxhp, int width, params Cell[] colors)
        {
            if (width < 8)
            {
                throw new ArgumentException("Width must be >= 8.");
            }

            int color_index = Math.Min(colors.Length - 1, Math.Max(0, (int)(Math.Min(hp, maxhp) / (Math.Max(maxhp, colors.Length) / (float)colors.Length))));
            float health_percentage = (float)hp / maxhp;

            ColoredGlyph[] barglyphs = new ColoredGlyph[width];
            barglyphs[0] = new ColoredGlyph(new Cell(Color.Gray, Color.Black, 'H'));
            barglyphs[1] = new ColoredGlyph(new Cell(Color.Gray, Color.Black, 'P'));
            barglyphs[2] = new ColoredGlyph(new Cell(Color.Gray, Color.Black, ':'));
            barglyphs[3] = new ColoredGlyph(new Cell(Color.Gray, Color.Black, ' '));
            barglyphs[4] = new ColoredGlyph(new Cell(Color.White, Color.Black, '['));
            barglyphs[width - 1] = new ColoredGlyph(new Cell(Color.White, Color.Black, ']'));
            for (int i = 5; i < width - 1; ++i)
            {
                Cell color = colors[color_index];

                if ((float)(i - 5) / (width - 6) <= health_percentage && health_percentage > 0)
                {
                    barglyphs[i] = new ColoredGlyph(color);
                    barglyphs[i].GlyphCharacter = '=';
                }
                else
                {
                    barglyphs[i] = new ColoredGlyph(new Cell(Color.Gray, Color.Black));
                    barglyphs[i].GlyphCharacter = '.';
                }

            }

            return new ColoredString(barglyphs);
        }

        public void ReceiveInfoUpdateFromGame(Guid game_handle, ref GameInfo info)
        {
            Clear();
            Print(0, 0, String.Format("T{0}: {1:0.00}", (char)255, info.CurrentTurnTime));
            Print(0, 1, String.Format("XY: ({0}, {1})", info.Player.LocalPosition.X, info.Player.LocalPosition.Y));
        }
    }
}
