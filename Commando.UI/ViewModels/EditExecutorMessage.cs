using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using twomindseye.Commando.Engine;

namespace twomindseye.Commando.UI.ViewModels
{
    sealed class EditExecutorMessage
    {
        public EditExecutorMessage(CommandExecutor executor)
        {
            Executor = executor;
        }

        public CommandExecutor Executor { get; private set; }
    }
}
