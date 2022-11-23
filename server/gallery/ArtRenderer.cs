using SixLabors.ImageSharp;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;

namespace gallery;

public static class ArtRenderer
{
    //public const int FinalSize = 1024;
    //public const int MaxInnerArtSize = FinalSize - 15;
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

    public static Image<Rgba32> RenderArtwork(int width, int height, string title, string author, int score, Image<Rgba32> piece)
    {
        int maxInnerWidth = width - 20;
        int maxInnerHeight = height - 20;

        piece.Mutate((i) =>
        {
            if (piece.Width > maxInnerWidth || piece.Height > maxInnerHeight)
            {
                float aspectRatio = (piece.Height / (float)piece.Width);
                if (piece.Width / (float)maxInnerWidth > piece.Height / (float)maxInnerHeight)
                    i.Resize(maxInnerWidth, (int)(maxInnerWidth * aspectRatio));
                else
                    i.Resize((int)(maxInnerHeight / aspectRatio), maxInnerHeight);
            }
        });

        var img = new Image<Rgba32>(width, height, Color.Transparent);
        img.Mutate(i =>
        {
            i.DrawImage(piece, new Point(width / 2 - piece.Width / 2, height / 2 - piece.Height / 2), 1);

            var rectSize = (MinRectSize.x, MinRectSize.y);
            rectSize.x = Math.Max(rectSize.x, title.Length * (int)titleFont.Size / 2);
            rectSize.x = Math.Max(rectSize.x, author.Length * (int)descriptionFont.Size / 2);

            var rect = new Rectangle(width / 2 - rectSize.x / 2, height - 10 - rectSize.y, rectSize.x, rectSize.y);
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
            }, author, Color.Black.WithAlpha(0.8f));

            i.DrawText(new TextOptions(descriptionFont)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Origin = new PointF(5, height - 5),
            }, $"€{score}", Color.DarkGreen);
        });

        return img;
    }
}
