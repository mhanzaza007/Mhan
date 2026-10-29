using Microsoft.Win32;

namespace AppleMusicDiscordPresence;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Mhan";

    public static bool IsEnabled()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return runKey?.GetValue(ValueName) is string;
    }

    public static void SetEnabled(bool enabled)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Windows startup settings could not be opened.");

        if (!enabled)
        {
            runKey.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Could not find the Mhan executable path.");
        runKey.SetValue(ValueName, $"\"{executablePath}\" --background", RegistryValueKind.String);
    }

    public static void EnsureCurrentExecutableIsRegistered()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (runKey?.GetValue(ValueName) is not string currentValue) return;

        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Could not find the Mhan executable path.");
        var expectedValue = $"\"{executablePath}\" --background";
        if (!string.Equals(currentValue, expectedValue, StringComparison.OrdinalIgnoreCase))
            runKey.SetValue(ValueName, expectedValue, RegistryValueKind.String);
    }
}

