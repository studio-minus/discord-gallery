using System.Text.Json;

namespace gallery;

public class Configuration
{
    public string ArtPath { get; set; } = "art";
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;

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
