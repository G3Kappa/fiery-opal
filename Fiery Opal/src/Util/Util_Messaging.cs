using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Windows;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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

        private static Color ParseColor(string c)
        {
            if (c.StartsWith("#"))
            {
                c = c.Substring(1);
                int ofs = c.Length == 3 ? 1 : 2;

                byte r = Byte.Parse(c.Substring(0, ofs), NumberStyles.HexNumber);
                byte g = Byte.Parse(c.Substring(1 * ofs, ofs), NumberStyles.HexNumber);
                byte b = Byte.Parse(c.Substring(2 * ofs, ofs), NumberStyles.HexNumber);

                if (ofs == 1)
                {
                    r *= 16; g *= 16; b *= 16;
                }

                return new Color(r, g, b);
            }
            else if (c.Length == 0) return Color.Transparent;
            else return Palette.Ui[c];
        }

        public static ColoredString FmtC(this string fmt, Color? fg, Color? bg, params object[] args)
        {
            // Valid examples: {0}, {1:RED}, {4:WHITE:CYAN}, {5:#FF00FF:BLACK}
            const string R_HEX = "#[0-9a-fA-F]{3}|#[0-9a-fA-F]{6}";
            const string R_NAME = "[a-zA-Z]+";

            const string R_COLOR = "(" + R_NAME + "|" + R_HEX + ")";
            const string R_OPT_ARG = "(?:\\:" + R_COLOR + ")?";

            const string R_ARG = @"\{(\d)" + R_OPT_ARG + R_OPT_ARG + @"\}";

            ColoredString ret = new ColoredString("", Color.White, Color.Black);
            foreach(Match match in Regex.Matches(fmt, R_ARG, RegexOptions.Compiled))
            {
                int argIdx = Int32.Parse(match.Groups[1].Value);
                if (argIdx >= args.Length) throw new ArgumentOutOfRangeException();

                int idx = fmt.IndexOf(match.Value);
                ret += fmt.Substring(0, idx).ToColoredString(fg ?? Color.Transparent, bg ?? Color.Transparent);
                fmt = fmt.Substring(idx + match.Length);

                Color cFg = match.Groups.Count > 2 ? ParseColor(match.Groups[2].Value) : fg ?? Color.Transparent;
                Color cBg = match.Groups.Count > 3 ? ParseColor(match.Groups[3].Value) : bg ?? Color.Transparent;

                ret += args[argIdx].ToString().ToColoredString(cFg, cBg);
            }
            ret += fmt.ToColoredString(fg, bg);

            return ret;
        }

        public static string CapitalizeFirst(this string s)
        {
            return Char.ToUpper(s[0]) + s.Substring(1);
        }

        public static ColoredString Insert(this ColoredString s, ColoredGlyph g, int pos, bool append=true)
        {
            var sg = new ColoredString(new[] { g });
            if (s.Count == 0) return sg;

            if (pos > s.Count) throw new IndexOutOfRangeException();
            if(pos > 0)
            {
                if(append)
                {
                    return s.SubString(0, pos) + sg + s.SubString(pos, s.Count - pos);
                }
                else if(pos + 1 < s.Count)
                {
                    return s.SubString(0, pos) + sg + s.SubString(pos + 1, s.Count - pos - 1);
                }
                else return s.SubString(0, pos) + sg;
            }
            else
            {
                if(append)
                {
                    return sg + s;
                }
                else if(1 < s.Count)
                {
                    return sg + s.SubString(1, s.Count - 1);
                }
                else return sg;
            }
        }

        public static ColoredString Recolor(this ColoredString s, Color? fg = null, Color? bg = null, bool onlyIfTransparent=false)
        {
            List<ColoredGlyph> glyphs = new List<ColoredGlyph>();
            foreach(var G in s)
            {
                var g = G.Clone();
                if (!onlyIfTransparent || g.Foreground.A == 0 && onlyIfTransparent)
                {
                    g.Foreground = fg ?? g.Foreground;
                }

                if (!onlyIfTransparent || g.Background.A == 0 && onlyIfTransparent)
                {
                    g.Background = bg ?? g.Background;
                }
                glyphs.Add(g);
            }
            return new ColoredString(glyphs.ToArray());
        }
    }
}
