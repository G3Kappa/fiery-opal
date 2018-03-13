using FieryOpal.Src;
using FieryOpal.Src.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static FieryOpal.Src.Keybind;
using FieryOpal.src;
using FieryOpal.Src.Ui;

namespace FieryOpal.src
{
    public abstract class ConfigParser<T>
    {
        public int CurrentLine { get; private set; }
        public string CurrentFile { get; private set; }
        public string CurrentDirectory { get; private set; }

        protected abstract T ParseLine(string s);
        public IEnumerable<T> Parse(string filename)
        {
            if (!File.Exists(filename))
            {
                yield break;
            }

            CurrentLine = 1;
            CurrentFile = filename;
            CurrentDirectory = Path.GetDirectoryName(filename);

            using (var file = File.OpenText(filename))
            {
                string line = null;
                while((line = file.ReadLine()) != null)
                {
                    if (line.Trim().Length == 0) continue;

                    var to_yield = ParseLine(line);
                    if (to_yield != null) yield return to_yield;
                    CurrentLine++;
                }
            }
        }
    }

    public abstract class ConfigLoader<T, Y>
    {
        public ConfigParser<T> Parser { get; }

        public ConfigLoader(ConfigParser<T> parser)
        {
            Parser = parser;
        }

        protected bool TrySetProperty(object obj, string prop_name, object value, string dict_index)
        {
            PropertyInfo prop = obj.GetType().GetProperty(prop_name, BindingFlags.Public | BindingFlags.Instance);

            if (prop == null || !prop.CanWrite) return false;

            Type prop_type = prop.PropertyType;
            if (dict_index == null)
            {
                if(value is string)
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(prop_type);
                    try
                    {
                        value = converter.ConvertFromString(value.ToString());
                    }
                    catch
                    {
                        Util.Err(String.Format("TrySetProperty> Invalid cast: expected '{0}' but got '{1}' for property '{2}'.", prop_type.ToString(), "String", prop_name), true);
                        return false;
                    }
                }
                prop.SetValue(obj, value, null);
            }
            else
            {
                object data = prop.GetValue(obj);
                var tkey = data.GetType().GetGenericArguments()[0];
                var tvalue = data.GetType().GetGenericArguments()[1];
                var add = data.GetType().GetMethod("Add", new[] {
                    tkey,
                    tvalue
                });

                TypeConverter to_tkey = TypeDescriptor.GetConverter(tkey);
                if (value is string)
                {
                    TypeConverter to_tvalue = TypeDescriptor.GetConverter(tvalue);
                    try
                    {
                        value = to_tvalue.ConvertFromString(value.ToString());
                    }
                    catch
                    {
                        Util.Err(String.Format("TrySetProperty> Invalid cast: expected '{0}' but got '{1}' for TValue of dictionary '{2}'.", tvalue.ToString(), "String", prop_name), true);
                        return false;
                    }
                }

                try
                {
                    add.Invoke(data, new object[] { to_tkey.ConvertFromString(dict_index), value });
                }
                catch
                {
                    Util.Err(String.Format("TrySetProperty> Invalid cast: expected '{0}' but got '{1}' for TKey of dictionary '{2}'.", tkey.ToString(), "String", prop_name), true);
                    return false;
                }
            }
            return true;
        }

        protected abstract Y BuildRepresentation(T[] tokens);

