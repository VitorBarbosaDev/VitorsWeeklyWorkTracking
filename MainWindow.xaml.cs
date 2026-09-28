using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Microsoft.Win32;
using System.IO;

namespace VitorsWeeklyWorkTracking;

public class GoalOption
{
    public string DisplayName { get; set; } = string.Empty;
    public TimeSpan? TargetDuration { get; set; }
    public bool IsCustom { get; set; } = false;

    public override string ToString() => DisplayName;
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly TimeEntryStorage _storage = new();
    private readonly ProjectStorage _projectStorage = new();
    private readonly ActivityStorage _activityStorage = new();
    private List<TimeEntry> _entries = new();
    private List<Project> _projects = new();
    private List<ActivityItem> _activities = new();
    private readonly System.Windows.Threading.DispatcherTimer _timer = new();

    private DateTime? _currentStartTime;
    private DateTime? _pendingStartTime;
    private DateTime? _pendingEndTime;
    private List<TimeEntry> _filteredEntries = new();
    private bool _isInitializing = true;

    private MiniTimerWidget? _miniWidget;
    private ScreenCorner _miniCorner = ScreenCorner.BottomRight;
    private bool _miniTimerEnabled = true;
    private bool _isCountDownMode = false;
    private GoalOption? _selectedGoal;
    private List<GoalOption> _goalOptions = new();

    private WorkRestSettings _workRestSettings;
    private readonly FocusIntervalManager _intervalManager;

    public MainWindow()
    {
        InitializeComponent();

        _workRestSettings = FocusCompanionStorage.Load();
        _intervalManager = new FocusIntervalManager(_workRestSettings);
        _intervalManager.AlertTriggered += IntervalManager_AlertTriggered;
        _intervalManager.PhaseChanged += IntervalManager_PhaseChanged;

        _entries = _storage.Load();
        _projects = _projectStorage.Load();
        _activities = _activityStorage.Load();

        InitializeThemeSelector();
        InitializeGoalOptions();
        InitializeMiniCornerSelector();
        InitializeCompanionControl();

        FilterStartDatePicker.SelectedDate = DateTime.Today;
        FilterEndDatePicker.SelectedDate = DateTime.Today;

        RefreshProjectsDropdown();
        RefreshActivitiesDropdown();
        RefreshFilterProjectsDropdown();
        RefreshFilterActivitiesDropdown();

        _isInitializing = false;
        RefreshGrid();

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateStatus();
        UpdateStatus();
    }

    private void InitializeCompanionControl()
    {
        CompanionControl.Initialize(_intervalManager);
        CompanionControl.SettingsChanged += (s) =>
        {
            _workRestSettings = s;
            UpdateStatus();
        };
        CompanionControl.IntervalModeToggled += () =>
        {
            UpdateStatus();
        };
        CompanionControl.SkipPhaseRequested += () =>
        {
            _intervalManager.SkipToNextPhase();
            UpdateStatus();
        };
        CompanionControl.ExtraRestRequested += (mins) =>
        {
            _intervalManager.AddExtraBreak(mins);
            UpdateStatus();
        };
        CompanionControl.OpenSettingsRequested += () =>
        {
            OpenIntervalSettings();
        };

        CompanionControl.Visibility = _workRestSettings.CompanionVisible ? Visibility.Visible : Visibility.Collapsed;
        CompanionQuickToggleButton.Content = _workRestSettings.CompanionVisible ? "🎨 Companion: ON" : "🎨 Companion: OFF";
    }

