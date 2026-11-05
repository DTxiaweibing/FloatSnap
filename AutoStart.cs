using System;
using Microsoft.Win32;

namespace FloatSnap;

/// <summary>
/// 开机自启动：写 HKCU 注册表 Run 键，无需管理员权限。
/// </summary>
public static class AutoStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "FloatSnap";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is string path && path.Length > 0;
    }

    public static void Enable()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        key.SetValue(ValueName, $"\"{exePath}\"");
    }

    public static void Disable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (key.GetValue(ValueName) != null)
            key.DeleteValue(ValueName);
    }

    public static void Toggle()
    {
        if (IsEnabled()) Disable(); else Enable();
    }
}
