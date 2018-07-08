using FieryOpal.Src.Actors;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;

namespace FieryOpal.Src.Ui.Windows
{

    public class OpalInfoWindow : OpalConsoleWindow
    {
        public struct GameInfo
        {
            public TurnTakingActor Player;
            public float CurrentTurnTime, CurrentPlayerDelay;
            public int FramesPerSecond;
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

        private GameInfo gameInfo = new GameInfo();

        public void ReceiveInfoUpdateFromGame(Guid game_handle, ref GameInfo info)
        {
            gameInfo = info;
        }

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
            if (gameInfo.Player == null) return;
            Clear();
            Print(0, 0, String.Format("FPS: {0}/{1}", gameInfo.FramesPerSecond, Nexus.InitInfo.FPSCap));
            Print(0, 2, String.Format("TIME:  {1:0.00}{0} {2:0.00}{3}       ", (char)255, gameInfo.CurrentTurnTime, gameInfo.CurrentPlayerDelay, 'D'));
            Print(0, 3, String.Format("W XY: ({0}, {1})", gameInfo.Player.Map?.ParentRegion?.WorldPosition.X, gameInfo.Player.Map?.ParentRegion?.WorldPosition.Y));
            Print(0, 4, String.Format("L XY: ({0}, {1})", gameInfo.Player.LocalPosition.X, gameInfo.Player.LocalPosition.Y));
            Print(0, 6, String.Format("RNG Seed: {0}", Nexus.InitInfo.RngSeed?.ToString() ?? "None"));
            Print(0, 7, "Biome: {0}".Fmt(Nexus.Player.Map?.ParentRegion?.Biome?.Type.ToString() ?? "None"));
        }
    }
}
