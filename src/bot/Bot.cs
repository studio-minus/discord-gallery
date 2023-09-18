using Discord.WebSocket;
using Discord;
using gallery.shared;
using System.Xml.Linq;

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

        "🎪 Attention, attention! Marvel at {0} breathtaking works of art at the Community Art Gallery! Just one week to witness these visual wonders! 🎪",

        "🎪 Step right up, folks! {0} masterpieces are on display at the Community Art Gallery! Seize this opportunity before the week is out! 🎪",

        "🎪 Gather round, art enthusiasts! {0} extraordinary exhibits are showcased at the Community Art Gallery! Hurry, they're only around for one week! 🎪",

        "🎪 Hear the call, ladies and gents! {0} stunning creations await at the Community Art Gallery! Be quick, they're only in town for a week! 🎪",

        "🎪 Listen up, everyone! {0} fantastic pieces of art are making a splash at the Community Art Gallery! Don't miss out, they're only here for one week! 🎪",

        "🎪 Don't miss this, folks! {0} captivating masterpieces are taking the spotlight at the Community Art Gallery! Time's ticking, they're only here for a week! 🎪",

        "🎪 All eyes here, art lovers! {0} awe-inspiring exhibits are making their debut at the Community Art Gallery! Catch them before they're gone in a week! 🎪",
    };

    private static readonly Func<string, bool>[] contentTypeFilters =
    {
        s => s.StartsWith("image/", StringComparison.InvariantCultureIgnoreCase) || s.StartsWith("audio/", StringComparison.InvariantCultureIgnoreCase), //has to be an image or audio
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
            {
                if (attachment.Size > 8_000_000) // skip files greater than 8 MB
                    continue;

                SubmissionReference artwork;
                
                // TODO this is not very expandable and this and .contentTypeFilters should rely on the same values
                if (attachment.ContentType.StartsWith("audio", StringComparison.InvariantCultureIgnoreCase)) // WebP required: the server serves the artwork images as webp and doesnt expect anything else
                    artwork = new CompositionReference(msg.Id, msg.Author.Username, msg.Author.GetAvatarUrl(ImageFormat.WebP), attachment.Url);
                else
                    artwork = new ImageSubmissionReference(msg.Id, msg.Author.Username, attachment.Url);

                artwork.Name = string.IsNullOrWhiteSpace(msg.Content) ? null : msg.Content;
                Curator.Add(artwork);
            }

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

    public async Task SendPublishMessage(Exhibition exhibition)
    {
        var message = string.Format(PublishMessages[Random.Shared.Next(0, PublishMessages.Length)], exhibition.Images.Length) + "\n\nhttps://gallery.studiominus.nl/";
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
