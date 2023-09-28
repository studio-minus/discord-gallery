using gallery.shared;
using HttpServerLite;
using Newtonsoft.Json;
using SixLabors.ImageSharp.Formats.Webp;
using System.Collections.Concurrent;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using Configuration = gallery.shared.Configuration;
using HttpMethod = HttpServerLite.HttpMethod;

namespace gallery.front;

public class ArtGalleryFront : IDisposable
{
    public readonly DirectoryInfo ArtDirectory;

    private static readonly ConcurrentDictionary<string, Submission> artworks = new();
    private static readonly ConcurrentDictionary<string, byte[]> responseCache = new();
    private static readonly ImageArtCache imageArtCache;
    private static readonly ImageArtCache discArtCache;

    private Webserver? server;

    static ArtGalleryFront()
    {
        imageArtCache = new ImageArtCache(new AuctionArtRenderer(Configuration.Current.ArtworkFontPath));
        discArtCache = new ImageArtCache(new DiscArtRenderer(Configuration.Current.DiscOverlayPath));
    }

    public ArtGalleryFront(DirectoryInfo artDirectory)
    {
        ArtDirectory = artDirectory;

        RefreshArtDirectory();
    }

    public void ClearCache()
    {
        artworks.Clear();
        responseCache.Clear();
        imageArtCache.Clear();
    }

    public void RefreshArtDirectory()
    {
        ClearCache();

        var found = new List<Submission>();
        foreach (var item in ArtDirectory.EnumerateFiles("*.json"))
        {
            Console.WriteLine("Art found: {0}", Path.GetFileNameWithoutExtension(item.Name));
            try
            {
                var artwork = JsonConvert.DeserializeObject(File.ReadAllText(item.FullName), JsonInstances.Settings);
                if (artwork != null)
                {
                    switch (artwork)
                    {
                        case ImageSubmission img:
                            found.Add(img);
                            break;
                        case CompositionSubmission comp:
                            found.Add(comp);
                            break;
                        default:
                            Console.Error.WriteLine("Failed to read art because it is of an unknown type");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to read artwork {0}: {1}", item.Name, e);
            }
        }

        foreach (var item in found.OrderBy(static a => a.Score))
            artworks.TryAdd(shortid.ShortId.Generate(), item);
    }

    public void StartServer()
    {
        server?.Dispose();

        if (Configuration.Current.SslCert == null)
            server = new Webserver(Configuration.Current.Ip, Configuration.Current.Port, Index);
        else
        {
            server = new Webserver(Configuration.Current.Ip, Configuration.Current.Port, Index,
                new System.Security.Cryptography.X509Certificates.X509Certificate2(Configuration.Current.SslCert, Configuration.Current.SslCertPassword));
            //server = new Webserver(Configuration.Current.Ip, Configuration.Current.Port, true, 
            //                       Configuration.Current.SslCert, Configuration.Current.SslCertPassword, Index);
        }

        server.Settings.Debug.Responses = true;
        server.Settings.Debug.Routing = true;
        server.Settings.Headers.Host = "https://" + server.Settings.Hostname + ":" + server.Settings.Port;
        server.Settings.Ssl.AcceptInvalidAcertificates = true;

        server.Events.Logger = Console.WriteLine;
        server.Events.Exception += (o, e) =>
        {
            Console.Error.WriteLine("{0}: {1}", e.Url, e.Exception.Message);
        };
        server.Routes.Content.Add(Configuration.Current.FrontPath, true);
        server.Start();

        Console.WriteLine("Listening on {0}:{1} ", server.Settings.Hostname, server.Settings.Port);
    }

    public void StopServer()
    {
        server?.Stop();
    }

    [StaticRoute(HttpMethod.GET, "/art")]
    public static async Task GetAllArt(HttpContext ctx)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        int c = 0;
        foreach (var item in artworks.OrderBy(static d => d.Value.Score))
        {
            sb.AppendFormat("\"{0}\"", item.Key);
            if (++c != artworks.Count)
                sb.Append(", ");
        }
        sb.Append(']');
        await ctx.Response.SendAsync(sb.ToString());
    }

    [StaticRoute(HttpMethod.GET, "/art/images")]
    public static async Task GetAllArtImages(HttpContext ctx)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        int c = 0;
        var dd = artworks.Where(d => d.Value is ImageSubmission).OrderBy(static d => d.Value.Score);

        foreach (var item in dd)
        {
            if (item.Value is ImageSubmission)
            {
                sb.AppendFormat("\"{0}\"", item.Key);
                if (++c != dd.Count())
                    sb.Append(", ");
            }
        }
        sb.Append(']');
        await ctx.Response.SendAsync(sb.ToString());
    }
     
