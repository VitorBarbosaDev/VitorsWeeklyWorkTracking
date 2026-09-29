using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public enum ScreenCorner
{
    BottomRight,
    BottomLeft,
    TopRight,
    TopLeft
}

public partial class MiniTimerWidget : Window
{
    public ScreenCorner CurrentCorner { get; private set; } = ScreenCorner.BottomRight;
    public string TargetMonitorId { get; private set; } = "Primary";
    public int TargetMonitorIndex { get; private set; } = -1;
    public bool IsPinnedOnTop { get; private set; } = true;

    public event Action? StartClicked;
    public event Action? ResumeClicked;
    public event Action? StopClicked;
    public event Action? StopBreakRequested;
    public event Action? NextSessionRequested;
    public event Action? ToggleCountModeClicked;
    public event Action? PomodoroModeToggled;
    public event Action? RestoreRequested;
    public event Action? ClosedByUser;
    public event Action? CheerRequested;
    public event Action? ArtModeToggled;
    public event Action<ScreenCorner>? CornerChanged;
    public event Action<string, int>? MonitorChanged;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private readonly System.Windows.Threading.DispatcherTimer _animTimer = new();
    private int _asciiFrameTick = 0;
    private string _currentSceneId = "cycling";
    private double _currentProgressFraction = 0.0;
    private bool _isTracking = false;
    private bool _isGoalReached = false;
    private bool _isRestPhase = false;
    private string _currentProjectName = "Work";
    private string _currentActivity = "";
    private int _focusXp = 0;
    private int _petHappiness = 100;
    private string _artMode = "graphics";
    private string? _cachedCompanionMiniLine = null;
    private DateTime? _lastCheerTime = null;
    private string? _customCheerMessage = null;

    public MiniTimerWidget()
    {
        InitializeComponent();

        Loaded += (s, e) => EnforceTopmost();
        Deactivated += (s, e) => EnforceTopmost();
        Activated += (s, e) => EnforceTopmost();

        _animTimer.Interval = TimeSpan.FromMilliseconds(120);
        _animTimer.Tick += AnimTimer_Tick;
        _animTimer.Start();

        MiniVisualCompanion.Clicked += () =>
        {
            TriggerMiniCheer();
        };
    }

    private void AnimTimer_Tick(object? sender, EventArgs e)
    {
        _asciiFrameTick++;

        // 1. Cheer message countdown and cleanup
        if (_lastCheerTime.HasValue && _customCheerMessage != null)
        {
            if ((DateTime.Now - _lastCheerTime.Value).TotalSeconds < 3.5)
            {
                MiniCompanionLine.Text = _customCheerMessage;
                MiniCompanionLine.Visibility = Visibility.Visible;
            }
            else
            {
                _customCheerMessage = null;
                if (!string.IsNullOrEmpty(_cachedCompanionMiniLine))
                {
                    MiniCompanionLine.Text = _cachedCompanionMiniLine;
                    MiniCompanionLine.Visibility = Visibility.Visible;
                }
                else
                {
                    MiniCompanionLine.Visibility = Visibility.Collapsed;
                }
            }
        }

        // 2. Animate ASCII art in mini ASCII mode
        if (IsVisible && string.Equals(_artMode, "ascii", StringComparison.OrdinalIgnoreCase))
        {
            var taskContext = string.IsNullOrWhiteSpace(_currentActivity)
                ? _currentProjectName
                : $"{_currentProjectName} • {_currentActivity}";

            var rendered = Services.AsciiArtEngine.Render(
                _currentSceneId,
                _asciiFrameTick,
                _currentProgressFraction,
                _isTracking,
                _isGoalReached,
                _isRestPhase,
                taskContext,
                _focusXp,
                _petHappiness);

            MiniAsciiText.Text = rendered.AsciiArt;
        }
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
        CornerSwitchButton.Content = corner switch
        {
            ScreenCorner.BottomRight => "📍 ↘ BR",
            ScreenCorner.BottomLeft => "📍 ↙ BL",
            ScreenCorner.TopRight => "📍 ↗ TR",
            ScreenCorner.TopLeft => "📍 ↖ TL",
            _ => "📍 ↘ BR"
        };
        CornerSwitchButton.ToolTip = "Screen Corner: " + corner + "\nClick to cycle to next corner (BR → BL → TL → TR)";
        CornerChanged?.Invoke(corner);
    }

