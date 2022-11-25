using HttpServerLite;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HttpMethod = HttpServerLite.HttpMethod;

namespace gallery;
public class Program
{
    private static readonly ConcurrentDictionary<string, Artwork> artworks = new();
    private static readonly ConcurrentDictionary<(string, int, int), byte[]> imgCache = new();

    public static async Task Main(string[] args)
    {
        Configuration.Load("config.json");
        var path = Path.GetFullPath(Configuration.Current.ArtPath);
        var dir = new DirectoryInfo(path);

        var found = new List<Artwork>();
        foreach (var item in dir.EnumerateFiles("*.json"))
        {
            Console.WriteLine("Art found: {0}", Path.GetFileNameWithoutExtension(item.Name));
            try
            {
                var artwork = JsonSerializer.Deserialize<Artwork>(File.ReadAllText(item.FullName));
                if (artwork != null)
                    found.Add(artwork);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to read artwork {0}: {1}", item.Name, e);
            }
        }

        foreach (var item in found.OrderBy(static a => a.Interactions))
            artworks.TryAdd(shortid.ShortId.Generate(), item);

        Webserver server;
        if (Configuration.Current.SslCert == null)
            server = new Webserver(Configuration.Current.Ip, Configuration.Current.Port, Index);
        else
            server = new Webserver(Configuration.Current.Ip, Configuration.Current.Port, true, Configuration.Current.SslCert, Configuration.Current.SslCertPassword, Index);

        server.Settings.Debug.Responses = true;
        server.Settings.Debug.Routing = true;
        server.Settings.Headers.Host = "https://" + server.Settings.Hostname + ":" + server.Settings.Port;

        server.Events.Logger = Console.WriteLine;
        server.Events.Exception += (o, e) =>
        {
            Console.Error.WriteLine("{0}: {1}", e.Url, e.Exception.Message);
        };
        server.Routes.Content.Add("/www/", true);
        // server.Routes.Content.BaseDirectory = "www";
        server.Start();

        Console.WriteLine("Listening on {0}:{1} ", server.Settings.Hostname, server.Settings.Port);
        Console.WriteLine("Press ENTER to exit");
        Console.ReadLine();
    }

    [StaticRoute(HttpMethod.GET, "/art")]
    public static async Task GetAllArt(HttpContext ctx)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        int c = 0;
        foreach (var item in artworks.OrderBy(static d => d.Value.Interactions))
        {
            sb.AppendFormat("\"{0}\"", item.Key);
            if (++c != artworks.Count)
                sb.Append(", ");
        }
        sb.Append(']');
        await ctx.Response.SendAsync(sb.ToString());
    }

    [ParameterRoute(HttpMethod.GET, "/art/{id}/image/{w}/{h}")]
    public static async Task GetArtImage(HttpContext ctx)
    {
        var id = ctx.Request.Url.Parameters["id"];
        int w = int.Parse(ctx.Request.Url.Parameters["w"]);
        int h = int.Parse(ctx.Request.Url.Parameters["h"]);

        w = Math.Min(1024, Math.Max(128, w));
        h = Math.Min(1024, Math.Max(128, h));

        byte[]? b;
        if (artworks.TryGetValue(id, out var artwork))
        {
            if (!imgCache.TryGetValue((id, w, h), out b))
            {
                var img = await ArtCache.Load(w, h, artwork);
                using var m = new MemoryStream();
                img.Save(m, new WebpEncoder() { Quality = 95 });
                b = m.ToArray();
                if (!imgCache.TryAdd((id, w, h), b))
                    Console.Error.WriteLine("Failed to cache artwork {0}", id);
            }
            ctx.Response.ContentType = "image/webp";
            await ctx.Response.SendAsync(b);
        }
        else if (!imgCache.TryGetValue(default, out b)) //not found, send wtf img
        {
            using var m = new MemoryStream();
            var img = Image.Load<Rgba32>("error.png");
            img.Save(m, new WebpEncoder() { Quality = 100 });
            b = m.ToArray();
            if (!imgCache.TryAdd(default, b))
                Console.Error.WriteLine("Failed to cache artwork {0}", id);
        }

        ctx.Response.ContentType = "image/webp";
        await ctx.Response.SendAsync(b);
    }

    [ParameterRoute(HttpMethod.GET, "/art/{id}")]
    public static async Task GetArt(HttpContext ctx)
    {
        var id = ctx.Request.Url.Parameters["id"];
        if (artworks.TryGetValue(id, out var artwork))
        {
            await ctx.Response.SendAsync(JsonSerializer.Serialize(new
            {
                artwork.Name,
                artwork.Author,
                artwork.Description,
                artwork.Interactions,
                Src = $"art/{id}/image/512/512"
            }));
        }

        ctx.Response.StatusCode = 404;
        await ctx.Response.SendAsync("Artwork not found");
    }

    [ParameterRoute(HttpMethod.POST, "/art")]
    public static async Task UploadArt(HttpContext ctx)
    {
        var b = new byte[1024];
        int count = ctx.Request.Data.Read(b);
        var body = JsonSerializer.Deserialize<UploadArwork>(Encoding.UTF8.GetString(b.AsSpan(0, count)));
        var id = shortid.ShortId.Generate();
        var art = new Artwork()
        {
            Author = body.Author,
            Name = body.Name,
            Interactions = body.Interactions,
            ImageData = body.ImageData
        };
        bool success = artworks.TryAdd(id, art);

        if (success)
        {
            File.WriteAllText(Configuration.Current.ArtPath + "/" + id + ".json", JsonSerializer.Serialize(art));
        }

        ctx.Response.StatusCode = success ? 200 : 500;
        await ctx.Response.SendAsync(success ? id : "Failed to add artwork to dictionary");
    }

    [StaticRoute(HttpMethod.GET, "/")]
    public static async Task Index(HttpContext ctx)
    {
        ctx.Response.ContentType = "text/html";
        await ctx.Response.SendAsync(File.ReadAllBytes("www/index.html"));
    }

    public class UploadArwork
    {
        public string Name { get; set; } = "Untitled";
        public string Author { get; set; } = "Unknown";
        public string ImageData { get; set; } = "https://i.imgur.com/ivtJ4zT_d.webp";
        public int Interactions { get; set; } = 0;
    }
}