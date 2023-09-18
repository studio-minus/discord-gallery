using gallery.shared;
using System.Linq;

namespace gallery.bot;

public class Curator : IDisposable
{
    public bool Enabled { get; private set; }

    private class SubmissionEntry
    {
        [Ceras.Include]
        public CompositionReference? Composition;
        [Ceras.Include]
        public ImageSubmissionReference? Image;
        [Ceras.Exclude]
        public bool IsImage => Image != null;
        [Ceras.Exclude]
        public bool IsComposition => Composition != null;

        public SubmissionEntry(CompositionReference? composition)
        {
            Composition = composition;
        }

        public SubmissionEntry(ImageSubmissionReference? image)
        {
            Image = image;
        }

        public SubmissionEntry()
        {
            
        }

        public SubmissionReference Submission
        {
            get
            {
                if (Composition != null)
                    return Composition;
                if (Image != null)
                    return Image;
                throw new NullReferenceException();
            }
        }
    }

    private readonly PersistentCollection<SubmissionEntry> art;

    public Curator(string path)
    {
        art = new PersistentCollection<SubmissionEntry>(path);
        foreach (var item in art)
            Console.WriteLine("Stored art found: {0}", item.Submission.MessageId);

    }

    public void Enable()
    {
        if (Enabled)
            return;

        Enabled = true;
        Console.WriteLine("Curator enabled! :)");
    }

    public void Disable()
    {
        if (!Enabled)
            return;

        Enabled = false;
        Console.WriteLine("Curator disabled. :(");
    }

    public void Add(SubmissionReference art)
    {
        Console.WriteLine("Artwork added: {0} by {1}: {2}", art.Name, art.Author, art.ImageUrl);
        switch (art)
        {
            case CompositionReference b:
                this.art.Add(new SubmissionEntry(b));
                break;
            case ImageSubmissionReference b:
                this.art.Add(new SubmissionEntry(b));
                break;
        }
    }

    public bool Remove(ulong messageid)
    {
        var found = art.FirstOrDefault(a => a.Submission.MessageId == messageid);
        if (found != null)
        {
            Console.WriteLine("Artwork removed: {0}", found.Submission.Name);
            art.Remove(found);
            return true;
        }

        return false;
    }

    public void IncrementScore(ulong messageid)
    {
        var found = art.FirstOrDefault(a => a.Submission.MessageId == messageid);
        if (found != null)
        {
            found.Submission.Score++;
            art.SaveToSource();
        }
    }

    public void DecrementScore(ulong messageid)
    {
        var found = art.FirstOrDefault(a => a.Submission.MessageId == messageid);
        if (found != null)
        {
            found.Submission.Score--;
            art.SaveToSource();
        }
    }

    public Exhibition GetExhibition(int maxImageCount)
    {
        var imgs = GetBestImageArt(maxImageCount);
        var comp = GetBestComposition(1).FirstOrDefault();

        return new Exhibition(imgs.ToArray(), comp);
    }

    public IEnumerable<ImageSubmission> GetBestImageArt(int amount)
    {
        var copy = art.Where(a => a.IsImage && a.Image != null)
            .Select(a => a.Image!).OrderByDescending(static artwork => artwork.Score).Take(amount).ToArray();
        foreach (var a in copy)
            yield return ReferenceToSubmission(a);
    }

    public IEnumerable<CompositionSubmission> GetBestComposition(int amount)
    {
        var copy = art.Where(a => a.IsComposition && a.Composition != null)
            .Select(a => a.Composition!).OrderByDescending(static artwork => artwork.Score).Take(amount).ToArray();
        foreach (var a in copy)
            yield return ReferenceToSubmission(a);
    }

    private CompositionSubmission ReferenceToSubmission(CompositionReference a)
    {
        return new CompositionSubmission
        {
            Name = a.Name,
            Author = a.Author,
            Score = a.Score,
            ImageData = a.ImageUrl,
            AudioData = a.AudioUrl
        };
    }

    private ImageSubmission ReferenceToSubmission(ImageSubmissionReference a)
    {
        return new ImageSubmission
        {
            Name = a.Name,
            Author = a.Author,
            Score = a.Score,
            ImageData = a.ImageUrl,
        };
    }

    public IEnumerable<Submission> GetAll()
    {
        var imgs = art.Where(a => a.IsImage && a.Image != null).Select(a => a.Image!).Select(ReferenceToSubmission).Cast<Submission>();
        var comps = art.Where(a => a.IsComposition && a.Composition != null).Select(a => a.Composition!).Select(ReferenceToSubmission).Cast<Submission>();

        return imgs.Concat(comps);
    }

    public int Count => art.Count;

    public void Clear()
    {
        art.Clear();
    }

    public void Dispose()
    {
        art.Dispose();
    }
}
