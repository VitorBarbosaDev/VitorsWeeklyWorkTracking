using System;
using System.Collections.Generic;
using System.IO;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class GoalNotificationTests : IDisposable
{
    private readonly string _testStorageDir;
    private readonly string _testPlanFilePath;
    private readonly CalendarPlanStorage _planStorage;

    public GoalNotificationTests()
    {
        _testStorageDir = Path.Combine(Path.GetTempPath(), "GoalNotificationTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDir);
        _testPlanFilePath = Path.Combine(_testStorageDir, "calendar-plans.json");
        _planStorage = new CalendarPlanStorage(_testPlanFilePath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testStorageDir))
            {
                Directory.Delete(_testStorageDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void DailyGoalAchieved_Triggered_WhenActiveTrackingMatchesDailyPlan()
    {
        var testDate = new DateTime(2026, 10, 19); // Monday
        var tracker = new GoalMilestoneTracker(_planStorage);

        // Plan 2 hours for today
        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem
            {
                Date = testDate,
                ProjectName = "Project Alpha",
                Activity = "Development",
                PlannedHours = 2.0
            }
        });

        // 1 hour already recorded
        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Development",
                StartTime = testDate.AddHours(9),
                EndTime = testDate.AddHours(10) // 1.0 hr
            }
        };

        tracker.Initialize(testDate, entries);
        Assert.False(tracker.HasShownDailyGoalNotification);

        DailyGoalSummary? triggeredDailySummary = null;
        double triggeredActiveHours = 0;
        tracker.DailyGoalAchieved += (summary, activeHours) =>
        {
            triggeredDailySummary = summary;
            triggeredActiveHours = activeHours;
        };

        // 30 mins active tracking -> total 1.5 hrs (< 2.0 hrs) -> No trigger
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(30));
        Assert.Null(triggeredDailySummary);
        Assert.False(tracker.HasShownDailyGoalNotification);

        // 60 mins active tracking -> total 2.0 hrs (== 2.0 hrs) -> Trigger daily goal!
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(60));
        Assert.NotNull(triggeredDailySummary);
        Assert.Equal(2.0, triggeredDailySummary.PlannedHours);
        Assert.Equal(2.0, triggeredActiveHours);
        Assert.True(tracker.HasShownDailyGoalNotification);

        // Subsequent check at 2.1 hrs should not re-trigger
        triggeredDailySummary = null;
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(66));
        Assert.Null(triggeredDailySummary);
    }

    [Fact]
    public void WeeklyGoalAchieved_Triggered_WhenActiveTrackingPassesWeeklyPlan()
    {
        var testDate = new DateTime(2026, 10, 21); // Wednesday
        var tracker = new GoalMilestoneTracker(_planStorage);

        // Plan 5 hours total across the week (Mon=2h, Tue=2h, Wed=1h)
        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = new DateTime(2026, 10, 19), ProjectName = "App", PlannedHours = 2.0 },
            new PlannedWorkItem { Date = new DateTime(2026, 10, 20), ProjectName = "App", PlannedHours = 2.0 },
            new PlannedWorkItem { Date = new DateTime(2026, 10, 21), ProjectName = "App", PlannedHours = 1.0 }
        });

        // 4 hours already completed (Mon=2h, Tue=2h)
        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "App",
                StartTime = new DateTime(2026, 10, 19, 9, 0, 0),
                EndTime = new DateTime(2026, 10, 19, 11, 0, 0)
            },
            new TimeEntry
            {
                ProjectName = "App",
                StartTime = new DateTime(2026, 10, 20, 9, 0, 0),
                EndTime = new DateTime(2026, 10, 20, 11, 0, 0)
            }
        };

        tracker.Initialize(testDate, entries);
        Assert.False(tracker.HasShownWeeklyGoalNotification);

        WeeklyGoalSummary? triggeredWeekly = null;
        double triggeredWeeklyHours = 0;
        tracker.WeeklyGoalAchieved += (summary, activeHours) =>
        {
            triggeredWeekly = summary;
            triggeredWeeklyHours = activeHours;
        };

        // Track 45 mins on Wednesday -> total 4.75 hrs (< 5.0 hrs) -> Not yet
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(45));
        Assert.Null(triggeredWeekly);
        Assert.False(tracker.HasShownWeeklyGoalNotification);

        // Track 60 mins on Wednesday -> total 5.0 hrs (>= 5.0 hrs) -> Weekly goal passed!
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(60));
        Assert.NotNull(triggeredWeekly);
        Assert.Equal(5.0, triggeredWeekly.TotalPlannedHours);
        Assert.Equal(5.0, triggeredWeeklyHours);
        Assert.True(tracker.HasShownWeeklyGoalNotification);

        // Next ticks should not re-trigger
        triggeredWeekly = null;
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(70));
        Assert.Null(triggeredWeekly);
    }

    [Fact]
    public void DailyAndWeeklyGoals_DoNotTrigger_WhenNoPlansExist()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>()); // No plans

        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "Unplanned",
                StartTime = testDate.AddHours(9),
                EndTime = testDate.AddHours(14)
            }
        };

        bool dailyFired = false;
        bool weeklyFired = false;
        tracker.DailyGoalAchieved += (_, _) => dailyFired = true;
        tracker.WeeklyGoalAchieved += (_, _) => weeklyFired = true;

        tracker.Initialize(testDate, entries);
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromHours(5));

        Assert.False(dailyFired);
        Assert.False(weeklyFired);
        Assert.False(tracker.HasShownDailyGoalNotification);
        Assert.False(tracker.HasShownWeeklyGoalNotification);
    }

    [Fact]
    public void StartupInitialization_MarksAlreadyAchievedGoals_AsShown()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Project", PlannedHours = 3.0 }
        });

        // 3.5 hours already logged
        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "Project",
                StartTime = testDate.AddHours(9),
                EndTime = testDate.AddHours(12.5)
            }
        };

        tracker.Initialize(testDate, entries);

        // Should be marked as already shown on startup
        Assert.True(tracker.HasShownDailyGoalNotification);
        Assert.True(tracker.HasShownWeeklyGoalNotification);

        // Checking milestones should not fire event
        bool dailyFired = false;
        tracker.DailyGoalAchieved += (_, _) => dailyFired = true;
        tracker.CheckMilestones(testDate, entries, TimeSpan.Zero);
        Assert.False(dailyFired);
    }

    [Fact]
    public void DateChange_ResetsDailyGoalNotification()
    {
        var day1 = new DateTime(2026, 10, 20); // Tuesday
        var day2 = new DateTime(2026, 10, 21); // Wednesday
        var tracker = new GoalMilestoneTracker(_planStorage);

        tracker.HasShownDailyGoalNotification = true;
        tracker.HasShownWeeklyGoalNotification = true;
        tracker.LastCheckDate = day1;

        var entries = new List<TimeEntry>();

        // Check on day 2
        tracker.CheckMilestones(day2, entries, TimeSpan.Zero);

        // Daily flag should be reset for new day, weekly preserved on mid-week day
        Assert.False(tracker.HasShownDailyGoalNotification);
        Assert.True(tracker.HasShownWeeklyGoalNotification);
        Assert.Equal(day2, tracker.LastCheckDate);
    }

    [Fact]
    public void MondayDateChange_ResetsWeeklyGoalNotification()
    {
        var sunday = new DateTime(2026, 10, 25); // Sunday
        var monday = new DateTime(2026, 10, 26); // Next Monday
        var tracker = new GoalMilestoneTracker(_planStorage);

        tracker.HasShownDailyGoalNotification = true;
        tracker.HasShownWeeklyGoalNotification = true;
        tracker.LastCheckDate = sunday;

        var entries = new List<TimeEntry>();

        // Check on next Monday
        tracker.CheckMilestones(monday, entries, TimeSpan.Zero);

        // Both daily and weekly flags should reset for a new week
        Assert.False(tracker.HasShownDailyGoalNotification);
        Assert.False(tracker.HasShownWeeklyGoalNotification);
    }

    [Fact]
    public void ResetIfPlansOrEntriesDecreased_ProperlyAdjustsFlags()
    {
        var tracker = new GoalMilestoneTracker(_planStorage);
        tracker.HasShownDailyGoalNotification = true;
        tracker.HasShownWeeklyGoalNotification = true;

        var todaySummary = new DailyGoalSummary
        {
            Date = DateTime.Today,
            PlannedHours = 4.0,
            ActualHours = 2.0 // Plan increased or entry deleted -> actual < planned
        };

        var weeklySummary = new WeeklyGoalSummary
        {
            TotalPlannedHours = 20.0,
            TotalActualHours = 15.0 // actual < planned
        };

        tracker.ResetIfPlansOrEntriesDecreased(todaySummary, weeklySummary);

        Assert.False(tracker.HasShownDailyGoalNotification);
        Assert.False(tracker.HasShownWeeklyGoalNotification);
    }

    [Fact]
    public void DailyGoalSummary_HelperProperties_FormatCorrectly()
    {
        var summary = new DailyGoalSummary
        {
            Date = new DateTime(2026, 10, 19),
            PlannedHours = 4.0,
            ActualHours = 4.0
        };

        Assert.Equal("19 Oct", summary.ShortDateFormatted);
        Assert.Equal("Mon", summary.ShortDayName);
        Assert.True(summary.IsGoalMet);
        Assert.Equal("✅", summary.DayStatusEmoji);
        Assert.Equal("100%", summary.FormattedCompletionPercentage);
    }

    [Fact]
    public void SaveEntry_SingleDayPlan_TriggersOnlyOneNotification_AndSuppressesWeeklyMilestone()
    {
        var testDate = new DateTime(2026, 10, 23); // Friday
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Project Alpha", PlannedHours = 3.0 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        int totalTriggers = 0;
        bool projectFired = false;
        bool dailyFired = false;
        bool weeklyFired = false;
        tracker.ProjectGoalAchieved += (_, _) => { projectFired = true; totalTriggers++; };
        tracker.DailyGoalAchieved += (_, _) => { dailyFired = true; totalTriggers++; };
        tracker.WeeklyGoalAchieved += (_, _) => { weeklyFired = true; totalTriggers++; };

        // User finishes and saves a 3-hour session
        entries.Add(new TimeEntry
        {
            ProjectName = "Project Alpha",
            StartTime = testDate.AddHours(9),
            EndTime = testDate.AddHours(12) // 3.0 hrs
        });

        tracker.CheckMilestones(testDate, entries, TimeSpan.Zero, "Project Alpha");

        // Exactly one notification triggers for the single-day plan
        Assert.Equal(1, totalTriggers);
        Assert.True(projectFired || dailyFired);
        Assert.False(weeklyFired); // Weekly goal popup is suppressed to avoid duplicate notifications
        Assert.True(tracker.HasShownDailyGoalNotification);
        Assert.True(tracker.HasShownWeeklyGoalNotification);

        // Subsequent check (e.g. user continues working) does not re-trigger
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(10), "Project Alpha");
        Assert.Equal(1, totalTriggers);
    }

    [Fact]
    public void SaveEntry_MultiDayPlan_TriggersWeeklyMilestone_WhenTotalWeekPassed()
    {
        var monDate = new DateTime(2026, 10, 19);
        var tueDate = new DateTime(2026, 10, 20);
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = monDate, ProjectName = "Project Alpha", PlannedHours = 3.0 },
            new PlannedWorkItem { Date = tueDate, ProjectName = "Project Beta", PlannedHours = 3.0 }
        });

        // Mon: Log 3h
        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                StartTime = monDate.AddHours(9),
                EndTime = monDate.AddHours(12) // 3.0h
            }
        };

        tracker.Initialize(tueDate, entries);

        bool weeklyFired = false;
        tracker.WeeklyGoalAchieved += (_, _) => weeklyFired = true;

        // Tue: User saves 3h on Beta, completing the 6.0h weekly goal
        entries.Add(new TimeEntry
        {
            ProjectName = "Project Beta",
            StartTime = tueDate.AddHours(9),
            EndTime = tueDate.AddHours(12) // 3.0h
        });

        tracker.CheckMilestones(tueDate, entries, TimeSpan.Zero, "Project Beta");

        // Total weekly goal (6.0h) is passed on multi-day week
        Assert.True(weeklyFired);
        Assert.True(tracker.HasShownWeeklyGoalNotification);
    }

    [Fact]
    public void WeeklyGoalSummary_CalculatesAllDaysAndProjectsAccurately()
    {
        var monday = new DateTime(2026, 10, 19);
        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = monday, ProjectName = "Alpha", PlannedHours = 4.0 },
            new PlannedWorkItem { Date = monday.AddDays(1), ProjectName = "Beta", PlannedHours = 3.0 }
        });

        var entries = new List<TimeEntry>
        {
            new TimeEntry { ProjectName = "Alpha", StartTime = monday.AddHours(9), EndTime = monday.AddHours(13) }, // 4h
            new TimeEntry { ProjectName = "Beta", StartTime = monday.AddDays(1).AddHours(9), EndTime = monday.AddDays(1).AddHours(13) } // 4h (1h surplus)
        };

        var weeklySummary = _planStorage.CalculateWeeklySummary(monday, entries);

        Assert.Equal(7.0, weeklySummary.TotalPlannedHours);
        Assert.Equal(8.0, weeklySummary.TotalActualHours);
        Assert.True(weeklySummary.IsGoalMet);
        Assert.Equal(1.0, weeklySummary.VarianceHours);
        Assert.Equal(7, weeklySummary.DailySummaries.Count);
        Assert.Equal(2, weeklySummary.ProjectComparisons.Count);

        var alphaComp = weeklySummary.ProjectComparisons.Find(p => p.ProjectName == "Alpha");
        Assert.NotNull(alphaComp);
        Assert.Equal(4.0, alphaComp.PlannedHours);
        Assert.Equal(4.0, alphaComp.ActualHours);
        Assert.True(alphaComp.IsGoalMet);

        var betaComp = weeklySummary.ProjectComparisons.Find(p => p.ProjectName == "Beta");
        Assert.NotNull(betaComp);
        Assert.Equal(3.0, betaComp.PlannedHours);
        Assert.Equal(4.0, betaComp.ActualHours);
        Assert.True(betaComp.IsGoalMet);
    }

    [Fact]
    public void ProjectDailyGoalAchieved_Triggered_WhenActiveTrackingMatchesProjectPlan()
    {
        var testDate = new DateTime(2026, 10, 19); // Monday
        var tracker = new GoalMilestoneTracker(_planStorage);

        // Plan 1.5h for Alpha and 2.0h for Beta
        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Project Alpha", PlannedHours = 1.5 },
            new PlannedWorkItem { Date = testDate, ProjectName = "Project Beta", PlannedHours = 2.0 }
        });

        // 0.5h already recorded for Alpha
        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                StartTime = testDate.AddHours(9),
                EndTime = testDate.AddHours(9.5) // 0.5h
            }
        };

        tracker.Initialize(testDate, entries);
        Assert.DoesNotContain("Project Alpha", tracker.ShownDailyProjectGoals);

        ProjectGoalSummary? triggeredProjectGoal = null;
        double triggeredHours = 0;
        tracker.ProjectGoalAchieved += (summary, activeHours) =>
        {
            triggeredProjectGoal = summary;
            triggeredHours = activeHours;
        };

        // Active tracking Project Alpha for 30m -> total 1.0h (< 1.5h) -> No trigger
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(30), "Project Alpha");
        Assert.Null(triggeredProjectGoal);

        // Active tracking Project Alpha for 60m -> total 1.5h (== 1.5h) -> Project goal triggered!
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(60), "Project Alpha");
        Assert.NotNull(triggeredProjectGoal);
        Assert.Equal("Project Alpha", triggeredProjectGoal.ProjectName);
        Assert.Equal(1.5, triggeredProjectGoal.PlannedHours);
        Assert.Equal(1.5, triggeredHours);
        Assert.Contains("Project Alpha", tracker.ShownDailyProjectGoals);

        // Daily overall goal (3.5h) is not reached yet
        Assert.False(tracker.HasShownDailyGoalNotification);

        // Next tick does not re-trigger
        triggeredProjectGoal = null;
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(65), "Project Alpha");
        Assert.Null(triggeredProjectGoal);
    }

    [Fact]
    public void ProjectDailyGoalAchieved_DoesNotTrigger_WhenTrackingDifferentProject()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Project Alpha", PlannedHours = 1.0 },
            new PlannedWorkItem { Date = testDate, ProjectName = "Project Beta", PlannedHours = 2.0 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        bool projectGoalFired = false;
        tracker.ProjectGoalAchieved += (_, _) => projectGoalFired = true;

        // User tracks Project Gamma for 2 hours
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromHours(2), "Project Gamma");

        Assert.False(projectGoalFired);
        Assert.Empty(tracker.ShownDailyProjectGoals);
    }

    [Fact]
    public void ProjectWeeklyGoalAchieved_Triggered_WhenActiveTrackingPassesWeeklyProjectPlan()
    {
        var testDate = new DateTime(2026, 10, 21); // Wednesday
        var tracker = new GoalMilestoneTracker(_planStorage);

        // Plan 4 hours for Alpha across the week (Mon=2h, Tue=2h), Wed has 0 planned for Alpha
        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = new DateTime(2026, 10, 19), ProjectName = "Alpha", PlannedHours = 2.0 },
            new PlannedWorkItem { Date = new DateTime(2026, 10, 20), ProjectName = "Alpha", PlannedHours = 2.0 }
        });

        // 3 hours already logged for Alpha (Mon=2h, Tue=1h)
        var entries = new List<TimeEntry>
        {
            new TimeEntry { ProjectName = "Alpha", StartTime = new DateTime(2026, 10, 19, 9, 0, 0), EndTime = new DateTime(2026, 10, 19, 11, 0, 0) }, // 2h
            new TimeEntry { ProjectName = "Alpha", StartTime = new DateTime(2026, 10, 20, 9, 0, 0), EndTime = new DateTime(2026, 10, 20, 10, 0, 0) }  // 1h
        };

        tracker.Initialize(testDate, entries);
        Assert.DoesNotContain("Alpha", tracker.ShownWeeklyProjectGoals);

        ProjectGoalSummary? triggeredProjectWeekly = null;
        tracker.ProjectGoalAchieved += (summary, _) => triggeredProjectWeekly = summary;

        // Track 30m on Wednesday -> total 3.5h (< 4.0h)
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(30), "Alpha");
        Assert.Null(triggeredProjectWeekly);

        // Track 60m on Wednesday -> total 4.0h (>= 4.0h) -> Weekly project goal triggered!
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(60), "Alpha");
        Assert.NotNull(triggeredProjectWeekly);
        Assert.Equal("Alpha", triggeredProjectWeekly.ProjectName);
        Assert.True(triggeredProjectWeekly.IsWeekly);
        Assert.Equal(4.0, triggeredProjectWeekly.PlannedHours);
        Assert.Contains("Alpha", tracker.ShownWeeklyProjectGoals);
    }

    [Fact]
    public void ProjectDailyGoalAchieved_Triggered_WhenManualEntrySaved()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Backend API", PlannedHours = 2.0 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        ProjectGoalSummary? triggered = null;
        tracker.ProjectGoalAchieved += (summary, _) => triggered = summary;

        // Save a 2-hour entry for Backend API
        entries.Add(new TimeEntry
        {
            ProjectName = "Backend API",
            StartTime = testDate.AddHours(10),
            EndTime = testDate.AddHours(12) // 2.0h
        });

        tracker.CheckMilestones(testDate, entries, TimeSpan.Zero, "Backend API");

        Assert.NotNull(triggered);
        Assert.Equal("Backend API", triggered.ProjectName);
        Assert.Equal(2.0, triggered.PlannedHours);
        Assert.Equal(2.0, triggered.ActualHours);
    }

    [Fact]
    public void ProjectDailyGoalAchieved_MultipleProjects_TriggerIndependently()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Proj1", PlannedHours = 1.0 },
            new PlannedWorkItem { Date = testDate, ProjectName = "Proj2", PlannedHours = 2.0 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        var triggeredProjects = new List<string>();
        tracker.ProjectGoalAchieved += (summary, _) => triggeredProjects.Add(summary.ProjectName);

        // Work 1h on Proj1
        entries.Add(new TimeEntry { ProjectName = "Proj1", StartTime = testDate.AddHours(8), EndTime = testDate.AddHours(9) });
        tracker.CheckMilestones(testDate, entries, TimeSpan.Zero, "Proj1");

        Assert.Single(triggeredProjects);
        Assert.Contains("Proj1", triggeredProjects);

        // Work 2h on Proj2
        entries.Add(new TimeEntry { ProjectName = "Proj2", StartTime = testDate.AddHours(9), EndTime = testDate.AddHours(11) });
        tracker.CheckMilestones(testDate, entries, TimeSpan.Zero, "Proj2");

        Assert.Equal(2, triggeredProjects.Count);
        Assert.Contains("Proj2", triggeredProjects);
    }

    [Fact]
    public void ProjectGoalSummary_HelperProperties_CalculateCorrectly()
    {
        var summary = new ProjectGoalSummary
        {
            ProjectName = "Design System",
            Date = new DateTime(2026, 10, 19),
            PlannedHours = 2.5,
            ActualHours = 3.0,
            IsWeekly = false
        };

        Assert.True(summary.IsGoalMet);
        Assert.Equal(0.5, summary.VarianceHours);
        Assert.Equal("+0.50 hrs", summary.FormattedVariance);
        Assert.Equal(120.0, summary.CompletionPercentage);
        Assert.Equal("120%", summary.FormattedCompletionPercentage);
        Assert.Equal("2.5 hrs", summary.FormattedPlannedHours);
        Assert.Equal("3.0 hrs", summary.FormattedActualHours);
    }

    [Fact]
    public void ResetIfPlansOrEntriesDecreased_ResetsProjectFlags_WhenEntriesDeleted()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Alpha", PlannedHours = 2.0 }
        });

        tracker.ShownDailyProjectGoals.Add("Alpha");
        tracker.ShownWeeklyProjectGoals.Add("Alpha");

        // Entries decreased to 1h
        var entries = new List<TimeEntry>
        {
            new TimeEntry { ProjectName = "Alpha", StartTime = testDate.AddHours(9), EndTime = testDate.AddHours(10) }
        };

        var todaySummary = _planStorage.CalculateDailySummary(testDate, entries);
        var weeklySummary = _planStorage.CalculateWeeklySummary(testDate, entries);

        tracker.ResetIfPlansOrEntriesDecreased(todaySummary, weeklySummary, entries);

        Assert.DoesNotContain("Alpha", tracker.ShownDailyProjectGoals);
        Assert.DoesNotContain("Alpha", tracker.ShownWeeklyProjectGoals);
    }

    [Fact]
    public void KeepWorking_DoesNotTriggerAgain_OnActiveTrackingTicks()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Alpha", PlannedHours = 2.0 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        int triggerCount = 0;
        tracker.ProjectGoalAchieved += (_, _) => triggerCount++;
        tracker.DailyGoalAchieved += (_, _) => triggerCount++;
        tracker.WeeklyGoalAchieved += (_, _) => triggerCount++;

        // At 2h 00m: Triggers once
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromHours(2.0), "Alpha");
        Assert.Equal(1, triggerCount);

        // User clicked "Keep Working" -> Timer keeps ticking and UI Progress Card updates:
        var todaySummary = _planStorage.CalculateDailySummary(testDate, entries);
        todaySummary.ActualHours += 2.0;
        var weeklySummary = _planStorage.CalculateWeeklySummary(testDate, entries);
        weeklySummary.TotalActualHours += 2.0;
        tracker.ResetIfPlansOrEntriesDecreased(todaySummary, weeklySummary, entries, TimeSpan.FromHours(2.0), "Alpha");

        // At 2h 00m 01s:
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromHours(2.0).Add(TimeSpan.FromSeconds(1)), "Alpha");
        Assert.Equal(1, triggerCount); // No duplicate trigger

        // Another UI update:
        tracker.ResetIfPlansOrEntriesDecreased(todaySummary, weeklySummary, entries, TimeSpan.FromHours(2.0).Add(TimeSpan.FromSeconds(1)), "Alpha");

        // At 2h 05m:
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromHours(2.0).Add(TimeSpan.FromMinutes(5)), "Alpha");
        Assert.Equal(1, triggerCount); // No duplicate trigger

        // At 2h 30m:
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromHours(2.5), "Alpha");
        Assert.Equal(1, triggerCount); // No duplicate trigger
    }

    [Fact]
    public void ActiveProjectTracking_DoesNotResetProjectGoalFlags_InResetIfPlansOrEntriesDecreased()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Project Beta", PlannedHours = 1.0 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        // Trigger goal milestone for Project Beta at 1h active tracking
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromHours(1.0), "Project Beta");
        Assert.Contains("Project Beta", tracker.ShownDailyProjectGoals);

        // Call ResetIfPlansOrEntriesDecreased with active elapsed tracking of 1.2 hours
        var todaySummary = _planStorage.CalculateDailySummary(testDate, entries);
        todaySummary.ActualHours += 1.2;
        var weeklySummary = _planStorage.CalculateWeeklySummary(testDate, entries);
        weeklySummary.TotalActualHours += 1.2;

        tracker.ResetIfPlansOrEntriesDecreased(todaySummary, weeklySummary, entries, TimeSpan.FromHours(1.2), "Project Beta");

        // Project Beta must remain in ShownDailyProjectGoals to prevent re-triggering popups
        Assert.Contains("Project Beta", tracker.ShownDailyProjectGoals);
    }

    [Fact]
    public void SingleDayProjectPlan_OnlyTriggersOnce_WhenDailyAndWeeklyGoalsCoincide()
    {
        var testDate = new DateTime(2026, 10, 20); // Tuesday
        var tracker = new GoalMilestoneTracker(_planStorage);

        // Project Alpha is only planned on Tuesday for 1.5h (only 1 day in the whole week)
        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Alpha", PlannedHours = 1.5 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        var triggeredEvents = new List<string>();
        tracker.ProjectGoalAchieved += (s, _) => triggeredEvents.Add($"Project: {s.ProjectName} (Weekly={s.IsWeekly})");
        tracker.DailyGoalAchieved += (_, _) => triggeredEvents.Add("Daily");
        tracker.WeeklyGoalAchieved += (_, _) => triggeredEvents.Add("Weekly");

        // Active tracking hits 1.5h
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromHours(1.5), "Alpha");

        // Exactly one notification is triggered
        Assert.Single(triggeredEvents);
        Assert.Equal("Project: Alpha (Weekly=False)", triggeredEvents[0]);

        // Both daily and weekly milestone flags are marked to prevent subsequent popups
        Assert.True(tracker.HasShownDailyGoalNotification);
        Assert.True(tracker.HasShownWeeklyGoalNotification);
        Assert.Contains("Alpha", tracker.ShownDailyProjectGoals);
        Assert.Contains("Alpha", tracker.ShownWeeklyProjectGoals);
    }

    [Theory]
    [InlineData("Star", "⭐")]
    [InlineData("star", "⭐")]
    [InlineData("Dinosaur", "🦖")]
    [InlineData("dino", "🦖")]
    [InlineData("Brontosaurus", "🦕")]
    [InlineData("Trophy", "🏆")]
    [InlineData("Medal", "🏅")]
    [InlineData("Rocket", "🚀")]
    [InlineData("Target", "🎯")]
    [InlineData("Crown", "👑")]
    [InlineData("Checkmark", "✅")]
    [InlineData("UnknownIcon", "⭐")]
    [InlineData(null, "⭐")]
    public void RewardIconHelper_ResolvesIconEmoji_Correctly(string? iconKey, string expectedEmoji)
    {
        var emoji = RewardIconHelper.GetIconEmoji(iconKey);
        Assert.Equal(expectedEmoji, emoji);
    }

    [Fact]
    public void GoalMilestoneTracker_MasterNotificationToggleDisabled_SuppressesAllPopups()
    {
        var testDate = new DateTime(2026, 10, 19);
        var settings = new WorkRestSettings
        {
            GoalNotificationsEnabled = false, // Master toggle off
            DailyGoalNotificationEnabled = true,
            WeeklyGoalNotificationEnabled = true,
            ProjectGoalNotificationEnabled = true
        };

        var tracker = new GoalMilestoneTracker(_planStorage, settings);
        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Alpha", PlannedHours = 2.0 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        bool eventFired = false;
        tracker.DailyGoalAchieved += (_, _) => eventFired = true;
        tracker.WeeklyGoalAchieved += (_, _) => eventFired = true;
        tracker.ProjectGoalAchieved += (_, _) => eventFired = true;

        // Reach goal
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromHours(2.5), "Alpha");

        // No notification popups triggered
        Assert.False(eventFired);
        // But tracking state is still registered
        Assert.True(tracker.HasShownDailyGoalNotification);
        Assert.Contains("Alpha", tracker.ShownDailyProjectGoals);
    }

    [Fact]
    public void GoalMilestoneTracker_GranularNotificationToggles_SuppressesSelectedTypes()
    {
        var testDate = new DateTime(2026, 10, 19);
        var settings = new WorkRestSettings
        {
            GoalNotificationsEnabled = true,
            DailyGoalNotificationEnabled = false,   // Daily disabled
            WeeklyGoalNotificationEnabled = true,
            ProjectGoalNotificationEnabled = false // Project disabled
        };

        var tracker = new GoalMilestoneTracker(_planStorage, settings);
        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Alpha", PlannedHours = 2.0 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        bool dailyFired = false;
        bool projectFired = false;
        tracker.DailyGoalAchieved += (_, _) => dailyFired = true;
        tracker.ProjectGoalAchieved += (_, _) => projectFired = true;

        tracker.CheckMilestones(testDate, entries, TimeSpan.FromHours(2.0), "Alpha");

        Assert.False(dailyFired);
        Assert.False(projectFired);
    }

    [Fact]
    public void CalendarPlanStorage_PlanItemActualHoursAndCompletion_CalculatesAccurately()
    {
        var testDate = new DateTime(2026, 10, 21);
        var planItemWithAct = new PlannedWorkItem
        {
            Date = testDate,
            ProjectName = "Project Alpha",
            Activity = "Development",
            PlannedHours = 2.0
        };

        var planItemGeneral = new PlannedWorkItem
        {
            Date = testDate,
            ProjectName = "Project Beta",
            Activity = "",
            PlannedHours = 1.5
        };

        var entries = new List<TimeEntry>
        {
            // Matching Alpha Development
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Development",
                StartTime = testDate.AddHours(9),
                EndTime = testDate.AddHours(11) // 2.0 hrs
            },
            // Alpha Design (different activity)
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                Activity = "Design",
                StartTime = testDate.AddHours(11),
                EndTime = testDate.AddHours(12) // 1.0 hr
            },
            // Beta Testing (general plan matches any activity for Beta)
            new TimeEntry
            {
                ProjectName = "Project Beta",
                Activity = "Testing",
                StartTime = testDate.AddHours(14),
                EndTime = testDate.AddHours(15) // 1.0 hr
            }
        };

        // Specific activity match for Alpha Development
        double alphaHours = _planStorage.CalculatePlanItemActualHours(planItemWithAct, entries);
        bool alphaCompleted = _planStorage.IsPlanItemCompleted(planItemWithAct, entries);
        Assert.Equal(2.0, alphaHours);
        Assert.True(alphaCompleted);

        // Beta has 1.0 hr out of 1.5 hrs planned -> not completed yet
        double betaHours = _planStorage.CalculatePlanItemActualHours(planItemGeneral, entries);
        bool betaCompleted = _planStorage.IsPlanItemCompleted(planItemGeneral, entries);
        Assert.Equal(1.0, betaHours);
        Assert.False(betaCompleted);
    }

    [Fact]
    public void ProjectGoalComparison_RewardFormatting_IncludesRewardIconWhenGoalMet()
    {
        var metGoal = new ProjectGoalComparison
        {
            ProjectName = "Alpha",
            PlannedHours = 2.0,
            ActualHours = 2.5,
            RewardIcon = "🦖"
        };

        var unmetGoal = new ProjectGoalComparison
        {
            ProjectName = "Beta",
            PlannedHours = 3.0,
            ActualHours = 1.0,
            RewardIcon = "🦖"
        };

        Assert.True(metGoal.IsGoalMet);
        Assert.Equal("🦖 Alpha", metGoal.FormattedProjectWithReward);
        Assert.Contains("🦖 Target Reached", metGoal.StatusText);

        Assert.False(unmetGoal.IsGoalMet);
        Assert.Equal("Beta", unmetGoal.FormattedProjectWithReward);
        Assert.Contains("In Progress", unmetGoal.StatusText);
    }

    [Fact]
    public void PlannedWorkItem_CompletedTaskTitle_DoesNotIncludeEmojiPrefix()
    {
        var completedPlan = new PlannedWorkItem
        {
            ProjectName = "Documentation & Coding",
            PlannedHours = 1.0,
            IsCompleted = true,
            RewardIcon = "⭐"
        };

        // Planned task titles should remain clean without emoji prefix
        Assert.Equal("Documentation & Coding", completedPlan.FormattedProjectWithReward);
        Assert.Equal("Documentation & Coding", completedPlan.ProjectName);
        // Status text retains the reward icon
        Assert.Equal("⭐ Goal Met", completedPlan.CompletionStatusText);
    }

    [Fact]
    public void GoalMilestoneTracker_OneMinutePlan_DoesNotTriggerAt34Or40Seconds_TriggersAt60Seconds()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        // 1 minute plan parsed via TimeFormatHelper.TryParseDurationHours("1m") -> 1.0 / 60.0 hours
        TimeFormatHelper.TryParseDurationHours("1m", out double oneMinuteInHours);
        Assert.Equal(1.0 / 60.0, oneMinuteInHours, precision: 6);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "Test1Min", PlannedHours = oneMinuteInHours }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        int triggerCount = 0;
        tracker.ProjectGoalAchieved += (_, _) => triggerCount++;
        tracker.DailyGoalAchieved += (_, _) => triggerCount++;

        // At 34 seconds: Must NOT trigger
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromSeconds(34), "Test1Min");
        Assert.Equal(0, triggerCount);

        // At 36 seconds: Must NOT trigger
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromSeconds(36), "Test1Min");
        Assert.Equal(0, triggerCount);

        // At 40 seconds: Must NOT trigger
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromSeconds(40), "Test1Min");
        Assert.Equal(0, triggerCount);

        // At 50 seconds: Must NOT trigger
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromSeconds(50), "Test1Min");
        Assert.Equal(0, triggerCount);

        // At 59 seconds: Must NOT trigger
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromSeconds(59), "Test1Min");
        Assert.Equal(0, triggerCount);

        // At 60 seconds (1 minute): Triggers exactly once!
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromSeconds(60), "Test1Min");
        Assert.Equal(1, triggerCount);

        // At 70 seconds: Does NOT duplicate trigger
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromSeconds(70), "Test1Min");
        Assert.Equal(1, triggerCount);
    }

    [Fact]
    public void CalendarPlanStorage_LegacyDecimalInput_SanitizesToOneMinuteMinimum()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        // Legacy input or typing "0.01" in textbox
        TimeFormatHelper.TryParseDurationHours("0.01", out double parsedHours);
        Assert.Equal(1.0 / 60.0, parsedHours, precision: 6);

        var plan = new PlannedWorkItem
        {
            Date = testDate,
            ProjectName = "LegacyTest",
            PlannedHours = 0.01 // 0.01 hours is 36 seconds legacy value
        };

        _planStorage.AddOrUpdatePlan(plan);

        // Verify storage sanitized to 1 minute (60s)
        var loaded = _planStorage.GetPlansForDate(testDate).First(p => p.ProjectName == "LegacyTest");
        Assert.Equal(1.0 / 60.0, loaded.PlannedHours, precision: 6);

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        int triggerCount = 0;
        tracker.ProjectGoalAchieved += (_, _) => triggerCount++;
        tracker.DailyGoalAchieved += (_, _) => triggerCount++;

        // At 34s and 36s (legacy 0.01h was 36s): Must NOT trigger
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromSeconds(34), "LegacyTest");
        Assert.Equal(0, triggerCount);

        tracker.CheckMilestones(testDate, entries, TimeSpan.FromSeconds(36), "LegacyTest");
        Assert.Equal(0, triggerCount);

        // At 60s: Triggers
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromSeconds(60), "LegacyTest");
        Assert.Equal(1, triggerCount);
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw new AggregateException("STA thread failed", exception);
        }
    }

    [Fact]
    public void DailyGoalAchievedDialog_AutoDismiss_CountsDownAndDismisses()
    {
        RunInSta(() =>
        {
            var summary = new DailyGoalSummary
            {
                Date = DateTime.Today,
                PlannedHours = 1.0,
                ActualHours = 1.0
            };

            var dialog = new DailyGoalAchievedDialog(summary)
            {
                AutoDismissSeconds = 25
            };

            Assert.Equal(25, dialog.RemainingSeconds);

            // Tick countdown
            dialog.TickAutoDismiss();
            Assert.Equal(24, dialog.RemainingSeconds);

            dialog.TickAutoDismiss();
            Assert.Equal(23, dialog.RemainingSeconds);

            // Trigger dismissal
            dialog.TriggerAutoDismiss();
            Assert.Equal(0, dialog.RemainingSeconds);
        });
    }

    [Fact]
    public void DailyGoalAchievedDialog_OpenPlannerButton_SetsRequestFlag()
    {
        RunInSta(() =>
        {
            var summary = new DailyGoalSummary
            {
                Date = DateTime.Today,
                PlannedHours = 1.0,
                ActualHours = 1.0
            };

            var dialog = new DailyGoalAchievedDialog(summary);
            Assert.False(dialog.RequestOpenPlanner);
        });
    }

    [Fact]
    public void WeeklyGoalAchievedDialog_AutoDismiss_CountsDownAndDismisses()
    {
        RunInSta(() =>
        {
            var summary = new WeeklyGoalSummary
            {
                WeekStartDate = DateTime.Today,
                WeekEndDate = DateTime.Today.AddDays(6),
                TotalPlannedHours = 5.0,
                TotalActualHours = 5.5
            };

            var dialog = new WeeklyGoalAchievedDialog(summary)
            {
                AutoDismissSeconds = 25
            };

            Assert.Equal(25, dialog.RemainingSeconds);

            dialog.TickAutoDismiss();
            Assert.Equal(24, dialog.RemainingSeconds);

            dialog.TriggerAutoDismiss();
            Assert.Equal(0, dialog.RemainingSeconds);
        });
    }

    [Fact]
    public void ProjectGoalAchievedDialog_AutoDismiss_CountsDownAndDismisses()
    {
        RunInSta(() =>
        {
            var summary = new ProjectGoalSummary
            {
                ProjectName = "AlphaProject",
                Date = DateTime.Today,
                PlannedHours = 2.0,
                ActualHours = 2.0,
                IsWeekly = false
            };

            var dialog = new ProjectGoalAchievedDialog(summary)
            {
                AutoDismissSeconds = 25
            };

            Assert.Equal(25, dialog.RemainingSeconds);

            dialog.TickAutoDismiss();
            Assert.Equal(24, dialog.RemainingSeconds);

            dialog.TriggerAutoDismiss();
            Assert.Equal(0, dialog.RemainingSeconds);
        });
    }

    [Fact]
    public void ProjectGoalAchievedDialog_WeeklyVariant_RendersWeeklyHeaders()
    {
        RunInSta(() =>
        {
            var summary = new ProjectGoalSummary
            {
                ProjectName = "BetaProject",
                Date = DateTime.Today,
                PlannedHours = 10.0,
                ActualHours = 11.0,
                IsWeekly = true
            };

            var dialog = new ProjectGoalAchievedDialog(summary);
            Assert.Contains("BetaProject", dialog.Title);
            Assert.Contains("Weekly Goal Passed", dialog.Title);
        });
    }

    [Theory]
    [InlineData("Gold")]
    [InlineData("Emerald")]
    [InlineData("Coral")]
    [InlineData("Violet")]
    [InlineData("Ocean")]
    [InlineData("Rose")]
    [InlineData("ThemeDefault")]
    public void RewardColorHelper_AllPalettes_ReturnValidBrushesForLightAndDarkThemes(string colorKey)
    {
        // Dark theme test (PitchBlack)
        ThemeManager.ApplyTheme(AppTheme.PitchBlack, save: false);
        var (darkBg, darkBorder, darkText) = RewardColorHelper.GetRewardBrushes(colorKey);
        Assert.NotNull(darkBg);
        Assert.NotNull(darkBorder);
        Assert.NotNull(darkText);

        // Light theme test (CandyPop)
        ThemeManager.ApplyTheme(AppTheme.CandyPop, save: false);
        var (lightBg, lightBorder, lightText) = RewardColorHelper.GetRewardBrushes(colorKey);
        Assert.NotNull(lightBg);
        Assert.NotNull(lightBorder);
        Assert.NotNull(lightText);

        // Reset to ModernLight
        ThemeManager.ApplyTheme(AppTheme.ModernLight, save: false);
    }

    [Fact]
    public void ThemeManager_ApplyAllAvailableThemes_DoesNotThrow()
    {
        foreach (var option in ThemeManager.AvailableThemes)
        {
            ThemeManager.ApplyTheme(option.Theme, save: false);
            Assert.Equal(option.Theme, ThemeManager.CurrentTheme);
        }

        ThemeManager.ApplyTheme(AppTheme.ModernLight, save: false);
    }

    [Fact]
    public void RewardColorHelper_CandyPopTheme_UsesLightPalette()
    {
        ThemeManager.ApplyTheme(AppTheme.CandyPop, save: false);

        var (goldBg, goldBorder, goldText) = RewardColorHelper.GetRewardBrushes("Gold");
        Assert.NotNull(goldBg);
        Assert.NotNull(goldBorder);
        Assert.NotNull(goldText);

        // Light gold bg is #FEF3C7, not dark #451A03
        var goldColor = ((System.Windows.Media.SolidColorBrush)goldBg).Color;
        Assert.True(goldColor.R > 200 && goldColor.G > 200);

        // Reset theme back to ModernLight
        ThemeManager.ApplyTheme(AppTheme.ModernLight, save: false);
    }

    [Fact]
    public void ActiveTracking_ContinuesTrackingInBackground_WhenGoalAchieved()
    {
        var testDate = new DateTime(2026, 10, 19);
        var tracker = new GoalMilestoneTracker(_planStorage);

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "FlowProject", PlannedHours = 1.0 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        int triggerCount = 0;
        tracker.ProjectGoalAchieved += (_, _) => triggerCount++;

        // At 50 mins: no goal
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(50), "FlowProject");
        Assert.Equal(0, triggerCount);

        // At 60 mins: goal achieved popup fired
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(60), "FlowProject");
        Assert.Equal(1, triggerCount);

        // Background tracking continues seamlessly without interruption (65 mins, 70 mins, 90 mins)
        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(65), "FlowProject");
        Assert.Equal(1, triggerCount);

        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(70), "FlowProject");
        Assert.Equal(1, triggerCount);

        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(90), "FlowProject");
        Assert.Equal(1, triggerCount);
    }

    [Fact]
    public void GoalMilestoneTracker_WithCustomMilestoneVolumeSettings_EvaluatesCorrectEffectiveVolume()
    {
        var testDate = new DateTime(2026, 10, 19);
        var settings = new WorkRestSettings
        {
            SoundAlertEnabled = true,
            SoundVolume = 30, // Normal timer volume
            GoalMilestoneSoundVolume = 95 // Loud goal milestone fanfare
        };

        var tracker = new GoalMilestoneTracker(_planStorage, settings);
        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = testDate, ProjectName = "LoudProject", PlannedHours = 1.0 }
        });

        var entries = new List<TimeEntry>();
        tracker.Initialize(testDate, entries);

        bool triggered = false;
        tracker.ProjectGoalAchieved += (_, _) =>
        {
            triggered = true;
            // Verify that at the moment of milestone achievement, the effective volume for daily/project goal is 95
            var milestoneVolume = SoundEffectManager.GetEffectiveVolume(SoundEffectType.DailyGoalAchieved, settings);
            Assert.Equal(95, milestoneVolume);
        };

        tracker.CheckMilestones(testDate, entries, TimeSpan.FromMinutes(60), "LoudProject");
        Assert.True(triggered);

        // When goal milestone volume is muted (0)
        settings.GoalMilestoneSoundVolume = 0;
        var mutedMilestoneVolume = SoundEffectManager.GetEffectiveVolume(SoundEffectType.DailyGoalAchieved, settings);
        Assert.Equal(0, mutedMilestoneVolume);

        // Master timer volume is still unaffected at 30
        var timerVolume = SoundEffectManager.GetEffectiveVolume(SoundEffectType.Start, settings);
        Assert.Equal(30, timerVolume);
    }

    [Fact]
    public void GetEffectiveEntryDate_SameDay_ReturnsStartTimeDate()
    {
        var testDate = new DateTime(2026, 10, 21);
        var entry = new TimeEntry
        {
            ProjectName = "Project Alpha",
            StartTime = testDate.AddHours(14),
            EndTime = testDate.AddHours(16)
        };

        var effDate = _planStorage.GetEffectiveEntryDate(entry);
        Assert.Equal(testDate.Date, effDate);
    }

    [Fact]
    public void GetEffectiveEntryDate_Overnight_WithExistingPlanOnStartDate_ReturnsStartDate()
    {
        var day1 = new DateTime(2026, 10, 21); // Wednesday
        var day2 = new DateTime(2026, 10, 22); // Thursday

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = day1, ProjectName = "Project Alpha", PlannedHours = 2.0 }
        });

        // Started 11pm on Wednesday, ended 1am on Thursday
        var entry = new TimeEntry
        {
            ProjectName = "Project Alpha",
            StartTime = day1.AddHours(23),
            EndTime = day2.AddHours(1)
        };

        var effDate = _planStorage.GetEffectiveEntryDate(entry);
        Assert.Equal(day1.Date, effDate);
    }

    [Fact]
    public void GetEffectiveEntryDate_Overnight_WithoutExistingPlanOnStartDate_ReturnsEndDate()
    {
        var day1 = new DateTime(2026, 10, 21); // Wednesday
        var day2 = new DateTime(2026, 10, 22); // Thursday

        // No plan on day1 for Project Beta, but planned on day2
        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = day2, ProjectName = "Project Beta", PlannedHours = 2.0 }
        });

        var entry = new TimeEntry
        {
            ProjectName = "Project Beta",
            StartTime = day1.AddHours(23),
            EndTime = day2.AddHours(1)
        };

        var effDate = _planStorage.GetEffectiveEntryDate(entry);
        Assert.Equal(day2.Date, effDate);
    }

    [Fact]
    public void GetEffectiveTrackingDate_ActiveOvernightTracking_WithPlanOnStartDate_ReturnsStartDate()
    {
        var day1 = new DateTime(2026, 10, 21); // Wednesday
        var day2 = new DateTime(2026, 10, 22); // Thursday

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = day1, ProjectName = "Project Alpha", PlannedHours = 2.0 }
        });

        var startTime = day1.AddHours(23); // 11pm Wednesday
        var currentDate = day2; // Thursday

        var effDate = _planStorage.GetEffectiveTrackingDate(startTime, currentDate, "Project Alpha");
        Assert.Equal(day1.Date, effDate);
    }

    [Fact]
    public void GetEffectiveTrackingDate_ActiveOvernightTracking_WithoutPlanOnStartDate_ReturnsEndDate()
    {
        var day1 = new DateTime(2026, 10, 21); // Wednesday
        var day2 = new DateTime(2026, 10, 22); // Thursday

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = day2, ProjectName = "Project Beta", PlannedHours = 2.0 }
        });

        var startTime = day1.AddHours(23); // 11pm Wednesday
        var currentDate = day2; // Thursday

        var effDate = _planStorage.GetEffectiveTrackingDate(startTime, currentDate, "Project Beta");
        Assert.Equal(day2.Date, effDate);
    }

    [Fact]
    public void GoalMilestoneTracker_OvernightTracking_WithPlanOnStartDate_TriggersGoalForStartDate()
    {
        var day1 = new DateTime(2026, 10, 21); // Wednesday
        var day2 = new DateTime(2026, 10, 22); // Thursday

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = day1, ProjectName = "Project Alpha", PlannedHours = 1.5 }
        });

        var tracker = new GoalMilestoneTracker(_planStorage);
        var entries = new List<TimeEntry>();
        tracker.Initialize(day1, entries);

        ProjectGoalSummary? triggeredProjectSummary = null;
        tracker.ProjectGoalAchieved += (summary, _) =>
        {
            triggeredProjectSummary = summary;
        };

        var activeStart = day1.AddHours(23); // Started 11pm on Wednesday
        // Active tracking across midnight: now 12:40am on Thursday (100 minutes elapsed)
        tracker.CheckMilestones(day2, entries, TimeSpan.FromMinutes(100), "Project Alpha", activeStart);

        Assert.NotNull(triggeredProjectSummary);
        Assert.Equal("Project Alpha", triggeredProjectSummary.ProjectName);
        Assert.Equal(day1.Date, triggeredProjectSummary.Date);
    }

    [Fact]
    public void GoalMilestoneTracker_OvernightTracking_WithoutPlanOnStartDate_TriggersGoalForNewDay()
    {
        var day1 = new DateTime(2026, 10, 21); // Wednesday
        var day2 = new DateTime(2026, 10, 22); // Thursday

        _planStorage.Save(new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Date = day2, ProjectName = "Project Beta", PlannedHours = 1.0 }
        });

        var tracker = new GoalMilestoneTracker(_planStorage);
        var entries = new List<TimeEntry>();
        tracker.Initialize(day1, entries);

        ProjectGoalSummary? triggeredProjectSummary = null;
        tracker.ProjectGoalAchieved += (summary, _) =>
        {
            triggeredProjectSummary = summary;
        };

        var activeStart = day1.AddHours(23); // Started 11pm on Wednesday
        // Active tracking across midnight: now 12:15am on Thursday (75 minutes elapsed)
        tracker.CheckMilestones(day2, entries, TimeSpan.FromMinutes(75), "Project Beta", activeStart);

        Assert.NotNull(triggeredProjectSummary);
        Assert.Equal("Project Beta", triggeredProjectSummary.ProjectName);
        Assert.Equal(day2.Date, triggeredProjectSummary.Date);
    }

    [Fact]
    public void CalendarPlanStorage_OvernightEntry_WithPlanOnStartDate_TicksOffStartDatePlan()
    {
        var day1 = new DateTime(2026, 10, 21); // Wednesday
        var day2 = new DateTime(2026, 10, 22); // Thursday

        var planDay1 = new PlannedWorkItem { Date = day1, ProjectName = "Project Alpha", PlannedHours = 2.0 };
        var planDay2 = new PlannedWorkItem { Date = day2, ProjectName = "Project Alpha", PlannedHours = 2.0 };
        _planStorage.Save(new List<PlannedWorkItem> { planDay1, planDay2 });

        // User tracked from 11pm Wednesday to 1:15am Thursday (2.25 hours)
        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                StartTime = day1.AddHours(23),
                EndTime = day2.AddHours(1).AddMinutes(15)
            }
        };

        // Day 1 plan should have 2.25 hours and be completed (ticked off)
        double day1ActualHours = _planStorage.CalculatePlanItemActualHours(planDay1, entries);
        bool day1Completed = _planStorage.IsPlanItemCompleted(planDay1, entries);
        Assert.Equal(2.25, day1ActualHours);
        Assert.True(day1Completed);

        // Day 2 plan should have 0 hours and NOT be completed
        double day2ActualHours = _planStorage.CalculatePlanItemActualHours(planDay2, entries);
        bool day2Completed = _planStorage.IsPlanItemCompleted(planDay2, entries);
        Assert.Equal(0.0, day2ActualHours);
        Assert.False(day2Completed);

        // Daily summaries
        var day1Summary = _planStorage.CalculateDailySummary(day1, entries);
        var day2Summary = _planStorage.CalculateDailySummary(day2, entries);
        Assert.Equal(2.25, day1Summary.ActualHours);
        Assert.True(day1Summary.IsGoalMet);
        Assert.Equal(0.0, day2Summary.ActualHours);
        Assert.False(day2Summary.IsGoalMet);
    }

    [Fact]
    public void CalendarPlanStorage_OvernightEntry_WithoutPlanOnStartDate_TicksOffNewDayPlan()
    {
        var day1 = new DateTime(2026, 10, 21); // Wednesday
        var day2 = new DateTime(2026, 10, 22); // Thursday

        // No plan on day 1 for Project Beta, but planned on day 2
        var planDay2 = new PlannedWorkItem { Date = day2, ProjectName = "Project Beta", PlannedHours = 2.0 };
        _planStorage.Save(new List<PlannedWorkItem> { planDay2 });

        // User tracked from 11pm Wednesday to 1:15am Thursday (2.25 hours)
        var entries = new List<TimeEntry>
        {
            new TimeEntry
            {
                ProjectName = "Project Beta",
                StartTime = day1.AddHours(23),
                EndTime = day2.AddHours(1).AddMinutes(15)
            }
        };

        // Day 2 plan should have 2.25 hours and be completed (ticked off)
        double day2ActualHours = _planStorage.CalculatePlanItemActualHours(planDay2, entries);
        bool day2Completed = _planStorage.IsPlanItemCompleted(planDay2, entries);
        Assert.Equal(2.25, day2ActualHours);
        Assert.True(day2Completed);

        // Daily summaries
        var day1Summary = _planStorage.CalculateDailySummary(day1, entries);
        var day2Summary = _planStorage.CalculateDailySummary(day2, entries);
        Assert.Equal(0.0, day1Summary.ActualHours);
        Assert.Equal(2.25, day2Summary.ActualHours);
        Assert.True(day2Summary.IsGoalMet);
    }

    [Fact]
    public void WeeklySummary_OvernightEntry_CorrectlyAllocatesToAppropriateDay()
    {
        var day1 = new DateTime(2026, 10, 21); // Wednesday
        var day2 = new DateTime(2026, 10, 22); // Thursday

        var planDay1 = new PlannedWorkItem { Date = day1, ProjectName = "Project Alpha", PlannedHours = 1.0 };
        var planDay2 = new PlannedWorkItem { Date = day2, ProjectName = "Project Beta", PlannedHours = 1.0 };
        _planStorage.Save(new List<PlannedWorkItem> { planDay1, planDay2 });

        var entries = new List<TimeEntry>
        {
            // Alpha overnight with plan on day1 -> allocated to Wednesday
            new TimeEntry
            {
                ProjectName = "Project Alpha",
                StartTime = day1.AddHours(23),
                EndTime = day2.AddHours(0.5) // 1.5h
            },
            // Beta overnight WITHOUT plan on day1 -> allocated to Thursday
            new TimeEntry
            {
                ProjectName = "Project Beta",
                StartTime = day1.AddHours(23),
                EndTime = day2.AddHours(0.5) // 1.5h
            }
        };

        var weeklySummary = _planStorage.CalculateWeeklySummary(day1, entries);
        Assert.Equal(2.0, weeklySummary.TotalPlannedHours);
        Assert.Equal(3.0, weeklySummary.TotalActualHours);

        var wedSummary = weeklySummary.DailySummaries.First(d => d.Date.Date == day1.Date);
        var thuSummary = weeklySummary.DailySummaries.First(d => d.Date.Date == day2.Date);

        Assert.Equal(1.5, wedSummary.ActualHours);
        Assert.True(wedSummary.IsGoalMet);

        Assert.Equal(1.5, thuSummary.ActualHours);
        Assert.True(thuSummary.IsGoalMet);
    }
}
