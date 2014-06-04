using System;

namespace twomindseye.Commando.API1
{
    /// <summary>
    /// Base class for all Commando API attributes.
    /// </summary>
    [Serializable]
    public abstract class APIAttribute : Attribute
    {
        internal APIAttribute()
        {
        }
    }
}
