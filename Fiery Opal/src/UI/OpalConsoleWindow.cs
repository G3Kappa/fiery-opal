using Microsoft.Xna.Framework;
using SadConsole;
using SadConsole.Shapes;
using SadConsole.Surfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.UI
{
    public class OpalConsoleWindow : SadConsole.Console, IPipelineSubscriber<OpalConsoleWindow>
    {
        SadConsole.Console borderSurfaceConsole;

        public Guid Handle { get; }
        public string Caption { get; set; }

        public OpalConsoleWindow(int width, int height, string caption = "Untitled") : base(width - 2, height - 2)
        {
            // Render the border and wrap it inside a console in order to print the caption
            BasicSurface borderSurface = new BasicSurface(width, height, base.textSurface.Font);
            var editor = new SurfaceEditor(borderSurface);

            Box box = Box.GetDefaultBox();
            box.Width = borderSurface.Width;
            box.Height = borderSurface.Height;
            box.Draw(editor);

            borderSurfaceConsole = new SadConsole.Console(borderSurface);

            // Assign a new handle to this window, used by MessagePipelines as addresses
            Handle = Guid.NewGuid();
            // Set the caption
            Caption = caption;
        }

        public override void Draw(TimeSpan delta)
        {
            // Store current position
            Point oldPos = Position;
            // Set the bordered surface the be rendered at this position
            borderSurfaceConsole.Position = oldPos;
            // Add 1,1 to this position so that Print() doesn't need that offset every time
            Position += new Point(1);
            // Print the caption at 1,0 on the bordered surface
            borderSurfaceConsole.Print(1, 0, Caption, Color.White, Color.Black);
            // Draw both surfaces
            borderSurfaceConsole.Draw(delta);
            base.Draw(delta);
            // Restore the position to its intended value
            Position = oldPos;
        }

        public virtual void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalConsoleWindow, string> action, bool is_broadcast)
        {
            string performed_action = action.Invoke(this);
        }

        /// <summary>
        /// Called when RegisterWindow of a WindowManager is called on this window.
        /// </summary>
        /// <param name="wm">The WindowManager that just registered this Window.</param>
        public virtual void OnWindowManagerRegistration(WindowManager wm) { }

        /// <summary>
        /// Called when UnregisterWindow of a WindowManager is called on this window.
        /// </summary>
        /// <param name="wm">The WindowManager that just unregistered this Window.</param>
        public virtual void OnWindowManagerUnregistration(WindowManager wm) { }
    }

    public class OpalLogWindow : OpalConsoleWindow
    {
        protected List<Tuple<ColoredString, bool>> LastShownMessages;
        public readonly int LastShownMessagesCap;
        public readonly int LastShownMessagesDumpAmount;

        public bool DebugMode { get; set; } = true;

        public OpalLogWindow(int w, int h, int last_shown_cap = 500, int dump_amount = 100) : base(w, h, "Log")
        {
            if (dump_amount > last_shown_cap) throw new ArgumentOutOfRangeException("dump_amount");

            LastShownMessagesCap = last_shown_cap;
            LastShownMessagesDumpAmount = dump_amount;
            LastShownMessages = new List<Tuple<ColoredString, bool>>(LastShownMessagesCap);
        }

        public void Log(ColoredString msg, bool debug)
        {
            if (debug && !DebugMode) return;

            LastShownMessages.Add(new Tuple<ColoredString, bool>(msg, debug));
            if (LastShownMessages.Count >= LastShownMessagesCap)
            {
                // TODO: Dump `LastShownMessagesDumpAmount` messages to disk.
                LastShownMessages.RemoveRange(0, LastShownMessagesDumpAmount);
                Log(new ColoredString(string.Format("Log: Dumped last {0} shown messages.", LastShownMessagesDumpAmount), new Cell(Color.Magenta, Color.Black)), true);
            }
        }

        public override void Draw(TimeSpan delta)
        {
            Clear();
            int lines_available = Math.Min(Height, LastShownMessages.Count);
            int debug_lines_ignored = 0;
            int new_line_offset = 0;
            for (int i = 0; i < lines_available; ++i)
            {
                var msg = LastShownMessages[LastShownMessages.Count - lines_available + i];
                if (msg.Item2 && !DebugMode)
                {
                    debug_lines_ignored++;
                    continue;
                }


                Print(0, i - debug_lines_ignored + new_line_offset, msg.Item1);

                int new_lines = msg.Item1.String.Length / Width;
                if (new_lines > 0)
                {
                    new_line_offset += new_lines + 1;
                    lines_available -= new_lines;
                    i--;
                }
            }

            base.Draw(delta);
        }
    }

    public class OpalGameWindow : OpalConsoleWindow
    {
        public MessagePipeline<OpalGame> InternalMessagePipeline { get; protected set; }
        protected List<WindowManager> ConnectedWindowManagers = new List<WindowManager>();
        public OpalGame Game { get; protected set; }

        public Viewport OverrideViewport { get; set; } = null;

        public OpalGameWindow(int w, int h, OpalGame g, Viewport overrideViewport = null) : base(w, h, "Fiery Opal")
        {
            Game = g;
            InternalMessagePipeline = new MessagePipeline<OpalGame>();
            InternalMessagePipeline.Subscribe(g);
            g.InternalMessagePipeline.Subscribe(this);
            OverrideViewport = overrideViewport;
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

            if (OverrideViewport != null)
            {
                var oldViewport = Game.Viewport;
                Game.Viewport = OverrideViewport;
                Game.Update(delta);
                Game.Viewport = oldViewport;
            }
            else
            {
                Game.Update(delta);
            }
        }

        public override void Draw(TimeSpan delta)
        {
            if(OverrideViewport != null)
            {
                var oldViewport = Game.Viewport;
                Game.Viewport = OverrideViewport;
                Game.Draw(delta);
                Game.Viewport = oldViewport;
            }
            else
            {
                Game.Draw(delta);
            }

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

        public OpalInfoWindow(int w, int h) : base(w, h, "Info")
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
            if(width < 8)
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

                if((float)(i - 5) / (width - 6) <= health_percentage && health_percentage > 0)
                {
                    barglyphs[i] = new ColoredGlyph(color);
                    barglyphs[i].GlyphCharacter = '=';
                } else
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
