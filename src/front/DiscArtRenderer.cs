namespace gallery.front;

public class DiscArtRenderer : IArtRenderer
{
    public readonly Image Overlay;

    public DiscArtRenderer(string overlayPath)
    {
        Overlay = Image.Load(overlayPath);
    }

    public Image<Rgba32> RenderArtwork(int width, int height, string title, string author, int score, Image<Rgba32> original)
    {
        const float ratio = 0.42f;

        var img = new Image<Rgba32>(width, height, Color.Black);
        using var art = original.Clone(i => i.Resize((int)(width * ratio), (int)(height * ratio)));
        using var overlay = Overlay.Clone(i => i.Resize(width, height));

        img.Mutate(i =>
        {
            i.DrawImage(art, new Point(
                width / 2 - art.Width / 2,
                height / 2 - art.Height / 2), 1);
            i.DrawImage(overlay, 1);
        });

        return img;
    }
}