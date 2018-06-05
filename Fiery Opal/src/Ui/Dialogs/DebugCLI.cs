using FieryOpal.Src.Actors;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
            var InputStyle = new SadConsole.Cell(Palette.DefaultTextStyle.Foreground, DefaultPalette["ShadeLight"].ChangeValue(-1, -1, -1, 192));
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

            var noclip = new CommandNoclip();
            RegisterDelegate("noclip", noclip);
            RegisterDelegate("tcl", noclip);

            var tfog = new CommandTogglefog();
            RegisterDelegate("togglefog", tfog);
            RegisterDelegate("tf", tfog);
        }

        protected override void BindKeys()
        {
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.F9, Keybind.KeypressState.Press, "Hide debug CLI"), (e) => Hide());
        }

        public override void Update(TimeSpan time)
        {
            base.Update(time);
        }

        protected string Exec(string str, ref int exitCode)
        {
            CmdHistory.Push(str);
            var args = str.Split(' ');

            exitCode = -1;
            if(CallDelegate(args, ref exitCode))
            {
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

                Util.LogCmd(Input.Text);
                int exitCode = 0;
                var msg = Exec(Input.Text, ref exitCode);
                Util.Log(msg, false, exitCode == 0 ? Palette.Ui["BoringMessage"] : Palette.Ui["ErrorMessage"]);
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
            base.Hide();
        }
    }

    public abstract class CommandDelegate
    {
        public string Cmd { get; set; }
        public Type[] Signature { get; }

        public CommandDelegate(Type[] signature)
        {
            Signature = signature;
        }

        public virtual string GetHelpText()
        {
            string types = String.Join(" ", Signature.Select(t => t.Name).ToArray());
            return "Usage: {0} {1}".Fmt(Cmd, types);
        }

        protected abstract object ParseArgument(Type T, string str);
        protected abstract int ExecInternal(object[] args);

        public int Execute(params string[] args)
        {
            if (args.Length != Signature.Length)
            {
                Util.Log(GetHelpText(), true, Palette.Ui["InfoMessage"]);
                return -1;
            }

            object[] arguments = new object[Signature.Length];
            for (int i = 0; i < Signature.Length; ++i)
            {
                object arg = null;
                if ((arg = ParseArgument(Signature[i], args[i])) == null)
                {
                    Util.Log(GetHelpText(), true, Palette.Ui["InfoMessage"]);
                    return -2;
                }
                arguments[i] = arg;
            }

            return ExecInternal(arguments);
        }
    }

    public class CommandRect : CommandDelegate
    {
        public static Type[] _Signature = new[] {
            typeof(int), // X (relative to player)
            typeof(int), // Y (relative to player)
            typeof(int), // Width
            typeof(int), // Height
            typeof(TileSkeleton), // Tile Skeleton name
        };

        public CommandRect() : base(_Signature)
        {

        }

        protected override int ExecInternal(object[] args)
        {
            int x = Nexus.Player.LocalPosition.X + (int)args[0];
            int y = Nexus.Player.LocalPosition.Y + (int)args[1];
            int w = (int)args[2];
            int h = (int)args[3];
            OpalTile tile = ((TileSkeleton)args[4]).Make(OpalTile.GetFirstFreeId());

            Nexus.Player.Map.Iter((s, mx, my, t) =>
            {
                s.SetTile(mx, my, tile);
                if(tile is IInteractive) tile = ((TileSkeleton)args[4]).Make(OpalTile.GetFirstFreeId());
                return false;
            }, new Rectangle(x, y, w, h));

            return 0;
        }

        protected override object ParseArgument(Type T, string str)
        {
            if (T == typeof(int))
            {
                int parsed = 0;
                if (!int.TryParse(str, out parsed)) return null;
                return parsed;
            }
            else if (T == typeof(TileSkeleton))
            {
                TileSkeleton ts = TileSkeleton.FromName(str);
                if(ts == null)
                {
                    Util.Log("Unknown tile.", true);
                }
                return ts;
            }

            return null;
        }
    }

    public class CommandNoclip : CommandDelegate
    {
        public static Type[] _Signature = new Type[0];

        public CommandNoclip() : base(_Signature)
        {

        }

        protected override int ExecInternal(object[] args)
        {
            Nexus.Player.SetCollision(Nexus.Player.IgnoresCollision);
            Util.Log(
                ("-- " + (!Nexus.Player.IgnoresCollision ? "Enabled " : "Disabled") + " collision.").ToColoredString(Palette.Ui["DebugMessage"]),
                false
            );
            return 0;
        }

        protected override object ParseArgument(Type T, string str)
        {
            return null;
        }
    }

    public class CommandTogglefog : CommandDelegate
    {
        public static Type[] _Signature = new Type[0];

        public CommandTogglefog() : base(_Signature)
        {

        }

        protected override int ExecInternal(object[] args)
        {
            Nexus.Player.Brain.TileMemory.Toggle();
            Util.Log(
                ("-- " + (Nexus.Player.Brain.TileMemory.IsEnabled ? "Enabled " : "Disabled") + " fog.").ToColoredString(Palette.Ui["DebugMessage"]),
                false
            );
            return 0;
        }

        protected override object ParseArgument(Type T, string str)
        {
            return null;
        }
    }

    public class CommandSpawn : CommandDelegate
    {
        public static Type[] _Signature = new Type[1] { typeof(int) };

        public CommandSpawn() : base(_Signature)
        {

        }

        protected override int ExecInternal(object[] args)
        {
            int qty = (int)args[0];
            for(int i = 0; i < qty; ++i)
            {
                Humanoid h = new Humanoid();
                h.ChangeLocalMap(Nexus.Player.Map, Nexus.Player.LocalPosition);
            }
            return 0;
        }

        protected override object ParseArgument(Type T, string str)
        {
            try
            {
                return int.Parse(str);
            }
            catch(FormatException e)
            {
                return null;
            }
        }
    }
}
