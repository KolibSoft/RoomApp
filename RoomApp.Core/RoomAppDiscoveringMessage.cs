using System;

namespace KolibSoft.RoomApp.Core
{

    /// <summary>
    /// Message to discover other apps in the room.
    /// </summary>
    public class RoomAppDiscoveringMessage
    {

        /// <summary>
        /// Requested app capabilities.
        /// </summary>
        public string[] Capabilities { get; set; } = Array.Empty<string>();
    
    }

}