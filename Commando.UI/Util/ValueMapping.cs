using System.Windows.Markup;

namespace twomindseye.Commando.UI.Util
{
    [ContentProperty("Value")]
    public class ValueMapping
    {
        public string Key { get; set; }
        public object Value { get; set; }
    }
}
