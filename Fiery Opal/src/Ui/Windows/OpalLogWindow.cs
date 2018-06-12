using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FieryOpal.Src.Ui.Windows
{
    public class OpalLogWindow : OpalConsoleWindow
    {
        protected List<Tuple<ColoredString, bool>> LastShownMessages;

        public bool DebugMode { get; set; } = true;
        protected List<Regex> SuppressedMessagesRegexps;

        public OpalLogWindow(int w, int h, Font f = null) : base(w, h, "Log", f)
        {
            LastShownMessages = new List<Tuple<ColoredString, bool>>();
            SuppressedMessagesRegexps = new List<Regex>();
#if DEBUG
            DebugMode = true;
#else
            DebugMode = false;
#endif
        }

        public bool AddSuppressionRule(Regex exp)
        {
            if (SuppressedMessagesRegexps.Contains(exp)) return false;
            SuppressedMessagesRegexps.Add(exp);
            return true;
        }

        public bool RemoveSuppressionRule(Regex exp)
        {
            if (!SuppressedMessagesRegexps.Contains(exp)) return false;
            SuppressedMessagesRegexps.Remove(exp);
            return true;
        }

        public void Log(ColoredString msg, bool debug)
        {
            // Don't even log debug messages in release mode
#if !DEBUG
            if (debug && !DebugMode) return; 
#endif
            string msg_str = msg.ToString();
            foreach (var exp in SuppressedMessagesRegexps)
            {
                if (exp.IsMatch(msg_str)) return;
            }


            Color debug_foreground = Palette.Ui["DebugMessage"];
            Color debug_background = Palette.Ui["DefaultBackground"];
            // Debug header has inverted foreground and background on purpose
            ColoredString debug_header = new ColoredString(debug ? "DBG:" : "", debug_background, debug_foreground);
            if (debug) debug_header += new ColoredString(" ", debug_background, debug_background);

            var tup = new Tuple<ColoredString, bool>(debug_header + msg, debug);
            if (LastShownMessages.Count > 0 && LastShownMessages[LastShownMessages.Count - 1].Item1.String.StartsWith(tup.Item1.String))
            {
                var last = LastShownMessages[LastShownMessages.Count - 1];
                int count = 2;

                var re = new Regex(@"x([\d]+)$");
                if (re.IsMatch(last.Item1.String))
                {
                    count = int.Parse(re.Match(last.Item1.String).Groups[1].Value) + 1;
                }

                tup = new Tuple<ColoredString, bool>(debug_header + msg + " x{0}".Fmt(count).ToColoredString(Palette.Ui["BoringMessage"]), debug);
                LastShownMessages.RemoveAt(LastShownMessages.Count - 1);
            }
            LastShownMessages.Add(tup);
            DumpToFile(tup);
        }

        private static string SessionFileName = "";
        public void DumpToFile(Tuple<ColoredString, bool> tup)
        {
            if (SessionFileName == "")
            {
                SessionFileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log";
            }

            Color debug_foreground = Palette.Ui["DebugMessage"];
            Color debug_background = Palette.Ui["DefaultBackground"];

            File.AppendAllText("cfg/log/" + SessionFileName, tup.Item1.ToString() + Environment.NewLine);
        }

        public void LoadSuppressionRules(InitConfigInfo init)
        {
            foreach (var exp in init.Suppress.Values)
                AddSuppressionRule(new Regex(exp, RegexOptions.Compiled));
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
