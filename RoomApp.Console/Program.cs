using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using KolibSoft.RoomApp.Core;
using KolibSoft.Rooms.Core.Protocol;
using KolibSoft.Rooms.Core.Services;
using KolibSoft.Rooms.Core.Sockets;

namespace KolibSoft.RoomApp.Console;

public class Serice : RoomAppService
{

    protected override void OnOnline(IRoomSocket socket)
    {
        base.OnOnline(socket);
        System.Console.WriteLine("Service is online");
    }

    protected override void OnOffline(IRoomSocket socket)
    {
        base.OnOnline(socket);
        System.Console.WriteLine("Service is offline");
    }

    protected override void OnMessageReceived(RoomMessage message)
    {
        base.OnMessageReceived(message);
        System.Console.SetCursorPosition(0, System.Console.CursorTop);
        System.Console.WriteLine($"{message.Verb} [{message.Channel}] {message.Content}");
        System.Console.Write("> ");
    }

    protected override void OnMessageSent(RoomMessage message)
    {
        base.OnMessageSent(message);
        System.Console.SetCursorPosition(0, System.Console.CursorTop);
        System.Console.WriteLine($"{message.Verb} [{message.Channel}] {message.Content}");
        System.Console.Write("> ");
    }

    public Serice(RoomAppManifest manifest, string[] capabilities) : base(manifest, capabilities) { }

}

public static class Program
{

    public static string? Prompt(string? hint = "> ")
    {
        System.Console.Write(hint);
        var input = System.Console.ReadLine();
        return input;
    }

    public static string? GetArgument(this string[] args, string name, string? hint = null, bool required = false)
    {
        var argName = $"--{name}";
        string? argument = args.FirstOrDefault(x => x.StartsWith(argName) && (x.Length == argName.Length || x[argName.Length] == '='));
        if (argument != null)
        {
            if (argument.Length == argName.Length) return name;
            if (argument[argName.Length] == '=') return argument[(argName.Length + 1)..];
        }
        while (required && string.IsNullOrWhiteSpace(argument)) argument = Prompt(hint ?? $"{name}: ");
        return argument;
    }

    public static string? GetOption(this string[] args, string name, string[] options, string? hint = null, bool required = false)
    {
        string? option = args.GetArgument(name, hint, required);
        while (required && !options.Contains(option)) option = Prompt(hint ?? $"{name}: ");
        return option;
    }

    public static int? GetInteger(this string[] args, string name, string? hint = null, bool required = false)
    {
        bool parsed;
        int integer;
        while (!(parsed = int.TryParse(args.GetArgument(name, hint, required), out integer)) && required) continue;
        return parsed ? integer : null;
    }

    public static IPEndPoint? GetIPEndpoint(this string[] args, string name, string? hint = null, bool required = false)
    {
        IPEndPoint? endpoint;
        while (!IPEndPoint.TryParse(args.GetArgument(name, hint, required)!, out endpoint) && required) continue;
        return endpoint;
    }

    public static Uri? GetUri(this string[] args, string name, string? hint = null, bool required = false)
    {
        Uri? uri;
        while (!TryParse(args.GetArgument(name, hint, required)!, out uri) && required) continue;
        return uri;
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

    public static string[]? GetArray(this string[] args, string name, string? hint = null, bool required = false)
    {
        string[]? array;
        while (!TryParse(args.GetArgument(name, hint, required)!, out array) && required) continue;
        return array;
        static bool TryParse(string value, [NotNullWhen(true)] out string[]? array)
        {
            try
            {
                array = value.Split(",");
                return true;
            }
            catch
            {
                array = null;
                return false;
            }
        }
    }

    public static async Task Main(params string[] args)
    {
        var buffering = args.GetInteger("buff") ?? 1024;
        var rating = args.GetInteger("rate") ?? 1024;
        var impl = args.GetOption("impl", ["TCP", "WEB"], null, true);
        var behavior = (args.GetOption("behavior", ["Manual", "DiscoverFirst", "AnnounceFirst"], null, false) ?? "DiscoverFirst") switch
        {
            "DiscoverFirst" => RoomAppBehavior.DiscoverFirst,
            "AnnounceFirst" => RoomAppBehavior.AnnounceFirst,
            _ => RoomAppBehavior.Manual
        };
        var manifest = new RoomAppManifest
        {
            Id = Guid.NewGuid(),
            Name = args.GetArgument("name", null, false) ?? $"App {Random.Shared.Next():x8}",
            Capabilities = args.GetArray("appCaps", null, false) ?? []
        };
        var capabilities = args.GetArray("connCaps", null, false) ?? [];
        if (impl == "TCP")
        {
            var server = args.GetArgument("server") ?? "127.0.0.1:55000";
            System.Console.WriteLine($"Using server: {server}");
            var service = new Serice(manifest, capabilities)
            {
                Logger = System.Console.Error,
                Behavior = behavior
            };
            await service.ConnectAsync(server, RoomService.TCP, rating);
            await CommandAsync(service);
        }
        else if (impl == "WEB")
        {
            var server = args.GetArgument("server") ?? "ws://localhost:55000/";
            System.Console.WriteLine($"Using server: {server}");
            var service = new Serice(manifest, capabilities)
            {
                Logger = System.Console.Error,
                Behavior = behavior
            };
            await service.ConnectAsync(server, RoomService.WEB, rating);
            await CommandAsync(service);
        }
        await Task.Delay(100);
        System.Console.Write($"Press a key to exit...");
        System.Console.ReadKey();
    }

    public static async Task CommandAsync(RoomAppService service)
    {
        while (service.IsOnline)
        {
            try
            {
                var command = Prompt("> ");
                switch (command)
                {
                    case "discover": await service.DiscoverApp(); break;
                    case "announce": await service.AnnounceApp(); break;
                }
            }
            catch (Exception error)
            {
                if (service.Logger != null) await service.Logger.WriteLineAsync($"Room App error: {error}");
            }
        }
    }

}