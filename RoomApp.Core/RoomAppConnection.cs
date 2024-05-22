using KolibSoft.Rooms.Core;
using KolibSoft.Rooms.Core.Protocol;

namespace KolibSoft.RoomApp.Core
{

    public sealed class RoomAppConnection
    {
        public RoomAppManifest Manifest { get; set; } = new RoomAppManifest();
        public int Channel { get; set; } = 0;
    }

}