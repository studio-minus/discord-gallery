using gallery.shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Concurrent;

namespace gallery.front;

public class ImageArtCache
{
    public IArtRenderer? Renderer;

    private readonly ConcurrentDictionary<ArtRequest, Image<Rgba32>> cache = new();

    public ImageArtCache(IArtRenderer renderer)
    {
        Renderer = renderer;
    }

    public struct ArtRequest : IEquatable<ArtRequest>
    {
        public int Width, Height;
        public string DataString;

        public override bool Equals(object? obj) => obj is ArtRequest request && Equals(request);

        public bool Equals(ArtRequest other)
                => Width == other.Width &&
                   Height == other.Height &&
                   DataString == other.DataString;

        public override int GetHashCode() => HashCode.Combine(Width, Height, DataString);

        public static bool operator ==(ArtRequest left, ArtRequest right) => left.Equals(right);

        public static bool operator !=(ArtRequest left, ArtRequest right) => !(left == right);
    }

    public async Task<Image<Rgba32>> Load(int w, int h, Submission work)
    {
        if (Renderer == null)
            throw new Exception($"{nameof(Renderer)} is unassigned");

        var dataString = work.ImageData ?? "error.png";

        var req = new ArtRequest { Width = w, Height = h, DataString = dataString };
        if (cache.TryGetValue(req, out var img))
            return img;
        var bytes = Array.Empty<byte>();

        if (dataString.StartsWith("data:"))
            bytes = Convert.FromBase64String(dataString[(dataString.LastIndexOf(',') + 1)..]);
        else if (dataString.StartsWith("http"))
        {
            using var r = new HttpClient();
            var response = await r.GetAsync(dataString);
            bytes = await response.Content.ReadAsByteArrayAsync();
        }
        else
            bytes = File.ReadAllBytes(dataString);

        using var piece = Image.Load<Rgba32>(bytes);
        img = Renderer.RenderArtwork(w, h, work.Name ?? "Untitled", work.Author ?? "Unknown", work.Score, piece);
        piece.Dispose();

        if (!cache.TryAdd(req, img))
        {
            img.Dispose();
            throw new Exception("Failed to add rendered artwork to the cache probably because of a duplicate key, somehow");
        }

        return img;
    }

    public void Clear()
    {
        foreach (var item in cache.Values)
            item.Dispose();
        cache.Clear();
    }

    public int Count => cache.Count;
}
