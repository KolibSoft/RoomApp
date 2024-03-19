using KolibSoft.Rooms.Core;

namespace KolibSoft.RoomApp.Core
{
    public class RoomAppConnection
    {
        public RoomAppManifest Manifest { get; set; } = new RoomAppManifest();
        public RoomChannel Channel { get; set; } = RoomChannel.Loopback;
    }
}