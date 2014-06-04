using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using Microsoft.Expression.Interactivity.Input;
using twomindseye.Commando.UI.Controls;

namespace twomindseye.Commando.UI.Util
{
    public class KeyTrigger : EventTriggerBase<UIElement>
    {
        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register("Key", typeof(Key), typeof(KeyTrigger));
        public static readonly DependencyProperty ModifiersProperty = DependencyProperty.Register("Modifiers", typeof(ModifierKeys), typeof(KeyTrigger));
        public static readonly DependencyProperty ActiveOnFocusProperty = DependencyProperty.Register("ActiveOnFocus", typeof(bool), typeof(KeyTrigger));
        public static readonly DependencyProperty FiredOnProperty = DependencyProperty.Register("FiredOn", typeof(KeyTriggerFiredOn), typeof(KeyTrigger), new PropertyMetadata(KeyTriggerFiredOn.KeyUp));
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register("IsEnabled", typeof(bool), typeof(KeyTrigger), new PropertyMetadata(true, OnIsEnabledChanged));
        public static readonly DependencyProperty ActionTextProperty = DependencyProperty.Register("ActionText", typeof(string), typeof(KeyTrigger), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(KeyTrigger), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty IsExtraProperty = DependencyProperty.Register("IsExtra", typeof(bool), typeof(KeyTrigger), new PropertyMetadata(default(bool)));

        static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((KeyTrigger) d).FireIsEnabledChanged();
        }

        UIElement _targetElement;
        bool _registered;

        void FireIsEnabledChanged()
        {
            var e = IsEnabledChanged;

            if (e != null)
            {
                e(this, null);
            }
        }

        public event EventHandler<EventArgs> IsEnabledChanged;

        public Key Key
        {
            get
            {
                return (Key)GetValue(KeyProperty);
            }
            set
            {
                SetValue(KeyProperty, (object)value);
            }
        }

        public ModifierKeys Modifiers
        {
            get
            {
                return (ModifierKeys)GetValue(ModifiersProperty);
            }
            set
            {
                SetValue(ModifiersProperty, (object)value);
            }
        }

        public bool ActiveOnFocus
        {
            get
            {
                return (bool)GetValue(ActiveOnFocusProperty);
            }
            set
            {
                SetValue(ActiveOnFocusProperty, value);
            }
        }

        public KeyTriggerFiredOn FiredOn
        {
            get
            {
                return (KeyTriggerFiredOn)GetValue(FiredOnProperty);
            }
            set
            {
                SetValue(FiredOnProperty, value);
            }
        }

        public bool IsEnabled
        {
            get
            {
                return (bool) GetValue(IsEnabledProperty);
            }
            set
            {
                SetValue(IsEnabledProperty, value);
            }
        }


        public string ActionText
        {
            get
            {
                return (string) GetValue(ActionTextProperty);
            }
            set
            {
                SetValue(ActionTextProperty, value);
            }
        }

        public string Description
        {
            get
            {
                return (string) GetValue(DescriptionProperty);
            }
            set
            {
                SetValue(DescriptionProperty, value);
            }
        }

        public bool IsExtra
        {
            get
            {
                return (bool) GetValue(IsExtraProperty);
            }
            set
            {
                SetValue(IsExtraProperty, value);
            }
        }

        protected override string GetEventName()
        {
            return "Loaded";
        }

        private void OnKeyPress(object sender, KeyEventArgs e)
        {
            if (!IsEnabled || !Source.IsVisible)
            {
                e.Handled = false;
                return;
            }

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key != Key || Keyboard.Modifiers != GetActualModifiers(key, Modifiers))
            {
                return;
            }

            InvokeActionsNew(e);
        }

        protected virtual void InvokeActionsNew(KeyEventArgs e)
        {
            InvokeActions(e);
        }

        private static ModifierKeys GetActualModifiers(Key key, ModifierKeys modifiers)
        {
            switch (key)
            {
                case Key.RightCtrl:
                case Key.LeftCtrl:
                    modifiers |= ModifierKeys.Control;
                    break;
                case Key.System:
                case Key.RightAlt:
                case Key.LeftAlt:
                    modifiers |= ModifierKeys.Alt;
                    break;
                case Key.RightShift:
                case Key.LeftShift:
                    modifiers |= ModifierKeys.Shift;
                    break;
            }

            return modifiers;
        }

        protected override void OnEvent(EventArgs eventArgs)
        {
            if (_registered)
            {
                return;
            }

            KeyMenuContext.Register(Source);

            _targetElement = !ActiveOnFocus ? GetRoot(Source) : Source;

            if (FiredOn == KeyTriggerFiredOn.KeyDown)
            {
                _targetElement.KeyDown += OnKeyPress;
            }
            else
            {
                _targetElement.KeyUp += OnKeyPress;
            }

            _registered = true;
        }

        protected override void OnDetaching()
        {
            if (_targetElement != null)
            {
                if (FiredOn == KeyTriggerFiredOn.KeyDown)
                {
                    _targetElement.KeyDown -= OnKeyPress;
                }
                else
                {
                    _targetElement.KeyUp -= OnKeyPress;
                }

                _registered = false;
            }

            base.OnDetaching();
        }

        private static UIElement GetRoot(DependencyObject current)
        {
            UIElement uiElement = null;
            for (; current != null; current = VisualTreeHelper.GetParent(current))
            {
                uiElement = current as UIElement;
            }
            return uiElement;
        }
    }
}