    public void SetMonitor(string? monitorId, int monitorIndex = -1)
    {
        TargetMonitorId = string.IsNullOrWhiteSpace(monitorId) ? "Primary" : monitorId;
        TargetMonitorIndex = monitorIndex;
        UpdateMonitorButtonText();
        ApplyCornerPosition();
        MonitorChanged?.Invoke(TargetMonitorId, TargetMonitorIndex);
    }

    public void UpdateMonitorButtonText()
    {
        var monitors = VitorsWeeklyWorkTracking.Services.ScreenHelper.GetMonitors();
        var currentMon = VitorsWeeklyWorkTracking.Services.ScreenHelper.GetMonitor(TargetMonitorId, TargetMonitorIndex);

        if (monitors.Count <= 1)
        {
            MonitorSwitchButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            MonitorSwitchButton.Visibility = Visibility.Visible;
            string shortName = currentMon.ShortName;
            string displayName = currentMon.DisplayName;
            MonitorSwitchButton.Content = "🖥️ " + shortName;
            MonitorSwitchButton.ToolTip = "Display Screen: " + displayName + "\nClick to cycle to next monitor";
        }
    }

    public void ApplyCornerPosition()
    {
        var workArea = VitorsWeeklyWorkTracking.Services.ScreenHelper.GetMonitorWorkArea(TargetMonitorId, TargetMonitorIndex);
        switch (CurrentCorner)
        {
            case ScreenCorner.BottomRight:
                Left = workArea.Right - Width - 16;
                Top = workArea.Bottom - Height - 16;
                break;
            case ScreenCorner.BottomLeft:
                Left = workArea.Left + 16;
                Top = workArea.Bottom - Height - 16;
                break;
            case ScreenCorner.TopRight:
                Left = workArea.Right - Width - 16;
                Top = workArea.Top + 16;
                break;
            case ScreenCorner.TopLeft:
                Left = workArea.Left + 16;
                Top = workArea.Top + 16;
                break;
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
        int petHappiness = 100,
        string artMode = "graphics",
        string? asciiArtText = null,
        bool isPaused = false)
    {
        _currentSceneId = sceneId ?? "cycling";
        _currentProgressFraction = progressFraction;
        _isTracking = isTracking;
        _isGoalReached = isGoalReached;
        _isRestPhase = isRestPhase;
        _currentProjectName = string.IsNullOrWhiteSpace(projectName) ? "Work" : projectName;
        _currentActivity = activity ?? "";
        _focusXp = focusXp;
        _petHappiness = petHappiness;
        _artMode = artMode;
        _cachedCompanionMiniLine = companionMiniLine;

        MiniTimerText.Text = timerText;
        MiniStatusText.Text = statusText;
        MiniStatusDot.Fill = statusColor;
        MiniStatusText.Foreground = statusColor;

        MiniProjectText.Text = string.IsNullOrWhiteSpace(projectName) ? "Unassigned Project" : projectName;
        MiniActivityText.Text = string.IsNullOrWhiteSpace(activity) ? "No Activity" : activity;

        MiniTimerModeButton.Content = isCountDownMode ? "⏳ DOWN" : "⏱ UP";

        bool isAscii = string.Equals(artMode, "ascii", StringComparison.OrdinalIgnoreCase);
        if (isAscii)
        {
            MiniVisualBorder.Visibility = Visibility.Collapsed;
            MiniAsciiBorder.Visibility = Visibility.Visible;
            MiniArtModeButton.Content = "📟";
            MiniArtModeButton.ToolTip = "Art Mode: Retro ASCII • Click to switch to Graphics";

            if (!string.IsNullOrEmpty(asciiArtText))
            {
                MiniAsciiText.Text = asciiArtText;
            }
        }
        else
        {
            MiniVisualBorder.Visibility = Visibility.Visible;
            MiniAsciiBorder.Visibility = Visibility.Collapsed;
            MiniArtModeButton.Content = "🎨";
            MiniArtModeButton.ToolTip = "Art Mode: Rich Graphics • Click to switch to ASCII";

            MiniVisualCompanion.UpdateState(
                sceneId ?? "cycling",
                progressFraction,
                isTracking,
                isGoalReached,
                isRestPhase,
                string.IsNullOrWhiteSpace(projectName) ? "Work" : projectName,
                focusXp,
                petHappiness);
        }

        if (_customCheerMessage == null)
        {
            if (!string.IsNullOrEmpty(companionMiniLine))
            {
                MiniCompanionLine.Text = companionMiniLine;
                MiniCompanionLine.Visibility = Visibility.Visible;
            }
            else
            {
                MiniCompanionLine.Visibility = Visibility.Collapsed;
            }
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
            MiniResumeButton.Visibility = Visibility.Collapsed;
            MiniStopButton.Visibility = Visibility.Visible;

            if (isRestPhase)
            {
                MiniStopBreakButton.Visibility = Visibility.Visible;
                MiniNextSessionButton.Visibility = Visibility.Collapsed;
            }
            else if (statusText.Contains("FOCUS", StringComparison.OrdinalIgnoreCase) || statusText.Contains("INTERVAL", StringComparison.OrdinalIgnoreCase))
            {
                MiniStopBreakButton.Visibility = Visibility.Collapsed;
                MiniNextSessionButton.Visibility = Visibility.Visible;
            }
            else
            {
                MiniStopBreakButton.Visibility = Visibility.Collapsed;
                MiniNextSessionButton.Visibility = Visibility.Collapsed;
            }
        }
        else if (isPaused)
        {
            MiniStartButton.Visibility = Visibility.Collapsed;
            MiniResumeButton.Visibility = Visibility.Visible;
            MiniStopButton.Visibility = Visibility.Collapsed;
            MiniStopBreakButton.Visibility = Visibility.Collapsed;
            MiniNextSessionButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            MiniStartButton.Visibility = Visibility.Visible;
            MiniResumeButton.Visibility = Visibility.Collapsed;
            MiniStopButton.Visibility = Visibility.Collapsed;
            MiniStopBreakButton.Visibility = Visibility.Collapsed;
            MiniNextSessionButton.Visibility = Visibility.Collapsed;
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

    public void UpdateWeeklyAndDailyGoals(WeeklyGoalSummary weekly, DailyGoalSummary today)
    {
        if (weekly.TotalPlannedHours > 0)
        {
            MiniWeeklyGoalLabel.Text = $"🎯 Week: {weekly.TotalActualHours:0.#}h/{weekly.TotalPlannedHours:0.#}h ({weekly.FormattedCompletionPercentage})";
        }
        else
        {
            MiniWeeklyGoalLabel.Text = weekly.TotalActualHours > 0
                ? $"🎯 Week: {weekly.TotalActualHours:0.#}h worked"
                : "🎯 Week: No plan set";
        }

        MiniWeeklyProgressBar.Value = Math.Clamp(weekly.CompletionPercentage, 0.0, 100.0);

        if (today.PlannedHours > 0)
        {
            MiniTodayGoalLabel.Text = $"📅 {today.ActualHours:0.#}h/{today.PlannedHours:0.#}h ({today.FormattedCompletionPercentage})";
        }
        else
        {
            MiniTodayGoalLabel.Text = today.ActualHours > 0
                ? $"📅 {today.ActualHours:0.#}h"
                : "📅 Today: 0.0h";
        }
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
            try
            {
                DragMove();
            }
            catch
            {
                // Ignore if not in valid state for drag
            }
        }
    }

    private void MiniCompanion_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void MiniCompanion_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        TriggerMiniCheer();
    }

    public void TriggerMiniCheer()
    {
        _lastCheerTime = DateTime.Now;
        _customCheerMessage = GetCheerMessageForScene(_currentSceneId);
        _petHappiness = Math.Min(100, _petHappiness + 10);

        MiniVisualCompanion.TriggerCheer();
        MiniCompanionLine.Text = _customCheerMessage;
        MiniCompanionLine.Visibility = Visibility.Visible;

        CheerRequested?.Invoke();

        if (string.Equals(_artMode, "ascii", StringComparison.OrdinalIgnoreCase))
        {
            var taskContext = string.IsNullOrWhiteSpace(_currentActivity)
                ? _currentProjectName
                : $"{_currentProjectName} • {_currentActivity}";

            var rendered = Services.AsciiArtEngine.Render(
                _currentSceneId,
                _asciiFrameTick,
                _currentProgressFraction,
                _isTracking,
                _isGoalReached,
                _isRestPhase,
                taskContext,
                _focusXp,
                _petHappiness);
            MiniAsciiText.Text = rendered.AsciiArt;
        }
    }

    private static string GetCheerMessageForScene(string sceneId)
    {
        return sceneId switch
        {
            "cycling" => "🚴 'Ring ring! Steady speed, you can do this!' ✨",
            "cafe" => "☕ 'Mmm, warm cozy coffee vibes! Breathe & focus!' 🌿",
            "coffeejazz" => "🎷 'Latte in hand, jazz vibes on... perfect focus!' 🍂",
            "icecream" => "🍦 'Ice cream for everyone! Sweet focus progress!' 🍓",
            "metro" => "🎧 'Vibing to lo-fi beats on the Tokyo line!' 🎵",
            "rocket" => "🚀 'Thrusters boosted! Full power ahead!' 🌟",
            "cat" => "🐱 'Purrrrr! (=^･ω･^=) Kitty loves your focus!' 💖",
            "runner" => "🏃 'Keep the pace! Gold medal focus sprint!' 🥇",
            "tamagotchi" => "👾 'Yay! Pet happiness +10%! Quest forward!' 💖",
            _ => "✨ Cheering you on! Fantastic focus progress!"
        };
    }

    private void CornerSwitchButton_Click(object sender, RoutedEventArgs e)
    {
        var nextCorner = CurrentCorner switch
        {
            ScreenCorner.BottomRight => ScreenCorner.BottomLeft,
            ScreenCorner.BottomLeft => ScreenCorner.TopLeft,
            ScreenCorner.TopLeft => ScreenCorner.TopRight,
            ScreenCorner.TopRight => ScreenCorner.BottomRight,
            _ => ScreenCorner.BottomRight
        };
        SetCorner(nextCorner);
    }

    private void MonitorSwitchButton_Click(object sender, RoutedEventArgs e)
    {
        var monitors = VitorsWeeklyWorkTracking.Services.ScreenHelper.GetMonitors();
        if (monitors.Count <= 1) return;

        var currentMon = VitorsWeeklyWorkTracking.Services.ScreenHelper.GetMonitor(TargetMonitorId, TargetMonitorIndex);
        int nextIdx = (currentMon.Index + 1) % monitors.Count;
        var nextMon = monitors[nextIdx];

        SetMonitor(nextMon.DeviceName, nextMon.Index);
    }

    private void PinOnTopButton_Click(object sender, RoutedEventArgs e)
    {
        SetPinnedOnTop(!IsPinnedOnTop);
    }

    private void MiniArtModeButton_Click(object sender, RoutedEventArgs e)
    {
        ArtModeToggled?.Invoke();
    }

    private void MiniVisualBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        TriggerMiniCheer();
    }