    [StaticRoute(HttpMethod.GET, "/art/musicdisc")]
    public static async Task GetMusicDisc(HttpContext ctx)
    {
        var artwork = artworks.Where(d => d.Value is CompositionSubmission).FirstOrDefault();
        ctx.Response.ContentType = "application/json";
        if (artwork.Value == null)
        {
            await ctx.Response.SendAsync("{}");
            return;
        }

        var obj = new
        {
            AudioData = $"/art/{artwork.Key}/audio",
            ImageData= $"/art/{artwork.Key}/image/128/128"
        };
        
        await ctx.Response.SendAsync(JsonConvert.SerializeObject(obj));
    }

    [ParameterRoute(HttpMethod.GET, "/art/{id}/image/{w}/{h}")]
    public static async Task GetArtImage(HttpContext ctx)
    {
        var id = ctx.Request.Url.Parameters["id"] ?? throw new Exception("id parameter missing");
        int w = int.Parse(ctx.Request.Url.Parameters["w"] ?? "128");
        int h = int.Parse(ctx.Request.Url.Parameters["h"] ?? "128");

        w = Math.Min(1024, Math.Max(128, w));
        h = Math.Min(1024, Math.Max(128, h));

        byte[]? b;
        if (artworks.TryGetValue(id, out var artwork))
        {
            var cache = artwork is CompositionSubmission ? discArtCache : imageArtCache;

            if (!responseCache.TryGetValue(ctx.Request.Url.Full, out b))
            {
                var img = await cache.Load(w, h, artwork);
                using var m = new MemoryStream();
                img.Save(m, new WebpEncoder() { Quality = 95 });
                b = m.ToArray();
                if (!responseCache.TryAdd(ctx.Request.Url.Full, b))
                    Console.Error.WriteLine("Failed to cache artwork {0}", id);
            }
        }
        else if (!responseCache.TryGetValue(ctx.Request.Url.Full, out b)) //not found, send wtf img
        {
            using var m = new MemoryStream();
            var img = SixLabors.ImageSharp.Image.Load<Rgba32>("error.png");
            img.Save(m, new WebpEncoder() { Quality = 100 });
            b = m.ToArray();
            if (!responseCache.TryAdd(ctx.Request.Url.Full, b))
                Console.Error.WriteLine("Failed to cache artwork {0}", id);
        }

        ctx.Response.ContentType = "image/webp";
        await ctx.Response.SendAsync(b);
    }

    [ParameterRoute(HttpMethod.GET, "/art/{id}/audio")]
    public static async Task GetArtAudio(HttpContext ctx)
    {
        var id = ctx.Request.Url.Parameters["id"];

        byte[]? b;
        if (artworks.TryGetValue(id, out var artwork) && artwork is CompositionSubmission comp)
        {
            if (!responseCache.TryGetValue(ctx.Request.Url.Full, out b))
            {
                using var r = new HttpClient();
                var response = await r.GetAsync(comp.AudioData);
                var mime = response.Content.Headers.ContentType ?? MediaTypeHeaderValue.Parse("audio/mp3");
                b = await response.Content.ReadAsByteArrayAsync();
                ctx.Response.ContentType = mime.ToString();
            }
        }
        else if (!responseCache.TryGetValue(ctx.Request.Url.Full, out b)) //not found, send wtf sound
        {
            ctx.Response.ContentType = "audio/ogg";
        }

        await ctx.Response.SendAsync(b);
    }

    [ParameterRoute(HttpMethod.GET, "/art/{id}")]
    public static async Task GetArt(HttpContext ctx)
    {
        var id = ctx.Request.Url.Parameters["id"];
        if (artworks.TryGetValue(id, out var artwork))
        {
            object an;

            switch (artwork)
            {
                case CompositionSubmission comp:
                    an = new
                    {
                        artwork.Name,
                        artwork.Author,
                        artwork.Description,
                        artwork.Score,
                        Audio = "art/{id}/audio",
                        Src = $"art/{id}/image/128/128"
                    };
                    break;
                default:
                    an = new
                    {
                        artwork.Name,
                        artwork.Author,
                        artwork.Description,
                        artwork.Score,
                        Src = $"art/{id}/image/512/512"
                    };
                    break;
            }

            await ctx.Response.SendAsync(JsonConvert.SerializeObject(an));
            return;
        }

        ctx.Response.StatusCode = 404;
        await ctx.Response.SendAsync("Artwork not found");
    }

    [StaticRoute(HttpMethod.GET, "/")]
    public static async Task Index(HttpContext ctx)
    {
        ctx.Response.ContentType = "text/html";
        var str = await File.ReadAllBytesAsync("www/index.html");
        await ctx.Response.SendAsync(str);
    }

    public void Dispose()
    {
        server?.Dispose();
    }

    public class UploadArtwork
    {
        public string Secret { get; set; } = "invalid";
        public string Name { get; set; } = "Untitled";
        public string Author { get; set; } = "Unknown";
        public string ImageData { get; set; } = "https://i.imgur.com/ivtJ4zT_d.webp";
        public int Score { get; set; } = 0;
    }
}