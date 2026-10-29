using System.Diagnostics;
using System.Text;

namespace AppleMusicDiscordPresence;

internal static class GitHubUpdateInstaller
{
    private const string ReplaceScript = """
        param([int]$WaitForPid, [string]$StagePath, [string]$TargetPath, [bool]$StartInBackground)
        $candidatePath = Join-Path (Split-Path -Parent $TargetPath) ".Mhan-update-$WaitForPid.exe"
        $arguments = @()
        if ($StartInBackground) { $arguments += '--background' }
        $deadline = [DateTime]::UtcNow.AddSeconds(90)
        while ((Get-Process -Id $WaitForPid -ErrorAction SilentlyContinue) -and [DateTime]::UtcNow -lt $deadline) {
            Start-Sleep -Milliseconds 250
        }
        try {
            if (Get-Process -Id $WaitForPid -ErrorAction SilentlyContinue) {
                throw 'Mhan did not close in time.'
            }
            Copy-Item -LiteralPath $StagePath -Destination $candidatePath -Force
            if (Test-Path -LiteralPath $TargetPath) {
                [System.IO.File]::Replace($candidatePath, $TargetPath, $null)
            } else {
                [System.IO.File]::Move($candidatePath, $TargetPath)
            }
            Remove-Item -LiteralPath $StagePath -Force -ErrorAction SilentlyContinue
            if ($StartInBackground) {
                Start-Process -FilePath $TargetPath -ArgumentList '--background' -WindowStyle Hidden
            } else {
                Start-Process -FilePath $TargetPath -WindowStyle Hidden
            }
        } catch {
            Remove-Item -LiteralPath $candidatePath -Force -ErrorAction SilentlyContinue
            try {
                Add-Type -AssemblyName System.Windows.Forms
                [System.Windows.Forms.MessageBox]::Show(
                    "Mhan could not install the update. Open Mhan.exe to continue. $($_.Exception.Message)",
                    'Mhan update', 'OK', 'Warning') | Out-Null
            } catch { }
            if (Test-Path -LiteralPath $TargetPath) {
                if ($StartInBackground) {
                    Start-Process -FilePath $TargetPath -ArgumentList '--background' -WindowStyle Hidden
                } else {
                    Start-Process -FilePath $TargetPath -WindowStyle Hidden
                }
            }
        }
        """;

    public static void ApplyAfterExit(string stagedExecutable, bool startInBackground)
    {
        var targetExecutable = Environment.ProcessPath
            ?? throw new InvalidOperationException("ไม่พบตำแหน่งไฟล์ Mhan.exe ปัจจุบัน");
        var scriptPath = Path.Combine(Path.GetTempPath(), $"Mhan-Update-{Environment.ProcessId}.ps1");
        File.WriteAllText(scriptPath, ReplaceScript, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var powershellPath = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = powershellPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
        startInfo.ArgumentList.Add(stagedExecutable);
        startInfo.ArgumentList.Add(targetExecutable);
        startInfo.ArgumentList.Add(startInBackground ? "$true" : "$false");
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the Mhan update helper.");
    }
}

