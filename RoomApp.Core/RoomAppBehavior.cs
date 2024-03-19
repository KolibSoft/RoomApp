using System;

namespace KolibSoft.RoomApp.Core
{

    [Flags]
    public enum RoomAppBehavior
    {
        Manual = 0b00,
        Announce = 0b01,
        Discover = 0b10,
        Default = Announce | Discover
    }

}