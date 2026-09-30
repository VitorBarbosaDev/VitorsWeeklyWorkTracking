using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class EditTaskDialog : Window
{
    private readonly TimeEntry _entry;
    private readonly List<Project> _projects;
    private readonly List<ActivityItem> _activities;
    private bool _isInitializing = true;

    public EditTaskDialog(TimeEntry entry, List<Project> projects, List<ActivityItem> activities)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _entry = entry;
        _projects = projects ?? new List<Project>();
        _activities = activities ?? new List<ActivityItem>();

        PopulateFields();
        _isInitializing = false;
        UpdateDurationPreview();
    }

    private void PopulateFields()
    {
        // 1. Projects dropdown
        ProjectComboBox.Items.Clear();
        var activeProjects = _projects.Where(p => p.IsActive).Select(p => p.Name).ToList();
        foreach (var pName in activeProjects)
        {
            ProjectComboBox.Items.Add(pName);
        }

        if (!string.IsNullOrWhiteSpace(_entry.ProjectName) && !ProjectComboBox.Items.Contains(_entry.ProjectName))
        {
            ProjectComboBox.Items.Add(_entry.ProjectName);
        }
        ProjectComboBox.SelectedItem = _entry.ProjectName;
        ProjectComboBox.Text = _entry.ProjectName ?? string.Empty;

        // 2. Activities dropdown
        ActivityComboBox.Items.Clear();
        var activeActivities = _activities.Where(a => a.IsActive).Select(a => a.Name).ToList();
        foreach (var aName in activeActivities)
        {
            ActivityComboBox.Items.Add(aName);
        }

        if (!string.IsNullOrWhiteSpace(_entry.Activity) && !ActivityComboBox.Items.Contains(_entry.Activity))
        {
            ActivityComboBox.Items.Add(_entry.Activity);
        }
        ActivityComboBox.SelectedItem = _entry.Activity;
        ActivityComboBox.Text = _entry.Activity ?? string.Empty;

        // 3. Description & Notes
        DescriptionTextBox.Text = _entry.Description ?? string.Empty;
        NoteTextBox.Text = _entry.Note ?? string.Empty;

        // 4. Start & End Times
        var start = _entry.StartTime != default ? _entry.StartTime : DateTime.Now.AddHours(-1);
        var end = _entry.EndTime != default ? _entry.EndTime : DateTime.Now;

        StartDatePicker.SelectedDate = start.Date;
        StartTimeTextBox.Text = start.ToString("HH:mm:ss");

        EndDatePicker.SelectedDate = end.Date;
        EndTimeTextBox.Text = end.ToString("HH:mm:ss");
    }

    private bool TryParseInputs(out DateTime startDateTime, out DateTime endDateTime, out string errorMessage)
    {
        startDateTime = default;
        endDateTime = default;
        errorMessage = string.Empty;

        var startDate = StartDatePicker.SelectedDate ?? DateTime.Today;
        var endDate = EndDatePicker.SelectedDate ?? startDate;

        if (!TryParseTimeOfDay(StartTimeTextBox.Text.Trim(), out var startTimeOfDay))
        {
            errorMessage = "Invalid Start Time. Expected HH:mm:ss (e.g. 09:30:00 or 14:15)";
            return false;
        }

        if (!TryParseTimeOfDay(EndTimeTextBox.Text.Trim(), out var endTimeOfDay))
        {
            errorMessage = "Invalid End Time. Expected HH:mm:ss (e.g. 10:45:00 or 17:30)";
            return false;
        }

        startDateTime = startDate.Date.Add(startTimeOfDay);
        endDateTime = endDate.Date.Add(endTimeOfDay);

        if (endDateTime < startDateTime)
        {
            errorMessage = "End time cannot be earlier than start time";
            return false;
        }

        return true;
    }

    private static bool TryParseTimeOfDay(string input, out TimeSpan timeOfDay)
    {
        timeOfDay = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string[] formats =
        {
            "h\\:mm\\:ss", "hh\\:mm\\:ss", "H\\:mm\\:ss", "HH\\:mm\\:ss",
            "h\\:mm", "hh\\:mm", "H\\:mm", "HH\\:mm",
            "g", "G"
        };

        if (TimeSpan.TryParseExact(input, formats, CultureInfo.InvariantCulture, out timeOfDay))
        {
            if (timeOfDay.TotalHours >= 24)
                return false;
            return true;
        }

        if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var dt)
            || DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out dt))
        {
            timeOfDay = dt.TimeOfDay;
            return true;
        }

        return false;
    }

    private void TimeInputs_Changed(object? sender, EventArgs e)
    {
        if (_isInitializing)
            return;

        UpdateDurationPreview();
    }

    private void UpdateDurationPreview()
    {
        if (TryParseInputs(out var start, out var end, out var error))
        {
            var duration = end - start;
            DurationPreviewText.Text = $"⏱️ Calculated Duration: {duration:hh\\:mm\\:ss} ({duration.TotalHours:F2} hrs)";
            DurationPreviewText.Foreground = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
            DurationPreviewBorder.BorderBrush = TryFindResource("Theme.Primary") as Brush ?? Brushes.DodgerBlue;
            ValidationStatusText.Text = string.Empty;
        }
        else
        {
            DurationPreviewText.Text = $"⚠️ {error}";
            DurationPreviewText.Foreground = TryFindResource("Theme.Danger") as Brush ?? Brushes.Crimson;
            DurationPreviewBorder.BorderBrush = TryFindResource("Theme.Danger") as Brush ?? Brushes.Crimson;
            ValidationStatusText.Text = error;
        }
    }

    private void AddMinutesToEnd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int minutes))
        {
            if (TryParseInputs(out var start, out var end, out _))
            {
                var newEnd = end.AddMinutes(minutes);
                EndDatePicker.SelectedDate = newEnd.Date;
                EndTimeTextBox.Text = newEnd.ToString("HH:mm:ss");
            }
            else if (StartDatePicker.SelectedDate != null && TryParseTimeOfDay(StartTimeTextBox.Text.Trim(), out var startTod))
            {
                var newEnd = StartDatePicker.SelectedDate.Value.Date.Add(startTod).AddMinutes(minutes);
                EndDatePicker.SelectedDate = newEnd.Date;
                EndTimeTextBox.Text = newEnd.ToString("HH:mm:ss");
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseInputs(out var startDateTime, out var endDateTime, out var error))
        {
            ThemedMessageBox.ShowWarning(this, error, "Invalid Time Period");
            return;
        }

        var selectedProject = ProjectComboBox.SelectedItem as Project;
        var projectName = selectedProject?.Name ?? ProjectComboBox.Text?.Trim() ?? string.Empty;

        var selectedActivity = ActivityComboBox.SelectedItem as ActivityItem;
        var activity = selectedActivity?.Name ?? ActivityComboBox.Text?.Trim() ?? string.Empty;

        _entry.ProjectName = projectName;
        _entry.Activity = activity;
        _entry.Description = DescriptionTextBox.Text?.Trim() ?? string.Empty;
        _entry.Note = NoteTextBox.Text?.Trim() ?? string.Empty;
        _entry.StartTime = startDateTime;
        _entry.EndTime = endDateTime;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
