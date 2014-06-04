using System;

namespace twomindseye.Commando.Engine.Load
{
    sealed class LoaderItemInitException : Exception
    {
        public LoaderItemInitException(string message) : base(message)
        {
        }

        public LoaderItemInitException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}