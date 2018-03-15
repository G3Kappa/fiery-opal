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

        public static void Log(ColoredString msg, bool debug)
        {
            GlobalLogPipeline.BroadcastLogMessage(null, msg, debug);
        }


        public static void Log(String msg, bool debug, Color? fg = null, Color? bg = null)
        {
            Color fore = fg.HasValue ? fg.Value : (debug ? Palette.Ui["BoringMessage"] : Palette.Ui["DefaultForeground"]);
            Color back = bg.HasValue ? bg.Value : Palette.Ui["DefaultBackground"];

            GlobalLogPipeline.BroadcastLogMessage(null, new ColoredString(msg, fore, back), debug);
        }

        public static void Err(String msg, bool debug = false)
        {
            Color fore = Palette.Ui["ErrorMessage"];
            Color back = Palette.Ui["DefaultBackground"];

            if (!debug)
            {
                ColoredString header = new ColoredString("ERR: ", fore, back);
                msg = header + msg;
            }

            GlobalLogPipeline.BroadcastLogMessage(null, new ColoredString(msg, fore, back), debug);
        }

        public static void Warn(String msg, bool debug = false)
        {
            Color fore = Palette.Ui["WarningMessage"];
            Color back = Palette.Ui["DefaultBackground"];

            if (!debug)
            {
                ColoredString header = new ColoredString("WARN: ", fore, back);
                msg = header + msg;
            }

            GlobalLogPipeline.BroadcastLogMessage(null, new ColoredString(msg, fore, back), debug);
        }

        public static string Localize(string s, params object[] args)
        {
            return String.Format(Program.Locale.Translation[s], args);
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
