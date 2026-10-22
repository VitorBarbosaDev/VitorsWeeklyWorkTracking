using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Controls;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class ShareAnalyticsAndPieChartTests
{
    [Theory]
    [InlineData(18.0, "18h")]
    [InlineData(18.25, "18h 15m")]
    [InlineData(18.5, "18h 30m")]
    [InlineData(18.75, "18h 45m")]
    [InlineData(0.5, "30m")]
    [InlineData(0.0, "0m")]
    [InlineData(0.005, "< 1m")]
    public void TimeFormatHelper_FormatHours_HandlesDurationsWithoutSeconds(double hours, string expected)
    {
        var result = TimeFormatHelper.FormatHours(hours);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TimeFormatHelper_FormatDuration_OmitSecondsByDefault()
    {
        var duration18Hours = TimeSpan.FromHours(18);
        Assert.Equal("18h", TimeFormatHelper.FormatDuration(duration18Hours));

        var duration18Hours30Mins = TimeSpan.FromHours(18.5);
        Assert.Equal("18h 30m", TimeFormatHelper.FormatDuration(duration18Hours30Mins));

        var duration45Mins = TimeSpan.FromMinutes(45);
        Assert.Equal("45m", TimeFormatHelper.FormatDuration(duration45Mins));

        var duration30Seconds = TimeSpan.FromSeconds(30);
        Assert.Equal("< 1m", TimeFormatHelper.FormatDuration(duration30Seconds));

        var durationZero = TimeSpan.Zero;
        Assert.Equal("0m", TimeFormatHelper.FormatDuration(durationZero));

        var durationWithSeconds = TimeSpan.FromHours(18).Add(TimeSpan.FromMinutes(12)).Add(TimeSpan.FromSeconds(34));
        Assert.Equal("18h 12m 34s", TimeFormatHelper.FormatDuration(durationWithSeconds, includeSeconds: true));
    }

    [Fact]
    public void AnalyticsModels_FormattedDuration_DoesNotIncludeSeconds()
    {
        var proj = new ProjectAnalyticsItem
        {
            ProjectName = "Alpha",
            TotalDuration = TimeSpan.FromHours(18)
        };
        Assert.Equal("18h", proj.FormattedDuration);

        var act = new ActivityAnalyticsItem
        {
            Activity = "Development",
            TotalDuration = TimeSpan.FromHours(18).Add(TimeSpan.FromMinutes(30))
        };
        Assert.Equal("18h 30m", act.FormattedDuration);

        var task = new TaskAnalyticsItem
        {
            Description = "Refactoring",
            TotalDuration = TimeSpan.FromMinutes(45)
        };
        Assert.Equal("45m", task.FormattedDuration);

        var daily = new DailyAnalyticsItem
        {
            Date = new DateTime(2026, 10, 1),
            TotalDuration = TimeSpan.FromHours(2)
        };
        Assert.Equal("2h", daily.FormattedDuration);
    }

    [Fact]
    public void ShareAnalyticsSummary_Calculate_ProjectShare_ReturnsAccurateMetricsAndSubBreakdowns()
    {
        var startDate = new DateTime(2026, 9, 28);
        var endDate = new DateTime(2026, 10, 4);

        var entries = new List<TimeEntry>
        {
            // Project Alpha entries on day 1 (Total: 10 hrs)
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Coding",
                Description = "Implement Auth API",
                Note = "OAuth2 endpoints",
                StartTime = new DateTime(2026, 9, 28, 9, 0, 0),
                EndTime = new DateTime(2026, 9, 28, 15, 0, 0) // 6 hours
            },
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Testing",
                Description = "Unit tests for Auth",
                Note = "Coverage 95%",
                StartTime = new DateTime(2026, 9, 28, 15, 30, 0),
                EndTime = new DateTime(2026, 9, 28, 19, 30, 0) // 4 hours
            },
            // Project Alpha entries on day 2 (Total: 8 hrs) -> Total Project Alpha = 18 hrs
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Coding",
                Description = "Implement Dashboard UI",
                Note = "WPF Charts",
                StartTime = new DateTime(2026, 9, 29, 9, 0, 0),
                EndTime = new DateTime(2026, 9, 29, 17, 0, 0) // 8 hours
            },
            // Another project entry (Total: 2 hrs) -> Total Period = 20 hrs
            new TimeEntry
            {
                ProjectName = "Project Beta",
                Activity = "Meetings",
                Description = "Sprint Planning",
                StartTime = new DateTime(2026, 9, 30, 10, 0, 0),
                EndTime = new DateTime(2026, 9, 30, 12, 0, 0) // 2 hours
            }
        };

        var summary = ShareAnalyticsSummary.Calculate(ShareCategory.Project, "Project Alpha", entries, startDate, endDate);

        Assert.Equal(ShareCategory.Project, summary.Category);
        Assert.Equal("Project Alpha", summary.ShareName);
        Assert.Equal(TimeSpan.FromHours(18), summary.TotalDuration);
        Assert.Equal(18.0, summary.TotalHours);
        Assert.Equal("18h", summary.FormattedTotalDuration);
        Assert.Equal("18.00 hrs", summary.FormattedTotalHours);

        // 18 hrs out of 20 hrs = 90.0%
        Assert.Equal(90.0, summary.SharePercentage, precision: 1);
        Assert.Equal("90.0%", summary.FormattedSharePercentage);

        Assert.Equal(3, summary.SessionCount);
        Assert.Equal(2, summary.ActiveDaysCount);
        Assert.Equal(TimeSpan.FromHours(6), summary.AverageSessionDuration); // 18 hrs / 3 sessions = 6 hrs
        Assert.Equal(TimeSpan.FromHours(9), summary.DailyAverage); // 18 hrs / 2 days = 9 hrs

        // Primary Breakdown: Activities within Project Alpha
        Assert.Equal(2, summary.PrimaryBreakdown.Count);
        var codingActivity = summary.PrimaryBreakdown.First(b => b.Name == "Coding");
        Assert.Equal(TimeSpan.FromHours(14), codingActivity.Duration); // 6h + 8h = 14h
        Assert.Equal(2, codingActivity.EntryCount);
        Assert.Equal((14.0 / 18.0) * 100.0, codingActivity.Percentage, precision: 1);

        var testingActivity = summary.PrimaryBreakdown.First(b => b.Name == "Testing");
        Assert.Equal(TimeSpan.FromHours(4), testingActivity.Duration);
        Assert.Equal(1, testingActivity.EntryCount);

        // Secondary Breakdown: Tasks within Project Alpha
        Assert.Equal(3, summary.SecondaryBreakdown.Count);

        // Daily Distribution
        Assert.Equal(2, summary.DailyDistribution.Count);
        Assert.Equal(3, summary.Sessions.Count);
    }

    [Fact]
    public void ShareAnalyticsSummary_Calculate_ActivityShare_ReturnsAccurateMetrics()
    {
        var startDate = new DateTime(2026, 9, 28);
        var endDate = new DateTime(2026, 10, 4);

        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Development",
                Description = "Backend API",
                StartTime = new DateTime(2026, 9, 28, 9, 0, 0),
                EndTime = new DateTime(2026, 9, 28, 13, 0, 0) // 4 hours
            },
            new TimeEntry
            {
                ProjectName = "Project Beta",
                Activity = "Development",
                Description = "Frontend Components",
                StartTime = new DateTime(2026, 9, 29, 10, 0, 0),
                EndTime = new DateTime(2026, 9, 29, 14, 0, 0) // 4 hours
            },
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Documentation",
                Description = "API Docs",
                StartTime = new DateTime(2026, 9, 30, 10, 0, 0),
                EndTime = new DateTime(2026, 9, 30, 12, 0, 0) // 2 hours
            }
        };

        var summary = ShareAnalyticsSummary.Calculate(ShareCategory.Activity, "Development", entries, startDate, endDate);

        Assert.Equal(ShareCategory.Activity, summary.Category);
        Assert.Equal("Development", summary.ShareName);
        Assert.Equal(TimeSpan.FromHours(8), summary.TotalDuration);
        Assert.Equal(80.0, summary.SharePercentage, precision: 1); // 8h / 10h = 80%

        // Primary Breakdown: Projects for this Activity
        Assert.Equal(2, summary.PrimaryBreakdown.Count);
        Assert.Contains(summary.PrimaryBreakdown, b => b.Name == "Project Alpha" && b.TotalHours == 4.0);
        Assert.Contains(summary.PrimaryBreakdown, b => b.Name == "Project Beta" && b.TotalHours == 4.0);
    }

    [Fact]
    public void ShareAnalyticsSummary_Calculate_TaskShare_ReturnsAccurateMetrics()
    {
        var startDate = new DateTime(2026, 9, 28);
        var endDate = new DateTime(2026, 10, 4);

        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Backend",
                Description = "Database Migration",
                StartTime = new DateTime(2026, 9, 28, 9, 0, 0),
                EndTime = new DateTime(2026, 9, 28, 12, 0, 0) // 3 hours
            },
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "DevOps",
                Description = "Database Migration",
                StartTime = new DateTime(2026, 9, 29, 10, 0, 0),
                EndTime = new DateTime(2026, 9, 29, 13, 0, 0) // 3 hours
            }
        };

        var summary = ShareAnalyticsSummary.Calculate(ShareCategory.Task, "Database Migration", entries, startDate, endDate);

        Assert.Equal(ShareCategory.Task, summary.Category);
        Assert.Equal("Database Migration", summary.ShareName);
        Assert.Equal(TimeSpan.FromHours(6), summary.TotalDuration);
        Assert.Equal(100.0, summary.SharePercentage);
        Assert.Equal(2, summary.SessionCount);
        Assert.Equal(2, summary.PrimaryBreakdown.Count);
    }

    [Fact]
    public void ShareAnalyticsSummary_Calculate_HandlesUnassignedAndEmptyGracefully()
    {
        var startDate = new DateTime(2026, 9, 28);
        var endDate = new DateTime(2026, 10, 4);

        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "",
                Activity = "",
                Description = "",
                StartTime = new DateTime(2026, 9, 28, 9, 0, 0),
                EndTime = new DateTime(2026, 9, 28, 10, 0, 0)
            }
        };

        var projSummary = ShareAnalyticsSummary.Calculate(ShareCategory.Project, "(Unassigned)", entries, startDate, endDate);
        Assert.Equal(TimeSpan.FromHours(1), projSummary.TotalDuration);
        Assert.Equal(1, projSummary.SessionCount);

        var emptySummary = ShareAnalyticsSummary.Calculate(ShareCategory.Project, "NonExistent", entries, startDate, endDate);
        Assert.Equal(TimeSpan.Zero, emptySummary.TotalDuration);
        Assert.Equal(0, emptySummary.SessionCount);
        Assert.Equal(0.0, emptySummary.SharePercentage);
        Assert.Empty(emptySummary.PrimaryBreakdown);
    }

    [Fact]
    public void ShareAnalyticsSummary_DailyDistribution_FormattingAndGrouping()
    {
        var startDate = new DateTime(2026, 9, 28);
        var endDate = new DateTime(2026, 10, 4);

        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Coding",
                Description = "Feature 1",
                StartTime = new DateTime(2026, 9, 28, 9, 0, 0),
                EndTime = new DateTime(2026, 9, 28, 12, 0, 0) // 3h
            },
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Testing",
                Description = "Feature 1 Tests",
                StartTime = new DateTime(2026, 9, 28, 13, 0, 0),
                EndTime = new DateTime(2026, 9, 28, 15, 0, 0) // 2h
            },
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Coding",
                Description = "Feature 2",
                StartTime = new DateTime(2026, 9, 29, 9, 0, 0),
                EndTime = new DateTime(2026, 9, 29, 13, 0, 0) // 4h
            }
        };

        var summary = ShareAnalyticsSummary.Calculate(ShareCategory.Project, "Project Alpha", entries, startDate, endDate);

        Assert.Equal(2, summary.DailyDistribution.Count);
        var day1 = summary.DailyDistribution.First(d => d.Date.Date == new DateTime(2026, 9, 28));
        Assert.Equal(TimeSpan.FromHours(5), day1.Duration);
        Assert.Equal("5h", day1.FormattedDuration);
        Assert.Equal(2, day1.EntryCount);
        Assert.Contains("Coding", day1.Summary);
        Assert.Contains("Testing", day1.Summary);

        var day2 = summary.DailyDistribution.First(d => d.Date.Date == new DateTime(2026, 9, 29));
        Assert.Equal(TimeSpan.FromHours(4), day2.Duration);
        Assert.Equal("4h", day2.FormattedDuration);
        Assert.Equal(1, day2.EntryCount);
    }

    [Fact]
    public void AnalyticsWindow_FilterAndClearFilters_UpdatesStateAndVisibility()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                    ThemeManager.Initialize();
                }

                var entries = new List<TimeEntry>
                {
                    new TimeEntry
                    {
                        ProjectName = "Project Alpha",
                        Activity = "Development",
                        Description = "Task 1",
                        StartTime = DateTime.Today.AddHours(9),
                        EndTime = DateTime.Today.AddHours(11) // 2h
                    },
                    new TimeEntry
                    {
                        ProjectName = "Project Beta",
                        Activity = "Design",
                        Description = "Task 2",
                        StartTime = DateTime.Today.AddHours(11),
                        EndTime = DateTime.Today.AddHours(12) // 1h
                    }
                };

                var projects = new List<Project>
                {
                    new Project { Name = "Project Alpha" },
                    new Project { Name = "Project Beta" }
                };

                var activities = new List<ActivityItem>
                {
                    new ActivityItem { Name = "Development" },
                    new ActivityItem { Name = "Design" }
                };

                var window = new AnalyticsWindow(entries, projects, activities);

                // Initially no filter is applied: ClearFilterButton should be Collapsed
                Assert.Equal(System.Windows.Visibility.Collapsed, window.ClearFilterButton.Visibility);
                Assert.Equal(0, window.ProjectFilterComboBox.SelectedIndex);
                Assert.Equal(0, window.ActivityFilterComboBox.SelectedIndex);

                // Apply project filter
                window.ProjectFilterComboBox.SelectedIndex = 1;
                Assert.Equal(System.Windows.Visibility.Visible, window.ClearFilterButton.Visibility);
                Assert.Equal("1 session", window.TotalSessionsTextBlock.Text);

                // Clear filters
                window.ClearFilters();
                Assert.Equal(0, window.ProjectFilterComboBox.SelectedIndex);
                Assert.Equal(0, window.ActivityFilterComboBox.SelectedIndex);
                Assert.Equal(System.Windows.Visibility.Collapsed, window.ClearFilterButton.Visibility);
                Assert.Equal("2 sessions", window.TotalSessionsTextBlock.Text);

                // Apply activity filter
                window.ActivityFilterComboBox.SelectedIndex = 1;
                Assert.Equal(System.Windows.Visibility.Visible, window.ClearFilterButton.Visibility);
                Assert.Equal("1 session", window.TotalSessionsTextBlock.Text);

                // Manually select "All Activities"
                window.ActivityFilterComboBox.SelectedIndex = 0;
                Assert.Equal(System.Windows.Visibility.Collapsed, window.ClearFilterButton.Visibility);
                Assert.Equal("2 sessions", window.TotalSessionsTextBlock.Text);

                // Apply both filters and test ClearFilterButton click event
                window.ProjectFilterComboBox.SelectedIndex = 1;
                window.ActivityFilterComboBox.SelectedIndex = 1;
                Assert.Equal(System.Windows.Visibility.Visible, window.ClearFilterButton.Visibility);

                window.ClearFilterButton.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Assert.Equal(0, window.ProjectFilterComboBox.SelectedIndex);
                Assert.Equal(0, window.ActivityFilterComboBox.SelectedIndex);
                Assert.Equal(System.Windows.Visibility.Collapsed, window.ClearFilterButton.Visibility);
                Assert.Equal("2 sessions", window.TotalSessionsTextBlock.Text);

                window.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }
}
