using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking.Controls;

public partial class AsciiCompanionControl : UserControl
{
    private FocusIntervalManager? _intervalManager;
    private readonly DispatcherTimer _animTimer = new();
    private int _frameTick = 0;
    private int _petHappiness = 100;
    private DateTime? _lastCheerTime;
    private string? _customCheerMessage;

    // Cache current state
    private bool _isInitializing = true;
    private bool _isTracking = false;
    private double _currentProgressFraction = 0.0;
    private bool _isGoalReached = false;
    private string _currentDetails = string.Empty;

    public event Action<WorkRestSettings>? SettingsChanged;
    public event Action? IntervalModeToggled;
    public event Action? SkipPhaseRequested;
    public event Action? NextSessionRequested;
    public event Action<int>? ExtraRestRequested;
    public event Action? OpenSettingsRequested;

    public AsciiCompanionControl()
    {
        InitializeComponent();

        SceneComboBox.ItemsSource = ArtSceneOption.AvailableScenes;

        VisualCompanionDisplay.Clicked += () => TriggerCheer();

        _animTimer.Interval = TimeSpan.FromMilliseconds(250);
        _animTimer.Tick += (s, e) =>
        {
            _frameTick++;
            RenderCurrentFrame();
        };
        _animTimer.Start();
    }

    public void Initialize(FocusIntervalManager intervalManager)
    {
        _isInitializing = true;
        _intervalManager = intervalManager;

        // Select saved scene
        var targetSceneId = !string.IsNullOrEmpty(_intervalManager.Settings.SelectedSceneId)
            ? _intervalManager.Settings.SelectedSceneId
            : "cycling";

        var savedScene = ArtSceneOption.AvailableScenes.FirstOrDefault(s => string.Equals(s.Id, targetSceneId, StringComparison.OrdinalIgnoreCase))
            ?? ArtSceneOption.AvailableScenes[0];

        SceneComboBox.SelectedItem = savedScene;
        _intervalManager.Settings.SelectedSceneId = savedScene.Id;

        ApplyArtModeState(_intervalManager.Settings.ArtMode);
        ApplyExpandedState(_intervalManager.Settings.CompanionExpanded);
        UpdateIntervalModeUi();

        _isInitializing = false;
        RenderCurrentFrame();
    }

    public void UpdateDisplay(
        bool isTracking,
        double progressPercentage,
        bool isGoalReached,
        string currentTaskDetails)
    {
        _isTracking = isTracking;
        _currentProgressFraction = Math.Clamp(progressPercentage / 100.0, 0.0, 1.0);
        _isGoalReached = isGoalReached;
        _currentDetails = currentTaskDetails;

        UpdateIntervalModeUi();
        RenderCurrentFrame();
    }

    private void RenderCurrentFrame()
    {
        if (_intervalManager == null) return;

        var selectedScene = SceneComboBox.SelectedItem as ArtSceneOption ?? ArtSceneOption.AvailableScenes[0];
        bool isInterval = _intervalManager.Settings.IntervalModeEnabled;
        bool isRestPhase = isInterval && _intervalManager.IsRestPhase;

        double progress = isInterval ? (_intervalManager.ProgressPercentage / 100.0) : _currentProgressFraction;
        bool goalReached = isInterval ? false : _isGoalReached;

        var rendered = AsciiArtEngine.Render(
            selectedScene.Id,
            _frameTick,
            progress,
            _isTracking,
            goalReached,
            isRestPhase,
            _currentDetails,
            _intervalManager.Settings.FocusXp,
            _petHappiness);

        VisualCompanionDisplay.UpdateState(
            selectedScene.Id,
            progress,
            _isTracking,
            goalReached,
            isRestPhase,
            _currentDetails,
            _intervalManager.Settings.FocusXp,
            _petHappiness);

        AsciiArtTextBlock.Text = rendered.AsciiArt;

        if (_customCheerMessage != null && _lastCheerTime != null && (DateTime.Now - _lastCheerTime.Value).TotalSeconds < 3)
        {
            StoryNarrativeTextBlock.Text = _customCheerMessage;
        }
        else
        {
            _customCheerMessage = null;
            StoryNarrativeTextBlock.Text = rendered.StoryText;
        }

        CompanionStatusIcon.Text = selectedScene.Icon;
        CompanionStatusText.Text = rendered.BadgeText;

        if (selectedScene.Id == "tamagotchi")
        {
            PetLevelInfoTextBlock.Visibility = Visibility.Visible;
            PetLevelInfoTextBlock.Text = $"⭐ Lvl {_intervalManager.Settings.FocusLevel} • XP: {_intervalManager.Settings.FocusXp}";
        }
        else
        {
            PetLevelInfoTextBlock.Visibility = Visibility.Collapsed;
        }
    }

