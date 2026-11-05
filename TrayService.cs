using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using FloatSnap;

namespace FloatSnap;

/// <summary>
/// 托盘图标与右键菜单（自启开关 / 打开截图文件夹 / 退出）。
/// </summary>
public class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Window _mainWindow;

    public TrayService(Window mainWindow)
    {
        _mainWindow = mainWindow;

        var menu = new ContextMenuStrip();

        var autoStartItem = new ToolStripMenuItem("开机自启动")
        {
            Checked = AutoStart.IsEnabled(),
            CheckOnClick = true
        };
        autoStartItem.CheckedChanged += (_, _) =>
        {
            if (autoStartItem.Checked) AutoStart.Enable(); else AutoStart.Disable();
        };

        var openFolderItem = new ToolStripMenuItem("打开截图文件夹");
        openFolderItem.Click += (_, _) => OpenScreenshotFolder();

        var exitItem = new ToolStripMenuItem("退出");

        menu.Items.Add(autoStartItem);
        menu.Items.Add(openFolderItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _icon = new NotifyIcon
        {
            Icon = ExtractIcon(),
            Text = "FloatSnap - 点一下截全屏，自动存图并复制",
            Visible = true,
            ContextMenuStrip = menu
        };

        _icon.DoubleClick += (_, _) => _mainWindow.Show();

        exitItem.Click += (_, _) =>
        {
            _icon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        };
    }

    public void NotifySaved(string fileName, int width, int height, bool copiedToClipboard)
    {
        string text = copiedToClipboard
            ? $"已保存 {fileName}（{width}×{height}），也已复制到剪贴板"
            : $"已保存 {fileName}（{width}×{height}），但剪贴板被其他程序占用";
        _icon.ShowBalloonTip(2000, "FloatSnap", text, ToolTipIcon.Info);
    }

    public void NotifyFailure(string message)
    {
        _icon.ShowBalloonTip(3000, "FloatSnap", $"截图失败：{message}", ToolTipIcon.Error);
    }

    private static void OpenScreenshotFolder()
    {
        string folder = ScreenCapture.ScreenshotFolder;
        System.IO.Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }

    private static Icon ExtractIcon()
    {
        try
        {
            // 单文件发布时 Assembly.Location 为空，用 ProcessPath 拿 exe 路径
            string? path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path))
            {
                using var ico = Icon.ExtractAssociatedIcon(path);
                if (ico != null) return new Icon(ico, 16, 16);
            }
        }
        catch { }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
