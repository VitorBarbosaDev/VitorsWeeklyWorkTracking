using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class EditMilestoneDialog : Window
{
    private readonly ProjectMilestone? _originalMilestone;
    private readonly List<Project> _projects;

    public ProjectMilestone? ResultMilestone { get; private set; }
    public bool IsDeleted { get; private set; }

    public EditMilestoneDialog(string? preselectedProject = null, DateTime? initialDate = null, List<Project>? projects = null)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _projects = projects ?? new ProjectStorage().Load().Where(p => p.IsActive).ToList();
        ProjectComboBox.ItemsSource = _projects;

        if (!string.IsNullOrWhiteSpace(preselectedProject))
        {
            var match = _projects.FirstOrDefault(p => p.Name.Equals(preselectedProject.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                ProjectComboBox.SelectedItem = match;
            }
            else
            {
                ProjectComboBox.Text = preselectedProject.Trim();
            }
        }
        else if (_projects.Count > 0)
        {
            ProjectComboBox.SelectedIndex = 0;
        }

        TargetDatePicker.SelectedDate = initialDate ?? DateTime.Today.AddDays(14);
        CompletedCheckBox.Visibility = Visibility.Collapsed;
        DeleteButton.Visibility = Visibility.Collapsed;

        HeaderTitleTextBlock.Text = "Add Project Milestone";
        HeaderSubtitleTextBlock.Text = "Set important project dates and daily countdown reminders";
        HeaderIconTextBlock.Text = "🚩";

        UpdateCountdownPreview();
    }

    public EditMilestoneDialog(ProjectMilestone milestone, List<Project>? projects = null)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _originalMilestone = milestone;
        _projects = projects ?? new ProjectStorage().Load();
        ProjectComboBox.ItemsSource = _projects;

        var match = _projects.FirstOrDefault(p => p.Name.Equals(milestone.ProjectName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            ProjectComboBox.SelectedItem = match;
        }
        else
        {
            ProjectComboBox.Text = milestone.ProjectName;
        }

        TitleTextBox.Text = milestone.Title;
        TargetDatePicker.SelectedDate = milestone.TargetDate;
        DescriptionTextBox.Text = milestone.Description ?? string.Empty;
        CompletedCheckBox.IsChecked = milestone.IsCompleted;
        CompletedCheckBox.Visibility = Visibility.Visible;
        DeleteButton.Visibility = Visibility.Visible;

        HeaderTitleTextBlock.Text = "Edit Project Milestone";
        HeaderSubtitleTextBlock.Text = $"Update milestone details for {milestone.ProjectName}";
        HeaderIconTextBlock.Text = "✏️";

        UpdateCountdownPreview();
    }

    private void TargetDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCountdownPreview();
    }

    private void CompletedCheckBox_CheckChanged(object sender, RoutedEventArgs e)
    {
        UpdateCountdownPreview();
    }

    private void UpdateCountdownPreview()
    {
        if (CountdownPreviewTextBlock == null) return;

        if (CompletedCheckBox.IsChecked == true)
        {
            CountdownPreviewTextBlock.Text = "✅ Completed";
            var successBrush = TryFindResource("Theme.Success") as Brush ?? new SolidColorBrush(Color.FromRgb(16, 185, 129));
            CountdownPreviewTextBlock.Foreground = successBrush;
            if (CountdownBadge != null)
            {
                CountdownBadge.Background = new SolidColorBrush(Color.FromArgb(35, 16, 185, 129));
                CountdownBadge.BorderBrush = successBrush;
            }
            return;
        }

        var target = TargetDatePicker.SelectedDate ?? DateTime.Today;
        int days = (target.Date - DateTime.Today).Days;

        if (days < 0)
        {
            CountdownPreviewTextBlock.Text = $"🚨 {Math.Abs(days)} day{(Math.Abs(days) == 1 ? "" : "s")} overdue";
            var dangerBrush = TryFindResource("Theme.Danger") as Brush ?? new SolidColorBrush(Color.FromRgb(239, 68, 68));
            CountdownPreviewTextBlock.Foreground = dangerBrush;
            if (CountdownBadge != null)
            {
                CountdownBadge.Background = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68));
                CountdownBadge.BorderBrush = dangerBrush;
            }
        }
        else if (days == 0)
        {
            CountdownPreviewTextBlock.Text = "🔥 Due today!";
            var dangerBrush = TryFindResource("Theme.Danger") as Brush ?? new SolidColorBrush(Color.FromRgb(239, 68, 68));
            CountdownPreviewTextBlock.Foreground = dangerBrush;
            if (CountdownBadge != null)
            {
                CountdownBadge.Background = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68));
                CountdownBadge.BorderBrush = dangerBrush;
            }
        }
        else if (days == 1)
        {
            CountdownPreviewTextBlock.Text = "⏰ 1 day remaining (tomorrow!)";
            var dangerBrush = TryFindResource("Theme.Danger") as Brush ?? new SolidColorBrush(Color.FromRgb(239, 68, 68));
            CountdownPreviewTextBlock.Foreground = dangerBrush;
            if (CountdownBadge != null)
            {
                CountdownBadge.Background = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68));
                CountdownBadge.BorderBrush = dangerBrush;
            }
        }
        else if (days <= 3)
        {
            CountdownPreviewTextBlock.Text = $"⏳ {days} days remaining";
            var warningBrush = TryFindResource("Theme.Warning") as Brush ?? new SolidColorBrush(Color.FromRgb(249, 115, 22));
            CountdownPreviewTextBlock.Foreground = warningBrush;
            if (CountdownBadge != null)
            {
                CountdownBadge.Background = new SolidColorBrush(Color.FromArgb(35, 249, 115, 22));
                CountdownBadge.BorderBrush = warningBrush;
            }
        }
        else if (days <= 7)
        {
            CountdownPreviewTextBlock.Text = $"⏳ {days} days remaining";
            var amberBrush = new SolidColorBrush(Color.FromRgb(234, 179, 8));
            CountdownPreviewTextBlock.Foreground = amberBrush;
            if (CountdownBadge != null)
            {
                CountdownBadge.Background = new SolidColorBrush(Color.FromArgb(35, 234, 179, 8));
                CountdownBadge.BorderBrush = amberBrush;
            }
        }
        else
        {
            CountdownPreviewTextBlock.Text = $"📅 {days} days remaining";
            var primaryBrush = TryFindResource("Theme.Primary") as Brush ?? new SolidColorBrush(Color.FromRgb(59, 130, 246));
            CountdownPreviewTextBlock.Foreground = primaryBrush;
            if (CountdownBadge != null)
            {
                CountdownBadge.Background = new SolidColorBrush(Color.FromArgb(30, 59, 130, 246));
                CountdownBadge.BorderBrush = primaryBrush;
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        string projectName = string.Empty;
        if (ProjectComboBox.SelectedItem is Project p)
        {
            projectName = p.Name;
        }
        else if (!string.IsNullOrWhiteSpace(ProjectComboBox.Text))
        {
            projectName = ProjectComboBox.Text.Trim();
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            ThemedMessageBox.ShowInfo(this, "Please select or enter a project name.", "Project Required");
            ProjectComboBox.Focus();
            return;
        }

        string title = TitleTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ThemedMessageBox.ShowInfo(this, "Please enter a milestone title / event name.", "Title Required");
            TitleTextBox.Focus();
            return;
        }

        DateTime targetDate = TargetDatePicker.SelectedDate ?? DateTime.Today;
        bool isCompleted = CompletedCheckBox.IsChecked == true;

        if (_originalMilestone != null)
        {
            ResultMilestone = new ProjectMilestone
            {
                Id = _originalMilestone.Id,
                ProjectName = projectName,
                Title = title,
                TargetDate = targetDate,
                Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
                IsCompleted = isCompleted,
                CompletedAt = isCompleted ? (_originalMilestone.CompletedAt ?? DateTime.Now) : null,
                Order = _originalMilestone.Order,
                CreatedAt = _originalMilestone.CreatedAt
            };
        }
        else
        {
            ResultMilestone = new ProjectMilestone
            {
                Id = Guid.NewGuid().ToString("N"),
                ProjectName = projectName,
                Title = title,
                TargetDate = targetDate,
                Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
                IsCompleted = isCompleted,
                CompletedAt = isCompleted ? DateTime.Now : null,
                Order = 0,
                CreatedAt = DateTime.Now
            };
        }

        DialogResult = true;
        Close();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = ThemedMessageBox.ConfirmDelete(this, "milestone", _originalMilestone?.Title ?? "milestone");

        if (confirm)
        {
            IsDeleted = true;
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