    public void UpdateIntervalModeUi()
    {
        if (_intervalManager == null) return;

        bool isInterval = _intervalManager.Settings.IntervalModeEnabled;
        IntervalModeToggleButton.Content = isInterval ? "🍅 Pomodoro: ON" : "🍅 Pomodoro: OFF";
        IntervalModeToggleButton.Foreground = isInterval
            ? (TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue)
            : (TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray);
        IntervalModeToggleButton.ToolTip = isInterval
            ? "Pomodoro Mode Active (Focus sprints & rest breaks) • Click to switch to Continuous Work Tracking"
            : "Continuous Work Tracking Active (No breaks/alerts) • Click to enable Pomodoro Interval Mode";

        if (isInterval && _isTracking)
        {
            IntervalCycleBadge.Visibility = Visibility.Visible;
            IntervalActionBar.Visibility = Visibility.Visible;

            int currentCycle = _intervalManager.CurrentCycle;
            int totalCycles = _intervalManager.Settings.CyclesBeforeLongBreak;
            IntervalCycleText.Text = $"🍅 Cycle {currentCycle}/{totalCycles}";

            if (_intervalManager.IsRestPhase)
            {
                IntervalPhaseLabel.Text = _intervalManager.CurrentPhase == IntervalPhase.LongBreak
                    ? "🛋️ LONG REST BREAK"
                    : "☕ SHORT REST BREAK";
                IntervalPhaseLabel.Foreground = TryFindResource("Theme.Success") as Brush ?? Brushes.LimeGreen;

                var remaining = _intervalManager.Remaining;
                IntervalPhaseTimerText.Text = $"{remaining:mm\\:ss} left • Take a deep breath & stretch!";
                ExtraRestButton.Visibility = Visibility.Visible;
                NextSessionButton.Visibility = Visibility.Collapsed;
                SkipPhaseButton.Content = "⚡ Stop Break & Start Focus";
                SkipPhaseButton.ToolTip = "Stop rest break early and start next focus session";
            }
            else
            {
                IntervalPhaseLabel.Text = "🎯 FOCUS SPRINT";
                IntervalPhaseLabel.Foreground = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;

                var remaining = _intervalManager.Remaining;
                IntervalPhaseTimerText.Text = $"{remaining:mm\\:ss} left • Session {currentCycle} of {totalCycles}";
                ExtraRestButton.Visibility = Visibility.Collapsed;
                NextSessionButton.Visibility = Visibility.Visible;
                NextSessionButton.Content = "⏩ Next Session";
                NextSessionButton.ToolTip = "Skip break and start next focus sprint";
                SkipPhaseButton.Content = "☕ Take Break";
                SkipPhaseButton.ToolTip = "End focus sprint early and take rest break";
            }
        }
        else if (isInterval && !_isTracking)
        {
            IntervalCycleBadge.Visibility = Visibility.Visible;
            IntervalActionBar.Visibility = Visibility.Visible;
            IntervalCycleText.Text = $"🍅 Interval Mode Ready";
            IntervalPhaseLabel.Text = $"🎯 { _intervalManager.Settings.FocusMinutes}m Focus / { _intervalManager.Settings.ShortBreakMinutes}m Rest";
            IntervalPhaseLabel.Foreground = TryFindResource("Theme.ForegroundMuted") as Brush ?? Brushes.Gray;
            IntervalPhaseTimerText.Text = $"Cycles before long break: { _intervalManager.Settings.CyclesBeforeLongBreak}";
            ExtraRestButton.Visibility = Visibility.Collapsed;
            SkipPhaseButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            IntervalCycleBadge.Visibility = Visibility.Collapsed;
            IntervalActionBar.Visibility = Visibility.Collapsed;
        }
    }

