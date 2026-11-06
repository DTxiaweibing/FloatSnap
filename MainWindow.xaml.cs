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

        try
        {
            // 先把悬浮按钮从画面上收起来，等窗口真正消失后再截，避免把自己截进去
            Hide();
            await Task.Delay(120);
            CaptureAndCopy();
        }
        catch (Exception ex)
        {
            _tray?.NotifyFailure(ex.Message);
        }
        finally
        {
            if (!IsVisible) Show();
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
