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
        ClientSize = new Size(510, 325);
        Font = new Font("Segoe UI", 9.5F);

        var description = new Label
        {
            Text = "Mhan ต้องใช้ GitHub token แบบอ่านอย่างเดียวเพื่อดาวน์โหลด Release จาก repository ส่วนตัวของคุณ\nสร้าง token แบบ Fine-grained แล้วกำหนดให้เข้าถึงเฉพาะ Mhan และ Contents: Read-only",
            Location = new Point(18, 18),
            Size = new Size(475, 52)
        };
        var link = new LinkLabel
        {
            Text = "เปิดหน้าสร้าง Fine-grained token ใน GitHub",
            Location = new Point(18, 79),
            AutoSize = true
        };
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(
            "https://github.com/settings/personal-access-tokens/new") { UseShellExecute = true });

        var scopeHelp = new Label
        {
            Text = "ตั้งค่า Repository access เป็น Only select repositories → Mhan\nPermissions → Repository permissions → Contents: Read-only\nจากนั้นสร้าง token แล้ววางด้านล่าง (ไม่ต้องส่ง token ให้ใคร)",
            Location = new Point(18, 108),
            Size = new Size(475, 58)
        };

        var tokenLabel = new Label { Text = "GitHub token", AutoSize = true, Location = new Point(18, 180) };
        _token.SetBounds(18, 202, 475, 30);
        _token.UseSystemPasswordChar = true;
        _token.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _status.Text = GitHubTokenStore.Load() is null
            ? "ยังไม่ได้ตั้งค่า token"
            : "มี token ที่บันทึกแบบเข้ารหัสสำหรับบัญชี Windows นี้แล้ว";
        _status.SetBounds(18, 241, 475, 22);
        _status.AutoEllipsis = true;

        var forget = new Button { Text = "ลบ token", Location = new Point(18, 276), Size = new Size(95, 32) };
        forget.Click += (_, _) =>
        {
            GitHubTokenStore.Delete();
            _token.Clear();
            TokenWasSaved = false;
            _status.Text = "ลบ token แล้ว; Mhan จะไม่ตรวจอัปเดตอัตโนมัติ";
        };

        var cancel = new Button { Text = "ปิด", Location = new Point(296, 276), Size = new Size(85, 32), DialogResult = DialogResult.Cancel };
        _save.Text = "ตรวจสอบและบันทึก";
        _save.SetBounds(390, 276, 103, 32);
        _save.Click += async (_, _) => await ValidateAndSaveAsync();

        Controls.AddRange([description, link, scopeHelp, tokenLabel, _token, _status, forget, cancel, _save]);
        AcceptButton = _save;
        CancelButton = cancel;
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

