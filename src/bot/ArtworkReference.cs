namespace gallery.bot;

public class ArtworkReference
{
    [Ceras.Include]
    public ulong MessageId;

    [Ceras.Include]
    public string Author;
    [Ceras.Include]
    public string ImageUrl;
    [Ceras.Include]
    public string? Name;
    [Ceras.Include]
    public int Score;

    public ArtworkReference()
    {
        Author = string.Empty;
        ImageUrl = string.Empty;
    }

    public ArtworkReference(ulong messageId, string author, string url)
    {
        MessageId = messageId;
        Author = author;
        ImageUrl = url;
    }
}

public class CompositionReference
{
    [Ceras.Include]
    public ulong MessageId;

    [Ceras.Include]
    public string Author;
    [Ceras.Include]
    public string ImageUrl;
    [Ceras.Include]
    public string AudioUrl;
    [Ceras.Include]
    public string? Name;
    [Ceras.Include]
    public int Score;

    public CompositionReference(ulong messageId, string author, string imageUrl, string audioUrl, string name)
    {
        MessageId = messageId;
        Author = author;
        ImageUrl = imageUrl;
        AudioUrl = audioUrl;
        Name = name;
    }
}