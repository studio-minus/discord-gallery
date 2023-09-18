using SixLabors.ImageSharp;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;

namespace gallery.front;

public class AuctionArtRenderer : IArtRenderer
{
    public readonly static (int x, int y) NameplateSize = (200, 100);

    private readonly FontCollection fonts = new();
    private readonly FontFamily vollkorn;
    private readonly Font titleFont;
    private readonly Font descriptionFont;

    public AuctionArtRenderer(string fontPath)
    {
        vollkorn = fonts.Add(fontPath);
        titleFont = vollkorn.CreateFont(38, FontStyle.Bold);
        descriptionFont = vollkorn.CreateFont(32, FontStyle.Italic);
    }

    public Image<Rgba32> RenderArtwork(int width, int height, string title, string author, int score, Image<Rgba32> piece)
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

            var rectSize = (NameplateSize.x, NameplateSize.y);
            rectSize.x = Math.Max(rectSize.x, title.Length * (int)(titleFont.Size / 1.5f));
            rectSize.x = Math.Max(rectSize.x, author.Length * (int)(descriptionFont.Size / 1.5f));

            var rect = new Rectangle(width / 2 - rectSize.x / 2, height - 10 - rectSize.y, rectSize.x, rectSize.y);
            i.Draw(Color.Black, 2, rect);
            i.Fill(Color.White, rect);

            // draw title
            i.DrawText(new RichTextOptions(titleFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                WrappingLength = -1,
                Origin = new PointF((rect.Left + rect.Right) / 2, rect.Top + 2)
            }, title, Color.Black);

            // draw author
            i.DrawText(new RichTextOptions(descriptionFont)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                WrappingLength = -1,
                Origin = new PointF((rect.Left + rect.Right) / 2, rect.Bottom + 5)
            }, author, Color.Black.WithAlpha(0.8f));

            // draw worth
            i.DrawText(new RichTextOptions(descriptionFont)
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Origin = new PointF(5, height - 5),
            }, $"€{score}", Color.DarkGreen);
        });

        return img;
    }
}
