namespace gallery.bot;

public abstract class SubmissionReference
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

    public SubmissionReference()
    {
        Author = string.Empty;
        ImageUrl = string.Empty;
    }

    public SubmissionReference(ulong messageId, string author, string url)
    {
        MessageId = messageId;
        Author = author;
        ImageUrl = url;
    }
}
