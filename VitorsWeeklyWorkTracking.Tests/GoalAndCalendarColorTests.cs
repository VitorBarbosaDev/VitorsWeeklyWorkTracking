using System;
using System.Collections.Generic;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class GoalAndCalendarColorTests
{
    [Fact]
    public void ProjectColorHelper_ConsistentColorForSameProjectName()
    {
        string hex1 = ProjectColorHelper.GetProjectColorHex("Work Tracking");
        string hex2 = ProjectColorHelper.GetProjectColorHex("work tracking");
        string hex3 = ProjectColorHelper.GetProjectColorHex("  Work Tracking  ");

        Assert.Equal(hex1, hex2);
        Assert.Equal(hex1, hex3);
        Assert.StartsWith("#", hex1);
    }

    [Fact]
    public void ProjectColorHelper_HandlesEmptyAndNullProjectNames()
    {
        string hexNull = ProjectColorHelper.GetProjectColorHex(null);
        string hexEmpty = ProjectColorHelper.GetProjectColorHex("   ");

        Assert.Equal(ProjectColorHelper.DefaultProjectColorHex, hexNull);
        Assert.Equal(ProjectColorHelper.DefaultProjectColorHex, hexEmpty);

        var brush = ProjectColorHelper.GetProjectBrush(null);
        Assert.NotNull(brush);
    }

    [Fact]
    public void ProjectColorHelper_GetProjectBadgeBrushes_ReturnsValidBrushes()
    {
        var (bgLight, borderLight, fgLight) = ProjectColorHelper.GetProjectBadgeBrushes("Project Alpha", isDark: false);
        Assert.NotNull(bgLight);
        Assert.NotNull(borderLight);
        Assert.NotNull(fgLight);

        var (bgDark, borderDark, fgDark) = ProjectColorHelper.GetProjectBadgeBrushes("Project Alpha", isDark: true);
        Assert.NotNull(bgDark);
        Assert.NotNull(borderDark);
        Assert.NotNull(fgDark);
    }

    [Theory]
    [InlineData(GoalStatusCategory.GoalMet)]
    [InlineData(GoalStatusCategory.InProgress)]
    [InlineData(GoalStatusCategory.Unplanned)]
    [InlineData(GoalStatusCategory.NotStarted)]
    [InlineData(GoalStatusCategory.RestDay)]
    [InlineData(GoalStatusCategory.GoalNotMet)]
    public void GoalColorHelper_GetStatusBrushes_ReturnsNonEmptyBrushesForBothThemes(GoalStatusCategory category)
    {
        var light = GoalColorHelper.GetStatusBrushes(category, isDark: false);
        Assert.NotNull(light.Background);
        Assert.NotNull(light.Border);
        Assert.NotNull(light.Foreground);

        var dark = GoalColorHelper.GetStatusBrushes(category, isDark: true);
        Assert.NotNull(dark.Background);
        Assert.NotNull(dark.Border);
        Assert.NotNull(dark.Foreground);
    }

    [Fact]
    public void GoalColorHelper_GetStatusBrushesForDailyGoal_CategorizesAccurately()
    {
        // Rest Day: planned 0, actual 0
        var rest = GoalColorHelper.GetStatusBrushesForDailyGoal(0, 0, isDark: false);
        var restExpected = GoalColorHelper.GetStatusBrushes(GoalStatusCategory.RestDay, isDark: false);
        Assert.Equal(restExpected.Background.Color, rest.Background.Color);

        // Unplanned: planned 0, actual > 0
        var unplanned = GoalColorHelper.GetStatusBrushesForDailyGoal(0, 2.5, isDark: false);
        var unplannedExpected = GoalColorHelper.GetStatusBrushes(GoalStatusCategory.Unplanned, isDark: false);
        Assert.Equal(unplannedExpected.Background.Color, unplanned.Background.Color);

        // Goal Met: actual >= planned
        var goalMet = GoalColorHelper.GetStatusBrushesForDailyGoal(3.0, 4.0, isDark: false);
        var goalMetExpected = GoalColorHelper.GetStatusBrushes(GoalStatusCategory.GoalMet, isDark: false);
        Assert.Equal(goalMetExpected.Background.Color, goalMet.Background.Color);

        // In Progress: actual > 0 and actual < planned
        var inProgress = GoalColorHelper.GetStatusBrushesForDailyGoal(5.0, 2.0, isDark: false);
        var inProgressExpected = GoalColorHelper.GetStatusBrushes(GoalStatusCategory.InProgress, isDark: false);
        Assert.Equal(inProgressExpected.Background.Color, inProgress.Background.Color);

        // Not Started: actual == 0, planned > 0
        var notStarted = GoalColorHelper.GetStatusBrushesForDailyGoal(5.0, 0, isDark: false);
        var notStartedExpected = GoalColorHelper.GetStatusBrushes(GoalStatusCategory.NotStarted, isDark: false);
        Assert.Equal(notStartedExpected.Background.Color, notStarted.Background.Color);

        // Goal Not Met for past date: actual < planned
        var pastNotMet = GoalColorHelper.GetStatusBrushesForDailyGoal(5.0, 0, DateTime.Today.AddDays(-1), isDark: false);
        var notMetExpected = GoalColorHelper.GetStatusBrushes(GoalStatusCategory.GoalNotMet, isDark: false);
        Assert.Equal(notMetExpected.Background.Color, pastNotMet.Background.Color);
    }

    [Fact]
    public void GoalColorHelper_GetVarianceBrush_DifferentiatesPositiveAndNegative()
    {
        var positiveBrush = GoalColorHelper.GetVarianceBrush(1.5, 3.0, 4.5, isDark: false);
        var negativeBrush = GoalColorHelper.GetVarianceBrush(-1.0, 3.0, 2.0, isDark: false);
        var restBrush = GoalColorHelper.GetVarianceBrush(0.0, 0.0, 0.0, isDark: false);

        Assert.NotNull(positiveBrush);
        Assert.NotNull(negativeBrush);
        Assert.NotNull(restBrush);
        Assert.NotEqual(positiveBrush.Color, negativeBrush.Color);
    }

    [Fact]
    public void GoalColorHelper_GetProgressBarBrush_ChangesWhenGoalIsMet()
    {
        var metBrush = GoalColorHelper.GetProgressBarBrush(100.0, isGoalMet: true, isDark: false);
        var inProgressBrush = GoalColorHelper.GetProgressBarBrush(50.0, isGoalMet: false, isDark: false);
        var zeroBrush = GoalColorHelper.GetProgressBarBrush(0.0, isGoalMet: false, isDark: false);

        Assert.NotNull(metBrush);
        Assert.NotNull(inProgressBrush);
        Assert.NotNull(zeroBrush);
        Assert.NotEqual(metBrush.Color, inProgressBrush.Color);
    }

    [Fact]
    public void DailyGoalSummary_ColorAndBrushProperties_ArePopulated()
    {
        var summary = new DailyGoalSummary
        {
            Date = new DateTime(2026, 10, 24), // Saturday
            PlannedHours = 4.0,
            ActualHours = 5.0
        };

        Assert.True(summary.IsWeekend);
        Assert.True(summary.IsGoalMet);
        Assert.NotNull(summary.StatusBackgroundBrush);
        Assert.NotNull(summary.StatusBorderBrush);
        Assert.NotNull(summary.StatusForegroundBrush);
        Assert.NotNull(summary.VarianceForegroundBrush);
        Assert.NotNull(summary.ProgressBarBrush);
    }

    [Fact]
    public void ProjectGoalComparison_ColorAndBrushProperties_ArePopulated()
    {
        var comparison = new ProjectGoalComparison
        {
            ProjectName = "Alpha Beta",
            PlannedHours = 2.0,
            ActualHours = 2.5
        };

        Assert.True(comparison.IsGoalMet);
        Assert.NotNull(comparison.ProjectBrush);
        Assert.NotNull(comparison.ProgressBarBrush);
        Assert.NotNull(comparison.VarianceForegroundBrush);
        Assert.NotNull(comparison.StatusBackgroundBrush);
        Assert.NotNull(comparison.StatusBorderBrush);
        Assert.NotNull(comparison.StatusForegroundBrush);
    }

    [Fact]
    public void PlannedWorkItem_ColorAndBrushProperties_ArePopulated()
    {
        var plan = new PlannedWorkItem
        {
            ProjectName = "Mobile App",
            Activity = "Development",
            PlannedHours = 3.0,
            ActualHours = 3.0,
            IsCompleted = true
        };

        Assert.True(plan.IsCompleted);
        Assert.NotNull(plan.ProjectBrush);
        Assert.NotNull(plan.ProgressBarBrush);
        Assert.NotNull(plan.StatusBackgroundBrush);
        Assert.NotNull(plan.StatusBorderBrush);
        Assert.NotNull(plan.StatusForegroundBrush);
    }

    [Fact]
    public void AnalyticsWindow_GoalsTab_InitializesWithColoredBindingsAndModels()
    {
        Exception? threadException = null;
        var thread = new System.Threading.Thread(() =>
        {
            string? tempPlanFile = null;
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                    ThemeManager.Initialize();
                }

                tempPlanFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"goal_test_plans_{Guid.NewGuid():N}.json");
                var testPlanStorage = new CalendarPlanStorage(tempPlanFile);
                CalendarPlanStorage.SetInstanceForTesting(testPlanStorage);

                var today = DateTime.Today;
                var testEntries = new List<TimeEntry>
                {
                    new TimeEntry
                    {
                        ProjectName = "Project Alpha",
                        Activity = "Development",
                        Description = "Backend coding",
                        StartTime = today.AddHours(9),
                        EndTime = today.AddHours(12) // 3h
                    },
                    new TimeEntry
                    {
                        ProjectName = "Project Beta",
                        Activity = "Design",
                        Description = "Figma review",
                        StartTime = today.AddHours(13),
                        EndTime = today.AddHours(15) // 2h
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

                testPlanStorage.Save(new List<PlannedWorkItem>
                {
                    new PlannedWorkItem
                    {
                        Date = today,
                        ProjectName = "Project Alpha",
                        Activity = "Development",
                        PlannedHours = 2.0
                    },
                    new PlannedWorkItem
                    {
                        Date = today,
                        ProjectName = "Project Beta",
                        Activity = "Design",
                        PlannedHours = 4.0
                    }
                });

                var window = new AnalyticsWindow(testEntries, projects, activities);
                window.Show();

                Assert.NotNull(window.GoalTabProgressBar.Foreground);
                Assert.NotNull(window.GoalsDailyDataGrid.ItemsSource);
                Assert.NotNull(window.GoalsProjectDataGrid.ItemsSource);

                // Verify draggable splitter between goals tables
                Assert.NotNull(window.GoalsTableGridSplitter);
                Assert.Equal(1, System.Windows.Controls.Grid.GetColumn(window.GoalsTableGridSplitter));
                Assert.Equal(System.Windows.Controls.GridResizeBehavior.PreviousAndNext, window.GoalsTableGridSplitter.ResizeBehavior);
                Assert.Equal(System.Windows.Controls.GridResizeDirection.Columns, window.GoalsTableGridSplitter.ResizeDirection);

                // Verify parent split grid column layout
                var splitGrid = window.GoalsTableGridSplitter.Parent as System.Windows.Controls.Grid;
                Assert.NotNull(splitGrid);
                Assert.Equal(3, splitGrid.ColumnDefinitions.Count);
                Assert.True(splitGrid.ColumnDefinitions[0].MinWidth >= 200);
                Assert.True(splitGrid.ColumnDefinitions[2].MinWidth >= 200);

                window.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
            finally
            {
                CalendarPlanStorage.ResetInstance();
                try
                {
                    if (tempPlanFile != null && System.IO.File.Exists(tempPlanFile))
                        System.IO.File.Delete(tempPlanFile);
                }
                catch { }
            }
        });

        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void CalendarPlannerWindow_InitializesWithColoredElementsAndCards()
    {
        Exception? threadException = null;
        var thread = new System.Threading.Thread(() =>
        {
            string? tempPlanFile = null;
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                    ThemeManager.Initialize();
                }

                tempPlanFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"planner_test_plans_{Guid.NewGuid():N}.json");
                var testPlanStorage = new CalendarPlanStorage(tempPlanFile);
                CalendarPlanStorage.SetInstanceForTesting(testPlanStorage);

                var today = DateTime.Today;
                var testEntries = new List<TimeEntry>
                {
                    new TimeEntry
                    {
                        ProjectName = "Project Alpha",
                        Activity = "Development",
                        Description = "UI implementation",
                        StartTime = today.AddHours(9),
                        EndTime = today.AddHours(11) // 2h
                    }
                };

                var projects = new List<Project>
                {
                    new Project { Name = "Project Alpha" }
                };

                var activities = new List<ActivityItem>
                {
                    new ActivityItem { Name = "Development" }
                };

                testPlanStorage.Save(new List<PlannedWorkItem>
                {
                    new PlannedWorkItem
                    {
                        Date = today,
                        ProjectName = "Project Alpha",
                        Activity = "Development",
                        PlannedHours = 2.0
                    }
                });

                var plannerWindow = new CalendarPlannerWindow(testEntries, projects, activities);
                plannerWindow.Show();

                Assert.NotNull(plannerWindow.WeeklyProgressBar.Foreground);
                Assert.NotNull(plannerWindow.AllPlansDataGrid.ItemsSource);
                Assert.NotNull(plannerWindow.ProjectGoalsDataGrid.ItemsSource);

                plannerWindow.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
            finally
            {
                CalendarPlanStorage.ResetInstance();
                try
                {
                    if (tempPlanFile != null && System.IO.File.Exists(tempPlanFile))
                        System.IO.File.Delete(tempPlanFile);
                }
                catch { }
            }
        });

        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void AppResources_DefinesModernGridSplitterStyles()
    {
        Exception? threadException = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var vertStyle = System.Windows.Application.Current.TryFindResource("ModernVerticalGridSplitter") as System.Windows.Style;
                var horizStyle = System.Windows.Application.Current.TryFindResource("ModernHorizontalGridSplitter") as System.Windows.Style;

                Assert.NotNull(vertStyle);
                Assert.NotNull(horizStyle);
                Assert.Equal(typeof(System.Windows.Controls.GridSplitter), vertStyle.TargetType);
                Assert.Equal(typeof(System.Windows.Controls.GridSplitter), horizStyle.TargetType);
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void PlannedWorkItem_StatusCategoriesAndTexts_ForPastPresentFuture()
    {
        // Future date, unstarted
        var future = new PlannedWorkItem { Date = DateTime.Today.AddDays(3), PlannedHours = 2.0, ActualHours = 0.0 };
        Assert.Equal("⚪ Not Started Yet", future.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.NotStarted, future.StatusCategory);

        // Future date, in progress
        var futureInProgress = new PlannedWorkItem { Date = DateTime.Today.AddDays(3), PlannedHours = 2.0, ActualHours = 0.5 };
        Assert.Equal("⏳ In Progress (25%)", futureInProgress.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.InProgress, futureInProgress.StatusCategory);

        // Today, unstarted
        var todayUnstarted = new PlannedWorkItem { Date = DateTime.Today, PlannedHours = 2.0, ActualHours = 0.0 };
        Assert.Equal("⚪ Not Started Yet", todayUnstarted.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.NotStarted, todayUnstarted.StatusCategory);

        // Today, in progress
        var todayInProgress = new PlannedWorkItem { Date = DateTime.Today, PlannedHours = 4.0, ActualHours = 3.0 };
        Assert.Equal("⏳ In Progress (75%)", todayInProgress.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.InProgress, todayInProgress.StatusCategory);

        // Past date, unstarted -> Goal Not Met
        var pastUnstarted = new PlannedWorkItem { Date = DateTime.Today.AddDays(-2), PlannedHours = 2.0, ActualHours = 0.0 };
        Assert.Equal("❌ Goal Not Met", pastUnstarted.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.GoalNotMet, pastUnstarted.StatusCategory);

        // Past date, partial -> Goal Not Met (percentage)
        var pastPartial = new PlannedWorkItem { Date = DateTime.Today.AddDays(-1), PlannedHours = 2.0, ActualHours = 1.0 };
        Assert.Equal("❌ Goal Not Met (50%)", pastPartial.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.GoalNotMet, pastPartial.StatusCategory);

        // Past date, met
        var pastMet = new PlannedWorkItem { Date = DateTime.Today.AddDays(-1), PlannedHours = 2.0, ActualHours = 2.0, IsCompleted = true, RewardIcon = "⭐" };
        Assert.Equal("⭐ Goal Met", pastMet.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.GoalMet, pastMet.StatusCategory);
    }

    [Fact]
    public void DailyGoalSummary_StatusCategoriesAndTexts_ForPastPresentFuture()
    {
        // Rest day
        var rest = new DailyGoalSummary { Date = DateTime.Today, PlannedHours = 0, ActualHours = 0 };
        Assert.Equal("💤 Rest Day", rest.StatusText);
        Assert.Equal(GoalStatusCategory.RestDay, rest.StatusCategory);

        // Unplanned work
        var unplanned = new DailyGoalSummary { Date = DateTime.Today, PlannedHours = 0, ActualHours = 2.0 };
        Assert.Contains("Unplanned", unplanned.StatusText);
        Assert.Equal(GoalStatusCategory.Unplanned, unplanned.StatusCategory);

        // Goal met
        var met = new DailyGoalSummary { Date = DateTime.Today, PlannedHours = 2.0, ActualHours = 2.5 };
        Assert.Contains("Goal Met", met.StatusText);
        Assert.Equal(GoalStatusCategory.GoalMet, met.StatusCategory);

        // Future unstarted
        var future = new DailyGoalSummary { Date = DateTime.Today.AddDays(2), PlannedHours = 2.0, ActualHours = 0.0 };
        Assert.Equal("⚪ Not Started Yet", future.StatusText);
        Assert.Equal(GoalStatusCategory.NotStarted, future.StatusCategory);

        // Past unstarted
        var pastUnstarted = new DailyGoalSummary { Date = DateTime.Today.AddDays(-2), PlannedHours = 2.0, ActualHours = 0.0 };
        Assert.Equal("❌ Goal Not Met", pastUnstarted.StatusText);
        Assert.Equal(GoalStatusCategory.GoalNotMet, pastUnstarted.StatusCategory);

        // Past partial
        var pastPartial = new DailyGoalSummary { Date = DateTime.Today.AddDays(-1), PlannedHours = 4.0, ActualHours = 1.0 };
        Assert.Equal("❌ Goal Not Met (25%)", pastPartial.StatusText);
        Assert.Equal(GoalStatusCategory.GoalNotMet, pastPartial.StatusCategory);
    }

    [Fact]
    public void DailyAnalyticsItem_GoalStatusText_ForPastPresentFuture()
    {
        var pastUnstarted = new DailyAnalyticsItem { Date = DateTime.Today.AddDays(-1), PlannedHours = 2.0, TotalDuration = TimeSpan.Zero };
        Assert.Equal("❌ Goal Not Met", pastUnstarted.GoalStatusText);

        var pastPartial = new DailyAnalyticsItem { Date = DateTime.Today.AddDays(-1), PlannedHours = 4.0, TotalDuration = TimeSpan.FromHours(1) };
        Assert.Equal("❌ Goal Not Met (25%)", pastPartial.GoalStatusText);

        var futureUnstarted = new DailyAnalyticsItem { Date = DateTime.Today.AddDays(1), PlannedHours = 2.0, TotalDuration = TimeSpan.Zero };
        Assert.Equal("⚪ Not Started Yet", futureUnstarted.GoalStatusText);

        var todayInProgress = new DailyAnalyticsItem { Date = DateTime.Today, PlannedHours = 4.0, TotalDuration = TimeSpan.FromHours(2) };
        Assert.Equal("⏳ In Progress (50%)", todayInProgress.GoalStatusText);
    }
}
