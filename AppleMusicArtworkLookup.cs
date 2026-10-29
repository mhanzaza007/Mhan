using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AppleMusicDiscordPresence;

internal sealed record AppleMusicArtwork(string ImageUrl, string? TrackUrl);

/// <summary>
/// Looks up public Apple catalog artwork so Discord can fetch the image itself.
/// A thumbnail read from Windows' local media session cannot be used directly
/// by Discord, which needs an image URL accessible from its servers.
/// </summary>
internal sealed class AppleMusicArtworkLookup
{
    private static readonly HttpClient Client = CreateClient();
    private readonly Dictionary<string, AppleMusicArtwork?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<AppleMusicArtwork?> FindAsync(string title, string artist)
    {
        var key = $"{Normalize(title)}\u001f{Normalize(artist)}";
        if (_cache.TryGetValue(key, out var cached)) return cached;

        AppleMusicArtwork? result = null;
        try
        {
            var term = Uri.EscapeDataString($"{title} {artist}");
            var url = $"https://itunes.apple.com/search?term={term}&entity=song&limit=25&country=th";
            using var response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            await using var content = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(content);

            if (json.RootElement.TryGetProperty("results", out var results)
                && results.ValueKind == JsonValueKind.Array)
            {
                var normalizedTitle = Normalize(title);
                var normalizedArtist = Normalize(artist);
                foreach (var item in results.EnumerateArray())
                {
                    var candidateTitle = Normalize(GetString(item, "trackName"));
                    var candidateArtist = Normalize(GetString(item, "artistName"));
                    if (candidateTitle != normalizedTitle || !ArtistsMatch(normalizedArtist, candidateArtist)) continue;

                    var imageUrl = MakeHttps(GetString(item, "artworkUrl100"));
                    if (imageUrl is null) continue;

                    imageUrl = imageUrl
                        .Replace("100x100bb", "600x600bb", StringComparison.OrdinalIgnoreCase)
                        .Replace("100x100-", "600x600-", StringComparison.OrdinalIgnoreCase);
                    result = new AppleMusicArtwork(imageUrl, MakeHttps(GetString(item, "trackViewUrl")));
                    break;
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            // Sharing the track name still works if the catalog is unavailable.
        }

        if (_cache.Count >= 128) _cache.Clear();
        _cache[key] = result;
        return result;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mhan", "1.0"));
        return client;
    }

    private static string GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static string Normalize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
        return builder.ToString();
    }

    private static bool ArtistsMatch(string requested, string catalogArtist) =>
        requested.Length > 0 && catalogArtist.Length > 0
        && (requested == catalogArtist || requested.Contains(catalogArtist, StringComparison.Ordinal)
            || catalogArtist.Contains(requested, StringComparison.Ordinal));

    private static string? MakeHttps(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return null;
        return uri.Scheme == Uri.UriSchemeHttps ? uri.AbsoluteUri : new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri.AbsoluteUri;
    }
}

