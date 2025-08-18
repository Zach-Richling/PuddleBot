using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using PuddleBot.Context;
using PuddleBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuddleBot.Modules
{
    public class NowPlayingModule(MusicContext musicContext, RestClient client) : ComponentInteractionModule<ButtonInteractionContext>
    {
        public static class NowPlayingInteractions
        {
            public const string SkipTrack = "skip-track";
        }

        [ComponentInteraction("skip-track")]
        public async Task SkipButton()
        {
            await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading));

            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);
            
            if (!playerResult.IsSuccess)
            {
                return;
            }
            
            var player = playerResult.Player;

            if (player.CurrentTrack != null)
            {
                await FollowupAsync(MusicContext.EmbedMessage($"Skipped: {player.CurrentTrack.IconTitle()}"));
            }

            await player.SkipAsync();
        }

        [ComponentInteraction("pause-track")]
        public async Task PauseButton()
        {
            await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading));

            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);

            if (!playerResult.IsSuccess)
            {
                return;
            }

            var player = playerResult.Player;

            if (player.IsPaused)
            {
                await FollowupAsync(MusicContext.EmbedMessage($"The track is already paused."));
            } 
            else
            {
                await player.PauseAsync();

                if (musicContext.NowPlayingChannels.TryGetValue(player.GuildId, out var value))
                {
                    if (value.Message is not null)
                    {
                        await value.Message.ModifyAsync(m =>
                        {
                            m.Components = [musicContext.GetNowPlayingActionRow(true)];
                        });
                    }
                }

                await FollowupAsync(MusicContext.EmbedMessage($"Playback has been paused."));
            }
        }

        [ComponentInteraction("resume-track")]
        public async Task ResumeButton()
        {
            await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading));

            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);

            if (!playerResult.IsSuccess)
            {
                return;
            }

            var player = playerResult.Player;

            if (!player.IsPaused)
            {
                await FollowupAsync(MusicContext.EmbedMessage($"The track is already playing."));
            }
            else
            {
                await player.ResumeAsync();

                if (musicContext.NowPlayingChannels.TryGetValue(player.GuildId, out var value))
                {
                    if (value.Message is not null)
                    {
                        await value.Message.ModifyAsync(m =>
                        {
                            m.Components = [musicContext.GetNowPlayingActionRow(false)];
                        });
                    }
                }

                await FollowupAsync(MusicContext.EmbedMessage($"Playback has been resumed."));
            }
        }

        [ComponentInteraction("volume-up")]
        public async Task VolumeUpButton()
        {
            await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading));

            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);

            if (!playerResult.IsSuccess)
            {
                return;
            }

            var player = playerResult.Player;

            var newVolume = player.Volume + 0.1f;
            await player.SetVolumeAsync(newVolume);
            await FollowupAsync(MusicContext.EmbedMessage($"Volume has been increased to {Math.Round(newVolume * 100)}%."));
        }

        [ComponentInteraction("volume-down")]
        public async Task VolumeDownButton()
        {
            await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading));

            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);

            if (!playerResult.IsSuccess)
            {
                return;
            }

            var player = playerResult.Player;

            var newVolume = player.Volume - 0.1f;
            await player.SetVolumeAsync(newVolume);
            await FollowupAsync(MusicContext.EmbedMessage($"Volume has been decreased to {Math.Round(newVolume * 100)}%."));
        }

        [ComponentInteraction("stop-track")]
        public async Task StopButton()
        {
            await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Loading));

            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);

            if (!playerResult.IsSuccess)
            {
                return;
            }

            var player = playerResult.Player;

            await player.StopAsync();
            await FollowupAsync(MusicContext.EmbedMessage($"Playback has been stopped."));
        }
    }
}
