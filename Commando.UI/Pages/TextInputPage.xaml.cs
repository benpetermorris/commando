using System;
using System.Windows.Input;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Command;

namespace twomindseye.Commando.UI.Pages
{
    public partial class TextInputPage
    {
        readonly Func<string, bool> _validate;
        readonly ViewModel _viewModel;

        public TextInputPage(string prompt, string text, Func<string, bool> validate, object userValue)
        {
            UserValue = userValue;
            _validate = validate;
            _viewModel = new ViewModel(prompt, text, new RelayCommand(SaveAndReturn));
            DataContext = _viewModel;
            InitializeComponent();
        }

        public object UserValue { get; private set; }

        void SaveAndReturn()
        {
            if (_validate != null && !_validate(_viewModel.Text))
            {
                return;
            }

            OnReturn(new ReturnEventArgs<string>(_viewModel.Text));
        }

        public sealed class ViewModel
        {
            public ViewModel(string prompt, string text, ICommand saveCommand)
            {
                Prompt = prompt;
                Text = text;
                SaveCommand = saveCommand;
            }

            public string Prompt { get; private set; }
            public string Text { get; set; }
            public ICommand SaveCommand { get; private set; }
        }
    }
}
