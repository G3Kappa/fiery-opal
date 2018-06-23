using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Windows;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Text;

namespace FieryOpal.Src
{
    public static partial class Util
    {
        public static readonly MessagePipeline<OpalConsoleWindow> GlobalLogPipeline = new MessagePipeline<OpalConsoleWindow>();

        private static IEnumerable<string> SplitLogString(String msg)
        {
            var split = msg.Split('\n');
            foreach (string s in split) yield return s;
        }

        public static void Log(ColoredString msg, bool debug)
        {
            GlobalLogPipeline.BroadcastLogMessage(null, msg, debug);
        }

        public static void LogBadge(string badge, string msg, bool debug, Color? fg = null, Color? bg = null)
        {
            Color fore = fg.HasValue ? fg.Value : (debug ? Palette.Ui["BoringMessage"] : Palette.Ui["DefaultForeground"]);
            Color back = bg.HasValue ? bg.Value : Palette.Ui["DefaultBackground"];

            ColoredString header = new ColoredString(badge, back, fore);
            Log(header + new ColoredString(" " + msg, fore, back), debug);
        }

        public static void LogText(String msg, bool debug, Color? fg = null, Color? bg = null)
        {
            if (msg.Contains("\n"))
            {
                SplitLogString(msg).ForEach(s => LogText(s, debug, fg, bg));
                return;
            }

            Color fore = fg.HasValue ? fg.Value : (debug ? Palette.Ui["BoringMessage"] : Palette.Ui["DefaultForeground"]);
            Color back = bg.HasValue ? bg.Value : Palette.Ui["DefaultBackground"];

            Log(new ColoredString(msg, fore, back), debug);
        }

        public static void Err(String msg, bool debug = false)
        {
            Color fore = Palette.Ui["ErrorMessage"];
            Color back = Palette.Ui["DefaultBackground"];
            LogBadge("ERR :", msg, debug, fore, back);
        }

        public static void Warn(String msg, bool debug = false)
        {
            Color fore = Palette.Ui["WarningMessage"];
            Color back = Palette.Ui["DefaultBackground"];
            LogBadge("WARN :", msg, debug, fore, back);
        }

        public static void LogCmd(String msg)
        {
            Color fore = Palette.Ui["DebugMessage"];
            Color back = Palette.Ui["DefaultBackground"];
            LogBadge("CMD >", msg, false, fore, back);
        }

        public static void LogServer(String msg, bool debug = false)
        {
            Color fore = Palette.Ui["ServerMessage"];
            Color back = Palette.Ui["DefaultBackground"];
            LogBadge("SERV:", msg, false, fore, back);
        }

        public static void LogClient(String msg, bool debug = false)
        {
            Color fore = Palette.Ui["ClientMessage"];
            Color back = Palette.Ui["DefaultBackground"];
            LogBadge("CLNT:", msg, false, fore, back);
        }

        public static void LogChat(String msg, bool debug = false)
        {
            Color fore = Palette.Ui["ChatMessage"];
            Color back = Palette.Ui["DefaultBackground"];
            LogBadge("CHAT:", msg, false, fore, back);
        }

        public static string Str(string s, params object[] args)
        {
            return String.Format(Nexus.Locale.Translation[s], args);
        }
    }

    public static partial class Extensions
    {
        public static string Repeat(this string s, int n)
        {
            StringBuilder sb = new StringBuilder();
            while (n-- > 0) sb.Append(s);
            return sb.ToString();
        }

        public static string Repeat(this char c, int n)
        {
            StringBuilder sb = new StringBuilder();
            while (n-- > 0) sb.Append(c);
            return sb.ToString();
        }

        public static ColoredString ToColoredString(this string s, Color? fg = null, Color? bg = null)
        {
            return new ColoredString(s, fg ?? Color.White, bg ?? Color.Transparent);
        }

        public static ColoredString ToColoredString(this string s, Cell c)
        {
            return new ColoredString(s, c);
        }

        public static ColoredString ToColoredString(this int glyph, Cell c)
        {
            return new ColoredString(((char)glyph).ToString(), c);
        }

        public static ColoredString ToColoredString(this char glyph, Cell c)
        {
            return new ColoredString(glyph.ToString(), c);
        }

        public static string CapitalizeFirst(this string s)
        {
            return Char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
