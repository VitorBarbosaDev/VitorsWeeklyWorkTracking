using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class TaskDetailsDialog : Window
{
    private readonly TimeEntry _entry;
    private readonly List<Project> _projects;
    private readonly List<ActivityItem> _activities;
    private DispatcherTimer? _copyFeedbackTimer;

    public bool WasModified { get; private set; } = false;

    public TaskDetailsDialog(TimeEntry entry, List<Project> projects, List<ActivityItem> activities)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _entry = entry;
        _projects = projects ?? new List<Project>();
        _activities = activities ?? new List<ActivityItem>();

        PopulateDetails();
    }

    public void PopulateDetails()
    {
        // Project badge
        ProjectBadgeText.Text = string.IsNullOrWhiteSpace(_entry.ProjectName)
            ? "Unassigned Project"
            : _entry.ProjectName;

        // Activity badge
        if (string.IsNullOrWhiteSpace(_entry.Activity))
        {
            ActivityBadgeBorder.Visibility = Visibility.Collapsed;
        }
        else
        {
            ActivityBadgeBorder.Visibility = Visibility.Visible;
            ActivityBadgeText.Text = _entry.Activity;
        }

        // Description
        DescriptionText.Text = string.IsNullOrWhiteSpace(_entry.Description)
            ? "(No description specified)"
            : _entry.Description;

        // Timing
        DateText.Text = _entry.StartTime.ToString("dddd, dd MMMM yyyy");
        DurationText.Text = $"{_entry.Duration:hh\\:mm\\:ss} ({_entry.Duration.TotalHours:F2} hrs)";
        StartTimeText.Text = _entry.StartTime.ToString("HH:mm:ss");
        EndTimeText.Text = _entry.EndTime.ToString("HH:mm:ss");

        // Notes
        if (string.IsNullOrWhiteSpace(_entry.Note))
        {
            NotesText.Text = "(No notes recorded for this task)";
            NotesText.FontStyle = FontStyles.Italic;
            NotesText.Foreground = TryFindResource("Theme.ForegroundSubtle") as Brush ?? Brushes.Gray;
            CopyNotesButton.Content = "📋 Copy Summary";
        }
        else
        {
            NotesText.Text = _entry.Note;
            NotesText.FontStyle = FontStyles.Normal;
            NotesText.Foreground = TryFindResource("Theme.Foreground") as Brush ?? Brushes.Black;
            CopyNotesButton.Content = "📋 Copy Notes";
        }
    }

    private void CopyNotesButton_Click(object sender, RoutedEventArgs e)
    {
        string textToCopy;
        if (!string.IsNullOrWhiteSpace(_entry.Note))
        {
            textToCopy = _entry.Note;
        }
        else
        {
            textToCopy = $"Task: {_entry.Description}\nProject: {_entry.ProjectName}\nActivity: {_entry.Activity}\nDate: {_entry.StartTime:yyyy-MM-dd}\nTime: {_entry.StartTime:HH:mm:ss} - {_entry.EndTime:HH:mm:ss} ({_entry.Duration:hh\\:mm\\:ss})";
        }

        try
        {
            Clipboard.SetText(textToCopy);
            CopyNotesButton.Content = "✓ Copied!";

            _copyFeedbackTimer?.Stop();
            _copyFeedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _copyFeedbackTimer.Tick += (s, args) =>
            {
                _copyFeedbackTimer.Stop();
                CopyNotesButton.Content = string.IsNullOrWhiteSpace(_entry.Note) ? "📋 Copy Summary" : "📋 Copy Notes";
            };
            _copyFeedbackTimer.Start();
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "TaskDetailsDialog.CopyNotes");
        }
    }

    private void EditTaskButton_Click(object sender, RoutedEventArgs e)
    {
        var editDialog = new EditTaskDialog(_entry, _projects, _activities)
        {
            Owner = this
        };

        if (editDialog.ShowDialog() == true)
        {
            WasModified = true;
            PopulateDetails();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
