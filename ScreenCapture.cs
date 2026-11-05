using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace FloatSnap;

/// <summary>
/// 全屏截图（多显示器时覆盖整个虚拟桌面），保存到图片库并写入剪贴板。
/// </summary>
public static class ScreenCapture
{
    private const int ClipboardRetries = 3;
    private const int RetryDelayMs = 60;

    /// <summary>截图保存目录：图片\FloatSnap</summary>
    public static string ScreenshotFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FloatSnap");

    public static Bitmap CaptureVirtualScreen()
    {
        Rectangle bounds = SystemInformation.VirtualScreen;
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }
        return bitmap;
    }

    /// <summary>
    /// 保存为 PNG（图片\FloatSnap\FloatSnap_yyyyMMdd_HHmmss.png，重名自动加序号），返回完整路径。
    /// </summary>
    public static string SaveToLibrary(Bitmap image)
    {
        string folder = ScreenshotFolder;
        Directory.CreateDirectory(folder);

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = Path.Combine(folder, $"FloatSnap_{stamp}.png");
        for (int suffix = 2; File.Exists(path); suffix++)
        {
            path = Path.Combine(folder, $"FloatSnap_{stamp}_{suffix}.png");
        }

        image.Save(path, ImageFormat.Png);
        return path;
    }

    /// <summary>
    /// 写入剪贴板。剪贴板被其他程序占用时重试，多次失败返回 false。
    /// </summary>
    public static bool CopyToClipboard(Bitmap image)
    {
        for (int attempt = 0; attempt < ClipboardRetries; attempt++)
        {
            try
            {
                Clipboard.SetImage(image);
                return true;
            }
            catch (ExternalException)
            {
                Thread.Sleep(RetryDelayMs);
            }
        }
        return false;
    }
}
