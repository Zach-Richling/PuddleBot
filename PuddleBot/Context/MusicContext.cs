using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using PuddleBot.Extensions;
using System.Collections.Concurrent;

namespace PuddleBot.Context
{
    public class MusicContext
    {
        public IAudioService AudioService { get; }
        public readonly ConcurrentDictionary<ulong, (ulong? channelId, ulong? messageId)> NowPlayingChannels = [];

        private readonly RestClient restClient;

        private static readonly IOptions<QueuedLavalinkPlayerOptions> playerOptions = Options.Create(new QueuedLavalinkPlayerOptions()
        {
            DisconnectOnStop = true,
            DisconnectOnDestroy = true,
            ClearQueueOnStop = true,
        });

        private static readonly PlayerRetrieveOptions retrieveOptions = new PlayerRetrieveOptions()
        {
            ChannelBehavior = PlayerChannelBehavior.Join
        };

        public static InteractionMessageProperties EmbedMessage(string message) => new()
        {
            Embeds = [
                new()
                {
                    Description = message,
                    Color = new Color(230, 126, 34) //Orange
                }
            ]
        };

        public static MessageProperties EmbedMessageRest(string message) => new()
        {
            Embeds = [
                new()
                {
                    Description = message,
                    Color = new Color(230, 126, 34) //Orange
                }
            ]
        };

        public MusicContext(IAudioService audioService, RestClient restClient)
        {
            audioService.TrackStarted += TrackStartedHandler;
            audioService.TrackException += TrackExceptionHandler;
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

        private async Task TrackStartedHandler(object sender, TrackStartedEventArgs args)
        {
            if (NowPlayingChannels.TryGetValue(args.Player.GuildId, out var value))
            {
                if (value.channelId is ulong channelId)
                {
                    if (value.messageId is ulong messageId)
                    {
                        await restClient.DeleteMessageAsync(channelId, messageId);
                    }

                    var track = args.Track;
                    var message = EmbedMessageRest($"Now Playing: {track.IconTitleTime()}");
                    message.Components = 
                    [
                        new ActionRowProperties()
                        {
                            Buttons = [
                                new ButtonProperties("skip-track", "Skip", ButtonStyle.Primary)
                            ]
                        }
                    ];

                    var newMessageId = await restClient.SendMessageAsync(channelId, message);

                    NowPlayingChannels.TryUpdate(
                        args.Player.GuildId,
                        (channelId, newMessageId.Id),
                        value
                    );
                }
            }
        }

        private async Task TrackExceptionHandler(object sender, TrackExceptionEventArgs args)
        {
            if (NowPlayingChannels.TryGetValue(args.Player.GuildId, out var value))
            {
                if (value.channelId is ulong channelId)
                {
                    await restClient.SendMessageAsync(channelId, EmbedMessageRest($"Error: {args.Track.IconTitle()}. {args.Exception.Message}"));
                }
            }
        }
    }
}
