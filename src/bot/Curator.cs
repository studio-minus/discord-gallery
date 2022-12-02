using Discord.WebSocket;
using Discord;
using gallery.shared;

namespace gallery.bot;

public class Curator : IDisposable
{
    public bool Enabled;
    public PersistentCollection<ArtworkReference> art;
    private readonly IDiscordClient client;

    public Curator(string path, IDiscordClient client)
    {
        this.client = client;
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

    public void ProcessMessage(SocketMessage message)
    {
        foreach (var attachment in message.Attachments)
        {
            if (attachment.ContentType.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase) 
                && !attachment.ContentType.Contains("gif", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Artwork: {0} by {1}: {2}", message.Content, message.Author.Username, attachment.Url);
                art.Add(new ArtworkReference(message.Id, message.Author.Username, attachment.Url)
                {
                    Name = string.IsNullOrWhiteSpace(message.Content) ? null : message.Content
                });
            }
        }
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
        foreach (var item in copy)
        {
            var artwork = new Artwork
            {
                Name = item.Name,
                Author = item.Author,
                Score = item.Score,
                ImageData = item.ImageUrl,
            };

            yield return artwork;
        }
    }

    public void Clear()
    {
        art.Clear();
    }

    public void DeleteMessage(Cacheable<IMessage, ulong> arg)
    {
        art.Remove(m => m.MessageId == arg.Id);
    }

    public void Dispose()
    {
        art.Dispose();
    }
}
