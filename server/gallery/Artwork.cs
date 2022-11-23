using Microsoft.VisualBasic;
using System.Xml.Linq;

namespace gallery;

public class Artwork
{
    public string? Name { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public int Interactions { get; set; }
    public string? ImageData { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
