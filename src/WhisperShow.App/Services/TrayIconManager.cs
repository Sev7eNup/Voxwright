using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using H.NotifyIcon;
using WhisperShow.App.Views;

namespace WhisperShow.App.Services;

public class TrayIconManager : IDisposable
{
    private TaskbarIcon? _trayIcon;

    public void Initialize(OverlayWindow overlayWindow, Func<SettingsWindow> settingsFactory,
        Func<HistoryWindow> historyFactory, Action shutdown)
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "WhisperShow - Speech to Text",
            Icon = CreateIcon()
        };

        var contextMenu = BuildContextMenu(overlayWindow, settingsFactory, historyFactory, shutdown);
        SetupRightClickBehavior(overlayWindow, contextMenu);
        SetupLeftClickBehavior(overlayWindow);

        _trayIcon.ForceCreate();
    }

    private System.Windows.Controls.ContextMenu BuildContextMenu(
        OverlayWindow overlayWindow,
        Func<SettingsWindow> settingsFactory,
        Func<HistoryWindow> historyFactory,
        Action shutdown)
    {
        var contextMenu = new System.Windows.Controls.ContextMenu();

        var showItem = new System.Windows.Controls.MenuItem { Header = "Show Overlay" };
        showItem.Click += (_, _) => { overlayWindow.Show(); overlayWindow.Activate(); };

        var hideItem = new System.Windows.Controls.MenuItem { Header = "Hide Overlay" };
        hideItem.Click += (_, _) => overlayWindow.Hide();

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings" };
        settingsItem.Click += (_, _) =>
        {
            var w = settingsFactory();
            w.Show();
            w.Activate();
        };

        var historyItem = new System.Windows.Controls.MenuItem { Header = "History" };
        historyItem.Click += (_, _) => historyFactory().ShowAndRefresh();

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) =>
        {
            Dispose();
            shutdown();
        };

        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(hideItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(historyItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());
        contextMenu.Items.Add(exitItem);

        return contextMenu;
    }

    private void SetupRightClickBehavior(OverlayWindow overlayWindow,
        System.Windows.Controls.ContextMenu contextMenu)
    {
        _trayIcon!.TrayRightMouseDown += (_, _) =>
        {
            // Win32 KB135788 workaround: the process must own a foreground window
            // before showing a tray context menu, otherwise it closes immediately.
            // The overlay has WS_EX_NOACTIVATE, so temporarily remove it.
            var hwnd = new WindowInteropHelper(overlayWindow).Handle;
            if (hwnd == IntPtr.Zero) return;

            int exStyle = NativeMethods.GetWindowLongW(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLongW(hwnd, NativeMethods.GWL_EXSTYLE,
                exStyle & ~NativeMethods.WS_EX_NOACTIVATE);
            NativeMethods.SetForegroundWindow(hwnd);

            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            contextMenu.IsOpen = true;

            void OnClosed(object s, RoutedEventArgs e)
            {
                contextMenu.Closed -= OnClosed;
                NativeMethods.SetWindowLongW(hwnd, NativeMethods.GWL_EXSTYLE, exStyle);
            }
            contextMenu.Closed += OnClosed;
        };
    }

    private void SetupLeftClickBehavior(OverlayWindow overlayWindow)
    {
        _trayIcon!.TrayLeftMouseDown += (_, _) =>
        {
            if (overlayWindow.IsVisible)
                overlayWindow.Hide();
            else
            {
                overlayWindow.Show();
                overlayWindow.Activate();
            }
        };
    }

    private static Icon CreateIcon()
    {
        var bitmap = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Speech bubble body (rounded rectangle)
        using var bubbleBrush = new SolidBrush(Color.FromArgb(108, 155, 242)); // #6C9BF2
        using var bubblePath = new System.Drawing.Drawing2D.GraphicsPath();
        var rect = new Rectangle(4, 2, 56, 42);
        int r = 12;
        bubblePath.AddArc(rect.X, rect.Y, r, r, 180, 90);
        bubblePath.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
        bubblePath.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
        bubblePath.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
        bubblePath.CloseFigure();
        g.FillPath(bubbleBrush, bubblePath);

        // Tail triangle (bottom-left)
        g.FillPolygon(bubbleBrush, [new System.Drawing.Point(12, 43), new System.Drawing.Point(24, 43), new System.Drawing.Point(8, 58)]);

        // Waveform bars (5 white bars, centered in bubble)
        using var barBrush = new SolidBrush(Color.White);
        int[] barHeights = [10, 22, 30, 18, 12];
        int barWidth = 6, gap = 3, startX = 13, centerY = 23;
        for (int i = 0; i < barHeights.Length; i++)
        {
            int x = startX + i * (barWidth + gap);
            int h = barHeights[i];
            g.FillRectangle(barBrush, x, centerY - h / 2, barWidth, h);
        }

        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }
}
