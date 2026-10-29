using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace AppleMusicDiscordPresence;

/// <summary>
/// Stores a read-only GitHub token encrypted for the current Windows user.
/// The token is only sent to api.github.com when checking or downloading a release.
/// </summary>
internal static class GitHubTokenStore
{
    private const uint UiForbidden = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Length;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob input,
        string? description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr prompt,
        uint flags,
        out DataBlob output);

    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob input,
        IntPtr description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr prompt,
        uint flags,
        out DataBlob output);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);

    public static string? Load()
    {
        var encrypted = SettingsStore.Load().EncryptedGitHubToken;
        if (string.IsNullOrWhiteSpace(encrypted)) return null;

        try
        {
            var tokenBytes = Unprotect(Convert.FromBase64String(encrypted));
            return Encoding.UTF8.GetString(tokenBytes);
        }
        catch (Exception ex) when (ex is FormatException or Win32Exception or CryptographicException)
        {
            return null;
        }
    }

    public static void Save(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var settings = SettingsStore.Load();
        settings.EncryptedGitHubToken = Convert.ToBase64String(Protect(Encoding.UTF8.GetBytes(token)));
        SettingsStore.Save(settings);
    }

    public static void Delete()
    {
        var settings = SettingsStore.Load();
        settings.EncryptedGitHubToken = "";
        SettingsStore.Save(settings);
    }

    private static byte[] Protect(byte[] plainText)
    {
        var input = CreateBlob(plainText);
        try
        {
            if (!CryptProtectData(ref input, "Mhan GitHub read-only token", IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero, UiForbidden, out var output))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            try { return CopyAndFree(output); }
            finally { if (output.Data != IntPtr.Zero) LocalFree(output.Data); }
        }
        finally { if (input.Data != IntPtr.Zero) Marshal.FreeHGlobal(input.Data); }
    }

    private static byte[] Unprotect(byte[] cipherText)
    {
        var input = CreateBlob(cipherText);
        try
        {
            if (!CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero, UiForbidden, out var output))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            try { return CopyAndFree(output); }
            finally { if (output.Data != IntPtr.Zero) LocalFree(output.Data); }
        }
        finally { if (input.Data != IntPtr.Zero) Marshal.FreeHGlobal(input.Data); }
    }

    private static DataBlob CreateBlob(byte[] bytes)
    {
        var memory = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, memory, bytes.Length);
        return new DataBlob { Length = bytes.Length, Data = memory };
    }

    private static byte[] CopyAndFree(DataBlob blob)
    {
        var bytes = new byte[blob.Length];
        Marshal.Copy(blob.Data, bytes, 0, blob.Length);
        return bytes;
    }
}

