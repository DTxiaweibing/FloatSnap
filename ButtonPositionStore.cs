using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32;

namespace FloatSnap;

/// <summary>
/// 悬浮按钮的位置记忆：存 HKCU\Software\FloatSnap（免管理员，卸载也不留垃圾文件）。
/// 只存 DIP 坐标；能不能用由调用方结合当前屏幕范围判断。
/// </summary>
internal static class ButtonPositionStore
{
    private const string KeyPath = @"Software\FloatSnap";
    private const string LeftValue = "ButtonLeft";
    private const string TopValue = "ButtonTop";

    /// <summary>读取上次拖到的位置。从没记过、或记录损坏时返回 false。</summary>
    public static bool TryLoad(out double left, out double top)
    {
        left = 0;
        top = 0;

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(KeyPath);
        if (key is null) return false;

        return TryParse(key.GetValue(LeftValue) as string, out left)
            && TryParse(key.GetValue(TopValue) as string, out top);
    }

    /// <summary>记住当前位置。写失败不影响按钮继续用，只记一行调试日志。</summary>
    public static void Save(double left, double top)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath);
            key.SetValue(LeftValue, left.ToString("R", CultureInfo.InvariantCulture));
            key.SetValue(TopValue, top.ToString("R", CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"保存按钮位置失败：{ex.Message}");
        }
    }

    private static bool TryParse(string? text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value);
    }
}
