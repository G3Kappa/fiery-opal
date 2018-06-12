using FieryOpal.Src.Actors;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FieryOpal.Src
{

    public delegate bool StringConversion<T>(string str, out dynamic value);

    public static class TypeConversionHelper<T>
    {
        private static StringConversion<T> _Converter;
        public static void RegisterConversion(StringConversion<T> conv)
        {
            _Converter = conv;
        }

        public static bool Convert(string str, out dynamic value)
        {
            value = default(T);
            if (_Converter == null) return false;
            return _Converter(str, out value);
        }

        public static void RegisterDefaultConversions()
        {
            TypeConversionHelper<int>.RegisterConversion((string s, out dynamic ret) =>
            {
                int iret = 0;
                ret = null;
                if (int.TryParse(s, out iret))
                {
                    ret = iret;
                    return true;
                }
                return false;
            });

            TypeConversionHelper<bool>.RegisterConversion((string s, out dynamic ret) =>
            {
                bool bret = false;
                ret = null;
                if (bool.TryParse(s, out bret))
                {
                    ret = bret;
                    return true;
                }
                return false;
            });

            TypeConversionHelper<TileSkeleton>.RegisterConversion((string s, out dynamic ret) =>
            {
                TileSkeleton ts = TileSkeleton.FromName(s);
                ret = ts;
                return ts != null;
            });
        }
    }

    public abstract class CommandDelegate
    {
        public string Cmd { get; set; }
        public Type[] Signature { get; }

        public CommandDelegate(String name, Type[] signature)
        {
            Signature = signature;
            Cmd = name;
        }

        public virtual string GetHelpText()
        {
            string types = String.Join(" ", Signature.Select(t => t.Name).ToArray());
            return "Usage: {0} {1}".Fmt(Cmd, types);
        }

        protected abstract dynamic ParseArgument(Type T, string str);
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

        public CommandRect(string name = "rect") : base(name, _Signature)
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
                if (tile is IInteractive) tile = ((TileSkeleton)args[4]).Make(OpalTile.GetFirstFreeId());
                return false;
            }, new Rectangle(x, y, w, h));

            return 0;
        }

        protected override dynamic ParseArgument(Type T, string str)
        {
            dynamic ret = null;
            if (T == typeof(int) && TypeConversionHelper<int>.Convert(str, out ret))
            {
                return ret;
            }
            else if (T == typeof(TileSkeleton) && TypeConversionHelper<TileSkeleton>.Convert(str, out ret))
            {
                return ret;
            }
            return null;
        }
    }

    public class CommandNoclip : CommandDelegate
    {
        public static Type[] _Signature = new Type[0];

        public CommandNoclip(string name = "tcl") : base(name, _Signature)
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

        protected override dynamic ParseArgument(Type T, string str)
        {
            return null;
        }
    }

    public class CommandTogglefog : CommandDelegate
    {
        public static Type[] _Signature = new Type[0];

        public CommandTogglefog(string name = "tf") : base(name, _Signature)
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

        protected override dynamic ParseArgument(Type T, string str)
        {
            return null;
        }
    }

    public class CommandSpawn : CommandDelegate
    {
        public static Type[] _Signature = new Type[4] { typeof(int), typeof(int), typeof(string), typeof(int) };

        public CommandSpawn(string name = "spawn") : base(name, _Signature)
        {

        }

        protected override int ExecInternal(object[] args)
        {
            int x = (int)args[0];
            int y = (int)args[1];
            string className = (string)args[2];
            int qty = (int)args[3];
            for (int i = 0; i < qty; ++i)
            {
                OpalActorBase h = OpalActorBase.MakeFromClassName(className);
                if (h == null)
                {
                    Util.Log("Unknown actor class.", true);
                    return 1;
                }
                Point pos = Nexus.Player.LocalPosition + new Point(x, y);
                h.ChangeLocalMap(Nexus.Player.Map, pos);
            }

            return 0;
        }

        protected override dynamic ParseArgument(Type T, string str)
        {
            dynamic ret = 0;
            if (T == typeof(string)) return str;
            if (T == typeof(int) && TypeConversionHelper<int>.Convert(str, out ret))
            {
                return ret;
            }
            return null;
        }
    }

    public class CommandLog : CommandDelegate
    {
        public static Type[] _Signature = new Type[2] { typeof(string), typeof(bool) };

        public CommandLog(string name = "log") : base(name, _Signature)
        {

        }

        protected override int ExecInternal(object[] args)
        {
            Util.Log((string)args[0], (bool)args[1]);
            return 0;
        }

        protected override dynamic ParseArgument(Type T, string str)
        {
            dynamic ret = false;
            if (T == typeof(string)) return str;
            if (T == typeof(bool) && TypeConversionHelper<bool>.Convert(str, out ret))
            {
                return ret;
            }

            return null;
        }
    }

    public class CommandDoFile : CommandDelegate
    {
        public static Type[] _Signature = new Type[2] { typeof(string), typeof(bool) };

        public CommandDoFile() : base("run", _Signature)
        {

        }

        protected override int ExecInternal(object[] args)
        {
            bool async = (bool)args[1];

            if(async)
            {
                Thread runner = new Thread(
                    () => LuaVM.DoFile((string)args[0])
                );
                runner.Start();
                return 0;
            }

            if (LuaVM.DoFile((string)args[0]) != null) return 0;
            return -1;
        }

        protected override dynamic ParseArgument(Type T, string str)
        {
            dynamic ret = false;
            if (T == typeof(string)) return str;
            if (T == typeof(bool) && TypeConversionHelper<bool>.Convert(str, out ret))
            {
                return ret;
            }

            return null;
        }
    }

    public class CommandStoreItem : CommandDelegate
    {
        public static Type[] _Signature = new Type[2] { typeof(string), typeof(int) };

        public CommandStoreItem(string name = "store") : base(name, _Signature)
        {

        }

        protected override int ExecInternal(object[] args)
        {
            for (int i = 0; i < (int)args[1]; ++i)
            {
                OpalActorBase h = OpalActorBase.MakeFromClassName((string)args[0]);
                if (h == null || !typeof(OpalActorBase).IsAssignableFrom(h.GetType()))
                {
                    Util.Log("Unknown actor class or actor class is not an item.", true);
                    return -1;
                }
            (h as Item).InteractWith(Nexus.Player);
            }
            return 0;
        }

        protected override dynamic ParseArgument(Type T, string str)
        {
            dynamic ret = null;
            if (T == typeof(string)) return str;
            if (T == typeof(int) && TypeConversionHelper<int>.Convert(str, out ret))
            {
                return ret;
            }
            return null;
        }
    }

    public class CommandEquipItem : CommandDelegate
    {
        public static Type[] _Signature = new Type[1] { typeof(string) };

        public CommandEquipItem(string name = "equip") : base(name, _Signature)
        {

        }

        protected override int ExecInternal(object[] args)
        {
            string name = (string)args[0];

            foreach (var item in Nexus.Player.Inventory.GetContents())
            {
                if (item.Name?.Equals(name, StringComparison.CurrentCultureIgnoreCase) ?? false)
                {
                    if (!(item is IEquipable))
                    {
                        Util.Log("Item is not equipable.", true);
                        return -2;
                    }

                    if(!Nexus.Player.Equipment.TryEquip((item as IEquipable), Nexus.Player))
                    {
                        Util.Log("That item is already equiped.", true);
                        return -3;
                    }
                    return 0;
                }
            }
            Util.Log("The player's inventory contains no items with that name.", true);
            return -1;
        }

        protected override dynamic ParseArgument(Type T, string str)
        {
            return str;
        }

    }

    public class CommandUnequipItem : CommandDelegate
    {
        public static Type[] _Signature = new Type[1] { typeof(string) };

        public CommandUnequipItem(string name = "unequip") : base(name, _Signature)
        {

        }

        protected override int ExecInternal(object[] args)
        {
            string name = (string)args[0];

            foreach (var ieq in Nexus.Player.Equipment.GetContents())
            {
                if ((ieq as Item).Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!Nexus.Player.Equipment.TryUnequip(ieq, Nexus.Player))
                    {
                        Util.Log("That item is not equiped right now.", true);
                        return -2;
                    }
                    return 0;
                }
            }
            Util.Log("The player has no equiped items with that name.", true);
            return -1;
        }

        protected override dynamic ParseArgument(Type T, string str)
        {
            return str;
        }

    }

}
