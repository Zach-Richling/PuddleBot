using Lavalink4NET;
using Lavalink4NET.Extensions;
using Lavalink4NET.NetCord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using PuddleBot.Context;

namespace PuddleBot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Configuration
               .AddEnvironmentVariables()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            VerifyRequiredConfiguration(builder.Configuration);
            WarnOptionalConfiguration(builder.Configuration);

            builder.Services
                .AddDiscordGateway(opt =>
                {
                    opt.Intents = GatewayIntents.AllNonPrivileged;
                })
                .AddApplicationCommands()
                .AddLavalink()
                .ConfigureLavalink(opt =>
                {
                    opt.BaseAddress = new Uri(builder.Configuration["Lavalink:BaseAddress"]!);
                    opt.Passphrase = builder.Configuration["Lavalink:Password"]!;
                })
                .AddSingleton<MusicContext>();



            var host = builder.Build()
                .AddModules(typeof(Program).Assembly)
                .AddSlashCommand("ping", "Ping!", () => "Pong!")
                .UseGatewayEventHandlers();

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
            List<(string Key, string Message)> optionalKeys = [];

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
