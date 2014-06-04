using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace twomindseye.Commando.UI.Util
{
    [ContentProperty]
    public class CommandKeyTrigger : KeyTrigger
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof (ICommand), typeof (CommandKeyTrigger), 
                                        new PropertyMetadata(default(ICommand), OnCommandChanged));

        public ICommand Command
        {
            get
            {
                return (ICommand) GetValue(CommandProperty);
            }
            set
            {
                SetValue(CommandProperty, value);
            }
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof (object), typeof (CommandKeyTrigger), 
                                        new PropertyMetadata(default(object), OnCommandParameterChanged));

        public object CommandParameter
        {
            get
            {
                return GetValue(CommandParameterProperty);
            }
            set
            {
                SetValue(CommandParameterProperty, value);
            }
        }

        static void OnCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CommandKeyTrigger)d).CommandCanExecuteChanged(null, null);
        }

        static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ckt = (CommandKeyTrigger) d;

            var oldCmd = e.OldValue as ICommand;

            if (oldCmd != null)
            {
                oldCmd.CanExecuteChanged -= ckt._ccecHandler;
            }

            var newCmd = e.NewValue as ICommand;

            if (newCmd != null)
            {
                newCmd.CanExecuteChanged += ckt._ccecHandler;
            }

            ckt.CommandCanExecuteChanged(null, null);
        }

        readonly EventHandler _ccecHandler;

        public CommandKeyTrigger()
        {
            _ccecHandler = CommandCanExecuteChanged;
        }

        void CommandCanExecuteChanged(object sender, EventArgs e)
        {
            IsEnabled = Command != null && Command.CanExecute(CommandParameter);
        }

        protected override void InvokeActionsNew(KeyEventArgs eventArgs)
        {
            if (Command != null && Command.CanExecute(CommandParameter))
            {
                Command.Execute(CommandParameter);
            }
        }
    }
}