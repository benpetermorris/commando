using System;

namespace twomindseye.Commando.API1.Facets
{
    // NB: this is for data that is not expected to change. 
    // For example, an IFilesystemItemFacet's Type (program or file), but not whether it exists.

    [AttributeUsage(AttributeTargets.Interface)]
    [Serializable]
    public sealed class ExtraDataDefinitionAttribute : APIAttribute
    {
        public ExtraDataDefinitionAttribute(string key, params string[] possibleValues)
        {
            Key = key;
            PossibleValues = possibleValues;
        }

        public string Key { get; private set; }
        public string[] PossibleValues { get; private set; }
    }
}