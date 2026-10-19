using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class WeeklyGoalAchievedDialog : Window
{
    private readonly DispatcherTimer _autoDismissTimer = new();
    private int _remainingSeconds = 25;
    private bool _isClosing = false;

    public bool RequestOpenPlanner { get; private set; } = false;
    public bool RequestOpenAnalytics { get; private set; } = false;
    public int RemainingSeconds => _remainingSeconds;
    public int AutoDismissSeconds { get; set; } = 25;

    public WeeklyGoalAchievedDialog(WeeklyGoalSummary weeklySummary)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        WeekRangeTextBlock.Text = weeklySummary.FormattedWeekRange;
        TotalTimeTextBlock.Text = $"🏆 {weeklySummary.FormattedPlannedHours} Weekly Target";
        GoalReachedTextBlock.Text = "🏆 WEEKLY GOAL CRUSHED! 🎉";
        TargetHoursTextBlock.Text = weeklySummary.FormattedPlannedHours;
        ActualHoursTextBlock.Text = weeklySummary.FormattedActualHours;
        CompletionRateTextBlock.Text = weeklySummary.FormattedCompletionPercentage;
        SurplusTextBlock.Text = weeklySummary.FormattedVariance;

        if (weeklySummary.DailySummaries != null && weeklySummary.DailySummaries.Count > 0)
        {
            DailyStripItemsControl.ItemsSource = weeklySummary.DailySummaries;
        }

        if (weeklySummary.ProjectComparisons != null && weeklySummary.ProjectComparisons.Count > 0)
        {
            ProjectComparisonsControl.ItemsSource = weeklySummary.ProjectComparisons;
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
        if (CelebrateButton != null)
        {
            CelebrateButton.Content = $"🎉 Celebrate ({_remainingSeconds}s)";
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

    private void CelebrateButton_Click(object sender, RoutedEventArgs e)
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

    private void OpenAnalyticsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoDismissTimer.Stop();
        RequestOpenAnalytics = true;
        Close();
    }
}
