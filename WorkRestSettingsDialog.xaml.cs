using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking;

public partial class WorkRestSettingsDialog : Window
{
    public WorkRestSettings Settings { get; private set; }
    private bool _isUpdatingUi = false;

    private readonly List<IntervalPreset> _presets = new()
    {
        new()
        {
            Name = "🍅 Standard Pomodoro (25/5)",
            Description = "Classic focus technique: 25 minutes of sprint work followed by 5 minutes of rest, with a 15-minute long break every 4 cycles.",
            FocusMinutes = 25,
            ShortBreakMinutes = 5,
            LongBreakMinutes = 15,
            CyclesBeforeLongBreak = 4
        },
        new()
        {
            Name = "⚡ Deep Focus (50/10)",
            Description = "Ideal for complex coding and deep thinking: 50 minutes of undisturbed focus followed by 10 minutes of rest, with 20 minutes long break every 3 cycles.",
            FocusMinutes = 50,
            ShortBreakMinutes = 10,
            LongBreakMinutes = 20,
            CyclesBeforeLongBreak = 3
        },
        new()
        {
            Name = "📚 Study With Me (45/15)",
            Description = "Popular YouTube study-with-me style: 45 minutes of studying followed by a relaxing 15-minute tea break, and 30-minute long break every 3 cycles.",
            FocusMinutes = 45,
            ShortBreakMinutes = 15,
            LongBreakMinutes = 30,
            CyclesBeforeLongBreak = 3
        },
        new()
        {
            Name = "🚀 Fast Sprint (20/5)",
            Description = "High-energy fast-paced sprints: 20 minutes of quick tasks followed by 5 minutes rest, with 15 minutes long break every 4 cycles.",
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
        Settings = new WorkRestSettings
        {
            IntervalModeEnabled = currentSettings.IntervalModeEnabled,
            FocusMinutes = currentSettings.FocusMinutes,
            ShortBreakMinutes = currentSettings.ShortBreakMinutes,
            LongBreakMinutes = currentSettings.LongBreakMinutes,
            CyclesBeforeLongBreak = currentSettings.CyclesBeforeLongBreak,
            SoundAlertEnabled = currentSettings.SoundAlertEnabled,
            AutoAdvancePhases = currentSettings.AutoAdvancePhases,
            SelectedSceneId = currentSettings.SelectedSceneId,
            CompanionVisible = currentSettings.CompanionVisible,
            CompanionExpanded = currentSettings.CompanionExpanded,
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

        FocusSlider.Value = Settings.FocusMinutes;
        ShortBreakSlider.Value = Settings.ShortBreakMinutes;
        LongBreakSlider.Value = Settings.LongBreakMinutes;
        CyclesSlider.Value = Settings.CyclesBeforeLongBreak;

        SoundAlertCheckBox.IsChecked = Settings.SoundAlertEnabled;
        AutoAdvanceCheckBox.IsChecked = Settings.AutoAdvancePhases;
        CompanionVisibleCheckBox.IsChecked = Settings.CompanionVisible;

        UpdateLabels();
        SelectMatchingPreset();

        _isUpdatingUi = false;
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
        FocusValueText.Text = $"{FocusSlider.Value:F0} min";
        ShortBreakValueText.Text = $"{ShortBreakSlider.Value:F0} min";
        LongBreakValueText.Text = $"{LongBreakSlider.Value:F0} min";
        CyclesValueText.Text = $"{CyclesSlider.Value:F0} cycles";
    }

    private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || PresetComboBox.SelectedItem is not IntervalPreset preset)
            return;

        PresetDescriptionText.Text = preset.Description;

        if (preset.Name.Contains("Custom"))
            return;

        _isUpdatingUi = true;
        FocusSlider.Value = preset.FocusMinutes;
        ShortBreakSlider.Value = preset.ShortBreakMinutes;
        LongBreakSlider.Value = preset.LongBreakMinutes;
        CyclesSlider.Value = preset.CyclesBeforeLongBreak;
        UpdateLabels();
        _isUpdatingUi = false;
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;

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
        Settings.FocusMinutes = (int)FocusSlider.Value;
        Settings.ShortBreakMinutes = (int)ShortBreakSlider.Value;
        Settings.LongBreakMinutes = (int)LongBreakSlider.Value;
        Settings.CyclesBeforeLongBreak = (int)CyclesSlider.Value;
        Settings.SoundAlertEnabled = SoundAlertCheckBox.IsChecked ?? true;
        Settings.AutoAdvancePhases = AutoAdvanceCheckBox.IsChecked ?? false;
        Settings.CompanionVisible = CompanionVisibleCheckBox.IsChecked ?? true;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
