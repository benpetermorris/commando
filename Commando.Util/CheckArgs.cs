using System;

namespace twomindseye.Commando.Util
{
    public static class CheckArgs
    {
        public static void NotNull(object arg, string paramName)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}
