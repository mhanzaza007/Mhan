using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace AppleMusicDiscordPresence;

internal static class GitHubUpdateInstaller
{
    private const string ReplaceScript = """
        param([int]$WaitForPid, [string]$StagePath, [string]$CurrentPath, [string]$ReleaseTag, [bool]$StartInBackground)
        $ErrorActionPreference = 'Stop'

        $currentDir = Split-Path -Parent $CurrentPath
        $currentDirName = Split-Path -Leaf $currentDir
        $targetDir = $currentDir
        if ($currentDirName -match '^Mhan-v\d+\.\d+\.\d+$' -and $ReleaseTag -match '^v\d+\.\d+\.\d+$') {
            $installRoot = Split-Path -Parent $currentDir
            $targetDir = Join-Path $installRoot ("Mhan-" + $ReleaseTag)
        }
        $TargetPath = Join-Path $targetDir 'Mhan.exe'
        $candidatePath = Join-Path $targetDir ".Mhan-update-$WaitForPid.exe"
        $backupPath = Join-Path $targetDir ".Mhan-rollback-$WaitForPid.exe"
        $oldStartupValue = $null
        $startupChanged = $false
        $startedProcess = $null
        $installSucceeded = $false

        $deadline = [DateTime]::UtcNow.AddSeconds(90)
        while ((Get-Process -Id $WaitForPid -ErrorAction SilentlyContinue) -and [DateTime]::UtcNow -lt $deadline) {
            Start-Sleep -Milliseconds 250
        }

        try {
            if (Get-Process -Id $WaitForPid -ErrorAction SilentlyContinue) {
                throw 'Mhan did not close in time.'
            }

            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            Remove-Item -LiteralPath $candidatePath, $backupPath -Force -ErrorAction SilentlyContinue
            Copy-Item -LiteralPath $StagePath -Destination $candidatePath -Force
            if (Test-Path -LiteralPath $TargetPath) {
                [System.IO.File]::Replace($candidatePath, $TargetPath, $backupPath)
            } else {
                [System.IO.File]::Move($candidatePath, $TargetPath)
            }

            $runKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Microsoft\Windows\CurrentVersion\Run', $true)
            if ($runKey) {
                try {
                    $oldStartupValue = $runKey.GetValue('Mhan', $null)
                    if ($oldStartupValue -is [string]) {
                        $runKey.SetValue('Mhan', ('"{0}" --background' -f $TargetPath), [Microsoft.Win32.RegistryValueKind]::String)
                        $startupChanged = $true
                    }
                } finally {
                    $runKey.Close()
                }
            }

            if ($StartInBackground) {
                $startedProcess = Start-Process -FilePath $TargetPath -ArgumentList '--background' -WindowStyle Hidden -PassThru
            } else {
                $startedProcess = Start-Process -FilePath $TargetPath -WindowStyle Hidden -PassThru
            }
            Start-Sleep -Milliseconds 1500
            if ($startedProcess.HasExited) {
                throw 'The updated Mhan closed immediately after launch.'
            }

            $installSucceeded = $true
        } catch {
            if (Test-Path -LiteralPath $backupPath) {
                Remove-Item -LiteralPath $TargetPath -Force -ErrorAction SilentlyContinue
                Move-Item -LiteralPath $backupPath -Destination $TargetPath -Force -ErrorAction SilentlyContinue
            } elseif ($TargetPath -ne $CurrentPath) {
                Remove-Item -LiteralPath $TargetPath -Force -ErrorAction SilentlyContinue
            }

            if ($startupChanged) {
                $restoreKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Microsoft\Windows\CurrentVersion\Run', $true)
                if ($restoreKey) {
                    try { $restoreKey.SetValue('Mhan', $oldStartupValue, [Microsoft.Win32.RegistryValueKind]::String) }
                    finally { $restoreKey.Close() }
                }
            }

            Remove-Item -LiteralPath $candidatePath, $StagePath -Force -ErrorAction SilentlyContinue
            if ($TargetPath -ne $CurrentPath -and (Test-Path -LiteralPath $targetDir)) {
                if ((Get-ChildItem -LiteralPath $targetDir -Force | Measure-Object).Count -eq 0) {
                    Remove-Item -LiteralPath $targetDir -Force -ErrorAction SilentlyContinue
                }
            }

            try {
                Add-Type -AssemblyName System.Windows.Forms
                [System.Windows.Forms.MessageBox]::Show(
                    "Mhan couldn't install the update. The current version will be opened. $($_.Exception.Message)",
                    'Mhan update', 'OK', 'Warning') | Out-Null
            } catch { }
            if (Test-Path -LiteralPath $CurrentPath) {
                if ($StartInBackground) {
                    Start-Process -FilePath $CurrentPath -ArgumentList '--background' -WindowStyle Hidden
                } else {
                    Start-Process -FilePath $CurrentPath -WindowStyle Hidden
                }
            }
        }

        if ($installSucceeded) {
            Remove-Item -LiteralPath $backupPath, $StagePath, $candidatePath -Force -ErrorAction SilentlyContinue
            if ($CurrentPath -ne $TargetPath) {
                Remove-Item -LiteralPath $CurrentPath -Force -ErrorAction SilentlyContinue
                if ($currentDirName -match '^Mhan-v\d+\.\d+\.\d+$' -and (Test-Path -LiteralPath $currentDir)) {
                    if ((Get-ChildItem -LiteralPath $currentDir -Force | Measure-Object).Count -eq 0) {
                        Remove-Item -LiteralPath $currentDir -Force -ErrorAction SilentlyContinue
                    }
                }
            }
        }
        """;

    public static void ApplyAfterExit(string stagedExecutable, string releaseTag, bool startInBackground)
    {
        if (!Regex.IsMatch(releaseTag, @"^v\d+\.\d+\.\d+$"))
            throw new InvalidDataException("ชื่อ tag ของ GitHub Release ไม่อยู่ในรูปแบบ v1.2.3");

        var currentExecutable = Environment.ProcessPath
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
        startInfo.ArgumentList.Add(currentExecutable);
        startInfo.ArgumentList.Add(releaseTag);
        startInfo.ArgumentList.Add(startInBackground ? "$true" : "$false");
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the Mhan update helper.");
    }
}

