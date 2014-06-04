using System.ComponentModel;

namespace twomindseye.Commando.Engine.Load
{
    public abstract class LoaderItem : INotifyPropertyChanged
    {
        internal int DatabaseId { get; set; }

        protected void RaisePropertyChanged(string propertyName)
        {
            var e = PropertyChanged;

            if (e != null)
            {
                e(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}