using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;

namespace twomindseye.Commando.Util
{
    public static class FilteredReadOnlyObservableCollection
    {
        public static ReadOnlyObservableCollection<T> Create<T>(ICollection<T> observed, Func<T, bool> predicate)
        {
            CheckArgs.NotNull(observed, "observed");
            CheckArgs.NotNull(predicate, "predicate");

            var filtered = new ObservableCollection<T>();

            return new FilteredCollection<T, T, object>(observed, filtered, predicate, null, null);
        }

        public static ReadOnlyObservableCollection<T> Create<T, TKey>(ICollection<T> observed, Func<T, TKey> order, IComparer<TKey> comparer = null)
        {
            CheckArgs.NotNull(observed, "observed");
            CheckArgs.NotNull(order, "order");

            var filtered = new ObservableCollection<T>();

            return new FilteredCollection<T, T, TKey>(observed, filtered, x => true, order, comparer);
        }

        public static ReadOnlyObservableCollection<T> Create<T, TKey>(ICollection<T> observed, Func<T, bool> predicate, Func<T, TKey> order, IComparer<TKey> comparer = null)
        {
            CheckArgs.NotNull(observed, "observed");
            CheckArgs.NotNull(predicate, "predicate");
            CheckArgs.NotNull(order, "order");

            var filtered = new ObservableCollection<T>();

            return new FilteredCollection<T, T, TKey>(observed, filtered, predicate, order, comparer);
        }

        public static ReadOnlyObservableCollection<TOut> Create<TIn, TOut>(ICollection<TIn> observed, Func<TIn, bool> predicate)
            where TOut : TIn
        {
            CheckArgs.NotNull(observed, "observed");
            CheckArgs.NotNull(predicate, "predicate");

            var filtered = new ObservableCollection<TOut>();

            return new FilteredCollection<TIn, TOut, object>(observed, filtered, predicate, null, null);
        }

        public static ReadOnlyObservableCollection<TOut> Create<TIn, TOut, TKey>(ICollection<TIn> observed, Func<TIn, bool> predicate, Func<TOut, TKey> order, IComparer<TKey> comparer = null)
            where TOut : TIn
        {
            CheckArgs.NotNull(observed, "observed");
            CheckArgs.NotNull(predicate, "predicate");
            CheckArgs.NotNull(order, "order");

            var filtered = new ObservableCollection<TOut>();

            return new FilteredCollection<TIn, TOut, TKey>(observed, filtered, predicate, order, comparer);
        }

        class FilteredCollection<TIn, TOut, TKey> : ReadOnlyObservableCollection<TOut>
            where TOut : TIn
        {
            readonly ObservableCollection<TOut> _filtered;
            readonly Func<TIn, bool> _predicate;
            readonly Func<TOut, TKey> _order;
            readonly IComparer<TKey> _comparer;
            readonly WeakEventBridge _bridge;

            public FilteredCollection(ICollection<TIn> observed, ObservableCollection<TOut> filtered, Func<TIn, bool> predicate, Func<TOut, TKey> order, IComparer<TKey> comparer)
                : base(filtered)
            {
                if (!(observed is INotifyCollectionChanged))
                {
                    throw new ArgumentException("observed");
                }

                _filtered = filtered;
                _predicate = predicate;
                _order = order;
                _comparer = comparer ?? Comparer<TKey>.Default;
                _bridge = new WeakEventBridge();
                _bridge.Bind(observed, "CollectionChanged", WeakEventBridge.AsDelegate(observed_CollectionChanged));

                InitFrom(observed);
            }

            void InitFrom(IEnumerable<TIn> items)
            {
                _filtered.Clear();

                var newitems = items
                    .Where(_predicate)
                    .Cast<TOut>();

                if (_order != null)
                {
                    newitems = newitems.OrderBy(_order);
                }

                foreach (TOut item in newitems)
                {
                    _filtered.Add(item);
                }
            }

            void observed_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    InitFrom((ObservableCollection<TIn>)sender);

                    return;
                }

                if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace)
                {
                    _filtered.Remove((TOut)e.OldItems[0]);
                }

                if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
                {
                    var item = (TOut)e.NewItems[0];

                    if (!_predicate(item))
                    {
                        return;
                    }

                    if (_order != null)
                    {
                        var itemkey = _order(item);
                        var index = 0;

                        foreach (var f in _filtered)
                        {
                            var fkey = _order(f);

                            if (_comparer.Compare(itemkey, fkey) == -1)
                            {
                                _filtered.Insert(index, item);
                                return;
                            }

                            index++;
                        }
                    }

                    _filtered.Add(item);
                }
            }
        }
    }
}
