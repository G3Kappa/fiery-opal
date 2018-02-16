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
            public string PlayerName;
            public string PlayerTitle;

            public int PlayerLevel;

            public int PlayerHp;
            public int PlayerMaxHp;

            public Point PlayerLocalPosition;
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
            Print(0, 0, String.Format("{0}, Level {1} {2}", info.PlayerName, info.PlayerLevel, info.PlayerTitle));

            var healthbar = MakeHealthbar(info.PlayerHp, info.PlayerMaxHp, (int)(Width / 1.5f), new Cell(Color.Red, Color.Black), new Cell(Color.Yellow, Color.Black), new Cell(Color.Green, Color.Black));
            Print(1, 3, healthbar);
            Print(healthbar.Count + 2, 3, new ColoredString(String.Format("({0}/{1})", info.PlayerHp, info.PlayerMaxHp), new Cell(Color.White, Color.Black)));

            var localpos_str = info.PlayerLocalPosition.ToString();
            Print(1, 4, new ColoredString(localpos_str.Substring(1, localpos_str.Length - 2), new Cell(Color.Gray, Color.Black)));


            //ConnectedWindowManagers.ForEach(wm => wm.InternalMessagePipeline.BroadcastLogMessage(this, new ColoredString("ReceiveInfoUpdateFromGame"), true));
        }
    }
}
