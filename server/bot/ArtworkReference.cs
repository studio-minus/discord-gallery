namespace bot;

public class ArtworkReference
{
    public readonly ulong MessageId;

    public string Author;
    public string Url;
    public string? Title;

    public ArtworkReference()
    {
        MessageId = 0;
        Author = string.Empty;
        Url = string.Empty;
    }

    public ArtworkReference(ulong messageId, string author, string url)
    {
        MessageId = messageId;
        Author = author;
        Url = url;
    }
}
