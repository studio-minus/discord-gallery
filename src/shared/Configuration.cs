﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace gallery.shared;

public class Configuration : IEquatable<Configuration?>
{
    public string ArtPath { get; set; } = "art";
    public string FrontPath { get; set; } = "www";
    public string ArtworkFontPath { get; set; } = "assets/Vollkorn.ttf";

    public string DiscordBotToken { get; set; } = "invalid";

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public ulong ChannelId { get; set; } = default;
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public ulong GuildId { get; set; } = default;

    public string Ip { get; set; } = "127.0.0.1";
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Port { get; set; } = 8080;

    public string? SslCert { get; set; } = null;
    public string? SslCertPassword { get; set; } = null;

    public static Configuration Current = new();

    public static void Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine("Config file \"{0}\" does not exist", path);
            return;
        }

        var loaded = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(path));
        if (loaded != null)
            Current = loaded;
        else
            Console.Error.WriteLine("Attempt to load null configuration");
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Configuration);
    }

    public bool Equals(Configuration? other)
    {
        return other is not null &&
               ArtPath == other.ArtPath &&
               FrontPath == other.FrontPath &&
               ArtworkFontPath == other.ArtworkFontPath &&
               DiscordBotToken == other.DiscordBotToken &&
               ChannelId == other.ChannelId &&
               GuildId == other.GuildId &&
               Ip == other.Ip &&
               Port == other.Port &&
               SslCert == other.SslCert &&
               SslCertPassword == other.SslCertPassword;
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        hash.Add(ArtPath);
        hash.Add(FrontPath);
        hash.Add(ArtworkFontPath);
        hash.Add(DiscordBotToken);
        hash.Add(ChannelId);
        hash.Add(GuildId);
        hash.Add(Ip);
        hash.Add(Port);
        hash.Add(SslCert);
        hash.Add(SslCertPassword);
        return hash.ToHashCode();
    }

    public static bool operator ==(Configuration? left, Configuration? right)
    {
        return EqualityComparer<Configuration>.Default.Equals(left, right);
    }

    public static bool operator !=(Configuration? left, Configuration? right)
    {
        return !(left == right);
    }
}
