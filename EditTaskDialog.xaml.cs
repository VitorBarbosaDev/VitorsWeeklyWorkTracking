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
        var projectList = _projects.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
        if (projectList.Count == 0 && _projects.Count > 0)
        {
            projectList = _projects.OrderBy(p => p.Name).ToList();
        }

        if (!string.IsNullOrWhiteSpace(_entry.ProjectName))
        {
            var match = projectList.FirstOrDefault(p => p.Name.Equals(_entry.ProjectName, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                var inactiveMatch = _projects.FirstOrDefault(p => p.Name.Equals(_entry.ProjectName, StringComparison.OrdinalIgnoreCase));
                if (inactiveMatch != null)
                {
                    projectList.Insert(0, inactiveMatch);
                }
                else
                {
                    var customProject = new Project { Name = _entry.ProjectName, IsActive = true };
                    projectList.Insert(0, customProject);
                }
            }
        }

        ProjectComboBox.ItemsSource = projectList;
        if (!string.IsNullOrWhiteSpace(_entry.ProjectName))
        {
            var selected = projectList.FirstOrDefault(p => p.Name.Equals(_entry.ProjectName, StringComparison.OrdinalIgnoreCase));
            if (selected != null)
            {
                ProjectComboBox.SelectedItem = selected;
            }
            ProjectComboBox.Text = _entry.ProjectName;
        }
        else if (projectList.Count > 0)
        {
            ProjectComboBox.SelectedIndex = 0;
        }

        // 2. Activities dropdown
        var activityList = _activities.Where(a => a.IsActive).OrderBy(a => a.Name).ToList();
        if (activityList.Count == 0 && _activities.Count > 0)
        {
            activityList = _activities.OrderBy(a => a.Name).ToList();
        }

        if (!string.IsNullOrWhiteSpace(_entry.Activity))
        {
            var match = activityList.FirstOrDefault(a => a.Name.Equals(_entry.Activity, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                var inactiveMatch = _activities.FirstOrDefault(a => a.Name.Equals(_entry.Activity, StringComparison.OrdinalIgnoreCase));
                if (inactiveMatch != null)
                {
                    activityList.Insert(0, inactiveMatch);
                }
                else
                {
                    var customActivity = new ActivityItem { Name = _entry.Activity, IsActive = true };
                    activityList.Insert(0, customActivity);
                }
            }
        }

        ActivityComboBox.ItemsSource = activityList;
        if (!string.IsNullOrWhiteSpace(_entry.Activity))
        {
            var selected = activityList.FirstOrDefault(a => a.Name.Equals(_entry.Activity, StringComparison.OrdinalIgnoreCase));
            if (selected != null)
            {
                ActivityComboBox.SelectedItem = selected;
            }
            ActivityComboBox.Text = _entry.Activity;
        }
        else if (activityList.Count > 0)
        {
            ActivityComboBox.SelectedIndex = 0;
        }

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

        string projectName;
        if (ProjectComboBox.SelectedItem is Project p && (string.IsNullOrWhiteSpace(ProjectComboBox.Text) || string.Equals(p.Name, ProjectComboBox.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            projectName = p.Name;
        }
        else
        {
            projectName = ProjectComboBox.Text?.Trim() ?? string.Empty;
        }

        string activityName;
        if (ActivityComboBox.SelectedItem is ActivityItem a && (string.IsNullOrWhiteSpace(ActivityComboBox.Text) || string.Equals(a.Name, ActivityComboBox.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            activityName = a.Name;
        }
        else
        {
            activityName = ActivityComboBox.Text?.Trim() ?? string.Empty;
        }

        _entry.ProjectName = projectName;
        _entry.Activity = activityName;
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
