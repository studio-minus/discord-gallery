using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace gallery.front;

public interface IArtRenderer
{
    Image<Rgba32> RenderArtwork(int width, int height, string title, string author, int score, Image<Rgba32> art);
}