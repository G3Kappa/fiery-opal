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
using System.Text.RegularExpressions;
using static FieryOpal.Src.Keybind;

namespace FieryOpal.Src
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
                while ((line = file.ReadLine()) != null)
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
                if (value is string)
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

    public class ReflectionBasedConfigLoader<T> : ConfigLoader<Tuple<string, string>, T>
        where T : new()
    {
        protected Func<ReflectionBasedConfigLoader<T>, string, object> DelegatedConversion;

        public ReflectionBasedConfigLoader(Func<ReflectionBasedConfigLoader<T>, string, object> convert_rhs)
            : base(new ConfigParserBase())
        {
            DelegatedConversion = convert_rhs;
        }

        private IEnumerable<Tuple<string, string>> Include(string path)
        {
            var tokens = Parser.Parse(path);
            if (tokens.Count() == 0)
            {
                Util.Err(String.Format("INCLUDE not found or empty: \"{0}\".", path));
                yield break;
            }
            foreach (var p in tokens)
            {
                yield return p;
            }
            Util.LogText(String.Format("INCLUDED: \"{0}\".", path), true);
        }

        protected override T BuildRepresentation(Tuple<string, string>[] tokens)
        {
            T ret = new T();
            Dictionary<string, string> Macros = new Dictionary<string, string>();
            List<Tuple<string, string>> token_list = new List<Tuple<string, string>>(tokens);
            var cur_dir = Parser.CurrentDirectory;
            while (token_list.Count > 0)
            {
                // Grab first token
                var t = token_list.First();
                token_list.RemoveAt(0);
                // Divide it into left hand side and right hand side
                string lhs = t.Item1, rhs = t.Item2;
                // If the LHS is an INCLUDE directive
                if (lhs == "INCLUDE")
                {
                    // Tokenize the included file
                    var include = Include(Path.Combine(cur_dir, rhs));
                    // Put the generated tokens on the list with high priority
                    token_list.InsertRange(0, include);
                    continue;
                }
                // Otherwise, if the LHS is a macro
                else if (lhs.StartsWith("#"))
                {
                    // Add its expansion to Macros
                    Macros[lhs] = rhs;
                    continue;
                }

                // For each defined macro
                foreach (var define in Macros.Where(d => rhs.Contains(d.Key)))
                {
                    // Try to apply it to the rhs
                    rhs = t.Item2.Replace(define.Key, define.Value);
                }

                // Try to cast the rsh (a string) to an object of the correct type by calling DelegatedConversion.
                object rhs_obj = DelegatedConversion(this, rhs);
                if (rhs_obj == null)
                {
                    Util.Err(String.Format("Invalid right-hand side expression: \"{0}\" for type {1}", rhs, typeof(T).ToString()));
                    continue;
                }

                // Identifiers can be dictionary accesses or simple values
                bool property_set = false;
                if (t.Item1.Contains("[")) // It's a dictionary entry
                {
                    var dict_name = t.Item1.Substring(0, lhs.IndexOf('['));
                    var dict_index = t.Item1.Substring(lhs.IndexOf('[') + 1, lhs.IndexOf(']') - lhs.IndexOf('[') - 1);

                    property_set = TrySetProperty(ret, dict_name, rhs_obj, dict_index);
                }
                else
                {
                    property_set = TrySetProperty(ret, lhs, rhs_obj, null);
                }
                if (!property_set) Util.Err(String.Format("Unknown property: \"{0}\" for type {1}", lhs, typeof(T).ToString()));
            }
            return ret;
        }
    }

    public class FontConfigInfo
    {
        public Font MainFont { get; set; }
        public Dictionary<string, Font> Spritesheets { get; set; } = new Dictionary<string, Font>();
    }

    public class FontConfigLoader : ReflectionBasedConfigLoader<FontConfigInfo>
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

        public int RegionWidth { get; set; } = 80;
        public int RegionHeight { get; set; } = 80;

        public int FPSCap { get; set; } = 60;

        public string DefaultFontPath { get; set; } = "gfx/Taffer.font";
        public string Locale { get; set; } = "cfg/locale/en_US.cfg";

        public Dictionary<string, string> Suppress { get; set; } = new Dictionary<string, string>();

        public int? RngSeed { get; set; } = null;
    }

    public class InitConfigLoader : ReflectionBasedConfigLoader<InitConfigInfo>
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
                if (ContainsKey(key))
                {
                    K value;
                    TryGetValue(key, out value);
                    return value;
                }
                K def = DefaultValueDelegate(key);
                Add(key, def);
                return def;
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

    public class LocalizationLoader : ReflectionBasedConfigLoader<LocalizationInfo>
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

    public class KeybindConfigLoader : ReflectionBasedConfigLoader<KeybindConfigInfo>
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

            if (ret.MainKey == Keys.None)
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

    public class PaletteConfigLoader : ReflectionBasedConfigLoader<PaletteConfigInfo>
    {
        static Regex RGBValueRegex = new Regex("(?:([\\d]{1,3}),?\\s*)", RegexOptions.Compiled);
        static Regex RGBAParserRegex = new Regex(RGBValueRegex.ToString().Repeat(4) + "?", RegexOptions.Compiled);

        private static Color ParseRhs(string rhs)
        {
            if (RGBAParserRegex.IsMatch(rhs))
            {
                var m = RGBAParserRegex.Match(rhs);
                int R = 0, G = 0, B = 0, A = 255;
                R = int.Parse(m.Groups[1].Value);
                G = int.Parse(m.Groups[2].Value);
                B = int.Parse(m.Groups[3].Value);
                if (m.Groups.Count == 5 && m.Groups[4].Value.Length > 0)
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
