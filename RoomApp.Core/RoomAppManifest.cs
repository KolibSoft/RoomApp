using System;

namespace KolibSoft.RoomApp.Core
{

    /// <summary>
    /// Room App Manifest.
    /// </summary>
    public class RoomAppManifest
    {

        /// <summary>
        /// App Id.
        /// </summary>
        public Guid Id { get; set; } = Guid.Empty;
        
        /// <summary>
        /// App Name.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Provided app capabilities.
        /// </summary>
        public string[] Capabilities { get; set; } = Array.Empty<string>();

    }

}