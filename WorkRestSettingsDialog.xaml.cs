using System;
using System.Collections.Generic;
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
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);
        Settings = new WorkRestSettings
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
            FocusXp = currentSettings.FocusXp,
            FocusStreak = currentSettings.FocusStreak,
            TotalSessionsCompleted = currentSettings.TotalSessionsCompleted
        };

        InitializeForm();
    }

    private void InitializeForm()
    {
        _isUpdatingUi = true;

        PresetComboBox.ItemsSource = _presets;

        UserNameTextBox.Text = Settings.UserName ?? string.Empty;
        WelcomePopupEnabledCheckBox.IsChecked = Settings.WelcomePopupEnabled;

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

        _isUpdatingUi = false;
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

    private void TestVolumeButton_Click(object sender, RoutedEventArgs e)
    {
        int vol = (int)(VolumeSlider?.Value ?? 75);
        SoundEffectManager.PlayPreview(SoundEffectType.Start, vol);
    }

    private void TestVariationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string varId)
        {
            int vol = (int)(VolumeSlider?.Value ?? 75);
            SoundEffectManager.PlayVariationPreview(varId, vol);
        }
    }

    private void TestEventButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string eventTypeStr && Enum.TryParse<SoundEffectType>(eventTypeStr, out var eventType))
        {
            int vol = (int)(VolumeSlider?.Value ?? 75);
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
        Settings.IntervalModeEnabled = IntervalModeEnabledCheckBox.IsChecked ?? true;
        Settings.UserName = UserNameTextBox.Text.Trim();
        Settings.WelcomePopupEnabled = WelcomePopupEnabledCheckBox.IsChecked ?? true;
        Settings.FocusMinutes = (int)FocusSlider.Value;
        Settings.ShortBreakMinutes = (int)ShortBreakSlider.Value;
        Settings.LongBreakMinutes = (int)LongBreakSlider.Value;
        Settings.CyclesBeforeLongBreak = (int)CyclesSlider.Value;

        Settings.SoundAlertEnabled = SoundAlertCheckBox.IsChecked ?? true;
        Settings.SoundVolume = (int)VolumeSlider.Value;
        Settings.SoundOnStartEnabled = SoundOnStartCheckBox.IsChecked ?? true;
        Settings.SoundOnEndEnabled = SoundOnEndCheckBox.IsChecked ?? true;
        Settings.SoundOnGoalAchievedEnabled = SoundOnGoalCheckBox.IsChecked ?? true;
        Settings.SoundOnDailyGoalAchievedEnabled = SoundOnDailyGoalCheckBox.IsChecked ?? true;
        Settings.SoundOnWeeklyGoalAchievedEnabled = SoundOnWeeklyGoalCheckBox.IsChecked ?? true;
        Settings.SoundOnBreakStartEnabled = SoundOnBreakStartCheckBox.IsChecked ?? true;
        Settings.SoundOnBreakEndEnabled = SoundOnBreakEndCheckBox.IsChecked ?? true;
        Settings.SoundOnCheerEnabled = SoundOnCheerCheckBox.IsChecked ?? true;

        Settings.EnabledSoundVariations = new List<string>();
        ReadVariationCheckState(Var_start_ascending, "start_ascending");
        ReadVariationCheckState(Var_start_pop, "start_pop");
        ReadVariationCheckState(Var_start_marimba, "start_marimba");
        ReadVariationCheckState(Var_start_digital, "start_digital");

        ReadVariationCheckState(Var_end_completion, "end_completion");
        ReadVariationCheckState(Var_end_accomplished, "end_accomplished");
        ReadVariationCheckState(Var_end_sparkle, "end_sparkle");
        ReadVariationCheckState(Var_end_calm, "end_calm");

        ReadVariationCheckState(Var_goal_victory, "goal_victory");
        ReadVariationCheckState(Var_goal_levelup, "goal_levelup");
        ReadVariationCheckState(Var_goal_bell, "goal_bell");
        ReadVariationCheckState(Var_goal_cosmic, "goal_cosmic");

        ReadVariationCheckState(Var_daily_triumph, "daily_triumph");
        ReadVariationCheckState(Var_daily_sunburst, "daily_sunburst");
        ReadVariationCheckState(Var_daily_chime, "daily_chime");
        ReadVariationCheckState(Var_daily_quest, "daily_quest");

        ReadVariationCheckState(Var_weekly_grand, "weekly_grand");
        ReadVariationCheckState(Var_weekly_champions, "weekly_champions");
        ReadVariationCheckState(Var_weekly_fiesta, "weekly_fiesta");
        ReadVariationCheckState(Var_weekly_celestial, "weekly_celestial");

        ReadVariationCheckState(Var_breakstart_zen, "breakstart_zen");
        ReadVariationCheckState(Var_breakstart_lofi, "breakstart_lofi");
        ReadVariationCheckState(Var_breakstart_bowl, "breakstart_bowl");
        ReadVariationCheckState(Var_breakstart_waterdrop, "breakstart_waterdrop");

        ReadVariationCheckState(Var_breakend_wakeup, "breakend_wakeup");
        ReadVariationCheckState(Var_breakend_ready, "breakend_ready");
        ReadVariationCheckState(Var_breakend_morning, "breakend_morning");
        ReadVariationCheckState(Var_breakend_focusbell, "breakend_focusbell");

        // Scene cheer variations
        ReadVariationCheckState(Var_cheer_cycling_bell, "cheer_cycling_bell");
        ReadVariationCheckState(Var_cheer_cycling_bark, "cheer_cycling_bark");
        ReadVariationCheckState(Var_cheer_cycling_whistle, "cheer_cycling_whistle");

        ReadVariationCheckState(Var_cheer_cafe_cup, "cheer_cafe_cup");
        ReadVariationCheckState(Var_cheer_cafe_raindrop, "cheer_cafe_raindrop");
        ReadVariationCheckState(Var_cheer_cafe_steam, "cheer_cafe_steam");

        ReadVariationCheckState(Var_cheer_jazz_sax, "cheer_jazz_sax");
        ReadVariationCheckState(Var_cheer_jazz_piano, "cheer_jazz_piano");
        ReadVariationCheckState(Var_cheer_jazz_sparkle, "cheer_jazz_sparkle");

        ReadVariationCheckState(Var_cheer_icecream_jingle, "cheer_icecream_jingle");
        ReadVariationCheckState(Var_cheer_icecream_pop, "cheer_icecream_pop");
        ReadVariationCheckState(Var_cheer_icecream_bell, "cheer_icecream_bell");

        ReadVariationCheckState(Var_cheer_metro_melody, "cheer_metro_melody");
        ReadVariationCheckState(Var_cheer_metro_lofi, "cheer_metro_lofi");
        ReadVariationCheckState(Var_cheer_metro_doors, "cheer_metro_doors");

        ReadVariationCheckState(Var_cheer_cat_meow, "cheer_cat_meow");
        ReadVariationCheckState(Var_cheer_cat_purr, "cheer_cat_purr");
        ReadVariationCheckState(Var_cheer_cat_bounce, "cheer_cat_bounce");

        ReadVariationCheckState(Var_cheer_rocket_laser, "cheer_rocket_laser");
        ReadVariationCheckState(Var_cheer_rocket_beacon, "cheer_rocket_beacon");
        ReadVariationCheckState(Var_cheer_rocket_warp, "cheer_rocket_warp");

        ReadVariationCheckState(Var_cheer_runner_whistle, "cheer_runner_whistle");
        ReadVariationCheckState(Var_cheer_runner_horn, "cheer_runner_horn");
        ReadVariationCheckState(Var_cheer_runner_squeak, "cheer_runner_squeak");

        ReadVariationCheckState(Var_cheer_pet_happy, "cheer_pet_happy");
        ReadVariationCheckState(Var_cheer_pet_levelup, "cheer_pet_levelup");
        ReadVariationCheckState(Var_cheer_pet_heart, "cheer_pet_heart");

        Settings.AutoAdvancePhases = AutoAdvanceCheckBox.IsChecked ?? false;
        Settings.CompanionVisible = CompanionVisibleCheckBox.IsChecked ?? true;

        if (MiniMonitorComboBox.SelectedItem is MonitorDisplayInfo mon)
        {
            Settings.MiniWidgetTargetMonitor = mon.DeviceName;
            Settings.MiniWidgetMonitorIndex = mon.Index;
        }

        Settings.MiniWidgetCorner = MiniCornerComboBox.SelectedIndex switch
        {
            1 => "BottomLeft",
            2 => "TopRight",
            3 => "TopLeft",
            _ => "BottomRight"
        };
        Settings.MiniWidgetEnabled = MiniTimerEnabledCheckBox.IsChecked ?? true;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
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
}
