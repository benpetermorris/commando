using System;
using System.Windows.Input;

namespace twomindseye.Commando.UI.Controls
{
    interface IConfirmableClient
    {
        ICommand ConfirmCommand { get; }
        event EventHandler<EventArgs> Finished;
    }
}
