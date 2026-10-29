using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AppleMusicDiscordPresence;

internal sealed class PresenceWindow : Form
{
    private readonly TextBox _applicationId = new();
    private readonly Label _status = new();
    private readonly Label _track = new();
    private readonly Button _startButton = new();
    private readonly Button _stopButton = new();
    private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = 2500 };
    private readonly System.Windows.Forms.Timer _updateTimer = new() { Interval = 6 * 60 * 60 * 1000 };
    private readonly NotifyIcon _trayIcon = new();
    private readonly Icon _appIcon;
    private readonly AppleMusicSessionReader _sessionReader = new();
    private readonly AppleMusicArtworkLookup _artworkLookup = new();
    private readonly DiscordIpcClient _discord = new();
    private ToolStripMenuItem? _autoStartMenuItem;
    private string? _lastTrackKey;
    private bool _isPolling;
    private bool _isRunning;
    private bool _lastArtworkAvailable;
    private bool _exitRequested;
    private bool _isCheckingForUpdates;
    private bool _updateRestarting;
    private readonly bool _startInBackground;

    public PresenceWindow(bool startInBackground)
    {
        _startInBackground = startInBackground;
        Text = "Mhan";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(510, 315);
        MinimumSize = new Size(526, 355);
        Font = new Font("Segoe UI", 10F);
        _appIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? (Icon)SystemIcons.Application.Clone();
        Icon = _appIcon;

        BuildInterface();
        ConfigureTrayIcon();

        var savedApplicationId = SettingsStore.Load().DiscordApplicationId;
        _applicationId.Text = savedApplicationId;
        _pollTimer.Tick += async (_, _) => await PollAsync();
        _updateTimer.Tick += async (_, _) => await CheckForUpdatesAsync();
        _updateTimer.Start();
        FormClosing += OnFormClosing;

        Shown += async (_, _) =>
        {
            if (startInBackground)
            {
                ShowInTaskbar = false;
                WindowState = FormWindowState.Minimized;
                Hide();
            }

            if (startInBackground && Regex.IsMatch(savedApplicationId, "^[0-9]{17,20}$"))
                await StartPresenceAsync();
            if (!_updateRestarting)
                await CheckForUpdatesAsync();
        };
    }

    private void BuildInterface()
    {
        var heading = new Label
        {
            Text = "แชร์เพลง Apple Music ไปยัง Discord",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Location = new Point(22, 18)
        };

        var intro = new Label
        {
            Text = "ใส่ Application ID ของ Discord แล้วกดเริ่มแชร์\nโปรแกรมจะแสดงชื่อเพลงกับศิลปินขณะกำลังเล่น",
            AutoSize = true,
            Location = new Point(22, 53)
        };

        var idLabel = new Label { Text = "Discord Application ID", AutoSize = true, Location = new Point(22, 112) };
        _applicationId.SetBounds(22, 137, 455, 30);
        _applicationId.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var developerLink = new LinkLabel
        {
            Text = "เปิด Discord Developer Portal เพื่อสร้างแอป",
            AutoSize = true,
            Location = new Point(22, 174)
        };
        developerLink.LinkClicked += (_, _) => OpenDeveloperPortal();

        _startButton.Text = "บันทึกและเริ่มแชร์";
        _startButton.SetBounds(22, 207, 150, 36);
        _startButton.Click += async (_, _) => await StartPresenceAsync();

        _stopButton.Text = "หยุดแชร์";
        _stopButton.SetBounds(182, 207, 100, 36);
        _stopButton.Enabled = false;
        _stopButton.Click += async (_, _) => await StopPresenceAsync();

        var updatesButton = new Button
        {
            Text = "ตั้งค่าอัปเดตอัตโนมัติ",
            Location = new Point(295, 207),
            Size = new Size(182, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        updatesButton.Click += (_, _) => ShowUpdateSettings();

        _status.Text = "สถานะ: ยังไม่เริ่ม";
        _status.AutoSize = true;
        _status.Location = new Point(22, 255);

        _track.Text = "";
        _track.AutoSize = false;
        _track.AutoEllipsis = true;
        _track.SetBounds(22, 275, 455, 24);
        _track.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        Controls.AddRange([heading, intro, idLabel, _applicationId, developerLink, _startButton, _stopButton, updatesButton, _status, _track]);
    }

    private void ConfigureTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("เปิดหน้าต่าง", null, (_, _) => ShowWindow());
        menu.Items.Add("หยุดแชร์", null, async (_, _) => await StopPresenceAsync());
        menu.Items.Add("ตั้งค่าอัปเดตอัตโนมัติ", null, (_, _) => ShowUpdateSettings());
        menu.Items.Add(new ToolStripSeparator());
        _autoStartMenuItem = new ToolStripMenuItem("เปิดเมื่อเข้า Windows")
        {
            CheckOnClick = true,
            Checked = StartupManager.IsEnabled()
        };
        _autoStartMenuItem.CheckedChanged += (_, _) =>
        {
            try
            {
                StartupManager.SetEnabled(_autoStartMenuItem.Checked);
                _trayIcon.ShowBalloonTip(1800, "Mhan", _autoStartMenuItem.Checked
                    ? "Mhan จะเริ่มทำงานเมื่อเข้า Windows"
                    : "ปิดการเริ่ม Mhan พร้อม Windows แล้ว", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _autoStartMenuItem.Checked = !_autoStartMenuItem.Checked;
                _trayIcon.ShowBalloonTip(2500, "Mhan", ShortError(ex), ToolTipIcon.Error);
            }
        };
        menu.Items.Add(_autoStartMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("ออกจากโปรแกรม", null, async (_, _) => await ExitAsync());

        _trayIcon.Icon = _appIcon;
        _trayIcon.Text = "Mhan";
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.Visible = true;
        _trayIcon.DoubleClick += (_, _) => ShowWindow();
    }

    private async Task StartPresenceAsync()
    {
        var applicationId = _applicationId.Text.Trim();
        if (!Regex.IsMatch(applicationId, "^[0-9]{17,20}$"))
        {
            SetStatus("กรุณาใส่ Application ID เป็นตัวเลข 17–20 หลัก");
            return;
        }

        try
        {
            var settings = SettingsStore.Load();
            settings.DiscordApplicationId = applicationId;
            SettingsStore.Save(settings);
            StartupManager.SetEnabled(true);
            if (_autoStartMenuItem is not null) _autoStartMenuItem.Checked = true;
            _isRunning = true;
            _lastTrackKey = null;
            _lastArtworkAvailable = false;
            _applicationId.Enabled = false;
            _startButton.Enabled = false;
            _stopButton.Enabled = true;
            SetStatus("กำลังหาเพลง Apple Music...");
            _pollTimer.Start();
            await PollAsync();
        }
        catch (Exception ex)
        {
            await StopPresenceAsync();
            SetStatus($"เริ่มไม่ได้: {ShortError(ex)}");
        }
    }

    private async Task StopPresenceAsync()
    {
        _isRunning = false;
        _pollTimer.Stop();
        _applicationId.Enabled = true;
        _startButton.Enabled = true;
        _stopButton.Enabled = false;
        _lastTrackKey = null;
        _lastArtworkAvailable = false;
        _track.Text = "";

        try
        {
            var applicationId = _applicationId.Text.Trim();
            if (Regex.IsMatch(applicationId, "^[0-9]{17,20}$") && _discord.IsConnected)
                await _discord.ClearActivityAsync(applicationId, CancellationToken.None);
            SetStatus("หยุดแชร์แล้ว");
        }
        catch (Exception ex)
        {
            SetStatus($"หยุดแชร์แล้ว (ล้างสถานะ Discord ไม่สำเร็จ: {ShortError(ex)})");
        }
    }

    private async Task PollAsync()
    {
        if (!_isRunning || _isPolling) return;
        _isPolling = true;
        try
        {
            var track = await _sessionReader.ReadCurrentTrackAsync();
            if (!_isRunning) return;

            if (track is null)
            {
                await ClearIfNeededAsync();
                SetStatus("ยังไม่พบเพลงจาก Apple Music");
                _track.Text = "เปิด Apple Music และเริ่มเล่นเพลงบนเครื่องนี้";
                return;
            }

            _track.Text = $"{track.Title} — {track.Artist}";
            if (!track.IsPlaying)
            {
                await ClearIfNeededAsync();
                SetStatus("Apple Music หยุดหรือพักเพลงอยู่");
                return;
            }

            var key = $"{track.Title}\u001f{track.Artist}";
            if (key != _lastTrackKey || !_discord.IsConnected)
            {
                var artwork = await _artworkLookup.FindAsync(track.Title, track.Artist);
                if (!_isRunning) return;
                track = track with { ArtworkUrl = artwork?.ImageUrl, TrackUrl = artwork?.TrackUrl };
                await _discord.SetTrackAsync(_applicationId.Text.Trim(), track, CancellationToken.None);
                _lastTrackKey = key;
                _lastArtworkAvailable = artwork is not null;
            }
            SetStatus(!_lastArtworkAvailable
                ? "กำลังแชร์เพลงไปยัง Discord (ไม่พบรูปปกในแคตตาล็อก)"
                : "กำลังแชร์เพลงไปยัง Discord");
        }
        catch (Exception ex)
        {
            SetStatus($"เชื่อมต่อไม่ได้: {ShortError(ex)}");
        }
        finally
        {
            _isPolling = false;
        }
    }

    private void ShowUpdateSettings()
    {
        using var dialog = new GitHubUpdateSettingsDialog();
        dialog.ShowDialog(this);
        if (dialog.TokenWasSaved)
            _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        if (_isCheckingForUpdates || _exitRequested) return;
        var token = GitHubTokenStore.Load();
        if (string.IsNullOrWhiteSpace(token)) return;

        _isCheckingForUpdates = true;
        try
        {
            var update = await new GitHubReleaseUpdater().DownloadIfNewerAsync(token, CancellationToken.None);
            if (update is null) return;

            SetStatus($"กำลังติดตั้งอัปเดต {update.Tag}...");
            await StopPresenceAsync();
            var restartInBackground = _startInBackground || !Visible || WindowState == FormWindowState.Minimized;
            GitHubUpdateInstaller.ApplyAfterExit(update.Path, restartInBackground);
            _updateRestarting = true;
            _exitRequested = true;
            _trayIcon.Visible = false;
            await _discord.DisposeAsync();
            Close();
        }
        catch (Exception ex)
        {
            SetStatus($"ตรวจสอบอัปเดตไม่สำเร็จ: {ShortError(ex)}");
        }
        finally
        {
            _isCheckingForUpdates = false;
        }
    }

    private async Task ClearIfNeededAsync()
    {
        if (_lastTrackKey is null || !_discord.IsConnected) return;
        await _discord.ClearActivityAsync(_applicationId.Text.Trim(), CancellationToken.None);
        _lastTrackKey = null;
    }

    private void SetStatus(string message) => _status.Text = $"สถานะ: {message}";

    private static string ShortError(Exception exception)
    {
        var message = exception.Message.Replace(Environment.NewLine, " ");
        return message.Length > 100 ? message[..100] + "…" : message;
    }

    private static void OpenDeveloperPortal()
    {
        Process.Start(new ProcessStartInfo("https://discord.com/developers/applications") { UseShellExecute = true });
    }

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private async Task ExitAsync()
    {
        _exitRequested = true;
        await StopPresenceAsync();
        _trayIcon.Visible = false;
        await _discord.DisposeAsync();
        Close();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_exitRequested) return;
        e.Cancel = true;
        Hide();
        _trayIcon.ShowBalloonTip(1800, "Mhan", "โปรแกรมยังทำงานอยู่ใกล้นาฬิกา", ToolTipIcon.Info);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pollTimer.Dispose();
            _updateTimer.Dispose();
            _trayIcon.Dispose();
            _appIcon.Dispose();
            _ = _discord.DisposeAsync();
        }
        base.Dispose(disposing);
    }
}

