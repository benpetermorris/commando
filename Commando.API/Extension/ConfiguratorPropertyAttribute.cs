using System;

namespace twomindseye.Commando.API1.Extension
{
    [AttributeUsage(AttributeTargets.Property)]
    [Serializable]
    public sealed class ConfiguratorPropertyAttribute : APIAttribute
    {
        public ConfiguratorPropertyAttribute()
        {
        }

        public ConfiguratorPropertyAttribute(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; set; }
        public string GroupName { get; set; }
        public string StoreKey { get; set; }
        public bool AllowUserValue { get; set; }
        public bool IsPassword { get; set; }
    }
}
