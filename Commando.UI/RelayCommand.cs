using System;
using System.Windows.Input;

namespace twomindseye.Commando.UI
{
    public class RelayCommand<TExecuteParam> : ICommand
    {
        Action<TExecuteParam> _execute;
        Func<TExecuteParam, bool> _canExecute;

        public RelayCommand(Action<TExecuteParam> execute, Func<TExecuteParam, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = x => execute();

            if (canExecute != null)
            {
                _canExecute = x => canExecute();
            }
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute((TExecuteParam)parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _execute((TExecuteParam)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            var e = CanExecuteChanged;

            if (e != null)
            {
                e(this, null);
            }
        }
    }

    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
            : base(execute, canExecute)
        {
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : base(execute, canExecute)
        {
        }
    }
}
