using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using twomindseye.Commando.UI.Util;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.UI.Controls
{
    public partial class KeyMenuControl
    {
        KeyMenuContext _context;

        public KeyMenuControl()
        {
            IsVisibleChanged += OnIsVisibleChanged;

            ToggleExtraCommand = new RelayCommand(() => ShowExtraCommands = !ShowExtraCommands);

            InitializeComponent();

            Resources.GetResource<DelegatingMultiValueConverter>("ItemVisibilityConverter").SetConvertFunc<string, bool, bool>(GetItemVisibility);
        }

        static object GetItemVisibility(string actionText, bool isExtra, bool showExtraCommands, object converterParameter)
        {
            return (!string.IsNullOrWhiteSpace(actionText) && (!isExtra || showExtraCommands))
                       ? Visibility.Visible
                       : Visibility.Collapsed;
        }

        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _context = !IsVisible ? null : KeyMenuContext.Find(this);
            _items.ItemsSource = _context == null ? null : _context.KeyInfoList;
        }

        public static readonly DependencyProperty ShowExtraCommandsProperty =
            DependencyProperty.Register("ShowExtraCommands", typeof (bool), typeof (KeyMenuControl), 
            new PropertyMetadata(default(bool)));

        public bool ShowExtraCommands
        {
            get
            {
                return (bool) GetValue(ShowExtraCommandsProperty);
            }
            set
            {
                SetValue(ShowExtraCommandsProperty, value);
            }
        }

        public ICommand ToggleExtraCommand { get; private set; }
    }
}
