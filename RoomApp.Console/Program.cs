using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using KolibSoft.RoomApp.Core;
using KolibSoft.Rooms.Core;

namespace KolibSoft.Rooms.Console;

public class Service : RoomAppService
{

    protected override void OnConnect(IRoomSocket socket)
    {
        base.OnConnect(socket);
        System.Console.WriteLine("Service Online");
    }

    protected override void OnDisconnect(IRoomSocket socket)
    {
        base.OnDisconnect(socket);
        System.Console.WriteLine("Service Offline");
    }

    public Service(RoomAppManifest manifest, string[] capabilities) : base(manifest, capabilities)
    {
        ConnectionChanged += (s, e) =>
        {
            if (Connections.Contains(e)) System.Console.WriteLine($"Connection online: {e.Manifest.Name}");
            else System.Console.WriteLine($"Connection offline: {e.Manifest.Name}");
        };
    }

}

public static class Program
{

    public static string? Prompt(string hint)
    {
        System.Console.Write(hint);
        var input = System.Console.ReadLine();
        return input;
    }

    public static string GetArgument(this string[] args, string name, string? hint = null, string? def = null)
    {
        var arg = args.FirstOrDefault(x => x.StartsWith(name));
        if (arg != null)
        {
            if (arg.Length == name.Length) return arg;
            if (arg[name.Length] == '=') return arg.Substring(name.Length + 1);
        }
        string? input = null;
        while (input == null) input = Prompt(hint ?? $"{name}: ") ?? def;
        return input;
    }

    public static int EnsureInteger(Func<string> func, int min = int.MinValue, int max = int.MaxValue)
    {
        if (max < min)
            throw new ArgumentException("Min value and max value overlaps");
        while (true) if (int.TryParse(func(), out int integer) && integer >= min && integer <= max) return integer;
    }

    public static Uri EnsureUri(Func<string> func)
    {
        while (true) if (Uri.TryCreate(func(), UriKind.RelativeOrAbsolute, out Uri? uri)) return uri;
    }

    public static string GetOption(this string[] args, string name, string[] options, string? hint = null)
    {
        if (!options.Any())
            throw new ArgumentException("Options can not be empty");
        while (true)
        {
            var input = args.GetArgument(name, hint, null);
            if (options.Contains(input)) return input;
        }
    }

    public static async Task Main(params string[] args)
    {
        var name = args.GetArgument("--name", null, "Room App");
        var server = args.GetArgument("--server");
        var impl = args.GetOption("--impl", ["TCP", "WEB"]);
        var behavior = args.GetOption("--behavior", ["Manual", "AnnounceFirst", "DiscoverFirst"]);
        var appCaps = args.GetArgument("--appCaps").Split(",");
        var conCaps = args.GetArgument("--conCaps").Split(",");
        var manifest = new RoomAppManifest
        {
            Id = Guid.NewGuid(),
            Name = name,
            Capabilities = appCaps
        };
        var service = new Service(manifest, conCaps);
        service.Behavior = behavior switch
        {
            "AnnounceFirst" => RoomAppBehavior.AnnounceFirst,
            "DiscoverFirst" => RoomAppBehavior.DiscoverFirst,
            _ => RoomAppBehavior.Manual
        };
        service.LogWriter = System.Console.Out;
        await service.ConnectAsync(server, impl);
        while (service.Status == RoomServiceStatus.Online) await Task.Delay(100);
    }

}