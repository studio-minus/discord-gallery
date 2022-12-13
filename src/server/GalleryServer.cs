using gallery.bot;
using gallery.front;
using gallery.shared;
using SixLabors.Fonts.Unicode;
using System.Text.Json;

namespace gallery.server;

public class GalleryServer : IDisposable
{
    public bool IsRunning { get; private set; }

    private readonly ArtGalleryFront gallery;
    public readonly Bot Bot;

    public GalleryServer()
    {
        gallery = new ArtGalleryFront(new DirectoryInfo(Path.GetFullPath(Configuration.Current.ArtPath))); //TODO is dit nodig?
        Bot = new Bot(Configuration.Current.DiscordBotToken, Configuration.Current.ChannelId, Configuration.Current.GuildId);
    }

    public async Task Start()
    {
        gallery.StartServer();
        await Bot.Start();
        IsRunning = true;
    }

    public async Task Stop()
    {
        gallery.StopServer();
        await Bot.Stop();
        IsRunning = false;
    }

    public void Publish(bool clear = true)
    {
        // get new curated exhibition
        var best = Bot.Curator.GetBestArtwork(5).ToArray();

        if (best.Length == 0)
        {
            Console.WriteLine("No new art was found... where is everyone? :(");
            return;
        }

        // clear old exhibition and old artwork
        if (clear)
        {
            Bot.Curator.Clear();
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

        Task.Run(async () => await Bot.SendPublishMessage(best));

        // read new exhibition
        gallery.RefreshArtDirectory();
    }

    public void Dispose()
    {
        gallery.Dispose();
        Bot.Dispose();
    }
}