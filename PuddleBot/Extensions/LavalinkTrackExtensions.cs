using Lavalink4NET.Tracks;

namespace PuddleBot.Extensions
{
    internal static class LavalinkTrackExtensions
    {
        public static string YoutubeEmoji { get; set; } = string.Empty;
        public static string SoundCloudEmoji { get; set; } = string.Empty;
        public static string BandcampEmoji { get; set; } = string.Empty;
        public static string TwitchEmoji { get; set; } = string.Empty;
        public static string AppleMusicEmoji { get; set; } = string.Empty;
        public static string VimeoEmoji { get; set; } = string.Empty;

        public static string IconTitle(this LavalinkTrack track) => $"{GetSourceIcon(track)} {track.Title}";
        public static string IconTitleTime(this LavalinkTrack track) => $"{GetSourceIcon(track)} {track.Title}. ({track.Duration:hh\\:mm\\:ss})";
        
        private static string GetSourceIcon(LavalinkTrack track) => track.SourceName switch
        { 
            "youtube" => YoutubeEmoji,
            "soundcloud" => SoundCloudEmoji,
            "bandcamp" => BandcampEmoji,
            "twitch" => TwitchEmoji,
            "applemusic" => AppleMusicEmoji,
            "vimeo" => VimeoEmoji,
            _ => string.Empty
        };
    }
}
