namespace KolibSoft.RoomApp.Core
{

    /// <summary>
    /// Message to announce an app in the room.
    /// </summary>
    public class RoomAppAnnouncementMessage
    {

        /// <summary>
        /// Room App Manifest.
        /// </summary>
        public RoomAppManifest Manifest { get; set; } = new RoomAppManifest();
    
    }

}