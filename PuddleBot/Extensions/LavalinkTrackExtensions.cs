using Lavalink4NET.Tracks;

namespace PuddleBot.Extensions
{
    internal static class LavalinkTrackExtensions
    {
        public static string IconTitle(this LavalinkTrack track) => $"{GetSourceIcon(track)} {track.Title}";
        public static string IconTitleTime(this LavalinkTrack track) => $"{GetSourceIcon(track)} {track.Title}. ({track.Duration:hh\\:mm\\:ss})";
        private static string GetSourceIcon(LavalinkTrack track) => track.SourceName switch
        { 
            "youtube" => "<:Youtube:1386415534400344094>",
            "soundcloud" => "<:Soundcloud:1386415494462177280>",
            "bandcamp" => "<:Bandcamp:1386415447796350996>",
            "twitch" => "<:Twitch:1386415430318686449>",
            "applemusic" => "<:AppleMusic:1386415518386229390>",
            "vimeo" => "<:Vimeo:1386415443081957396>",
            _ => "<:Youtube:1386415534400344094>" // Default to YouTube icon for unsupported sources
        };
        
    }
}
