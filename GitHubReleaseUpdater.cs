using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace AppleMusicDiscordPresence;

internal sealed record DownloadedMhanUpdate(string Tag, string Path);

/// <summary>
/// Checks and downloads the single latest Mhan.exe from the public GitHub release.
/// Authentication is optional so the updater also works if the repository becomes private.
/// </summary>
internal sealed class GitHubReleaseUpdater
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/mhanzaza007/Mhan/releases/latest";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(5) };

    public async Task ValidateAccessAsync(string? token, CancellationToken cancellationToken)
    {
        var release = await GetLatestReleaseAsync(token, cancellationToken);
        if (release.AssetUrl is null)
            throw new InvalidOperationException("GitHub Release ล่าสุดไม่มีไฟล์ Mhan.exe");
        if (release.Sha256 is null)
            throw new InvalidOperationException("GitHub Release ไม่มี SHA-256 digest สำหรับตรวจสอบไฟล์อัปเดต");
    }

    public async Task<DownloadedMhanUpdate?> DownloadIfNewerAsync(string? token, CancellationToken cancellationToken)
    {
        var release = await GetLatestReleaseAsync(token, cancellationToken);
        if (release.AssetUrl is null || release.Sha256 is null)
            throw new InvalidOperationException("GitHub Release ล่าสุดไม่มีไฟล์ Mhan.exe หรือ SHA-256 สำหรับตรวจสอบ");
        if (!System.Text.RegularExpressions.Regex.IsMatch(release.Tag, @"^v\d+\.\d+\.\d+$"))
            throw new InvalidDataException("ชื่อ tag ของ GitHub Release ไม่อยู่ในรูปแบบ v1.2.3");
        if (!Version.TryParse(release.Tag[1..], out var releaseVersion))
            throw new InvalidDataException("ไม่สามารถอ่านหมายเลขเวอร์ชันจาก GitHub Release ได้");

        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        if (currentVersion is not null && NormalizeVersion(releaseVersion) <= NormalizeVersion(currentVersion))
            return null;

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
            await EnsureSuccessAsync(response, cancellationToken, string.IsNullOrWhiteSpace(token));

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
        string? token, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, LatestReleaseUrl, token, "application/vnd.github+json");
        using var response = await Client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken, string.IsNullOrWhiteSpace(token));

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

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? token, string accept)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd(accept);
        request.Headers.UserAgent.ParseAdd("Mhan/1.6.0");
        request.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response, CancellationToken cancellationToken, bool anonymousRequest = false)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("GitHub token ไม่ถูกต้องหรือหมดอายุ");
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException(anonymousRequest
                ? "ไม่พบ GitHub Release; ตรวจว่า repository เป็น Public และมี Release ล่าสุดพร้อมไฟล์ Mhan.exe"
                : "token นี้อ่าน Release ของ Mhan ไม่ได้ ตรวจสิทธิ์ Contents: read และเลือก repository Mhan");

        var details = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"GitHub ตอบกลับ {(int)response.StatusCode}: {details}", null, response.StatusCode);
    }

    private static string GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static Version NormalizeVersion(Version version) => new(
        version.Major,
        Math.Max(version.Minor, 0),
        Math.Max(version.Build, 0),
        Math.Max(version.Revision, 0));
}
