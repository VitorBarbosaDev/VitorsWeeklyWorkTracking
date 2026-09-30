using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Microsoft.Win32;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

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
    private bool _hasPlayedGoalAchievedSound = false;
    private bool _hasPlayedDailyGoalSound = false;
    private bool _hasPlayedWeeklyGoalSound = false;
    private DateTime _lastGoalCheckDate = DateTime.Today;
    private GoalOption? _selectedGoal;
    private List<GoalOption> _goalOptions = new();

    private WorkRestSettings _workRestSettings;
    private readonly FocusIntervalManager _intervalManager;

    public MainWindow()
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _workRestSettings = FocusCompanionStorage.Load();
        _miniTimerEnabled = _workRestSettings.MiniWidgetEnabled;
        _miniCorner = _workRestSettings.MiniWidgetCorner switch
        {
            "BottomLeft" => ScreenCorner.BottomLeft,
            "TopRight" => ScreenCorner.TopRight,
            "TopLeft" => ScreenCorner.TopLeft,
            _ => ScreenCorner.BottomRight
        };
        _intervalManager = new FocusIntervalManager(_workRestSettings);
        _intervalManager.AlertTriggered += IntervalManager_AlertTriggered;
        _intervalManager.AlertDismissed += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                IntervalAlertBanner.Visibility = Visibility.Collapsed;
            });
        };
        _intervalManager.PhaseChanged += IntervalManager_PhaseChanged;

        _entries = _storage.Load();
        _projects = _projectStorage.Load();
        _activities = _activityStorage.Load();

        var initToday = CalendarPlanStorage.Instance.CalculateDailySummary(DateTime.Today, _entries);
        _hasPlayedDailyGoalSound = initToday.PlannedHours > 0 && initToday.ActualHours >= initToday.PlannedHours;

        var initWeek = CalendarPlanStorage.Instance.CalculateWeeklySummary(DateTime.Today, _entries);
        _hasPlayedWeeklyGoalSound = initWeek.TotalPlannedHours > 0 && initWeek.TotalActualHours >= initWeek.TotalPlannedHours;

        InitializeThemeSelector();
        InitializeGoalOptions();
        InitializeMiniCornerSelector();
        InitializeMiniMonitorSelector();
        InitializeCompanionControl();

        FilterStartDatePicker.SelectedDate = DateTime.Today;
        FilterEndDatePicker.SelectedDate = DateTime.Today;

        RefreshProjectsDropdown();
        RefreshActivitiesDropdown();
        RefreshFilterProjectsDropdown();
        RefreshFilterActivitiesDropdown();

        _isInitializing = false;
        RefreshGrid();
        UpdateGoalProgressCard();

        CalendarPlanStorage.Instance.PlansChanged += () => Dispatcher.Invoke(UpdateGoalProgressCard);

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateStatus();
        UpdateStatus();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CheckDailyWelcome();
    }

    private void CheckDailyWelcome()
    {
        if (!_workRestSettings.WelcomePopupEnabled)
            return;

        var today = DateTime.Today;
        if (_workRestSettings.LastWelcomePopupDate.HasValue && _workRestSettings.LastWelcomePopupDate.Value.Date == today)
            return;

        _workRestSettings.LastWelcomePopupDate = today;
        FocusCompanionStorage.Save(_workRestSettings);

        var todayPlans = CalendarPlanStorage.Instance.GetPlansForDate(today);
        var welcomeDialog = new WelcomeBackDialog(_workRestSettings.UserName, todayPlans)
        {
            Owner = this
        };

        welcomeDialog.ShowDialog();

        if (welcomeDialog.RequestOpenPlanner)
        {
            OpenCalendarPlanner_Click(this, new RoutedEventArgs());
        }
    }

    private void InitializeCompanionControl()
    {
        CompanionControl.Initialize(_intervalManager);
        CompanionControl.SettingsChanged += (s) =>
        {
            _workRestSettings = s;
            UpdatePomodoroButtonStates();
            UpdateStatus();
        };
        CompanionControl.IntervalModeToggled += () =>
        {
            _workRestSettings.IntervalModeEnabled = _intervalManager.Settings.IntervalModeEnabled;
            UpdatePomodoroButtonStates();
            UpdateStatus();
        };
        CompanionControl.SkipPhaseRequested += () =>
        {
            DismissIntervalAlert();
            _intervalManager.SkipToNextPhase();
            UpdateStatus();
        };
        CompanionControl.NextSessionRequested += () =>
        {
            DismissIntervalAlert();
            _intervalManager.SkipToNextSession();
            UpdateStatus();
        };
        CompanionControl.ExtraRestRequested += (mins) =>
        {
            DismissIntervalAlert();
            _intervalManager.AddExtraBreak(mins);
            UpdateStatus();
        };
        CompanionControl.OpenSettingsRequested += () =>
        {
            OpenIntervalSettings();
        };

        CompanionControl.Visibility = _workRestSettings.CompanionVisible ? Visibility.Visible : Visibility.Collapsed;
        CompanionQuickToggleButton.Content = _workRestSettings.CompanionVisible ? "🎨 Companion: ON" : "🎨 Companion: OFF";
        UpdatePomodoroButtonStates();
    }

    private void DismissIntervalAlert()
    {
        _intervalManager.DismissAlert();
        IntervalAlertBanner.Visibility = Visibility.Collapsed;
    }

    private void IntervalManager_AlertTriggered(object? sender, IntervalAlertEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (e.NewPhase == IntervalPhase.Focus)
            {
                IntervalAlertIcon.Text = "⚡";
                IntervalAlertTitle.Text = e.Title;
                IntervalAlertMessage.Text = e.Message;
                IntervalAlertTip.Text = e.Tip;
                IntervalAlertPrimaryAction.Content = "⚡ Start Focus";
                IntervalAlertSecondaryAction.Visibility = Visibility.Collapsed;
            }
            else
            {
                IntervalAlertIcon.Text = "☕";
                IntervalAlertTitle.Text = e.Title;
                IntervalAlertMessage.Text = e.Message;
                IntervalAlertTip.Text = e.Tip;
                IntervalAlertPrimaryAction.Content = "☕ Start Rest";
                IntervalAlertSecondaryAction.Content = "⚡ Skip Break & Work";
                IntervalAlertSecondaryAction.Visibility = Visibility.Visible;
            }
            IntervalAlertBanner.Visibility = Visibility.Visible;
            UpdateStatus();
        });
    }

    private void IntervalManager_PhaseChanged(object? sender, IntervalPhase phase)
    {
        Dispatcher.Invoke(() =>
        {
            if (!_intervalManager.IsAlertPending || phase == IntervalPhase.Focus || phase == IntervalPhase.None)
            {
                IntervalAlertBanner.Visibility = Visibility.Collapsed;
            }
            UpdateStatus();
        });
    }

    private void IntervalAlertPrimaryAction_Click(object sender, RoutedEventArgs e)
    {
        DismissIntervalAlert();
        if (!_intervalManager.Settings.AutoAdvancePhases)
        {
            _intervalManager.AdvanceToNextPhase();
        }
        UpdateStatus();
    }

    private void IntervalAlertSecondaryAction_Click(object sender, RoutedEventArgs e)
    {
        DismissIntervalAlert();
        _intervalManager.SkipToNextSession();
        UpdateStatus();
    }

    private void IntervalAlertDismissButton_Click(object sender, RoutedEventArgs e)
    {
        DismissIntervalAlert();
        UpdateStatus();
    }

    private void HeroStopBreakButton_Click(object sender, RoutedEventArgs e)
    {
        DismissIntervalAlert();
        SoundEffectManager.PlayBreakEndSound(_workRestSettings);
        _intervalManager.StopBreakAndStartFocus();
        UpdateStatus();
    }

    private void HeroNextSessionButton_Click(object sender, RoutedEventArgs e)
    {
        DismissIntervalAlert();
        SoundEffectManager.PlayBreakEndSound(_workRestSettings);
        _intervalManager.SkipToNextSession();
        UpdateStatus();
    }

    private void HeroTakeBreakButton_Click(object sender, RoutedEventArgs e)
    {
        DismissIntervalAlert();
        SoundEffectManager.PlayBreakStartSound(_workRestSettings);
        _intervalManager.SkipToNextPhase();
        UpdateStatus();
    }

    private void HeroExtraRestButton_Click(object sender, RoutedEventArgs e)
    {
        DismissIntervalAlert();
        _intervalManager.AddExtraBreak(5);
        UpdateStatus();
    }

    private void PomodoroQuickToggleButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePomodoroMode();
    }

    private void HeroPomodoroButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePomodoroMode();
    }

    private void TogglePomodoroMode()
    {
        bool newMode = !_intervalManager.Settings.IntervalModeEnabled;
        SetPomodoroMode(newMode);
    }

    private void SetPomodoroMode(bool enabled)
    {
        _workRestSettings.IntervalModeEnabled = enabled;
        _intervalManager.SetIntervalMode(enabled, _currentStartTime);

        DismissIntervalAlert();

        UpdatePomodoroButtonStates();
        CompanionControl.UpdateIntervalModeUi();
        //_miniWidget?.SetPomodoroState(enabled);
        UpdateStatus();
    }

    private void UpdatePomodoroButtonStates()
    {
        bool enabled = _intervalManager.Settings.IntervalModeEnabled;

        /*if (PomodoroQuickToggleButton != null)
        {
            PomodoroQuickToggleButton.Content = enabled ? "🍅 Pomodoro: ON" : "🍅 Pomodoro: OFF";
            PomodoroQuickToggleButton.Foreground = enabled
                ? (TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue)
                : (TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray);
            PomodoroQuickToggleButton.ToolTip = enabled
                ? "Pomodoro Mode Active (Focus sprints & rest breaks) • Click to switch to Continuous Work Tracking"
                : "Continuous Work Tracking Active (No breaks/alerts) • Click to enable Pomodoro Interval Mode";
        }*/

        if (_miniWidget != null)
        {
            //_miniWidget.SetPomodoroState(enabled);
        }
    }

    private void AudioSettingsQuickButton_Click(object sender, RoutedEventArgs e)
    {
        OpenIntervalSettings();
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
            FocusCompanionStorage.Save(_workRestSettings);
            _intervalManager.UpdateSettings(_workRestSettings);
            CompanionControl.Initialize(_intervalManager);
            CompanionControl.Visibility = _workRestSettings.CompanionVisible ? Visibility.Visible : Visibility.Collapsed;
            CompanionQuickToggleButton.Content = _workRestSettings.CompanionVisible ? "🎨 Companion: ON" : "🎨 Companion: OFF";

            _miniTimerEnabled = _workRestSettings.MiniWidgetEnabled;
            _miniCorner = _workRestSettings.MiniWidgetCorner switch
            {
                "BottomLeft" => ScreenCorner.BottomLeft,
                "TopRight" => ScreenCorner.TopRight,
                "TopLeft" => ScreenCorner.TopLeft,
                _ => ScreenCorner.BottomRight
            };

            RefreshMiniCornerSelector();
            RefreshMiniMonitorSelector();

            if (_miniWidget != null)
            {
                _miniWidget.SetCorner(_miniCorner);
                _miniWidget.SetMonitor(_workRestSettings.MiniWidgetTargetMonitor, _workRestSettings.MiniWidgetMonitorIndex);
            }

            UpdatePomodoroButtonStates();
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
        /*MiniCornerComboBox.Items.Clear();
        MiniCornerComboBox.Items.Add("↘ Bottom-Right");
        MiniCornerComboBox.Items.Add("↙ Bottom-Left");
        MiniCornerComboBox.Items.Add("↗ Top-Right");
        MiniCornerComboBox.Items.Add("↖ Top-Left");
        MiniCornerComboBox.Items.Add("❌ Disabled");
        if (!_miniTimerEnabled)
        {
            MiniCornerComboBox.SelectedIndex = 4;
        }
        else
        {
            MiniCornerComboBox.SelectedIndex = _miniCorner switch
            {
                ScreenCorner.BottomRight => 0,
                ScreenCorner.BottomLeft => 1,
                ScreenCorner.TopRight => 2,
                ScreenCorner.TopLeft => 3,
                _ => 0
            };
        }*/
    }

    private void RefreshMiniCornerSelector()
    {
        _isInitializing = true;
        
        /*if (!_miniTimerEnabled)
        {
            MiniCornerComboBox.SelectedIndex = 4;
        }
        else
        {
            MiniCornerComboBox.SelectedIndex = _miniCorner switch
            {
                ScreenCorner.BottomRight => 0,
                ScreenCorner.BottomLeft => 1,
                ScreenCorner.TopRight => 2,
                ScreenCorner.TopLeft => 3,
                _ => 0
            };
        }*/
        _isInitializing = false;
    }

    private void InitializeMiniMonitorSelector()
    {
        var monitors = ScreenHelper.GetMonitors();
    //    MiniMonitorComboBox.ItemsSource = monitors;

        var targetMon = ScreenHelper.GetMonitor(_workRestSettings.MiniWidgetTargetMonitor, _workRestSettings.MiniWidgetMonitorIndex);
        var matched = monitors.FirstOrDefault(m => m.Index == targetMon.Index) ?? monitors.FirstOrDefault();
       // MiniMonitorComboBox.SelectedItem = matched;
    }

    private void RefreshMiniMonitorSelector()
    {
        _isInitializing = true;
        var monitors = ScreenHelper.GetMonitors();
        /*MiniMonitorComboBox.ItemsSource = monitors;*/

        var targetMon = ScreenHelper.GetMonitor(_workRestSettings.MiniWidgetTargetMonitor, _workRestSettings.MiniWidgetMonitorIndex);
        var matched = monitors.FirstOrDefault(m => m.Index == targetMon.Index) ?? monitors.FirstOrDefault();
        /*MiniMonitorComboBox.SelectedItem = matched;*/
        _isInitializing = false;
    }

    private void MiniMonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        /*if (MiniMonitorComboBox.SelectedItem is MonitorDisplayInfo mon)
        {
            _workRestSettings.MiniWidgetTargetMonitor = mon.DeviceName;
            _workRestSettings.MiniWidgetMonitorIndex = mon.Index;
            FocusCompanionStorage.Save(_workRestSettings);
            _miniWidget?.SetMonitor(mon.DeviceName, mon.Index);
        }*/
    }

    private void MiniCornerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        /*switch (MiniCornerComboBox.SelectedIndex)
        {
            case 0:
                _miniTimerEnabled = true;
                _miniCorner = ScreenCorner.BottomRight;
                _workRestSettings.MiniWidgetCorner = "BottomRight";
                _workRestSettings.MiniWidgetEnabled = true;
                FocusCompanionStorage.Save(_workRestSettings);
                _miniWidget?.SetCorner(ScreenCorner.BottomRight);
                break;
            case 1:
                _miniTimerEnabled = true;
                _miniCorner = ScreenCorner.BottomLeft;
                _workRestSettings.MiniWidgetCorner = "BottomLeft";
                _workRestSettings.MiniWidgetEnabled = true;
                FocusCompanionStorage.Save(_workRestSettings);
                _miniWidget?.SetCorner(ScreenCorner.BottomLeft);
                break;
            case 2:
                _miniTimerEnabled = true;
                _miniCorner = ScreenCorner.TopRight;
                _workRestSettings.MiniWidgetCorner = "TopRight";
                _workRestSettings.MiniWidgetEnabled = true;
                FocusCompanionStorage.Save(_workRestSettings);
                _miniWidget?.SetCorner(ScreenCorner.TopRight);
                break;
            case 3:
                _miniTimerEnabled = true;
                _miniCorner = ScreenCorner.TopLeft;
                _workRestSettings.MiniWidgetCorner = "TopLeft";
                _workRestSettings.MiniWidgetEnabled = true;
                FocusCompanionStorage.Save(_workRestSettings);
                _miniWidget?.SetCorner(ScreenCorner.TopLeft);
                break;
            case 4:
                _miniTimerEnabled = false;
                _workRestSettings.MiniWidgetEnabled = false;
                FocusCompanionStorage.Save(_workRestSettings);
                _miniWidget?.Hide();
                break;
        }*/
    }

    private void GoalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        if (GoalComboBox.SelectedItem is GoalOption opt)
        {
            _hasPlayedGoalAchievedSound = false;

            if (opt.DisplayName.Contains("Pomodoro") && !_intervalManager.Settings.IntervalModeEnabled)
            {
                SetPomodoroMode(true);
            }

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
            _miniWidget.SetMonitor(_workRestSettings.MiniWidgetTargetMonitor, _workRestSettings.MiniWidgetMonitorIndex);
            _miniWidget.StartClicked += () =>
            {
                if (_pendingStartTime != null && _pendingEndTime != null)
                {
                    ResumeSession();
                }
                else if (StartButton.IsEnabled && StartButton.Visibility == Visibility.Visible)
                {
                    StartButton_Click(this, new RoutedEventArgs());
                }
            };
            _miniWidget.ResumeClicked += () =>
            {
                ResumeSession();
            };
            _miniWidget.StopClicked += () =>
            {
                if (StopButton.IsEnabled && StopButton.Visibility == Visibility.Visible)
                {
                    StopButton_Click(this, new RoutedEventArgs());
                }
            };
            _miniWidget.StopBreakRequested += () =>
            {
                DismissIntervalAlert();
                SoundEffectManager.PlayBreakEndSound(_workRestSettings);
                _intervalManager.StopBreakAndStartFocus();
                UpdateStatus();
            };
            _miniWidget.TakeBreakRequested += () =>
            {
                DismissIntervalAlert();
                SoundEffectManager.PlayBreakStartSound(_workRestSettings);
                _intervalManager.SkipToNextPhase();
                UpdateStatus();
            };
            _miniWidget.NextSessionRequested += () =>
            {
                DismissIntervalAlert();
                SoundEffectManager.PlayBreakEndSound(_workRestSettings);
                _intervalManager.SkipToNextSession();
                UpdateStatus();
            };
            _miniWidget.ToggleCountModeClicked += () =>
            {
                ToggleCountMode();
            };
            _miniWidget.PomodoroModeToggled += () =>
            {
                TogglePomodoroMode();
            };
            _miniWidget.RestoreRequested += () =>
            {
                RestoreMainWindow();
            };
            _miniWidget.CheerRequested += () =>
            {
                CompanionControl.TriggerCheer();
            };
            _miniWidget.ArtModeToggled += () =>
            {
                var newMode = _intervalManager.Settings.ArtMode == "ascii" ? "graphics" : "ascii";
                _intervalManager.Settings.ArtMode = newMode;
                FocusCompanionStorage.Save(_intervalManager.Settings);
                CompanionControl.Initialize(_intervalManager);
                UpdateStatus();
            };
            _miniWidget.CornerChanged += (corner) =>
            {
                _miniCorner = corner;
                _workRestSettings.MiniWidgetCorner = corner.ToString();
                FocusCompanionStorage.Save(_workRestSettings);
                _isInitializing = true;
                /*MiniCornerComboBox.SelectedIndex = corner switch
                {
                    ScreenCorner.BottomRight => 0,
                    ScreenCorner.BottomLeft => 1,
                    ScreenCorner.TopRight => 2,
                    ScreenCorner.TopLeft => 3,
                    _ => 0
                };*/
                _isInitializing = false;
            };
            _miniWidget.MonitorChanged += (monitorId, monitorIndex) =>
            {
                _workRestSettings.MiniWidgetTargetMonitor = monitorId;
                _workRestSettings.MiniWidgetMonitorIndex = monitorIndex;
                FocusCompanionStorage.Save(_workRestSettings);
                RefreshMiniMonitorSelector();
            };
            UpdateGoalProgressCard();
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try
        {
            var helper = new WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                var source = HwndSource.FromHwnd(helper.Handle);
                source?.AddHook(WndProc);
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "MainWindow.OnSourceInitialized");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == App.WM_SHOWINSTANCE && msg != 0)
        {
            Dispatcher.InvokeAsync(RestoreMainWindow);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void RestoreMainWindow()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Show();
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_currentStartTime != null)
        {
            var selectedProject = ProjectComboBox.SelectedItem as Project;
            var projectName = selectedProject?.Name ?? ProjectComboBox.Text?.Trim();
            string taskInfo = !string.IsNullOrWhiteSpace(projectName)
                ? $" for \"{projectName}\""
                : "";

            var confirmed = ThemedMessageBox.ConfirmClose(
                this,
                $"You are currently tracking a task{taskInfo}.\n\nAre you sure you want to stop tracking and close the application?",
                title: "Tracking Active - Confirm Exit",
                exitButtonText: "Close Application",
                stayButtonText: "Keep Tracking",
                subtitle: "Active timer running");

            if (!confirmed)
            {
                e.Cancel = true;
                return;
            }
        }
        else if (_pendingStartTime != null && _pendingEndTime != null)
        {
            var confirmed = ThemedMessageBox.ConfirmClose(
                this,
                "You have an unsaved time entry recorded.\n\nAre you sure you want to discard it and close the application?",
                title: "Unsaved Session - Confirm Exit",
                exitButtonText: "Discard & Exit",
                stayButtonText: "Keep Working",
                subtitle: "Unsaved session in progress");

            if (!confirmed)
            {
                e.Cancel = true;
                return;
            }
        }

        base.OnClosing(e);

        if (!e.Cancel)
        {
            CleanupAndCloseApplication();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        CleanupAndCloseApplication();
        Application.Current?.Shutdown();
    }

    private void CleanupAndCloseApplication()
    {
        try
        {
            _timer.Stop();

            if (_miniWidget != null)
            {
                try
                {
                    _miniWidget.Close();
                }
                catch { }
                _miniWidget = null;
            }

            if (Application.Current != null)
            {
                foreach (var win in Application.Current.Windows.OfType<Window>().ToArray())
                {
                    if (win != this)
                    {
                        try
                        {
                            win.Close();
                        }
                        catch { }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "MainWindow.CleanupAndCloseApplication");
        }
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
        _hasPlayedGoalAchievedSound = false;
        _currentStartTime = DateTime.Now;
        _pendingStartTime = null;
        _pendingEndTime = null;

        _intervalManager.StartTracking(_currentStartTime.Value);
        SoundEffectManager.PlayStartSound(_workRestSettings);

        StartButton.Visibility = Visibility.Collapsed;
        ResumeButton.Visibility = Visibility.Collapsed;
        StopButton.Visibility = Visibility.Visible;
        StopButton.IsEnabled = true;
        ReviewActionBar.Visibility = Visibility.Collapsed;
        IntervalAlertBanner.Visibility = Visibility.Collapsed;

        _timer.Start();
        UpdateStatus();
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        ResumeSession();
    }

    private void ResumeSession()
    {
        if (_pendingStartTime == null || _pendingEndTime == null)
            return;

        var previousDuration = _pendingEndTime.Value - _pendingStartTime.Value;
        var now = DateTime.Now;
        _currentStartTime = now - previousDuration;
        _pendingStartTime = null;
        _pendingEndTime = null;

        _intervalManager.ResumeTracking(now);
        SoundEffectManager.PlayStartSound(_workRestSettings);

        StartButton.Visibility = Visibility.Collapsed;
        ResumeButton.Visibility = Visibility.Collapsed;
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

        var now = DateTime.Now;
        _pendingStartTime = _currentStartTime.Value;
        _pendingEndTime = now;
        _currentStartTime = null;

        _intervalManager.PauseTracking(now);
        SoundEffectManager.PlayEndSound(_workRestSettings);
        _timer.Stop();

        StopButton.Visibility = Visibility.Collapsed;
        StartButton.Visibility = Visibility.Collapsed;
        ResumeButton.Visibility = Visibility.Visible;
        ResumeButton.IsEnabled = true;

        ReviewActionBar.Visibility = Visibility.Visible;
        ResumeSessionButton.Visibility = Visibility.Visible;
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

        var postToday = CalendarPlanStorage.Instance.CalculateDailySummary(DateTime.Today, _entries);
        if (!_hasPlayedDailyGoalSound && postToday.PlannedHours > 0 && postToday.ActualHours >= postToday.PlannedHours)
        {
            _hasPlayedDailyGoalSound = true;
            SoundEffectManager.PlayDailyGoalAchievedSound(_workRestSettings);
        }

        var postWeek = CalendarPlanStorage.Instance.CalculateWeeklySummary(DateTime.Today, _entries);
        if (!_hasPlayedWeeklyGoalSound && postWeek.TotalPlannedHours > 0 && postWeek.TotalActualHours >= postWeek.TotalPlannedHours)
        {
            _hasPlayedWeeklyGoalSound = true;
            SoundEffectManager.PlayWeeklyGoalAchievedSound(_workRestSettings);
        }

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

        _intervalManager.StopTracking();

        ReviewActionBar.Visibility = Visibility.Collapsed;
        ResumeSessionButton.Visibility = Visibility.Collapsed;
        SaveEntryButton.Visibility = Visibility.Collapsed;
        DiscardButton.Visibility = Visibility.Collapsed;
        StopButton.Visibility = Visibility.Collapsed;
        ResumeButton.Visibility = Visibility.Collapsed;
        StartButton.Visibility = Visibility.Visible;
        StartButton.IsEnabled = true;

        DescriptionTextBox.Clear();
        NoteTextBox.Clear();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (DateTime.Today != _lastGoalCheckDate)
        {
            _lastGoalCheckDate = DateTime.Today;
            _hasPlayedDailyGoalSound = false;
            if (DateTime.Today.DayOfWeek == DayOfWeek.Monday)
            {
                _hasPlayedWeeklyGoalSound = false;
            }
        }

        if (!_intervalManager.IsAlertPending && IntervalAlertBanner.Visibility == Visibility.Visible)
        {
            IntervalAlertBanner.Visibility = Visibility.Collapsed;
        }

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

                IntervalHeroActionBar.Visibility = Visibility.Visible;

                if (_intervalManager.IsRestPhase)
                {
                    HeroStopBreakButton.Visibility = Visibility.Visible;
                    HeroExtraRestButton.Visibility = Visibility.Visible;
                    HeroNextSessionButton.Visibility = Visibility.Collapsed;
                    HeroTakeBreakButton.Visibility = Visibility.Collapsed;

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
                    Title = $"☕ [{displayTime}] Rest Break - Fun Work Tracker";
                    AppTaskbarItemInfo.Description = $"☕ [{displayTime}] Rest Break ({projectName})";
                    AppTaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    AppTaskbarItemInfo.ProgressValue = Math.Clamp(_intervalManager.Elapsed.TotalSeconds / Math.Max(1, _intervalManager.CurrentPhaseTargetDuration.TotalSeconds), 0.01, 1.0);
                    AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: true, goalReached: false);
                }
                else
                {
                    HeroStopBreakButton.Visibility = Visibility.Collapsed;
                    HeroExtraRestButton.Visibility = Visibility.Collapsed;
                    HeroNextSessionButton.Visibility = Visibility.Visible;
                    HeroTakeBreakButton.Visibility = Visibility.Visible;

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
                    Title = $"🔴 [{displayTime}] {projectName} - Fun Work Tracker";
                    AppTaskbarItemInfo.Description = $"{modeLabel} [{displayTime}] {projectName}{activitySuffix}";
                    AppTaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                    AppTaskbarItemInfo.ProgressValue = Math.Clamp(focusElapsed.TotalSeconds / Math.Max(1, focusTarget.TotalSeconds), 0.01, 1.0);
                    AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: true, goalReached: false);
                }
            }
            else if (hasGoal && goalDuration > TimeSpan.Zero)
            {
                IntervalHeroActionBar.Visibility = Visibility.Collapsed;
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
                    if (!_hasPlayedGoalAchievedSound)
                    {
                        _hasPlayedGoalAchievedSound = true;
                        SoundEffectManager.PlayGoalAchievedSound(_workRestSettings);
                    }
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
                Title = $"🔴 [{displayTime}] {projectName} - Fun Work Tracker";
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

                Title = $"🔴 [{displayTime}] {projectName} - Fun Work Tracker";
                AppTaskbarItemInfo.Description = $"⏱️ [{displayTime}] {projectName}{activitySuffix}";
                AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: true, goalReached: false);
            }

            TimerDisplayTextBlock.Text = displayTime;

            // Check Daily and Weekly Goals achievement during active tracking
            var todayGoalSummary = CalendarPlanStorage.Instance.CalculateDailySummary(DateTime.Today, _entries);
            var weeklyGoalSummary = CalendarPlanStorage.Instance.CalculateWeeklySummary(DateTime.Today, _entries);

            double activeTodayHours = todayGoalSummary.ActualHours + elapsed.TotalHours;
            double activeWeeklyHours = weeklyGoalSummary.TotalActualHours + elapsed.TotalHours;

            if (todayGoalSummary.PlannedHours > 0 && activeTodayHours >= todayGoalSummary.PlannedHours)
            {
                if (!_hasPlayedDailyGoalSound)
                {
                    _hasPlayedDailyGoalSound = true;
                    SoundEffectManager.PlayDailyGoalAchievedSound(_workRestSettings);
                }
            }

            if (weeklyGoalSummary.TotalPlannedHours > 0 && activeWeeklyHours >= weeklyGoalSummary.TotalPlannedHours)
            {
                if (!_hasPlayedWeeklyGoalSound)
                {
                    _hasPlayedWeeklyGoalSound = true;
                    SoundEffectManager.PlayWeeklyGoalAchievedSound(_workRestSettings);
                }
            }

            // Determine companion animation progress:
            // When Pomodoro mode is ON: animation tracks the Pomodoro focus/break interval progress.
            // When Pomodoro mode is OFF: animations play continuously, looping every 20 minutes (1200 seconds).
            double companionProgressFraction;
            double companionProgressPercentage;
            bool companionGoalReached;
            bool companionRestPhase;

            if (isIntervalMode)
            {
                companionProgressPercentage = _intervalManager.ProgressPercentage;
                companionProgressFraction = Math.Clamp(_intervalManager.ProgressPercentage / 100.0, 0.0, 1.0);
                companionGoalReached = false;
                companionRestPhase = _intervalManager.IsRestPhase;
            }
            else
            {
                const double continuousLoopSeconds = 20.0 * 60.0; // 20 minutes
                var totalElapsedSeconds = Math.Max(0.0, elapsed.TotalSeconds);
                companionProgressFraction = (totalElapsedSeconds % continuousLoopSeconds) / continuousLoopSeconds;
                companionProgressPercentage = companionProgressFraction * 100.0;
                companionGoalReached = false;
                companionRestPhase = false;
            }

            // Update Companion Control
            CompanionControl.UpdateDisplay(
                isTracking: true,
                progressPercentage: companionProgressPercentage,
                isGoalReached: companionGoalReached,
                currentTaskDetails: $"{projectName}{activitySuffix}");

            // Mini companion line calculation
            var miniScene = AsciiArtEngine.Render(
                _intervalManager.Settings.SelectedSceneId,
                0,
                companionProgressFraction,
                isTracking: true,
                isGoalReached: companionGoalReached,
                isRestPhase: companionRestPhase,
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
                    alertMessage: alertText,
                    sceneId: _intervalManager.Settings.SelectedSceneId,
                    progressFraction: companionProgressFraction,
                    isGoalReached: companionGoalReached,
                    isRestPhase: companionRestPhase,
                    focusXp: _intervalManager.Settings.FocusXp,
                    petHappiness: 100,
                    artMode: _intervalManager.Settings.ArtMode,
                    asciiArtText: miniScene.AsciiArt,
                    isIntervalMode: isIntervalMode);
            }
        }
        else if (_pendingStartTime != null && _pendingEndTime != null)
        {
            IntervalHeroActionBar.Visibility = Visibility.Collapsed;
            var elapsed = _pendingEndTime.Value - _pendingStartTime.Value;
            displayTime = $"{elapsed:hh\\:mm\\:ss}";

            TimerDisplayTextBlock.Text = displayTime;
            HeroStatusBadgeText.Text = "SESSION PAUSED";
            HeroStatusDot.Fill = warningBrush;
            HeroStatusBadgeText.Foreground = warningBrush;
            HeroStatusHeadline.Text = "Ready to Save or Resume";
            HeroStatusSubhead.Text = $"Duration: {displayTime} — Click Save Entry, Resume, or Discard.";
            GoalProgressContainer.Visibility = Visibility.Collapsed;

            StatusTextBlock.Text = "Status: Session paused — save entry, resume, or discard.";
            Title = $"⏸ [{displayTime}] Paused - Fun Work Tracker";
            AppTaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            AppTaskbarItemInfo.Description = $"⏸ [{displayTime}] Paused ({projectName})";
            AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: false, goalReached: false);

            CompanionControl.UpdateDisplay(
                isTracking: false,
                progressPercentage: 100.0,
                isGoalReached: true,
                currentTaskDetails: $"{projectName}{activitySuffix}");

            if (_miniWidget != null && _miniWidget.IsVisible)
            {
                var reviewScene = AsciiArtEngine.Render(
                    _intervalManager.Settings.SelectedSceneId,
                    0,
                    1.0,
                    isTracking: false,
                    isGoalReached: true,
                    isRestPhase: false,
                    contextDetails: $"{projectName}{activitySuffix}",
                    focusXp: _intervalManager.Settings.FocusXp);

                _miniWidget.UpdateDisplay(
                    isTracking: false,
                    timerText: displayTime,
                    statusText: "PAUSED",
                    statusColor: warningBrush,
                    projectName: projectName,
                    activity: activity,
                    isCountDownMode: _isCountDownMode,
                    hasGoal: false,
                    goalProgressPercentage: 0,
                    goalStatsText: "",
                    goalProgressBrush: null,
                    companionMiniLine: "[Session Paused - Ready to Save or Resume]",
                    alertMessage: null,
                    sceneId: _intervalManager.Settings.SelectedSceneId,
                    progressFraction: 1.0,
                    isGoalReached: true,
                    isRestPhase: false,
                    focusXp: _intervalManager.Settings.FocusXp,
                    petHappiness: 100,
                    artMode: _intervalManager.Settings.ArtMode,
                    asciiArtText: reviewScene.AsciiArt,
                    isPaused: true,
                    isIntervalMode: isIntervalMode);
            }
        }
        else
        {
            IntervalHeroActionBar.Visibility = Visibility.Collapsed;
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
            Title = "Fun Work Tracker";
            AppTaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            AppTaskbarItemInfo.Description = "Fun Work Tracker";
            AppTaskbarItemInfo.Overlay = CreateTaskbarOverlayImage(isRecording: false, goalReached: false);

            CompanionControl.UpdateDisplay(
                isTracking: false,
                progressPercentage: 0.0,
                isGoalReached: false,
                currentTaskDetails: $"{projectName}{activitySuffix}");

            if (_miniWidget != null && _miniWidget.IsVisible)
            {
                var idleScene = AsciiArtEngine.Render(
                    _intervalManager.Settings.SelectedSceneId,
                    0,
                    0.0,
                    isTracking: false,
                    isGoalReached: false,
                    isRestPhase: false,
                    contextDetails: $"{projectName}{activitySuffix}",
                    focusXp: _intervalManager.Settings.FocusXp);

                _miniWidget.UpdateDisplay(
                    isTracking: false,
                    timerText: "00:00:00",
                    statusText: isIntervalMode ? "INTERVAL" : "READY",
                    statusColor: TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray,
                    projectName: projectName,
                    activity: activity,
                    isCountDownMode: _isCountDownMode,
                    hasGoal: hasGoal || isIntervalMode,
                    goalProgressPercentage: 0,
                    goalStatsText: isIntervalMode ? $"🍅 {_intervalManager.Settings.FocusMinutes}m Focus" : (hasGoal ? $"🎯 Goal: {FormatCompact(goalDuration)}" : ""),
                    goalProgressBrush: null,
                    companionMiniLine: isIntervalMode ? $"[🍅 {_intervalManager.Settings.FocusMinutes}m Focus / {_intervalManager.Settings.ShortBreakMinutes}m Rest]" : idleScene.MiniLine,
                    alertMessage: null,
                    sceneId: _intervalManager.Settings.SelectedSceneId,
                    progressFraction: 0.0,
                    isGoalReached: false,
                    isRestPhase: false,
                    focusXp: _intervalManager.Settings.FocusXp,
                    petHappiness: 100,
                    artMode: _intervalManager.Settings.ArtMode,
                    asciiArtText: idleScene.AsciiArt,
                    isIntervalMode: isIntervalMode);
            }
        }

        UpdateGoalProgressCard();
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
        EntriesEmptyStatePanel.Visibility = _filteredEntries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        UpdateSummaryStats();
        UpdateGoalProgressCard();
    }

    private void UpdateGoalProgressCard()
    {
        var weeklySummary = CalendarPlanStorage.Instance.CalculateWeeklySummary(DateTime.Today, _entries);
        var todaySummary = CalendarPlanStorage.Instance.CalculateDailySummary(DateTime.Today, _entries);

        double activeElapsedHours = 0;
        if (_currentStartTime != null)
        {
            activeElapsedHours = (DateTime.Now - _currentStartTime.Value).TotalHours;
        }
        else if (_pendingStartTime != null && _pendingEndTime != null)
        {
            activeElapsedHours = (_pendingEndTime.Value - _pendingStartTime.Value).TotalHours;
        }

        if (activeElapsedHours > 0)
        {
            todaySummary.ActualHours += activeElapsedHours;
            weeklySummary.TotalActualHours += activeElapsedHours;
            var todayDaily = weeklySummary.DailySummaries.FirstOrDefault(d => d.Date.Date == DateTime.Today);
            if (todayDaily != null)
            {
                todayDaily.ActualHours += activeElapsedHours;
            }
        }

        if (todaySummary.ActualHours < todaySummary.PlannedHours)
        {
            _hasPlayedDailyGoalSound = false;
        }
        else if (todaySummary.PlannedHours > 0 && todaySummary.ActualHours >= todaySummary.PlannedHours)
        {
            _hasPlayedDailyGoalSound = true;
        }

        if (weeklySummary.TotalActualHours < weeklySummary.TotalPlannedHours)
        {
            _hasPlayedWeeklyGoalSound = false;
        }
        else if (weeklySummary.TotalPlannedHours > 0 && weeklySummary.TotalActualHours >= weeklySummary.TotalPlannedHours)
        {
            _hasPlayedWeeklyGoalSound = true;
        }

        // Weekly goal text
        if (weeklySummary.TotalPlannedHours > 0)
        {
            GoalSummaryHeadlineText.Text = $"{weeklySummary.TotalPlannedHours:F1}h planned • {weeklySummary.TotalActualHours:F1}h worked ({weeklySummary.FormattedCompletionPercentage}) • {weeklySummary.FormattedRemainingHours}";
        }
        else
        {
            GoalSummaryHeadlineText.Text = weeklySummary.TotalActualHours > 0 
                ? $"Worked {weeklySummary.TotalActualHours:F1}h this week (No weekly plan set)" 
                : "No weekly plan scheduled yet (Click Planner to set goals)";
        }

        GoalWeeklyProgressBar.Value = Math.Min(100, weeklySummary.CompletionPercentage);
        GoalWeeklyProgressPctText.Text = weeklySummary.FormattedCompletionPercentage;

        // Today's badge text
        if (todaySummary.PlannedHours > 0)
        {
            GoalTodayBadgeText.Text = $"📅 Today: {todaySummary.PlannedHours:F1}h plan • {todaySummary.ActualHours:F1}h act ({todaySummary.FormattedCompletionPercentage})";
        }
        else
        {
            GoalTodayBadgeText.Text = todaySummary.ActualHours > 0
                ? $"📅 Today: {todaySummary.ActualHours:F1}h worked (No goal)"
                : "📅 Today: 0.0h (No plan)";
        }

        // Mini 7-day indicators (Mon to Sun)
        var miniTexts = new[] { MiniDay0Text, MiniDay1Text, MiniDay2Text, MiniDay3Text, MiniDay4Text, MiniDay5Text, MiniDay6Text };
        var miniBorders = new[] { MiniDay0Border, MiniDay1Border, MiniDay2Border, MiniDay3Border, MiniDay4Border, MiniDay5Border, MiniDay6Border };
        var dayShortNames = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        for (int i = 0; i < 7; i++)
        {
            var daySummary = weeklySummary.DailySummaries[i];
            bool isToday = daySummary.Date.Date == DateTime.Today;

            string planStr = daySummary.PlannedHours > 0 ? $"{daySummary.PlannedHours:0.#}h" : "0h";
            string actStr = daySummary.ActualHours > 0 ? $"{daySummary.ActualHours:0.#}h" : "0h";
            miniTexts[i].Text = $"{dayShortNames[i]}: {actStr}/{planStr}";

            if (isToday)
            {
                miniBorders[i].BorderBrush = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
                miniBorders[i].BorderThickness = new Thickness(1.5);
                miniTexts[i].Foreground = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
            }
            else if (daySummary.IsGoalMet)
            {
                miniBorders[i].BorderBrush = TryFindResource("Theme.Success") as Brush ?? Brushes.LimeGreen;
                miniBorders[i].BorderThickness = new Thickness(1);
                miniTexts[i].Foreground = TryFindResource("Theme.Success") as Brush ?? Brushes.LimeGreen;
            }
            else
            {
                miniBorders[i].BorderBrush = TryFindResource("Theme.CardBorder") as Brush ?? Brushes.LightGray;
                miniBorders[i].BorderThickness = new Thickness(1);
                miniTexts[i].Foreground = TryFindResource(i < 5 ? "Theme.ForegroundMuted" : "Theme.ForegroundSubtle") as Brush ?? Brushes.Gray;
            }
        }

        // Sync with floating mini timer widget
        _miniWidget?.UpdateWeeklyAndDailyGoals(weeklySummary, todaySummary);
    }

    private void OpenCalendarPlanner_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CalendarPlannerWindow(_entries, _projects, _activities)
        {
            Owner = this
        };

        dialog.ShowDialog();
        UpdateGoalProgressCard();
        RefreshGrid();
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

    private void ViewEntryDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TimeEntry entry)
        {
            ShowTimeEntryDetails(entry);
        }
    }

    private void ViewDetailsSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesDataGrid.SelectedItem is TimeEntry selectedEntry)
        {
            ShowTimeEntryDetails(selectedEntry);
        }
        else
        {
            ThemedMessageBox.ShowInfo(this, "Please select a task from the list to view its details.", "No Task Selected");
        }
    }

    private void EntriesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EntriesDataGrid.SelectedItem is TimeEntry selectedEntry)
        {
            ShowTimeEntryDetails(selectedEntry);
        }
    }

    private void ShowTimeEntryDetails(TimeEntry entry)
    {
        var dialog = new TaskDetailsDialog(entry, _projects, _activities)
        {
            Owner = this
        };

        dialog.ShowDialog();

        if (dialog.WasModified)
        {
            _storage.Save(_entries);
            RefreshFilterProjectsDropdown(FilterProjectComboBox.SelectedItem?.ToString());
            RefreshFilterActivitiesDropdown(FilterActivityComboBox.SelectedItem?.ToString());
            RefreshGrid();
        }
    }

    private void EditEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TimeEntry entry)
        {
            EditTimeEntry(entry);
        }
    }

    private void EditSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesDataGrid.SelectedItem is TimeEntry selectedEntry)
        {
            EditTimeEntry(selectedEntry);
        }
        else
        {
            ThemedMessageBox.ShowInfo(this, "Please select a task from the list to edit.", "No Task Selected");
        }
    }

    private void EditTimeEntry(TimeEntry entry)
    {
        var dialog = new EditTaskDialog(entry, _projects, _activities)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            _storage.Save(_entries);
            RefreshFilterProjectsDropdown(FilterProjectComboBox.SelectedItem?.ToString());
            RefreshFilterActivitiesDropdown(FilterActivityComboBox.SelectedItem?.ToString());
            RefreshGrid();
        }
    }

    private void ContextMenu_ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is TimeEntry entry)
        {
            ShowTimeEntryDetails(entry);
        }
        else if (EntriesDataGrid.SelectedItem is TimeEntry selected)
        {
            ShowTimeEntryDetails(selected);
        }
    }

    private void ContextMenu_Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is TimeEntry entry)
        {
            EditTimeEntry(entry);
        }
        else if (EntriesDataGrid.SelectedItem is TimeEntry selected)
        {
            EditTimeEntry(selected);
        }
    }

    private void ContextMenu_CopyNotes_Click(object sender, RoutedEventArgs e)
    {
        var entry = (sender as MenuItem)?.DataContext as TimeEntry ?? EntriesDataGrid.SelectedItem as TimeEntry;
        if (entry != null)
        {
            string text = !string.IsNullOrWhiteSpace(entry.Note)
                ? entry.Note
                : $"Task: {entry.Description}\nProject: {entry.ProjectName}\nActivity: {entry.Activity}\nDate: {entry.StartTime:yyyy-MM-dd}\nTime: {entry.StartTime:HH:mm:ss} - {entry.EndTime:HH:mm:ss} ({entry.Duration:hh\\:mm\\:ss})";
            try
            {
                Clipboard.SetText(text);
                ThemedMessageBox.ShowSuccess(this, "Task notes / details copied to clipboard.", "Copied");
            }
            catch (Exception ex)
            {
                StoragePathHelper.LogError(ex, "MainWindow.ContextMenu_CopyNotes");
            }
        }
    }

    private void ContextMenu_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is TimeEntry entry)
        {
            DeleteTimeEntry(entry);
        }
        else if (EntriesDataGrid.SelectedItem is TimeEntry selected)
        {
            DeleteTimeEntry(selected);
        }
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
            ThemedMessageBox.ShowInfo(this, "Please select a task from the list to delete.", "No Task Selected");
        }
    }

    private void EntriesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && EntriesDataGrid.SelectedItem is TimeEntry selectedEntry)
        {
            e.Handled = true;
            DeleteTimeEntry(selectedEntry);
        }
        else if (e.Key == Key.F2 && EntriesDataGrid.SelectedItem is TimeEntry editEntry)
        {
            e.Handled = true;
            EditTimeEntry(editEntry);
        }
        else if (e.Key == Key.Enter && EntriesDataGrid.SelectedItem is TimeEntry viewEntry)
        {
            e.Handled = true;
            ShowTimeEntryDetails(viewEntry);
        }
    }

    private void DeleteTimeEntry(TimeEntry entry)
    {
        var taskDesc = !string.IsNullOrWhiteSpace(entry.Description)
            ? $"\"{entry.Description}\""
            : (!string.IsNullOrWhiteSpace(entry.ProjectName) ? $"entry for {entry.ProjectName}" : "this task");

        string detail = $"Project: {entry.ProjectName ?? "None"}\nActivity: {entry.Activity ?? "General"}\nTime: {entry.StartTime:g} ({entry.Duration:hh\\:mm\\:ss})";

        var confirmed = ThemedMessageBox.ConfirmDelete(
            this,
            $"Are you sure you want to delete {taskDesc}?",
            title: "Confirm Delete Task",
            deleteButtonText: "Delete Task",
            cancelButtonText: "Cancel",
            detailText: detail);

        if (confirmed)
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
        var defaultFileName = $"{timestamp}_funWorkTracker-time-export.csv";

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
        ThemedMessageBox.ShowSuccess(this, $"Export complete:\n{dialog.FileName}", "Export Complete");
    }
}

