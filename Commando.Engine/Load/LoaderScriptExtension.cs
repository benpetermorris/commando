using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Jint;
using twomindseye.Commando.API1.Commands;
using twomindseye.Commando.API1.Extension;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine.DB;
using twomindseye.Commando.Engine.Extension;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.Engine.Load
{
    public sealed class LoaderScriptExtension : LoaderExtension
    {
        readonly string _script;
        readonly Usings _usings;
        readonly List<ScriptedCommand> _commands;
        readonly List<AssemblyName> _imports;
        readonly ScriptedCommandContainer _container;

        internal LoaderScriptExtension(string path)
            : base(path)
        {
            _script = File.ReadAllText(Path);
            _usings = new Usings();
            _commands = new List<ScriptedCommand>();
            _imports = new List<AssemblyName>();
            _container = new ScriptedCommandContainer(this);

            ExtensionStore.InitExtension(this);

            Description = System.IO.Path.GetFileName(Path);
            Load();

            foreach (var command in _commands)
            {
                var lcInfo = new LoaderCommand(this, command);
                command.LoaderCommand = lcInfo;
                AddItem(lcInfo);
            }

            var lccInfo = new LoaderCommandContainer(this, _container, typeof(ScriptedCommandContainer));
            ExtensionStore.InitCommandContainer(lccInfo, Items.Cast<LoaderCommand>());
            AddItem(lccInfo);
        }

        void Load()
        {
            var gotOtherLines = false;

            using (var reader = File.OpenText(Path))
            {
                var lines = ScriptReader.ReadLines(reader);

                foreach (var line in lines)
                {
                    if (line.Text.StartsWith("///"))
                    {
                        var sectionName = line.Text.Substring(3).Trim();
                        var sectionLines = ScriptReader.ReadSection(lines);

                        switch (sectionName)
                        {
                            case "extension":
                                LoadExtension(sectionLines);
                                break;
                            case "imports":
                                if (gotOtherLines)
                                {
                                    throw new InvalidOperationException("imports section must appear before any commands or script content");
                                }
                                LoadImports(sectionLines);
                                if (MissingDependencies.Count > 0)
                                {
                                    return;
                                }
                                break;
                            case "usings":
                                if (gotOtherLines || _commands.Count > 0)
                                {
                                    throw new InvalidOperationException("usings section must appear before any commands or script content");
                                }
                                if (_imports.Count == 0)
                                {
                                    throw new InvalidOperationException("imports section must appear before usings");
                                }
                                LoadUsings(sectionLines);
                                break;
                            case "command":
                                LoadCommand(sectionLines);
                                break;
                        }
                    }
                    else if (line.Text != "")
                    {
                        gotOtherLines = true;
                    }
                }
            }
        }

        void LoadImports(IEnumerable<ScriptReader.Line> sectionLines)
        {
            foreach (var line in sectionLines)
            {
                AssemblyName assemblyName;

                try
                {
                    assemblyName = new AssemblyName(line.Text);
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Could not parse import line: " + line.Text);
                }

                if (!TypeDescriptor.GetAssemblyNames().Contains(assemblyName, TypeDescriptor.AssemblyNameComparer))
                {
                    AddMissingDependency(assemblyName);
                }
                else
                {
                    _imports.Add(assemblyName);
                }
            }
        }

        void LoadExtension(IEnumerable<ScriptReader.Line> sectionLines)
        {
            foreach (var line in sectionLines)
            {
                var key = line.Text.TakeWhile(x => !char.IsWhiteSpace(x)).CharsToString();
                var value = line.Text.Substring(key.Length).Trim();

                switch (key)
                {
                    case "description":
                        Description = value;
                        break;
                    default:
                        throw new InvalidOperationException("Unknown extension section key: " + key);
                }
            }
        }

        void LoadUsings(IEnumerable<ScriptReader.Line> sectionLines)
        {
            foreach (var line in sectionLines)
            {
                var split = line.Text.Split('=');

                if (split.Length == 2)
                {
                    _usings.Add(new Using(split[1].Trim(), split[0].Trim()));
                }
                else
                {
                    _usings.Add(new Using(split[0].Trim()));
                }
            }
        }

        void LoadCommand(IEnumerable<ScriptReader.Line> sectionLines)
        {
            var command = new CommandLoadInfo();
            ParameterLoadInfo currentParameter = null;
            var gotKeys = new HashSet<string>();
            var inParams = false;

            foreach (var line in sectionLines)
            {
                var key = line.Text.TakeWhile(x => !char.IsWhiteSpace(x)).CharsToString().ToLower();
                var value = line.Text.Substring(key.Length).Trim();

                if (gotKeys.Contains(key))
                {
                    throw new InvalidOperationException("Duplicate command section key: " + key);
                }

                if (key == "param")
                {
                    currentParameter = new ParameterLoadInfo();
                    gotKeys.Clear();
                    currentParameter.Name = value;
                    command.Parameters.Add(currentParameter);
                    continue;
                }

                if (key.StartsWith("param"))
                {
                    inParams = true;

                    if (currentParameter == null)
                    {
                        throw new InvalidOperationException("command section key must be 'param'");
                    }
                }
                else if (inParams)
                {
                    throw new InvalidOperationException("all non-parameter keys must come before parameter keys");
                }

                gotKeys.Add(key);

                switch (key)
                {
                    case "function":
                        command.FunctionName = value;
                        break;
                    case "title":
                        command.Title = value;
                        break;
                    case "aliases":
                        command.Aliases = value.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                        break;
                    case "paramoptional":
                        currentParameter.Optional = value == "true";
                        break;
                    case "paramtype":
                        var query = from testName in _usings.EnumerateTestNames(value)
                                    from assemblyName in _imports
                                    let descriptor = TypeDescriptor.TryGet(testName, assemblyName)
                                    where descriptor != null
                                    select descriptor;

                        currentParameter.Type = query.FirstOrDefault();

                        if (currentParameter.Type == null)
                        {
                            throw new InvalidOperationException("Could not resolve paramtype: " + value);
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Unknown command section key: " + key);
                }
            }

            _commands.Add(new ScriptedCommand(_container, command));
        }

        protected override void UnloadImpl()
        {
        }

        public override bool OnLoadCompleting(IList<LoaderExtension> extensionSet)
        {
            return true;
        }

        internal sealed class ParameterLoadInfo
        {
            public ParameterLoadInfo()
            {
                Filters = new List<FilterExtraDataAttribute>();
            }

            public string Name { get; set; }
            public List<FilterExtraDataAttribute> Filters { get; private set; }
            public bool Optional { get; set; }
            public TypeDescriptor Type { get; set; }
        }

        internal sealed class CommandLoadInfo
        {
            public CommandLoadInfo()
            {
                Parameters = new List<ParameterLoadInfo>();
            }

            public string FunctionName { get; set; }
            public string Title { get; set; }
            public string[] Aliases { get; set; }
            public List<ParameterLoadInfo> Parameters { get; private set; }
        }

        sealed class ScriptedCommandContainer : CommandContainer
        {
            readonly LoaderScriptExtension _extension;

            public ScriptedCommandContainer(LoaderScriptExtension extension)
            {
                _extension = extension;
            }

            public void Initialize(IExtensionHooks extensionHooks)
            {
            }

            public void Invoke(string methodName, object[] parameters)
            {
                var jint = new JintEngine();
                jint.DisableSecurity();
                jint.Visitor.Usings.CopyFrom(_extension._usings);
                jint.SetParameter("ExtensionHooks", _extension.Hooks);
                jint.Run(_extension._script);
                jint.CallFunction(methodName, parameters);
            }
        }
        
        static class ScriptReader
        {
            public sealed class Line
            {
                public string Text { get; private set; }
                public int Number { get; private set; }

                public Line(string text, int number)
                {
                    Text = text;
                    Number = number;
                }
            }

            public static IEnumerable<Line> ReadLines(TextReader reader)
            {
                var line = 0;
                string text;

                while ((text = reader.ReadLine()) != null)
                {
                    yield return new Line(text.Trim(), ++line);
                }
            }

            public static IEnumerable<Line> ReadSection(IEnumerable<Line> lines)
            {
                foreach (var line in lines)
                {
                    if (line.Text == "")
                    {
                        yield break;
                    }

                    if (!line.Text.StartsWith("///"))
                    {
                        throw new InvalidOperationException(String.Format("Line {0}: expected /// or a blank line to end the section", line.Number));
                    }

                    yield return new Line(line.Text.Substring(3).Trim(), line.Number);
                }
            }
        }
    }
}