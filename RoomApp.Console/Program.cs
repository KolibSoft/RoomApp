using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using KolibSoft.RoomApp.Core;
using KolibSoft.Rooms.Core.Protocol;
using KolibSoft.Rooms.Core.Services;
using KolibSoft.Rooms.Core.Streams;

var impl = await args.GetOptionAsync("impl", ["TCP", "WEB"]);
var settings = await args.GetArgumentAsync("settings", "settings.json");
var options = await args.GetArgumentAsync("options", "options.json");

var behavior = await args.GetOptionAsync("behavior", ["Manual", "DiscoverFirst", "AnnounceFirst"], "DiscoverFirst") switch
{
    "Manual" => RoomAppBehavior.Manual,
    "DiscoverFirst" => RoomAppBehavior.DiscoverFirst,
    "AnnounceFirst" => RoomAppBehavior.AnnounceFirst,
    _ => throw new Exception("Invalid behavior"),
};
Console.WriteLine($"Using behavior: {behavior}");

Settings? _settings = null;
if (File.Exists(settings))
{
    var json = await File.ReadAllTextAsync(settings);
    _settings = JsonSerializer.Deserialize<Settings>(json);
}

var manifest = _settings?.AppManifest ?? new RoomAppManifest
{
    Id = Guid.NewGuid(),
    Name = $"App {args.GetHashCode()}",
    Capabilities = Array.Empty<string>()
};

var capabilities = _settings?.Capabilities ?? Array.Empty<string>();

if (impl == "TCP")
{
    var endpoint = await args.GetIPEndpointAsync("endpoint", new IPEndPoint(IPAddress.Any, 55000));
    Console.WriteLine($"Using endpoint: {endpoint}");
    var service = new Service(manifest, capabilities)
    {
        Logger = System.Console.Error.WriteLine,
        Behavior = behavior
    };
    using var client = new TcpClient();
    await client.ConnectAsync(endpoint!);
    using var stream = new RoomNetworkStream(client);
    await HandShake(stream, options);
    service.Start();
    CommandAsync(service);
    await service.ListenAsync(stream);
    service.Stop();
}
else if (impl == "WEB")
{
    var uri = await args.GetUriAsync("uri", new Uri("wss://localhost:55000/"));
    Console.WriteLine($"Using uri: {uri}");
    var service = new Service(manifest, capabilities)
    {
        Logger = System.Console.Error.WriteLine,
        Behavior = behavior
    };
    using var client = new ClientWebSocket();
    await client.ConnectAsync(uri!, default);
    using var stream = new RoomWebStream(client);
    await HandShake(stream, options);
    service.Start();
    CommandAsync(service);
    await service.ListenAsync(stream);
    service.Stop();
}
await Task.Delay(100);
Console.Write($"Press a key to exit...");
Console.ReadKey();
return;

static async Task HandShake(IRoomStream stream, string? options = null)
{
    Console.WriteLine($"Configuring connection");
    using var file = options != null && File.Exists(options) ? new FileStream(options, FileMode.Open, FileAccess.Read) : Stream.Null;
    await stream.WriteMessageAsync(new RoomMessage
    {
        Verb = "OPTIONS",
        Content = file
    });
    var message = await stream.ReadMessageAsync();
    if (message.Verb != "OPTIONS" || message.Channel != 0)
    {
        Console.WriteLine($"[{message.Channel}] {message.Verb}: {await message.Content.ReadAsTextAsync()}");
        throw new InvalidOperationException("Can not configure connection");
    }
    Console.WriteLine($"Connection configured");
}

static async void CommandAsync(RoomAppService service)
{
    while (service.IsRunning)
    {
        try
        {
            var command = await Task.Run(() => System.Console.ReadLine());
            switch (command)
            {
                case "discover": service.DiscoverApp(); break;
                case "announce": service.AnnounceApp(); break;
                case "connections":
                    foreach (var connection in service.Connections)
                        System.Console.WriteLine($"{connection.Manifest.Name} [{connection.Channel}]");
                    break;
            }
        }
        catch (Exception error)
        {
            service.Logger?.Invoke($"Room App error: {error}");
        }
    }
}


public static class ConsoleUtils
{

