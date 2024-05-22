using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KolibSoft.Rooms.Core.Protocol;
using KolibSoft.Rooms.Core.Services;
using KolibSoft.Rooms.Core.Streams;

namespace KolibSoft.RoomApp.Core
{

    /// <summary>
    /// Room App Service.
    /// </summary>
    public class RoomAppService : RoomService
    {

        public RoomAppManifest Manifest { get; set; }
        public string[] Capabilities { get; set; }
        public RoomAppBehavior Behavior { get; set; } = RoomAppBehavior.DiscoverFirst;
        public ImmutableArray<RoomAppConnection> Connections { get; private set; } = ImmutableArray.Create<RoomAppConnection>();
        public event EventHandler<RoomAppConnection>? ConnectionChanged;

        protected override async ValueTask OnReceiveAsync(IRoomStream stream, RoomMessage message, CancellationToken token)
        {
            try
            {
                if (message.Verb == RoomAppVerbs.AppAnnouncement && message.Content.Length > 0)
                {
                    var connection = Connections.FirstOrDefault(x => x.Channel == message.Channel);
                    var announcement = await message.Content.ReadAsJsonAsync<AnnouncementMessage>(token: token);
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
                                if (Behavior == RoomAppBehavior.DiscoverFirst) AnnounceApp(message.Channel);
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
                        var discovering = await message.Content.ReadAsJsonAsync<DiscoveringMessage>(token: token);
                        if (discovering != null && Manifest.Capabilities.Any(x => discovering.Capabilities.Contains(x)))
                            AnnounceApp(message.Channel);
                    }
                }
            }
            catch (Exception error)
            {
                Logger?.Invoke($"Room App Service error: {error}");
            }
        }

        public override async ValueTask ListenAsync(IRoomStream stream, CancellationToken token = default)
        {
            if (_stream != null) throw new InvalidOperationException("Stream already listening");
            _stream = stream;
            await base.ListenAsync(stream, token);
            _stream = null;
        }

        public override void Enqueue(IRoomStream stream, RoomMessage message)
        {
            if (_stream != stream) throw new InvalidOperationException("Stream already listening");
            base.Enqueue(stream, message);
        }

        public void Send(RoomMessage message)
        {
            if (_stream == null) throw new InvalidOperationException("Stream not listening");
            Enqueue(_stream, message);
        }

        public async void AnnounceApp(int channel = -1)
        {
            Send(new RoomMessage
            {
                Verb = RoomAppVerbs.AppAnnouncement,
                Channel = channel,
                Content = await RoomContentUtils.CreateAsJsonAsync(new AnnouncementMessage { Manifest = Manifest })
            });
        }

        public async void DiscoverApp(int channel = -1)
        {
            Send(new RoomMessage
            {
                Verb = RoomAppVerbs.AppDiscovering,
                Channel = channel,
                Content = await RoomContentUtils.CreateAsJsonAsync(new DiscoveringMessage { Capabilities = Capabilities })
            });
        }

        public RoomAppService(RoomAppManifest manifest, string[] capabilities)
        {
            Manifest = manifest;
            Capabilities = capabilities;
        }

        private IRoomStream? _stream;

        private class AnnouncementMessage
        {
            public RoomAppManifest Manifest { get; set; } = new RoomAppManifest();
        }

        public class DiscoveringMessage
        {
            public string[] Capabilities { get; set; } = Array.Empty<string>();
        }

    }
}