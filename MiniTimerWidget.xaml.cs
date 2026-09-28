using System.Windows;
using System.Windows.Input;
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

    public event Action? StartClicked;
    public event Action? StopClicked;
    public event Action? ToggleCountModeClicked;
    public event Action? RestoreRequested;
    public event Action? ClosedByUser;
    public event Action<ScreenCorner>? CornerChanged;

    public MiniTimerWidget()
    {
        InitializeComponent();
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
        Brush? goalProgressBrush = null)
    {
        MiniTimerText.Text = timerText;
        MiniStatusText.Text = statusText;
        MiniStatusDot.Fill = statusColor;
        MiniStatusText.Foreground = statusColor;

        MiniProjectText.Text = string.IsNullOrWhiteSpace(projectName) ? "Unassigned Project" : projectName;
        MiniActivityText.Text = string.IsNullOrWhiteSpace(activity) ? "No Activity" : activity;

        MiniTimerModeButton.Content = isCountDownMode ? "⏳ DOWN" : "⏱ UP";

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
