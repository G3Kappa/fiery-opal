using FieryOpal.Src.Multiplayer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Ui.Dialogs
{
    public class DebugCLI : OpalDialog
    {
        public InputBox Input { get; protected set; }

        protected Dictionary<string, CommandDelegate> Delegates = new Dictionary<string, CommandDelegate>();
        protected static Stack<string> CmdHistory = new Stack<string>();
        protected static int CmdHistoryIndex = -1;

        protected bool RegisterDelegate(string cmd, CommandDelegate del)
        {
            if (Delegates.ContainsKey(cmd)) return false;
            Delegates[cmd] = del;
            del.Cmd = cmd;
            return true;
        }

        protected bool CallDelegate(string[] args, ref int exitCode)
        {
            if (args.Length == 0) return false;
            if (new[] { "h", "help", "aiut" }.Contains(args[0].ToLower()))
            {
                Color d_fore = Palette.Ui["DefaultForeground"];
                Color b_fore = Palette.Ui["BoringMessage"];
                Color back = Palette.Ui["DefaultBackground"];

                foreach (var s in Delegates.Keys)
                {
                    Util.Log(new ColoredString(s, d_fore, back) + new ColoredString("({0})".Fmt(Delegates[s].GetSignatureString()), b_fore, back), false);
                }
                exitCode = 0;
                return true;
            }

            if (!Delegates.ContainsKey(args[0])) return false;
            exitCode = Delegates[args[0]].Execute(args.Skip(1).ToArray());
            return true;
        }

        public DebugCLI() : base()
        {
            Borderless = true;
            CloseOnESC = false;

            textSurface.DefaultBackground =
                Theme.FillStyle.Background =
                new Color(0, 0, 0, 0);

            Input = new InputBox(Width);
            Input.Position = new Point(0, 0);
            Input.DisableMouse = true;
            Input.ExclusiveFocus = true;
            Input.UseKeyboard = false;
            var InputStyle = new Cell(Palette.DefaultTextStyle.Foreground, DefaultPalette["ShadeLight"].ChangeValue(-1, -1, -1, 192));
            Input.Theme = new SadConsole.Themes.InputBoxTheme()
            {
                Focused = InputStyle,
                Normal = InputStyle,
                Disabled = InputStyle,
                MouseOver = InputStyle,
            };
            Add(Input);
            Clear();

            RegisterDelegate("rect", new CommandRect());
            RegisterDelegate("spawn", new CommandSpawn());
            RegisterDelegate("log", new CommandLog());
            RegisterDelegate("run", new CommandDoFile());
            RegisterDelegate("store", new CommandStoreItem());
            RegisterDelegate("equip", new CommandEquipItem());
            RegisterDelegate("unequip", new CommandUnequipItem());
            RegisterDelegate("startsv", new CommandStartServer());
            RegisterDelegate("connect", new CommandStartClient());
            RegisterDelegate("say", new CommandSendChatMsg());

            var noclip = new CommandNoclip();
            RegisterDelegate("noclip", noclip);
            RegisterDelegate("tcl", noclip);

            var tfog = new CommandTogglefog();
            RegisterDelegate("togglefog", tfog);
            RegisterDelegate("tf", tfog);
        }

        public override void Update(TimeSpan time)
        {
            base.Update(time);
        }

        private static IEnumerable<string> SplitArgs(string str)
        {
            bool insideQuotes = false;
            string s = "";
            for (int i = 0; i < str.Length; ++i)
            {
                if (!insideQuotes && (str[i] == ' ' || str[i] == ','))
                {
                    if (s.Trim().Length > 0) yield return s.Trim();
                    s = "";
                    continue;
                }
                else if (str[i] == '"')
                {
                    if (i == 0 || i > 0 && str[i - 1] != '\\')
                    {
                        insideQuotes = !insideQuotes;
                        continue; //Don't append these quotes
                    }
                }
                s += str[i];
            }
            if (s.Trim().Length > 0) yield return s.Trim();
        }

        protected string Exec(string str, ref int exitCode)
        {
            CmdHistory.Push(str);
            var args = SplitArgs(str).ToArray();

            exitCode = -1;
            if (CallDelegate(args, ref exitCode))
            {
                if (exitCode == 0 && !Delegates[args[0]].PrintZeroExitCode)
                    return "";
                return "Exit Code: {0}".Fmt(exitCode);
            }
            return "Unknown command";
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            if (info.IsKeyPressed(Keys.Enter))
            {
                Input.DisableKeyboard = true;
                Input.ProcessKeyboard(info);

                if(!Input.Text.StartsWith("!")) Util.LogCmd(Input.Text);

                var commands = Input.Text.Split('&');
                foreach (var c in commands)
                {
                    int exitCode = 0;
                    var msg = Exec(c.Trim().TrimStart('!'), ref exitCode);
                    if (msg.Length == 0) continue;

                    if (exitCode == 0)
                    {
                        Util.LogText(msg, false, Palette.Ui["BoringMessage"]);
                    }
                    else Util.Err(msg);
                }

                Input.Text = "";

                Hide();
                return false;
            }
            else if (info.IsKeyPressed(Keys.Escape)) return false;
            else if (info.IsKeyPressed(Keys.Up))
            {
                if (CmdHistoryIndex >= CmdHistory.Count - 1) return false;
                CmdHistoryIndex++;

                Input.Text = CmdHistory.ElementAt(CmdHistoryIndex);
                return false;
            }
            else if (info.IsKeyPressed(Keys.Down))
            {
                if (CmdHistoryIndex <= 0)
                {
                    Input.Text = "";
                    return false;
                }
                CmdHistoryIndex--;
                Input.Text = CmdHistory.ElementAt(CmdHistoryIndex);
                return false;
            }

            Input.UseKeyboard = true;
            Input.ProcessKeyboard(info);
            Input.UseKeyboard = false;
            return base.ProcessKeyboard(info);
        }

        public override void Hide()
        {
            CmdHistoryIndex = -1;
            Keybind.PopState();
            base.Hide();
        }

        protected override void BindKeys()
        {
            base.BindKeys();
            Keybind.BindKey(
                new Keybind.KeybindInfo(Keys.V, Keybind.KeypressState.Press, "Debug CLI: Paste", true),
                (info) =>
                {
                    Input.Text += System.Windows.Forms.Clipboard.GetText();
                }
            );
        }

        public IEnumerable<CommandDelegate> GetRegisteredDelegates()
        {
            return Delegates.Values;
        }
    }
}
