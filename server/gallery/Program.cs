using HttpServerLite;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using HttpMethod = HttpServerLite.HttpMethod;

namespace gallery;
public class Program
{
    private static ReadOnlyDictionary<int, Artwork> artworks;
    private static readonly ConcurrentDictionary<int, byte[]> imgCache = new();

    public static async Task Main(string[] args)
    {
        Configuration.Load("config.json");
        var path = Path.GetFullPath(Configuration.Current.ArtPath);
        var dir = new DirectoryInfo(path);

        var found = new List<Artwork>();
        var art = new Dictionary<int, Artwork>();
        foreach (var item in dir.EnumerateFiles("*.json"))
        {
            Console.WriteLine("Art found: {0}", Path.GetFileNameWithoutExtension(item.Name));
            try
            {
                var artwork = JsonSerializer.Deserialize<Artwork>(File.ReadAllText(item.FullName));
                if (artwork != null)
                {
                    await artwork.Initialise();
                    found.Add(artwork);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to read artwork {0}: {1}", item.Name, e);
            }
        }

        foreach (var item in found.OrderBy(static a => a.Interactions))
            art.Add(art.Count, item);
        artworks = new ReadOnlyDictionary<int, Artwork>(art);

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
        foreach (var item in artworks)
        {
            sb.AppendFormat("\"{0}\"", item.Key);
            if (++c != artworks.Count)
                sb.Append(", ");
        }
        sb.Append(']');
        await ctx.Response.SendAsync(sb.ToString());
    }

    [ParameterRoute(HttpMethod.GET, "/art/{id}/image")]
    public static async Task GetArtImage(HttpContext ctx)
    {
        var b = Array.Empty<byte>();

        if (int.TryParse(ctx.Request.Url.Parameters["id"], out var id) && artworks.TryGetValue(id, out var artwork))
        {
            if (!imgCache.TryGetValue(id, out b))
            {
                using var m = new MemoryStream();
                artwork.Img.Save(m, new WebpEncoder() { Quality = 80 });
                b = m.ToArray();
                if (!imgCache.TryAdd(id, b))
                    Console.Error.WriteLine("Failed to cache artwork {0}", id);
            }
            ctx.Response.ContentType = "image/webp";
            await ctx.Response.SendAsync(b);
        }
        else if (!imgCache.TryGetValue(-1, out b)) //not found, send wtf img
        {
            using var m = new MemoryStream();
            var img = Image.Load<Rgb24>("error.png");
            img.Save(m, new WebpEncoder() { Quality = 80 });
            b = m.ToArray();
            if (!imgCache.TryAdd(-1, b))
                Console.Error.WriteLine("Failed to cache artwork {0}", id);
        }

        ctx.Response.ContentType = "image/webp";
        await ctx.Response.SendAsync(b);
    }

    [ParameterRoute(HttpMethod.GET, "/art/{id}")]
    public static async Task GetArt(HttpContext ctx)
    {
        if (int.TryParse(ctx.Request.Url.Parameters["id"], out var id) && artworks.TryGetValue(id, out var artwork))
        {
            await ctx.Response.SendAsync(JsonSerializer.Serialize(new
            {
                artwork.Name,
                artwork.Author,
                artwork.Description,
                artwork.Interactions,
                Src = $"art/{id}/image"
            }));
        }

        ctx.Response.StatusCode = 404;
        await ctx.Response.SendAsync("Artwork not found");
    }

    [StaticRoute(HttpMethod.GET, "/")]
    public static async Task Index(HttpContext ctx)
    {
        ctx.Response.ContentType = "text/html";
        await ctx.Response.SendAsync(File.ReadAllBytes("www/index.html"));
    }
}