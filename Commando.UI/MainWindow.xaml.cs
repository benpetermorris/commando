using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using twomindseye.Commando.Engine.Load;
using twomindseye.Commando.UI.Controls;
using twomindseye.Commando.UI.Pages;
using twomindseye.Commando.UI.Util;
using twomindseye.Commando.UI.ViewModels;

namespace twomindseye.Commando.UI
{
    /// <summary>
    /// Interaction logic for CommandWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        readonly double[] _scales = {1.0, 1.25, 1.5, 1.75};
        readonly double[] _heights = {300, 400, 500};
        Canvas _canvas;
        Grid _mainGrid;
        Grid _contentGrid;
        Canvas _contentCanvas;
        ScaleTransform _scaleTrans;
        int _scaleIndex;
        int _heightIndex;
        UIElement _lastPage;
        NavigationMode _lastMode;

        public MainWindow()
        {
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            ThreadPool.QueueUserWorkItem(LoadEngine);

            ScaleUpCommand = new RelayCommand(() => SetScaleIndex(_scaleIndex + 1), () => _scaleIndex < _scales.Length - 1);
            ScaleDownCommand = new RelayCommand(() => SetScaleIndex(_scaleIndex - 1), () => _scaleIndex > 0);
            IncreaseHeightCommand = new RelayCommand(() => SetHeightIndex(_heightIndex + 1), () => _heightIndex < _heights.Length - 1);
            DecreaseHeightCommand = new RelayCommand(() => SetHeightIndex(_heightIndex - 1), () => _heightIndex > 0);
            GoBackCommand = new RelayCommand(GoBack, () => CanGoBack);
            SettingsCommand = new RelayCommand(() => NavigationService.Navigate(new SettingsPage()), () => !(NavigationService.Content is SettingsPage));

            DataContext = this;

            InitializeComponent();

            Navigated += MainWindowNavigated;
            Navigating += MainWindow_Navigating;

            CommandBindings.Add(new CommandBinding(NavigationCommands.BrowseBack, (x, y) => { }));
            Messenger.Default.Register<CommandExecutedMessage>(this, OnCommandExecuted);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine(e.ExceptionObject.ToString());
            Application.Current.Shutdown();
        }

        void OnCommandExecuted(CommandExecutedMessage msg)
        {
            if (msg.Exception == null)
            {
                Close();
                return;
            }

            MessageBox.Show(this, msg.Exception.Message, "Commando", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _canvas = (Canvas) GetTemplateChild("_canvas");
            _mainGrid = (Grid) GetTemplateChild("_mainGrid");
            _scaleTrans = (ScaleTransform) GetTemplateChild("_scaleTrans");
            _contentCanvas = (Canvas) GetTemplateChild("_contentCanvas");
            _contentGrid = (Grid) GetTemplateChild("_contentGrid");

            InitializeLayout();
            NavigationService.Navigate(new CommandSessionPage());
        }

        void InitializeLayout()
        {
            var primary = System.Windows.Forms.Screen.PrimaryScreen;
            Width = _canvas.Width = primary.Bounds.Width;
            Height = _canvas.Height = primary.Bounds.Height;
            Left = 0;
            Top = 0;
            Canvas.SetLeft(_mainGrid, (primary.Bounds.Width - _mainGrid.Width)/2);
            Canvas.SetTop(_mainGrid, primary.Bounds.Bottom - _mainGrid.Height*2);

            SetHeightIndex(_heightIndex);
            SetScaleIndex(_scaleIndex);
        }

        private static void LoadEngine(object notused)
        {
            var path = Path.Combine(
                Path.GetDirectoryName(Loader.EngineDirectory),
                "Extensions");

            Loader.AddExtensionDirectories(Directory.EnumerateDirectories(path));
        }

        void SetHeightIndex(int heightIndex)
        {
            _heightIndex = Math.Max(0, Math.Min(heightIndex, _heights.Length - 1));
            var oldHeight = _mainGrid.Height;
            _mainGrid.Height = _heights[_heightIndex];
            var diff = _mainGrid.Height - oldHeight;
            Canvas.SetTop(_mainGrid, Canvas.GetTop(_mainGrid) - diff);
        }

        void SetScaleIndex(int scaleIndex)
        {
            _scaleIndex = Math.Max(0, Math.Min(scaleIndex, _scales.Length - 1));
            _scaleTrans.ScaleX = _scales[_scaleIndex];
            _scaleTrans.ScaleY = _scales[_scaleIndex];
        }

        void MainWindow_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            _lastPage = (UIElement) Content;
            _lastMode = e.NavigationMode;
        }

        void MainWindowNavigated(object sender, NavigationEventArgs e)
        {
            GoBackCommand.RaiseCanExecuteChanged();

            var newCtrl = (UIElement)e.Content;

            BindingOperations.SetBinding(newCtrl, HeightProperty, Utility.CreateBinding(_contentGrid, "ActualHeight"));
            BindingOperations.SetBinding(newCtrl, WidthProperty, Utility.CreateBinding(_contentGrid, "ActualWidth"));
            RemoveLogicalChild(newCtrl);
            _contentCanvas.Children.Add(newCtrl);

            if (_lastPage == null)
            {
                return;
            }

            var moveRight = _lastMode == NavigationMode.Forward || _lastMode == NavigationMode.New;

            var ctx = KeyMenuContext.GetContext(this);
            ctx.PauseUpdates();

            var newDa = new DoubleAnimation();
            newDa.From = moveRight ? _contentGrid.ActualWidth : -_contentGrid.ActualWidth;
            newDa.To = 0;
            newDa.Duration = TimeSpan.FromSeconds(0.175);
            newDa.EasingFunction = new PowerEase();
            newDa.Completed += (s, e2) => ctx.ResumeUpdates();
            newCtrl.Visibility = Visibility.Visible;
            newCtrl.BeginAnimation(Canvas.LeftProperty, newDa);

            var oldDa = new DoubleAnimation();
            oldDa.From = 0;
            oldDa.To = -newDa.From;
            oldDa.Duration = newDa.Duration;
            oldDa.EasingFunction = new PowerEase();
            oldDa.Completed += (s, e2) => _contentCanvas.Children.Remove(_lastPage);
            _lastPage.BeginAnimation(Canvas.LeftProperty, oldDa);
        }

        public RelayCommand ScaleUpCommand { get; private set; }
        public RelayCommand ScaleDownCommand { get; private set; }
        public RelayCommand IncreaseHeightCommand { get; private set; }
        public RelayCommand DecreaseHeightCommand { get; private set; }
        public RelayCommand GoBackCommand { get; private set; }
        public RelayCommand SettingsCommand { get; private set; } 
    }
}
