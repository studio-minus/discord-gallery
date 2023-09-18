namespace gallery.bot;

public class CompositionReference : SubmissionReference
{
    [Ceras.Include]
    public string AudioUrl;

    public CompositionReference() :base()
    {
        AudioUrl = string.Empty;
    }

    public CompositionReference(ulong messageId, string author, string url, string audioUrl) : base(messageId, author, url)
    {
        AudioUrl = audioUrl;
    }
}