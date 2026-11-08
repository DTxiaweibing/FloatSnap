using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using FloatSnap;

namespace FloatSnap;

public partial class MainWindow : Window
{
    private TrayService? _tray;
    private bool _capturing;

    public MainWindow()
    {
        InitializeComponent();
        PositionAtTopRight();
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

    // ---------- 位置：屏幕右上角（距顶 180 DIP，让开窗口的 最小化/关闭 按钮）----------
    private void PositionAtTopRight()
    {
        Left = SystemParameters.WorkArea.Right - Width - 16;
        Top = SystemParameters.WorkArea.Top + 180;
    }

    // ---------- 点击：全屏截图进剪贴板 ----------
    private async void Btn_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
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
    private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        AnimateOpacity(BtnCircle, 0.85);
    }

    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        AnimateOpacity(BtnCircle, 0.55);
    }

    private void AnimateOpacity(System.Windows.Shapes.Shape target, double to)
    {
        var anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(120));
        target.BeginAnimation(OpacityProperty, anim);
    }

    // ---------- 按住空白处拖动微调位置 ----------
    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try { DragMove(); } catch { }
    }
}
