using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class ProjectGoalAchievedDialog : Window
{
    private readonly DispatcherTimer _autoDismissTimer = new();
    private int _remainingSeconds = 25;
    private bool _isClosing = false;

    public bool RequestOpenPlanner { get; private set; } = false;
    public int RemainingSeconds => _remainingSeconds;
    public int AutoDismissSeconds { get; set; } = 25;

    public class DisplayPlanItem
    {
        public string ProjectName { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public double PlannedHours { get; set; }
        public string FormattedDuration => TimeFormatHelper.FormatHoursWithUnit(PlannedHours);
        public Visibility HasActivityVisibility => string.IsNullOrWhiteSpace(Activity) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility HasNoteVisibility => string.IsNullOrWhiteSpace(Note) ? Visibility.Collapsed : Visibility.Visible;
    }

    public ProjectGoalAchievedDialog(ProjectGoalSummary projectSummary)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        DateTextBlock.Text = projectSummary.FormattedDate;
        PlannedHoursTextBlock.Text = projectSummary.FormattedPlannedHours;
        TrackedHoursTextBlock.Text = projectSummary.FormattedActualHours;
        CompletionTextBlock.Text = projectSummary.FormattedCompletionPercentage;

        string projectName = string.IsNullOrWhiteSpace(projectSummary.ProjectName) ? "Project" : projectSummary.ProjectName;
        Title = projectSummary.IsWeekly ? $"🏆 {projectName} - Weekly Goal Passed!" : $"🎯 {projectName} - Goal Achieved!";

        ProjectNameTextBlock.Text = projectName;

        if (projectSummary.IsWeekly)
        {
            BadgeTextBlock.Text = "🏆 WEEKLY PROJECT TARGET PASSED";
            GoalReachedTextBlock.Text = "🏆 Weekly Target Crushed! 🎉";
            SubheadTextBlock.Text = $"Incredible dedication! You have passed your weekly planned work target for {projectName}.";
            HeroEmojiTextBlock.Text = "🏆";
            SectionTitleTextBlock.Text = $"📋 Weekly Planned Activities for {projectName}";
        }
        else
        {
            BadgeTextBlock.Text = "🎯 PROJECT GOAL COMPLETED";
            GoalReachedTextBlock.Text = "🎯 Target Reached!";
            SubheadTextBlock.Text = $"Great job! You have reached your planned work time for {projectName} today.";
            HeroEmojiTextBlock.Text = "🎉";
            SectionTitleTextBlock.Text = $"📋 Today's Planned Activities for {projectName}";
        }

        if (projectSummary.PlannedItems != null && projectSummary.PlannedItems.Count > 0)
        {
            var items = projectSummary.PlannedItems.Select(p => new DisplayPlanItem
            {
                ProjectName = string.IsNullOrWhiteSpace(p.ProjectName) ? projectName : p.ProjectName,
                Activity = p.Activity ?? string.Empty,
                Note = p.Note ?? string.Empty,
                PlannedHours = p.PlannedHours
            }).ToList();
            PlannedItemsControl.ItemsSource = items;
        }
        else
        {
            var items = new List<DisplayPlanItem>
            {
                new DisplayPlanItem
                {
                    ProjectName = projectName,
                    Activity = projectSummary.ActivitiesSummary != "None" ? projectSummary.ActivitiesSummary : string.Empty,
                    Note = projectSummary.TasksSummary != "None" ? projectSummary.TasksSummary : "Scheduled work session",
                    PlannedHours = projectSummary.PlannedHours
                }
            };
            PlannedItemsControl.ItemsSource = items;
        }

        _remainingSeconds = AutoDismissSeconds;
        UpdateAutoDismissText();

        _autoDismissTimer.Interval = TimeSpan.FromSeconds(1);
        _autoDismissTimer.Tick += AutoDismissTimer_Tick;

        Loaded += (s, e) =>
        {
            _remainingSeconds = AutoDismissSeconds;
            UpdateAutoDismissText();
            _autoDismissTimer.Start();
        };

        Unloaded += (s, e) =>
        {
            _autoDismissTimer.Stop();
        };
    }

    public void TickAutoDismiss()
    {
        if (_isClosing) return;
        _remainingSeconds--;
        UpdateAutoDismissText();
        if (_remainingSeconds <= 0)
        {
            StartFadeOutAndClose();
        }
    }

    public void TriggerAutoDismiss()
    {
        _remainingSeconds = 0;
        StartFadeOutAndClose();
    }

    private void AutoDismissTimer_Tick(object? sender, EventArgs e)
    {
        TickAutoDismiss();
    }

    private void UpdateAutoDismissText()
    {
        if (AutoDismissTextBlock != null)
        {
            AutoDismissTextBlock.Text = $"⏱️ Fading out in {_remainingSeconds}s";
        }
        if (KeepWorkingButton != null)
        {
            KeepWorkingButton.Content = $"⚡ Keep Working ({_remainingSeconds}s)";
        }
    }

    private void StartFadeOutAndClose()
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoDismissTimer.Stop();

        try
        {
            var fadeAnimation = new DoubleAnimation
            {
                From = this.Opacity,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(1.2)
            };
            fadeAnimation.Completed += (s, e) =>
            {
                try { Close(); } catch { }
            };
            this.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        }
        catch
        {
            try { Close(); } catch { }
        }
    }

    private void KeepWorkingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoDismissTimer.Stop();
        Close();
    }

    private void OpenPlannerButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoDismissTimer.Stop();
        RequestOpenPlanner = true;
        Close();
    }
}
