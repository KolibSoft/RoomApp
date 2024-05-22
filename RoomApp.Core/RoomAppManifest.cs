using System;

namespace KolibSoft.RoomApp.Core
{

    public sealed class RoomAppManifest
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public string[] Capabilities { get; set; } = Array.Empty<string>();
    }

}