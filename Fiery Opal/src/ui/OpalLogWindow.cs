using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;

namespace FieryOpal.src.ui
{
    public class OpalLogWindow : OpalConsoleWindow
    {
        protected List<Tuple<ColoredString, bool>> LastShownMessages;
        public readonly int LastShownMessagesCap;
        public readonly int LastShownMessagesDumpAmount;

        public bool DebugMode { get; set; } = true;

        public OpalLogWindow(int w, int h, int last_shown_cap = 500, int dump_amount = 100, Font f = null) : base(w, h, "Log", f)
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
}
