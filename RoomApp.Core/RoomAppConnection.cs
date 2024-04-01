using KolibSoft.Rooms.Core;
using KolibSoft.Rooms.Core.Protocol;

namespace KolibSoft.RoomApp.Core
{

    /// <summary>
    /// Binds an app manifest and a channel.
    /// </summary>
    public class RoomAppConnection
    {

        /// <summary>
        /// Room App Manifest.
        /// </summary>
        public RoomAppManifest Manifest { get; set; } = new RoomAppManifest();

        /// <summary>
        /// Room App Channel.
        /// </summary>
        public RoomChannel Channel { get; set; } = RoomChannel.Loopback;

    }

}