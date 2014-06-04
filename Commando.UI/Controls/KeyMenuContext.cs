using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
using twomindseye.Commando.UI.Util;
using twomindseye.Commando.Util;

namespace twomindseye.Commando.UI.Controls
{
    class KeyMenuContext : DependencyObject
    {
        readonly List<KeyInfo> _allKeyInfos;
        readonly ObservableCollection<KeyInfo> _activeKeyInfos;
        int _paused;

        private KeyMenuContext()
        {
            _activeKeyInfos = new ObservableCollection<KeyInfo>();
            _allKeyInfos = new List<KeyInfo>();
            KeyInfoList = new ReadOnlyObservableCollection<KeyInfo>(_activeKeyInfos);
        }

        public ReadOnlyObservableCollection<KeyInfo> KeyInfoList { get; private set; }

        public static readonly DependencyProperty HasContextProperty =
            DependencyProperty.RegisterAttached("HasContext", typeof (bool), typeof (KeyMenuContext), 
            new PropertyMetadata(default(bool), OnHasContextChanged));

        static void OnHasContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (GetHasContext(d))
            {
                SetContext(d, new KeyMenuContext());
            }
            else
            {
                throw new InvalidOperationException("not supported");
            }
        }

        public static void SetHasContext(DependencyObject element, bool value)
        {
            element.SetValue(HasContextProperty, value);
        }

        public static bool GetHasContext(DependencyObject element)
        {
            return (bool) element.GetValue(HasContextProperty);
        }

        static readonly DependencyProperty RegisteredProperty =
            DependencyProperty.RegisterAttached("Registered", typeof (bool), typeof (KeyMenuContext), 
            new PropertyMetadata(default(bool)));

        static void SetRegistered(DependencyObject element, bool value)
        {
            element.SetValue(RegisteredProperty, value);
        }

        static bool GetRegistered(DependencyObject element)
        {
            return (bool)element.GetValue(RegisteredProperty);
        }

        static readonly DependencyProperty ContextProperty =
             DependencyProperty.RegisterAttached("Context", typeof(KeyMenuContext), typeof(KeyMenuContext),
             new PropertyMetadata(default(KeyMenuContext)));

        static void SetContext(DependencyObject element, KeyMenuContext value)
        {
            element.SetValue(ContextProperty, value);
        }

        public static KeyMenuContext GetContext(DependencyObject element)
        {
            return (KeyMenuContext)element.GetValue(ContextProperty);
        }
 
        public void PauseUpdates()
        {
            _paused++;
        }

        public void ResumeUpdates()
        {
            if (_paused == 0)
            {
                return;
            }

            if (--_paused == 0)
            {
                UpdateFor(_allKeyInfos);
            }
        }

        static public void Register(UIElement el)
        {
            var ctx = Find(el);

            if (ctx == null)
            {
                el.IsVisibleChanged += PreContextIsVisibleChanged;
                return;
            }

            if (!GetRegistered(el))
            {
                SetRegistered(el, true);
                ctx.AddElement(el);
            }
        }

        static void PreContextIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var el = (UIElement) sender;

            if (!el.IsVisible)
            {
                return;
            }

            el.IsVisibleChanged -= PreContextIsVisibleChanged;

            var ctx = Find(el);

            if (ctx == null)
            {
                return;
            }

            if (!GetRegistered(el))
            {
                SetRegistered(el, true);
                ctx.AddElement(el);
            }
        }

        private void AddElement(UIElement e)
        {
            e.IsVisibleChanged += OnElementIsActiveChanged;
            e.IsEnabledChanged += OnElementIsActiveChanged;
            UpdateFor(e);
        }

        private void RemoveElement(UIElement e)
        {
            _allKeyInfos.RemoveWhere(x => x.Element == e, x => x.KeyTrigger.IsEnabledChanged -= OnKeyTriggerIsEnabledChanged);
            _activeKeyInfos.RemoveWhere(x => x.Element == e);

            e.IsVisibleChanged -= OnElementIsActiveChanged;
            e.IsEnabledChanged -= OnElementIsActiveChanged;
        }

        void OnKeyTriggerIsEnabledChanged(object sender, EventArgs e)
        {
            UpdateFor(_allKeyInfos.Where(x => x.KeyTrigger == (KeyTrigger)sender));
        }

        void OnElementIsActiveChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateFor((UIElement)sender);
        }

        private void UpdateFor(UIElement e)
        {
            foreach (var keyTrigger in Interaction.GetTriggers(e).OfType<KeyTrigger>().Except(_allKeyInfos.Select(y => y.KeyTrigger)))
            {
                keyTrigger.IsEnabledChanged += OnKeyTriggerIsEnabledChanged;

                _allKeyInfos.Add(new KeyInfo(keyTrigger, e, keyTrigger.Key, keyTrigger.Modifiers));
            }

            UpdateFor(_allKeyInfos.Where(x => x.Element == e));
        }

        private void UpdateFor(IEnumerable<KeyInfo> infos)
        {
            if (_paused != 0)
            {
                return;
            }

            var toAdd = new List<KeyInfo>();

            foreach (var info in infos)
            {
                var shouldBeActive = info.Element.IsVisible && info.Element.IsEnabled && info.KeyTrigger.IsEnabled;
                var isActive = _activeKeyInfos.Contains(info);

                if (shouldBeActive && !isActive)
                {
                    toAdd.Add(info);
                }
                else if (!shouldBeActive && isActive)
                {
                    _activeKeyInfos.Remove(info);
                }
            }

            if (toAdd.Count <= 0)
            {
                return;
            }

            var newActiveList = toAdd
                .Union(_activeKeyInfos)
                .OrderBy(x => _allKeyInfos.IndexOf(x))
                .ToArray();

            for (var i = 0; i < newActiveList.Length; i++)
            {
                if (i > _activeKeyInfos.Count - 1)
                {
                    _activeKeyInfos.Add(newActiveList[i]);
                }
                else if (newActiveList[i] != _activeKeyInfos[i])
                {
                    _activeKeyInfos.Insert(i, newActiveList[i]);
                }
            }
        }

        public static KeyMenuContext Find(DependencyObject element)
        {
            var at = element;

            while (at != null)
            {
                var ctx = GetContext(at);

                if (ctx != null)
                {
                    return ctx;
                }

                at = VisualTreeHelper.GetParent(at);
            }

            return null;
        }

        public class KeyInfo
        {
            public KeyInfo(KeyTrigger source, UIElement element, Key key, ModifierKeys modifiers)
            {
                KeyTrigger = source;
                Element = element;
                Key = key;
                Modifiers = modifiers;
                KeyText = EnumUtil
                    .GetEnumFlagStringValues(modifiers)
                    .Concat(GetBetterKeyText(key))
                    .JoinStrings("+");
            }

            public string KeyText { get; private set; }
            public KeyTrigger KeyTrigger { get; private set; }
            public UIElement Element { get; private set; }
            public Key Key { get; private set; }
            public ModifierKeys Modifiers { get; private set; }

            static string GetBetterKeyText(Key key)
            {
                switch (key)
                {
                    case Key.Add:
                        return "Plus";
                    case Key.Subtract:
                        return "Minus";
                }

                var s = key.ToString();

                if (s.StartsWith("Oem"))
                {
                    s = s.Substring(3);
                }
                else if (s.Length == 2 && s[0] == 'D' && char.IsNumber(s[1]))
                {
                    s = s.Substring(1);
                }

                return s;
            }
        }
    }
}
