using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Parse;

namespace twomindseye.Commando.Engine
{
    public sealed class CommandGeneratorResult
    {
        readonly List<RequiresConfigurationException> _rcExceptions;
        readonly ReadOnlyCollection<RequiresConfigurationException> _rcExceptionsColl;
        readonly List<CommandExecutor> _commands;
        readonly ReadOnlyCollection<CommandExecutor> _commandsColl;

        internal CommandGeneratorResult()
        {
            _rcExceptions = new List<RequiresConfigurationException>();
            _rcExceptionsColl = new ReadOnlyCollection<RequiresConfigurationException>(_rcExceptions);
            _commands = new List<CommandExecutor>();
            _commandsColl = new ReadOnlyCollection<CommandExecutor>(_commands);
        }

        internal void AddRCException(RequiresConfigurationException ex)
        {
            if (!_rcExceptions.Contains(ex))
            {
                _rcExceptions.Add(ex);
            }
        }

        internal void AddExecutor(CommandExecutor executor)
        {
            _commands.Add(executor);
        }

        public ReadOnlyCollection<RequiresConfigurationException> RequiresConfigurationExceptions
        {
            get
            {
                return _rcExceptionsColl;
            }
        }

        public ReadOnlyCollection<CommandExecutor> Commands
        {
            get
            {
                return _commandsColl;
            }
        }
    }
}