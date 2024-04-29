using gallery.shared;
using Newtonsoft.Json;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Concurrent;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using WatsonWebserver;
using WatsonWebserver.Core;
using Configuration = gallery.shared.Configuration;
using HttpMethod = WatsonWebserver.Core.HttpMethod;

namespace gallery.front;

public class ArtGalleryFront : IDisposable
{
    public readonly DirectoryInfo ArtDirectory;
    public readonly DirectoryInfo CacheDirectory;

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

    public ArtGalleryFront(DirectoryInfo artDirectory, DirectoryInfo cacheDirectory)
    {
        ArtDirectory = artDirectory;
        CacheDirectory = cacheDirectory;

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

        server = new Webserver(new WatsonWebserver.Core.WebserverSettings(Configuration.Current.Ip, Configuration.Current.Port, false), Index);

        server.Settings.Debug.Responses = true;
        server.Settings.Debug.Routing = true;
        //server.Settings.Headers.Add("Host", "https://" + Configuration.Current.Ip + ":" + Configuration.Current.Port);

        server.Events.Logger = Console.WriteLine;
        server.Events.ExceptionEncountered += (o, e) =>
        {
            Console.Error.WriteLine("{0}: {1}", e.Url, e.Exception.Message);
        };

        server.Routes.PreAuthentication.Content.Add(Configuration.Current.FrontPath, true);
        server.Routes.PreAuthentication.Content.Add(Configuration.Current.CachePath, true);

        var st = server.Routes.PreAuthentication.Static;
        st.Add(HttpMethod.GET, "/", Index);
        st.Add(HttpMethod.GET, "/art", GetAllArt);
        st.Add(HttpMethod.GET, "/art/images", GetAllArtImages);
        st.Add(HttpMethod.GET, "/art/musicdisc", GetMusicDisc);

        var par = server.Routes.PreAuthentication.Parameter;
        par.Add(HttpMethod.GET, "/art/{id}/image/{w}/{h}", GetArtImage);
        par.Add(HttpMethod.GET, "/art/{id}/audio", GetArtAudio);
        par.Add(HttpMethod.GET, "/art/{id}", GetArt);

        server.Start();

        Console.WriteLine("Listening on {0}:{1} ", Configuration.Current.Ip, Configuration.Current.Port);
    }

    public void StopServer()
    {
        server?.Stop();
    }

    public async Task GetAllArt(HttpContextBase ctx)
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
        await ctx.Response.Send(sb.ToString());
    }

    public async Task GetAllArtImages(HttpContextBase ctx)
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
        await ctx.Response.Send(sb.ToString());
    }

    public async Task GetMusicDisc(HttpContextBase ctx)
    {
        var artwork = artworks.Where(d => d.Value is CompositionSubmission).FirstOrDefault();
        ctx.Response.ContentType = "application/json";
        if (artwork.Value == null)
        {
            await ctx.Response.Send("{}");
            return;
        }

        var obj = new
        {
            AudioData = $"/art/{artwork.Key}/audio",
            ImageData = $"/art/{artwork.Key}/image/128/128"
        };

        await ctx.Response.Send(JsonConvert.SerializeObject(obj));
    }

    public async Task GetArtImage(HttpContextBase ctx)
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
            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>("error.png");
            img.Save(m, new WebpEncoder() { Quality = 100 });
            b = m.ToArray();
            if (!responseCache.TryAdd(ctx.Request.Url.Full, b))
                Console.Error.WriteLine("Failed to cache artwork {0}", id);
        }

        ctx.Response.ContentType = "image/webp";
        await ctx.Response.Send(b);
    }

    public async Task GetArtAudio(HttpContextBase ctx)
    {
        var id = ctx.Request.Url.Parameters["id"] ?? throw new Exception("id parameter missing");

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

        await ctx.Response.Send(b);
    }

    public async Task GetArt(HttpContextBase ctx)
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

            await ctx.Response.Send(JsonConvert.SerializeObject(an));
            return;
        }

        ctx.Response.StatusCode = 404;
        await ctx.Response.Send("Artwork not found");
    }


    public async Task Index(HttpContextBase ctx)
    {
        ctx.Response.ContentType = "text/html";
        var str = await File.ReadAllBytesAsync("www/index.html");
        await ctx.Response.Send(str);
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