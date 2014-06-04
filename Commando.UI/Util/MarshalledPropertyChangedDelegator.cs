using System;
using System.ComponentModel;
using twomindseye.Commando.API1;
using twomindseye.Commando.API1.Extension;

namespace twomindseye.Commando.UI.Util
{
    public sealed class MarshalledPropertyChangedDelegator : MarshalByRefObject, INotifyPropertyChanged
    {
        public void HandlePropertyChanged(object sender, SerializablePropertyChangedEventArgs e)
        {
            var ev = PropertyChanged;

            if (ev != null)
            {
                ev(this, new PropertyChangedEventArgs(e.PropertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}