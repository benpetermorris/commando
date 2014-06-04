using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace twomindseye.Commando.UI.Util
{
    static class Utility
    {
        public static BindingBase CreateBinding(object source, string path)
        {
            return new Binding(path) {Source = source};
        }
    }
}
