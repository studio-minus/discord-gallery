using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using WatsonWebserver;

namespace gallery;

public class Program
{
    public class Configuration
    {
        public string ArtPath { get; set; } = "art";
        public string Ip { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 8090;
    }

    public class Artwork
    {
        public string? Name { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public int Interactions { get; set; }
        public string ImageData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        [System.NonSerialized]
        public Image<Rgb24> Img;

        public async Task Initialise()
        {
            const int maxSize = 1024;

            var bytes = Array.Empty<byte>();
            if (ImageData.StartsWith("data:"))
                bytes = Convert.FromBase64String(ImageData[(ImageData.LastIndexOf(',') + 1)..]);
            else if (ImageData.StartsWith("http"))
            {
                using var r = new HttpClient();
                var response = await r.GetAsync(ImageData);
                bytes = await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                bytes = File.ReadAllBytes(ImageData);
            }

            Img = Image.Load<Rgb24>(bytes);
            Img.Mutate((i) =>
            {
                if (Img.Width > maxSize || Img.Height > maxSize)
                {
                    float aspectRatio = (Img.Height / (float)Img.Width);
                    if (Img.Width > Img.Height)
                        i.Resize(maxSize, (int)(maxSize * aspectRatio));
                    else
                        i.Resize((int)(maxSize / aspectRatio), maxSize);
                }
            });

            Width = Img.Width;
            Height = Img.Height;
        }
    }

    public static Configuration Config = new Configuration();
    public static Dictionary<int, Artwork> Artworks = new();

    private static readonly Dictionary<int, byte[]> imgCache = new();

    public static async Task Main(string[] args)
    {
        var path = Path.GetFullPath(Config.ArtPath);
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
                    Artworks.Add(Artworks.Count, artwork);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to read artwork {0}: {1}", item.Name, e);
            }
        }

        var server = new Server(Config.Ip, Config.Port, false);
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
        foreach (var item in Artworks)
            sb.AppendFormat("\"{0}\"", item.Key);
        sb.Append(']');
        await ctx.Response.Send(sb.ToString());
    }

    [ParameterRoute(WatsonWebserver.HttpMethod.GET, "/img/{id}")]
    public static async Task GetArtImage(HttpContext ctx)
    {
        if (int.TryParse(ctx.Request.Url.Parameters["id"], out var id) && Artworks.TryGetValue(id, out var artwork))
        {
            if (!imgCache.TryGetValue(id, out var b))
            {
                using var m = new MemoryStream();
                artwork.Img.Save(m, new WebpEncoder() { Quality = 80 });
                b = m.ToArray();
                imgCache.Add(id, b);
            }
            ctx.Response.ContentType = "image/webp";
            await ctx.Response.Send(b);
        }
        else
        {
            //not found, send wtf img
            if (!imgCache.TryGetValue(-1, out var b))
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

        ctx.Response.StatusCode = 404;
        await ctx.Response.Send("Artwork not found");
    }

    [ParameterRoute(WatsonWebserver.HttpMethod.GET, "/art/{id}")]
    public static async Task GetArt(HttpContext ctx)
    {
        if (int.TryParse(ctx.Request.Url.Parameters["id"], out var id) && Artworks.TryGetValue(id, out var artwork))
        {
            await ctx.Response.Send(JsonSerializer.Serialize(new
            {
                artwork.Name,
                artwork.Author,
                artwork.Description,
                artwork.Interactions,
                Src = $"/img/{id}"
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