using System;

namespace twomindseye.Commando.Engine
{
    [Serializable]
    public sealed class CommandExecutionException : Exception
    {
        public CommandExecutionException(string message) : base(message)
        {
        }

        public CommandExecutionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CommandExecutionException(Exception innerException) : base("The command failed to execute.", innerException)
        {
        }
    }
}
