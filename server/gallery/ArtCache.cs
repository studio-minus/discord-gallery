using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace gallery;

public static class ArtCache
{
    public static Dictionary<ArtRequest, Image<Rgba32>> Cache = new();

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

    public static async Task<Image<Rgba32>> Load(int w, int h, Artwork work)
    {
        var dataString = work.ImageData ?? "error.png";

        var req = new ArtRequest { Width = w, Height = h, DataString = dataString };
        if (Cache.TryGetValue(req, out var img))
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
        img = ArtRenderer.RenderArtwork(w, h, work.Name ?? "Untitled", work.Author ?? "Unknown", work.Interactions, piece);
        Cache.Add(req, img);
        piece.Dispose();
        return img;
    }
}
