using Discord.WebSocket;
using Discord;
using Newtonsoft.Json.Linq;
using System.Net.Mime;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace gallery.bot;

public class Bot : IDisposable
{
    public readonly Curator Curator;

    private readonly DiscordSocketClient client;
    private readonly string token;
    private readonly ulong channelId;
    private readonly ulong guildId;

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

    public void Dispose()
    {
        client?.Dispose();
        Curator.Dispose();
    }
}
