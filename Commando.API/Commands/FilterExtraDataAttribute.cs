using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using twomindseye.Commando.API1.Facets;

namespace twomindseye.Commando.API1.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Serializable]
    public sealed class FilterExtraDataAttribute : APIAttribute
    {
        public FilterExtraDataAttribute(string key, params string[] validValues)
        {
            Key = key;
            ValidValues = validValues;
        }

        public FilterExtraDataAttribute(Type extraDataType, string key, params string[] validValues)
        {
            ExtraDataType = extraDataType;
            Key = key;
            ValidValues = validValues;
        }

        public bool Validate(TypeMoniker parameterType, FacetMoniker moniker)
        {
            var sourceType = ExtraDataType == null
                                 ? parameterType.AssemblyQualifiedName
                                 : ExtraDataType.AssemblyQualifiedName;

            var data = moniker[sourceType, Key];

            if (data == null)
            {
                return AllowMissing;
            }

            if ((RegularExpression == null) == (ValidValues == null))
            {
                throw new InvalidOperationException("One and only one of ValidValues or RegularExpression must be non-null");
            }

            if (ValidValues != null)
            {
                return ValidValues.Contains(data, StringComparer.CurrentCultureIgnoreCase);
            }

            return Regex.IsMatch(data, RegularExpression, RegexOptions.IgnoreCase);
        }

        public TypeMoniker ExtraDataType { get; private set; }
        public string Key { get; private set; }
        public string RegularExpression { get; set; }
        public string[] ValidValues { get; set; }
        public bool AllowMissing { get; set; }
    }
}
