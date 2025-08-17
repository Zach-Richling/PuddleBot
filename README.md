# Puddle Bot
Puddle Bot is a discord bot for playing music. It is powered by [Lavalink4NET](https://github.com/angelobreuer/Lavalink4NET) and [NetCord](https://github.com/NetCordDev/NetCord).

## Configuration
Settings can be supplied through environment variables or an appsettings.json file.
### Appsettings.json
```json
{
  "Discord": {
    "Token": "YOUR BOT TOKEN"
  },

  "Lavalink": {
    "Password": "YOUR LAVALINK SERVER PASSWORD",

    //If running through aspire locally, supply the Port, otherwise supply the BaseAddress
    "Port": 7625,
    "BaseAddress": "http://LAVALINK SERVER IP:PORT", 
  }
}
```
### Environment Variables
- **Discord:Token** : Your bot token
- **Lavalink:BaseAddress** : Your lavalink server ip and port
- **Lavalink:Password** : Your lavalink server password
## Deployment
You can pull the repo and complile the source yourself or use the image on [docker hub](https://hub.docker.com/r/puddlebuddy/puddle-bot).
### Docker Compose
```yaml
services:
  server:
    image: fredboat/lavalink
    container_name: puddle-bot-lavalink
    restart: unless-stopped
    environment:
      #Lavalink server configuration
      SERVER_PORT: 7625 # This can be any port
      LAVALINK_SERVER_PASSWORD: [YOUR LAVALINK SERVER PASSWORD HERE]

      #Recommended plugin for youtube support
      LAVALINK_SERVER_SOURCES_YOUTUBE: false
      LAVALINK_PLUGINS_0_DEPENDENCY: dev.lavalink.youtube:youtube-plugin:1.13.3
      PLUGINS_YOUTUBE_ENABLED: true
      PLUGINS_YOUTUBE_CLIENTS_0: MUSIC
      PLUGINS_YOUTUBE_CLIENTS_1: ANDROID_VR
      PLUGINS_YOUTUBE_CLIENTS_2: WEB
      PLUGINS_YOUTUBE_CLIENTS_3: WEBEMBEDDED

      #Optional for spotify or apple music support
      #LAVALINK_PLUGINS_1_DEPENDENCY: com.github.topi314.lavasrc:lavasrc-plugin:4.7.1

      #Optional for spotify support
      #PLUGINS_LAVASRC_SOURCES_SPOTIFY: true
      #PLUGINS_LAVASRC_SPOTIFY_CLIENTID: [SPOTIFY API CLIENT ID]
      #PLUGINS_LAVASRC_SPOTIFY_CLIENTSECRET: [SPOTIFY API CLIENT SECRET]

      #Optional for apple music support
      #PLUGINS_LAVASRC_SOURCES_APPLEMUSIC: true

  bot:
    image: puddlebuddy/puddle-bot
    container_name: puddle-bot
    restart: unless-stopped
    depends_on:
      - server
    environment:
      "Discord:Token": [YOUR DISCORD BOT TOKEN HERE]
      "Lavalink:BaseAddress": http://server:7625
      "Lavalink:Password": [YOUR LAVALINK SERVER PASSWORD HERE]
```
