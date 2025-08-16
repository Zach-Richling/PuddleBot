using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using PuddleBot.Context;
using PuddleBot.Extensions;
using System.Text;

namespace PuddleBot.Modules
{
    public class MusicModule(MusicContext musicContext) : ApplicationCommandModule<ApplicationCommandContext>
    {
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

        private static InteractionMessageProperties EmbedMessage(string message) => new()
        {
            Embeds = [
                new()
                {
                    Description = message,
                    Color = new Color(230, 126, 34) //Orange
                }
            ]
        };

        private static string GetErrorMessage(PlayerRetrieveStatus retrieveStatus) => retrieveStatus switch
        {
            PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
            PlayerRetrieveStatus.BotNotConnected => "The bot is not currently connected.",
            _ => "Unknown error.",
        };

        [SlashCommand("play", "Play a track.", Contexts = [InteractionContextType.Guild])]
        public async Task Play(string url) => await Play(url, false);

        [SlashCommand("play-top", "Play a track at the top of the queue.", Contexts = [InteractionContextType.Guild])]
        public async Task PlayTop(string url) => await Play(url, true);

        [SlashCommand("pause", "Pauses the current track.", Contexts = [InteractionContextType.Guild])]
        public async Task Pause()
        {
            await RespondLoadingAsync();

            var playerResult = await GetPlayerAsync();
            if (!playerResult.IsSuccess)
                return;

            var player = playerResult.Player;
            await player.PauseAsync();

            await FollowupAsync(EmbedMessage($"Track has been paused"));
        }

        [SlashCommand("resume", "Resumes the current track.", Contexts = [InteractionContextType.Guild])]
        public async Task Resume()
        {
            await RespondLoadingAsync();

            var playerResult = await GetPlayerAsync();
            if (!playerResult.IsSuccess)
                return;

            var player = playerResult.Player;
            await player.ResumeAsync();

            await FollowupAsync(EmbedMessage($"Track has been resumed"));
        }

        [SlashCommand("stop", "Stops playing tracks.", Contexts = [InteractionContextType.Guild])]
        public async Task Stop()
        {
            await RespondLoadingAsync();
            var playerResult = await GetPlayerAsync();
            if (!playerResult.IsSuccess)
                return;

            var player = playerResult.Player;
            await player.StopAsync();

            await FollowupAsync(EmbedMessage($"Stopped playback and cleared the queue."));
        }

        [SlashCommand("skip", "Skip the current track.", Contexts = [InteractionContextType.Guild])]
        public async Task Skip()
        {
            await RespondLoadingAsync();

            var playerResult = await GetPlayerAsync();
            if (!playerResult.IsSuccess)
                return;

            var player = playerResult.Player;

            if (player.CurrentTrack != null)
            {
                await FollowupAsync(EmbedMessage($"Skipped: {player.CurrentTrack.IconTitle()}"));
            }

            await player.SkipAsync();
        }

        [SlashCommand("queue", "Lists the current tracks in the queue.", Contexts = [InteractionContextType.Guild])]
        public async Task Queue()
        {
            await RespondLoadingAsync();

            var playerResult = await GetPlayerAsync();
            if (!playerResult.IsSuccess)
                return;

            var queue = playerResult.Player.Queue;
            var nowPlaying = playerResult.Player.CurrentTrack;

            if (nowPlaying == null)
            {
                await FollowupAsync(EmbedMessage("There are no tracks in the queue."));
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"**Now Playing**: {nowPlaying.IconTitleTime()}");

            builder.AppendLine();
            builder.AppendLine("**Queued Tracks:**");
            var totalDuration = new TimeSpan();
            for (int i = 0; i < Math.Min(queue.Count, 10); i++)
            {
                var track = queue[i].Track;
                builder.AppendLine($"{i}. {track?.IconTitleTime()}");

                if (track != null)
                {
                    totalDuration = totalDuration.Add(track.Duration);
                }
            }

            if (queue.Count > 10)
            {
                builder.AppendLine($"and {queue.Count - 10} more...");
            }

            builder.AppendLine();
            builder.AppendLine($"Total Time: {totalDuration:hh\\:mm\\:ss}");

            await FollowupAsync(EmbedMessage(builder.ToString()));
        }

        [SlashCommand("clear", "Clears the queue.", Contexts = [InteractionContextType.Guild])]
        public async Task Clear()
        {
            await RespondLoadingAsync();

            var playerResult = await GetPlayerAsync();
            if (!playerResult.IsSuccess)
                return;

            var player = playerResult.Player;
            var queueCount = player.Queue.Count;

            await player.Queue.RemoveRangeAsync(0, queueCount);
            await FollowupAsync(EmbedMessage($"Cleared: {queueCount} tracks"));
        }

        [SlashCommand("shuffle", "Shuffles the queue.", Contexts = [InteractionContextType.Guild])]
        public async Task Shuffle()
        {
            await RespondLoadingAsync();

            var playerResult = await GetPlayerAsync();
            if (!playerResult.IsSuccess)
                return;

            var player = playerResult.Player;
            player.Shuffle = !player.Shuffle;

            await FollowupAsync(EmbedMessage($"Shuffle: {(player.Shuffle ? "On" : "Off")}"));
        }

        private async Task<PlayerResult<QueuedLavalinkPlayer>> GetPlayerAsync(ulong? channelId = null)
        {
            var playerResult = await musicContext.AudioService.Players.RetrieveAsync(
                Context.Guild!.Id,
                channelId,
                playerFactory: PlayerFactory.Queued,
                options: playerOptions,
                retrieveOptions: retrieveOptions
            );

            if (!playerResult.IsSuccess)
            {
                await FollowupAsync(EmbedMessage(GetErrorMessage(playerResult.Status)));
            }

            return playerResult;
        }

        private async Task Play(string url, bool top)
        {
            await RespondLoadingAsync();
            var guild = Context.Guild!;

            if (!guild.VoiceStates.TryGetValue(Context.User.Id, out var voiceState))
            {
                await FollowupAsync(EmbedMessage("You are not connected to any voice channel!"));
                return;
            }

            var textChannelId = Context.Interaction.Channel.Id;

            musicContext.NowPlayingChannels.AddOrUpdate(
                guild.Id,
                key => (textChannelId, null),
                (key, value) => value.channelId != textChannelId ? (textChannelId, null) : value
            );

            var client = Context.Client;

            var playerResult = await GetPlayerAsync(voiceState.ChannelId);
            if (!playerResult.IsSuccess)
                return;

            var player = playerResult.Player;

            var tracks = await musicContext.AudioService.Tracks.LoadTracksAsync(url, TrackSearchMode.YouTube);

            if (tracks.IsFailed)
            {
                await FollowupAsync(EmbedMessage($"Error: {tracks.Exception?.Message}"));
                return;
            }

            if (tracks.Count == 0)
            {
                await FollowupAsync(EmbedMessage("Couldn't find any tracks."));
                return;
            }

            var isValidUri = Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            foreach (var track in tracks.Tracks)
            {
                if (top)
                {
                    await player.Queue.InsertAsync(0, new TrackQueueItem(track));
                }
                else
                {
                    await player.PlayAsync(track);
                }

                if (!isValidUri)
                {
                    break;
                }
            }

            if (tracks.Count > 1 && isValidUri)
            {
                await FollowupAsync(EmbedMessage($"Queued: {tracks.Count} tracks"));
            }
            else
            {
                await FollowupAsync(EmbedMessage($"Queued: {tracks.Track.IconTitle()}"));
            }
        }

        private async Task<InteractionCallbackResponse?> RespondLoadingAsync() =>
            await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading));
    }
}
