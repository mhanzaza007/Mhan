using Windows.Media.Control;

namespace AppleMusicDiscordPresence;

internal sealed record TrackInfo(
    string Title,
    string Artist,
    bool IsPlaying,
    string ServiceName,
    string SourceId,
    TimeSpan Position,
    string? ArtworkUrl = null,
    string? TrackUrl = null,
    string? PreviewLyric = null);

internal sealed class AppleMusicSessionReader
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;

    public async Task<TrackInfo?> ReadCurrentTrackAsync(bool showAppleMusic, bool showSpotify)
    {
        _manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

        var sessions = _manager.GetSessions()
            .Select(candidate => (Session: candidate, ServiceName: GetServiceName(candidate.SourceAppUserModelId)))
            .Where(candidate => candidate.ServiceName == "Apple Music" && showAppleMusic
                || candidate.ServiceName == "Spotify" && showSpotify)
            .ToArray();

        var session = sessions.FirstOrDefault(candidate => candidate.Session.GetPlaybackInfo().PlaybackStatus
                == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            .Session
            ?? sessions.FirstOrDefault().Session;

        if (session is null) return null;

        var properties = await session.TryGetMediaPropertiesAsync();
        var title = string.IsNullOrWhiteSpace(properties.Title) ? "Unknown track" : properties.Title.Trim();
        var artist = string.IsNullOrWhiteSpace(properties.Artist) ? "Unknown artist" : properties.Artist.Trim();
        var isPlaying = session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        var serviceName = GetServiceName(session.SourceAppUserModelId) ?? "Music";
        var position = session.GetTimelineProperties().Position;

        return new TrackInfo(title, artist, isPlaying, serviceName, session.SourceAppUserModelId, position);
    }

    private static string? GetServiceName(string sourceAppUserModelId)
    {
        if (sourceAppUserModelId.Contains("applemusic", StringComparison.OrdinalIgnoreCase)
            || sourceAppUserModelId.Contains("apple.music", StringComparison.OrdinalIgnoreCase))
            return "Apple Music";

        if (sourceAppUserModelId.Contains("spotify", StringComparison.OrdinalIgnoreCase))
            return "Spotify";

        return null;
    }
}
