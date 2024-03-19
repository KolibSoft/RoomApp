using KolibSoft.Rooms.Core;

namespace KolibSoft.RoomApp.Core
{
    public static class RoomAppVerbs
    {
        public static readonly RoomVerb AppAnnouncement = RoomVerb.Parse("AAv");
        public static readonly RoomVerb AppDiscovering = RoomVerb.Parse("ADv");
    }
}