    private void SceneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (SceneComboBox.SelectedItem is ArtSceneOption scene && _intervalManager != null)
        {
            _intervalManager.Settings.SelectedSceneId = scene.Id;
            FocusCompanionStorage.Save(_intervalManager.Settings);
            SettingsChanged?.Invoke(_intervalManager.Settings);
            RenderCurrentFrame();
        }
    }

    private void CheerButton_Click(object sender, RoutedEventArgs e)
    {
        TriggerCheer();
    }

    private void ArtBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        TriggerCheer();
    }

    public void TriggerCheer()
    {
        if (_intervalManager == null) return;

        _petHappiness = Math.Min(100, _petHappiness + 10);
        _lastCheerTime = DateTime.Now;

        VisualCompanionDisplay.TriggerCheer();

        var selectedScene = SceneComboBox.SelectedItem as ArtSceneOption ?? ArtSceneOption.AvailableScenes[0];
        _customCheerMessage = selectedScene.Id switch
        {
            "cycling" => "🚴 'Ring ring! Steady speed, you can do this!' ✨",
            "cafe" => "☕ 'Mmm, smelling that fresh warm brew! Breathe & focus!' 🌿",
            "coffeejazz" => "🎷 'Latte in hand, lo-fi jazz on... perfect focus vibes!' 🍂",
            "icecream" => "🍦 'Ice cream for everyone! Sweet focus progress!' 🍓",
            "metro" => "🎧 'Vibing to lo-fi beats on the Tokyo line! Stay in the groove!' 🎵",
            "rocket" => "🚀 'Thrusters boosted! Full power ahead!' 🌟",
            "cat" => "🐱 'Purrrrr! (=^･ω･^=) Kitty loves your focus!' 💖",
            "runner" => "🏃 'Keep the pace! Gold medal focus sprint!' 🥇",
            "tamagotchi" => "👾 'Yay! Pet happiness +10%! Let's conquer this quest!' 💖",
            "lumberjack" => "🪓 'Chop chop! Timber down, van packing up nicely!' 🌲",
            _ => "✨ Cheering you on! Fantastic focus energy!"
        };

        SoundEffectManager.PlayCheerSound(selectedScene.Id, _intervalManager.Settings);
        RenderCurrentFrame();
    }

    private void ArtModeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_intervalManager == null) return;

        var newMode = _intervalManager.Settings.ArtMode == "ascii" ? "graphics" : "ascii";
        _intervalManager.Settings.ArtMode = newMode;
        FocusCompanionStorage.Save(_intervalManager.Settings);

        ApplyArtModeState(newMode);
        RenderCurrentFrame();
        SettingsChanged?.Invoke(_intervalManager.Settings);
    }

    private void ApplyArtModeState(string mode)
    {
        bool isAscii = string.Equals(mode, "ascii", StringComparison.OrdinalIgnoreCase);
        if (isAscii)
        {
            VisualArtBorder.Visibility = Visibility.Collapsed;
            AsciiArtBorder.Visibility = Visibility.Visible;
            ArtModeToggleButton.Content = "📟 ASCII";
            ArtModeToggleButton.ToolTip = "Current: Retro ASCII Art • Click to switch to Rich Animated Graphics";
        }
        else
        {
            VisualArtBorder.Visibility = Visibility.Visible;
            AsciiArtBorder.Visibility = Visibility.Collapsed;
            ArtModeToggleButton.Content = "🎨 Graphics";
            ArtModeToggleButton.ToolTip = "Current: Rich Animated Graphics • Click to switch to Retro ASCII Art";
        }
    }

    private void IntervalModeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_intervalManager == null) return;

        _intervalManager.Settings.IntervalModeEnabled = !_intervalManager.Settings.IntervalModeEnabled;
        FocusCompanionStorage.Save(_intervalManager.Settings);

        UpdateIntervalModeUi();
        IntervalModeToggled?.Invoke();
        SettingsChanged?.Invoke(_intervalManager.Settings);
    }

    private void IntervalSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsRequested?.Invoke();
    }

    private void ToggleExpandButton_Click(object sender, RoutedEventArgs e)
    {
        if (_intervalManager == null) return;

        bool newExpanded = !_intervalManager.Settings.CompanionExpanded;
        _intervalManager.Settings.CompanionExpanded = newExpanded;
        FocusCompanionStorage.Save(_intervalManager.Settings);

        ApplyExpandedState(newExpanded);
    }

    private void ApplyExpandedState(bool expanded)
    {
        ArtDisplayContainer.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        ToggleExpandButton.Content = expanded ? "▲" : "▼";
        ToggleExpandButton.ToolTip = expanded ? "Collapse Art Companion" : "Expand Art Companion";
    }

    private void SkipPhaseButton_Click(object sender, RoutedEventArgs e)
    {
        SkipPhaseRequested?.Invoke();
    }

    private void NextSessionButton_Click(object sender, RoutedEventArgs e)
    {
        NextSessionRequested?.Invoke();
    }

    private void ExtraRestButton_Click(object sender, RoutedEventArgs e)
    {
        ExtraRestRequested?.Invoke(5);
    }
}
