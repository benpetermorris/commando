using System;

namespace twomindseye.Commando.API1.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    [Serializable]
    public sealed class CommandAttribute : APIAttribute
    {
        public CommandAttribute(string name)
        {
            Aliases = string.Empty;
            Name = name;
        }

        public string Name { get; private set; }
        public string GetIconMethodName { get; set; }
        public string Aliases { get; set; }
        public string[] AliasesSplit
        {
            get { return Aliases.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); }
        }
    }
}
