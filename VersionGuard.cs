using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;

namespace FloatSnap;

/// <summary>
/// 启动自检：当前 exe 比系统里已安装的版本旧时提醒用户。
///
/// Windows Installer 会拒绝降级（MajorUpgrade 返回 1638），但免安装 exe 不经过安装程序、
/// 没有任何版本检查——双击就是静默跑旧代码。这道检查放在程序自己身上，是唯一能兜住的场景。
/// 只在「确实有更新的版本已安装」时提示，正常运行完全不打扰。
/// </summary>
internal static class VersionGuard
{
    private const string ProductName = "FloatSnap";

    /// <summary>国内直连 Gitee，GitHub 需要 CDN 加速，所以默认走 Gitee 的最新版页面。</summary>
    private const string DownloadPage = "https://gitee.com/xia-guard/FloatSnap/releases/latest";

    /// <summary>启动时调用：比已安装版本旧就提示一次，并允许用户直接去下载最新版。</summary>
    public static void WarnIfOlderThanInstalled()
    {
        Version? self = GetSelfVersion();
        Version? installed = GetInstalledVersion();
        if (self is null || installed is null) return;
        if (self >= installed) return;

        MessageBoxResult choice = MessageBox.Show(
            $"检测到本文件是旧版（{self.ToString(3)}），系统里已安装 {ProductName} {installed.ToString(3)}。\n\n" +
            "继续运行的是旧代码，可能缺少最新的修复。\n" +
            "建议直接使用已安装的版本，并删除本文件以免误点。\n\n" +
            "是否打开 Gitee 下载页获取最新版？（国内访问更快）",
            ProductName,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (choice == MessageBoxResult.Yes) OpenDownloadPage();
    }

    /// <summary>当前 exe 的版本（统一补成 4 段，避免 1.0.4 与 1.0.4.0 比较出错）。</summary>
    private static Version? GetSelfVersion()
    {
        return Normalize(Assembly.GetExecutingAssembly().GetName().Version);
    }

    /// <summary>系统里已安装的 FloatSnap 版本（读卸载信息，64/32 位视图都查）。</summary>
    private static Version? GetInstalledVersion()
    {
        foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            Version? found = FindInView(view);
            if (found is not null) return found;
        }
        return null;
    }

    private static Version? FindInView(RegistryView view)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
        using RegistryKey? uninstall = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        if (uninstall is null) return null;

        foreach (string name in uninstall.GetSubKeyNames())
        {
            using RegistryKey? app = uninstall.OpenSubKey(name);
            if (app is null) continue;
            if (!string.Equals(app.GetValue("DisplayName") as string, ProductName, StringComparison.Ordinal)) continue;

            Version? parsed = Normalize(app.GetValue("DisplayVersion") as string);
            if (parsed is not null) return parsed;
        }
        return null;
    }

    private static Version? Normalize(string? text)
    {
        return Version.TryParse(text?.Trim(), out Version? v) ? Normalize(v) : null;
    }

    private static Version? Normalize(Version? v)
    {
        if (v is null) return null;
        return new Version(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
    }

    private static void OpenDownloadPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(DownloadPage) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"打开下载页失败：{ex.Message}");
        }
    }
}