    private void IntervalManager_AlertTriggered(object? sender, IntervalAlertEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            IntervalAlertIcon.Text = e.NewPhase == IntervalPhase.Focus ? "⚡" : "☕";
            IntervalAlertTitle.Text = e.Title;
            IntervalAlertMessage.Text = e.Message;
            IntervalAlertTip.Text = e.Tip;
            IntervalAlertPrimaryAction.Content = e.NewPhase == IntervalPhase.Focus ? "⚡ Start Focus" : "☕ Start Rest";
            IntervalAlertBanner.Visibility = Visibility.Visible;
            UpdateStatus();
        });
    }

    private void IntervalManager_PhaseChanged(object? sender, IntervalPhase phase)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateStatus();
        });
    }

    private void IntervalAlertPrimaryAction_Click(object sender, RoutedEventArgs e)
    {
        _intervalManager.DismissAlert();
        IntervalAlertBanner.Visibility = Visibility.Collapsed;
        if (!_intervalManager.Settings.AutoAdvancePhases)
        {
            _intervalManager.AdvanceToNextPhase();
        }
        UpdateStatus();
    }

    private void IntervalAlertDismissButton_Click(object sender, RoutedEventArgs e)
    {
        _intervalManager.DismissAlert();
        IntervalAlertBanner.Visibility = Visibility.Collapsed;
    }

    private void CompanionQuickToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _workRestSettings.CompanionVisible = !_workRestSettings.CompanionVisible;
        FocusCompanionStorage.Save(_workRestSettings);
        CompanionControl.Visibility = _workRestSettings.CompanionVisible ? Visibility.Visible : Visibility.Collapsed;
        CompanionQuickToggleButton.Content = _workRestSettings.CompanionVisible ? "🎨 Companion: ON" : "🎨 Companion: OFF";
    }

    private void OpenIntervalSettings()
    {
        var dialog = new WorkRestSettingsDialog(_workRestSettings)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            _workRestSettings = dialog.Settings;
            _intervalManager.UpdateSettings(_workRestSettings);
            CompanionControl.Initialize(_intervalManager);
            CompanionControl.Visibility = _workRestSettings.CompanionVisible ? Visibility.Visible : Visibility.Collapsed;
            CompanionQuickToggleButton.Content = _workRestSettings.CompanionVisible ? "🎨 Companion: ON" : "🎨 Companion: OFF";
            UpdateStatus();
        }
    }

    private void InitializeGoalOptions()
    {
        _goalOptions = new List<GoalOption>
        {
            new() { DisplayName = "🎯 No Goal (Count Up)", TargetDuration = null },
            new() { DisplayName = "⏱️ 15 Minutes", TargetDuration = TimeSpan.FromMinutes(15) },
            new() { DisplayName = "🍅 25 Min (Pomodoro)", TargetDuration = TimeSpan.FromMinutes(25) },
            new() { DisplayName = "⏱️ 30 Minutes", TargetDuration = TimeSpan.FromMinutes(30) },
            new() { DisplayName = "⏱️ 45 Minutes", TargetDuration = TimeSpan.FromMinutes(45) },
            new() { DisplayName = "⏱️ 1 Hour", TargetDuration = TimeSpan.FromHours(1) },
            new() { DisplayName = "⏱️ 1.5 Hours", TargetDuration = TimeSpan.FromMinutes(90) },
            new() { DisplayName = "⏱️ 2 Hours", TargetDuration = TimeSpan.FromHours(2) },
            new() { DisplayName = "⏱️ 3 Hours", TargetDuration = TimeSpan.FromHours(3) },
            new() { DisplayName = "⏱️ 4 Hours", TargetDuration = TimeSpan.FromHours(4) },
            new() { DisplayName = "✏️ Custom Minutes...", IsCustom = true }
        };

        GoalComboBox.ItemsSource = _goalOptions;
        GoalComboBox.SelectedIndex = 0;
        _selectedGoal = _goalOptions[0];
    }

    private void InitializeMiniCornerSelector()
    {
        MiniCornerComboBox.Items.Clear();
        MiniCornerComboBox.Items.Add("↘ Bottom-Right");
        MiniCornerComboBox.Items.Add("↙ Bottom-Left");
        MiniCornerComboBox.Items.Add("❌ Disabled");
        MiniCornerComboBox.SelectedIndex = 0;
    }

    private void MiniCornerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        switch (MiniCornerComboBox.SelectedIndex)
        {
            case 0:
                _miniTimerEnabled = true;
                _miniCorner = ScreenCorner.BottomRight;
                _miniWidget?.SetCorner(ScreenCorner.BottomRight);
                break;
            case 1:
                _miniTimerEnabled = true;
                _miniCorner = ScreenCorner.BottomLeft;
                _miniWidget?.SetCorner(ScreenCorner.BottomLeft);
                break;
            case 2:
                _miniTimerEnabled = false;
                _miniWidget?.Hide();
                break;
        }
    }

    private void GoalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        if (GoalComboBox.SelectedItem is GoalOption opt)
        {
            if (opt.IsCustom)
            {
                var dialog = new CustomGoalDialog(30)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true)
                {
                    var mins = dialog.SelectedMinutes;
                    var customGoal = new GoalOption
                    {
                        DisplayName = $"🎯 Custom ({mins}m)",
                        TargetDuration = TimeSpan.FromMinutes(mins)
                    };

                    _goalOptions.Insert(_goalOptions.Count - 1, customGoal);
                    GoalComboBox.ItemsSource = null;
                    GoalComboBox.ItemsSource = _goalOptions;
                    GoalComboBox.SelectedItem = customGoal;
                    _selectedGoal = customGoal;
                }
                else
                {
                    GoalComboBox.SelectedItem = _selectedGoal ?? _goalOptions[0];
                    return;
                }
            }
            else
            {
                _selectedGoal = opt;
            }

            UpdateStatus();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            if (_miniTimerEnabled)
            {
                EnsureMiniWidget();
                _miniWidget?.ApplyCornerPosition();
                _miniWidget?.Show();
                UpdateStatus();
            }
        }
        else
        {
            if (_miniWidget != null && _miniWidget.IsVisible)
            {
                _miniWidget.Hide();
            }
        }
    }

    private void MiniOverlayQuickButton_Click(object sender, RoutedEventArgs e)
    {
        EnsureMiniWidget();
        WindowState = WindowState.Minimized;
    }

    private void TimerDisplayTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ToggleCountMode();
    }

    private void TimerModeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleCountMode();
    }

    private void ToggleCountMode()
    {
        _isCountDownMode = !_isCountDownMode;
        TimerModeToggleButton.Content = _isCountDownMode ? "⏳ DOWN" : "⏱ UP";
        UpdateStatus();
    }

    private void EnsureMiniWidget()
    {
        if (_miniWidget == null)
        {
            _miniWidget = new MiniTimerWidget();
            _miniWidget.SetCorner(_miniCorner);
            _miniWidget.StartClicked += () =>
            {
                if (StartButton.IsEnabled && StartButton.Visibility == Visibility.Visible)
                {
                    StartButton_Click(this, new RoutedEventArgs());
                }
            };
            _miniWidget.StopClicked += () =>
            {
                if (StopButton.IsEnabled && StopButton.Visibility == Visibility.Visible)
                {
                    StopButton_Click(this, new RoutedEventArgs());
                }
            };
            _miniWidget.ToggleCountModeClicked += () =>
            {
                ToggleCountMode();
            };
            _miniWidget.RestoreRequested += () =>
            {
                RestoreMainWindow();
            };
            _miniWidget.CornerChanged += (corner) =>
            {
                _miniCorner = corner;
                _isInitializing = true;
                MiniCornerComboBox.SelectedIndex = corner == ScreenCorner.BottomRight ? 0 : 1;
                _isInitializing = false;
            };
        }
    }

    private void RestoreMainWindow()
    {
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    private void InitializeThemeSelector()
    {
        ThemeComboBox.ItemsSource = ThemeManager.AvailableThemes;
        ThemeComboBox.SelectedItem = ThemeManager.AvailableThemes.FirstOrDefault(t => t.Theme == ThemeManager.CurrentTheme) 
                                     ?? ThemeManager.AvailableThemes[0];
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        if (ThemeComboBox.SelectedItem is ThemeOption option)
        {
            ThemeManager.ApplyTheme(option.Theme);
            UpdateStatus();
        }
    }

    private void RefreshActivitiesDropdown(string? selectActivityName = null)
    {
        var activeActivities = _activities.Where(a => a.IsActive).OrderBy(a => a.Name).ToList();
        ActivityComboBox.ItemsSource = null;
        ActivityComboBox.ItemsSource = activeActivities;

        if (!string.IsNullOrEmpty(selectActivityName))
        {
            var match = activeActivities.FirstOrDefault(a => a.Name == selectActivityName);
            if (match != null)
            {
                ActivityComboBox.SelectedItem = match;
            }
            else
            {
                ActivityComboBox.Text = selectActivityName;
            }
        }
        else if (ActivityComboBox.SelectedItem == null && activeActivities.Count > 0)
        {
            ActivityComboBox.SelectedIndex = 0;
        }
    }

    private void RefreshProjectsDropdown(string? selectProjectName = null)
    {
        var activeProjects = _projects.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
        ProjectComboBox.ItemsSource = null;
        ProjectComboBox.ItemsSource = activeProjects;

        if (!string.IsNullOrEmpty(selectProjectName))
        {
            ProjectComboBox.SelectedItem = activeProjects.FirstOrDefault(p => p.Name == selectProjectName);
        }
        else if (ProjectComboBox.SelectedItem == null && activeProjects.Count > 0)
        {
            ProjectComboBox.SelectedIndex = 0;
        }
    }

    private void RefreshFilterProjectsDropdown(string? selectedFilter = null)
    {
        var previousSelection = selectedFilter ?? FilterProjectComboBox.SelectedItem?.ToString();

        FilterProjectComboBox.Items.Clear();
        FilterProjectComboBox.Items.Add("All Projects");

        var projectNames = _projects.Select(p => p.Name)
            .Union(_entries.Select(e => e.ProjectName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        foreach (var name in projectNames)
        {
            FilterProjectComboBox.Items.Add(name);
        }

        if (!string.IsNullOrEmpty(previousSelection) && FilterProjectComboBox.Items.Contains(previousSelection))
        {
            FilterProjectComboBox.SelectedItem = previousSelection;
        }
        else
        {
            FilterProjectComboBox.SelectedIndex = 0;
        }
    }

    private void RefreshFilterActivitiesDropdown(string? selectedFilter = null)
    {
        var previousSelection = selectedFilter ?? FilterActivityComboBox.SelectedItem?.ToString();

        FilterActivityComboBox.Items.Clear();
        FilterActivityComboBox.Items.Add("All Activities");

        var activities = _activities.Select(a => a.Name)
            .Union(_entries.Select(e => e.Activity))
            .Where(act => !string.IsNullOrWhiteSpace(act))
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        foreach (var act in activities)
        {
            FilterActivityComboBox.Items.Add(act);
        }

        if (!string.IsNullOrEmpty(previousSelection) && FilterActivityComboBox.Items.Contains(previousSelection))
        {
            FilterActivityComboBox.SelectedItem = previousSelection;
        }
        else
        {
            FilterActivityComboBox.SelectedIndex = 0;
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _currentStartTime = DateTime.Now;
        _pendingStartTime = null;
        _pendingEndTime = null;

        if (_intervalManager.Settings.IntervalModeEnabled)
        {
            _intervalManager.StartTracking(_currentStartTime.Value);
        }

        StartButton.Visibility = Visibility.Collapsed;
        StopButton.Visibility = Visibility.Visible;
        StopButton.IsEnabled = true;
        ReviewActionBar.Visibility = Visibility.Collapsed;
        IntervalAlertBanner.Visibility = Visibility.Collapsed;

        _timer.Start();
        UpdateStatus();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStartTime == null)
            return;

        _pendingStartTime = _currentStartTime.Value;
        _pendingEndTime = DateTime.Now;
        _currentStartTime = null;

        _intervalManager.StopTracking();
        _timer.Stop();

        StopButton.Visibility = Visibility.Collapsed;
        StartButton.Visibility = Visibility.Visible;
        StartButton.IsEnabled = false;

        ReviewActionBar.Visibility = Visibility.Visible;
        SaveEntryButton.Visibility = Visibility.Visible;
        DiscardButton.Visibility = Visibility.Visible;
        IntervalAlertBanner.Visibility = Visibility.Collapsed;

        UpdateStatus();
    }

    private void SaveEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingStartTime == null || _pendingEndTime == null)
            return;

        var selectedProject = ProjectComboBox.SelectedItem as Project;
        var projectName = selectedProject?.Name ?? ProjectComboBox.Text?.Trim() ?? string.Empty;
        var selectedActivity = ActivityComboBox.SelectedItem as ActivityItem;
        var activity = selectedActivity?.Name ?? ActivityComboBox.Text?.Trim() ?? ActivityComboBox.SelectedItem?.ToString() ?? string.Empty;

        var entry = new TimeEntry
        {
            ProjectName = projectName,
            Activity = activity,
            Description = DescriptionTextBox.Text,
            Note = NoteTextBox.Text,
            StartTime = _pendingStartTime.Value,
            EndTime = _pendingEndTime.Value
        };

        _entries.Add(entry);
        _storage.Save(_entries);

        RefreshFilterProjectsDropdown();
        RefreshFilterActivitiesDropdown();
        ClearPendingState();
        RefreshGrid();
    }

    private void DiscardButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingState();
    }

    private void ClearPendingState()
    {
        _pendingStartTime = null;
        _pendingEndTime = null;

        ReviewActionBar.Visibility = Visibility.Collapsed;
        SaveEntryButton.Visibility = Visibility.Collapsed;
        DiscardButton.Visibility = Visibility.Collapsed;
        StopButton.Visibility = Visibility.Collapsed;
        StartButton.Visibility = Visibility.Visible;
        StartButton.IsEnabled = true;

        DescriptionTextBox.Clear();
        NoteTextBox.Clear();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var selectedProject = ProjectComboBox.SelectedItem as Project;
        var projectName = selectedProject?.Name ?? ProjectComboBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(projectName)) projectName = "Unassigned Project";

        var selectedActivity = ActivityComboBox.SelectedItem as ActivityItem;
        var activity = selectedActivity?.Name ?? ActivityComboBox.Text?.Trim() ?? ActivityComboBox.SelectedItem?.ToString() ?? string.Empty;
        var activitySuffix = !string.IsNullOrEmpty(activity) ? $" [{activity}]" : "";

        var hasGoal = _selectedGoal?.TargetDuration != null;
        var goalDuration = _selectedGoal?.TargetDuration ?? TimeSpan.Zero;
        bool isIntervalMode = _intervalManager.Settings.IntervalModeEnabled;

        double goalProgressPercentage = 0;
        string goalStatsText = "";
        bool goalReached = false;
        string displayTime = "00:00:00";

        Brush primaryBrush = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
        Brush successBrush = TryFindResource("Theme.Success") as Brush ?? Brushes.LimeGreen;
        Brush dangerBrush = TryFindResource("Theme.Danger") as Brush ?? Brushes.Crimson;
        Brush warningBrush = TryFindResource("Theme.Warning") as Brush ?? Brushes.Goldenrod;
        Brush goalBarBrush = primaryBrush;

        if (_currentStartTime != null)
        {
            var elapsed = DateTime.Now - _currentStartTime.Value;

            if (isIntervalMode)
            {
                _intervalManager.CheckTick(DateTime.Now);

                if (_intervalManager.IsRestPhase)
                {
                    var breakRemaining = _intervalManager.Remaining;
                    var breakElapsed = _intervalManager.Elapsed;
                    var breakTarget = _intervalManager.CurrentPhaseTargetDuration;
                    goalProgressPercentage = _intervalManager.ProgressPercentage;
                    displayTime = $"{breakRemaining:mm\\:ss}";
                    goalBarBrush = successBrush;
                    goalStatsText = $"{goalProgressPercentage:F0}% • {breakElapsed:mm\\:ss} / {breakTarget:mm\\:ss} ({breakRemaining:mm\\:ss} left in break)";

                    HeroStatusBadgeText.Text = _intervalManager.CurrentPhase == IntervalPhase.LongBreak ? "🛋️ LONG REST BREAK" : "☕ SHORT REST BREAK";
                    HeroStatusDot.Fill = successBrush;
                    HeroStatusBadgeText.Foreground = successBrush;
                    HeroStatusHeadline.Text = $"Rest & Recharge: {projectName} ☕";
                    HeroStatusSubhead.Text = $"Take a breather, stretch, and hydrate! • Break {breakRemaining:mm\\:ss} left";

                    GoalProgressBar.Value = goalProgressPercentage;
                    GoalProgressBar.Foreground = goalBarBrush;
                    GoalStatsTextBlock.Text = goalStatsText;
                    GoalProgressContainer.Visibility = Visibility.Visible;

                    StatusTextBlock.Text = $"☕ REST TIME: {projectName} ({displayTime})";
                    Title = $"☕ [{displayTime}] Rest Break - Freelance Work Tracker";
                    AppTaskbarItemInfo.Description = $"☕ [{displayTime}] Rest Break ({projectName})";
                    AppTaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    AppTaskbarItemInfo.ProgressValue = Math.Clamp(_intervalManager.Elapsed.TotalSeconds / Math.Max(1, _intervalManager.CurrentPhaseTargetDuration.TotalSeconds), 0.01, 1.0);
                    AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: true, goalReached: false);
                }
                else
                {
                    var focusRemaining = _intervalManager.Remaining;
                    var focusElapsed = _intervalManager.Elapsed;
                    var focusTarget = _intervalManager.CurrentPhaseTargetDuration;
                    goalProgressPercentage = _intervalManager.ProgressPercentage;
                    displayTime = _isCountDownMode ? $"{focusRemaining:mm\\:ss}" : $"{focusElapsed:mm\\:ss}";
                    goalBarBrush = primaryBrush;
                    goalStatsText = $"{goalProgressPercentage:F0}% • {focusElapsed:mm\\:ss} / {focusTarget:mm\\:ss} ({focusRemaining:mm\\:ss} left in sprint)";

                    HeroStatusBadgeText.Text = "🎯 FOCUS SPRINT";
                    HeroStatusDot.Fill = dangerBrush;
                    HeroStatusBadgeText.Foreground = dangerBrush;
                    HeroStatusHeadline.Text = $"Focus Sprint: {projectName}";
                    HeroStatusSubhead.Text = !string.IsNullOrEmpty(activity)
                        ? $"Activity: {activity} • Session {_intervalManager.CurrentCycle}/{_intervalManager.Settings.CyclesBeforeLongBreak} ({_intervalManager.Settings.FocusMinutes}m sprint)"
                        : $"Session {_intervalManager.CurrentCycle}/{_intervalManager.Settings.CyclesBeforeLongBreak} ({_intervalManager.Settings.FocusMinutes}m sprint)";

                    GoalProgressBar.Value = goalProgressPercentage;
                    GoalProgressBar.Foreground = goalBarBrush;
                    GoalStatsTextBlock.Text = goalStatsText;
                    GoalProgressContainer.Visibility = Visibility.Visible;

                    StatusTextBlock.Text = $"🔴 FOCUS: {projectName}{activitySuffix} ({displayTime})";
                    var modeLabel = _isCountDownMode ? "⏳" : "⏱️";
                    Title = $"🔴 [{displayTime}] {projectName} - Freelance Work Tracker";
                    AppTaskbarItemInfo.Description = $"{modeLabel} [{displayTime}] {projectName}{activitySuffix}";
                    AppTaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    AppTaskbarItemInfo.ProgressValue = Math.Clamp(focusElapsed.TotalSeconds / Math.Max(1, focusTarget.TotalSeconds), 0.01, 1.0);
                    AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: true, goalReached: false);
                }
            }
            else if (hasGoal && goalDuration > TimeSpan.Zero)
            {
                var totalSeconds = elapsed.TotalSeconds;
                var goalSeconds = goalDuration.TotalSeconds;
                goalProgressPercentage = Math.Clamp((totalSeconds / goalSeconds) * 100.0, 0.0, 100.0);

                if (elapsed < goalDuration)
                {
                    var remaining = goalDuration - elapsed;
                    goalStatsText = $"{goalProgressPercentage:F0}% • {elapsed:mm\\:ss} / {goalDuration:hh\\:mm\\:ss} ({FormatCompact(remaining)} left)";
                    goalBarBrush = primaryBrush;

                    if (_isCountDownMode)
                    {
                        displayTime = $"{remaining:hh\\:mm\\:ss}";
                    }
                    else
                    {
                        displayTime = $"{elapsed:hh\\:mm\\:ss}";
                    }

                    HeroStatusBadgeText.Text = "RECORDING";
                    HeroStatusDot.Fill = dangerBrush;
                    HeroStatusBadgeText.Foreground = dangerBrush;
                    HeroStatusHeadline.Text = $"Tracking: {projectName}";
                    HeroStatusSubhead.Text = !string.IsNullOrEmpty(activity)
                        ? $"Activity: {activity} • Goal: {FormatCompact(goalDuration)}"
                        : $"Goal: {FormatCompact(goalDuration)} • Started at {_currentStartTime.Value:hh:mm:ss tt}";
                }
                else
                {
                    goalReached = true;
                    var overtime = elapsed - goalDuration;
                    goalStatsText = $"100% REACHED! 🎉 • Overtime: +{overtime:hh\\:mm\\:ss}";
                    goalBarBrush = successBrush;

                    if (_isCountDownMode)
                    {
                        displayTime = $"+{overtime:hh\\:mm\\:ss}";
                    }
                    else
                    {
                        displayTime = $"{elapsed:hh\\:mm\\:ss}";
                    }

                    HeroStatusBadgeText.Text = "🎯 GOAL REACHED";
                    HeroStatusDot.Fill = successBrush;
                    HeroStatusBadgeText.Foreground = successBrush;
                    HeroStatusHeadline.Text = $"Goal Achieved: {projectName} 🎉";
                    HeroStatusSubhead.Text = $"Completed {FormatCompact(goalDuration)} • Overtime: +{overtime:hh\\:mm\\:ss}";
                }

                GoalProgressBar.Value = goalProgressPercentage;
                GoalProgressBar.Foreground = goalBarBrush;
                GoalStatsTextBlock.Text = goalStatsText;
                GoalProgressContainer.Visibility = Visibility.Visible;

                AppTaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                AppTaskbarItemInfo.ProgressValue = Math.Clamp(totalSeconds / goalSeconds, 0.01, 1.0);

                TimerDisplayTextBlock.Text = displayTime;
                StatusTextBlock.Text = $"🔴 RECORDING: {projectName}{activitySuffix} ({displayTime})";

                var modeLabel = _isCountDownMode && hasGoal ? "⏳" : "⏱️";
                Title = $"🔴 [{displayTime}] {projectName} - Freelance Work Tracker";
                AppTaskbarItemInfo.Description = $"{modeLabel} [{displayTime}] {projectName}{activitySuffix}";
                AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: true, goalReached: goalReached);
            }
            else
            {
                GoalProgressContainer.Visibility = Visibility.Collapsed;
                displayTime = $"{elapsed:hh\\:mm\\:ss}";

                HeroStatusBadgeText.Text = "RECORDING";
                HeroStatusDot.Fill = dangerBrush;
                HeroStatusBadgeText.Foreground = dangerBrush;
                HeroStatusHeadline.Text = $"Tracking: {projectName}";
                HeroStatusSubhead.Text = !string.IsNullOrEmpty(activity)
                    ? $"Activity: {activity} • Started at {_currentStartTime.Value:hh:mm:ss tt}"
                    : $"Started at {_currentStartTime.Value:hh:mm:ss tt}";

                AppTaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;

                TimerDisplayTextBlock.Text = displayTime;
                StatusTextBlock.Text = $"🔴 RECORDING: {projectName}{activitySuffix} ({displayTime})";

                Title = $"🔴 [{displayTime}] {projectName} - Freelance Work Tracker";
                AppTaskbarItemInfo.Description = $"⏱️ [{displayTime}] {projectName}{activitySuffix}";
                AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: true, goalReached: false);
            }

            TimerDisplayTextBlock.Text = displayTime;

            // Update Companion Control
            CompanionControl.UpdateDisplay(
                isTracking: true,
                progressPercentage: goalProgressPercentage,
                isGoalReached: goalReached,
                currentTaskDetails: $"{projectName}{activitySuffix}");

            // Mini companion line calculation
            var miniScene = AsciiArtEngine.Render(
                _intervalManager.Settings.SelectedSceneId,
                0,
                isIntervalMode ? (_intervalManager.ProgressPercentage / 100.0) : (goalProgressPercentage / 100.0),
                isTracking: true,
                isGoalReached: goalReached,
                isRestPhase: isIntervalMode && _intervalManager.IsRestPhase,
                contextDetails: $"{projectName}{activitySuffix}",
                focusXp: _intervalManager.Settings.FocusXp);

            string? alertText = (_intervalManager.IsAlertPending && _intervalManager.LastAlert != null)
                ? _intervalManager.LastAlert.Title
                : null;

            if (_miniWidget != null && _miniWidget.IsVisible)
            {
                var statusColor = isIntervalMode
                    ? (_intervalManager.IsRestPhase ? successBrush : dangerBrush)
                    : (goalReached ? successBrush : dangerBrush);
                var statusLabel = isIntervalMode
                    ? (_intervalManager.IsRestPhase ? "☕ REST" : "🎯 FOCUS")
                    : (goalReached ? "GOAL" : "RECORDING");

                _miniWidget.UpdateDisplay(
                    isTracking: true,
                    timerText: displayTime,
                    statusText: statusLabel,
                    statusColor: statusColor,
                    projectName: projectName,
                    activity: activity,
                    isCountDownMode: _isCountDownMode,
                    hasGoal: hasGoal || isIntervalMode,
                    goalProgressPercentage: goalProgressPercentage,
                    goalStatsText: goalStatsText,
                    goalProgressBrush: goalBarBrush,
                    companionMiniLine: miniScene.MiniLine,
                    alertMessage: alertText);
            }
        }
        else if (_pendingStartTime != null && _pendingEndTime != null)
        {
            var elapsed = _pendingEndTime.Value - _pendingStartTime.Value;
            displayTime = $"{elapsed:hh\\:mm\\:ss}";

            TimerDisplayTextBlock.Text = displayTime;
            HeroStatusBadgeText.Text = "SESSION RECORDED";
            HeroStatusDot.Fill = warningBrush;
            HeroStatusBadgeText.Foreground = warningBrush;
            HeroStatusHeadline.Text = "Ready to Save";
            HeroStatusSubhead.Text = $"Duration: {displayTime} — Click Save Entry or Discard.";
            GoalProgressContainer.Visibility = Visibility.Collapsed;

            StatusTextBlock.Text = "Status: Review entry — save or discard.";
            Title = "Freelance Work Tracker";
            AppTaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            AppTaskbarItemInfo.Description = "Freelance Work Tracker";
            AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: false, goalReached: false);

            CompanionControl.UpdateDisplay(
                isTracking: false,
                progressPercentage: 100.0,
                isGoalReached: true,
                currentTaskDetails: $"{projectName}{activitySuffix}");

            if (_miniWidget != null && _miniWidget.IsVisible)
            {
                _miniWidget.UpdateDisplay(
                    isTracking: false,
                    timerText: displayTime,
                    statusText: "RECORDED",
                    statusColor: warningBrush,
                    projectName: projectName,
                    activity: activity,
                    isCountDownMode: _isCountDownMode,
                    hasGoal: false,
                    goalProgressPercentage: 0,
                    goalStatsText: "",
                    goalProgressBrush: null,
                    companionMiniLine: "[Session Recorded - Ready to Save]");
            }
        }
        else
        {
            TimerDisplayTextBlock.Text = "00:00:00";
            HeroStatusBadgeText.Text = isIntervalMode ? "🍅 INTERVAL READY" : "READY";
            HeroStatusDot.Fill = TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray;
            HeroStatusBadgeText.Foreground = TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray;
            HeroStatusHeadline.Text = isIntervalMode
                ? $"Ready: {_intervalManager.Settings.FocusMinutes}m Focus / {_intervalManager.Settings.ShortBreakMinutes}m Rest"
                : "Ready to Track";
            HeroStatusSubhead.Text = isIntervalMode
                ? $"Click START to begin Focus Session 1 of {_intervalManager.Settings.CyclesBeforeLongBreak}"
                : (hasGoal ? $"Goal: {FormatCompact(goalDuration)} • Select project & activity to begin" : "Select project & activity to begin");
            GoalProgressContainer.Visibility = Visibility.Collapsed;

            StatusTextBlock.Text = isIntervalMode
                ? $"Status: Ready ({_intervalManager.Settings.FocusMinutes}m Focus / {_intervalManager.Settings.ShortBreakMinutes}m Rest)"
                : "Status: Idle";
            Title = "Freelance Work Tracker";
            AppTaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            AppTaskbarItemInfo.Description = "Freelance Work Tracker";
            AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: false, goalReached: false);

            CompanionControl.UpdateDisplay(
                isTracking: false,
                progressPercentage: 0.0,
                isGoalReached: false,
                currentTaskDetails: $"{projectName}{activitySuffix}");

            if (_miniWidget != null && _miniWidget.IsVisible)
            {
                _miniWidget.UpdateDisplay(
                    isTracking: false,
                    timerText: "00:00:00",
                    statusText: isIntervalMode ? "INTERVAL" : "READY",
                    statusColor: TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray,
                    projectName: projectName,
                    activity: activity,
                    isCountDownMode: _isCountDownMode,
                    hasGoal: false,
                    goalProgressPercentage: 0,
                    goalStatsText: "",
                    goalProgressBrush: null,
                    companionMiniLine: isIntervalMode ? $"[🍅 {_intervalManager.Settings.FocusMinutes}m Focus / {_intervalManager.Settings.ShortBreakMinutes}m Rest]" : null);
            }
        }
    }

    private static string FormatCompact(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
        {
            return ts.Minutes > 0 ? $"{(int)ts.TotalHours}h {ts.Minutes}m" : $"{(int)ts.TotalHours}h";
        }
        if (ts.TotalMinutes >= 1)
        {
            return ts.Seconds > 0 ? $"{(int)ts.TotalMinutes}m {ts.Seconds}s" : $"{(int)ts.TotalMinutes}m";
        }
        return $"{(int)ts.TotalSeconds}s";
    }

    private static ImageSource CreateTaskbarOverlayImage(bool isRecording, bool goalReached)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var brush = goalReached
                ? new SolidColorBrush(Color.FromRgb(22, 163, 74))
                : (isRecording ? new SolidColorBrush(Color.FromRgb(220, 38, 38)) : new SolidColorBrush(Color.FromRgb(217, 119, 6)));
            brush.Freeze();
            dc.DrawEllipse(brush, new Pen(Brushes.White, 1.5), new Point(8, 8), 5.5, 5.5);
        }
        var rtb = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();
        return rtb;
    }

    private void RefreshGrid()
    {
        if (_isInitializing)
            return;

        var startDate = FilterStartDatePicker.SelectedDate;
        var endDate = FilterEndDatePicker.SelectedDate;

        var selectedProject = FilterProjectComboBox.SelectedItem?.ToString();
        var isAllProjects = string.IsNullOrEmpty(selectedProject) || selectedProject == "All Projects";

        var selectedActivity = FilterActivityComboBox.SelectedItem?.ToString();
        var isAllActivities = string.IsNullOrEmpty(selectedActivity) || selectedActivity == "All Activities";

        var query = _entries.AsEnumerable();

        if (startDate.HasValue)
        {
            var startDateTime = startDate.Value.Date;
            query = query.Where(e => e.StartTime >= startDateTime);
        }

        if (endDate.HasValue)
        {
            var endDateTime = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(e => e.StartTime <= endDateTime);
        }

        if (!isAllProjects)
        {
            query = query.Where(e => e.ProjectName.Equals(selectedProject, StringComparison.OrdinalIgnoreCase));
        }

        if (!isAllActivities)
        {
            query = query.Where(e => e.Activity.Equals(selectedActivity, StringComparison.OrdinalIgnoreCase));
        }

        _filteredEntries = query.OrderByDescending(e => e.StartTime).ToList();

        EntriesDataGrid.ItemsSource = null;
        EntriesDataGrid.ItemsSource = _filteredEntries;

        UpdateSummaryStats();
    }

    private void UpdateSummaryStats()
    {
        var totalTicks = _filteredEntries.Sum(e => e.Duration.Ticks);
        var totalDuration = TimeSpan.FromTicks(totalTicks);
        var sessionCount = _filteredEntries.Count;

        StatsTotalTimeTextBlock.Text = $"{(int)totalDuration.TotalHours}h {totalDuration.Minutes:D2}m {totalDuration.Seconds:D2}s";
        StatsTotalSessionsTextBlock.Text = $"{sessionCount} session{(sessionCount == 1 ? "" : "s")}";

        // Top Project
        var topProject = _filteredEntries
            .GroupBy(e => string.IsNullOrWhiteSpace(e.ProjectName) ? "(Unassigned)" : e.ProjectName)
            .OrderByDescending(g => g.Sum(x => x.Duration.Ticks))
            .FirstOrDefault();

        if (topProject != null && totalTicks > 0)
        {
            var topTicks = topProject.Sum(x => x.Duration.Ticks);
            var percentage = ((double)topTicks / totalTicks) * 100.0;
            StatsTopProjectTextBlock.Text = $"{topProject.Key} ({percentage:F0}%)";
        }
        else if (topProject != null)
        {
            StatsTopProjectTextBlock.Text = topProject.Key;
        }
        else
        {
            StatsTopProjectTextBlock.Text = "None";
        }

        // Top Activity
        var topActivity = _filteredEntries
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Activity) ? "(Unassigned)" : e.Activity)
            .OrderByDescending(g => g.Sum(x => x.Duration.Ticks))
            .FirstOrDefault();

        if (topActivity != null && totalTicks > 0)
        {
            var topActTicks = topActivity.Sum(x => x.Duration.Ticks);
            var percentage = ((double)topActTicks / totalTicks) * 100.0;
            StatsTopActivityTextBlock.Text = $"{topActivity.Key} ({percentage:F0}%)";
        }
        else if (topActivity != null)
        {
            StatsTopActivityTextBlock.Text = topActivity.Key;
        }
        else
        {
            StatsTopActivityTextBlock.Text = "None";
        }
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        RefreshGrid();
    }

    private void TodayButton_Click(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;
        FilterStartDatePicker.SelectedDate = DateTime.Today;
        FilterEndDatePicker.SelectedDate = DateTime.Today;
        _isInitializing = false;
        RefreshGrid();
    }

    private void ShowAllButton_Click(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;
        FilterStartDatePicker.SelectedDate = null;
        FilterEndDatePicker.SelectedDate = null;
        FilterProjectComboBox.SelectedIndex = 0;
        FilterActivityComboBox.SelectedIndex = 0;
        _isInitializing = false;
        RefreshGrid();
    }

    private void AddNewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var currentlySelected = (ProjectComboBox.SelectedItem as Project)?.Name;
        var currentFilterSelected = FilterProjectComboBox.SelectedItem?.ToString();
        var currentActivityFilterSelected = FilterActivityComboBox.SelectedItem?.ToString();

        var dialog = new ProjectManagementWindow(_projectStorage, _projects)
        {
            Owner = this
        };

        dialog.ShowDialog();
        RefreshProjectsDropdown(currentlySelected);
        RefreshFilterProjectsDropdown(currentFilterSelected);
        RefreshFilterActivitiesDropdown(currentActivityFilterSelected);
        RefreshGrid();
    }

    private void AddNewActivityButton_Click(object sender, RoutedEventArgs e)
    {
        var currentlySelected = (ActivityComboBox.SelectedItem as ActivityItem)?.Name ?? ActivityComboBox.Text?.Trim();
        var currentFilterSelected = FilterActivityComboBox.SelectedItem?.ToString();

        var dialog = new ActivityManagementWindow(_activityStorage, _activities)
        {
            Owner = this
        };

        dialog.ShowDialog();
        RefreshActivitiesDropdown(currentlySelected);
        RefreshFilterActivitiesDropdown(currentFilterSelected);
        RefreshGrid();
    }

    private void AnalyticsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AnalyticsWindow(_entries, _projects, _activities)
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void DeleteEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TimeEntry entry)
        {
            DeleteTimeEntry(entry);
        }
    }

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesDataGrid.SelectedItem is TimeEntry selectedEntry)
        {
            DeleteTimeEntry(selectedEntry);
        }
        else
        {
            MessageBox.Show("Please select a task from the list to delete.", "No Task Selected", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void EntriesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && EntriesDataGrid.SelectedItem is TimeEntry selectedEntry)
        {
            e.Handled = true;
            DeleteTimeEntry(selectedEntry);
        }
    }

    private void DeleteTimeEntry(TimeEntry entry)
    {
        var taskDesc = !string.IsNullOrWhiteSpace(entry.Description)
            ? $"\"{entry.Description}\""
            : (!string.IsNullOrWhiteSpace(entry.ProjectName) ? $"entry for {entry.ProjectName}" : "this task");

        var result = MessageBox.Show(
            $"Are you sure you want to delete {taskDesc} ({entry.StartTime:g} - {entry.Duration:hh\\:mm\\:ss})?",
            "Confirm Delete Task",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _entries.Remove(entry);
            _storage.Save(_entries);
            RefreshFilterProjectsDropdown(FilterProjectComboBox.SelectedItem?.ToString());
            RefreshFilterActivitiesDropdown(FilterActivityComboBox.SelectedItem?.ToString());
            RefreshGrid();
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var timestamp = DateTime.Now.ToString("dd-MM-yyyy");
        var defaultFileName = $"{timestamp}_freelance-time-export.csv";

        var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            DefaultExt = ".csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            InitialDirectory = AppContext.BaseDirectory
        };

        if (dialog.ShowDialog() != true)
            return;

        CsvExportService.Export(_filteredEntries, dialog.FileName);
        MessageBox.Show($"Export complete:\n{dialog.FileName}");
    }
}

