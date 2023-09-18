namespace gallery.bot;

public class ImageSubmissionReference : SubmissionReference
{
    public ImageSubmissionReference() : base()
    {

    }

    public ImageSubmissionReference(ulong messageId, string author, string url) : base(messageId, author, url)
    {
    }
}
