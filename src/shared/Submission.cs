namespace gallery.shared;

public abstract class Submission
{
    public string? Name { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public int Score { get; set; }
    public string? ImageData { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
