using System;
using System.Windows;
using System.Windows.Interop;

namespace FloatSnap;

/// <summary>
/// 让悬浮按钮窗口永远不抢焦点。
///
/// 原理：给窗口加上 WS_EX_NOACTIVATE（点击不激活）和 WS_EX_TOOLWINDOW（不进 Alt+Tab），
/// 并拦截 WM_MOUSEACTIVATE 返回 MA_NOACTIVATE。这样点击截图按钮时，前台窗口始终还是
/// 原来那个（终端 / 浏览器），焦点不会「终端 → FloatSnap → 终端」来回跳一次。
///
/// 这一步是修掉「在终端里点截图会弹出软键盘」的关键：Windows 的屏幕键盘（TabTip /
/// TextInputHost）会在输入焦点重新落到可输入区域时自动唤起。浏览器（Chromium）自己会
/// 压掉这种自动唤起，所以同样有光标闪烁却不会弹；终端没有这层保护，焦点一跳回来就把
/// 键盘顶上来了。不让焦点离开终端，键盘自然就不会被唤起。
/// </summary>
internal static class NoActivateBehavior
{
    /// <summary>必须在窗口 Handle 创建之后调用（SourceInitialized 或之后）。</summary>
    public static void Apply(Window window)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;

        long exStyle = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        exStyle |= NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));

        HwndSource.FromHwnd(handle)?.AddHook(Hook);
    }

    private static IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_MOUSEACTIVATE)
        {
            handled = true;
            return new IntPtr(NativeMethods.MA_NOACTIVATE);
        }
        return IntPtr.Zero;
    }
}
