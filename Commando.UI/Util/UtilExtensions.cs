using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace twomindseye.Commando.UI.Util
{
    static class UtilExtensions
    {
        public static T GetResource<T>(this ResourceDictionary dict, string name)
        {
            return (T) dict[name];
        }
    }
}
