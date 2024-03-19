using System;

namespace KolibSoft.RoomApp.Core
{

    [Flags]
    public enum RoomAppBehavior
    {
        Manual,
        DiscoverFirst,
        AnnounceFirst
    }

}