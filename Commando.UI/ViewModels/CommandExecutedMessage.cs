using twomindseye.Commando.API1;
using twomindseye.Commando.Engine;

namespace twomindseye.Commando.UI.ViewModels
{
    class CommandExecutedMessage
    {
        public CommandExecutedMessage(CommandExecutor executor, CommandExecutionException exception = null)
        {
            Executor = executor;
            Exception = exception;
        }

        public CommandExecutor Executor { get; private set; }
        public CommandExecutionException Exception { get; private set; }
    }
}
