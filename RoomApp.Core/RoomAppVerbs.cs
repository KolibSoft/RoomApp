using KolibSoft.Rooms.Core;

namespace KolibSoft.RoomApp.Core
{

    /// <summary>
    /// Room App Verbs.
    /// </summary>
    public static class RoomAppVerbs
    {

        /// <summary>
        /// Verb to announce an app.
        /// </summary>
        public static readonly RoomVerb AppAnnouncement = RoomVerb.Parse("AAv");

        /// <summary>
        /// Verb to discover other apps.
        /// </summary>
        public static readonly RoomVerb AppDiscovering = RoomVerb.Parse("ADv");

    }

}