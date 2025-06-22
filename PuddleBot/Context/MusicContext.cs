using Lavalink4NET;
using NetCord.Rest;
using System.Collections.Concurrent;
using Lavalink4NET.Events.Players;
using NetCord;
using PuddleBot.Extensions;

namespace PuddleBot.Context
{
    public class MusicContext
    {
        public IAudioService AudioService { get; }
        public readonly ConcurrentDictionary<ulong, (ulong? channelId, ulong? messageId)> NowPlayingChannels = [];

        private readonly RestClient restClient;

        public MusicContext(IAudioService audioService, RestClient restClient)
        {
            audioService.TrackStarted += TrackStartedHandler;
            audioService.TrackException += TrackExceptionHandler;
            AudioService = audioService;
            this.restClient = restClient;
        }

        private static MessageProperties EmbedMessage(string message) => new() 
        {
            Embeds = [
                new()
                {
                    Description = message,
                    Color = new Color(230, 126, 34) // Orange
                }
            ]
        };
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
                    var newMessageId = await restClient.SendMessageAsync(channelId, EmbedMessage($"Now Playing: {track.IconTitleTime()}"));

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
                    await restClient.SendMessageAsync(channelId, EmbedMessage($"Error: {args.Track.IconTitle()}. {args.Exception.Message}"));
                }
            }
        }
    }
}
