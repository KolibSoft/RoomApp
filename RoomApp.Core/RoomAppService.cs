using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using KolibSoft.Rooms.Core;

namespace KolibSoft.RoomApp.Core
{

    /// <summary>
    /// Room App Service.
    /// </summary>
    public class RoomAppService : RoomService
    {

        /// <summary>
        /// App manifest for the current service.
        /// </summary>
        public RoomAppManifest Manifest { get; set; }

        /// <summary>
        /// Requested capabilities for other room apps.
        /// </summary>
        public string[] Capabilities { get; set; }

        /// <summary>
        /// App behavior to announce and discover apps.
        /// </summary>
        public RoomAppBehavior Behavior { get; set; } = RoomAppBehavior.DiscoverFirst;

        /// <summary>
        /// Available room app connections in the room.
        /// </summary>
        public ImmutableArray<RoomAppConnection> Connections { get; private set; } = ImmutableArray.Create<RoomAppConnection>();

        /// <summary>
        /// Connection changed event.
        /// </summary>
        public event EventHandler<RoomAppConnection>? ConnectionChanged;

        /// <summary>
        /// Announce this app using the specified channel (using Broadcast channel if no specified).
        /// </summary>
        /// <param name="channel">Room channel to use.</param>
        /// <returns></returns>
        public async Task AnnounceApp(RoomChannel? channel = null)
        {
            var json = JsonSerializer.Serialize(new RoomAppAnnouncementMessage
            {
                Manifest = Manifest
            });
            await SendAsync(new RoomMessage
            {
                Verb = RoomAppVerbs.AppAnnouncement,
                Channel = channel ?? RoomChannel.Broadcast,
                Content = RoomContent.Parse(json)
            });
        }

        /// <summary>
        /// Attempts to discover other app in the room using the specified channel (using Broadcast channel if no specified).
        /// </summary>
        /// <param name="channel">Room channel to use.</param>
        /// <returns></returns>
        public async Task DiscoverApp(RoomChannel? channel = null)
        {
            var json = JsonSerializer.Serialize(new RoomAppDiscoveringMessage
            {
                Capabilities = Capabilities
            });
            await SendAsync(new RoomMessage
            {
                Verb = RoomAppVerbs.AppDiscovering,
                Channel = channel ?? RoomChannel.Broadcast,
                Content = RoomContent.Parse(json)
            });
            await Task.Delay(100);
        }

        protected override void OnConnect(IRoomSocket socket)
        {
            base.OnConnect(socket);
            Connections = Connections.Clear();
        }

        protected override async void OnMessageReceived(RoomMessage message)
        {
            base.OnMessageReceived(message);
            try
            {
                if (message.Verb == RoomAppVerbs.AppAnnouncement && message.Content.Length > 0)
                {
                    var connection = Connections.FirstOrDefault(x => x.Channel == message.Channel);
                    var json = message.Content.ToString();
                    var announcement = JsonSerializer.Deserialize<RoomAppAnnouncementMessage>(json);
                    if (announcement != null)
                    {
                        var capable = Capabilities.Any(x => announcement.Manifest.Capabilities.Contains(x));
                        if (capable)
                        {
                            if (connection == null)
                            {
                                connection = new RoomAppConnection { Manifest = announcement.Manifest, Channel = message.Channel };
                                Connections = Connections.Add(connection);
                                ConnectionChanged?.Invoke(this, connection);
                                if (Behavior == RoomAppBehavior.DiscoverFirst) await AnnounceApp(message.Channel);
                            }
                            else if (connection != null)
                            {
                                connection.Manifest = announcement.Manifest;
                                ConnectionChanged?.Invoke(this, connection);
                            }
                        }
                        else if (connection != null)
                        {
                            Connections = Connections.Remove(connection);
                            ConnectionChanged?.Invoke(this, connection);
                        }
                    }
                }
                else if (message.Verb == RoomAppVerbs.AppDiscovering && message.Content.Length > 0)
                {
                    var connection = Connections.FirstOrDefault(x => x.Channel == message.Channel);
                    if (connection != null || Behavior == RoomAppBehavior.AnnounceFirst)
                    {
                        var json = message.Content.ToString();
                        var discovering = JsonSerializer.Deserialize<RoomAppDiscoveringMessage>(json);
                        if (discovering != null && Manifest.Capabilities.Any(x => discovering.Capabilities.Contains(x)))
                            await AnnounceApp(message.Channel);
                    }
                }
            }
            catch (Exception e)
            {
                var wrtier = LogWriter;
                if (wrtier != null) await wrtier.WriteLineAsync($"Room App Service exception: {e.Message}\n{e.StackTrace}");
            }
        }

        protected override void OnDisconnect(IRoomSocket socket)
        {
            base.OnDisconnect(socket);
            Connections = Connections.Clear();
        }

        /// <summary>
        /// Constructs a new Room App Service with the specified app manifest and the requested room apps capabilities.
        /// </summary>
        /// <param name="manifest">This app manifest.</param>
        /// <param name="capabilities">Requested other apps caoabpilities.</param>
        public RoomAppService(RoomAppManifest manifest, string[] capabilities)
        {
            Manifest = manifest;
            Capabilities = capabilities;
        }

    }
}