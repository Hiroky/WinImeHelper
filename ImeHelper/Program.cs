using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Microsoft.Win32;

namespace ImeHelper;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\ImeHelper.SingleInstance", out bool created);
        if (!created) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayContext());
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ImeHelper";

    private readonly ImeEnforcer _enforcer = new();
    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly Icon _iconOn = CreateIcon(Color.FromArgb(0, 120, 215));
    private readonly Icon _iconOff = CreateIcon(Color.Gray);

    public TrayContext()
    {
        _enabledItem = new ToolStripMenuItem("有効", null, (_, _) => SetEnabled(!_enforcer.Enabled));
        _startupItem = new ToolStripMenuItem("Windows 起動時に実行", null, (_, _) => ToggleStartup())
        {
            Checked = IsStartupRegistered()
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_enabledItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => ExitThread());

        _tray = new NotifyIcon { ContextMenuStrip = menu, Visible = true };
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) SetEnabled(!_enforcer.Enabled);
        };

        SetEnabled(true);
    }

    private void SetEnabled(bool enabled)
    {
        _enforcer.Enabled = enabled;
        _enabledItem.Checked = enabled;
        _tray.Icon = enabled ? _iconOn : _iconOff;
        _tray.Text = enabled ? "ImeHelper: 日本語IME 常時ON (有効)" : "ImeHelper: 停止中";
    }

    private static bool IsStartupRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) is string;
    }

    private void ToggleStartup()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (IsStartupRegistered())
            key.DeleteValue(AppName, false);
        else
            key.SetValue(AppName, $"\"{Environment.ProcessPath}\"");
        _startupItem.Checked = IsStartupRegistered();
    }

    private static Icon CreateIcon(Color bg)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using var brush = new SolidBrush(bg);
            g.FillEllipse(brush, 0, 0, 31, 31);
            using var font = new Font("Yu Gothic UI", 15, FontStyle.Bold, GraphicsUnit.Pixel);
            var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("あ", font, Brushes.White, new RectangleF(0, 1, 32, 32), fmt);
        }
        IntPtr h = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(h).Clone();
        NativeMethods.DestroyIcon(h);
        return icon;
    }

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        _tray.Dispose();
        _enforcer.Dispose();
        _iconOn.Dispose();
        _iconOff.Dispose();
        base.ExitThreadCore();
    }
}
