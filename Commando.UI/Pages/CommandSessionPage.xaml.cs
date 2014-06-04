using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using twomindseye.Commando.Engine.Load;
using twomindseye.Commando.UI.ViewModels;

namespace twomindseye.Commando.UI.Pages
{
    public partial class CommandSessionPage
    {
        readonly CommandSessionViewModel _viewModel;

        public CommandSessionPage()
        {
            DataContext = _viewModel = new CommandSessionViewModel(Dispatcher);

            Messenger.Default.Register<EditExecutorMessage>(this, _viewModel, msg => NavigationService.Navigate(new EditCommandPage(msg.Executor)));
            Messenger.Default.Register<CommandRequiresConfigurationMessage>(this, _viewModel, ConfigureCommand);

            InitializeComponent();

            _commandsList.SelectionChanged += CommandsList_SelectionChanged;
            _commandsList.SizeChanged += CommandsList_SizeChanged;

            Loaded += CommandSessionControl_Loaded;
        }

        void ConfigureCommand(CommandRequiresConfigurationMessage msg)
        {
            var rce = msg.RequiresConfigurationException;

            var lt = Loader.ConfiguratorTypes.FirstOrDefault(x => x.Type == rce.ConfiguratorType);

            var configPage = new ConfiguratorSetPage();
            configPage.ConfiguratorTypes.Add(new ConfiguratorSetPageItem(lt, rce.Description));
            configPage.Return += ConfigPageReturn;

            NavigationService.Navigate(configPage);
        }

        void ConfigPageReturn(object sender, ReturnEventArgs<bool> e)
        {
            if (e.Result)
            {
                _viewModel.ExecuteCommand.Execute(_viewModel.SelectedCommand);
            }
        }

        void CommandSessionControl_Loaded(object sender, RoutedEventArgs e)
        {
            _commandText.Text = "pawel 10:30";
        }

        void CommandsList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_commandsList.SelectedItem != null)
            {
                _commandsList.ScrollIntoView(_commandsList.SelectedItem);
            }
        }

        void CommandsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _commandsList.ScrollIntoView(_commandsList.SelectedItem);
        }
    }
}