    private void MiniAsciiBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        TriggerMiniCheer();
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

    /*public void SetPomodoroState(bool isIntervalMode)
    {
        if (MiniPomodoroButton != null)
        {
            MiniPomodoroButton.Content = isIntervalMode ? "🍅 ON" : "🍅 OFF";
            MiniPomodoroButton.Foreground = isIntervalMode
                ? (TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue)
                : (TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray);
            MiniPomodoroButton.ToolTip = isIntervalMode
                ? "Pomodoro Mode Active (Click to switch to Continuous Tracking)"
                : "Continuous Work Tracking Active (Click to switch to Pomodoro Interval Mode)";
        }
    }*/

    private void MiniTimerText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ToggleCountModeClicked?.Invoke();
    }

    private void MiniPomodoroButton_Click(object sender, RoutedEventArgs e)
    {
        PomodoroModeToggled?.Invoke();
    }

    private void MiniTimerModeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleCountModeClicked?.Invoke();
    }

    private void MiniStartButton_Click(object sender, RoutedEventArgs e)
    {
        StartClicked?.Invoke();
    }

    private void MiniResumeButton_Click(object sender, RoutedEventArgs e)
    {
        ResumeClicked?.Invoke();
    }

    private void MiniStopButton_Click(object sender, RoutedEventArgs e)
    {
        StopClicked?.Invoke();
    }

    private void MiniStopBreakButton_Click(object sender, RoutedEventArgs e)
    {
        MiniAlertBanner.Visibility = Visibility.Collapsed;
        StopBreakRequested?.Invoke();
    }

    private void MiniNextSessionButton_Click(object sender, RoutedEventArgs e)
    {
        MiniAlertBanner.Visibility = Visibility.Collapsed;
        NextSessionRequested?.Invoke();
    }
}
