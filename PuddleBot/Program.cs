using Lavalink4NET.Extensions;
using Lavalink4NET.NetCord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using PuddleBot.Context;
using PuddleBot.Extensions;

namespace PuddleBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Logging.AddConsole();

            builder.Configuration
               .AddEnvironmentVariables()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            VerifyRequiredConfiguration(builder.Configuration);
            WarnOptionalConfiguration(builder.Configuration);

            builder.AddServiceDefaults();

            builder.Services
                .AddDiscordGateway(opt =>
                {
                    opt.Intents = GatewayIntents.AllNonPrivileged;
                })
                .AddApplicationCommands()
                .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
                .AddLavalink()
                .ConfigureLavalink(opt =>
                {
                    //If running locally, aspire will provide the lavalink server address
                    var aspireLavalink = builder.Configuration.GetValue<string?>("services:lavalink:http:0");
                    opt.BaseAddress = new Uri(aspireLavalink ?? builder.Configuration["Lavalink:BaseAddress"]!);
                    opt.Passphrase = builder.Configuration["Lavalink:Password"]!;
                })
                .AddSingleton<MusicContext>();

            var host = builder.Build()
                .AddModules(typeof(Program).Assembly)
                .AddSlashCommand("ping", "Ping!", () => "Pong!")
                .UseGatewayEventHandlers();

            var gatewayClient = host.Services.GetRequiredService<GatewayClient>();
            gatewayClient.Ready += async e =>
            {
                var restClient = host.Services.GetRequiredService<RestClient>();
                var emojis = await restClient.GetApplicationEmojisAsync(e.ApplicationId);

                foreach (var emoji in emojis)
                {
                    var emojiString = $"<:{emoji.Name}:{emoji.Id}>";

                    _ = emoji.Name switch
                    {
                        "Youtube" => LavalinkTrackExtensions.YoutubeEmoji = emojiString,
                        "Soundcloud" => LavalinkTrackExtensions.SoundCloudEmoji = emojiString,
                        "Bandcamp" => LavalinkTrackExtensions.BandcampEmoji = emojiString,
                        "Twitch" => LavalinkTrackExtensions.TwitchEmoji = emojiString,
                        "AppleMusic" => LavalinkTrackExtensions.AppleMusicEmoji = emojiString,
                        "Vimeo" => LavalinkTrackExtensions.VimeoEmoji = emojiString,
                        "Spotify" => LavalinkTrackExtensions.SpotifyEmoji = emojiString,
                        _ => null
                    };
                }
            };

            await host.RunAsync();
        }

        private static void VerifyRequiredConfiguration(ConfigurationManager config)
        {
            List<(string Key, string Message)> requiredKeys =
            [
                ("Discord:Token", "The token used to login to the discord bot user."),
                ("Lavalink:BaseAddress", "The URL and port of your lavalink server."),
                ("Lavalink:Password", "The passphrase of your lavalink server.")
            ];

            var missingKeys = requiredKeys.Where(x => string.IsNullOrEmpty(config[x.Key]));

            if (missingKeys.Any())
            {
                var message = $"{DateTime.Now:MM/dd/yyyy hh:mm:ss tt} [ERROR] Missing required configuration keys: ";

                foreach (var (Key, Message) in missingKeys)
                {
                    message += $"{Environment.NewLine}\t- {Key}. {Message}";
                }

                Console.WriteLine(message);
                throw new InvalidOperationException(message);
            }
        }

        private static void WarnOptionalConfiguration(ConfigurationManager config)
        {
            List<(string Key, string Message)> optionalKeys =
            [
                ("Spotify:ClientId", "Spotify API client id."),
                ("Spotify:ClientSecret", "Spotify API client secret.")
            ];

            var missingKeys = optionalKeys.Where(x => string.IsNullOrEmpty(config[x.Key]));

            if (missingKeys.Any())
            {
                var message = $"{DateTime.Now:MM/dd/yyyy hh:mm:ss tt} [WARN] Missing optional configuration keys: ";

                foreach (var (Key, Message) in missingKeys)
                {
                    message += $"{Environment.NewLine}\t- {Key}. {Message}";
                }

                Console.WriteLine(message);
            }
        }
    }
}
