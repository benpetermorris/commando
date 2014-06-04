using System.Text.RegularExpressions;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using twomindseye.Commando.API1.Facets;
using twomindseye.Commando.Engine;
using twomindseye.Commando.UI.ViewModels;

namespace twomindseye.Commando.UI.Pages
{
    /// <summary>
    /// Interaction logic for EditCommandControl.xaml
    /// </summary>
    public partial class EditCommandPage
    {
        EditCommandViewModel _viewModel;

        public EditCommandPage(CommandExecutor executor)
        {
            DataContext = _viewModel = new EditCommandViewModel(executor, false);

            Messenger.Default.Register<EditCommandViewModel.AliasCommandMessage>(this, _viewModel, OnAliasCommand);
            Messenger.Default.Register<EditCommandViewModel.AliasFacetMessage>(this, _viewModel, OnAliasFacet);

            InitializeComponent();
        }

        void OnAliasFacet(EditCommandViewModel.AliasFacetMessage msg)
        {
            // TODO: validation
            var page = new TextInputPage(
                "Enter an alias for the facet", 
                "", 
                str => Regex.IsMatch(str, "^[A-Za-z0-9]{0,16}$"),
                msg.Moniker);

            page.Return += OnAliasFacetPageReturn;

            NavigationService.Navigate(page);
        }

        static void OnAliasFacetPageReturn(object sender, ReturnEventArgs<string> e)
        {
            var page = (TextInputPage)sender;
            PersistenceUtil.SetFacetMonikerAlias((FacetMoniker)page.UserValue, e.Result);
        }

        void OnAliasCommand(EditCommandViewModel.AliasCommandMessage msg)
        {
            // TODO: validation
            var page = new TextInputPage(
                "Enter an alias for the command",
                "",
                str => Regex.IsMatch(str, "^[A-Za-z0-9]{0,16}$"),
                msg.Executor);

            page.Return += OnAliasCommandPageReturn;

            NavigationService.Navigate(page);
        }

        static void OnAliasCommandPageReturn(object sender, ReturnEventArgs<string> e)
        {
            var page = (TextInputPage)sender;
            PersistenceUtil.AliasPartialCommand((CommandExecutor)page.UserValue, e.Result);
        }
    }
}
