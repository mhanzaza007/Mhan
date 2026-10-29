using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace AppleMusicDiscordPresence;

internal sealed record DownloadedMhanUpdate(string Tag, string Path);

/// <summary>
/// Checks and downloads the single latest Mhan.exe from the private GitHub release.
/// The fine-grained token only needs Contents: read on mhanzaza007/Mhan.
/// </summary>
internal sealed class GitHubReleaseUpdater
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/mhanzaza007/Mhan/releases/latest";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(5) };

    public async Task ValidateTokenAsync(string token, CancellationToken cancellationToken)
    {
        var release = await GetLatestReleaseAsync(token, cancellationToken);
        if (release.AssetUrl is null)
            throw new InvalidOperationException("GitHub Release ล่าสุดไม่มีไฟล์ Mhan.exe");
        if (release.Sha256 is null)
            throw new InvalidOperationException("GitHub Release ไม่มี SHA-256 digest สำหรับตรวจสอบไฟล์อัปเดต");
    }

    public async Task<DownloadedMhanUpdate?> DownloadIfNewerAsync(string token, CancellationToken cancellationToken)
    {
        var release = await GetLatestReleaseAsync(token, cancellationToken);
        if (release.AssetUrl is null || release.Sha256 is null)
            throw new InvalidOperationException("GitHub Release ล่าสุดไม่มีไฟล์ Mhan.exe หรือ SHA-256 สำหรับตรวจสอบ");

        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("ไม่พบตำแหน่งไฟล์ Mhan.exe ปัจจุบัน");
        await using (var current = File.OpenRead(executablePath))
        {
            var currentHash = Convert.ToHexString(await SHA256.HashDataAsync(current, cancellationToken));
            if (currentHash.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase)) return null;
        }

        var stagingPath = Path.Combine(Path.GetTempPath(), $"Mhan-update-{Guid.NewGuid():N}.exe");
        try
        {
            using var request = CreateRequest(HttpMethod.Get, release.AssetUrl, token, "application/octet-stream");
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);

            if (response.Content.Headers.ContentLength is > 1024L * 1024 * 1024)
                throw new InvalidOperationException("ไฟล์อัปเดตมีขนาดใหญ่ผิดปกติ");

            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = new FileStream(stagingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await input.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }

            await using (var downloaded = File.OpenRead(stagingPath))
            {
                var downloadedHash = Convert.ToHexString(await SHA256.HashDataAsync(downloaded, cancellationToken));
                if (!downloadedHash.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA-256 ของไฟล์อัปเดตไม่ตรงกับ GitHub Release");
            }

            return new DownloadedMhanUpdate(release.Tag, stagingPath);
        }
        catch
        {
            try { File.Delete(stagingPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    private static async Task<(string Tag, string? AssetUrl, string? Sha256)> GetLatestReleaseAsync(
        string token, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, LatestReleaseUrl, token, "application/vnd.github+json");
        using var response = await Client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var tag = GetString(root, "tag_name");
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return (tag, null, null);

        foreach (var asset in assets.EnumerateArray())
        {
            if (!string.Equals(GetString(asset, "name"), "Mhan.exe", StringComparison.OrdinalIgnoreCase)) continue;
            var url = GetString(asset, "url");
            var digest = GetString(asset, "digest");
            var sha256 = digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? digest[7..] : null;
            return (tag, Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Host == "api.github.com" ? uri.AbsoluteUri : null, sha256);
        }

        return (tag, null, null);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string token, string accept)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd(accept);
        request.Headers.UserAgent.ParseAdd("Mhan/1.1");
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("GitHub token ไม่ถูกต้องหรือหมดอายุ");
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("token นี้อ่าน Release ส่วนตัวของ Mhan ไม่ได้ ตรวจสิทธิ์ Contents: read และเลือก repo Mhan");

        var details = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"GitHub ตอบกลับ {(int)response.StatusCode}: {details}", null, response.StatusCode);
    }

    private static string GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}

