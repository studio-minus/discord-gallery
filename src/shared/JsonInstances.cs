using Newtonsoft.Json;

namespace gallery.shared;

public static class JsonInstances
{
    public static readonly JsonSerializerSettings Settings;

    static JsonInstances()
    {
        Settings = new JsonSerializerSettings
        {
            Formatting =  Formatting.Indented,
            TypeNameHandling = TypeNameHandling.All,
        };
    }
}