    public static Task<string?> PromptAsync(string? hint = "> ") => Task.Run(() =>
    {
        Console.Write(hint);
        var input = Console.ReadLine();
        return input;
    });

    [return: NotNullIfNotNull(nameof(def))]
    public static async Task<string?> GetArgumentAsync(this string[] args, string name, string? def = null, string? hint = null)
    {
        var argName = $"--{name}";
        string? argument = args.FirstOrDefault(x => x.StartsWith(argName) && (x.Length == argName.Length || x[argName.Length] == '='));
        if (argument != null)
        {
            if (argument.Length == argName.Length) return name;
            if (argument[argName.Length] == '=') return argument[(argName.Length + 1)..];
        }
        if (!string.IsNullOrWhiteSpace(def)) return def;
        while (string.IsNullOrWhiteSpace(argument)) argument = await PromptAsync(hint ?? $"{name}: ");
        return argument;
    }

    [return: NotNullIfNotNull(nameof(def))]
    public static async Task<string?> GetOptionAsync(this string[] args, string name, string[] options, string? def = null, string? hint = null)
    {
        string? option = await args.GetArgumentAsync(name, def, hint);
        while (!options.Contains(option)) option = await PromptAsync(hint ?? $"{name}: ");
        return option;
    }

    [return: NotNullIfNotNull(nameof(def))]
    public static async Task<int?> GetIntegerAsync(this string[] args, string name, int? def = null, string? hint = null)
    {
        int integer;
        while (!int.TryParse(await args.GetArgumentAsync(name, def?.ToString(), hint), out integer)) continue;
        return integer;
    }

    [return: NotNullIfNotNull(nameof(def))]
    public static async Task<IPEndPoint?> GetIPEndpointAsync(this string[] args, string name, IPEndPoint? def = null, string? hint = null)
    {
        IPEndPoint? endpoint;
        while (!IPEndPoint.TryParse((await args.GetArgumentAsync(name, def?.ToString(), hint))!, out endpoint)) continue;
        return endpoint;
    }

    [return: NotNullIfNotNull(nameof(def))]
    public static async Task<Uri?> GetUriAsync(this string[] args, string name, Uri? def = null, string? hint = null)
    {
        Uri? uri;
        while (!TryParse((await args.GetArgumentAsync(name, def?.ToString(), hint))!, out uri)) continue;
        return uri ?? def;
        static bool TryParse(string value, [NotNullWhen(true)] out Uri? uri)
        {
            try
            {
                uri = new Uri(value);
                return true;
            }
            catch
            {
                uri = null;
                return false;
            }
        }
    }

}

public class Service : RoomAppService
{

    protected override async ValueTask OnReceiveAsync(IRoomStream stream, RoomMessage message, CancellationToken token)
    {
        var clone = new MemoryStream();
        await message.Content.CopyToAsync(clone, token);
        message.Content.Seek(0, SeekOrigin.Begin);
        clone.Seek(0, SeekOrigin.Begin);
        Console.WriteLine($"[{message.Channel}]< {message.Verb}: {await clone.ReadAsTextAsync(token: token)}");
        await base.OnReceiveAsync(stream, message, token);
    }

    protected override async ValueTask OnSendAsync(IRoomStream stream, RoomMessage message, CancellationToken token)
    {
        var clone = new MemoryStream();
        await message.Content.CopyToAsync(clone, token);
        message.Content.Seek(0, SeekOrigin.Begin);
        clone.Seek(0, SeekOrigin.Begin);
        Console.WriteLine($"[{message.Channel}]> {message.Verb}: {await clone.ReadAsTextAsync(token: token)}");
        await base.OnSendAsync(stream, message, token);
    }

    protected override void OnStart()
    {
        base.OnStart();
        Console.WriteLine("Service started");
    }

    protected override void OnStop()
    {
        base.OnStop();
        Console.WriteLine("Service stopped");
    }

    public Service(RoomAppManifest manifest, string[] capabilities) : base(manifest, capabilities) { }

}

public class Settings
{
    public RoomStreamOptions? StreamOptions { get; set; }
    public RoomServiceOptions? ServiceOptions { get; set; }
    public RoomAppManifest? AppManifest { get; set; }
    public string[]? Capabilities { get; set; }
}