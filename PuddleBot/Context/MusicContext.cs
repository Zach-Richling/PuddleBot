using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using PuddleBot.Extensions;
using PuddleBot.Modules;
using System.Collections.Concurrent;

namespace PuddleBot.Context
{
    public class MusicContext
    {
        public IAudioService AudioService { get; }
        public readonly ConcurrentDictionary<ulong, (ulong? ChannelId, RestMessage? Message)> NowPlayingChannels = [];

        private readonly RestClient restClient;

        private static readonly IOptions<QueuedLavalinkPlayerOptions> playerOptions = Options.Create(new QueuedLavalinkPlayerOptions()
        {
            DisconnectOnStop = true,
            DisconnectOnDestroy = true,
            ClearQueueOnStop = true
        });

        private static readonly PlayerRetrieveOptions retrieveOptions = new PlayerRetrieveOptions()
        {
            ChannelBehavior = PlayerChannelBehavior.Join
        };

        public static InteractionMessageProperties EmbedMessage(string message, User? discordUser = null) => new()
        {
            Embeds = [
                new()
                {
                    Description = message,
                    Color = new Color(230, 126, 34), //Orange
                    Author = discordUser != null ? new()
                    {
                        IconUrl = discordUser.GetAvatarUrl()?.ToString(),
                        Name = discordUser.GlobalName
                    } : null
                }
            ]
        };

        public static MessageProperties EmbedMessageRest(string message, User? discordUser = null) => new()
        {
            Embeds = [
                new()
                {
                    Description = message,
                    Color = new Color(230, 126, 34), //Orange
                    Author = discordUser != null ? new()
                    {
                        IconUrl = discordUser.GetAvatarUrl()?.ToString(),
                        Name = discordUser.GlobalName
                    } : null
                }
            ]
        };

        public MusicContext(IAudioService audioService, RestClient restClient)
        {
            audioService.TrackStarted += TrackStartedHandler;
            audioService.TrackException += TrackExceptionHandler;
            audioService.TrackEnded += TrackEndedHandler;
            AudioService = audioService;
            this.restClient = restClient;
        }

        public async Task<PlayerResult<QueuedLavalinkPlayer>> GetPlayerAsync(ulong guildId, ulong? channelId = null) => await AudioService.Players.RetrieveAsync
        (
            guildId,
            channelId,
            playerFactory: PlayerFactory.Queued,
            options: playerOptions,
            retrieveOptions: retrieveOptions
        );

        public static MessageProperties GetNowPlayingMessage(LavalinkTrack track, bool paused)
        {
            var message = EmbedMessageRest($"Now Playing: {track.IconTitleTime()}");
            message.Components = [GetNowPlayingActionRow(paused)];
            return message;
        }

        public static ActionRowProperties GetNowPlayingActionRow(bool paused) => new ActionRowProperties()
        {
            Buttons =
            [
                new ButtonProperties(NowPlayingModule.SkipId, "⏭", ButtonStyle.Primary),
                paused ? new ButtonProperties(NowPlayingModule.ResumeId, "▶", ButtonStyle.Primary) : new ButtonProperties(NowPlayingModule.PauseId, "⏸", ButtonStyle.Primary),
                new ButtonProperties(NowPlayingModule.VolumeUpId, "🔊", ButtonStyle.Primary),
                new ButtonProperties(NowPlayingModule.VolumeDownId, "🔉", ButtonStyle.Primary),
                new ButtonProperties(NowPlayingModule.StopId, "⏹", ButtonStyle.Danger)
            ]
        };

        private async Task TrackStartedHandler(object sender, TrackStartedEventArgs args)
        {
            if (NowPlayingChannels.TryGetValue(args.Player.GuildId, out var value))
            {
                if (value.ChannelId is ulong channelId)
                {
                    if (value.Message is RestMessage message)
                    {
                        await restClient.DeleteMessageAsync(channelId, message.Id);
                    }

                    var track = args.Track;
                    var newMessage = GetNowPlayingMessage(track, false);

                    var newMessageId = await restClient.SendMessageAsync(channelId, newMessage);

                    NowPlayingChannels.TryUpdate(
                        args.Player.GuildId,
                        (channelId, newMessageId),
                        value
                    );
                }
            }
        }

        private async Task TrackEndedHandler(object sender, TrackEndedEventArgs args)
        {
            if (NowPlayingChannels.TryGetValue(args.Player.GuildId, out var value))
            {
                if (value.Message != null && args.Player.CurrentTrack == null)
                {
                    await restClient.DeleteMessageAsync(value.Message.ChannelId, value.Message.Id);
                    NowPlayingChannels.Remove(args.Player.GuildId, out _);
                }
            }
        }

        private async Task TrackExceptionHandler(object sender, TrackExceptionEventArgs args)
        {
            if (NowPlayingChannels.TryGetValue(args.Player.GuildId, out var value))
            {
                if (value.ChannelId is ulong channelId)
                {
                    await restClient.SendMessageAsync(channelId, EmbedMessageRest($"Error: {args.Track.IconTitle()}. {args.Exception.Message}"));
                }
            }
        }
    }
}
