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

        Assert.Equal("#94A3B8", hexNull);
        Assert.Equal("#94A3B8", hexEmpty);

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
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                    ThemeManager.Initialize();
                }

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

                CalendarPlanStorage.Instance.Save(new List<PlannedWorkItem>
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

                window.Close();
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
    public void CalendarPlannerWindow_InitializesWithColoredElementsAndCards()
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
                    ThemeManager.Initialize();
                }

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

                CalendarPlanStorage.Instance.Save(new List<PlannedWorkItem>
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
        });

        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }
}
