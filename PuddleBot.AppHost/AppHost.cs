using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(Path.GetFullPath("../PuddleBot/appsettings.json"), optional: false, reloadOnChange: true);

var lavalinkPort = builder.Configuration.GetValue<int?>("Lavalink:Port") 
    ?? throw new InvalidOperationException("Lavalink:Port is not configured.");

var lavalink = builder.AddContainer("lavalink", "fredboat/lavalink")
    .WithHttpEndpoint(port: lavalinkPort, targetPort: lavalinkPort)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["SERVER_PORT"] = lavalinkPort;
        context.EnvironmentVariables["LAVALINK_SERVER_PASSWORD"] = builder.Configuration["Lavalink:Password"]
            ?? throw new InvalidOperationException("Lavalink:Password is not configured.");
    
        context.EnvironmentVariables["LAVALINK_SERVER_SOURCES_YOUTUBE"] = false;
        context.EnvironmentVariables["LAVALINK_PLUGINS_0_DEPENDENCY"] = "dev.lavalink.youtube:youtube-plugin:1.13.3";
        context.EnvironmentVariables["PLUGINS_YOUTUBE_ENABLED"] = true;
        context.EnvironmentVariables["PLUGINS_YOUTUBE_CLIENTS_0"] = "MUSIC";
        context.EnvironmentVariables["PLUGINS_YOUTUBE_CLIENTS_1"] = "ANDROID_VR";
        context.EnvironmentVariables["PLUGINS_YOUTUBE_CLIENTS_2"] = "WEB";
        context.EnvironmentVariables["PLUGINS_YOUTUBE_CLIENTS_3"] = "WEBEMBEDDED";

        var spotifyClientId = builder.Configuration["Spotify:ClientId"];
        var spotifyClientSecret = builder.Configuration["Spotify:ClientSecret"];

        if (!string.IsNullOrEmpty(spotifyClientId) && !string.IsNullOrEmpty(spotifyClientSecret))
        {
            context.EnvironmentVariables["LAVALINK_PLUGINS_1_DEPENDENCY"] = "com.github.topi314.lavasrc:lavasrc-plugin:4.7.1";
            context.EnvironmentVariables["PLUGINS_LAVASRC_SOURCES_SPOTIFY"] = true;
            context.EnvironmentVariables["PLUGINS_LAVASRC_SOURCES_APPLEMUSIC"] = true;
            context.EnvironmentVariables["PLUGINS_LAVASRC_SPOTIFY_CLIENTID"] = spotifyClientId;
            context.EnvironmentVariables["PLUGINS_LAVASRC_SPOTIFY_CLIENTSECRET"] = spotifyClientSecret;
        }
    });

var lavalinkEndpoint = lavalink.GetEndpoint("http");

builder.AddProject<Projects.PuddleBot>("puddlebot")
    .WithReference(lavalinkEndpoint)
    .WaitFor(lavalink);

builder.Build().Run();
