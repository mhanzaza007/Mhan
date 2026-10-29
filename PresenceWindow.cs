using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AppleMusicDiscordPresence;

internal sealed class PresenceWindow : Form
{
    private readonly TextBox _applicationId = new();
    private readonly CheckBox _showAppleMusic = new();
    private readonly Label _status = new();
    private readonly Label _track = new();
    private readonly Panel _statusDot = new();
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
        ClientSize = new Size(720, 590);
        MinimumSize = new Size(680, 560);
        Font = new Font("Segoe UI", 10F);
        _appIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? (Icon)SystemIcons.Application.Clone();
        Icon = _appIcon;

        BuildInterface();
        ConfigureTrayIcon();

        var savedSettings = SettingsStore.Load();
        _applicationId.Text = savedSettings.DiscordApplicationId;
        _showAppleMusic.Checked = savedSettings.ShowAppleMusic;
        _showAppleMusic.CheckedChanged += async (_, _) => await OnDisplaySelectionChangedAsync();
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

            if (startInBackground && savedSettings.ShowAppleMusic && Regex.IsMatch(savedSettings.DiscordApplicationId, "^[0-9]{17,20}$"))
                await StartPresenceAsync();
            else if (startInBackground && !savedSettings.ShowAppleMusic)
                SetStatus("ปิดการแสดง Apple Music ไว้");
            if (!_updateRestarting)
                await CheckForUpdatesAsync();
        };
    }

    private void BuildInterface()
    {
        BackColor = Color.FromArgb(244, 242, 238);
        ForeColor = Color.FromArgb(40, 38, 42);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = BackColor,
            Padding = new Padding(24),
            ColumnCount = 1,
            RowCount = 9
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 148));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(40, 36, 42), Padding = new Padding(20, 12, 18, 12) };
        var accent = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Color.FromArgb(193, 67, 80) };
        var brand = new Label
        {
            Text = "Mhan",
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 19F, FontStyle.Bold),
            Location = new Point(20, 8)
        };
        var subtitle = new Label
        {
            Text = "Apple Music  →  Discord",
            AutoSize = true,
            ForeColor = Color.FromArgb(207, 200, 203),
            Font = new Font("Segoe UI", 9.5F),
            Location = new Point(22, 42)
        };
        header.Controls.AddRange([brand, subtitle, accent]);

        var musicCard = CreateCard();
        var musicHeading = CreateSectionHeading("การแสดงผล");
        musicHeading.Location = new Point(18, 14);
        var musicHint = new Label
        {
            Text = "เลือกข้อมูลที่ต้องการแชร์บนโปรไฟล์ Discord",
            AutoSize = true,
            ForeColor = Color.FromArgb(116, 111, 113),
            Location = new Point(18, 40)
        };
        _showAppleMusic.Text = "Apple Music";
        _showAppleMusic.AutoSize = true;
        _showAppleMusic.Font = new Font(Font, FontStyle.Bold);
        _showAppleMusic.ForeColor = ForeColor;
        _showAppleMusic.Location = new Point(18, 72);
        var musicDetail = new Label
        {
            Text = "ชื่อเพลงและศิลปินขณะแอปกำลังเล่นเพลง",
            AutoSize = true,
            ForeColor = Color.FromArgb(116, 111, 113),
            Location = new Point(150, 75),
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
        };
        musicCard.Controls.AddRange([musicHeading, musicHint, _showAppleMusic, musicDetail]);

        var discordCard = CreateCard();
        var discordHeading = CreateSectionHeading("เชื่อมต่อ Discord");
        discordHeading.Location = new Point(18, 14);
        var idLabel = new Label
        {
            Text = "Application ID",
            AutoSize = true,
            ForeColor = Color.FromArgb(99, 94, 97),
            Location = new Point(18, 45)
        };
        _applicationId.SetBounds(18, 68, 500, 31);
        _applicationId.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _applicationId.BorderStyle = BorderStyle.FixedSingle;
        _applicationId.BackColor = Color.FromArgb(250, 249, 247);
        _applicationId.ForeColor = ForeColor;
        _applicationId.Font = new Font("Segoe UI", 10F);

        var developerLink = new LinkLabel
        {
            Text = "สร้างหรือดู Application ID ใน Discord Developer Portal  ↗",
            AutoSize = true,
            LinkColor = Color.FromArgb(79, 93, 150),
            ActiveLinkColor = Color.FromArgb(60, 70, 120),
            Location = new Point(18, 111)
        };
        developerLink.LinkClicked += (_, _) => OpenDeveloperPortal();
        discordCard.Controls.AddRange([discordHeading, idLabel, _applicationId, developerLink]);

        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = BackColor };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        _startButton.Text = "บันทึก ID และเริ่มแชร์";
        StyleButton(_startButton, Color.FromArgb(185, 55, 69), Color.White);
        _startButton.Margin = new Padding(0, 0, 8, 0);
        _startButton.Click += async (_, _) => await StartPresenceAsync();

        _stopButton.Text = "หยุดแชร์";
        StyleButton(_stopButton, Color.FromArgb(226, 222, 217), Color.FromArgb(58, 54, 57));
        _stopButton.Enabled = false;
        _stopButton.Margin = new Padding(0, 0, 8, 0);
        _stopButton.Click += async (_, _) => await StopPresenceAsync();

        var updatesButton = new Button { Text = "อัปเดตอัตโนมัติ" };
        StyleButton(updatesButton, Color.FromArgb(226, 222, 217), Color.FromArgb(58, 54, 57));
        updatesButton.Margin = Padding.Empty;
        updatesButton.Click += (_, _) => ShowUpdateSettings();
        actions.Controls.Add(_startButton, 0, 0);
        actions.Controls.Add(_stopButton, 1, 0);
        actions.Controls.Add(updatesButton, 2, 0);

        var statusCard = CreateCard();
        _statusDot.SetBounds(18, 22, 10, 10);
        _statusDot.BackColor = Color.FromArgb(181, 145, 84);
        _status.Text = "ยังไม่เริ่ม";
        _status.AutoSize = true;
        _status.Font = new Font(Font, FontStyle.Bold);
        _status.ForeColor = Color.FromArgb(102, 96, 97);
        _status.Location = new Point(36, 17);
        _track.Text = "ใส่ Application ID แล้วเลือกเริ่มแชร์เพื่อเชื่อมต่อ";
        _track.AutoSize = false;
        _track.AutoEllipsis = true;
        _track.ForeColor = Color.FromArgb(116, 111, 113);
        _track.SetBounds(18, 48, 560, 24);
        _track.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        statusCard.Controls.AddRange([_statusDot, _status, _track]);

        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(musicCard, 0, 2);
        layout.Controls.Add(discordCard, 0, 4);
        layout.Controls.Add(actions, 0, 6);
        layout.Controls.Add(statusCard, 0, 8);
        Controls.Add(layout);
    }

    private static Panel CreateCard()
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16) };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(227, 222, 216));
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        return card;
    }

    private static Label CreateSectionHeading(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        ForeColor = Color.FromArgb(45, 41, 45)
    };

    private static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.Dock = DockStyle.Fill;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
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
        if (!_showAppleMusic.Checked)
        {
            SetStatus("ติ๊ก Apple Music ก่อนเริ่มแชร์");
            return;
        }

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

    private async Task OnDisplaySelectionChangedAsync()
    {
        try
        {
            var settings = SettingsStore.Load();
            settings.DiscordApplicationId = _applicationId.Text.Trim();
            settings.ShowAppleMusic = _showAppleMusic.Checked;
            SettingsStore.Save(settings);

            if (!_showAppleMusic.Checked)
            {
                if (_isRunning) await StopPresenceAsync();
                else SetStatus("ปิดการแสดง Apple Music ไว้");
                return;
            }

            if (!_isRunning && Regex.IsMatch(_applicationId.Text.Trim(), "^[0-9]{17,20}$"))
                await StartPresenceAsync();
            else if (!_isRunning)
                SetStatus("เลือก Apple Music แล้ว; ใส่ Application ID เพื่อเริ่มแชร์");
        }
        catch (Exception ex)
        {
            SetStatus($"บันทึกตัวเลือกไม่ได้: {ShortError(ex)}");
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

    private void SetStatus(string message)
    {
        _status.Text = message;
        var color = message.Contains("กำลังแชร์", StringComparison.Ordinal)
            ? Color.FromArgb(54, 133, 101)
            : message.Contains("เชื่อมต่อไม่ได้", StringComparison.Ordinal)
                || message.Contains("ไม่สำเร็จ", StringComparison.Ordinal)
                || message.Contains("เริ่มไม่ได้", StringComparison.Ordinal)
                ? Color.FromArgb(185, 55, 69)
                : Color.FromArgb(181, 145, 84);
        _status.ForeColor = color;
        _statusDot.BackColor = color;
    }

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

