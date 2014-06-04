using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine;

namespace twomindseye.Commando.UI.ViewModels
{
    class EditCommandViewModel : ViewModelBase
    {
        readonly List<EditCommandArgumentViewModel> _argumentViewModels;

        public EditCommandViewModel(CommandExecutor executor, bool allowPartialExecutors)
        {
            OriginalExecutor = executor;
            
            _argumentViewModels = executor.Arguments
                .Select((x, i) => new EditCommandArgumentViewModel(executor, i, allowPartialExecutors))
                .ToList();

            ArgumentEditors = new ReadOnlyCollection<EditCommandArgumentViewModel>(_argumentViewModels);

            ExecuteCommand = new RelayCommand(Execute, () => CreateFinalExecutor() != null);
            
            AliasFacetCommand = new RelayCommand<FacetMoniker>(
                moniker => Messenger.Default.Send(new AliasFacetMessage(moniker), this), 
                moniker => moniker != null);

            AliasCommandCommand = new RelayCommand(
                () => Messenger.Default.Send(new AliasCommandMessage(CreateFinalExecutor()), this),
                () => CreateFinalExecutor() != null);
        }

        void Execute()
        {
            var executor = CreateFinalExecutor();

            try
            {
                executor.Invoke();

                Messenger.Default.Send(new CommandExecutedMessage(executor));
            }
            catch (CommandExecutionException ex)
            {
                Messenger.Default.Send(new CommandExecutedMessage(executor, ex));
            }
        }

        public CommandExecutor OriginalExecutor { get; private set; }
        public ReadOnlyCollection<EditCommandArgumentViewModel> ArgumentEditors { get; private set; }

        public ICommand AliasCommandCommand { get; private set; }
        public RelayCommand<FacetMoniker> AliasFacetCommand { get; private set; } 
        public ICommand ExecuteCommand { get; private set; }

        public CommandExecutor CreateFinalExecutor()
        {
            try
            {
                return OriginalExecutor.CreateSuggestedVersion(ArgumentEditors.Select(x => x.SelectedTuple).ToArray());
            }
            catch
            {
                return null;
            }
        }

        public sealed class AliasCommandMessage
        {
            public AliasCommandMessage(CommandExecutor executor)
            {
                Executor = executor;
            }

            public CommandExecutor Executor { get; private set; }
        }

        public sealed class AliasFacetMessage
        {
            public AliasFacetMessage(FacetMoniker moniker)
            {
                Moniker = moniker;
            }

            public FacetMoniker Moniker { get; private set; }
        }
    }
}