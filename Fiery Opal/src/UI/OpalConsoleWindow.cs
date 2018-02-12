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
                    //lines_available++;
                    continue;
                }


                Print(0, i - debug_lines_ignored + new_line_offset, msg.Item1);

                int new_lines = msg.Item1.String.Length / Width;
                if (new_lines > 0)
                {
                    new_line_offset += new_lines;
                    lines_available -= new_lines;
                }
            }

            base.Draw(delta);
        }

        public override void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalConsoleWindow, string> action, bool is_broadcast)
        {
            string performed_action = action.Invoke(this);
            if (new[] { "Update", "Draw" }.Contains(performed_action)) return;

            Log(new ColoredString("ReceiveMessage: " + performed_action, new Cell(Color.DarkMagenta, Color.Black)), true);
        }
    }
}
