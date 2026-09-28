using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace VitorsWeeklyWorkTracking;

public enum ScreenCorner
{
    BottomRight,
    BottomLeft
}

public partial class MiniTimerWidget : Window
{
    public ScreenCorner CurrentCorner { get; private set; } = ScreenCorner.BottomRight;
    public bool IsPinnedOnTop { get; private set; } = true;

    public event Action? StartClicked;
    public event Action? StopClicked;
    public event Action? ToggleCountModeClicked;
    public event Action? RestoreRequested;
    public event Action? ClosedByUser;
    public event Action? CheerRequested;
    public event Action<ScreenCorner>? CornerChanged;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public MiniTimerWidget()
    {
        InitializeComponent();

        Loaded += (s, e) => EnforceTopmost();
        Deactivated += (s, e) => EnforceTopmost();
        Activated += (s, e) => EnforceTopmost();
        MiniVisualCompanion.Clicked += () =>
        {
            MiniVisualCompanion.TriggerCheer();
            CheerRequested?.Invoke();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        EnforceTopmost();
    }

    public void SetPinnedOnTop(bool pinned)
    {
        IsPinnedOnTop = pinned;
        Topmost = pinned;
        PinOnTopButton.Content = pinned ? "📌 Pinned" : "📌 Unpinned";
        PinOnTopButton.ToolTip = pinned
            ? "Always on Top: Active (Draws above all apps) • Click to unpin"
            : "Always on Top: Inactive • Click to pin on top";

        try
        {
            var helper = new WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                var targetHwnd = pinned ? HWND_TOPMOST : HWND_NOTOPMOST;
                SetWindowPos(helper.Handle, targetHwnd, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }
        catch
        {
            // Ignore interop exceptions
        }
    }

    public void EnforceTopmost()
    {
        if (!IsPinnedOnTop) return;
        try
        {
            Topmost = true;
            var helper = new WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                SetWindowPos(helper.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }
        catch
        {
            // Ignore
        }
    }

    public void SetCorner(ScreenCorner corner)
    {
        CurrentCorner = corner;
        ApplyCornerPosition();
        CornerSwitchButton.Content = corner == ScreenCorner.BottomRight ? "📍 ↘ BR" : "📍 ↙ BL";
        CornerChanged?.Invoke(corner);
    }

    public void ApplyCornerPosition()
    {
        var workArea = SystemParameters.WorkArea;
        if (CurrentCorner == ScreenCorner.BottomRight)
        {
            Left = workArea.Right - Width - 16;
            Top = workArea.Bottom - Height - 16;
        }
        else
        {
            Left = workArea.Left + 16;
            Top = workArea.Bottom - Height - 16;
        }

        EnforceTopmost();
    }

    public void UpdateDisplay(
        bool isTracking,
        string timerText,
        string statusText,
        Brush statusColor,
        string projectName,
        string activity,
        bool isCountDownMode,
        bool hasGoal,
        double goalProgressPercentage,
        string goalStatsText,
        Brush? goalProgressBrush = null,
        string? companionMiniLine = null,
        string? alertMessage = null,
        string? sceneId = null,
        double progressFraction = 0.0,
        bool isGoalReached = false,
        bool isRestPhase = false,
        int focusXp = 0,
        int petHappiness = 100)
    {
        MiniTimerText.Text = timerText;
        MiniStatusText.Text = statusText;
        MiniStatusDot.Fill = statusColor;
        MiniStatusText.Foreground = statusColor;

        MiniProjectText.Text = string.IsNullOrWhiteSpace(projectName) ? "Unassigned Project" : projectName;
        MiniActivityText.Text = string.IsNullOrWhiteSpace(activity) ? "No Activity" : activity;

        MiniTimerModeButton.Content = isCountDownMode ? "⏳ DOWN" : "⏱ UP";

        // Update Mini Visual Animated Companion
        MiniVisualCompanion.UpdateState(
            sceneId ?? "cycling",
            progressFraction,
            isTracking,
            isGoalReached,
            isRestPhase,
            string.IsNullOrWhiteSpace(projectName) ? "Work" : projectName,
            focusXp,
            petHappiness);

        if (!string.IsNullOrEmpty(companionMiniLine))
        {
            MiniCompanionLine.Text = companionMiniLine;
            MiniCompanionLine.Visibility = Visibility.Visible;
        }
        else
        {
            MiniCompanionLine.Visibility = Visibility.Collapsed;
        }

        if (!string.IsNullOrEmpty(alertMessage))
        {
            MiniAlertText.Text = alertMessage;
            MiniAlertBanner.Visibility = Visibility.Visible;
        }
        else
        {
            MiniAlertBanner.Visibility = Visibility.Collapsed;
        }

        if (isTracking)
        {
            MiniStartButton.Visibility = Visibility.Collapsed;
            MiniStopButton.Visibility = Visibility.Visible;
        }
        else
        {
            MiniStartButton.Visibility = Visibility.Visible;
            MiniStopButton.Visibility = Visibility.Collapsed;
        }

        if (hasGoal)
        {
            MiniGoalContainer.Visibility = Visibility.Visible;
            MiniGoalProgressBar.Value = Math.Clamp(goalProgressPercentage, 0.0, 100.0);
            if (goalProgressBrush != null)
            {
                MiniGoalProgressBar.Foreground = goalProgressBrush;
            }
            MiniGoalStatsText.Text = goalStatsText;
        }
        else
        {
            MiniGoalContainer.Visibility = Visibility.Collapsed;
        }

        EnforceTopmost();
    }

    private void MiniAlertBanner_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        MiniAlertBanner.Visibility = Visibility.Collapsed;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double click restores main window
            RestoreRequested?.Invoke();
        }
        else if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CornerSwitchButton_Click(object sender, RoutedEventArgs e)
    {
        var newCorner = CurrentCorner == ScreenCorner.BottomRight ? ScreenCorner.BottomLeft : ScreenCorner.BottomRight;
        SetCorner(newCorner);
    }

    private void PinOnTopButton_Click(object sender, RoutedEventArgs e)
    {
        SetPinnedOnTop(!IsPinnedOnTop);
    }

    private void MiniVisualBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        MiniVisualCompanion.TriggerCheer();
        CheerRequested?.Invoke();
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        RestoreRequested?.Invoke();
    }

    private void CloseMiniButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        ClosedByUser?.Invoke();
    }

    private void MiniTimerText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ToggleCountModeClicked?.Invoke();
    }

    private void MiniTimerModeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleCountModeClicked?.Invoke();
    }

    private void MiniStartButton_Click(object sender, RoutedEventArgs e)
    {
        StartClicked?.Invoke();
    }

    private void MiniStopButton_Click(object sender, RoutedEventArgs e)
    {
        StopClicked?.Invoke();
    }
}
