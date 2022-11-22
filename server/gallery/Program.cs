using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using System.Text.Json;
using WatsonWebserver;

namespace gallery;
public class Program
{
    private static Dictionary<int, Artwork> artworks = new();
    private static readonly Dictionary<int, byte[]> imgCache = new();

    public static async Task Main(string[] args)
    {
        Configuration.Load("config.json");

        var path = Path.GetFullPath(Configuration.Current.ArtPath);
        var dir = new DirectoryInfo(path);
        foreach (var item in dir.EnumerateFiles("*.json"))
        {
            Console.WriteLine("Art found: {0}", Path.GetFileNameWithoutExtension(item.Name));
            try
            {
                var artwork = JsonSerializer.Deserialize<Artwork>(File.ReadAllText(item.FullName));
                if (artwork != null)
                {
                    await artwork.Initialise();
                    artworks.Add(artworks.Count, artwork);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to read artwork {0}: {1}", item.Name, e);
            }
        }

        var server = new Server(Configuration.Current.Ip, Configuration.Current.Port, false);
        server.Events.Logger = Console.WriteLine;
        server.Events.ExceptionEncountered += (o, e) =>
        {
            Console.Error.WriteLine("{0}: {1}", e.Url, e.Exception.Message);
        };
        server.Routes.Content.Add("/www/", true);
        // server.Routes.Content.BaseDirectory = "www";
        server.Start();

        Console.WriteLine("Press ENTER to exit");
        Console.ReadLine();
    }

    [StaticRoute(WatsonWebserver.HttpMethod.GET, "/art")]
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
        await ctx.Response.Send(sb.ToString());
    }

    [ParameterRoute(WatsonWebserver.HttpMethod.GET, "/art/{id}/image")]
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
                imgCache.Add(id, b);
            }
            ctx.Response.ContentType = "image/webp";
            await ctx.Response.Send(b);
        }
        else if (!imgCache.TryGetValue(-1, out b)) //not found, send wtf img
        {
            using var m = new MemoryStream();
            var img = Image.Load<Rgb24>("error.png");
            img.Save(m, new WebpEncoder() { Quality = 80 });
            b = m.ToArray();
            imgCache.Add(-1, b);
        }

        ctx.Response.ContentType = "image/webp";
        await ctx.Response.Send(b);
    }

    [ParameterRoute(WatsonWebserver.HttpMethod.GET, "/art/{id}")]
    public static async Task GetArt(HttpContext ctx)
    {
        if (int.TryParse(ctx.Request.Url.Parameters["id"], out var id) && artworks.TryGetValue(id, out var artwork))
        {
            await ctx.Response.Send(JsonSerializer.Serialize(new
            {
                artwork.Name,
                artwork.Author,
                artwork.Description,
                artwork.Interactions,
                Src = $"art/{id}/image"
            }));
        }

        ctx.Response.StatusCode = 404;
        await ctx.Response.Send("Artwork not found");
    }

    [StaticRoute(WatsonWebserver.HttpMethod.GET, "/")]
    public static async Task Index(HttpContext ctx)
    {
        ctx.Response.ContentType = "text/html";
        await ctx.Response.Send(File.ReadAllBytes("www/index.html"));
    }
}