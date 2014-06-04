using System;

namespace twomindseye.Commando.API1.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Serializable]
    public sealed class CommandParameterAttribute : APIAttribute
    {
        public CommandParameterAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
        public bool Optional { get; set; }
    }
}
