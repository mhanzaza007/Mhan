using Windows.Media.Control;

namespace AppleMusicDiscordPresence;

internal sealed record TrackInfo(
    string Title,
    string Artist,
    bool IsPlaying,
    string SourceId,
    string? ArtworkUrl = null,
    string? TrackUrl = null);

internal sealed class AppleMusicSessionReader
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;

    public async Task<TrackInfo?> ReadCurrentTrackAsync()
    {
        _manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

        var session = _manager.GetSessions()
            .FirstOrDefault(candidate => IsAppleMusic(candidate.SourceAppUserModelId));

        if (session is null) return null;

        var properties = await session.TryGetMediaPropertiesAsync();
        var title = string.IsNullOrWhiteSpace(properties.Title) ? "Unknown track" : properties.Title.Trim();
        var artist = string.IsNullOrWhiteSpace(properties.Artist) ? "Unknown artist" : properties.Artist.Trim();
        var isPlaying = session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

        return new TrackInfo(title, artist, isPlaying, session.SourceAppUserModelId);
    }

    private static bool IsAppleMusic(string sourceAppUserModelId) =>
        sourceAppUserModelId.Contains("applemusic", StringComparison.OrdinalIgnoreCase)
        || sourceAppUserModelId.Contains("apple.music", StringComparison.OrdinalIgnoreCase);
}

