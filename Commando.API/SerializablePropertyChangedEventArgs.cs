using System;

namespace twomindseye.Commando.API1
{
    [Serializable]
    public sealed class SerializablePropertyChangedEventArgs : EventArgs
    {
        public SerializablePropertyChangedEventArgs(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; private set; }
    }
}