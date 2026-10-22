using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class ActivityManagementWindow : Window
{
    private readonly ActivityStorage _storage;
    private readonly List<ActivityItem> _activities;
    private readonly List<Border> _swatchElements = new();

    public ActivityManagementWindow(ActivityStorage storage, List<ActivityItem> activities)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);
        _storage = storage;
        _activities = activities;
        KeyDown += ActivityManagementWindow_KeyDown;
        BuildPaletteSwatches();
        RefreshLists();
        if (ActiveActivitiesListBox.Items.Count > 0)
        {
            ActiveActivitiesListBox.SelectedIndex = 0;
        }
        else
        {
            UpdateColorSection(null);
        }
    }

    private void ActivityManagementWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2)
        {
            RenameActivity();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && !NewActivityTextBox.IsFocused && !CustomHexTextBox.IsFocused)
        {
            DeleteActivity();
            e.Handled = true;
        }
    }

    private void BuildPaletteSwatches()
    {
        PaletteSwatchesPanel.Children.Clear();
        _swatchElements.Clear();

        foreach (var (hex, name) in ActivityColorHelper.CuratedPalette)
        {
            var brush = CreateBrush(hex);
            var swatchBorder = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(5),
                Background = brush,
                BorderBrush = TryFindResource("Theme.CardBorder") as Brush ?? Brushes.Gray,
                BorderThickness = new Thickness(1.5),
                Margin = new Thickness(3),
                Cursor = Cursors.Hand,
                ToolTip = $"{name} ({hex})",
                Tag = hex
            };

            swatchBorder.MouseLeftButtonUp += (s, e) =>
            {
                if (s is Border b && b.Tag is string colorHex)
                {
                    SetSelectedActivityColor(colorHex);
                }
            };

            _swatchElements.Add(swatchBorder);
            PaletteSwatchesPanel.Children.Add(swatchBorder);
        }
    }

    private ActivityItem? GetSelectedActivity()
    {
        if (ActivitiesTabControl == null) return null;
        if (ActivitiesTabControl.SelectedIndex <= 0)
            return (ActiveActivitiesListBox?.SelectedItem as ActivityItem) ?? (InactiveActivitiesListBox?.SelectedItem as ActivityItem);
        return (InactiveActivitiesListBox?.SelectedItem as ActivityItem) ?? (ActiveActivitiesListBox?.SelectedItem as ActivityItem);
    }

    private void UpdateColorSection(ActivityItem? activity)
    {
        if (activity == null)
        {
            ColorTargetLabel.Text = "Select an activity above to customize its color (Default: Grey)";
            CurrentColorIndicator.Background = Brushes.Transparent;
            CurrentColorHexLabel.Text = "—";
            CustomHexTextBox.Text = string.Empty;
            CustomHexTextBox.IsEnabled = false;
            ApplyCustomHexButton.IsEnabled = false;
            ResetColorButton.IsEnabled = false;
            PaletteSwatchesPanel.IsEnabled = false;
            UpdateSwatchHighlights(null);
            return;
        }

        CustomHexTextBox.IsEnabled = true;
        ApplyCustomHexButton.IsEnabled = true;
        PaletteSwatchesPanel.IsEnabled = true;
        ResetColorButton.IsEnabled = activity.HasCustomColor;

        var displayHex = activity.DisplayColorHex;
        ColorTargetLabel.Text = $"Color for: {activity.Name}";
        CurrentColorIndicator.Background = activity.Brush;
        CurrentColorHexLabel.Text = activity.HasCustomColor ? $"{displayHex} (Custom)" : $"{displayHex} (Default Grey)";
        CustomHexTextBox.Text = displayHex;

        UpdateSwatchHighlights(displayHex);
    }

    private void UpdateSwatchHighlights(string? activeHex)
    {
        foreach (var border in _swatchElements)
        {
            var hex = border.Tag as string;
            bool isMatch = !string.IsNullOrEmpty(activeHex) && string.Equals(hex, activeHex, StringComparison.OrdinalIgnoreCase);
            if (isMatch)
            {
                border.BorderBrush = TryFindResource("Theme.Foreground") as Brush ?? Brushes.White;
                border.BorderThickness = new Thickness(2.5);
            }
            else
            {
                border.BorderBrush = TryFindResource("Theme.CardBorder") as Brush ?? Brushes.Gray;
                border.BorderThickness = new Thickness(1.5);
            }
        }
    }

    private void SetSelectedActivityColor(string? hex)
    {
        var selected = GetSelectedActivity();
        if (selected == null)
        {
            ColorTargetLabel.Text = "Select an activity above to customize its color";
            return;
        }

        selected.ColorHex = string.IsNullOrWhiteSpace(hex) ? null : ActivityColorHelper.NormalizeHex(hex);
        ActivityColorHelper.SetActivityColor(selected.Name, selected.ColorHex);
        _storage.Save(_activities);

        var selectedName = selected.Name;
        RefreshLists();
        ReselectActivity(selectedName);
    }

    private void ReselectActivity(string name)
    {
        if (ActivitiesTabControl.SelectedIndex <= 0)
        {
            ActiveActivitiesListBox.SelectedItem = _activities.FirstOrDefault(a => a.IsActive && a.Name == name)
                ?? _activities.FirstOrDefault(a => a.Name == name);
        }
        else
        {
            InactiveActivitiesListBox.SelectedItem = _activities.FirstOrDefault(a => !a.IsActive && a.Name == name)
                ?? _activities.FirstOrDefault(a => a.Name == name);
        }
        UpdateColorSection(GetSelectedActivity());
    }

    private void ActivityListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateColorSection(GetSelectedActivity());
    }

    private void ActivitiesTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl)
        {
            UpdateColorSection(GetSelectedActivity());
        }
    }

    private void ActivityListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        RenameActivity();
    }

    private void RenameActivityButton_Click(object sender, RoutedEventArgs e)
    {
        RenameActivity();
    }

    private void RenameActivity()
    {
        var selected = GetSelectedActivity();
        if (selected == null)
        {
            ThemedMessageBox.ShowInfo(this, "Please select an activity to rename.", "Rename Activity");
            return;
        }

        string oldName = selected.Name;
        string? newName = ThemedMessageBox.Prompt(
            this,
            $"Enter a new name for activity \"{oldName}\":",
            "Rename Activity",
            oldName,
            validator: val =>
            {
                if (string.IsNullOrWhiteSpace(val))
                    return "Activity name cannot be empty.";
                if (!val.Trim().Equals(oldName, StringComparison.OrdinalIgnoreCase) &&
                    _activities.Any(a => a.Name.Equals(val.Trim(), StringComparison.OrdinalIgnoreCase)))
                    return $"An activity named \"{val.Trim()}\" already exists.";
                return null;
            });

        if (string.IsNullOrWhiteSpace(newName) || newName.Trim().Equals(oldName, StringComparison.Ordinal))
            return;

        newName = newName.Trim();
        selected.Name = newName;

        if (selected.HasCustomColor)
        {
            ActivityColorHelper.SetActivityColor(newName, selected.ColorHex);
            ActivityColorHelper.RemoveActivityColor(oldName);
        }

        _storage.Save(_activities);

        // Update time entries
        try
        {
            var entriesPath = StoragePathHelper.GetFilePath("time-entries.json");
            if (File.Exists(entriesPath))
            {
                var json = File.ReadAllText(entriesPath);
                var entries = JsonSerializer.Deserialize<List<TimeEntry>>(json);
                if (entries != null)
                {
                    bool modified = false;
                    foreach (var entry in entries)
                    {
                        if (string.Equals(entry.Activity, oldName, StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Activity = newName;
                            modified = true;
                        }
                    }
                    if (modified)
                    {
                        var newJson = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(entriesPath, newJson);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ActivityManagement.RenameActivity.TimeEntries");
        }

        // Update calendar plans
        try
        {
            var plansPath = StoragePathHelper.GetFilePath("calendar-plans.json");
            if (File.Exists(plansPath))
            {
                var json = File.ReadAllText(plansPath);
                var plans = JsonSerializer.Deserialize<List<PlannedWorkItem>>(json);
                if (plans != null)
                {
                    bool modified = false;
                    foreach (var plan in plans)
                    {
                        if (string.Equals(plan.Activity, oldName, StringComparison.OrdinalIgnoreCase))
                        {
                            plan.Activity = newName;
                            modified = true;
                        }
                    }
                    if (modified)
                    {
                        var newJson = JsonSerializer.Serialize(plans, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(plansPath, newJson);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ActivityManagement.RenameActivity.CalendarPlans");
        }

        RefreshLists();
        ReselectActivity(newName);
    }

    private void DeleteActivityButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteActivity();
    }

    private void SyncActivitiesButton_Click(object sender, RoutedEventArgs e)
    {
        SyncActivities();
    }

    private void SyncActivities()
    {
        var added = _storage.SyncActivities(_activities);
        RefreshLists();
        if (added.Count > 0)
        {
            ThemedMessageBox.ShowSuccess(this, $"Synced successfully! Added {added.Count} activities found in your recorded entries and calendar plans: {string.Join(", ", added.Select(a => a.Name))}", "Sync Activities");
        }
        else
        {
            ThemedMessageBox.ShowInfo(this, "All activities from your time entries and calendar plans are already in your activities list.", "Sync Activities");
        }
    }

    private void DeleteActivity()
    {
        var selected = GetSelectedActivity();
        if (selected == null)
        {
            ThemedMessageBox.ShowInfo(this, "Please select an activity to delete.", "Delete Activity");
            return;
        }

        string activityName = selected.Name;
        int entryCount = 0;
        int planCount = 0;

        try
        {
            var entriesPath = StoragePathHelper.GetFilePath("time-entries.json");
            if (File.Exists(entriesPath))
            {
                var json = File.ReadAllText(entriesPath);
                var entries = JsonSerializer.Deserialize<List<TimeEntry>>(json);
                if (entries != null)
                {
                    entryCount = entries.Count(entry => string.Equals(entry.Activity, activityName, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
        catch { }

        try
        {
            var plansPath = StoragePathHelper.GetFilePath("calendar-plans.json");
            if (File.Exists(plansPath))
            {
                var json = File.ReadAllText(plansPath);
                var plans = JsonSerializer.Deserialize<List<PlannedWorkItem>>(json);
                if (plans != null)
                {
                    planCount = plans.Count(p => string.Equals(p.Activity, activityName, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
        catch { }

        string message;
        if (entryCount > 0 || planCount > 0)
        {
            message = $"Activity \"{activityName}\" has {entryCount} recorded time entries and {planCount} calendar plans.\n\nDeleting it removes it from your activity list (historical time entries and plans will retain their activity name in logs).\n\nAre you sure you want to delete this activity?";
        }
        else
        {
            message = $"Are you sure you want to delete activity \"{activityName}\"?";
        }

        bool confirmed = ThemedMessageBox.ConfirmDelete(
            this,
            message,
            $"Delete Activity: {activityName}",
            "Delete Activity",
            "Cancel");

        if (!confirmed) return;

        _activities.Remove(selected);
        ActivityColorHelper.RemoveActivityColor(activityName);
        _storage.Save(_activities);

        RefreshLists();
        UpdateColorSection(GetSelectedActivity());
    }

    private void ApplyCustomHexButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyCustomHex();
    }

    private void CustomHexTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyCustomHex();
        }
    }

    private void ApplyCustomHex()
    {
        var input = CustomHexTextBox.Text.Trim();
        if (!ActivityColorHelper.IsValidHex(input))
        {
            ThemedMessageBox.ShowWarning(this, "Please enter a valid HEX color code (e.g., #10B981 or 10B981).", "Invalid Color Code");
            return;
        }

        SetSelectedActivityColor(input);
    }

    private void OpenColorWheelButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedActivity();
        if (selected == null)
        {
            ThemedMessageBox.ShowWarning(this, "Please select an activity first to choose a color.", "Select Activity");
            return;
        }

        var currentColor = selected.ColorHex ?? ActivityColorHelper.DefaultActivityColorHex;
        var chosenHex = ColorPickerDialog.PickColor(this, currentColor, $"🎨 Color for '{selected.Name}'");
        if (!string.IsNullOrWhiteSpace(chosenHex))
        {
            SetSelectedActivityColor(chosenHex);
        }
    }

    private void ResetColorButton_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedActivityColor(null);
    }

    private void RefreshLists()
    {
        var active = _activities.Where(a => a.IsActive).OrderBy(a => a.Name).ToList();
        ActiveActivitiesListBox.ItemsSource = null;
        ActiveActivitiesListBox.ItemsSource = active;
        ActiveActivitiesEmptyState.Visibility = active.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var inactive = _activities.Where(a => !a.IsActive).OrderBy(a => a.Name).ToList();
        InactiveActivitiesListBox.ItemsSource = null;
        InactiveActivitiesListBox.ItemsSource = inactive;
        InactiveActivitiesEmptyState.Visibility = inactive.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddActivityButton_Click(object sender, RoutedEventArgs e)
    {
        AddActivity();
    }

    private void NewActivityTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddActivity();
        }
    }

    private void AddActivity()
    {
        var name = NewActivityTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ThemedMessageBox.ShowWarning(this, "Please enter an activity name.", "Invalid Name");
            return;
        }

        if (_activities.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            ThemedMessageBox.ShowWarning(this, "An activity with this name already exists.", "Duplicate Activity");
            return;
        }

        var newActivity = new ActivityItem
        {
            Name = name,
            IsActive = true
        };

        _activities.Add(newActivity);
        _storage.Save(_activities);

        NewActivityTextBox.Clear();
        RefreshLists();
        ActiveActivitiesListBox.SelectedItem = newActivity;
        ActivitiesTabControl.SelectedIndex = 0;
    }

    private void ArchiveActivityButton_Click(object sender, RoutedEventArgs e)
    {
        if (ActiveActivitiesListBox.SelectedItem is ActivityItem selected)
        {
            selected.IsActive = false;
            _storage.Save(_activities);
            RefreshLists();
            UpdateColorSection(GetSelectedProjectActivity());
        }
        else
        {
            ThemedMessageBox.ShowInfo(this, "Please select an active activity to archive.", "No Selection");
        }
    }

    private ActivityItem? GetSelectedProjectActivity() => GetSelectedActivity();

    private void ActivateActivityButton_Click(object sender, RoutedEventArgs e)
    {
        if (InactiveActivitiesListBox.SelectedItem is ActivityItem selected)
        {
            selected.IsActive = true;
            _storage.Save(_activities);
            RefreshLists();
            UpdateColorSection(GetSelectedProjectActivity());
        }
        else
        {
            ThemedMessageBox.ShowInfo(this, "Please select an inactive activity to activate.", "No Selection");
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private static SolidColorBrush CreateBrush(string hex)
    {
        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
        catch
        {
            var fallback = new SolidColorBrush(Colors.MediumSeaGreen);
            fallback.Freeze();
            return fallback;
        }
    }
}
