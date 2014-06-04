//using System;
//using GalaSoft.MvvmLight.Messaging;

//namespace twomindseye.Commando.UI
//{
//    public static class MessengerExtensions
//    {
//        /// <summary>
//        /// Register to receive messages in a handler that is also passed the recipient. Useful for ensuring no strong references
//        /// to an instance in the usual case of Messenger.Register&lt;SomeMessage&gt;(this, this.SomeHandler), where in the current
//        /// MVVM Light design 'this' will never be garbage-collected as long as the Messenger is still alive.
//        /// </summary>
//        /// <typeparam name="TRecipient">The type of the recipient class.</typeparam>
//        /// <typeparam name="TMessage">The type of the message.</typeparam>
//        /// <param name="messenger">The messenger instance.</param>
//        /// <param name="recipient">The recipient instance.</param>
//        /// <param name="token">The token.</param>
//        /// <param name="finalHandler">The handler method to call. This should be a static method, or a lambda that does not close over any instance variables.</param>
//        public static void Register<TRecipient, TMessage>(this IMessenger messenger, TRecipient recipient, object token, Action<TRecipient, TMessage> finalHandler)
//            where TRecipient : class
//        {
//            if (recipient == null)
//            {
//                throw new ArgumentNullException("recipient");
//            }

//            if (finalHandler == null)
//            {
//                throw new ArgumentNullException("finalHandler");
//            }

//            if (messenger == null)
//            {
//                throw new ArgumentNullException("messenger");
//            }

//            // We can't use the recipient parameter in the closure, since that would simply create another strong
//            // reference -- the situation we're trying to avoid. And since MVVM Light doesn't pass the recipient to 
//            // the message handler, we have no option but to create a WeakReference to it here and use that in the closure.
//            var weakRef = new WeakReference(recipient);

//            messenger.Register<TMessage>(recipient, token,
//                msg =>
//                {
//                    var r2 = weakRef.Target as TRecipient;

//                    if (r2 == null)
//                    {
//                        // This shouldn't happen, since the fact that the Messenger is calling this method
//                        // means the recipient is still alive. Still, I don't feel right ignoring the possibility.
//                        // In addition, there's no great advantage to throwing an exception here -- so we simply assume 
//                        // the Messenger will unregister this closure when this method returns.
//                        return;
//                    }

//                    finalHandler(r2, msg);
//                });
//        }

//        /// <summary>
//        /// Register to receive messages in a handler that is also passed the recipient. Useful for ensuring no strong references
//        /// to an instance in the usual case of Messenger.Register&lt;SomeMessage&gt;(this, this.SomeHandler), where in the current
//        /// MVVM Light design 'this' will never be garbage-collected as long as the Messenger is still alive.
//        /// </summary>
//        /// <typeparam name="TRecipient">The type of the recipient class.</typeparam>
//        /// <typeparam name="TMessage">The type of the message.</typeparam>
//        /// <param name="messenger">The messenger instance.</param>
//        /// <param name="recipient">The recipient instance.</param>
//        /// <param name="finalHandler">The handler method to call. This should be a static method, or a lambda that does not close over any variables.</param>
//        public static void Register<TRecipient, TMessage>(this IMessenger messenger, TRecipient recipient, Action<TRecipient, TMessage> finalHandler)
//            where TRecipient : class
//        {
//            messenger.Register(recipient, null, finalHandler);
//        }
//    }
//}
