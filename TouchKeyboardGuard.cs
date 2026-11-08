using System;
using System.Text;

namespace FloatSnap;

/// <summary>
/// 软键盘兜底保护。
///
/// 主修复是 NoActivateBehavior（不抢焦点，从源头不让键盘被唤起）；这里是第二层保险：
/// 万一某些系统/终端组合仍然把键盘顶上来了，只在「截图前没有、截图后冒出来」的情况下
/// 把它收回去，用户自己主动调出的键盘不会被误关。
/// </summary>
internal static class TouchKeyboardGuard
{
    private const string TabTipClass = "IPTip_Main_Window";        // Win10 触摸键盘
    private const string CoreWindowClass = "Windows.UI.Core.CoreWindow"; // Win11 键盘宿主窗口类
    private const string TextInputHostExe = "TextInputHost.exe";

    /// <summary>当前屏幕上可见的软键盘窗口句柄；没有则返回 <see cref="IntPtr.Zero"/>。</summary>
    public static IntPtr FindVisible()
    {
        IntPtr tabTip = NativeMethods.FindWindow(TabTipClass, null);
        if (tabTip != IntPtr.Zero && NativeMethods.IsWindowVisible(tabTip)) return tabTip;

        IntPtr found = IntPtr.Zero;
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            var className = new StringBuilder(256);
            if (NativeMethods.GetClassName(hWnd, className, className.Capacity) == 0) return true;
            if (!string.Equals(className.ToString(), CoreWindowClass, StringComparison.Ordinal)) return true;
            if (!IsTextInputHost(hWnd)) return true;

            found = hWnd;
            return false; // 找到就停
        }, IntPtr.Zero);

        return found;
    }

    /// <summary>截图前记下键盘状态，截图后调用：只在键盘是这次新冒出来的时候收起它。</summary>
    public static void HideIfAppeared(IntPtr keyboardBeforeCapture)
    {
        if (keyboardBeforeCapture != IntPtr.Zero) return; // 之前就开着，那是用户自己要的，别动

        IntPtr keyboard = FindVisible();
        if (keyboard != IntPtr.Zero)
        {
            NativeMethods.ShowWindow(keyboard, NativeMethods.SW_HIDE);
        }
    }

    private static bool IsTextInputHost(IntPtr hWnd)
    {
        NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == 0) return false;

        IntPtr process = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process == IntPtr.Zero) return false;

        try
        {
            var path = new StringBuilder(1024);
            uint size = (uint)path.Capacity;
            if (!NativeMethods.QueryFullProcessImageName(process, 0, path, ref size)) return false;
            return path.ToString().EndsWith(TextInputHostExe, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }
}
