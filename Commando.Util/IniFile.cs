using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace twomindseye.Commando.Util
{
    public sealed class IniFile
    {
        static readonly Regex s_sectionRegex = new Regex(@"\[(\w+)\]");
        static readonly Regex s_keyvalRegex = new Regex(@"(\S+?)=(.*)");

        Dictionary<string, Dictionary<string, string>> _data;

        public IniFile()
        {
            _data = new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);
        }

        public static IniFile LoadFrom(string fileName)
        {
            using (var reader = File.OpenText(fileName))
            {
                return LoadFrom(reader);
            }
        }

        public static IniFile LoadFrom(TextReader reader)
        {
            var rvl = new IniFile();
            rvl.Load(reader);
            return rvl;
        }

        void Load(TextReader reader)
        {
            string currentSection = null;
            string line = null;
            var lineNumber = 0;

            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;

                line = line.Trim();

                if (line.StartsWith(";") || line.StartsWith("#") || line == "")
                {
                    continue;
                }

                var sectionMatch = s_sectionRegex.Match(line);

                if (sectionMatch.Success)
                {
                    currentSection = sectionMatch.Groups[1].Value;
                    _data[currentSection] = new Dictionary<string, string>();
                    
                    continue;
                }
                
                if (currentSection == null)
                {
                    throw new ArgumentException("expected a section on line " + lineNumber);
                }

                var kvMatch = s_keyvalRegex.Match(line);

                if (kvMatch.Success)
                {
                    _data[currentSection][kvMatch.Groups[1].Value] = kvMatch.Groups[2].Value;
                }
                else
                {
                    throw new ArgumentException("expected a keyvalue pair on line " + lineNumber);
                }
            }
        }

        public string this[string section, string key, bool optional = false]
        {
            get
            {
                Dictionary<string, string> keyValues;

                if (!_data.TryGetValue(section, out keyValues))
                {
                    if (optional)
                    {
                        return null;
                    }

                    throw new ArgumentException("did not find a [section] named " + section);
                }

                string value = null;

                if (!keyValues.TryGetValue(key, out value))
                {
                    if (optional)
                    {
                        return null;
                    }

                    throw new ArgumentException("missing a value for key " + key + " in section " + section);
                }

                return value;
            }
            set
            {
                Dictionary<string, string> keyValues;

                if (!_data.TryGetValue(section, out keyValues))
                {
                    if (value == null)
                    {
                        return;
                    }

                    _data[section] = keyValues = new Dictionary<string, string>();
                }

                if (value == null)
                {
                    keyValues.Remove(key);

                    return;
                }

                keyValues[key] = value;
            }
        }

        public IEnumerable<string> GetKeys(string section, bool optional = false)
        {
            Dictionary<string, string> keyValues;

            if (!_data.TryGetValue(section, out keyValues))
            {
                if (optional)
                {
                    return null;
                }

                throw new ArgumentException("did not find a [section] named " + section);
            }

            return keyValues.Keys.AsEnumerable();
        }

        public IEnumerable<string> Sections
        {
            get
            {
                return _data.Keys.AsEnumerable();
            }
        }

        public void ValidateKeyNames(string section, params string[] validKeyNames)
        {
            var invalid = _data[section].Keys.Except(validKeyNames).ToArray();

            if (invalid.Length > 0)
            {
                // exception type is clearly a hack
                throw new InvalidOperationException(
                    string.Format(
                        "Invalid key(s) in section '{0}': {1}",
                        section,
                        string.Join(", ", invalid)));
            }
        }
    }
}
