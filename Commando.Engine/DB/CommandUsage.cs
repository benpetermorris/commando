using System;
using twomindseye.Commando.Engine.Extension;

namespace twomindseye.Commando.Engine.DB
{
    sealed class CommandUsage
    {
        public CommandUsage(CommandExecutor executor, DateTime at)
        {
            Executor = executor;
            Command = Executor.Command;
            At = at;
        }

        public CommandUsage(Command command, DateTime at)
        {
            Command = command;
            At = at;
        }

        public Command Command { get; private set; }
        public CommandExecutor Executor { get; private set; }
        public DateTime At { get; private set; }
    }
}