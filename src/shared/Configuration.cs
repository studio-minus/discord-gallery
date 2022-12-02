using System.Text.Json;
using System.Text.Json.Serialization;

namespace gallery.shared;

public class Configuration
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
}
