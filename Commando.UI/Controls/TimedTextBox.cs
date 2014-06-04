using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace twomindseye.Commando.UI.Controls
{
    class TimedTextBox : TextBox
    {
        readonly DispatcherTimer _timer;
        string _lastEventText;
        DateTime _textLastModifiedAt;

        public TimedTextBox()
        {
            TextChanged += CommandTextBoxTextChanged;

            _textLastModifiedAt = DateTime.MinValue;

            _timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
            _timer.Interval = TimeSpan.FromMilliseconds(250);
            _timer.Tick += TimerTick;
            _timer.Start();
        }

        public double EventIntervalSeconds
        {
            get { return (double)GetValue(EventIntervalSecondsProperty); }
            set { SetValue(EventIntervalSecondsProperty, value); }
        }

        public static readonly DependencyProperty EventIntervalSecondsProperty =
            DependencyProperty.Register("EventIntervalSeconds", typeof(double), typeof(TimedTextBox), 
            new UIPropertyMetadata(0.7));

        public string TimedText
        {
            get { return (string)GetValue(TimedTextProperty); }
            set { SetValue(TimedTextProperty, value); }
        }

        public static readonly DependencyProperty TimedTextProperty =
            DependencyProperty.Register("TimedText", typeof(string), typeof(TimedTextBox), 
            new UIPropertyMetadata(null));

        public event EventHandler<TimedTextChangedEventArgs> TimedTextChanged;

        void CommandTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_lastEventText != Text)
            {
                _textLastModifiedAt = DateTime.Now;
            }
        }

        void TimerTick(object sender, EventArgs e)
        {
            if (DateTime.Now.Subtract(_textLastModifiedAt).TotalSeconds <= EventIntervalSeconds || _lastEventText == Text)
            {
                return;
            }

            TimedText = Text;

            _lastEventText = Text;

            var ev = TimedTextChanged;

            if (ev != null)
            {
                ev(this, new TimedTextChangedEventArgs(_lastEventText, CaretIndex));
            }
        }
    }
}
