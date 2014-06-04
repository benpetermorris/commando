using System;

namespace twomindseye.Commando.API1.Extension
{
    [Serializable]
    public sealed class ConfiguratorValidationException : Exception
    {
        public ConfiguratorValidationException(string message, string propertyName)
            : base(message)
        {
            PropertyName = propertyName;
        }

        public ConfiguratorValidationException(string message)
            : base(message)
        {
        }

        public ConfiguratorValidationException(string message, string propertyName, Exception innerException) : base(message, innerException)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; private set; }
    }
}
