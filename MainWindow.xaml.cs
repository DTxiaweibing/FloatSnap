using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FloatSnap;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace FloatSnap;

public partial class MainWindow : Window
{
    /// <summary>按住后移动超过这么多 DIP 才算拖动，否则松手就是一次点击（截图）。</summary>
    private const double DragThreshold = 4;

    private TrayService? _tray;
    private bool _capturing;

    // ---------- 拖动状态 ----------
    private bool _pressed;                       // 鼠标/手指还按着
    private bool _dragged;                       // 这次按下已经超过阈值，算拖动
    private NativeMethods.POINT _pressCursor;    // 按下时的光标位置（物理像素）
    private double _pressLeft;                   // 按下时的窗口位置（DIP）
    private double _pressTop;

    public MainWindow()
    {
        InitializeComponent();
        RestorePosition();
        AnimateOpacity(BtnCircle, 0.55);
        Loaded += (_, _) => _tray = new TrayService(this);
        Closed += (_, _) => _tray?.Dispose();
    }

    // ---------- 关键：窗口永远不激活，避免把前台焦点从终端/浏览器抢过来 ----------
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NoActivateBehavior.Apply(this);
    }

    // ---------- 位置：优先用上次拖到的位置，其次是默认的右上角 ----------
    private void RestorePosition()
    {
        if (!ButtonPositionStore.TryLoad(out double left, out double top))
        {
            PositionAtTopRight();
            return;
        }

        // 记下的位置可能已经不在任何屏幕上（比如拔了副屏），夹回可见范围
        (Left, Top) = ClampToScreens(left, top);
    }

    /// <summary>默认位置：屏幕右上角，距顶 180 DIP，让开窗口的最小化/关闭按钮。</summary>
    private void PositionAtTopRight()
    {
        Left = SystemParameters.WorkArea.Right - Width - 16;
        Top = SystemParameters.WorkArea.Top + 180;
    }

    // ---------- 按下：先只记起点，不截图 ----------
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_capturing || _pressed) return;
        if (!NativeMethods.GetCursorPos(out _pressCursor)) return;

        e.Handled = true;
        _pressed = true;
        _dragged = false;
        _pressLeft = Left;
        _pressTop = Top;
        CaptureMouse();
    }

    // ---------- 移动：超过阈值就跟着光标走 ----------
    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_pressed) return;

        // 松手消息偶尔会丢（拖动中窗口被系统重排等），这里兜底收尾，免得卡在“按下”状态
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndPress();
            return;
        }

        if (!NativeMethods.GetCursorPos(out NativeMethods.POINT now)) return;

        (double scaleX, double scaleY) = GetDpiScale();
        double dx = (now.X - _pressCursor.X) / scaleX;
        double dy = (now.Y - _pressCursor.Y) / scaleY;

        if (!_dragged)
        {
            if (Math.Abs(dx) < DragThreshold && Math.Abs(dy) < DragThreshold) return;
            _dragged = true;
        }

        (Left, Top) = ClampToScreens(_pressLeft + dx, _pressTop + dy);
    }

    // ---------- 松手：没拖动过就是一次点击（截图），拖动过就只记住位置 ----------
    private async void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_pressed) return;
        e.Handled = true;

        bool dragged = _dragged;
        EndPress();

        if (dragged)
        {
            ButtonPositionStore.Save(Left, Top);
            return;
        }

        await CaptureAndNotifyAsync();
    }

    private void EndPress()
    {
        _pressed = false;
        _dragged = false;
        if (IsMouseCaptured) ReleaseMouseCapture();
    }

    // ---------- 截图：全屏截图存图 + 进剪贴板 ----------
    private async Task CaptureAndNotifyAsync()
    {
        if (_capturing) return;
        _capturing = true;

        // 截图前软键盘是什么状态，先记一笔（兜底逻辑要用）
        IntPtr keyboardBeforeCapture = TouchKeyboardGuard.FindVisible();

        try
        {
            // 先把悬浮按钮从画面上收起来，等它真正不显示后再截，避免把自己截进去。
            // 注意：这里只隐藏内容（Visibility），不 Hide() 整个窗口——
            // 窗口一旦隐藏/显示，前台窗口就可能被重新指派，焦点也跟着跳，正是软键盘弹出来的诱因。
            Root.Visibility = Visibility.Hidden;
            await Task.Delay(120);
            CaptureAndCopy();

            // 兜底：万一这次点击还是把系统键盘顶上来了，收回去
            await Task.Delay(60);
            TouchKeyboardGuard.HideIfAppeared(keyboardBeforeCapture);
        }
        catch (Exception ex)
        {
            _tray?.NotifyFailure(ex.Message);
        }
        finally
        {
            Root.Visibility = Visibility.Visible;
            _capturing = false;
        }
    }

    private void CaptureAndCopy()
    {
        using var shot = ScreenCapture.CaptureVirtualScreen();
        string path = ScreenCapture.SaveToLibrary(shot);
        bool copied = ScreenCapture.CopyToClipboard(shot);
        _tray?.NotifySaved(System.IO.Path.GetFileName(path), shot.Width, shot.Height, copied);
    }

    // ---------- 悬停透明度变化 ----------
    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimateOpacity(BtnCircle, 0.85);
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        // 拖动时鼠标会跟着窗口跑、频繁进出边界，这里保持高亮，别闪
        if (_pressed) return;
        AnimateOpacity(BtnCircle, 0.55);
    }

    private void AnimateOpacity(System.Windows.Shapes.Shape target, double to)
    {
        var anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(120));
        target.BeginAnimation(OpacityProperty, anim);
    }

    // ---------- 单位与边界 ----------

    /// <summary>
    /// 窗口当前 DPI 缩放（物理像素 / DIP）。光标坐标是物理像素，窗口 Left/Top 是 DIP，
    /// 2 倍缩放的平板上不换算会走成两倍距离。
    /// </summary>
    private (double X, double Y) GetDpiScale()
    {
        Matrix toDevice = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        return (toDevice.M11 > 0 ? toDevice.M11 : 1, toDevice.M22 > 0 ? toDevice.M22 : 1);
    }

    /// <summary>把窗口夹进「所有屏幕合起来的虚拟桌面」，保证按钮始终完整可见、不会拖丢。</summary>
    private (double Left, double Top) ClampToScreens(double left, double top)
    {
        double minLeft = SystemParameters.VirtualScreenLeft;
        double minTop = SystemParameters.VirtualScreenTop;
        double maxLeft = Math.Max(minLeft, minLeft + SystemParameters.VirtualScreenWidth - Width);
        double maxTop = Math.Max(minTop, minTop + SystemParameters.VirtualScreenHeight - Height);
        return (Math.Clamp(left, minLeft, maxLeft), Math.Clamp(top, minTop, maxTop));
    }
}
