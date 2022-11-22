using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace gallery;

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
        const int finalSize = 1024;
        const int maxSize = 1000;

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

        var piece = Image.Load<Rgb24>(bytes);
        piece.Mutate((i) =>
        {
            if (piece.Width > maxSize || piece.Height > maxSize)
            {
                float aspectRatio = (piece.Height / (float)piece.Width);
                if (piece.Width > piece.Height)
                    i.Resize(maxSize, (int)(maxSize * aspectRatio));
                else
                    i.Resize((int)(maxSize / aspectRatio), maxSize);
            }
            i.Flip(FlipMode.Vertical);
        });

        Width = piece.Width;
        Height = piece.Height;

        Img = new Image<Rgb24>(finalSize, finalSize, Color.Wheat);
        Img.Mutate(i =>
        {
            var center = new Point(finalSize / 2);
            var offset = new Point(Width / -2, Height / 2);
            center.Offset(offset);
            i.DrawImage(piece, center, 1);
        });

        piece.Dispose();
    }
}
