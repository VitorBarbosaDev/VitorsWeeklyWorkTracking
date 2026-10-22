using System;
using System.Collections.Generic;
using System.Windows;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class ShareAnalyticsDialog : Window
{
    private readonly ShareCategory _category;
    private readonly string _shareName;
    private readonly List<TimeEntry> _periodEntries;
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;
    private readonly Action<ShareAnalyticsNavigationAction, string>? _onNavigate;
    private readonly ShareAnalyticsSummary _summary;

    public ShareAnalyticsDialog(
        ShareCategory category,
        string shareName,
        List<TimeEntry> periodEntries,
        DateTime startDate,
        DateTime endDate,
        Action<ShareAnalyticsNavigationAction, string>? onNavigate = null)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _category = category;
        _shareName = shareName;
        _periodEntries = periodEntries ?? new List<TimeEntry>();
        _startDate = startDate;
        _endDate = endDate;
        _onNavigate = onNavigate;

        _summary = ShareAnalyticsSummary.Calculate(category, shareName, _periodEntries, _startDate, _endDate);

        PopulateUI();
    }

    public ShareAnalyticsSummary Summary => _summary;

    private void PopulateUI()
    {
        Title = $"Share Analytics — {_shareName}";
        ShareTitleText.Text = string.IsNullOrWhiteSpace(_shareName) ? "(Unassigned)" : _shareName;
        PeriodText.Text = $"Period: {_startDate:yyyy-MM-dd} – {_endDate:yyyy-MM-dd}";

        switch (_category)
        {
            case ShareCategory.Project:
                HeaderIconText.Text = "📁";
                CategoryBadgeText.Text = "PROJECT SHARE";
                PrimaryBreakdownTitleText.Text = "🏷️ Activity Distribution within Project";
                SecondaryBreakdownTitleText.Text = "📝 Task & Description Breakdown";
                NavigateBreakdownButton.Content = "📁 Open Project Breakdown Tab";
                break;

            case ShareCategory.Activity:
                HeaderIconText.Text = "🏷️";
                CategoryBadgeText.Text = "ACTIVITY SHARE";
                PrimaryBreakdownTitleText.Text = "📁 Project Distribution for Activity";
                SecondaryBreakdownTitleText.Text = "📝 Task & Description Breakdown";
                NavigateBreakdownButton.Content = "🏷️ Open Activity Breakdown Tab";
                break;

            case ShareCategory.Task:
                HeaderIconText.Text = "📝";
                CategoryBadgeText.Text = "TASK SHARE";
                PrimaryBreakdownTitleText.Text = "📁 Projects & Activities for this Task";
                SecondaryBreakdownTitleText.Text = "📋 Notes & Variations";
                NavigateBreakdownButton.Content = "📝 Open Task Breakdown Tab";
                break;
        }

        // Summary metric cards
        SharePercentageBadgeText.Text = _summary.FormattedSharePercentage;
        TotalTimeStatText.Text = _summary.FormattedTotalDuration;
        TotalHoursDecimalText.Text = _summary.FormattedTotalHours;
        SessionsCountStatText.Text = $"{_summary.SessionCount}";
        ActiveDaysStatText.Text = $"{_summary.ActiveDaysCount} day{(_summary.ActiveDaysCount == 1 ? "" : "s")}";
        AvgSessionStatText.Text = _summary.FormattedAverageSession;
        DailyAverageStatText.Text = _summary.FormattedDailyAverage;

        // Data grids
        PrimaryBreakdownDataGrid.ItemsSource = _summary.PrimaryBreakdown;
        SecondaryBreakdownDataGrid.ItemsSource = _summary.SecondaryBreakdown;
        DailyDistributionDataGrid.ItemsSource = _summary.DailyDistribution;
        SessionsLogDataGrid.ItemsSource = _summary.Sessions;
    }

    private void NavigateBreakdownButton_Click(object sender, RoutedEventArgs e)
    {
        var action = _category switch
        {
            ShareCategory.Project => ShareAnalyticsNavigationAction.GoToProjectBreakdown,
            ShareCategory.Activity => ShareAnalyticsNavigationAction.GoToActivityBreakdown,
            _ => ShareAnalyticsNavigationAction.GoToTaskBreakdown
        };

        _onNavigate?.Invoke(action, _shareName);
        Close();
    }

    private void FilterWorkLogButton_Click(object sender, RoutedEventArgs e)
    {
        _onNavigate?.Invoke(ShareAnalyticsNavigationAction.FilterInWorkLog, _shareName);
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}