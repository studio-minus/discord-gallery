using gallery.bot;
using gallery.front;
using gallery.shared;
using System.Text.Json;

namespace gallery.server;

public class GalleryServer
{
    public bool IsRunning { get; private set; }

    private readonly ArtGalleryFront gallery;
    private readonly Bot bot;

    public GalleryServer()
    {
        gallery = new ArtGalleryFront(new DirectoryInfo(Path.GetFullPath(Configuration.Current.ArtPath))); //TODO is dit nodig?
        bot = new Bot(Configuration.Current.DiscordBotToken, Configuration.Current.ChannelId, Configuration.Current.GuildId);
    }

    public async Task Start()
    {
        gallery.StartServer();
        await bot.Start();
        IsRunning = true;
    }

    public async Task Stop()
    {
        gallery.StopServer();
        await bot.Stop();
        IsRunning = false;
    }

    public void Publish(bool clear = true)
    {
        // get new curated exhibition
        var best = bot.Curator.GetBestArtwork(5).ToArray();

        // clear old exhibition and old artwork
        if (clear)
        {
            bot.Curator.Clear();
            foreach (var item in Directory.GetFiles(Configuration.Current.ArtPath, "*.json"))
                File.Delete(item);
        }

        // populate art directory for persistence
        foreach (var item in best)
        {
            var json = JsonSerializer.Serialize(item);
            File.WriteAllText($"{Configuration.Current.ArtPath}/{Guid.NewGuid()}.json", json);
            Console.WriteLine("Saved new artwork to exhibition: {0}", item.Name);
        }

        // read new exhibition
        gallery.RefreshArtDirectory();
    }
}