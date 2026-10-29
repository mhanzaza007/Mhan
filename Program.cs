namespace AppleMusicDiscordPresence;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var startInBackground = args.Contains("--background", StringComparer.OrdinalIgnoreCase);
        using var singleInstance = new Mutex(initiallyOwned: true, "Mhan.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            if (!startInBackground)
                MessageBox.Show("โปรแกรมเปิดอยู่แล้ว สามารถเรียกได้จากไอคอนใกล้นาฬิกา", "Mhan",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new PresenceWindow(startInBackground));
    }
}

