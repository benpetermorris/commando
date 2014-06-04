using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace twomindseye.Commando.Util
{
    public enum WeakBridgeDuplicateMode
    {
        /// <summary>
        /// Allow duplicate event handlers to be added to a single event. Higher performance than CheckAndPrevent.
        /// </summary>
        Allow,
        /// <summary>
        /// Check for and prevent duplicate event handlers from being added to a single event. Degrades binding performance,
        /// but makes it convenient to ensure a single event will be called by a single handler.
        /// </summary>
        CheckAndPrevent
    }

    public class WeakEventBridge
    {
        readonly WeakReference _thisReference;
        readonly WeakBridgeDuplicateMode _defaultDuplicateMode;
        readonly bool _unregisterInFinalizer;
        readonly object _bindInfosLock;
        readonly int _growBindInfosBy;
        BindInfo[] _bindInfos;
        long _nextBindInfoId;

        //
        // Public methods
        //

        /// <summary>
        /// Constructor. 
        /// </summary>
        /// <param name="defaultDuplicateMode">true, to allow the same handler to be bound to the same
        /// event more than once.</param>
        /// <param name="unregisterInFinalizer">true, if this instance should deregister remaining handler shims
        /// in its finalizer. If false, finalization is suppressed, and any shims still registered
        /// when the bridge is collected will remain registered until called--at which time they will be 
        /// automatically removed.</param>
        /// <param name="initialHandlerCount">The initial number of event handler slots.</param>
        /// <param name="growBy">The number of event handler slots to grow by, when the current number is used up.</param>
        public WeakEventBridge(WeakBridgeDuplicateMode defaultDuplicateMode = WeakBridgeDuplicateMode.Allow,
            bool unregisterInFinalizer = true, int initialHandlerCount = 2, int growBy = 2)
        {
            _thisReference = new WeakReference(this);
            _defaultDuplicateMode = defaultDuplicateMode;
            _unregisterInFinalizer = unregisterInFinalizer;
            _bindInfosLock = new object();
            _growBindInfosBy = Math.Max(growBy, 1);
            _bindInfos = new BindInfo[initialHandlerCount];
            GC.SuppressFinalize(this);
        }

        // suppressed if _unregisterInFinalizer is false, so no need to check in this method
        ~WeakEventBridge()
        {
            UnbindAll();
        }

        /// <summary>
        /// Weakly bind a handler to a public static event on the specified type.
        /// </summary>
        /// <param name="sourceType">The type that exposes the event.</param>
        /// <param name="eventName">The name of the public static event to bind to.</param>
        /// <param name="handler">The user handler to be called. The signature of the handler must match the event.</param>
        /// <param name="duplicateMode"></param>
        public void Bind(Type sourceType, string eventName, Delegate handler,
            WeakBridgeDuplicateMode? duplicateMode = null)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException("sourceType");
            }

            AddEventTarget(sourceType, null, eventName, handler, duplicateMode);
        }

        /// <summary>
        /// Weakly bind a handler to a public instance event on the specified object.
        /// </summary>
        /// <param name="source">The instance that exposes the event.</param>
        /// <param name="eventName">The name of the public instance event to bind to.</param>
        /// <param name="handler">The user handler to be called.</param>
        /// <param name="duplicateMode"></param>
        public void Bind(object source, string eventName, Delegate handler,
            WeakBridgeDuplicateMode? duplicateMode = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            AddEventTarget(null, source, eventName, handler, duplicateMode);
        }

        public void Unbind(Type sourceType, string eventName, Delegate handler)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException("sourceType");
            }

            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            RemoveEventTargets(GetBindInfoMatcher(sourceType, eventName: eventName, handler: handler));
        }

        public void Unbind(object source, string eventName, Delegate handler)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            RemoveEventTargets(GetBindInfoMatcher(source: source, eventName: eventName, handler: handler));
        }

        public void UnbindAll()
        {
            RemoveEventTargets(x => true);
        }

        public void UnbindAll(Delegate handler)
        {
            RemoveEventTargets(GetBindInfoMatcher(handler: handler));
        }

        public void UnbindAll(object source, string eventName = null, Delegate handler = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            RemoveEventTargets(GetBindInfoMatcher(source: source, eventName: eventName, handler: handler));
        }

        public void UnbindAll(Type sourceType, string eventName = null, Delegate handler = null)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException("sourceType");
            }

            RemoveEventTargets(GetBindInfoMatcher(sourceType, eventName: eventName));
        }

        public void UnbindAll(string eventName)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            RemoveEventTargets(GetBindInfoMatcher(eventName: eventName));
        }

        public void RemoveInvalid()
        {
            RemoveEventTargets(x => x.Shim.SourceType == null && x.Shim.Source == null);
        }

        public int GetBoundCount()
        {
            return GetBoundCount(x => true);
        }

        public int GetBoundCount(Delegate handler)
        {
            return GetBoundCount(GetBindInfoMatcher(handler: handler));
        }

        public int GetBoundCount(object source, string eventName = null, Delegate handler = null)
        {
            return GetBoundCount(GetBindInfoMatcher(source: source, eventName: eventName, handler: handler));
        }

        public int GetBoundCount(Type sourceType, string eventName = null, Delegate handler = null)
        {
            return GetBoundCount(GetBindInfoMatcher(sourceType, eventName: eventName, handler: handler));
        }

        public static Delegate AsDelegate(EventHandler handler)
        {
            return handler;
        }

        public static Delegate AsDelegate<T>(EventHandler<T> handler) where T : EventArgs
        {
            return handler;
        }

        public static Delegate AsDelegate(NotifyCollectionChangedEventHandler handler)
        {
            return handler;
        }

        public static Delegate AsDelegate(PropertyChangedEventHandler handler)
        {
            return handler;
        }

        //
        // Private methods
        //

        static Func<BindInfo, bool> GetBindInfoMatcher(Type sourceType = null, object source = null, 
            string eventName = null, Delegate handler = null, object target = null)
        {
            Func<BindInfo, bool> filter = info => true;

            if (sourceType != null)
            {
                filter = info => info.Shim.SourceType == sourceType;
            }
            else if (source != null)
            {
                filter = info => info.Shim.Source == source;
            }

            if (eventName != null)
            {
                var ext = filter;
                filter = info => info.Shim.EventName == eventName && ext(info);
            }

            if (handler != null)
            {
                var ext = filter;
                filter = info => info.ClientHandler == handler && ext(info);
            }
            else if (target != null)
            {
                var ext = filter;
                filter = info => info.ClientHandler.Target == target && ext(info);
            }

            return filter;
        }

        void AddEventTarget(Type sourceType, object source, string eventName, Delegate handler, WeakBridgeDuplicateMode? duplicateMode)
        {
            if ((sourceType == null) == (source == null))
            {
                throw new ArgumentException("Exactly one of sourceType and source must be non-null");
            }

            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            EventHandlerShim shim;
            
            lock (_bindInfosLock)
            {
                var match = GetBindInfoMatcher(sourceType, source, eventName, handler);

                if ((duplicateMode ?? _defaultDuplicateMode) == WeakBridgeDuplicateMode.CheckAndPrevent &&
                    _bindInfos.Where(x => x != null).Any(match))
                {
                    // already bound
                    return;
                }

                // find unused index
                var handlerIndex = Array.IndexOf(_bindInfos, null);

                if (handlerIndex == -1)
                {
                    // no unused: expand array
                    handlerIndex = _bindInfos.Length;
                    Array.Resize(ref _bindInfos, _bindInfos.Length + _growBindInfosBy);
                }

                // may throw ArgumentException
                var handlerId = _nextBindInfoId++;
                shim = new EventHandlerShim(sourceType, source, eventName, _thisReference, handlerIndex, handlerId, 
                    handler.Method.GetParameters());
                _bindInfos[handlerIndex] = new BindInfo(handler, shim, handlerId);

                // DEADLOCK (uncomment the next line, and comment out the same call just below)
                // shim.RegisterEventHandler();
            }

            // Don't register the event handler in the synchronization context to avoid the possibility of 
            // deadlocks triggered by side-effects of binding (e.g. further bind/unbind activity via an 
            // add accessor in the event source class)
            shim.RegisterEventHandler();

            if (_unregisterInFinalizer)
            {
                GC.ReRegisterForFinalize(this);
            }
        }

        void RemoveEventTargets(Func<BindInfo, bool> predicate)
        {
            var allNull = true;
            List<BindInfo> toUnbind = null;

            lock (_bindInfosLock)
            {
                for (var i = 0; i < _bindInfos.Length; i++)
                {
                    var bi = _bindInfos[i];

                    if (bi == null)
                    {
                        continue;
                    }

                    if (predicate(_bindInfos[i]))
                    {
                        if (toUnbind == null)
                        {
                            toUnbind = new List<BindInfo>();
                        }

                        // DEADLOCK (uncomment the next line, and comment out the same call in the loop below)
                        // bi.Shim.Unbind();

                        toUnbind.Add(bi);
                        _bindInfos[i] = null;
                    }
                    else
                    {
                        allNull = false;
                    }
                }
            }

            if (toUnbind != null)
            {
                // Don't unbind in the synchronization context to avoid the possibility of deadlocks triggered
                // by side-effects of unbinding (e.g. further bind/unbind activity via a remove accessor on
                // the event source class)
                foreach (var item in toUnbind)
                {
                    item.Shim.Unbind();
                }
            }

            if (allNull && _unregisterInFinalizer)
            {
                GC.SuppressFinalize(this);
            }
        }

        int GetBoundCount(Func<BindInfo, bool> predicate)
        {
            lock (_bindInfosLock)
            {
                return _bindInfos.Where(x => x != null).Where(predicate).Count();
            }
        }

        bool CallBindInfoHandler(object source, object eventArgs, int handlerIndex, long handlerId)
        {
            BindInfo info;

            lock (_bindInfosLock)
            {
                info = _bindInfos[handlerIndex];

                // RemoveEventTargets alters state inside the lock but unbinds EventTargets just
                // outside (see comment in that method). It's possible that this handler has been called 
                // just before the calling EventTarget was to be removed. It's also possible that another 
                // handler has been added in its place. In the first case, info will simply be null -- 
                // in the second, the registered handler's unique ID will be different from the one passed 
                // to this method.

                if (info == null || info.HandlerId != handlerId)
                {
                    return false;
                }
            }

            info.CallHandler(source, eventArgs);

            return true;
        }

        class BindInfo
        {
            delegate void EventHandlerInvoker(Delegate eventHandler, object source, object eventArgs);

            //
            // Static
            //

            static readonly Dictionary<Type, EventHandlerInvoker> s_eventHandlerInvokers;

            static BindInfo()
            {
                s_eventHandlerInvokers = new Dictionary<Type, EventHandlerInvoker>();
            }

            //
            // Instance
            //

            volatile EventHandlerInvoker _eventHandlerInvoker;

            public BindInfo(Delegate clientHandler, EventHandlerShim shim, long handlerId)
            {
                ClientHandler = clientHandler;
                Shim = shim;
                HandlerId = handlerId;
                _eventHandlerInvoker = GetEventHandlerInvoker(ClientHandler.GetType(), ClientHandler.Method.GetParameters());
            }

            public long HandlerId { get; private set; }
            public Delegate ClientHandler { get; private set; }
            public EventHandlerShim Shim { get; private set; }

            public void CallHandler(object source, object eventArgs)
            {
                _eventHandlerInvoker(ClientHandler, source, eventArgs);
            }

            // Tradeoff: more memory for greater speed. Calling Handler.DynamicInvoke is very slow; this 
            // method creates a static delegate that itself invokes a single type of delegate (i.e., the type of 
            // Handler), and the result is ~14x faster. The delegate is not bound to a particular event source or target 
            // instance--it is reused wherever the same event handling signature is found.
            static EventHandlerInvoker GetEventHandlerInvoker(Type delegateType, ParameterInfo[] delegateParameters)
            {
                EventHandlerInvoker invoker;

                lock (s_eventHandlerInvokers)
                {
                    if (s_eventHandlerInvokers.TryGetValue(delegateType, out invoker))
                    {
                        return invoker;
                    }

                    // void EventHandlerInvoker(Delegate handler, object source, object e) 
                    // {
                    //     ((delegateType)handler)((handlerParam0Type)source, (handlerParam1Type)e);
                    // }

                    var userHandlerParam = Expression.Parameter(typeof(Delegate), "handler");
                    var sourceParam = Expression.Parameter(typeof(object), "source");
                    var eParam = Expression.Parameter(typeof(object), "e");
                    var invokeExpr = Expression.Invoke(
                        Expression.Convert(userHandlerParam, delegateType),
                        Expression.Convert(sourceParam, delegateParameters[0].ParameterType),
                        Expression.Convert(eParam, delegateParameters[1].ParameterType));

                    invoker = Expression
                        .Lambda<EventHandlerInvoker>(invokeExpr, userHandlerParam, sourceParam, eParam)
                        .Compile();

                    s_eventHandlerInvokers[delegateType] = invoker;
                }

                return invoker;
            }
        }

        class EventHandlerShim
        {
            //
            // Static
            //

            static readonly MethodInfo s_genericHandlerMethodInfo;

            static EventHandlerShim()
            {
                s_genericHandlerMethodInfo = typeof(EventHandlerShim).GetMethod("GenericEventHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            //
            // Instance
            //

            // Non-null if the event is a static event:
            Type _sourceType;

            // Non-null if the event is an instance event. We can't have a strong reference to the 
            // source of the event because the bridge references this, and the source's lifetime 
            // would therefore be influenced by the lifetime of the bridge.
            WeakReference _source;

            // Conversely, we can't have a strong reference to the bridge, because the source has 
            // a strong reference to this instance (indirectly, through the handler we add to its 
            // event); and since the bridge has strong references to the event targets (through 
            // their delegates), a strong ref to the bridge would also influence the lifetimes
            // of those targets.
            WeakReference _bridge;

            int _bridgeHandlerIndex;
            long _bridgeHandlerId;
            EventInfo _eventInfo;
            Delegate _eventHandler;

            public EventHandlerShim(Type sourceType, object source, string eventName, 
                WeakReference bridgeReference, int bridgeHandlerIndex, long bridgeHandlerId,
                ParameterInfo[] handlerParameters)
            {
                // assumption: exactly one of sourceType and source is non-null

                if (sourceType != null)
                {
                    _sourceType = sourceType;
                }
                else
                {
                    _source = new WeakReference(source);
                    sourceType = source.GetType();
                }

                Init(sourceType, eventName, bridgeReference, bridgeHandlerIndex, bridgeHandlerId, handlerParameters);
            }

            // called by the bridge after exiting its synchronization context
            public void RegisterEventHandler()
            {
                _eventInfo.AddEventHandler(_source != null ? _source.Target : null, _eventHandler);
            }

            /// <summary>
            /// Valid if this instance handles an instance event generated by Source, and 
            /// if Source has not been collected.
            /// </summary>
            public object Source
            {
                get
                {
                    return _source != null ? _source.Target : null;
                }
            }

            /// <summary>
            /// Valid if this instance handles a static event generated by SourceType.
            /// </summary>
            public Type SourceType
            {
                get
                {
                    return _sourceType;
                }
            }

            /// <summary>
            /// The name of the bound event.
            /// </summary>
            public string EventName
            {
                get
                {
                    return _eventInfo.Name;
                }
            }

            /// <summary>
            /// Unbind the associated event handler from the source event.
            /// </summary>
            public void Unbind()
            {
                if (_eventHandler == null)
                {
                    return;
                }

                lock (this)
                {
                    if (_eventHandler == null)
                    {
                        return;
                    }

                    if (_source != null)
                    {
                        // remove from instance event
                        var source = _source.Target;

                        if (source != null)
                        {
                            _eventInfo.RemoveEventHandler(source, _eventHandler);
                        }
                    }
                    else
                    {
                        // remove from static event
                        _eventInfo.RemoveEventHandler(null, _eventHandler);
                    }

                    _source = null;
                    _sourceType = null;
                    _bridge = null;
                    _eventHandler = null;
                    _eventInfo = null;
                }
            }

            static EventInfo FindEvent(Type type, string eventName, bool findStatic)
            {
                Func<Type, EventInfo> queryType = qt => qt.GetEvent(eventName, 
                    BindingFlags.Public | (findStatic ? BindingFlags.Static : BindingFlags.Instance));

                var info = queryType(type);

                if (info == null)
                {
                    info = type.GetInterfaces().Select(queryType).Where(x => x != null).FirstOrDefault();
                }

                return info;
            }

            /// <summary>
            /// Initialize this instance.
            /// </summary>
            /// <param name="sourceType">The event source type.</param>
            /// <param name="eventName">The name of the event.</param>
            /// <param name="bridgeReference">A WeakReference to the WeakEventBridge.</param>
            /// <param name="bridgeHandlerIndex">The index into the bridge's _bindInfos array that identifies the
            /// actual handler for the event.</param>
            /// <param name="bridgeHandlerId">A unique identifier that must be matched in the indexed BindInfo
            /// class if the actual handler is to be called.</param>
            /// <param name="handlerParameters"></param>
            void Init(Type sourceType, string eventName, WeakReference bridgeReference, 
                int bridgeHandlerIndex, long bridgeHandlerId, ParameterInfo[] handlerParameters)
            {
                _eventInfo = FindEvent(sourceType, eventName, _source == null);

                if (_eventInfo == null)
                {
                    throw new ArgumentException(
                        string.Format(
                            "A{0} event named {1} was not found in the type {2}. Only public events are supported.",
                            _source == null ? " static" : "n instance",
                            eventName,
                            sourceType.Name));
                }

                var eventType = _eventInfo.EventHandlerType;
                var eventParameters = eventType.GetMethod("Invoke").GetParameters();

                // validate event parameters against handler parameters
                if (eventParameters.Length != 2)
                {
                    throw new ArgumentException("events must have exactly two parameters");
                }

                if (handlerParameters.Length != 2)
                {
                    throw new ArgumentException("handlers must have exactly two parameters");
                }

                for (var i = 0; i < eventParameters.Length; i++)
                {
                    var eventParamType = eventParameters[i].ParameterType;
                    var handlerParamType = handlerParameters[i].ParameterType;

                    if (!handlerParamType.IsAssignableFrom(eventParamType))
                    {
                        throw new ArgumentException(
                            string.Format(
                                "event parameter of type '{0}' is incompatible with handler parameter of type '{1}'",
                                eventParamType.FullName,
                                handlerParamType.FullName));
                    }
                }

                // the runtime returns the same MethodInfo instance for all calls to MakeGenericMethod with the same
                // generic parameter type:
                var genericMethod = s_genericHandlerMethodInfo.MakeGenericMethod(eventParameters[1].ParameterType);
                _eventHandler = Delegate.CreateDelegate(eventType, this, genericMethod);

                _bridge = bridgeReference;
                _bridgeHandlerIndex = bridgeHandlerIndex;
                _bridgeHandlerId = bridgeHandlerId;

                // bridge calls RegisterEventHandler when it's ready
            }

            /// <summary>
            /// Called by the CallEventTargetHandler delegate, which itself is called by the source event.
            /// </summary>
            /// <param name="sender">The event sender, as provided by the event source.</param>
            /// <param name="e">The event args, as provided by the event source.</param>
            public void Handler(object sender, object e)
            {
                WeakEventBridge bridge = null;

                lock (this)
                {
                    if (_bridge != null)
                    {
                        bridge = _bridge.Target as WeakEventBridge;
                    }
                }

                if (bridge == null || !bridge.CallBindInfoHandler(sender, e, _bridgeHandlerIndex, _bridgeHandlerId))
                {
                    Unbind();
                }
            }

            // Used in Init() to construct event handling delegates that are added to event sources.
            void GenericEventHandler<T>(object sender, T e)
            {
                Handler(sender, e);
            }
        }
    }
}
