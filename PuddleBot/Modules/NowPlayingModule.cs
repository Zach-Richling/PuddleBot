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
    public class NowPlayingModule(MusicContext musicContext) : ComponentInteractionModule<ButtonInteractionContext>
    {
        public const string SkipId = "skip-track";
        public const string PauseId = "pause-track";
        public const string ResumeId = "resume-track";
        public const string VolumeUpId = "volume-up";
        public const string VolumeDownId = "volume-down";
        public const string StopId = "stop-track";

        [ComponentInteraction(SkipId)]
        public async Task SkipButton()
        {
            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);
            
            if (!playerResult.IsSuccess)
            {
                return;
            }
            
            var player = playerResult.Player;

            if (player.CurrentTrack != null)
            {
                await RespondAsync($"Skipped: {player.CurrentTrack.IconTitle()}", Context.User);
            }

            await player.SkipAsync();
        }

        [ComponentInteraction(PauseId)]
        public async Task PauseButton()
        {
            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);

            if (!playerResult.IsSuccess)
            {
                return;
            }

            var player = playerResult.Player;

            if (player.IsPaused)
            {
                await RespondAsync("The track is already paused.", Context.User);
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
                            m.Components = [MusicContext.GetNowPlayingActionRow(true)];
                        });
                    }
                }

                await RespondAsync("Playback has been paused.", Context.User);
            }
        }

        [ComponentInteraction(ResumeId)]
        public async Task ResumeButton()
        {
            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);

            if (!playerResult.IsSuccess)
            {
                return;
            }

            var player = playerResult.Player;

            if (!player.IsPaused)
            {
                await RespondAsync("The track is already playing.", Context.User);
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
                            m.Components = [MusicContext.GetNowPlayingActionRow(false)];
                        });
                    }
                }

                await RespondAsync("Playback has been resumed.", Context.User);
            }
        }

        [ComponentInteraction(VolumeUpId)]
        public async Task VolumeUpButton()
        {
            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);

            if (!playerResult.IsSuccess)
            {
                return;
            }

            var player = playerResult.Player;

            var newVolume = player.Volume + 0.1f;
            await player.SetVolumeAsync(newVolume);

            await RespondAsync($"Volume has been increased to {Math.Round(newVolume * 100)}%.", Context.User);
        }

        [ComponentInteraction(VolumeDownId)]
        public async Task VolumeDownButton()
        {
            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);

            if (!playerResult.IsSuccess)
            {
                return;
            }

            var player = playerResult.Player;

            var newVolume = player.Volume - 0.1f;
            await player.SetVolumeAsync(newVolume);

            await RespondAsync($"Volume has been decreased to {Math.Round(newVolume * 100)}%.", Context.User);
        }

        [ComponentInteraction(StopId)]
        public async Task StopButton()
        {
            var guild = Context.Guild!;
            var playerResult = await musicContext.GetPlayerAsync(guild.Id);

            if (!playerResult.IsSuccess)
            {
                return;
            }

            var player = playerResult.Player;

            await player.StopAsync();
            await RespondAsync("Playback has been stopped.", Context.User);
        }

        private async Task RespondAsync(string message, User? discordUser = null) => 
            await base.RespondAsync(InteractionCallback.Message(MusicContext.EmbedMessage(message, discordUser)));
    }
}
