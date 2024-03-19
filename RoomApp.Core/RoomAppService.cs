using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using KolibSoft.Rooms.Core;

namespace KolibSoft.RoomApp.Core
{
    public class RoomAppService : RoomService
    {

        public RoomAppManifest Manifest { get; set; }
        public string[] Capabilities { get; set; }
        public RoomAppBehavior Behavior { get; set; } = RoomAppBehavior.Default;
        public RoomAppStatus Status { get; private set; } = RoomAppStatus.Offline;
        public ImmutableArray<RoomAppConnection> Connections { get; private set; } = ImmutableArray.Create<RoomAppConnection>();

        public event EventHandler<RoomAppStatus>? StatusChanged;
        public event EventHandler<RoomAppConnection>? ConnectionChanged;

        public async Task AnnounceApp(RoomChannel? channel = null)
        {
            var json = JsonSerializer.Serialize(Manifest);
            await SendAsync(new RoomMessage
            {
                Verb = RoomAppVerbs.AppAnnouncement,
                Channel = channel ?? RoomChannel.Broadcast,
                Content = RoomContent.Parse(json)
            });
        }

        public async Task DiscoverApp(RoomChannel? channel = null)
        {
            var json = JsonSerializer.Serialize(Capabilities);
            await SendAsync(new RoomMessage
            {
                Verb = RoomAppVerbs.AppDiscovering,
                Channel = channel ?? RoomChannel.Broadcast,
                Content = RoomContent.Parse(json)
            });
        }

        protected override void OnConnect(IRoomSocket socket)
        {
            base.OnConnect(socket);
            if (Socket == socket)
            {
                Status = RoomAppStatus.Online;
                Connections = Connections.Clear();
                StatusChanged?.Invoke(this, RoomAppStatus.Online);
            }
        }

        protected override async void OnMessageReceived(RoomMessage message)
        {
            base.OnMessageReceived(message);
            if (message.Verb == RoomAppVerbs.AppAnnouncement)
            {
                var json = message.Content.ToString();
                var manifest = JsonSerializer.Deserialize<RoomAppManifest>(json);
                if (manifest != null)
                {
                    var connection = Connections.FirstOrDefault(x => x.Manifest.Id == manifest.Id);
                    var capable = Capabilities.Any(x => manifest.Capabilities.Contains(x));
                    if (connection == null && capable)
                    {
                        connection = new RoomAppConnection { Manifest = manifest, Channel = message.Channel };
                        Connections = Connections.Add(connection);
                        ConnectionChanged?.Invoke(this, connection);
                    }
                    else if (connection?.Channel == message.Channel && capable)
                    {
                        connection.Manifest = manifest;
                        ConnectionChanged?.Invoke(this, connection);
                    }
                    else if (connection != null)
                    {
                        Connections = Connections.Remove(connection);
                        ConnectionChanged?.Invoke(this, connection);
                    }
                }
            }
            else if (message.Verb == RoomAppVerbs.AppDiscovering)
            {
                if (Behavior.HasFlag(RoomAppBehavior.Announce)) await AnnounceApp(message.Channel);
                if (Behavior.HasFlag(RoomAppBehavior.Discover)) await DiscoverApp(message.Channel);
            }
        }

        protected override void OnDisconnect(IRoomSocket socket)
        {
            base.OnDisconnect(socket);
            if (Socket == socket)
            {
                Status = RoomAppStatus.Offline;
                Connections = Connections.Clear();
                StatusChanged?.Invoke(this, RoomAppStatus.Offline);
            }
        }

        public RoomAppService(RoomAppManifest manifest, string[] capabilities)
        {
            Manifest = manifest;
            Capabilities = capabilities;
        }

    }
}