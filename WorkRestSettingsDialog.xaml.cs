using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class WorkRestSettingsDialog : Window
{
    public WorkRestSettings Settings { get; private set; }
    private bool _isUpdatingUi = false;
    private bool _isExplicitlySaved = false;
    private bool _suppressClosingPrompt = false;

    // Baseline snapshot fields for unsaved changes detection
    private bool _initialIntervalModeEnabled;
    private string _initialUserName = string.Empty;
    private bool _initialWelcomePopupEnabled;
    private bool _initialMilestoneRemindersEnabled;
    private string _initialMilestoneDisplayMode = "NextOnly";
    private int _initialFocusMinutes;
    private int _initialShortBreakMinutes;
    private int _initialLongBreakMinutes;
    private int _initialCyclesBeforeLongBreak;
    private bool _initialSoundAlertEnabled;
    private int _initialSoundVolume;
    private int _initialGoalMilestoneSoundVolume;
    private bool _initialSoundOnStartEnabled;
    private bool _initialSoundOnEndEnabled;
    private bool _initialSoundOnGoalAchievedEnabled;
    private bool _initialSoundOnDailyGoalAchievedEnabled;
    private bool _initialSoundOnWeeklyGoalAchievedEnabled;
    private bool _initialSoundOnBreakStartEnabled;
    private bool _initialSoundOnBreakEndEnabled;
    private bool _initialSoundOnCheerEnabled;
    private HashSet<string> _initialSoundVariations = new();
    private bool _initialAutoAdvancePhases;
    private bool _initialCompanionVisible;
    private string _initialRewardIcon = string.Empty;
    private string _initialRewardColor = string.Empty;
    private bool _initialGoalNotificationsEnabled;
    private bool _initialDailyGoalNotificationEnabled;
    private bool _initialWeeklyGoalNotificationEnabled;
    private bool _initialProjectGoalNotificationEnabled;
    private string _initialTargetMonitor = string.Empty;
    private int _initialMonitorIndex;
    private string _initialCorner = "BottomRight";
    private bool _initialMiniWidgetEnabled;
    private bool _initialStreakEnabled;
    private HashSet<DayOfWeek> _initialStreakExcludedDays = new();

    /// <summary>
    /// Optional override for unsaved changes confirmation prompt (useful for unit tests).
    /// </summary>
    public Func<MessageBoxResult>? PromptUnsavedChangesHandler { get; set; }

    private readonly List<IntervalPreset> _presets = new()
    {
        new()
        {
            Name = "🍅 Classic Pomodoro (25/5)",
            Description = "Classic Pomodoro cycle: 25 minutes of high focus followed by 5 minutes of rest.",
            FocusMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        },
        new()
        {
            Name = "⚡ Deep Focus (50/10)",
            Description = "Long focus blocks: 50 minutes of deep work followed by 10 minutes of restorative break.",
            FocusMinutes = 50,
            ShortBreakMinutes = 10,
            LongBreakMinutes = 25,
            CyclesBeforeLongBreak = 3
        },
        new()
        {
            Name = "📚 Study With Me (45/15)",
            Description = "Relaxing YouTube-style Study With Me rhythm: 45 minutes study + 15 minutes coffee break.",
            FocusMinutes = 45,
            ShortBreakMinutes = 15,
            LongBreakMinutes = 30,
            CyclesBeforeLongBreak = 3
        },
        new()
        {
            Name = "🚀 Fast Sprint (20/5)",
            Description = "Quick bursts: 20 minutes sprint + 5 minutes quick stretch.",
            FocusMinutes = 20,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        },
        new()
        {
            Name = "⚙️ Custom Intervals",
            Description = "Configure your own personalized work and rest durations below.",
            FocusMinutes = 30,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        }
    };

    public WorkRestSettingsDialog(WorkRestSettings currentSettings)
    {
        _isUpdatingUi = true;
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);
        Settings = CloneSettings(currentSettings);

        InitializeForm();
    }

    private static WorkRestSettings CloneSettings(WorkRestSettings currentSettings)
    {
        return new WorkRestSettings
        {
            IntervalModeEnabled = currentSettings.IntervalModeEnabled,
            FocusMinutes = currentSettings.FocusMinutes,
            ShortBreakMinutes = currentSettings.ShortBreakMinutes,
            LongBreakMinutes = currentSettings.LongBreakMinutes,
            CyclesBeforeLongBreak = currentSettings.CyclesBeforeLongBreak,
            SoundAlertEnabled = currentSettings.SoundAlertEnabled,
            SoundVolume = currentSettings.SoundVolume,
            SoundOnStartEnabled = currentSettings.SoundOnStartEnabled,
            SoundOnEndEnabled = currentSettings.SoundOnEndEnabled,
            SoundOnGoalAchievedEnabled = currentSettings.SoundOnGoalAchievedEnabled,
            SoundOnDailyGoalAchievedEnabled = currentSettings.SoundOnDailyGoalAchievedEnabled,
            SoundOnWeeklyGoalAchievedEnabled = currentSettings.SoundOnWeeklyGoalAchievedEnabled,
            SoundOnBreakStartEnabled = currentSettings.SoundOnBreakStartEnabled,
            SoundOnBreakEndEnabled = currentSettings.SoundOnBreakEndEnabled,
            SoundOnCheerEnabled = currentSettings.SoundOnCheerEnabled,
            EnabledSoundVariations = currentSettings.EnabledSoundVariations != null
                ? new List<string>(currentSettings.EnabledSoundVariations)
                : new List<string>(),
            AutoAdvancePhases = currentSettings.AutoAdvancePhases,
            SelectedSceneId = currentSettings.SelectedSceneId,
            ArtMode = currentSettings.ArtMode,
            CompanionVisible = currentSettings.CompanionVisible,
            CompanionExpanded = currentSettings.CompanionExpanded,
            MiniWidgetCorner = currentSettings.MiniWidgetCorner,
            MiniWidgetTargetMonitor = currentSettings.MiniWidgetTargetMonitor,
            MiniWidgetMonitorIndex = currentSettings.MiniWidgetMonitorIndex,
            MiniWidgetEnabled = currentSettings.MiniWidgetEnabled,
            UserName = currentSettings.UserName,
            WelcomePopupEnabled = currentSettings.WelcomePopupEnabled,
            LastWelcomePopupDate = currentSettings.LastWelcomePopupDate,
            ForceWelcomePopupOnNextLaunch = currentSettings.ForceWelcomePopupOnNextLaunch,
            FocusXp = currentSettings.FocusXp,
            FocusStreak = currentSettings.FocusStreak,
            TotalSessionsCompleted = currentSettings.TotalSessionsCompleted,
            CompletionRewardIcon = currentSettings.CompletionRewardIcon,
            CompletionRewardColor = currentSettings.CompletionRewardColor,
            GoalMilestoneSoundVolume = currentSettings.GoalMilestoneSoundVolume,
            GoalNotificationsEnabled = currentSettings.GoalNotificationsEnabled,
            DailyGoalNotificationEnabled = currentSettings.DailyGoalNotificationEnabled,
            WeeklyGoalNotificationEnabled = currentSettings.WeeklyGoalNotificationEnabled,
            ProjectGoalNotificationEnabled = currentSettings.ProjectGoalNotificationEnabled,
            MilestoneRemindersEnabled = currentSettings.MilestoneRemindersEnabled,
            MilestoneDisplayMode = currentSettings.MilestoneDisplayMode
        };
    }

    private void InitializeForm()
    {
        _isUpdatingUi = true;
        try
        {
            PresetComboBox.ItemsSource = _presets;

            UserNameTextBox.Text = Settings.UserName ?? string.Empty;
            WelcomePopupEnabledCheckBox.IsChecked = Settings.WelcomePopupEnabled;
            UpdateForceWelcomeButtonState();

            // Reward Icon, Color & Goal Notification preferences
            RewardIconComboBox.ItemsSource = RewardIconHelper.AvailableIcons;
            var selectedIconOpt = RewardIconHelper.AvailableIcons.FirstOrDefault(i => i.Key.Equals(Settings.CompletionRewardIcon, StringComparison.OrdinalIgnoreCase))
                ?? RewardIconHelper.AvailableIcons[0];
            RewardIconComboBox.SelectedItem = selectedIconOpt;

            RewardColorComboBox.ItemsSource = RewardColorHelper.AvailableColors;
            var selectedColorOpt = RewardColorHelper.AvailableColors.FirstOrDefault(c => c.Key.Equals(Settings.CompletionRewardColor, StringComparison.OrdinalIgnoreCase))
                ?? RewardColorHelper.AvailableColors[0];
            RewardColorComboBox.SelectedItem = selectedColorOpt;

            UpdateRewardPreview();

            GoalNotificationsEnabledCheckBox.IsChecked = Settings.GoalNotificationsEnabled;
            DailyGoalNotificationCheckBox.IsChecked = Settings.DailyGoalNotificationEnabled;
            WeeklyGoalNotificationCheckBox.IsChecked = Settings.WeeklyGoalNotificationEnabled;
            ProjectGoalNotificationCheckBox.IsChecked = Settings.ProjectGoalNotificationEnabled;
            if (GranularNotificationsPanel != null)
            {
                GranularNotificationsPanel.IsEnabled = Settings.GoalNotificationsEnabled;
            }

            // Project Milestone preferences
            if (MilestoneRemindersEnabledCheckBox != null)
            {
                MilestoneRemindersEnabledCheckBox.IsChecked = Settings.MilestoneRemindersEnabled;
            }
            if (MilestoneQueueModeComboBox != null)
            {
                MilestoneQueueModeComboBox.SelectedIndex = (Settings.MilestoneDisplayMode == "AllUpcoming") ? 1 : 0;
            }

            IntervalModeEnabledCheckBox.IsChecked = Settings.IntervalModeEnabled;

            FocusSlider.Value = Settings.FocusMinutes;
            ShortBreakSlider.Value = Settings.ShortBreakMinutes;
            LongBreakSlider.Value = Settings.LongBreakMinutes;
            CyclesSlider.Value = Settings.CyclesBeforeLongBreak;

            SelectMatchingPreset();
            UpdateLabels();

            // Audio Settings
            SoundAlertCheckBox.IsChecked = Settings.SoundAlertEnabled;
            VolumeSlider.Value = Math.Clamp(Settings.SoundVolume, 0, 100);
            VolumeValueText.Text = $"{Settings.SoundVolume}%";

            if (GoalMilestoneVolumeSlider != null)
            {
                GoalMilestoneVolumeSlider.Value = Math.Clamp(Settings.GoalMilestoneSoundVolume, 0, 100);
            }
            if (GoalMilestoneVolumeValueText != null)
            {
                GoalMilestoneVolumeValueText.Text = $"{Settings.GoalMilestoneSoundVolume}%";
            }
            if (AudioTabGoalVolumeSlider != null)
            {
                AudioTabGoalVolumeSlider.Value = Math.Clamp(Settings.GoalMilestoneSoundVolume, 0, 100);
            }
            if (AudioTabGoalVolumeValueText != null)
            {
                AudioTabGoalVolumeValueText.Text = $"{Settings.GoalMilestoneSoundVolume}%";
            }

            SoundOnStartCheckBox.IsChecked = Settings.SoundOnStartEnabled;
            SoundOnEndCheckBox.IsChecked = Settings.SoundOnEndEnabled;
            SoundOnGoalCheckBox.IsChecked = Settings.SoundOnGoalAchievedEnabled;
            SoundOnDailyGoalCheckBox.IsChecked = Settings.SoundOnDailyGoalAchievedEnabled;
            SoundOnWeeklyGoalCheckBox.IsChecked = Settings.SoundOnWeeklyGoalAchievedEnabled;
            SoundOnBreakStartCheckBox.IsChecked = Settings.SoundOnBreakStartEnabled;
            SoundOnBreakEndCheckBox.IsChecked = Settings.SoundOnBreakEndEnabled;
            SoundOnCheerCheckBox.IsChecked = Settings.SoundOnCheerEnabled;

            // Initialize Variation CheckBoxes
            SetVariationCheckState(Var_start_ascending, "start_ascending");
            SetVariationCheckState(Var_start_pop, "start_pop");
            SetVariationCheckState(Var_start_marimba, "start_marimba");
            SetVariationCheckState(Var_start_digital, "start_digital");

            SetVariationCheckState(Var_end_completion, "end_completion");
            SetVariationCheckState(Var_end_accomplished, "end_accomplished");
            SetVariationCheckState(Var_end_sparkle, "end_sparkle");
            SetVariationCheckState(Var_end_calm, "end_calm");

            SetVariationCheckState(Var_goal_victory, "goal_victory");
            SetVariationCheckState(Var_goal_levelup, "goal_levelup");
            SetVariationCheckState(Var_goal_bell, "goal_bell");
            SetVariationCheckState(Var_goal_cosmic, "goal_cosmic");

            SetVariationCheckState(Var_daily_triumph, "daily_triumph");
            SetVariationCheckState(Var_daily_sunburst, "daily_sunburst");
            SetVariationCheckState(Var_daily_chime, "daily_chime");
            SetVariationCheckState(Var_daily_quest, "daily_quest");

            SetVariationCheckState(Var_weekly_grand, "weekly_grand");
            SetVariationCheckState(Var_weekly_champions, "weekly_champions");
            SetVariationCheckState(Var_weekly_fiesta, "weekly_fiesta");
            SetVariationCheckState(Var_weekly_celestial, "weekly_celestial");

            SetVariationCheckState(Var_breakstart_zen, "breakstart_zen");
            SetVariationCheckState(Var_breakstart_lofi, "breakstart_lofi");
            SetVariationCheckState(Var_breakstart_bowl, "breakstart_bowl");
            SetVariationCheckState(Var_breakstart_waterdrop, "breakstart_waterdrop");

            SetVariationCheckState(Var_breakend_wakeup, "breakend_wakeup");
            SetVariationCheckState(Var_breakend_ready, "breakend_ready");
            SetVariationCheckState(Var_breakend_morning, "breakend_morning");
            SetVariationCheckState(Var_breakend_focusbell, "breakend_focusbell");

            // Scene cheer variations
            SetVariationCheckState(Var_cheer_cycling_bell, "cheer_cycling_bell");
            SetVariationCheckState(Var_cheer_cycling_bark, "cheer_cycling_bark");
            SetVariationCheckState(Var_cheer_cycling_whistle, "cheer_cycling_whistle");

            SetVariationCheckState(Var_cheer_cafe_cup, "cheer_cafe_cup");
            SetVariationCheckState(Var_cheer_cafe_raindrop, "cheer_cafe_raindrop");
            SetVariationCheckState(Var_cheer_cafe_steam, "cheer_cafe_steam");

            SetVariationCheckState(Var_cheer_jazz_sax, "cheer_jazz_sax");
            SetVariationCheckState(Var_cheer_jazz_piano, "cheer_jazz_piano");
            SetVariationCheckState(Var_cheer_jazz_sparkle, "cheer_jazz_sparkle");

            SetVariationCheckState(Var_cheer_icecream_jingle, "cheer_icecream_jingle");
            SetVariationCheckState(Var_cheer_icecream_pop, "cheer_icecream_pop");
            SetVariationCheckState(Var_cheer_icecream_bell, "cheer_icecream_bell");

            SetVariationCheckState(Var_cheer_metro_melody, "cheer_metro_melody");
            SetVariationCheckState(Var_cheer_metro_lofi, "cheer_metro_lofi");
            SetVariationCheckState(Var_cheer_metro_doors, "cheer_metro_doors");

            SetVariationCheckState(Var_cheer_cat_meow, "cheer_cat_meow");
            SetVariationCheckState(Var_cheer_cat_purr, "cheer_cat_purr");
            SetVariationCheckState(Var_cheer_cat_bounce, "cheer_cat_bounce");

            SetVariationCheckState(Var_cheer_rocket_laser, "cheer_rocket_laser");
            SetVariationCheckState(Var_cheer_rocket_beacon, "cheer_rocket_beacon");
            SetVariationCheckState(Var_cheer_rocket_warp, "cheer_rocket_warp");

            SetVariationCheckState(Var_cheer_runner_whistle, "cheer_runner_whistle");
            SetVariationCheckState(Var_cheer_runner_horn, "cheer_runner_horn");
            SetVariationCheckState(Var_cheer_runner_squeak, "cheer_runner_squeak");

            SetVariationCheckState(Var_cheer_pet_happy, "cheer_pet_happy");
            SetVariationCheckState(Var_cheer_pet_levelup, "cheer_pet_levelup");
            SetVariationCheckState(Var_cheer_pet_heart, "cheer_pet_heart");

            SetVariationCheckState(Var_cheer_lumberjack_chop, "cheer_lumberjack_chop");
            SetVariationCheckState(Var_cheer_lumberjack_timber, "cheer_lumberjack_timber");
            SetVariationCheckState(Var_cheer_lumberjack_van, "cheer_lumberjack_van");

            UpdateSoundControlsState();

            // Floating Mini Timer & Display Monitor
            var monitors = ScreenHelper.GetMonitors();
            MiniMonitorComboBox.ItemsSource = monitors;

            var targetMon = ScreenHelper.GetMonitor(Settings.MiniWidgetTargetMonitor, Settings.MiniWidgetMonitorIndex);
            var matchedMon = monitors.FirstOrDefault(m => m.Index == targetMon.Index) ?? monitors.FirstOrDefault();
            MiniMonitorComboBox.SelectedItem = matchedMon;

            MiniCornerComboBox.SelectedIndex = Settings.MiniWidgetCorner switch
            {
                "BottomLeft" => 1,
                "TopRight" => 2,
                "TopLeft" => 3,
                _ => 0
            };
            MiniTimerEnabledCheckBox.IsChecked = Settings.MiniWidgetEnabled;

            AutoAdvanceCheckBox.IsChecked = Settings.AutoAdvancePhases;
            CompanionVisibleCheckBox.IsChecked = Settings.CompanionVisible;

            LoadStreakSettingsToUI();
            SnapshotInitialState();
        }
        finally
        {
            _isUpdatingUi = false;
        }
    }

    private void SetVariationCheckState(CheckBox? checkBox, string variationId)
    {
        if (checkBox == null) return;
        checkBox.IsChecked = Settings.IsVariationEnabled(variationId);
    }

    private void ReadVariationCheckState(CheckBox? checkBox, string variationId)
    {
        if (checkBox == null) return;
        if (checkBox.IsChecked == true)
        {
            Settings.EnabledSoundVariations.Add(variationId);
        }
    }

    private void UpdateSoundControlsState()
    {
        bool soundMasterEnabled = SoundAlertCheckBox.IsChecked == true;
        if (SoundControlsContainer != null)
        {
            SoundControlsContainer.IsEnabled = soundMasterEnabled;
            SoundControlsContainer.Opacity = soundMasterEnabled ? 1.0 : 0.45;
        }

        UpdateEventVariationPanelsState();
    }

    private void UpdateEventVariationPanelsState()
    {
        if (StartVariationsPanel != null)
            StartVariationsPanel.IsEnabled = SoundOnStartCheckBox.IsChecked == true;
        if (EndVariationsPanel != null)
            EndVariationsPanel.IsEnabled = SoundOnEndCheckBox.IsChecked == true;
        if (GoalVariationsPanel != null)
            GoalVariationsPanel.IsEnabled = SoundOnGoalCheckBox.IsChecked == true;
        if (DailyGoalVariationsPanel != null)
            DailyGoalVariationsPanel.IsEnabled = SoundOnDailyGoalCheckBox.IsChecked == true;
        if (WeeklyGoalVariationsPanel != null)
            WeeklyGoalVariationsPanel.IsEnabled = SoundOnWeeklyGoalCheckBox.IsChecked == true;
        if (BreakStartVariationsPanel != null)
            BreakStartVariationsPanel.IsEnabled = SoundOnBreakStartCheckBox.IsChecked == true;
        if (BreakEndVariationsPanel != null)
            BreakEndVariationsPanel.IsEnabled = SoundOnBreakEndCheckBox.IsChecked == true;
        if (CheerVariationsPanel != null)
            CheerVariationsPanel.IsEnabled = SoundOnCheerCheckBox.IsChecked == true;
    }

    private void SoundAlertCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSoundControlsState();
    }

    private void SoundEventToggle_Changed(object sender, RoutedEventArgs e)
    {
        UpdateEventVariationPanelsState();
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VolumeValueText != null && VolumeSlider != null)
        {
            VolumeValueText.Text = $"{VolumeSlider.Value:F0}%";
        }
    }

    private void GoalMilestoneVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GoalMilestoneVolumeValueText != null && GoalMilestoneVolumeSlider != null)
        {
            GoalMilestoneVolumeValueText.Text = $"{GoalMilestoneVolumeSlider.Value:F0}%";
        }

        if (!_isUpdatingUi && AudioTabGoalVolumeSlider != null && GoalMilestoneVolumeSlider != null)
        {
            _isUpdatingUi = true;
            AudioTabGoalVolumeSlider.Value = GoalMilestoneVolumeSlider.Value;
            if (AudioTabGoalVolumeValueText != null)
            {
                AudioTabGoalVolumeValueText.Text = $"{GoalMilestoneVolumeSlider.Value:F0}%";
            }
            _isUpdatingUi = false;
        }
    }

    private void AudioTabGoalVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AudioTabGoalVolumeValueText != null && AudioTabGoalVolumeSlider != null)
        {
            AudioTabGoalVolumeValueText.Text = $"{AudioTabGoalVolumeSlider.Value:F0}%";
        }

        if (!_isUpdatingUi && GoalMilestoneVolumeSlider != null && AudioTabGoalVolumeSlider != null)
        {
            _isUpdatingUi = true;
            GoalMilestoneVolumeSlider.Value = AudioTabGoalVolumeSlider.Value;
            if (GoalMilestoneVolumeValueText != null)
            {
                GoalMilestoneVolumeValueText.Text = $"{AudioTabGoalVolumeSlider.Value:F0}%";
            }
            _isUpdatingUi = false;
        }
    }

    private void TestGoalMilestoneVolumeButton_Click(object sender, RoutedEventArgs e)
    {
        int vol = (int)(GoalMilestoneVolumeSlider?.Value ?? (AudioTabGoalVolumeSlider?.Value ?? Settings.GoalMilestoneSoundVolume));
        SoundEffectManager.PlayGoalMilestonePreview(vol);
    }

    private void TestVolumeButton_Click(object sender, RoutedEventArgs e)
    {
        int vol = (int)(VolumeSlider?.Value ?? 75);
        SoundEffectManager.PlayPreview(SoundEffectType.Start, vol);
    }

    private void TestVariationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string varId)
        {
            bool isGoalVar = varId.StartsWith("goal_", StringComparison.OrdinalIgnoreCase) ||
                             varId.StartsWith("daily_", StringComparison.OrdinalIgnoreCase) ||
                             varId.StartsWith("weekly_", StringComparison.OrdinalIgnoreCase);

            int vol = isGoalVar
                ? (int)(GoalMilestoneVolumeSlider?.Value ?? (AudioTabGoalVolumeSlider?.Value ?? Settings.GoalMilestoneSoundVolume))
                : (int)(VolumeSlider?.Value ?? Settings.SoundVolume);

            SoundEffectManager.PlayVariationPreview(varId, vol);
        }
    }

    private void TestEventButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string eventTypeStr && Enum.TryParse<SoundEffectType>(eventTypeStr, out var eventType))
        {
            bool isGoalEvent = eventType is SoundEffectType.GoalAchieved or SoundEffectType.DailyGoalAchieved or SoundEffectType.WeeklyGoalAchieved;

            int vol = isGoalEvent
                ? (int)(GoalMilestoneVolumeSlider?.Value ?? (AudioTabGoalVolumeSlider?.Value ?? Settings.GoalMilestoneSoundVolume))
                : (int)(VolumeSlider?.Value ?? Settings.SoundVolume);

            SoundEffectManager.PlayPreview(eventType, vol);
        }
    }

    private void TestSceneButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string sceneId)
        {
            int vol = (int)(VolumeSlider?.Value ?? 75);
            SoundEffectManager.PlayScenePreview(sceneId, vol);
        }
    }

    private void SelectMatchingPreset()
    {
        var matched = _presets.FirstOrDefault(p =>
            p.FocusMinutes == Settings.FocusMinutes &&
            p.ShortBreakMinutes == Settings.ShortBreakMinutes &&
            p.LongBreakMinutes == Settings.LongBreakMinutes &&
            p.CyclesBeforeLongBreak == Settings.CyclesBeforeLongBreak);

        if (matched != null)
        {
            PresetComboBox.SelectedItem = matched;
            PresetDescriptionText.Text = matched.Description;
        }
        else
        {
            PresetComboBox.SelectedIndex = _presets.Count - 1; // Custom
            PresetDescriptionText.Text = _presets.Last().Description;
        }
    }

    private void UpdateLabels()
    {
        if (FocusValueText != null && FocusSlider != null)
            FocusValueText.Text = $"{FocusSlider.Value:F0} min";
        if (ShortBreakValueText != null && ShortBreakSlider != null)
            ShortBreakValueText.Text = $"{ShortBreakSlider.Value:F0} min";
        if (LongBreakValueText != null && LongBreakSlider != null)
            LongBreakValueText.Text = $"{LongBreakSlider.Value:F0} min";
        if (CyclesValueText != null && CyclesSlider != null)
            CyclesValueText.Text = $"{CyclesSlider.Value:F0} cycles";
    }

    private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || PresetComboBox?.SelectedItem is not IntervalPreset preset || PresetDescriptionText == null)
            return;

        PresetDescriptionText.Text = preset.Description;

        if (preset.Name.Contains("Custom"))
            return;

        _isUpdatingUi = true;
        if (FocusSlider != null) FocusSlider.Value = preset.FocusMinutes;
        if (ShortBreakSlider != null) ShortBreakSlider.Value = preset.ShortBreakMinutes;
        if (LongBreakSlider != null) LongBreakSlider.Value = preset.LongBreakMinutes;
        if (CyclesSlider != null) CyclesSlider.Value = preset.CyclesBeforeLongBreak;
        UpdateLabels();
        _isUpdatingUi = false;
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi || FocusSlider == null || ShortBreakSlider == null || LongBreakSlider == null || CyclesSlider == null || PresetComboBox == null || PresetDescriptionText == null)
            return;

        UpdateLabels();

        // Check if values match a preset
        var currentFocus = (int)FocusSlider.Value;
        var currentShort = (int)ShortBreakSlider.Value;
        var currentLong = (int)LongBreakSlider.Value;
        var currentCycles = (int)CyclesSlider.Value;

        var matched = _presets.Take(_presets.Count - 1).FirstOrDefault(p =>
            p.FocusMinutes == currentFocus &&
            p.ShortBreakMinutes == currentShort &&
            p.LongBreakMinutes == currentLong &&
            p.CyclesBeforeLongBreak == currentCycles);

        _isUpdatingUi = true;
        if (matched != null)
        {
            PresetComboBox.SelectedItem = matched;
            PresetDescriptionText.Text = matched.Description;
        }
        else
        {
            PresetComboBox.SelectedIndex = _presets.Count - 1;
            PresetDescriptionText.Text = _presets.Last().Description;
        }
        _isUpdatingUi = false;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettingsToModel();
        _isExplicitlySaved = true;
        _suppressClosingPrompt = true;
        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Extracts all current settings from the UI controls into the Settings model and saves the streak profile.
    /// </summary>
    public void SaveSettingsToModel()
    {
        Settings.IntervalModeEnabled = IntervalModeEnabledCheckBox?.IsChecked ?? true;
        Settings.UserName = UserNameTextBox?.Text.Trim() ?? string.Empty;
        Settings.WelcomePopupEnabled = WelcomePopupEnabledCheckBox?.IsChecked ?? true;
        Settings.MilestoneRemindersEnabled = MilestoneRemindersEnabledCheckBox?.IsChecked ?? true;
        Settings.MilestoneDisplayMode = (MilestoneQueueModeComboBox?.SelectedIndex == 1) ? "AllUpcoming" : "NextOnly";
        Settings.FocusMinutes = (int)(FocusSlider?.Value ?? Settings.FocusMinutes);
        Settings.ShortBreakMinutes = (int)(ShortBreakSlider?.Value ?? Settings.ShortBreakMinutes);
        Settings.LongBreakMinutes = (int)(LongBreakSlider?.Value ?? Settings.LongBreakMinutes);
        Settings.CyclesBeforeLongBreak = (int)(CyclesSlider?.Value ?? Settings.CyclesBeforeLongBreak);

        Settings.SoundAlertEnabled = SoundAlertCheckBox?.IsChecked ?? true;
        Settings.SoundVolume = (int)(VolumeSlider?.Value ?? Settings.SoundVolume);
        Settings.GoalMilestoneSoundVolume = (int)(GoalMilestoneVolumeSlider?.Value ?? (AudioTabGoalVolumeSlider?.Value ?? Settings.GoalMilestoneSoundVolume));
        Settings.SoundOnStartEnabled = SoundOnStartCheckBox?.IsChecked ?? true;
        Settings.SoundOnEndEnabled = SoundOnEndCheckBox?.IsChecked ?? true;
        Settings.SoundOnGoalAchievedEnabled = SoundOnGoalCheckBox?.IsChecked ?? true;
        Settings.SoundOnDailyGoalAchievedEnabled = SoundOnDailyGoalCheckBox?.IsChecked ?? true;
        Settings.SoundOnWeeklyGoalAchievedEnabled = SoundOnWeeklyGoalCheckBox?.IsChecked ?? true;
        Settings.SoundOnBreakStartEnabled = SoundOnBreakStartCheckBox?.IsChecked ?? true;
        Settings.SoundOnBreakEndEnabled = SoundOnBreakEndCheckBox?.IsChecked ?? true;
        Settings.SoundOnCheerEnabled = SoundOnCheerCheckBox?.IsChecked ?? true;

        Settings.EnabledSoundVariations = GetSelectedSoundVariations();

        Settings.AutoAdvancePhases = AutoAdvanceCheckBox?.IsChecked ?? false;
        Settings.CompanionVisible = CompanionVisibleCheckBox?.IsChecked ?? true;

        // Save Reward Icon and Goal Notifications
        if (RewardIconComboBox?.SelectedItem is RewardIconOption selectedReward)
        {
            Settings.CompletionRewardIcon = selectedReward.Key;
        }

        if (RewardColorComboBox?.SelectedItem is RewardColorOption selectedColor)
        {
            Settings.CompletionRewardColor = selectedColor.Key;
        }

        Settings.GoalNotificationsEnabled = GoalNotificationsEnabledCheckBox?.IsChecked ?? true;
        Settings.DailyGoalNotificationEnabled = DailyGoalNotificationCheckBox?.IsChecked ?? true;
        Settings.WeeklyGoalNotificationEnabled = WeeklyGoalNotificationCheckBox?.IsChecked ?? true;
        Settings.ProjectGoalNotificationEnabled = ProjectGoalNotificationCheckBox?.IsChecked ?? true;

        if (MiniMonitorComboBox?.SelectedItem is MonitorDisplayInfo mon)
        {
            Settings.MiniWidgetTargetMonitor = mon.DeviceName;
            Settings.MiniWidgetMonitorIndex = mon.Index;
        }

        Settings.MiniWidgetCorner = MiniCornerComboBox?.SelectedIndex switch
        {
            1 => "BottomLeft",
            2 => "TopRight",
            3 => "TopLeft",
            _ => "BottomRight"
        };
        Settings.MiniWidgetEnabled = MiniTimerEnabledCheckBox?.IsChecked ?? true;

        // Save Streak settings
        var streakProfile = StreakService.Instance.Profile;
        streakProfile.Enabled = StreakSystemEnabledCheckBox?.IsChecked ?? true;
        streakProfile.ExcludedDays = GetCurrentExcludedDays().ToList();
        StreakService.Instance.SaveProfile();
    }

    private void SnapshotInitialState()
    {
        _initialIntervalModeEnabled = IntervalModeEnabledCheckBox.IsChecked ?? true;
        _initialUserName = UserNameTextBox.Text.Trim();
        _initialWelcomePopupEnabled = WelcomePopupEnabledCheckBox.IsChecked ?? true;
        _initialMilestoneRemindersEnabled = MilestoneRemindersEnabledCheckBox?.IsChecked ?? true;
        _initialMilestoneDisplayMode = (MilestoneQueueModeComboBox?.SelectedIndex == 1) ? "AllUpcoming" : "NextOnly";
        _initialFocusMinutes = (int)FocusSlider.Value;
        _initialShortBreakMinutes = (int)ShortBreakSlider.Value;
        _initialLongBreakMinutes = (int)LongBreakSlider.Value;
        _initialCyclesBeforeLongBreak = (int)CyclesSlider.Value;
        _initialSoundAlertEnabled = SoundAlertCheckBox.IsChecked ?? true;
        _initialSoundVolume = (int)VolumeSlider.Value;
        _initialGoalMilestoneSoundVolume = (int)(GoalMilestoneVolumeSlider?.Value ?? (AudioTabGoalVolumeSlider?.Value ?? Settings.GoalMilestoneSoundVolume));
        _initialSoundOnStartEnabled = SoundOnStartCheckBox.IsChecked ?? true;
        _initialSoundOnEndEnabled = SoundOnEndCheckBox.IsChecked ?? true;
        _initialSoundOnGoalAchievedEnabled = SoundOnGoalCheckBox.IsChecked ?? true;
        _initialSoundOnDailyGoalAchievedEnabled = SoundOnDailyGoalCheckBox.IsChecked ?? true;
        _initialSoundOnWeeklyGoalAchievedEnabled = SoundOnWeeklyGoalCheckBox.IsChecked ?? true;
        _initialSoundOnBreakStartEnabled = SoundOnBreakStartCheckBox.IsChecked ?? true;
        _initialSoundOnBreakEndEnabled = SoundOnBreakEndCheckBox.IsChecked ?? true;
        _initialSoundOnCheerEnabled = SoundOnCheerCheckBox.IsChecked ?? true;
        _initialSoundVariations = new HashSet<string>(GetSelectedSoundVariations());
        _initialAutoAdvancePhases = AutoAdvanceCheckBox.IsChecked ?? false;
        _initialCompanionVisible = CompanionVisibleCheckBox.IsChecked ?? true;
        _initialRewardIcon = (RewardIconComboBox.SelectedItem as RewardIconOption)?.Key ?? Settings.CompletionRewardIcon;
        _initialRewardColor = (RewardColorComboBox.SelectedItem as RewardColorOption)?.Key ?? Settings.CompletionRewardColor;
        _initialGoalNotificationsEnabled = GoalNotificationsEnabledCheckBox.IsChecked ?? true;
        _initialDailyGoalNotificationEnabled = DailyGoalNotificationCheckBox.IsChecked ?? true;
        _initialWeeklyGoalNotificationEnabled = WeeklyGoalNotificationCheckBox.IsChecked ?? true;
        _initialProjectGoalNotificationEnabled = ProjectGoalNotificationCheckBox.IsChecked ?? true;
        _initialTargetMonitor = (MiniMonitorComboBox.SelectedItem as MonitorDisplayInfo)?.DeviceName ?? Settings.MiniWidgetTargetMonitor;
        _initialMonitorIndex = (MiniMonitorComboBox.SelectedItem as MonitorDisplayInfo)?.Index ?? Settings.MiniWidgetMonitorIndex;
        _initialCorner = GetCurrentCorner();
        _initialMiniWidgetEnabled = MiniTimerEnabledCheckBox.IsChecked ?? true;

        var streakProfile = StreakService.Instance.Profile;
        _initialStreakEnabled = StreakSystemEnabledCheckBox?.IsChecked ?? streakProfile.Enabled;
        _initialStreakExcludedDays = GetCurrentExcludedDays();
    }

    private string GetCurrentCorner()
    {
        return MiniCornerComboBox?.SelectedIndex switch
        {
            1 => "BottomLeft",
            2 => "TopRight",
            3 => "TopLeft",
            _ => "BottomRight"
        };
    }

    /// <summary>
    /// Checks whether any configuration values in the UI currently differ from their initial values.
    /// </summary>
    public bool HasUnsavedChanges()
    {
        if (_isUpdatingUi) return false;

        if ((IntervalModeEnabledCheckBox?.IsChecked ?? true) != _initialIntervalModeEnabled)
            return true;

        if (!string.Equals((UserNameTextBox?.Text?.Trim() ?? string.Empty), _initialUserName, StringComparison.Ordinal))
            return true;

        if ((WelcomePopupEnabledCheckBox?.IsChecked ?? true) != _initialWelcomePopupEnabled)
            return true;

        if ((MilestoneRemindersEnabledCheckBox?.IsChecked ?? true) != _initialMilestoneRemindersEnabled)
            return true;

        var currentMilestoneMode = (MilestoneQueueModeComboBox?.SelectedIndex == 1) ? "AllUpcoming" : "NextOnly";
        if (!string.Equals(currentMilestoneMode, _initialMilestoneDisplayMode, StringComparison.OrdinalIgnoreCase))
            return true;

        if ((int)(FocusSlider?.Value ?? _initialFocusMinutes) != _initialFocusMinutes)
            return true;

        if ((int)(ShortBreakSlider?.Value ?? _initialShortBreakMinutes) != _initialShortBreakMinutes)
            return true;

        if ((int)(LongBreakSlider?.Value ?? _initialLongBreakMinutes) != _initialLongBreakMinutes)
            return true;

        if ((int)(CyclesSlider?.Value ?? _initialCyclesBeforeLongBreak) != _initialCyclesBeforeLongBreak)
            return true;

        if ((SoundAlertCheckBox?.IsChecked ?? true) != _initialSoundAlertEnabled)
            return true;

        if ((int)(VolumeSlider?.Value ?? _initialSoundVolume) != _initialSoundVolume)
            return true;

        int currentGoalVol = (int)(GoalMilestoneVolumeSlider?.Value ?? (AudioTabGoalVolumeSlider?.Value ?? _initialGoalMilestoneSoundVolume));
        if (currentGoalVol != _initialGoalMilestoneSoundVolume)
            return true;

        if ((SoundOnStartCheckBox?.IsChecked ?? true) != _initialSoundOnStartEnabled)
            return true;

        if ((SoundOnEndCheckBox?.IsChecked ?? true) != _initialSoundOnEndEnabled)
            return true;

        if ((SoundOnGoalCheckBox?.IsChecked ?? true) != _initialSoundOnGoalAchievedEnabled)
            return true;

        if ((SoundOnDailyGoalCheckBox?.IsChecked ?? true) != _initialSoundOnDailyGoalAchievedEnabled)
            return true;

        if ((SoundOnWeeklyGoalCheckBox?.IsChecked ?? true) != _initialSoundOnWeeklyGoalAchievedEnabled)
            return true;

        if ((SoundOnBreakStartCheckBox?.IsChecked ?? true) != _initialSoundOnBreakStartEnabled)
            return true;

        if ((SoundOnBreakEndCheckBox?.IsChecked ?? true) != _initialSoundOnBreakEndEnabled)
            return true;

        if ((SoundOnCheerCheckBox?.IsChecked ?? true) != _initialSoundOnCheerEnabled)
            return true;

        var currentVars = GetSelectedSoundVariations();
        if (!_initialSoundVariations.SetEquals(currentVars))
            return true;

        if ((AutoAdvanceCheckBox?.IsChecked ?? false) != _initialAutoAdvancePhases)
            return true;

        if ((CompanionVisibleCheckBox?.IsChecked ?? true) != _initialCompanionVisible)
            return true;

        var selectedReward = RewardIconComboBox?.SelectedItem as RewardIconOption;
        var currentRewardIcon = selectedReward?.Key ?? _initialRewardIcon;
        if (!string.Equals(currentRewardIcon, _initialRewardIcon, StringComparison.OrdinalIgnoreCase))
            return true;

        var selectedColor = RewardColorComboBox?.SelectedItem as RewardColorOption;
        var currentRewardColor = selectedColor?.Key ?? _initialRewardColor;
        if (!string.Equals(currentRewardColor, _initialRewardColor, StringComparison.OrdinalIgnoreCase))
            return true;

        if ((GoalNotificationsEnabledCheckBox?.IsChecked ?? true) != _initialGoalNotificationsEnabled)
            return true;

        if ((DailyGoalNotificationCheckBox?.IsChecked ?? true) != _initialDailyGoalNotificationEnabled)
            return true;

        if ((WeeklyGoalNotificationCheckBox?.IsChecked ?? true) != _initialWeeklyGoalNotificationEnabled)
            return true;

        if ((ProjectGoalNotificationCheckBox?.IsChecked ?? true) != _initialProjectGoalNotificationEnabled)
            return true;

        if (MiniMonitorComboBox?.SelectedItem is MonitorDisplayInfo mon)
        {
            if (mon.DeviceName != _initialTargetMonitor || mon.Index != _initialMonitorIndex)
                return true;
        }

        string currentCorner = GetCurrentCorner();
        if (!string.Equals(currentCorner, _initialCorner, StringComparison.OrdinalIgnoreCase))
            return true;

        if ((MiniTimerEnabledCheckBox?.IsChecked ?? true) != _initialMiniWidgetEnabled)
            return true;

        if (StreakSystemEnabledCheckBox != null && (StreakSystemEnabledCheckBox.IsChecked ?? true) != _initialStreakEnabled)
            return true;

        var currentExcluded = GetCurrentExcludedDays();
        if (!_initialStreakExcludedDays.SetEquals(currentExcluded))
            return true;

        return false;
    }

    private MessageBoxResult PromptUnsavedChanges()
    {
        if (PromptUnsavedChangesHandler != null)
        {
            return PromptUnsavedChangesHandler();
        }

        return ThemedMessageBox.ConfirmSave(
            this,
            message: "You have unsaved changes in your settings.\n\nWould you like to save them before exiting?",
            title: "Unsaved Changes",
            saveButtonText: "💾 Save Settings",
            dontSaveButtonText: "Discard Changes",
            cancelButtonText: "Keep Editing",
            subtitle: "Unsaved modifications detected");
    }

    private HashSet<DayOfWeek> GetCurrentExcludedDays()
    {
        var excluded = new HashSet<DayOfWeek>();
        if (SettingsExcludeMonCheckBox?.IsChecked == true) excluded.Add(DayOfWeek.Monday);
        if (SettingsExcludeTueCheckBox?.IsChecked == true) excluded.Add(DayOfWeek.Tuesday);
        if (SettingsExcludeWedCheckBox?.IsChecked == true) excluded.Add(DayOfWeek.Wednesday);
        if (SettingsExcludeThuCheckBox?.IsChecked == true) excluded.Add(DayOfWeek.Thursday);
        if (SettingsExcludeFriCheckBox?.IsChecked == true) excluded.Add(DayOfWeek.Friday);
        if (SettingsExcludeSatCheckBox?.IsChecked == true) excluded.Add(DayOfWeek.Saturday);
        if (SettingsExcludeSunCheckBox?.IsChecked == true) excluded.Add(DayOfWeek.Sunday);
        return excluded;
    }

    private List<string> GetSelectedSoundVariations()
    {
        var list = new List<string>();
        AddIfVariationChecked(Var_start_ascending, "start_ascending", list);
        AddIfVariationChecked(Var_start_pop, "start_pop", list);
        AddIfVariationChecked(Var_start_marimba, "start_marimba", list);
        AddIfVariationChecked(Var_start_digital, "start_digital", list);

        AddIfVariationChecked(Var_end_completion, "end_completion", list);
        AddIfVariationChecked(Var_end_accomplished, "end_accomplished", list);
        AddIfVariationChecked(Var_end_sparkle, "end_sparkle", list);
        AddIfVariationChecked(Var_end_calm, "end_calm", list);

        AddIfVariationChecked(Var_goal_victory, "goal_victory", list);
        AddIfVariationChecked(Var_goal_levelup, "goal_levelup", list);
        AddIfVariationChecked(Var_goal_bell, "goal_bell", list);
        AddIfVariationChecked(Var_goal_cosmic, "goal_cosmic", list);

        AddIfVariationChecked(Var_daily_triumph, "daily_triumph", list);
        AddIfVariationChecked(Var_daily_sunburst, "daily_sunburst", list);
        AddIfVariationChecked(Var_daily_chime, "daily_chime", list);
        AddIfVariationChecked(Var_daily_quest, "daily_quest", list);

        AddIfVariationChecked(Var_weekly_grand, "weekly_grand", list);
        AddIfVariationChecked(Var_weekly_champions, "weekly_champions", list);
        AddIfVariationChecked(Var_weekly_fiesta, "weekly_fiesta", list);
        AddIfVariationChecked(Var_weekly_celestial, "weekly_celestial", list);

        AddIfVariationChecked(Var_breakstart_zen, "breakstart_zen", list);
        AddIfVariationChecked(Var_breakstart_lofi, "breakstart_lofi", list);
        AddIfVariationChecked(Var_breakstart_bowl, "breakstart_bowl", list);
        AddIfVariationChecked(Var_breakstart_waterdrop, "breakstart_waterdrop", list);

        AddIfVariationChecked(Var_breakend_wakeup, "breakend_wakeup", list);
        AddIfVariationChecked(Var_breakend_ready, "breakend_ready", list);
        AddIfVariationChecked(Var_breakend_morning, "breakend_morning", list);
        AddIfVariationChecked(Var_breakend_focusbell, "breakend_focusbell", list);

        // Scene cheer variations
        AddIfVariationChecked(Var_cheer_cycling_bell, "cheer_cycling_bell", list);
        AddIfVariationChecked(Var_cheer_cycling_bark, "cheer_cycling_bark", list);
        AddIfVariationChecked(Var_cheer_cycling_whistle, "cheer_cycling_whistle", list);

        AddIfVariationChecked(Var_cheer_cafe_cup, "cheer_cafe_cup", list);
        AddIfVariationChecked(Var_cheer_cafe_raindrop, "cheer_cafe_raindrop", list);
        AddIfVariationChecked(Var_cheer_cafe_steam, "cheer_cafe_steam", list);

        AddIfVariationChecked(Var_cheer_jazz_sax, "cheer_jazz_sax", list);
        AddIfVariationChecked(Var_cheer_jazz_piano, "cheer_jazz_piano", list);
        AddIfVariationChecked(Var_cheer_jazz_sparkle, "cheer_jazz_sparkle", list);

        AddIfVariationChecked(Var_cheer_icecream_jingle, "cheer_icecream_jingle", list);
        AddIfVariationChecked(Var_cheer_icecream_pop, "cheer_icecream_pop", list);
        AddIfVariationChecked(Var_cheer_icecream_bell, "cheer_icecream_bell", list);

        AddIfVariationChecked(Var_cheer_metro_melody, "cheer_metro_melody", list);
        AddIfVariationChecked(Var_cheer_metro_lofi, "cheer_metro_lofi", list);
        AddIfVariationChecked(Var_cheer_metro_doors, "cheer_metro_doors", list);

        AddIfVariationChecked(Var_cheer_cat_meow, "cheer_cat_meow", list);
        AddIfVariationChecked(Var_cheer_cat_purr, "cheer_cat_purr", list);
        AddIfVariationChecked(Var_cheer_cat_bounce, "cheer_cat_bounce", list);

        AddIfVariationChecked(Var_cheer_rocket_laser, "cheer_rocket_laser", list);
        AddIfVariationChecked(Var_cheer_rocket_beacon, "cheer_rocket_beacon", list);
        AddIfVariationChecked(Var_cheer_rocket_warp, "cheer_rocket_warp", list);

        AddIfVariationChecked(Var_cheer_runner_whistle, "cheer_runner_whistle", list);
        AddIfVariationChecked(Var_cheer_runner_horn, "cheer_runner_horn", list);
        AddIfVariationChecked(Var_cheer_runner_squeak, "cheer_runner_squeak", list);

        AddIfVariationChecked(Var_cheer_pet_happy, "cheer_pet_happy", list);
        AddIfVariationChecked(Var_cheer_pet_levelup, "cheer_pet_levelup", list);
        AddIfVariationChecked(Var_cheer_pet_heart, "cheer_pet_heart", list);

        AddIfVariationChecked(Var_cheer_lumberjack_chop, "cheer_lumberjack_chop", list);
        AddIfVariationChecked(Var_cheer_lumberjack_timber, "cheer_lumberjack_timber", list);
        AddIfVariationChecked(Var_cheer_lumberjack_van, "cheer_lumberjack_van", list);

        return list;
    }

    private static void AddIfVariationChecked(CheckBox? cb, string variationId, List<string> list)
    {
        if (cb?.IsChecked == true)
        {
            list.Add(variationId);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_suppressClosingPrompt || _isExplicitlySaved)
        {
            base.OnClosing(e);
            return;
        }

        if (HasUnsavedChanges())
        {
            var result = PromptUnsavedChanges();
            if (result == MessageBoxResult.Yes)
            {
                SaveSettingsToModel();
                _isExplicitlySaved = true;
                _suppressClosingPrompt = true;
                try
                {
                    DialogResult = true;
                }
                catch { }
            }
            else if (result == MessageBoxResult.No)
            {
                _suppressClosingPrompt = true;
                try
                {
                    DialogResult = false;
                }
                catch { }
            }
            else
            {
                // Cancel or dismissed dialog
                e.Cancel = true;
                return;
            }
        }

        base.OnClosing(e);
    }

    private void RewardIconComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi) return;
        UpdateRewardPreview();
    }

    private void RewardColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi) return;
        UpdateRewardPreview();
    }

    private void UpdateRewardPreview()
    {
        var iconOpt = RewardIconComboBox?.SelectedItem as RewardIconOption;
        var colorOpt = RewardColorComboBox?.SelectedItem as RewardColorOption;

        if (iconOpt != null)
        {
            if (RewardIconPreviewEmoji != null)
                RewardIconPreviewEmoji.Text = iconOpt.Emoji;
            if (RewardIconPreviewText != null)
                RewardIconPreviewText.Text = $"Goal Reached! (100% Completed)";
        }

        var (bgBrush, borderBrush, textBrush) = RewardColorHelper.GetRewardBrushes(colorOpt?.Key ?? Settings.CompletionRewardColor, this);
        if (RewardPreviewBorder != null)
        {
            RewardPreviewBorder.Background = bgBrush;
            RewardPreviewBorder.BorderBrush = borderBrush;
        }
        if (RewardIconPreviewText != null)
        {
            RewardIconPreviewText.Foreground = textBrush;
        }
    }

    private void GoalNotificationsEnabledCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUi) return;
        bool isEnabled = GoalNotificationsEnabledCheckBox?.IsChecked ?? true;
        if (GranularNotificationsPanel != null)
        {
            GranularNotificationsPanel.IsEnabled = isEnabled;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateForceWelcomeButtonState()
    {
        if (ForceWelcomeNextLaunchButton == null) return;

        if (Settings.ForceWelcomePopupOnNextLaunch)
        {
            ForceWelcomeNextLaunchButton.Content = "✓ Will show WELCOME BACK POPUP on next launch";
            ForceWelcomeNextLaunchButton.ToolTip = "The Welcome Back popup is set to display on the next launch. Click to cancel.";
        }
        else
        {
            ForceWelcomeNextLaunchButton.Content = "Show WELCOME BACK POPUP on next launch";
            ForceWelcomeNextLaunchButton.ToolTip = "Show the Welcome Back popup again next time the application is launched today.";
        }
    }

    private void ForceWelcomeNextLaunchButton_Click(object sender, RoutedEventArgs e)
    {
        Settings.ForceWelcomePopupOnNextLaunch = !Settings.ForceWelcomePopupOnNextLaunch;
        if (Settings.ForceWelcomePopupOnNextLaunch && WelcomePopupEnabledCheckBox.IsChecked != true)
        {
            WelcomePopupEnabledCheckBox.IsChecked = true;
            Settings.WelcomePopupEnabled = true;
        }

        // Persist immediately to settings storage so it takes effect even if closed without Save
        var saved = FocusCompanionStorage.Load();
        saved.ForceWelcomePopupOnNextLaunch = Settings.ForceWelcomePopupOnNextLaunch;
        if (Settings.ForceWelcomePopupOnNextLaunch)
        {
            saved.WelcomePopupEnabled = true;
        }
        FocusCompanionStorage.Save(saved);

        UpdateForceWelcomeButtonState();
    }

    private void PreviewWelcomeButton_Click(object sender, RoutedEventArgs e)
    {
        var previewName = UserNameTextBox.Text.Trim();
        var todayPlans = CalendarPlanStorage.Instance.GetPlansForDate(DateTime.Today);
        var previewDialog = new WelcomeBackDialog(previewName, todayPlans)
        {
            Owner = this
        };
        previewDialog.ShowDialog();
    }

    private void BackupsButton_Click(object sender, RoutedEventArgs e)
    {
        bool confirmed = ThemedMessageBox.Confirm(
            this,
            "Are you sure you want to create a backup of your data?",
            "Confirm Backup",
            "Backup Now",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        var created = BackupService.Instance.CreateBackup(
            reason: "Manual Backup",
            note: "Created from Settings",
            isManual: true,
            pruneAfter: false);

        if (created != null)
        {
            ThemedMessageBox.ShowSuccess(
                this,
                $"Backup snapshot created successfully!\n\nSnapshot contains:\n• {created.TimeEntryCount} Time Entries\n• {created.ProjectCount} Projects\n• {created.ActivityCount} Activities\n• {created.CalendarPlanCount} Calendar Plans\n• Total size: {created.FormattedSize}",
                "Backup Created");
        }
        else
        {
            ThemedMessageBox.ShowWarning(
                this,
                "No active data was found to back up.",
                "Backup Incomplete");
        }
    }

    private void ManageBackupsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new BackupManagementWindow
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void LoadStreakSettingsToUI()
    {
        var streakProfile = StreakService.Instance.Profile;
        if (StreakSystemEnabledCheckBox != null)
        {
            StreakSystemEnabledCheckBox.IsChecked = streakProfile.Enabled;
        }

        var excluded = streakProfile.ExcludedDays ?? new List<DayOfWeek>();
        if (SettingsExcludeMonCheckBox != null) SettingsExcludeMonCheckBox.IsChecked = excluded.Contains(DayOfWeek.Monday);
        if (SettingsExcludeTueCheckBox != null) SettingsExcludeTueCheckBox.IsChecked = excluded.Contains(DayOfWeek.Tuesday);
        if (SettingsExcludeWedCheckBox != null) SettingsExcludeWedCheckBox.IsChecked = excluded.Contains(DayOfWeek.Wednesday);
        if (SettingsExcludeThuCheckBox != null) SettingsExcludeThuCheckBox.IsChecked = excluded.Contains(DayOfWeek.Thursday);
        if (SettingsExcludeFriCheckBox != null) SettingsExcludeFriCheckBox.IsChecked = excluded.Contains(DayOfWeek.Friday);
        if (SettingsExcludeSatCheckBox != null) SettingsExcludeSatCheckBox.IsChecked = excluded.Contains(DayOfWeek.Saturday);
        if (SettingsExcludeSunCheckBox != null) SettingsExcludeSunCheckBox.IsChecked = excluded.Contains(DayOfWeek.Sunday);

        int remainingSick = StreakService.Instance.GetRemainingSickDays(DateTime.Today);
        if (SettingsSickDaysInfoText != null)
        {
            SettingsSickDaysInfoText.Text = string.Format("🛡️ Rest Days: {0} / {1} remaining (Max 3, refreshes monthly)", remainingSick, StreakProfile.MaxRestDaysLimit);
        }

        var entries = new TimeEntryStorage().Load();
        double totalHours = entries.Sum(e => e.Duration.TotalHours);
        int availableStars = streakProfile.GetAvailableGoldStars(totalHours);
        bool canBuy = streakProfile.CanBuyRestDay(totalHours, DateTime.Today.Year, DateTime.Today.Month, out var reason);

        if (SettingsGoldStarsInfoText != null)
        {
            SettingsGoldStarsInfoText.Text = string.Format("⭐ Gold Stars: {0} ⭐ Available (1 star per worked hour | Rest days cost 30 stars)", availableStars);
        }

        if (SettingsBuyRestDayButton != null)
        {
            SettingsBuyRestDayButton.IsEnabled = canBuy;
            SettingsBuyRestDayButton.ToolTip = reason;
        }
    }

    private void SettingsBuyRestDayButton_Click(object sender, RoutedEventArgs e)
    {
        var entries = new TimeEntryStorage().Load();
        if (StreakService.Instance.BuyRestDay(entries, DateTime.Today))
        {
            LoadStreakSettingsToUI();
            ThemedMessageBox.Show(this,
                "You successfully purchased 1 Rest Day for 30 Gold Stars! Your rest day protection is active.",
                "⭐ Rest Day Purchased!",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            StreakService.Instance.CanBuyRestDay(entries.Sum(x => x.Duration.TotalHours), DateTime.Today, out var reason);
            ThemedMessageBox.Show(this,
                reason,
                "Unable to Purchase Rest Day",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenStreakManagerButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new StreakDialog(null, StreakService.Instance)
        {
            Owner = this
        };
        dialog.ShowDialog();
        LoadStreakSettingsToUI();
    }
}
