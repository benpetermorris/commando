using System;

namespace twomindseye.Commando.API1.Parse
{
    [Flags]
    public enum ParseMode
    {
        Match = 0x01,
        Suggest = 0x02,
        All = Match | Suggest
    }
}