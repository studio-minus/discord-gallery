using Discord.WebSocket;
using Discord;
using gallery.shared;
using System.Net.Mail;

namespace gallery.bot;

public class Curator : IDisposable
{
    public bool Enabled { get; private set; }

    private readonly PersistentCollection<ArtworkReference> art;

    public Curator(string path)
    {
        art = new PersistentCollection<ArtworkReference>(path);
        foreach (var item in art)
            Console.WriteLine("Stored art found: {0}", item.MessageId);

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

        Console.WriteLine("Curator disabled. :(");
    }

    public void Add(ArtworkReference art)
    {
        Console.WriteLine("Artwork added: {0} by {1}: {2}", art.Name, art.Author, art.ImageUrl);
        this.art.Add(art);
    }

    public bool Remove(ulong messageid)
    {
        var found = art.FirstOrDefault(a => a.MessageId == messageid);
        if (found != null)
        {
            Console.WriteLine("Artwork removed: {0}", found.Name);
            art.Remove(found);
            return true;
        }

        return false;
    }

    public void IncrementScore(ulong messageid)
    {
        var found = art.FirstOrDefault(a => a.MessageId == messageid);
        if (found != null)
        {
            found.Score++;
            art.SaveToSource();
        }
    }

    public void DecrementScore(ulong messageid)
    {
        var found = art.FirstOrDefault(a => a.MessageId == messageid);
        if (found != null)
        {
            found.Score--;
            art.SaveToSource();
        }
    }

    public IEnumerable<Artwork> GetBestArtwork(int amount)
    {
        var copy = art.OrderByDescending(static artwork => artwork.Score).Take(amount).ToArray();
        foreach (var a in copy)
        {
            var artwork = new Artwork
            {
                Name = a.Name,
                Author = a.Author,
                Score = a.Score,
                ImageData = a.ImageUrl,
            };

            yield return artwork;
        }
    }

    public IReadOnlyCollection<Artwork> GetAll()
    {
        return art.Select(a => new Artwork
        {
            Name = a.Name,
            Author = a.Author,
            Score = a.Score,
            ImageData = a.ImageUrl,
        }).ToList().AsReadOnly();
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
