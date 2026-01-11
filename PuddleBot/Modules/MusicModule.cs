using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
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

            if (musicContext.NowPlayingChannels.TryGetValue(player.GuildId, out var value))
            {
                if (value.Message is not null)
                {
                    await value.Message.ModifyAsync(m =>
                    {
                        m.Components = [MusicContext.GetNowPlayingActionRow(true)];
                    });
                }
            }

            await FollowupAsync(MusicContext.EmbedMessage($"Track has been paused"));
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

            if (musicContext.NowPlayingChannels.TryGetValue(player.GuildId, out var value))
            {
                if (value.Message is not null)
                {
                    await value.Message.ModifyAsync(m =>
                    {
                        m.Components = [MusicContext.GetNowPlayingActionRow(false)];
                    });
                }
            }

            await FollowupAsync(MusicContext.EmbedMessage($"Track has been resumed"));
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

            await FollowupAsync(MusicContext.EmbedMessage($"Stopped playback and cleared the queue."));
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
                await FollowupAsync(MusicContext.EmbedMessage($"Skipped: {player.CurrentTrack.IconTitle()}"));
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
                await FollowupAsync(MusicContext.EmbedMessage("There are no tracks in the queue."));
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

            await FollowupAsync(MusicContext.EmbedMessage(builder.ToString()));
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
            await FollowupAsync(MusicContext.EmbedMessage($"Cleared: {queueCount} tracks"));
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

            await FollowupAsync(MusicContext.EmbedMessage($"Shuffle: {(player.Shuffle ? "On" : "Off")}"));
        }

        [SlashCommand("repeat", "Toggle repeat mode.", Contexts = [InteractionContextType.Guild])]
        public async Task Repeat()
        {
            await RespondLoadingAsync();

            var playerResult = await GetPlayerAsync();
            if (!playerResult.IsSuccess)
                return;

            var player = playerResult.Player;

            player.RepeatMode = player.RepeatMode != TrackRepeatMode.Track
                ? TrackRepeatMode.Track
                : TrackRepeatMode.None;

            await FollowupAsync(MusicContext.EmbedMessage($"Repeat: {(player.RepeatMode == TrackRepeatMode.Track ? "On" : "Off")}"));
        }

        private async Task<PlayerResult<QueuedLavalinkPlayer>> GetPlayerAsync(ulong? channelId = null)
        {
            var playerResult = await musicContext.GetPlayerAsync(Context.Guild!.Id, channelId);

            if (!playerResult.IsSuccess)
            {
                await FollowupAsync(MusicContext.EmbedMessage(GetErrorMessage(playerResult.Status)));
            }

            return playerResult;
        }

        private async Task Play(string url, bool top)
        {
            await RespondLoadingAsync();
            var guild = Context.Guild!;

            if (!guild.VoiceStates.TryGetValue(Context.User.Id, out var voiceState))
            {
                await FollowupAsync(MusicContext.EmbedMessage("You are not connected to any voice channel!"));
                return;
            }

            var textChannelId = Context.Interaction.Channel.Id;

            musicContext.NowPlayingChannels.AddOrUpdate(
                guild.Id,
                key => (textChannelId, null),
                (key, value) => value.ChannelId != textChannelId ? (textChannelId, null) : value
            );

            var client = Context.Client;

            var playerResult = await GetPlayerAsync(voiceState.ChannelId);
            if (!playerResult.IsSuccess)
                return;

            var player = playerResult.Player;

            var tracks = await musicContext.AudioService.Tracks.LoadTracksAsync(url, TrackSearchMode.YouTube);

            if (tracks.IsFailed)
            {
                await FollowupAsync(MusicContext.EmbedMessage($"Error: {tracks.Exception?.Message}"));
                return;
            }

            if (tracks.Count == 0)
            {
                await FollowupAsync(MusicContext.EmbedMessage("Couldn't find any tracks."));
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
                await FollowupAsync(MusicContext.EmbedMessage($"Queued: {tracks.Count} tracks"));
            }
            else
            {
                await FollowupAsync(MusicContext.EmbedMessage($"Queued: {tracks.Track.IconTitle()}"));
            }
        }

        private async Task<InteractionCallbackResponse?> RespondLoadingAsync() =>
            await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading));
    }
}
