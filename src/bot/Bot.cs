using Discord.WebSocket;
using Discord;
using gallery.shared;
using System.Text;

namespace gallery.bot;

public class Bot : IDisposable
{
    public readonly Curator Curator;

    private readonly DiscordSocketClient client;
    private readonly string token;
    private readonly ulong channelId;
    private readonly ulong guildId;

    private static readonly string[] PublishMessages =
    {
        "🎪 Step right up, ladies and gents, and behold the finest art this community has to offer! " +
        "I have put together a spectacular exhibition at the Community Art Gallery, featuring {0} masterpieces of unmatched beauty and splendor. " +
        "Don't miss this opportunity to feast your eyes on these wondrous works of art, on display for one week only! 🎪",

        "🎪 Gather around, everyone, and come see the awe-inspiring artwork on show at the Community Art Gallery! " +
        "I have curated a stunning exhibition of {0} breathtaking masterpieces that you won't want to miss. " +
        "This is your chance to witness art of unparalleled beauty and magnificence - it's here for only one week! 🎪",

        "🎪 Step up and witness the most extraordinary art this town has to offer! " +
        "I have organized a phenomenal exhibition at the Community Art Gallery, presenting {0} remarkable pieces of art. " +
        "Don't miss the opportunity to feast your eyes on these exquisite works of art - it's only here for one week! 🎪",

        "🎪 Everyone, come take a look at the remarkable art that I have on display at the Community Art Gallery! " +
        "I have compiled an amazing exhibition of {0} remarkable works of art that you simply have to see. " +
        "This is your chance to experience art of remarkable beauty - it's here for one week only! 🎪",

        "🎪Come one, come all, and see the incredible art I have in store at the Community Art Gallery! " +
        "I have put together a magnificent exhibition of {0} magnificent masterpieces that you won't want to miss. " +
        "Don't miss the chance to witness art of unparalleled beauty and grandeur - it's here for one week! 🎪",
    };

    private static readonly Func<string, bool>[] contentTypeFilters =
    {
        s => s.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase), //has to be an image
        s => !s.Contains("gif", StringComparison.InvariantCultureIgnoreCase), // disallow gifs
        s => !s.Contains("apng", StringComparison.InvariantCultureIgnoreCase), // disallow animated pngs
    };

    public Bot(string token, ulong channelId, ulong guildId)
    {
        var cfg = new DiscordSocketConfig();
        cfg.GatewayIntents |= GatewayIntents.GuildMembers;
        cfg.GatewayIntents |= GatewayIntents.GuildBans;
        cfg.GatewayIntents |= GatewayIntents.MessageContent;

        client = new DiscordSocketClient(cfg);
        client.Log += t => { Console.WriteLine(t.ToString()); return Task.CompletedTask; };
        client.Ready += OnReady;
        client.GuildAvailable += OnGuildAvailable;
        client.GuildUnavailable += OnGuildUnavailable;
        client.MessageReceived += OnMessageReceived;
        client.MessageDeleted += OnMessageDeleted;
        client.ReactionAdded += ReactionAdded;
        client.ReactionRemoved += ReactionRemoved;

        Curator = new("curator_data");

        this.token = token;
        this.channelId = channelId;
        this.guildId = guildId;
    }

    public async Task Start()
    {
        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();
    }

    private Task OnGuildAvailable(SocketGuild msg)
    {
        if (msg.Id != guildId)
            return Task.CompletedTask;

        Curator.Enable();
        return Task.CompletedTask;
    }

    private Task OnMessageReceived(SocketMessage msg)
    {
        if (!Curator.Enabled || msg.Channel.Id != channelId)
            return Task.CompletedTask;

        // Ik hoef hier toch niet the checken voor guild ID omdat channel IDs universeel zijn

        //if (arg.Channel is not IGuildChannel g || g.GuildId != guildId)
        //    return Task.CompletedTask;

        if (msg.Type is MessageType.Reply && msg.Reference != null && msg.Reference.MessageId.IsSpecified)
        {
            var replyTo = msg.Reference.MessageId;
            Curator.IncrementScore(replyTo.Value);
        }

        foreach (var attachment in msg.Attachments)
            if (contentTypeFilters.All(filter => filter(attachment.ContentType)))
                Curator.Add(new ArtworkReference(msg.Id, msg.Author.Username, attachment.Url)
                {
                    Name = string.IsNullOrWhiteSpace(msg.Content) ? null : msg.Content
                });

        return Task.CompletedTask;
    }

    private Task OnMessageDeleted(Cacheable<IMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel)
    {
        if (!Curator.Enabled || channel.Id != channelId)
            return Task.CompletedTask;

        // Ik hoef hier ook niet the checken voor guild ID omdat channel IDs universeel zijn

        //if (arg.Channel is not IGuildChannel g || g.GuildId != guildId)
        //    return Task.CompletedTask;

        Curator.Remove(msg.Id);
        return Task.CompletedTask;
    }

    private Task ReactionRemoved(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        if (!Curator.Enabled || channel.Id != channelId)
            return Task.CompletedTask;

        Curator.DecrementScore(msg.Id);
        return Task.CompletedTask;
    }

    private Task ReactionAdded(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        if (!Curator.Enabled || channel.Id != channelId)
            return Task.CompletedTask;

        Curator.IncrementScore(msg.Id);
        return Task.CompletedTask;
    }

    private Task OnGuildUnavailable(SocketGuild msg)
    {
        if (msg.Id != guildId)
            return Task.CompletedTask;

        Curator.Disable();
        return Task.CompletedTask;
    }

    private Task OnReady()
    {
        Console.WriteLine($"{client.CurrentUser} is connected!");
        return Task.CompletedTask;
    }

    public async Task Stop()
    {
        await client.LogoutAsync();
        await client.StopAsync();
    }

    public async Task SendPublishMessage(Artwork[] best)
    {
        var message = string.Format(PublishMessages[Random.Shared.Next(0, PublishMessages.Length)], best.Length) + "\n\nhttps://gallery.studiominus.nl/";
        var channel = await client.GetChannelAsync(channelId);
        if (channel is ITextChannel txt)
            await txt.SendMessageAsync(message);
    }

    public void Dispose()
    {
        client?.Dispose();
        Curator.Dispose();
    }
}
