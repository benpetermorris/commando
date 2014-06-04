using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace twomindseye.Commando.UI.Util
{
    public sealed class IndirectBindingCollection : FreezableCollection<IndirectBinding>
    {
        public IndirectBindingCollection()
        {
            ((INotifyCollectionChanged)this).CollectionChanged += IndirectBindingCollectionCollectionChanged;
        }

        internal FrameworkElement AttachedObject { get; set; }

        void IndirectBindingCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (IndirectBinding b in e.NewItems)
                {
                    if (!DesignerProperties.GetIsInDesignMode(this))
                    {
                        b.BindTo(AttachedObject);
                    }
                }
            }
        }
    }

    public sealed class IndirectBinding : FrameworkElement
    {
        public static readonly DependencyProperty IndirectBindingsProperty =
            DependencyProperty.RegisterAttached("ShadowIndirectBindings", typeof (IndirectBindingCollection), typeof (IndirectBinding), 
            null);

        public static IndirectBindingCollection GetIndirectBindings(DependencyObject obj)
        {
            var coll = (IndirectBindingCollection)obj.GetValue(IndirectBindingsProperty);

            if (coll == null)
            {
                coll = new IndirectBindingCollection();
                coll.AttachedObject = (FrameworkElement) obj;
                obj.SetValue(IndirectBindingsProperty, coll);
            }

            return coll;
        }

        FrameworkElement _targetObject;
        Binding _binding;

        internal void BindTo(FrameworkElement target)
        {
            _targetObject = target;

            var dcBinding = new Binding("DataContext");
            dcBinding.Source = _targetObject;
            SetBinding(DataContextProperty, dcBinding);

            TryBind();
        }

        static DependencyProperty GetDependencyProperty(Type type, string propertyName)
        {
            while (type != null)
            {
                var field = type.GetField(propertyName + "Property", BindingFlags.Public | BindingFlags.Static);
                
                if (field != null)
                {
                    return (DependencyProperty) field.GetValue(null);
                }

                type = type.BaseType;
            }

            return null;
        }

        void TryBind()
        {
            if (_targetObject == null || TargetPropertyName == null || SourceObject == null || SourcePropertyName == null)
            {
                return;
            }

            var property = GetDependencyProperty(_targetObject.GetType(), TargetPropertyName);

            if (property == null)
            {
                return;
            }

            _binding = new Binding(SourcePropertyName);
            _binding.Mode = Mode;
            _binding.Source = SourceObject;
            _targetObject.SetBinding(property, _binding);
        }

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", typeof (BindingMode), typeof (IndirectBinding), 
            new PropertyMetadata(BindingMode.Default));

        public BindingMode Mode
        {
            get
            {
                return (BindingMode) GetValue(ModeProperty);
            }
            set
            {
                SetValue(ModeProperty, value);
            }
        }

        public static readonly DependencyProperty TargetPropertyNameProperty =
            DependencyProperty.Register("TargetPropertyName", typeof (string), typeof (IndirectBinding), 
            new PropertyMetadata(default(string), OnBindRelatedPropertyChanged));

        public string TargetPropertyName
        {
            get
            {
                return (string) GetValue(TargetPropertyNameProperty);
            }
            set
            {
                SetValue(TargetPropertyNameProperty, value);
            }
        }

        public static readonly DependencyProperty SourceObjectProperty =
            DependencyProperty.Register("SourceObject", typeof (object), typeof (IndirectBinding), 
            new PropertyMetadata(default(object), OnBindRelatedPropertyChanged));

        static void OnBindRelatedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((IndirectBinding) d).TryBind();
        }

        public object SourceObject
        {
            get
            {
                return (object) GetValue(SourceObjectProperty);
            }
            set
            {
                SetValue(SourceObjectProperty, value);
            }
        }

        public static readonly DependencyProperty SourcePropertyNameProperty =
            DependencyProperty.Register("SourcePropertyName", typeof (string), typeof (IndirectBinding), 
            new PropertyMetadata(default(string), OnBindRelatedPropertyChanged));

        public string SourcePropertyName
        {
            get
            {
                return (string) GetValue(SourcePropertyNameProperty);
            }
            set
            {
                SetValue(SourcePropertyNameProperty, value);
            }
        }
    }
}
