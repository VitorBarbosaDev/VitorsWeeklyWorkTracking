using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class DailyGoalAchievedDialog : Window
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

    public DailyGoalAchievedDialog(DailyGoalSummary dailySummary)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        DateTextBlock.Text = dailySummary.Date.ToString("dddd, dd MMMM yyyy");
        TotalTimeTextBlock.Text = $"🎯 {dailySummary.FormattedPlannedHours} Daily Target";
        GoalReachedTextBlock.Text = "Daily Goal Reached!";
        PlannedHoursTextBlock.Text = dailySummary.FormattedPlannedHours;
        TrackedHoursTextBlock.Text = dailySummary.FormattedActualHours;
        CompletionTextBlock.Text = dailySummary.FormattedCompletionPercentage;

        if (dailySummary.PlannedItems != null && dailySummary.PlannedItems.Count > 0)
        {
            var items = dailySummary.PlannedItems.Select(p => new DisplayPlanItem
            {
                ProjectName = string.IsNullOrWhiteSpace(p.ProjectName) ? "General Work" : p.ProjectName,
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
                    ProjectName = string.IsNullOrWhiteSpace(dailySummary.ProjectsSummary) || dailySummary.ProjectsSummary == "None" ? "Scheduled Work" : dailySummary.ProjectsSummary,
                    Activity = dailySummary.ActivitiesSummary != "None" ? dailySummary.ActivitiesSummary : string.Empty,
                    Note = dailySummary.TasksSummary != "None" ? dailySummary.TasksSummary : "Daily scheduled activity",
                    PlannedHours = dailySummary.PlannedHours
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
