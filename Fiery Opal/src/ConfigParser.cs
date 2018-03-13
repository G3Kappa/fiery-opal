using FieryOpal.Src;
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
        protected static Regex AssignmentRegex = new Regex(@"^\s*(.*?)=(.*)\s*$");

        protected override Tuple<string, string> ParseLine(string s)
        {
            if (CommentRegex.IsMatch(s)) return null;
            if (!AssignmentRegex.IsMatch(s))
            {
                Util.Err(String.Format("{0}:{1}> Invalid expression or assignment.", CurrentFile, CurrentLine));
                return null;
            }

            var matches = AssignmentRegex.Matches(s);
            return new Tuple<string, string>(matches[0].Groups[1].Value, matches[0].Groups[2].Value);
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
            foreach (var t in tokens)
            {
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
            : base((self, lhs) => Global.LoadFont(Path.Combine(self.Parser.CurrentDirectory, lhs)).GetFont(Font.FontSizes.One))
        {

        }
    }

    public class InitConfigInfo
    {
        public int ProgramWidth { get; set; }
        public int ProgramHeight { get; set; }

        public int WorldWidth { get; set; }
        public int WorldHeight { get; set; }

        public Dictionary<string, int> TestInt { get; set; } = new Dictionary<string, int>();
        public Dictionary<int, bool> TestBool { get; set; } = new Dictionary<int, bool>();
    }

    internal class InitConfigLoader : RelectionBasedConfigLoader<InitConfigInfo>
    {
        public InitConfigLoader()
            : base((self, lhs) => lhs)
        {

        }
    }
}
