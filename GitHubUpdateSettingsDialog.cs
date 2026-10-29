using System.Diagnostics;

namespace AppleMusicDiscordPresence;

internal sealed class GitHubUpdateSettingsDialog : Form
{
    private readonly TextBox _token = new();
    private readonly Label _status = new();
    private readonly Button _save = new();
    public bool TokenWasSaved { get; private set; }

    public GitHubUpdateSettingsDialog()
    {
        Text = "ตั้งค่าอัปเดตอัตโนมัติ — Mhan";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 410);
        BackColor = Color.FromArgb(255, 249, 241);
        ForeColor = Color.FromArgb(74, 61, 57);
        Font = new Font("Segoe UI", 9.5F);

        var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(241, 244, 232) };
        var headerAccent = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Color.FromArgb(132, 153, 111) };
        var title = new Label
        {
            Text = "อัปเดตอัตโนมัติ",
            AutoSize = true,
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.FromArgb(74, 61, 57),
            Location = new Point(20, 10)
        };
        var subtitle = new Label
        {
            Text = "เชื่อมต่อ Release ของ Mhan บน GitHub",
            AutoSize = true,
            ForeColor = Color.FromArgb(132, 139, 111),
            Location = new Point(21, 42)
        };
        header.Controls.AddRange([title, subtitle, headerAccent]);

        var description = new Label
        {
            Text = "Mhan ต้องใช้ GitHub token แบบอ่านอย่างเดียวเพื่อดาวน์โหลด Release จาก repository ส่วนตัวของคุณ\nสร้าง token แบบ Fine-grained แล้วกำหนดให้เข้าถึงเฉพาะ Mhan และ Contents: Read-only",
            Location = new Point(20, 88),
            Size = new Size(520, 48),
            ForeColor = Color.FromArgb(84, 72, 66)
        };
        var link = new LinkLabel
        {
            Text = "เปิดหน้าสร้าง Fine-grained token ใน GitHub",
            Location = new Point(20, 142),
            AutoSize = true
        };
        link.LinkColor = Color.FromArgb(103, 130, 82);
        link.ActiveLinkColor = Color.FromArgb(76, 101, 59);
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(
            "https://github.com/settings/personal-access-tokens/new") { UseShellExecute = true });

        var scopeHelp = new Label
        {
            Text = "ตั้งค่า Repository access เป็น Only select repositories → Mhan\nPermissions → Repository permissions → Contents: Read-only\nจากนั้นสร้าง token แล้ววางด้านล่าง (ไม่ต้องส่ง token ให้ใคร)",
            Location = new Point(20, 171),
            Size = new Size(520, 58),
            ForeColor = Color.FromArgb(138, 120, 111)
        };

        var tokenLabel = new Label
        {
            Text = "GitHub token",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Location = new Point(20, 240)
        };
        _token.SetBounds(20, 263, 520, 31);
        _token.UseSystemPasswordChar = true;
        _token.BorderStyle = BorderStyle.FixedSingle;
        _token.BackColor = Color.FromArgb(255, 253, 249);
        _token.ForeColor = ForeColor;
        _token.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _status.Text = GitHubTokenStore.Load() is null
            ? "ยังไม่ได้ตั้งค่า token"
            : "มี token ที่บันทึกแบบเข้ารหัสสำหรับบัญชี Windows นี้แล้ว";
        _status.SetBounds(20, 304, 520, 22);
        _status.AutoEllipsis = true;
        _status.ForeColor = Color.FromArgb(138, 120, 111);

        var forget = new Button { Text = "ลบ token", Location = new Point(20, 350), Size = new Size(100, 36) };
        StyleButton(forget, Color.FromArgb(244, 235, 226), Color.FromArgb(86, 70, 64));
        forget.Click += (_, _) =>
        {
            GitHubTokenStore.Delete();
            _token.Clear();
            TokenWasSaved = false;
            _status.Text = "ลบ token แล้ว; Mhan จะไม่ตรวจอัปเดตอัตโนมัติ";
        };

        var cancel = new Button { Text = "ปิด", Location = new Point(350, 350), Size = new Size(80, 36), DialogResult = DialogResult.Cancel };
        StyleButton(cancel, Color.FromArgb(244, 235, 226), Color.FromArgb(86, 70, 64));
        _save.Text = "ตรวจสอบและบันทึก";
        _save.SetBounds(440, 350, 100, 36);
        StyleButton(_save, Color.FromArgb(132, 153, 111), Color.White);
        _save.Click += async (_, _) => await ValidateAndSaveAsync();

        Controls.AddRange([header, description, link, scopeHelp, tokenLabel, _token, _status, forget, cancel, _save]);
        AcceptButton = _save;
        CancelButton = cancel;
    }

    private static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, .08F);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, .05F);
    }

    private async Task ValidateAndSaveAsync()
    {
        var token = _token.Text.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            _status.Text = "วาง token ก่อนกดตรวจสอบและบันทึก";
            return;
        }

        _save.Enabled = false;
        _status.Text = "กำลังตรวจสอบสิทธิ์กับ GitHub...";
        try
        {
            await new GitHubReleaseUpdater().ValidateTokenAsync(token, CancellationToken.None);
            GitHubTokenStore.Save(token);
            _token.Clear();
            TokenWasSaved = true;
            _status.Text = "ตรวจสอบสำเร็จ บันทึก token แบบเข้ารหัสแล้ว; Mhan จะตรวจเมื่อเปิดโปรแกรม";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message.Length > 115 ? ex.Message[..115] + "…" : ex.Message;
        }
        finally
        {
            _save.Enabled = true;
        }
    }
}

