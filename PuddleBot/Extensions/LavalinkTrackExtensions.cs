using Lavalink4NET.Tracks;

namespace PuddleBot.Extensions
{
    internal static class LavalinkTrackExtensions
    {
        public static string IconTitle(this LavalinkTrack track) => $"{GetSourceIcon(track)} {track.Title}";
        public static string IconTitleTime(this LavalinkTrack track) => $"{GetSourceIcon(track)} {track.Title}. ({track.Duration:hh\\:mm\\:ss})";
        private static string GetSourceIcon(LavalinkTrack track) => track.SourceName switch
        { 
            "youtube" => "<:Youtube:1386204946546036737>",
            "soundcloud" => "<:Soundcloud:1386205241934086154>",
            "bandcamp" => "<:Bandcamp:1386205694222536755>",
            "twitch" => "<:Twitch:1386205704381267968>",
            "applemusic" => "<:AppleMusic:1386204975415300166>",
            _ => "<:Youtube:1386204946546036737>" // Default to YouTube icon for unsupported sources
        };
        
    }
}
