using Discord.WebSocket;
using Discord;
using Newtonsoft.Json.Linq;
using System.Net.Mime;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace bot;

public class Program
{
    private static async Task Main(string[] args)
    {
        var bot = new Bot();
        await bot.Start();
    }
}

public readonly struct Paths
{
    public const string ApiSecret = "invalid";
    public const string Endpoint = "http://127.0.0.1/art";
    public const string TokenFilePath = "token.txt";
    public const string ChannelFilePath = "channel.txt";
    public const string GuildFilePath = "guild.txt";
}

public class Bot
{
    private readonly DiscordSocketClient client;
    private readonly string token;
    private readonly ulong channelId;
    private readonly ulong guildId;

    private readonly Curator curator = new("curator_data");

    public Bot()
    {
        if (!File.Exists(Paths.TokenFilePath)) throw new Exception($"{Paths.TokenFilePath} could not be found");
        if (!File.Exists(Paths.ChannelFilePath)) throw new Exception($"{Paths.ChannelFilePath} could not be found");
        if (!File.Exists(Paths.GuildFilePath)) throw new Exception($"{Paths.GuildFilePath} could not be found");

        token = File.ReadAllText(Paths.TokenFilePath);
        var sChannelId = File.ReadAllText(Paths.ChannelFilePath);
        var sGuildId = File.ReadAllText(Paths.GuildFilePath);

        if (!ulong.TryParse(sChannelId, out channelId)) throw new Exception($"Invalid format for channel {sChannelId}");
        if (!ulong.TryParse(sGuildId, out guildId)) throw new Exception($"Invalid format for channel {sGuildId}");

        var cfg = new DiscordSocketConfig();
        cfg.GatewayIntents |= GatewayIntents.GuildMembers;
        cfg.GatewayIntents |= GatewayIntents.GuildBans;
        cfg.GatewayIntents |= GatewayIntents.MessageContent;

        client = new DiscordSocketClient(cfg);
        client.Log += async t => { Console.WriteLine(t.ToString()); };
        client.Ready += OnReady;
        client.GuildAvailable += OnGuildAvailable;
        client.GuildUnavailable += OnGuildUnavailable;
        client.MessageReceived += OnMessageReceived;
        client.MessageDeleted += OnMessageDeleted;
    }

    public async Task Start()
    {
        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();
        await Task.Delay(Timeout.Infinite);
    }

    private async Task OnGuildAvailable(SocketGuild arg)
    {
        if (arg.Id != guildId)
            return;

        curator.Enable();
    }

    private async Task OnMessageReceived(SocketMessage arg)
    {
        if (!curator.Enabled || arg.Channel.Id != channelId)
            return;

        await curator.ProcessMessage(arg);
    }

    private async Task OnMessageDeleted(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        if (!curator.Enabled || arg2.Id != channelId)
            return;

        await curator.DeleteMessage(arg1);
    }

    private async Task OnGuildUnavailable(SocketGuild arg)
    {
        if (arg.Id != guildId)
            return;

        curator.Disable();
    }

    private Task OnReady()
    {
        Console.WriteLine($"{client.CurrentUser} is connected!");
        return Task.CompletedTask;
    }
}

public class Curator
{
    public bool Enabled;
    public PersistentCollection<ArtworkReference> art;

    public Curator(string path)
    {
        this.art = new PersistentCollection<ArtworkReference>(path);
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

    public async Task ProcessMessage(SocketMessage arg)
    {
        foreach (var item in arg.Attachments)
        {
            if (item.ContentType.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase) && !item.ContentType.Contains("gif", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Artwork: {0} by {1}: {2}", arg.Content, arg.Author.Username, item.Url);
                art.Add(new ArtworkReference(arg.Id, arg.Author.Username, item.Url)
                {
                    Title = string.IsNullOrWhiteSpace(arg.Content) ? null : arg.Content
                });
            }
        }
    }

    public void UploadToGallery()
    {
        //TODO remove existing artwork, upload new artwork (moet dit met API? kun je de applicaties niet samenvoegen zodat je gewoon van een directory kunt lezen? is echt veel makkelijker en veiliger :)"
    }

    public async Task DeleteMessage(Cacheable<IMessage, ulong> arg)
    {
        art.Remove(m => m.MessageId == arg.Id);
    }
}
