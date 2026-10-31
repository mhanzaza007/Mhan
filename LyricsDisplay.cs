using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AppleMusicDiscordPresence;

internal sealed class LyricsDisplay
{
    private static readonly HttpClient Client = CreateClient();
    private static readonly Regex TimestampPattern = new(@"\[(?<minutes>\d{2,}):(?<seconds>\d{2})(?:\.(?<fraction>\d{1,3}))?\]", RegexOptions.Compiled);
    private readonly Dictionary<string, IReadOnlyList<(TimeSpan At, string Text)>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _retryAfter = new(StringComparer.OrdinalIgnoreCase);

    public async Task<string?> GetLineAsync(string title, string artist, TimeSpan position)
    {
        var key = $"{artist}\u001f{title}";
        if (!_cache.TryGetValue(key, out var lines))
        {
            if (_retryAfter.TryGetValue(key, out var retryAt) && DateTime.UtcNow < retryAt)
                return null;

            lines = await FetchAsync(key, title, artist);
            if (lines is null) return null;

            _cache[key] = lines;
            _retryAfter.Remove(key);
        }

        if (lines.Count == 0) return null;
        string? current = null;
        foreach (var line in lines)
        {
            if (line.At > position) break;
            if (!string.IsNullOrWhiteSpace(line.Text)) current = line.Text;
        }
        return current;
    }

    private async Task<IReadOnlyList<(TimeSpan At, string Text)>?> FetchAsync(string cacheKey, string title, string artist)
    {
        TimeSpan? retryDelay = null;
        var artistCandidates = new List<string> { artist };
        string? album = null;
        foreach (var separator in new[] { " — ", " – ", " | " })
        {
            var separatorIndex = artist.IndexOf(separator, StringComparison.Ordinal);
            if (separatorIndex > 0)
            {
                artistCandidates.Add(artist[..separatorIndex].Trim());
                album = artist[(separatorIndex + separator.Length)..].Trim();
                break;
            }
        }

        var candidates = artistCandidates.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        for (var i = 0; i < candidates.Length; i++)
        {
            try
            {
                if (i > 0) await Task.Delay(TimeSpan.FromMilliseconds(350));
                using var response = await Client.GetAsync(BuildLrclibUrl(title, candidates[i], album));
                if (response.StatusCode is System.Net.HttpStatusCode.TooManyRequests or System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    retryDelay = GetRetryDelay(response);
                    break;
                }
                if (!response.IsSuccessStatusCode) continue;

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var json = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 16 });
                if (!json.RootElement.TryGetProperty("syncedLyrics", out var synced) || synced.ValueKind != JsonValueKind.String)
                    continue;
                var result = ParseLrc(synced.GetString() ?? "");
                if (result.Count > 0) return result;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                retryDelay = TimeSpan.FromSeconds(5);
                break;
            }
        }

        try
        {
            using var response = await Client.GetAsync(BuildLrcMuxUrl(title, candidates[^1], album));
            if (response.StatusCode is System.Net.HttpStatusCode.TooManyRequests or System.Net.HttpStatusCode.ServiceUnavailable)
            {
                var muxDelay = GetRetryDelay(response);
                if (retryDelay is null || muxDelay > retryDelay.Value) retryDelay = muxDelay;
            }
            else if (response.IsSuccessStatusCode)
            {
                var lrc = await response.Content.ReadAsStringAsync();
                var result = ParseLrc(lrc);
                if (result.Count > 0) return result;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            if (retryDelay is null || retryDelay.Value < TimeSpan.FromSeconds(30))
                retryDelay = TimeSpan.FromSeconds(30);
        }

        _retryAfter[cacheKey] = DateTime.UtcNow.Add(retryDelay ?? TimeSpan.FromSeconds(30));
        return null;
    }

    private static string BuildLrclibUrl(string title, string artist, string? album)
    {
        var url = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(title)}&artist_name={Uri.EscapeDataString(artist)}";
        return string.IsNullOrWhiteSpace(album) ? url : $"{url}&album_name={Uri.EscapeDataString(album)}";
    }

    private static string BuildLrcMuxUrl(string title, string artist, string? album)
    {
        var url = $"https://api.lrcmux.dev/get?title={Uri.EscapeDataString(title)}&artist={Uri.EscapeDataString(artist)}";
        if (!string.IsNullOrWhiteSpace(album)) url += $"&album={Uri.EscapeDataString(album)}";
        return $"{url}&level=line&strict=true&format=lrc";
    }

    private static IReadOnlyList<(TimeSpan At, string Text)> ParseLrc(string contents)
    {
        var result = new List<(TimeSpan At, string Text)>();
        foreach (var line in contents.Split('\n'))
        {
            var matches = TimestampPattern.Matches(line);
            if (matches.Count == 0) continue;
            var text = TimestampPattern.Replace(line, "").Trim();
            foreach (Match match in matches)
            {
                if (!int.TryParse(match.Groups["minutes"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
                    || !int.TryParse(match.Groups["seconds"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
                    || minutes > 360 || seconds > 59)
                    continue;

                var fraction = match.Groups["fraction"].Value;
                if (fraction.Length > 0 && !int.TryParse(fraction, NumberStyles.None, CultureInfo.InvariantCulture, out _))
                    continue;
                var milliseconds = fraction.Length switch
                {
                    1 => int.Parse(fraction, CultureInfo.InvariantCulture) * 100,
                    2 => int.Parse(fraction, CultureInfo.InvariantCulture) * 10,
                    3 => int.Parse(fraction, CultureInfo.InvariantCulture),
                    _ => 0
                };
                result.Add((TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(milliseconds), text));
            }
        }
        return result.OrderBy(item => item.At).ToArray();
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        var seconds = retryAfter?.Delta?.TotalSeconds
            ?? (retryAfter?.Date is DateTimeOffset date ? (date - DateTimeOffset.UtcNow).TotalSeconds : 2);
        if (!double.IsFinite(seconds)) seconds = 2;
        return TimeSpan.FromSeconds(Math.Clamp(seconds, 2, 300));
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(5),
            MaxResponseContentBufferSize = 1_000_000
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mhan", "1.6.0"));
        return client;
    }
}