        public Y LoadFile(string filename)
        {
            var tokens = Parser.Parse(filename);
            return BuildRepresentation(tokens.ToArray());
        }
    }

    public class ConfigParserBase : ConfigParser<Tuple<string, string>>
    {
        protected static Regex CommentRegex = new Regex(@"^\s*\/\/");
        protected static Regex AssignmentRegex = new Regex(@"^\s*(.*?)\s*=\s*(.*)\s*$");

        protected override Tuple<string, string> ParseLine(string s)
        {
            if (CommentRegex.IsMatch(s)) return null;
            if (!AssignmentRegex.IsMatch(s))
            {
                Util.Err(String.Format("{0}:{1}> Invalid expression or assignment.", CurrentFile, CurrentLine));
                return null;
            }

            var matches = AssignmentRegex.Matches(s);
            var lhs_rhs = new Tuple<string, string>(matches[0].Groups[1].Value, matches[0].Groups[2].Value);
            return lhs_rhs;
        }
    }

    public class RelectionBasedConfigLoader<T> : ConfigLoader<Tuple<string, string>, T>
        where T : new()
    {
        protected Func<RelectionBasedConfigLoader<T>, string, object> DelegatedConversion;

        public RelectionBasedConfigLoader(Func<RelectionBasedConfigLoader<T>, string, object> convert_rhs) 
            : base(new ConfigParserBase())
        {
            DelegatedConversion = convert_rhs;
        }

        protected override T BuildRepresentation(Tuple<string, string>[] tokens)
        {
            T ret = new T();
            Stack<Tuple<string, string>> token_stack = new Stack<Tuple<string, string>>(tokens);
            var cur_dir = Parser.CurrentDirectory;
            while(token_stack.Count > 0)
            {
                var t = token_stack.Pop();
                if(t.Item1 == "INCLUDE")
                {
                    var rev = Parser.Parse(Path.Combine(cur_dir, t.Item2)).Reverse();
                    if(rev.Count() == 0)
                    {
                        Util.Err(String.Format("INCLUDE not found: \"{0}\".", t.Item2));
                        continue;
                    }
                    foreach (var p in rev)
                    {
                        token_stack.Push(p);
                    }
                    continue;
                }

                object rhs = DelegatedConversion(this, t.Item2);
                if(rhs == null)
                {
                    continue;
                }

                bool property_set = false;
                if (t.Item1.Contains("[")) // It's a dictionary entry
                {
                    var dict_name = t.Item1.Substring(0, t.Item1.IndexOf('['));
                    var dict_index = t.Item1.Substring(t.Item1.IndexOf('[') + 1, t.Item1.IndexOf(']') - t.Item1.IndexOf('[') - 1);

                    property_set = TrySetProperty(ret, dict_name, rhs, dict_index);
                }
                else
                {
                    property_set = TrySetProperty(ret, t.Item1, rhs, null);
                }
                if(!property_set) Util.Err(String.Format("Unknown property: \"{0}\" for type {1}", t.Item1, typeof(T).ToString()));
            }
            return ret;
        }
    }

    public class FontConfigInfo
    {
        public Font FirstPersonViewportFont { get; set; }
        public Font MainFont { get; set; }
        public Dictionary<string, Font> Spritesheets { get; set; } = new Dictionary<string, Font>();
    }

    public class FontConfigLoader : RelectionBasedConfigLoader<FontConfigInfo>
    {
        public FontConfigLoader()
            : base((self, rhs) => Global.LoadFont(rhs).GetFont(Font.FontSizes.One))
        {

        }
    }

    public class InitConfigInfo
    {
        public int ProgramWidth { get; set; } = 180;
        public int ProgramHeight { get; set; } = 80;

        public int WorldWidth { get; set; } = 100;
        public int WorldHeight { get; set; } = 100;

        public string DefaultFontPath { get; set; } = "gfx/Taffer.font";
        public string Locale { get; set; } = "cfg/locale/en_US.cfg";
    }

    public class InitConfigLoader : RelectionBasedConfigLoader<InitConfigInfo>
    {
        public InitConfigLoader()
            : base((self, rhs) => rhs)
        {

        }
    }

    public class DefaultDictionary<T, K> : Dictionary<T, K>
        where K : class
    {
        protected Func<T, K> DefaultValueDelegate;

        public DefaultDictionary(Func<T, K> defaultDelegate) : base()
        {
            DefaultValueDelegate = defaultDelegate;
        }

        public new K this[T key]
        {
            get
            {
                if(ContainsKey(key))
                {
                    K value;
                    TryGetValue(key, out value);
                    return value;
                }
                return DefaultValueDelegate(key);
            }
            set
            {
                Add(key, value);
            }
        }
    }

    public class LocalizationInfo
    {
        public static string GetDefaultString(string key)
        {
            Util.Warn(String.Format("Locale> No localization string provided for '{0}'.", key), true);
            return key;
        }

        public DefaultDictionary<string, string> Translation { get; set; } = new DefaultDictionary<string, string>(GetDefaultString);
    }

    public class LocalizationLoader : RelectionBasedConfigLoader<LocalizationInfo>
    {
        public LocalizationLoader()
            : base((self, rhs) => rhs)
        {

        }
    }

    public class KeybindConfigInfo
    {
        public Dictionary<string, KeybindInfo> Player { get; set; } = new Dictionary<string, KeybindInfo>();

        public PlayerActionsKeyConfiguration GetPlayerKeybinds()
        {
            var cfg = new PlayerActionsKeyConfiguration();
            foreach (var kv in Player)
            {
                cfg.AssignKey(Util.GetEnumValueFromName<PlayerAction>(kv.Key), kv.Value);
            }
            return cfg;
        }

}

    public class KeybindConfigLoader : RelectionBasedConfigLoader<KeybindConfigInfo>
    {
        static Regex KeybindRegex = new Regex("(?:Key:)?\\s*([\\w]+)\\s*,\\s*(?:State:)?\\s*(Press|Down|Release)\\s*,\\s*(?:Help:)?\\s*\"(.*?)\"\\s*,\\s*(?:Ctrl:)?\\s*(true|false)\\s*,\\s*(?:Shift:)?\\s*(true|false)\\s*,\\s*(?:Alt:)?\\s*(true|false)\\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static KeybindInfo? ParseRhs(string rhs)
        {
            if (!KeybindRegex.IsMatch(rhs))
            {
                Util.Err(String.Format("Unparsed KeybindInfo: \"{0}\"", rhs));
                return null;
            }

            var groups = KeybindRegex.Match(rhs).Groups;
            var ret = new KeybindInfo();

            ret.MainKey = Util.GetEnumValueFromName<Keys>(groups[1].Value);
            ret.State = Util.GetEnumValueFromName<KeypressState>(groups[2].Value);
            ret.HelpText = groups[3].Value;
            ret.CtrlDown = bool.Parse(groups[4].Value);
            ret.ShiftDown = bool.Parse(groups[5].Value);
            ret.AltDown = bool.Parse(groups[6].Value);

            if(ret.MainKey == Keys.None)
            {
                Util.Err(String.Format("Unknown key: \"{0}\"; HelpText: \"{1}\"", groups[1].Value, ret.HelpText));
            }

            return ret;
        }

        public KeybindConfigLoader()
            : base((self, lhs) => ParseRhs(lhs))
        {

        }
    }

    public class PaletteConfigInfo
    {
        public Dictionary<string, Color> Ui { get; set; } = new Dictionary<string, Color>();
        public Dictionary<string, Color> Terrain { get; set; } = new Dictionary<string, Color>();
        public Dictionary<string, Color> Vegetation { get; set; } = new Dictionary<string, Color>();
        public Dictionary<string, Color> Creatures { get; set; } = new Dictionary<string, Color>();
    }

    public class PaletteConfigLoader : RelectionBasedConfigLoader<PaletteConfigInfo>
    {
        static Regex RGBValueRegex = new Regex("(?:([\\d]{1,3}),?\\s*)", RegexOptions.Compiled);
        static Regex RGBAParserRegex = new Regex(RGBValueRegex.ToString().Repeat(4) + "?", RegexOptions.Compiled);

        private static Color ParseRhs(string rhs)
        {
            if(RGBAParserRegex.IsMatch(rhs))
            {
                var m = RGBAParserRegex.Match(rhs);
                int R = 0, G = 0, B = 0, A = 255;
                R = int.Parse(m.Groups[1].Value);
                G = int.Parse(m.Groups[2].Value);
                B = int.Parse(m.Groups[3].Value);
                if(m.Groups.Count == 5 && m.Groups[4].Value.Length > 0)
                {
                    A = int.Parse(m.Groups[4].Value);
                }
                return new Color(R, G, B, A);
            }

            return Color.Magenta;
        }

        public PaletteConfigLoader()
            : base((self, lhs) => ParseRhs(lhs))
        {
        }
    }
}
