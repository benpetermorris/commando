using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using twomindseye.Commando.API1;
using twomindseye.Commando.Engine;
using twomindseye.Commando.Engine.Load;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.UI.ViewModels
{
    class CommandSessionViewModel : ViewModelBase
    {
        readonly Dispatcher _dispatcher;
        string _commandText;
        ReadOnlyCollection<CommandExecutor> _generatedCommands;
        CommandExecutor _selectedCommand;

        public CommandSessionViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;

            Loader.LoadComplete += Loader_LoadComplete;

            ExecuteCommand = new RelayCommand<CommandExecutor>(ExecuteSelected, executor => executor != null);
            EditSelectedExecutorCommand = new RelayCommand<CommandExecutor>(EditSelected, executor => executor != null);

            SelectNextCommand = new RelayCommand(
                () => SelectedCommand = GetAdjacentCommand(true), 
                () => SelectedCommand != GetAdjacentCommand(true));
            SelectPreviousCommand = new RelayCommand(
                () => SelectedCommand = GetAdjacentCommand(false), 
                () => SelectedCommand != GetAdjacentCommand(false));
        }

        void EditSelected(CommandExecutor obj)
        {
            Messenger.Default.Send(new EditExecutorMessage(SelectedCommand), this);
        }

        void ExecuteSelected(CommandExecutor executor)
        {
            try
            {
                executor.Invoke();

                Messenger.Default.Send(new CommandExecutedMessage(executor));
            }
            catch (CommandExecutionException ex)
            {
                var rce = ex.InnerException as RequiresConfigurationException;

                if (rce != null)    
                {
                    Messenger.Default.Send(new CommandRequiresConfigurationMessage(executor, rce), this);

                    return;
                }

                Messenger.Default.Send(new CommandExecutedMessage(executor, ex));
            }
        }

        CommandExecutor GetAdjacentCommand(bool next)
        {
            var index = GeneratedCommands == null ? -1 : GeneratedCommands.IndexOf(SelectedCommand);

            if (index == -1)
            {
                return null;
            }

            if (next)
            {
                index = index == GeneratedCommands.Count - 1 ? index : index + 1;
            }
            else
            {
                index = index == 0 ? 0 : index - 1;
            }

            return GeneratedCommands[index];
        }

        void Loader_LoadComplete(object sender, EventArgs e)
        {
            if (CommandText != null)
            {
                ThreadPool.QueueUserWorkItem(notused => Generate(_commandText, -1));
            }
        }

        public string CommandText
        {
            get
            {
                return _commandText;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = null;
                }

                if (_commandText == value)
                {
                    return;
                }

                _commandText = value;
                RaisePropertyChanged("CommandText");

                if (_commandText == null)
                {
                    GeneratedCommands = null;
                    return;
                }

                ThreadPool.QueueUserWorkItem(notused => Generate(_commandText, -1));
            }
        }

        public ReadOnlyCollection<CommandExecutor> GeneratedCommands
        {
            get
            {
                return _generatedCommands;
            }
            set
            {
                _generatedCommands = value;
                RaisePropertyChanged("GeneratedCommands");
            }
        }

        public CommandExecutor SelectedCommand
        {
            get
            {
                return _selectedCommand;
            }
            set
            {
                _selectedCommand = value;
                RaisePropertyChanged("SelectedCommand");
            }
        }

        public ICommand SelectNextCommand { get; private set; }
        public ICommand SelectPreviousCommand { get; private set; }
        public RelayCommand<CommandExecutor> ExecuteCommand { get; private set; }
        public RelayCommand<CommandExecutor> EditSelectedExecutorCommand { get; private set; }

        void SetCommands(string currentText, ReadOnlyCollection<CommandExecutor> commands)
        {
            if (CommandText == currentText)
            {
                GeneratedCommands = commands;
                SelectedCommand = commands.Any() ? commands[0] : null;
            }
        }

        void Generate(string currentText, int caretIndex)
        {
            var result = CommandGenerator.GenerateCommands(currentText, caretIndex);
            var commands = CommandPredictor.ReorderCommands(result.Commands).ToList();
            var coll = new ReadOnlyCollection<CommandExecutor>(commands);
            _dispatcher.BeginInvoke(() => SetCommands(currentText, coll));
        }
    }

    class CommandRequiresConfigurationMessage
    {
        public CommandRequiresConfigurationMessage(CommandExecutor executor, RequiresConfigurationException rce)
        {
            Executor = executor;
            RequiresConfigurationException = rce;
        }

        public CommandExecutor Executor { get; private set; }
        public RequiresConfigurationException RequiresConfigurationException { get; private set; }
    }
}
