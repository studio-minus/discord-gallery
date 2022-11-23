using SixLabors.ImageSharp;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;

namespace gallery;

public static class ArtRenderer
{
    public const int FinalSize = 1024;
    public const int MaxInnerArtSize = FinalSize - 15;
    public static (int x, int y) MinRectSize = (200, 100);

    private static readonly FontCollection fonts = new FontCollection();
    private static readonly FontFamily vollkorn;
    private static readonly Font titleFont;
    private static readonly Font descriptionFont;

    static ArtRenderer()
    {
        vollkorn = fonts.Add("Vollkorn.ttf");
        titleFont = vollkorn.CreateFont(38, FontStyle.Bold);
        descriptionFont = vollkorn.CreateFont(32, FontStyle.Italic);
    }

    public static Image<Rgb24> RenderArtwork(string title, string author, int score, Image<Rgb24> piece)
    {
        piece.Mutate((i) =>
        {
            if (piece.Width > MaxInnerArtSize || piece.Height > MaxInnerArtSize)
            {
                float aspectRatio = (piece.Height / (float)piece.Width);
                if (piece.Width > piece.Height)
                    i.Resize(MaxInnerArtSize, (int)(MaxInnerArtSize * aspectRatio));
                else
                    i.Resize((int)(MaxInnerArtSize / aspectRatio), MaxInnerArtSize);
            }
        });

        var img = new Image<Rgb24>(FinalSize, FinalSize, Color.Wheat);
        img.Mutate(i =>
        {
            i.DrawImage(piece, new Point(FinalSize / 2 - piece.Width / 2, FinalSize / 2 - piece.Height / 2), 1);

            var rectSize = (MinRectSize.x, MinRectSize.y);
            rectSize.x = Math.Max(rectSize.x, title.Length * (int)titleFont.Size / 2);
            rectSize.x = Math.Max(rectSize.x, author.Length * (int)descriptionFont.Size / 2);

            var rect = new Rectangle(FinalSize / 2 - rectSize.x / 2, FinalSize - 10 - rectSize.y, rectSize.x, rectSize.y);
            i.Draw(Color.Black, 2, rect);
            i.Fill(Color.White, rect);
            i.DrawText(new TextOptions(titleFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                WrappingLength = rectSize.x,
                Origin = new PointF((rect.Left + rect.Right) / 2, rect.Top + 2)
            }, title, Color.Black);

            i.DrawText(new TextOptions(descriptionFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                WrappingLength = rectSize.x,
                Origin = new PointF((rect.Left + rect.Right) / 2, rect.Bottom + 5)
            }, author, Color.Black.WithAlpha(0.2f));

            i.DrawText(new TextOptions(descriptionFont)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Origin = new PointF(0, FinalSize),
            }, $"€{score}", Color.DarkGreen);
        });

        return img;
    }
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
            bytes = File.ReadAllBytes(ImageData);

        using var piece = Image.Load<Rgb24>(bytes);

        Img = ArtRenderer.RenderArtwork(Name ?? "Untitled", Author ?? "Unknown", Interactions, piece);
        Width = Img.Width;
        Height = Img.Height;

        piece.Dispose();
    }
}
