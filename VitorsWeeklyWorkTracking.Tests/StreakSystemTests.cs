using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class StreakSystemTests : IDisposable
{
    private readonly string _tempPlanFilePath;
    private readonly string _tempStreakFilePath;
    private readonly CalendarPlanStorage _planStorage;
    private readonly StreakStorage _streakStorage;
    private readonly StreakService _streakService;

    public StreakSystemTests()
    {
        _tempPlanFilePath = Path.Combine(Path.GetTempPath(), $"calendar-plans-test-{Guid.NewGuid():N}.json");
        _tempStreakFilePath = Path.Combine(Path.GetTempPath(), $"streaks-test-{Guid.NewGuid():N}.json");

        _planStorage = new CalendarPlanStorage(_tempPlanFilePath);
        CalendarPlanStorage.SetInstanceForTesting(_planStorage);

        _streakStorage = new StreakStorage(_tempStreakFilePath);
        _streakService = new StreakService(_streakStorage, _planStorage);
    }

    public void Dispose()
    {
        CalendarPlanStorage.SetInstanceForTesting(null);
        try
        {
            if (File.Exists(_tempPlanFilePath)) File.Delete(_tempPlanFilePath);
            if (File.Exists(_tempStreakFilePath)) File.Delete(_tempStreakFilePath);
        }
        catch { }
    }

    [Fact]
    public void EvaluateDate_GoldTier_AwardedWhenWorkIsSavedWithoutGoals()
    {
        var date = new DateTime(2026, 11, 2); // Monday
        var entries = new List<TimeEntry>
        {
            new()
            {
                ProjectName = "General Work",
                Activity = "Coding",
                StartTime = date.AddHours(9),
                EndTime = date.AddHours(11) // 2 hours
            }
        };

        var eval = _streakService.EvaluateDate(date, entries, date);

        Assert.Equal(StreakDayStatus.Active, eval.Status);
        Assert.Equal(StreakTier.Gold, eval.Tier);
        Assert.Equal(2.0, eval.ActualHours);
        Assert.Contains("No planned goals scheduled", eval.Reason);
    }

    [Fact]
    public void EvaluateDate_NoWorkToday_ShowsPendingInsteadOfMissed()
    {
        var today = new DateTime(2026, 11, 4); // Wednesday
        _streakService.Profile.AutoApplySickDays = false;

        var eval = _streakService.EvaluateDate(today, new List<TimeEntry>(), today);

        Assert.Equal(StreakDayStatus.Future, eval.Status);
        Assert.Equal("Pending today's work", eval.Reason);
        Assert.Equal("PENDING", eval.TierBadgeText);
    }

    [Fact]
    public void EvaluateDate_BronzeTier_AwardedWhenGoalsAreScheduledButNotCompleted()
    {
        var date = new DateTime(2026, 11, 2); // Monday

        // Plan a project with 3 hours planned
        _planStorage.AddOrUpdatePlan(new PlannedWorkItem
        {
            Id = "p1",
            Date = date,
            ProjectName = "Project Alpha",
            PlannedHours = 3.0
        });

        // User works 1 hour on another project (or less than planned on Project Alpha without completing it)
        var entries = new List<TimeEntry>
        {
            new()
            {
                ProjectName = "Project Beta",
                StartTime = date.AddHours(9),
                EndTime = date.AddHours(10) // 1 hour
            }
        };

        var eval = _streakService.EvaluateDate(date, entries, date);

        Assert.Equal(StreakDayStatus.Active, eval.Status);
        Assert.Equal(StreakTier.Bronze, eval.Tier);
        Assert.Equal(1.0, eval.ActualHours);
        Assert.Equal(0, eval.CompletedProjectGoalsCount);
        Assert.Equal(1, eval.TotalPlannedProjectGoalsCount);
    }

    [Fact]
    public void EvaluateDate_SilverTier_AwardedWhenOneOfMultipleProjectGoalsCompleted()
    {
        var date = new DateTime(2026, 11, 2); // Monday

        // Plan 2 projects for the day: Project A (2h) and Project B (3h)
        _planStorage.AddOrUpdatePlan(new PlannedWorkItem
        {
            Id = "p1",
            Date = date,
            ProjectName = "Project A",
            PlannedHours = 2.0
        });
        _planStorage.AddOrUpdatePlan(new PlannedWorkItem
        {
            Id = "p2",
            Date = date,
            ProjectName = "Project B",
            PlannedHours = 3.0
        });

        // User works 2.5h on Project A (completed) and 0h on Project B (not completed)
        var entries = new List<TimeEntry>
        {
            new()
            {
                ProjectName = "Project A",
                StartTime = date.AddHours(9),
                EndTime = date.AddHours(11.5)
            }
        };

        var eval = _streakService.EvaluateDate(date, entries, date);

        Assert.Equal(StreakDayStatus.Active, eval.Status);
        Assert.Equal(StreakTier.Silver, eval.Tier);
        Assert.Equal(1, eval.CompletedProjectGoalsCount);
        Assert.Equal(2, eval.TotalPlannedProjectGoalsCount);
    }

    [Fact]
    public void EvaluateDate_GoldTier_AwardedWhenAllScheduledGoalsCompleted()
    {
        var date = new DateTime(2026, 11, 2); // Monday

        // Plan 2 projects for the day: Project A (2h) and Project B (1h)
        _planStorage.AddOrUpdatePlan(new PlannedWorkItem
        {
            Id = "p1",
            Date = date,
            ProjectName = "Project A",
            PlannedHours = 2.0
        });
        _planStorage.AddOrUpdatePlan(new PlannedWorkItem
        {
            Id = "p2",
            Date = date,
            ProjectName = "Project B",
            PlannedHours = 1.0
        });

        // User works 2.0h on Project A and 1.0h on Project B -> All completed!
        var entries = new List<TimeEntry>
        {
            new()
            {
                ProjectName = "Project A",
                StartTime = date.AddHours(9),
                EndTime = date.AddHours(11)
            },
            new()
            {
                ProjectName = "Project B",
                StartTime = date.AddHours(13),
                EndTime = date.AddHours(14)
            }
        };

        var eval = _streakService.EvaluateDate(date, entries, date);

        Assert.Equal(StreakDayStatus.Active, eval.Status);
        Assert.Equal(StreakTier.Gold, eval.Tier);
        Assert.Equal(2, eval.CompletedProjectGoalsCount);
        Assert.Equal(2, eval.TotalPlannedProjectGoalsCount);
    }

    [Fact]
    public void CalculateStreak_ConsecutiveWorkDays_CalculatesStreakAndCounts()
    {
        var today = new DateTime(2026, 11, 4); // Wednesday
        var day1 = new DateTime(2026, 11, 2); // Monday
        var day2 = new DateTime(2026, 11, 3); // Tuesday
        var day3 = new DateTime(2026, 11, 4); // Wednesday

        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = day1.AddHours(9), EndTime = day1.AddHours(10) },
            new() { ProjectName = "P1", StartTime = day2.AddHours(9), EndTime = day2.AddHours(11) },
            new() { ProjectName = "P1", StartTime = day3.AddHours(9), EndTime = day3.AddHours(12) }
        };

        var summary = _streakService.CalculateStreak(today, entries);

        Assert.Equal(3, summary.CurrentStreak);
        Assert.Equal(3, summary.LongestStreak);
        Assert.Equal(3, summary.TotalActiveDays);
        Assert.Equal(3, summary.GoldDaysCount);
    }

    [Fact]
    public void CalculateStreak_ExcludedWeekendDays_PreservesStreakWithoutBreaking()
    {
        // Monday Nov 2 to Friday Nov 6: 5 active work days
        // Saturday Nov 7 & Sunday Nov 8: Excluded rest days (no work)
        // Monday Nov 9: Active work day
        var today = new DateTime(2026, 11, 9); // Monday next week
        var entries = new List<TimeEntry>();

        for (int i = 0; i < 5; i++)
        {
            var d = new DateTime(2026, 11, 2).AddDays(i); // Mon..Fri
            entries.Add(new TimeEntry
            {
                ProjectName = "Project X",
                StartTime = d.AddHours(9),
                EndTime = d.AddHours(12)
            });
        }

        // Add work for Monday Nov 9
        entries.Add(new TimeEntry
        {
            ProjectName = "Project X",
            StartTime = today.AddHours(9),
            EndTime = today.AddHours(12)
        });

        // Set excluded days to Sat & Sun
        _streakService.SetExcludedDays(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });

        var summary = _streakService.CalculateStreak(today, entries);

        // Current streak should seamlessly connect Friday and Monday: 6 active days
        Assert.Equal(6, summary.CurrentStreak);
        Assert.Equal(6, summary.LongestStreak);
        Assert.Equal(6, summary.TotalActiveDays);
    }

    [Fact]
    public void PreNotifiedHolidays_PreservesStreakAcrossHoliday()
    {
        var tuesday = new DateTime(2026, 11, 3);
        var wednesdayHoliday = new DateTime(2026, 11, 4);
        var thursday = new DateTime(2026, 11, 5);

        // Pre-notify holiday on Wednesday
        _streakService.AddHoliday(wednesdayHoliday, "National Holiday");

        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = tuesday.AddHours(9), EndTime = tuesday.AddHours(12) },
            // Wednesday has 0 work logged (holiday)
            new() { ProjectName = "P1", StartTime = thursday.AddHours(9), EndTime = thursday.AddHours(12) }
        };

        var summary = _streakService.CalculateStreak(thursday, entries);

        Assert.Equal(2, summary.CurrentStreak);
        Assert.Equal(2, summary.LongestStreak);
    }

    [Fact]
    public void HolidayManagement_AddAndRemoveHolidays_UpdatesProfile()
    {
        var holDate = new DateTime(2026, 12, 25);
        _streakService.AddHoliday(holDate, "Christmas Day");

        Assert.True(_streakService.Profile.IsHoliday(holDate, out var name));
        Assert.Equal("Christmas Day", name);

        _streakService.RemoveHoliday(holDate);
        Assert.False(_streakService.Profile.IsHoliday(holDate, out _));
    }

    [Fact]
    public void HolidayRange_AddHolidayRange_AddsAllDaysAndPreservesStreak()
    {
        var friday = new DateTime(2026, 11, 6);
        var mondayNextWeek = new DateTime(2026, 11, 16);

        // Pre-notify 1-week holiday range from Monday Nov 9 to Friday Nov 13
        var rangeStart = new DateTime(2026, 11, 9);
        var rangeEnd = new DateTime(2026, 11, 13);
        int added = _streakService.AddHolidayRange(rangeStart, rangeEnd, "Trip to Japan");

        Assert.Equal(5, added);
        for (var d = rangeStart; d <= rangeEnd; d = d.AddDays(1))
        {
            Assert.True(_streakService.Profile.IsHoliday(d, out var name));
            Assert.Equal("Trip to Japan", name);
        }

        // Work on Friday Nov 6, weekend Nov 7-8 excluded, 5 days holiday Nov 9-13, weekend Nov 14-15 excluded, work on Monday Nov 16
        _streakService.SetExcludedDays(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });
        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = friday.AddHours(9), EndTime = friday.AddHours(12) },
            new() { ProjectName = "P1", StartTime = mondayNextWeek.AddHours(9), EndTime = mondayNextWeek.AddHours(12) }
        };

        var summary = _streakService.CalculateStreak(mondayNextWeek, entries);
        Assert.Equal(2, summary.CurrentStreak);
    }

    [Fact]
    public void HolidayRange_SwappedDates_NormalizesRangeOrder()
    {
        var start = new DateTime(2026, 12, 10);
        var end = new DateTime(2026, 12, 5);

        // Pass end date as start and start date as end
        int count = _streakService.AddHolidayRange(start, end, "Winter Vacation");

        Assert.Equal(6, count); // Dec 5, 6, 7, 8, 9, 10 = 6 days
        Assert.True(_streakService.Profile.IsHoliday(new DateTime(2026, 12, 5), out _));
        Assert.True(_streakService.Profile.IsHoliday(new DateTime(2026, 12, 10), out _));
        Assert.True(_streakService.Profile.IsHoliday(new DateTime(2026, 12, 7), out _));
    }

    [Fact]
    public void HolidayRange_OverlappingRange_UpdatesDescriptionsWithoutDuplicates()
    {
        var start = new DateTime(2026, 7, 1);
        var end = new DateTime(2026, 7, 3);

        _streakService.AddHolidayRange(start, end, "Summer Camp");
        Assert.Equal(3, _streakService.Profile.Holidays.Count);

        // Extend or overlap range: July 2 to July 5 with new description
        _streakService.AddHolidayRange(new DateTime(2026, 7, 2), new DateTime(2026, 7, 5), "Beach Resort");

        // Total distinct days should be July 1, 2, 3, 4, 5 = 5 days total
        Assert.Equal(5, _streakService.Profile.Holidays.Count);
        Assert.True(_streakService.Profile.IsHoliday(new DateTime(2026, 7, 1), out var desc1));
        Assert.Equal("Summer Camp", desc1);
        Assert.True(_streakService.Profile.IsHoliday(new DateTime(2026, 7, 2), out var desc2));
        Assert.Equal("Beach Resort", desc2);
        Assert.True(_streakService.Profile.IsHoliday(new DateTime(2026, 7, 5), out var desc5));
        Assert.Equal("Beach Resort", desc5);
    }

    [Fact]
    public void HolidayRange_RemoveHolidayRange_RemovesOnlyTargetDays()
    {
        _streakService.AddHolidayRange(new DateTime(2026, 8, 1), new DateTime(2026, 8, 10), "August Break");
        Assert.Equal(10, _streakService.Profile.Holidays.Count);

        // Remove mid-range days: Aug 4 to Aug 6 (3 days)
        int removed = _streakService.RemoveHolidayRange(new DateTime(2026, 8, 4), new DateTime(2026, 8, 6));
        Assert.Equal(3, removed);
        Assert.Equal(7, _streakService.Profile.Holidays.Count);

        Assert.True(_streakService.Profile.IsHoliday(new DateTime(2026, 8, 3), out _));
        Assert.False(_streakService.Profile.IsHoliday(new DateTime(2026, 8, 5), out _));
        Assert.True(_streakService.Profile.IsHoliday(new DateTime(2026, 8, 7), out _));
    }

    [Fact]
    public void HolidayRange_ClearAllHolidays_RemovesAllEntries()
    {
        _streakService.AddHolidayRange(new DateTime(2026, 9, 1), new DateTime(2026, 9, 5), "Vacation");
        Assert.Equal(5, _streakService.Profile.Holidays.Count);

        _streakService.ClearAllHolidays();
        Assert.Empty(_streakService.Profile.Holidays);
    }

    [Fact]
    public void StreakDialog_HolidayRangeMode_TogglesVisibilityAndPreviewsDuration()
    {
        RunInSta(() =>
        {
            var dialog = new StreakDialog(new List<TimeEntry>(), _streakService);

            // Default: single day mode
            Assert.False(dialog.HolidayDateRangeCheckBox.IsChecked);
            Assert.Equal(System.Windows.Visibility.Collapsed, dialog.HolidayEndDateContainer.Visibility);
            Assert.Equal(System.Windows.Visibility.Collapsed, dialog.HolidayQuickPresetsPanel.Visibility);
            Assert.Equal("➕ Add Holiday", dialog.AddHolidayButton.Content.ToString());

            // Switch to range mode
            dialog.HolidayDateRangeCheckBox.IsChecked = true;
            Assert.Equal(System.Windows.Visibility.Visible, dialog.HolidayEndDateContainer.Visibility);
            Assert.Equal(System.Windows.Visibility.Visible, dialog.HolidayQuickPresetsPanel.Visibility);
            Assert.Equal(System.Windows.Visibility.Visible, dialog.HolidayRangeDurationText.Visibility);
            Assert.Contains("day", dialog.HolidayRangeDurationText.Text);

            // Switch back to single day mode
            dialog.HolidayDateRangeCheckBox.IsChecked = false;
            Assert.Equal(System.Windows.Visibility.Collapsed, dialog.HolidayEndDateContainer.Visibility);
            Assert.Equal(System.Windows.Visibility.Collapsed, dialog.HolidayQuickPresetsPanel.Visibility);
            Assert.Equal("➕ Add Holiday", dialog.AddHolidayButton.Content.ToString());
        });
    }

    [Fact]
    public void StreakDialog_HolidayRangeMode_QuickPresetsAndAdd()
    {
        RunInSta(() =>
        {
            _streakService.ClearAllHolidays();
            var dialog = new StreakDialog(new List<TimeEntry>(), _streakService);

            var start = new DateTime(2026, 11, 10);
            dialog.HolidayDatePicker.SelectedDate = start;
            dialog.HolidayDateRangeCheckBox.IsChecked = true;

            // Find +7d button in HolidayQuickPresetsPanel
            var quickButtons = dialog.HolidayQuickPresetsPanel.Children.OfType<System.Windows.Controls.Button>().ToList();
            var sevenDaysBtn = quickButtons.FirstOrDefault(b => (string)b.Tag == "7");
            Assert.NotNull(sevenDaysBtn);

            sevenDaysBtn.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

            Assert.Equal(start.AddDays(6), dialog.HolidayEndDatePicker.SelectedDate);
            Assert.Contains("7 days", dialog.HolidayRangeDurationText.Text);
            Assert.Equal("➕ Add Range (7d)", dialog.AddHolidayButton.Content.ToString());

            // Add holiday range
            dialog.HolidayDescriptionTextBox.Text = "Thanksgiving Break";
            dialog.AddHolidayButton.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

            Assert.Equal(7, _streakService.Profile.Holidays.Count);
            Assert.True(_streakService.Profile.IsHoliday(start, out var desc1));
            Assert.Equal("Thanksgiving Break", desc1);
            Assert.True(_streakService.Profile.IsHoliday(start.AddDays(6), out var desc7));
            Assert.Equal("Thanksgiving Break", desc7);
        });
    }

    [Fact]
    public void SickDaySystem_RestoresStreakAndDecrementsMonthlyCounter()
    {
        var mon = new DateTime(2026, 11, 2);
        var tueMissed = new DateTime(2026, 11, 3);
        var wed = new DateTime(2026, 11, 4);

        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = mon.AddHours(9), EndTime = mon.AddHours(12) },
            // Tuesday: missed (0 work)
            new() { ProjectName = "P1", StartTime = wed.AddHours(9), EndTime = wed.AddHours(12) }
        };

        // Before using sick day, without sick day Tuesday breaks the streak:
        _streakService.Profile.AutoApplySickDays = false;
        _streakService.SaveProfile();

        var summaryBefore = _streakService.CalculateStreak(wed, entries);
        // Only Wednesday counts in current streak because Tuesday broke it:
        Assert.Equal(1, summaryBefore.CurrentStreak);

        // Now apply sick day on Tuesday
        bool used = _streakService.UseSickDay(tueMissed);
        Assert.True(used);
        Assert.Equal(2, _streakService.GetRemainingSickDays(tueMissed)); // 3 - 1 = 2 remaining

        var summaryAfter = _streakService.CalculateStreak(wed, entries);
        // Streak is restored across Tuesday!
        Assert.Equal(2, summaryAfter.CurrentStreak);
    }

    [Fact]
    public void SickDaySystem_MonthlyRefresh_ResetsTo3OnNewCalendarMonth()
    {
        var novDate1 = new DateTime(2026, 11, 10);
        var novDate2 = new DateTime(2026, 11, 15);
        var novDate3 = new DateTime(2026, 11, 20);

        _streakService.UseSickDay(novDate1);
        _streakService.UseSickDay(novDate2);
        _streakService.UseSickDay(novDate3);

        // All 3 used in November 2026:
        Assert.Equal(0, _streakService.GetRemainingSickDays(new DateTime(2026, 11, 25)));
        Assert.False(_streakService.Profile.HasRemainingSickDay(2026, 11));

        // When December 2026 arrives, monthly allowance is freshly 3!
        var decDate = new DateTime(2026, 12, 5);
        Assert.Equal(3, _streakService.GetRemainingSickDays(decDate));
        Assert.True(_streakService.Profile.HasRemainingSickDay(2026, 12));
    }

    [Fact]
    public void SickDaySystem_RevokeSickDay_RestoresRemainingCounter()
    {
        var date = new DateTime(2026, 11, 5);
        _streakService.UseSickDay(date);
        Assert.Equal(2, _streakService.GetRemainingSickDays(date));

        _streakService.RemoveSickDay(date);
        Assert.Equal(3, _streakService.GetRemainingSickDays(date));
    }

    [Fact]
    public void SickDaySystem_CannotApplyToDateWithRecordedWork()
    {
        var date = new DateTime(2026, 11, 5);
        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = date.AddHours(9), EndTime = date.AddHours(10) }
        };

        Assert.False(_streakService.TryUseSickDay(date, entries, out var reason));
        Assert.Contains("Work is already recorded", reason);
        Assert.False(_streakService.Profile.IsSickDay(date));
        Assert.Equal(3, _streakService.GetRemainingSickDays(date));
    }

    [Fact]
    public void SickDaySystem_CannotApplyToExcludedRestDay()
    {
        var date = new DateTime(2026, 11, 7); // Saturday
        _streakService.SetExcludedDays(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });

        Assert.False(_streakService.TryUseSickDay(date, new List<TimeEntry>(), out var reason));
        Assert.Contains("marked as a regular rest day", reason);
        Assert.False(_streakService.Profile.IsSickDay(date));
        Assert.Equal(3, _streakService.GetRemainingSickDays(date));
    }

    [Fact]
    public void SickDaySystem_CannotApplyToScheduledHoliday()
    {
        var date = new DateTime(2026, 11, 5);
        _streakService.AddHoliday(date, "Personal holiday");

        Assert.False(_streakService.TryUseSickDay(date, new List<TimeEntry>(), out var reason));
        Assert.Contains("marked as a holiday (Personal holiday)", reason);
        Assert.False(_streakService.Profile.IsSickDay(date));
        Assert.Equal(3, _streakService.GetRemainingSickDays(date));
    }

    [Fact]
    public void SickDaySystem_RevokeAfterMonthRefresh_ReturnsCreditEvenWhenCurrentAllowanceIsFull()
    {
        var oldDate = new DateTime(2026, 10, 30);
        var currentDate = new DateTime(2026, 11, 4);
        Assert.True(_streakService.UseSickDay(oldDate));
        Assert.Equal(3, _streakService.GetRemainingSickDays(currentDate));

        Assert.True(_streakService.RemoveSickDay(oldDate));

        Assert.Equal(4, _streakService.GetRemainingSickDays(currentDate));
        var reloadedProfile = _streakStorage.Load();
        Assert.Equal(1, reloadedProfile.ReturnedSickDays);
        Assert.Contains(oldDate, reloadedProfile.RevokedSickDates);
        Assert.True(_streakService.UseSickDay(oldDate));
        Assert.Equal(3, _streakService.GetRemainingSickDays(currentDate));
    }

    [Fact]
    public void SickDaySystem_RevokingStreakDayBreaksStreakAndReapplyingRestoresIt()
    {
        var monday = new DateTime(2026, 11, 2);
        var tuesday = new DateTime(2026, 11, 3);
        var wednesday = new DateTime(2026, 11, 4);
        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = monday.AddHours(9), EndTime = monday.AddHours(12) },
            new() { ProjectName = "P1", StartTime = wednesday.AddHours(9), EndTime = wednesday.AddHours(12) }
        };
        _streakService.Profile.AutoApplySickDays = false;
        Assert.True(_streakService.UseSickDay(tuesday));
        Assert.Equal(2, _streakService.CalculateStreak(wednesday, entries).CurrentStreak);
        Assert.True(_streakService.WouldRevokingSickDayBreakStreak(tuesday, entries, wednesday));

        _streakService.Profile.AutoApplySickDays = true;
        Assert.True(_streakService.RemoveSickDay(tuesday));
        Assert.Equal(1, _streakService.CalculateStreak(wednesday, entries).CurrentStreak);
        Assert.False(_streakService.Profile.IsSickDay(tuesday));

        Assert.True(_streakService.UseSickDay(tuesday));
        Assert.Equal(2, _streakService.CalculateStreak(wednesday, entries).CurrentStreak);
    }

    [Fact]
    public void SettingsToggle_DisablingStreak_HidesStreakMetrics()
    {
        var today = new DateTime(2026, 11, 4);
        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = today.AddHours(9), EndTime = today.AddHours(12) }
        };

        _streakService.IsEnabled = false;

        var summary = _streakService.CalculateStreak(today, entries);

        Assert.False(summary.IsEnabled);
        Assert.Equal("🔥 Streak: Off", summary.DisplayBadge);

        // Re-enable
        _streakService.IsEnabled = true;
        var summaryEnabled = _streakService.CalculateStreak(today, entries);
        Assert.True(summaryEnabled.IsEnabled);
        Assert.Equal(1, summaryEnabled.CurrentStreak);
    }

    [Fact]
    public void Storage_SavesAndLoadsStreakProfile_Correctly()
    {
        var profile = new StreakProfile
        {
            Enabled = true,
            AutoApplySickDays = false,
            ExcludedDays = new List<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
            Holidays = new List<HolidayItem>
            {
                new() { Date = new DateTime(2026, 12, 25), Description = "Christmas" }
            },
            UsedSickDates = new List<DateTime> { new(2026, 11, 3) }
        };

        _streakStorage.Save(profile);
        var loaded = _streakStorage.Load();

        Assert.True(loaded.Enabled);
        Assert.False(loaded.AutoApplySickDays);
        Assert.Equal(3, loaded.ExcludedDays.Count);
        Assert.Contains(DayOfWeek.Friday, loaded.ExcludedDays);
        Assert.Single(loaded.Holidays);
        Assert.Equal("Christmas", loaded.Holidays[0].Description);
        Assert.Single(loaded.UsedSickDates);
    }

    [Fact]
    public void AutoApplySickDays_AutomaticallyProtectsMissedWorkday()
    {
        var monday = new DateTime(2026, 11, 2);
        var tuesdayMissed = new DateTime(2026, 11, 3);
        var wednesday = new DateTime(2026, 11, 4);

        _streakService.Profile.AutoApplySickDays = true;
        _streakService.SaveProfile();

        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = monday.AddHours(9), EndTime = monday.AddHours(12) },
            new() { ProjectName = "P1", StartTime = wednesday.AddHours(9), EndTime = wednesday.AddHours(12) }
        };

        var summary = _streakService.CalculateStreak(wednesday, entries);

        // Auto applied sick day on Tuesday, so streak is preserved across Mon + Tue(sick) + Wed = 2 active days
        Assert.Equal(2, summary.CurrentStreak);
        Assert.Equal(2, _streakService.GetRemainingSickDays(wednesday));
        Assert.True(_streakService.Profile.IsSickDay(tuesdayMissed));
    }

    [Fact]
    public void SickDaySystem_ExceedingMonthlyAllowance_FailsAndBreaksStreak()
    {
        var nov1 = new DateTime(2026, 11, 2); // Mon - Work
        var nov2 = new DateTime(2026, 11, 3); // Tue - Sick 1
        var nov3 = new DateTime(2026, 11, 4); // Wed - Sick 2
        var nov4 = new DateTime(2026, 11, 5); // Thu - Sick 3
        var nov5 = new DateTime(2026, 11, 6); // Fri - Missed 4th (limit exceeded)
        var nov6 = new DateTime(2026, 11, 9); // Mon next week - Work

        _streakService.UseSickDay(nov2);
        _streakService.UseSickDay(nov3);
        _streakService.UseSickDay(nov4);

        // Try applying 4th sick day in November
        bool applied4th = _streakService.UseSickDay(nov5);
        Assert.False(applied4th);
        Assert.Equal(0, _streakService.GetRemainingSickDays(nov5));

        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = nov1.AddHours(9), EndTime = nov1.AddHours(12) },
            new() { ProjectName = "P1", StartTime = nov6.AddHours(9), EndTime = nov6.AddHours(12) }
        };

        var summary = _streakService.CalculateStreak(nov6, entries);
        // nov5 breaks the streak because 4th sick day could not be used
        Assert.Equal(1, summary.CurrentStreak);
    }

    [Fact]
    public void WorkOnExcludedDay_EarnsStreakTierAndIncrementsStreak()
    {
        var saturday = new DateTime(2026, 11, 7); // Excluded weekend day
        _streakService.SetExcludedDays(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });

        var entries = new List<TimeEntry>
        {
            new()
            {
                ProjectName = "Weekend Coding",
                StartTime = saturday.AddHours(10),
                EndTime = saturday.AddHours(13)
            }
        };

        var eval = _streakService.EvaluateDate(saturday, entries, saturday);
        Assert.Equal(StreakDayStatus.Active, eval.Status);
        Assert.Equal(StreakTier.Gold, eval.Tier);

        var summary = _streakService.CalculateStreak(saturday, entries);
        Assert.Equal(1, summary.CurrentStreak);
    }

    [Fact]
    public void MultipleConsecutiveHolidays_PreservesStreakUnbroken()
    {
        var mon = new DateTime(2026, 11, 2);
        var tueHol = new DateTime(2026, 11, 3);
        var wedHol = new DateTime(2026, 11, 4);
        var thuHol = new DateTime(2026, 11, 5);
        var fri = new DateTime(2026, 11, 6);

        _streakService.AddHoliday(tueHol, "Holiday 1");
        _streakService.AddHoliday(wedHol, "Holiday 2");
        _streakService.AddHoliday(thuHol, "Holiday 3");

        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = mon.AddHours(9), EndTime = mon.AddHours(12) },
            new() { ProjectName = "P1", StartTime = fri.AddHours(9), EndTime = fri.AddHours(12) }
        };

        var summary = _streakService.CalculateStreak(fri, entries);
        Assert.Equal(2, summary.CurrentStreak);
        Assert.Equal(2, summary.LongestStreak);
    }

    [Fact]
    public void EmptyEntries_ReturnsZeroStreakAndNoneTier()
    {
        var today = new DateTime(2026, 11, 2);
        var entries = new List<TimeEntry>();

        var summary = _streakService.CalculateStreak(today, entries);

        Assert.Equal(0, summary.CurrentStreak);
        Assert.Equal(0, summary.LongestStreak);
        Assert.Equal(StreakTier.None, summary.TodayTier);
        Assert.Equal("🔥 0", summary.DisplayBadge);
    }

    [Fact]
    public void StreakDayEvaluation_SickDayEmoji_UsesCompatiblePillEmoji()
    {
        var eval = new StreakDayEvaluation
        {
            Date = new DateTime(2026, 11, 2),
            Status = StreakDayStatus.SickDay,
            Tier = StreakTier.None
        };

        Assert.Equal("💊", eval.TierEmoji);
        Assert.Contains("💊", eval.ShortDescription);
    }

    [Fact]
    public void SoundEffectManager_PlayStreakTierAchievedSound_DoesNotThrow()
    {
        var settings = new WorkRestSettings { SoundAlertEnabled = false };
        SoundEffectManager.PlayStreakTierAchievedSound(StreakTier.Bronze, settings);
        SoundEffectManager.PlayStreakTierAchievedSound(StreakTier.Silver, settings);
        SoundEffectManager.PlayStreakTierAchievedSound(StreakTier.Gold, settings);
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new System.Windows.Application();
                    try
                    {
                        var appDict = (System.Windows.ResourceDictionary)System.Windows.Application.LoadComponent(
                            new Uri("/VitorsWeeklyWorkTracking;component/App.xaml", UriKind.Relative));
                        app.Resources.MergedDictionaries.Add(appDict);
                    }
                    catch
                    {
                        // Fallback if component load fails
                    }
                    ThemeManager.Initialize();
                }
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
    public void StreakTierAchievedDialog_InitializesPropertiesAndAutoDismiss()
    {
        RunInSta(() =>
        {
            var summary = new StreakSummary
            {
                CurrentStreak = 5,
                LongestStreak = 10,
                TodayTier = StreakTier.Gold
            };

            var dialog = new StreakTierAchievedDialog(StreakTier.Gold, summary)
            {
                AutoDismissSeconds = 25
            };

            Assert.Equal(StreakTier.Gold, dialog.AchievedTier);
            Assert.False(dialog.RequestOpenStreakDialog);

            dialog.TickAutoDismiss();
            Assert.Equal(24, dialog.RemainingSeconds);

            dialog.TriggerAutoDismiss();
            Assert.Equal(0, dialog.RemainingSeconds);
        });
    }

    [Theory]
    [InlineData(30, "1 Month Streak", "🏆 1 MONTH MILESTONE")]
    [InlineData(60, "2 Months Streak", "⚡ 2 MONTHS MILESTONE")]
    [InlineData(90, "3 Months Streak", "🌟 3 MONTHS (1 QUARTER)")]
    [InlineData(180, "6 Months Streak", "💎 6 MONTHS (HALF YEAR)")]
    [InlineData(365, "1 Year Streak", "👑 1 YEAR STREAK LEGEND")]
    [InlineData(455, "1 Year 3 Months Streak", "🔥 15 MONTHS")]
    [InlineData(545, "1 Year 6 Months Streak", "💎 18 MONTHS")]
    [InlineData(635, "1 Year 9 Months Streak", "⚡ 21 MONTHS")]
    [InlineData(730, "2 Years Streak", "👑 2 YEARS LEGEND")]
    [InlineData(820, "2 Years 3 Months Streak", "🔥 27 MONTHS")]
    [InlineData(910, "2 Years 6 Months Streak", "💎 30 MONTHS")]
    [InlineData(1000, "2 Years 9 Months Streak", "⚡ 33 MONTHS")]
    [InlineData(1095, "3 Years Streak", "👑 3 YEARS LEGEND")]
    public void StreakMilestoneHelper_RecognizesAllSpecifiedMilestoneTiers(int days, string expectedTitle, string expectedBadge)
    {
        bool isMilestone = StreakMilestoneHelper.IsMilestoneStreak(days, out var milestone);

        Assert.True(isMilestone);
        Assert.NotNull(milestone);
        Assert.Equal(days, milestone.Days);
        Assert.Equal(expectedTitle, milestone.Title);
        Assert.Equal(expectedBadge, milestone.Badge);
        Assert.False(string.IsNullOrWhiteSpace(milestone.Headline));
        Assert.False(string.IsNullOrWhiteSpace(milestone.Description));
        Assert.False(string.IsNullOrWhiteSpace(milestone.AsciiArt));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(29)]
    [InlineData(31)]
    [InlineData(59)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(364)]
    [InlineData(366)]
    public void StreakMilestoneHelper_ReturnsFalseForNonMilestoneDays(int days)
    {
        bool isMilestone = StreakMilestoneHelper.IsMilestoneStreak(days, out var milestone);

        Assert.False(isMilestone);
        Assert.Null(milestone);
    }

    [Fact]
    public void StreakMilestoneHelper_GetNextUpcomingMilestone_ReturnsCorrectNextTarget()
    {
        var nextFrom10 = StreakMilestoneHelper.GetNextUpcomingMilestone(10);
        Assert.NotNull(nextFrom10);
        Assert.Equal(30, nextFrom10.Days);

        var nextFrom30 = StreakMilestoneHelper.GetNextUpcomingMilestone(30);
        Assert.NotNull(nextFrom30);
        Assert.Equal(60, nextFrom30.Days);

        var nextFrom365 = StreakMilestoneHelper.GetNextUpcomingMilestone(365);
        Assert.NotNull(nextFrom365);
        Assert.Equal(455, nextFrom365.Days);
    }

    [Fact]
    public void StreakService_MilestoneCelebrationTracking_PreventsDuplicatePopups()
    {
        var summary = new StreakSummary
        {
            CurrentStreak = 30,
            LongestStreak = 30,
            IsEnabled = true
        };

        // 1. Initial check: milestone should be uncelebrated
        var milestone = _streakService.CheckForUncelebratedMilestone(summary);
        Assert.NotNull(milestone);
        Assert.Equal(30, milestone.Days);

        // 2. Mark celebrated
        _streakService.MarkMilestoneCelebrated(30);

        // 3. Subsequent check: should return null
        var alreadyCelebrated = _streakService.CheckForUncelebratedMilestone(summary);
        Assert.Null(alreadyCelebrated);

        // 4. Persistence check: reload profile and confirm celebrated list preserved
        _streakService.Reload();
        Assert.True(_streakService.Profile.IsMilestoneCelebrated(30));
        Assert.Null(_streakService.CheckForUncelebratedMilestone(summary));
    }

    [Fact]
    public void StreakService_DisabledSystem_DoesNotTriggerMilestoneCelebrations()
    {
        _streakService.IsEnabled = false;

        var summary = new StreakSummary
        {
            CurrentStreak = 30,
            IsEnabled = false
        };

        var milestone = _streakService.CheckForUncelebratedMilestone(summary);
        Assert.Null(milestone);
    }

    [Fact]
    public void SoundEffectManager_PlayStreakMilestoneAchievedSound_DoesNotThrow()
    {
        var settings = new WorkRestSettings { SoundAlertEnabled = false };
        SoundEffectManager.PlayStreakMilestoneAchievedSound(settings);
    }

    [Fact]
    public void StreakMilestoneAchievedDialog_InitializesPropertiesAndAutoDismiss()
    {
        RunInSta(() =>
        {
            var milestone = StreakMilestoneHelper.GetMilestone(30)!;
            var summary = new StreakSummary
            {
                CurrentStreak = 30,
                LongestStreak = 30,
                TotalActiveDays = 30
            };

            var dialog = new StreakMilestoneAchievedDialog(milestone, summary)
            {
                AutoDismissSeconds = 25
            };

            Assert.Equal(milestone, dialog.Milestone);
            Assert.False(dialog.RequestOpenStreakDialog);

            dialog.TickAutoDismiss();
            Assert.Equal(24, dialog.RemainingSeconds);

            dialog.TriggerAutoDismiss();
            Assert.Equal(0, dialog.RemainingSeconds);
        });
    }

    [Fact]
    public void WelcomeBackDialog_WithCompletedStreakToday_AppliesFieryThemeStyling()
    {
        RunInSta(() =>
        {
            var summary = new StreakSummary
            {
                CurrentStreak = 14,
                LongestStreak = 20,
                TodayTier = StreakTier.Gold,
                TodayStatus = StreakDayStatus.Active,
                IsEnabled = true
            };

            var dialog = new WelcomeBackDialog("Vitor", new List<PlannedWorkItem>(), null, summary);

            Assert.Equal(System.Windows.Visibility.Visible, dialog.StreakPanel.Visibility);
            Assert.Contains("14 Days", dialog.StreakCountTextBlock.Text);
            Assert.Equal("🥇 Gold Completed", dialog.StreakStatusBadgeTextBlock.Text);
            Assert.Contains("Gold Tier", dialog.StreakDetailsTextBlock.Text);
        });
    }

    [Fact]
    public void WelcomeBackDialog_WithPendingStreak_AppliesPendingCardStyling()
    {
        RunInSta(() =>
        {
            var summary = new StreakSummary
            {
                CurrentStreak = 7,
                LongestStreak = 10,
                TodayTier = StreakTier.None,
                TodayStatus = StreakDayStatus.Missed,
                IsEnabled = true
            };

            var dialog = new WelcomeBackDialog("Vitor", new List<PlannedWorkItem>(), null, summary);

            Assert.Equal(System.Windows.Visibility.Visible, dialog.StreakPanel.Visibility);
            Assert.Contains("7 Days", dialog.StreakCountTextBlock.Text);
            Assert.Equal("⏳ Pending Today", dialog.StreakStatusBadgeTextBlock.Text);
        });
    }

    [Fact]
    public void WelcomeBackDialog_WithDisabledStreak_HidesStreakPanel()
    {
        RunInSta(() =>
        {
            var summary = new StreakSummary
            {
                IsEnabled = false
            };

            var dialog = new WelcomeBackDialog("Vitor", new List<PlannedWorkItem>(), null, summary);

            Assert.Equal(System.Windows.Visibility.Collapsed, dialog.StreakPanel.Visibility);
        });
    }

    [Fact]
    public void StreakColorHelper_IsStreakOnFire_CorrectlyEvaluatesActiveTiers()
    {
        // 1. Disabled or null
        Assert.False(StreakColorHelper.IsStreakOnFire(null));
        Assert.False(StreakColorHelper.IsStreakOnFire(new StreakSummary { IsEnabled = false, TodayTier = StreakTier.Gold }));

        // 2. Pending / No work recorded today
        Assert.False(StreakColorHelper.IsStreakOnFire(new StreakSummary { IsEnabled = true, TodayTier = StreakTier.None, TodayStatus = StreakDayStatus.Missed }));
        Assert.False(StreakColorHelper.IsStreakOnFire(new StreakSummary { IsEnabled = true, TodayTier = StreakTier.None, TodayStatus = StreakDayStatus.Holiday }));
        Assert.False(StreakColorHelper.IsStreakOnFire(new StreakSummary { IsEnabled = true, TodayTier = StreakTier.None, TodayStatus = StreakDayStatus.SickDay }));
        Assert.False(StreakColorHelper.IsStreakOnFire(new StreakSummary { IsEnabled = true, TodayTier = StreakTier.None, TodayStatus = StreakDayStatus.Excluded }));

        // 3. Bronze Streak (Work saved) -> ON FIRE!
        Assert.True(StreakColorHelper.IsStreakOnFire(new StreakSummary { IsEnabled = true, TodayTier = StreakTier.Bronze, TodayStatus = StreakDayStatus.Active }));

        // 4. Silver Streak (Goal completed) -> ON FIRE!
        Assert.True(StreakColorHelper.IsStreakOnFire(new StreakSummary { IsEnabled = true, TodayTier = StreakTier.Silver, TodayStatus = StreakDayStatus.Active }));

        // 5. Gold Streak (All goals completed) -> ON FIRE!
        Assert.True(StreakColorHelper.IsStreakOnFire(new StreakSummary { IsEnabled = true, TodayTier = StreakTier.Gold, TodayStatus = StreakDayStatus.Active }));
    }

    [Fact]
    public void StreakColorHelper_GetStreakButtonBrushes_ReturnsFieryGradientsForTiers()
    {
        RunInSta(() =>
        {
            // Bronze Streak (Even a bronze streak sets it on fire)
            var bronzeSummary = new StreakSummary
            {
                IsEnabled = true,
                CurrentStreak = 5,
                TodayTier = StreakTier.Bronze,
                TodayStatus = StreakDayStatus.Active
            };
            var (bronzeBg, bronzeBorder, bronzeFg, bronzeThickness) = StreakColorHelper.GetStreakButtonBrushes(bronzeSummary);
            Assert.IsType<System.Windows.Media.LinearGradientBrush>(bronzeBg);
            Assert.Equal(1.5, bronzeThickness);
            Assert.Equal(System.Windows.Media.Brushes.White, bronzeFg);

            // Silver Streak
            var silverSummary = new StreakSummary
            {
                IsEnabled = true,
                CurrentStreak = 10,
                TodayTier = StreakTier.Silver,
                TodayStatus = StreakDayStatus.Active
            };
            var (silverBg, silverBorder, silverFg, silverThickness) = StreakColorHelper.GetStreakButtonBrushes(silverSummary);
            Assert.IsType<System.Windows.Media.LinearGradientBrush>(silverBg);
            Assert.Equal(1.5, silverThickness);

            // Gold Streak
            var goldSummary = new StreakSummary
            {
                IsEnabled = true,
                CurrentStreak = 25,
                TodayTier = StreakTier.Gold,
                TodayStatus = StreakDayStatus.Active
            };
            var (goldBg, goldBorder, goldFg, goldThickness) = StreakColorHelper.GetStreakButtonBrushes(goldSummary);
            Assert.IsType<System.Windows.Media.LinearGradientBrush>(goldBg);
            Assert.Equal(2.0, goldThickness);

            // Pending Day (White / neutral unlit fire button)
            var pendingSummary = new StreakSummary
            {
                IsEnabled = true,
                CurrentStreak = 5,
                TodayTier = StreakTier.None,
                TodayStatus = StreakDayStatus.Missed
            };
            var (pendingBg, pendingBorder, pendingFg, pendingThickness) = StreakColorHelper.GetStreakButtonBrushes(pendingSummary);
            Assert.IsType<System.Windows.Media.SolidColorBrush>(pendingBg);
            Assert.Equal(1.0, pendingThickness);

            // Disabled Streak
            var disabledSummary = new StreakSummary { IsEnabled = false };
            var (disabledBg, disabledBorder, disabledFg, disabledThickness) = StreakColorHelper.GetStreakButtonBrushes(disabledSummary);
            Assert.IsType<System.Windows.Media.SolidColorBrush>(disabledBg);
            Assert.Equal(1.0, disabledThickness);
        });
    }

    [Fact]
    public void StreakColorHelper_ApplyStreakButtonStyle_SetsFieryAppearanceOnButton()
    {
        RunInSta(() =>
        {
            var button = new System.Windows.Controls.Button();

            // 1. Initial pending state (white unlit icon)
            var pendingSummary = new StreakSummary
            {
                IsEnabled = true,
                CurrentStreak = 3,
                TodayTier = StreakTier.None,
                TodayStatus = StreakDayStatus.Missed
            };
            StreakColorHelper.ApplyStreakButtonStyle(button, pendingSummary);
            Assert.Equal("🔥 3", button.Content);
            Assert.Equal(System.Windows.FontWeights.SemiBold, button.FontWeight);
            Assert.Contains("Pending Today", button.ToolTip?.ToString() ?? "");

            // 2. User gets Bronze streak -> Button is set ON FIRE!
            var bronzeSummary = new StreakSummary
            {
                IsEnabled = true,
                CurrentStreak = 4,
                LongestStreak = 10,
                TodayTier = StreakTier.Bronze,
                TodayStatus = StreakDayStatus.Active
            };
            StreakColorHelper.ApplyStreakButtonStyle(button, bronzeSummary);

            Assert.Equal("🔥 4 🥉", button.Content);
            Assert.Equal(System.Windows.FontWeights.Bold, button.FontWeight);
            Assert.IsType<System.Windows.Media.LinearGradientBrush>(button.Background);
            Assert.Equal(System.Windows.Media.Brushes.White, button.Foreground);
            Assert.Contains("ON FIRE", button.ToolTip?.ToString() ?? "");
            Assert.Contains("Bronze Tier", button.ToolTip?.ToString() ?? "");

            // 3. User achieves Gold tier
            var goldSummary = new StreakSummary
            {
                IsEnabled = true,
                CurrentStreak = 4,
                LongestStreak = 10,
                TodayTier = StreakTier.Gold,
                TodayStatus = StreakDayStatus.Active
            };
            StreakColorHelper.ApplyStreakButtonStyle(button, goldSummary);

            Assert.Equal("🔥 4 🥇", button.Content);
            Assert.Equal(System.Windows.FontWeights.Bold, button.FontWeight);
            Assert.IsType<System.Windows.Media.LinearGradientBrush>(button.Background);
            Assert.Contains("Gold Tier Legend", button.ToolTip?.ToString() ?? "");
        });
    }

    [Fact]
    public void StreakColorHelper_GetStreakButtonTooltip_FormatsAllStatesProperly()
    {
        // Disabled
        var disabled = new StreakSummary { IsEnabled = false };
        Assert.Contains("disabled in Settings", StreakColorHelper.GetStreakButtonTooltip(disabled));

        // Protected Holiday
        var holiday = new StreakSummary
        {
            IsEnabled = true,
            CurrentStreak = 12,
            TodayTier = StreakTier.None,
            TodayStatus = StreakDayStatus.Holiday
        };
        var holidayTip = StreakColorHelper.GetStreakButtonTooltip(holiday);
        Assert.Contains("Safe & Protected", holidayTip);
        Assert.Contains("Scheduled Holiday", holidayTip);

        // Protected Sick Day
        var sick = new StreakSummary
        {
            IsEnabled = true,
            CurrentStreak = 8,
            TodayTier = StreakTier.None,
            TodayStatus = StreakDayStatus.SickDay
        };
        var sickTip = StreakColorHelper.GetStreakButtonTooltip(sick);
        Assert.Contains("Protected Sick Day", sickTip);

        // Pending 0 streak
        var zero = new StreakSummary
        {
            IsEnabled = true,
            CurrentStreak = 0,
            TodayTier = StreakTier.None,
            TodayStatus = StreakDayStatus.Missed
        };
        Assert.Contains("0 Days", StreakColorHelper.GetStreakButtonTooltip(zero));
    }

    [Fact]
    public void StreakTierAchievedDialog_ContainsScrollViewer_AndWrapsTextWithoutClipping()
    {
        RunInSta(() =>
        {
            var summary = new StreakSummary
            {
                CurrentStreak = 5,
                LongestStreak = 10,
                TodayTier = StreakTier.Gold,
                TodayStatus = StreakDayStatus.Active
            };

            var dialog = new StreakTierAchievedDialog(StreakTier.Gold, summary);

            // Find ScrollViewer in the dialog
            var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(dialog);
            Assert.NotNull(scrollViewer);
            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Auto, scrollViewer.VerticalScrollBarVisibility);
            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Disabled, scrollViewer.HorizontalScrollBarVisibility);

            // Verify TextWrapping is enabled
            Assert.Equal(System.Windows.TextWrapping.Wrap, dialog.RewardCriteriaTextBlock.TextWrapping);
            Assert.Equal(System.Windows.TextWrapping.Wrap, dialog.NextTierHintTextBlock.TextWrapping);
            Assert.Equal(System.Windows.TextWrapping.Wrap, dialog.StreakHeadlineTextBlock.TextWrapping);
        });
    }

    [Fact]
    public void StreakMilestoneAchievedDialog_ContainsScrollViewer_AndWrapsTextWithoutClipping()
    {
        RunInSta(() =>
        {
            var milestone = StreakMilestoneHelper.GetMilestone(30)!;
            var summary = new StreakSummary
            {
                CurrentStreak = 30,
                LongestStreak = 30,
                TotalActiveDays = 30
            };

            var dialog = new StreakMilestoneAchievedDialog(milestone, summary);

            // Find ScrollViewer in the dialog
            var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(dialog);
            Assert.NotNull(scrollViewer);
            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Auto, scrollViewer.VerticalScrollBarVisibility);
            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Disabled, scrollViewer.HorizontalScrollBarVisibility);

            // Verify TextWrapping is enabled on milestone details
            Assert.Equal(System.Windows.TextWrapping.Wrap, dialog.MilestoneHighlightTextBlock.TextWrapping);
            Assert.Equal(System.Windows.TextWrapping.Wrap, dialog.NextMilestoneHintTextBlock.TextWrapping);
            Assert.Equal(System.Windows.TextWrapping.Wrap, dialog.StreakHeadlineTextBlock.TextWrapping);
        });
    }

    [Fact]
    public void CalculateStreak_Past14DaysTimeline_Populates14DaysHistory()
    {
        var today = new DateTime(2026, 11, 2);
        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = today.AddHours(9), EndTime = today.AddHours(11) }
        };

        var summary = _streakService.CalculateStreak(today, entries);

        Assert.NotNull(summary.RecentDays);
        Assert.Equal(14, summary.RecentDays.Count);
        Assert.Equal(today.AddDays(-13).Date, summary.RecentDays.First().Date);
        Assert.Equal(today.Date, summary.RecentDays.Last().Date);
    }

    [Fact]
    public void StreakDayEvaluation_ColorCoding_MatchesTiersAndStatuses()
    {
        // 1. Gold Tier
        var goldEval = new StreakDayEvaluation { Tier = StreakTier.Gold, Status = StreakDayStatus.Active };
        Assert.Equal("#F59E0B", goldEval.BorderColorHex);
        Assert.Equal("#2A1B02", goldEval.BgColorHex);
        Assert.Equal("GOLD", goldEval.TierBadgeText);
        Assert.NotNull(goldEval.CardBackground);
        Assert.NotNull(goldEval.CardBorderBrush);
        Assert.NotNull(goldEval.BadgeBackground);
        Assert.Equal(1.5, goldEval.CardBorderThickness.Top);

        // 2. Silver Tier
        var silverEval = new StreakDayEvaluation { Tier = StreakTier.Silver, Status = StreakDayStatus.Active };
        Assert.Equal("#38BDF8", silverEval.BorderColorHex);
        Assert.Equal("#0F172A", silverEval.BgColorHex);
        Assert.Equal("SILVER", silverEval.TierBadgeText);
        Assert.NotNull(silverEval.CardBackground);
        Assert.NotNull(silverEval.CardBorderBrush);
        Assert.NotNull(silverEval.BadgeBackground);

        // 3. Bronze Tier
        var bronzeEval = new StreakDayEvaluation { Tier = StreakTier.Bronze, Status = StreakDayStatus.Active };
        Assert.Equal("#EA580C", bronzeEval.BorderColorHex);
        Assert.Equal("#241206", bronzeEval.BgColorHex);
        Assert.Equal("BRONZE", bronzeEval.TierBadgeText);
        Assert.NotNull(bronzeEval.CardBackground);
        Assert.NotNull(bronzeEval.CardBorderBrush);
        Assert.NotNull(bronzeEval.BadgeBackground);

        // 4. Sick Day
        var sickEval = new StreakDayEvaluation { Tier = StreakTier.None, Status = StreakDayStatus.SickDay };
        Assert.Equal("#F43F5E", sickEval.BorderColorHex);
        Assert.Equal("#291018", sickEval.BgColorHex);
        Assert.Equal("SICK DAY", sickEval.TierBadgeText);
        Assert.NotNull(sickEval.CardBackground);
        Assert.NotNull(sickEval.CardBorderBrush);

        // 5. Holiday
        var holidayEval = new StreakDayEvaluation { Tier = StreakTier.None, Status = StreakDayStatus.Holiday };
        Assert.Equal("#10B981", holidayEval.BorderColorHex);
        Assert.Equal("#0B2019", holidayEval.BgColorHex);
        Assert.Equal("HOLIDAY", holidayEval.TierBadgeText);
        Assert.NotNull(holidayEval.CardBackground);
        Assert.NotNull(holidayEval.CardBorderBrush);

        // 6. Rest Day (Excluded)
        var excludedEval = new StreakDayEvaluation { Tier = StreakTier.None, Status = StreakDayStatus.Excluded };
        Assert.Equal("#3B82F6", excludedEval.BorderColorHex);
        Assert.Equal("#111827", excludedEval.BgColorHex);
        Assert.Equal("REST DAY", excludedEval.TierBadgeText);
        Assert.NotNull(excludedEval.CardBackground);
        Assert.NotNull(excludedEval.CardBorderBrush);

        // 7. Missed Day
        var missedEval = new StreakDayEvaluation { Tier = StreakTier.None, Status = StreakDayStatus.Missed };
        Assert.Equal("#EF4444", missedEval.BorderColorHex);
        Assert.Equal("#231215", missedEval.BgColorHex);
        Assert.Equal("MISSED", missedEval.TierBadgeText);
        Assert.NotNull(missedEval.CardBackground);
        Assert.NotNull(missedEval.CardBorderBrush);

        // 8. Pending / Future
        var pendingEval = new StreakDayEvaluation { Tier = StreakTier.None, Status = StreakDayStatus.Future };
        Assert.Equal("#52525B", pendingEval.BorderColorHex);
        Assert.Equal("#18181B", pendingEval.BgColorHex);
        Assert.Equal("PENDING", pendingEval.TierBadgeText);
        Assert.NotNull(pendingEval.CardBackground);
        Assert.NotNull(pendingEval.CardBorderBrush);
    }

    [Fact]
    public void GetMonthStreakSummary_EvaluatesAllDaysInMonth()
    {
        var november = new DateTime(2026, 11, 1);
        _streakService.Profile.AutoApplySickDays = false;
        _streakService.SetExcludedDays(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });

        // Add 3 days of work in November: Nov 2 (Bronze), Nov 3 (Silver), Nov 4 (Gold)
        // Set goal for Nov 2 (1 planned project of 3h, only worked 1h -> Bronze)
        _planStorage.AddOrUpdatePlan(new PlannedWorkItem
        {
            Id = "plan-nov2",
            Date = new DateTime(2026, 11, 2),
            ProjectName = "Project Gamma",
            PlannedHours = 3.0
        });

        // Set goal for Nov 3 (2 planned projects, 1 completed -> Silver)
        _planStorage.AddOrUpdatePlan(new PlannedWorkItem
        {
            Id = "plan-nov3-1",
            Date = new DateTime(2026, 11, 3),
            ProjectName = "Project Alpha",
            PlannedHours = 2.0
        });
        _planStorage.AddOrUpdatePlan(new PlannedWorkItem
        {
            Id = "plan-nov3-2",
            Date = new DateTime(2026, 11, 3),
            ProjectName = "Project Beta",
            PlannedHours = 3.0
        });

        // Set goal for Nov 4 (1 planned project, completed -> Gold)
        _planStorage.AddOrUpdatePlan(new PlannedWorkItem
        {
            Id = "plan-nov4",
            Date = new DateTime(2026, 11, 4),
            ProjectName = "Project Alpha",
            PlannedHours = 2.0
        });

        var entries = new List<TimeEntry>
        {
            // Nov 2: 1 hour on Alpha (Bronze - goal was Gamma 3h, not completed)
            new() { ProjectName = "Project Alpha", StartTime = new DateTime(2026, 11, 2, 9, 0, 0), EndTime = new DateTime(2026, 11, 2, 10, 0, 0) },
            // Nov 3: 2.5 hours on Project Alpha (completed 1/2 goals -> Silver)
            new() { ProjectName = "Project Alpha", StartTime = new DateTime(2026, 11, 3, 9, 0, 0), EndTime = new DateTime(2026, 11, 3, 11, 30, 0) },
            // Nov 4: 2.0 hours on Project Alpha (completed 1/1 goals -> Gold)
            new() { ProjectName = "Project Alpha", StartTime = new DateTime(2026, 11, 4, 9, 0, 0), EndTime = new DateTime(2026, 11, 4, 11, 0, 0) }
        };

        // Add a holiday on Nov 10
        _streakService.AddHoliday(new DateTime(2026, 11, 10), "Nov Vacation");
        // Add a sick day on Nov 12
        _streakService.UseSickDay(new DateTime(2026, 11, 12));

        var monthSummary = _streakService.GetMonthStreakSummary(november, entries, new DateTime(2026, 12, 1));

        Assert.Equal(2026, monthSummary.Year);
        Assert.Equal(11, monthSummary.Month);
        Assert.Equal("November 2026", monthSummary.MonthName);
        Assert.Equal(30, monthSummary.TotalDays);
        Assert.Equal(30, monthSummary.Days.Count);

        // Check active days and tier counts
        Assert.Equal(3, monthSummary.ActiveDaysCount);
        Assert.Equal(1, monthSummary.BronzeDaysCount);
        Assert.Equal(1, monthSummary.SilverDaysCount);
        Assert.Equal(1, monthSummary.GoldDaysCount);

        // Check protected days
        Assert.Equal(1, monthSummary.HolidayDaysCount);
        Assert.Equal(1, monthSummary.SickDaysCount);
        Assert.Equal(9, monthSummary.ExcludedDaysCount); // 9 weekend days in November 2026

        // Check summary headline
        Assert.Contains("3 Active Days", monthSummary.SummaryText);
        Assert.Contains("1 🥇", monthSummary.SummaryText);
        Assert.Contains("1 🥈", monthSummary.SummaryText);
        Assert.Contains("1 🥉", monthSummary.SummaryText);
    }

    [Fact]
    public void StreakDialog_MonthCycling_SwitchesBetween14DaysAndMonthViews()
    {
        RunInSta(() =>
        {
            var today = new DateTime(2026, 11, 2);
            var entries = new List<TimeEntry>
            {
                new() { ProjectName = "Project Alpha", StartTime = today.AddHours(9), EndTime = today.AddHours(12) }
            };

            var dialog = new StreakDialog(entries, _streakService);

            // 1. Initial state: 14-day timeline view
            Assert.False(dialog.ShowingMonthView);
            Assert.Equal("Last 14 Days", dialog.ActiveViewBadgeText.Text);
            var initialItems = dialog.RecentDaysItemsControl.ItemsSource as System.Collections.IEnumerable;
            Assert.NotNull(initialItems);
            int count14 = initialItems.Cast<object>().Count();
            Assert.Equal(14, count14);

            // 2. Cycle to month view
            dialog.ShowMonthView(today);
            Assert.True(dialog.ShowingMonthView);
            Assert.Equal(11, dialog.SelectedMonth.Month);
            Assert.Equal(2026, dialog.SelectedMonth.Year);
            Assert.Contains("November 2026", dialog.TimelineTitleTextBlock.Text);

            var monthItems = dialog.RecentDaysItemsControl.ItemsSource as System.Collections.IEnumerable;
            Assert.NotNull(monthItems);
            int countMonth = monthItems.Cast<object>().Count();
            Assert.Equal(30, countMonth); // 30 days in November

            // 3. Cycle to previous month (October 2026)
            dialog.CycleToPreviousMonth();
            Assert.Equal(10, dialog.SelectedMonth.Month);
            Assert.Equal(2026, dialog.SelectedMonth.Year);
            Assert.Contains("October 2026", dialog.TimelineTitleTextBlock.Text);
            var octItems = dialog.RecentDaysItemsControl.ItemsSource as System.Collections.IEnumerable;
            Assert.NotNull(octItems);
            Assert.Equal(31, octItems.Cast<object>().Count()); // 31 days in October

            // 4. Cycle to next month (back to November 2026)
            dialog.CycleToNextMonth();
            Assert.Equal(11, dialog.SelectedMonth.Month);

            // 5. Return to 14 days view
            dialog.Show14DaysView();
            Assert.False(dialog.ShowingMonthView);
            Assert.Equal("Last 14 Days", dialog.ActiveViewBadgeText.Text);
            var returned14Items = dialog.RecentDaysItemsControl.ItemsSource as System.Collections.IEnumerable;
            Assert.NotNull(returned14Items);
            Assert.Equal(14, returned14Items.Cast<object>().Count());
        });
    }

    [Fact]
    public void CalculateStreak_MissedStreakDay_IdentifiesBreakingDayAndCalculatesPotentialRestoration()
    {
        // Work on previous week: Mon Oct 26 to Fri Oct 30 (5 days)
        // Sat Oct 31 & Sun Nov 1: Excluded rest days
        // Mon Nov 2 (yesterday): Missed (no work)
        // Tue Nov 3 (today): Worked 2 hours (Bronze tier)
        var monLastWeek = new DateTime(2026, 10, 26);
        var fridayLastWeek = new DateTime(2026, 10, 30);
        var missedMonday = new DateTime(2026, 11, 2);
        var today = new DateTime(2026, 11, 3);

        _streakService.Profile.AutoApplySickDays = false;
        _streakService.SetExcludedDays(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });

        var entries = new List<TimeEntry>();
        for (var d = monLastWeek; d <= fridayLastWeek; d = d.AddDays(1))
        {
            entries.Add(new TimeEntry
            {
                ProjectName = "Project Alpha",
                StartTime = d.AddHours(9),
                EndTime = d.AddHours(12)
            });
        }

        // Today's work
        entries.Add(new TimeEntry
        {
            ProjectName = "Project Alpha",
            StartTime = today.AddHours(9),
            EndTime = today.AddHours(11)
        });

        var summary = _streakService.CalculateStreak(today, entries);

        // Without sick day: current streak is 1 (today only), because missedMonday broke the chain
        Assert.Equal(1, summary.CurrentStreak);
        Assert.Equal(missedMonday.Date, summary.MissedStreakDay);
        // Potential restored streak: 1 (today) + 5 (previous week) = 6 days!
        Assert.Equal(6, summary.PotentialRestoredStreak);

        // Now apply a sick day to the missed Monday
        bool applied = _streakService.UseSickDay(missedMonday);
        Assert.True(applied);

        // Recalculate streak
        var restoredSummary = _streakService.CalculateStreak(today, entries);
        Assert.Equal(6, restoredSummary.CurrentStreak);
        Assert.Null(restoredSummary.MissedStreakDay); // No longer broken
    }

    [Fact]
    public void CalculateStreak_RecentMissedDays_OnlyIncludesMissedDaysInLast30Days()
    {
        var today = new DateTime(2026, 11, 2); // Monday
        _streakService.Profile.AutoApplySickDays = false;
        _streakService.SetExcludedDays(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });

        // Add a holiday for Friday Oct 30
        _streakService.AddHoliday(new DateTime(2026, 10, 30), "Day Off");

        // Add work for Thursday Oct 29
        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "P1", StartTime = new DateTime(2026, 10, 29, 9, 0, 0), EndTime = new DateTime(2026, 10, 29, 12, 0, 0) }
        };

        var summary = _streakService.CalculateStreak(today, entries);

        // RecentMissedDays should not contain:
        // - Worked day (Oct 29)
        // - Holiday (Oct 30)
        // - Excluded weekend (Oct 31, Nov 1)
        // - Future dates
        Assert.DoesNotContain(summary.RecentMissedDays, d => d.Date == new DateTime(2026, 10, 29));
        Assert.DoesNotContain(summary.RecentMissedDays, d => d.Date == new DateTime(2026, 10, 30));
        Assert.DoesNotContain(summary.RecentMissedDays, d => d.Date == new DateTime(2026, 10, 31));
        Assert.DoesNotContain(summary.RecentMissedDays, d => d.Date == new DateTime(2026, 11, 1));

        // Wed Oct 28 has no work and is a regular weekday, so it is in RecentMissedDays
        Assert.Contains(summary.RecentMissedDays, d => d.Date == new DateTime(2026, 10, 28));
    }

    [Fact]
    public void StreakDialog_ShowsMissedDayRestorationCardAndRestoresStreak()
    {
        RunInSta(() =>
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var twoDaysAgo = today.AddDays(-2);
            var threeDaysAgo = today.AddDays(-3);

            _streakService.Profile.AutoApplySickDays = false;
            _streakService.SetExcludedDays(Array.Empty<DayOfWeek>()); // No excluded days for clear test

            // Work 3 days ago and 2 days ago
            var entries = new List<TimeEntry>
            {
                new()
                {
                    ProjectName = "Project Alpha",
                    StartTime = threeDaysAgo.AddHours(9),
                    EndTime = threeDaysAgo.AddHours(12)
                },
                new()
                {
                    ProjectName = "Project Alpha",
                    StartTime = twoDaysAgo.AddHours(9),
                    EndTime = twoDaysAgo.AddHours(12)
                }
            };

            var dialog = new StreakDialog(entries, _streakService);

            // Yesterday was missed and broke the streak -> Restoration card should be visible
            Assert.Equal(System.Windows.Visibility.Visible, dialog.MissedDayRestorationCard.Visibility);
            Assert.NotNull(dialog.RestoreStreakSickDayButton.Tag);
            Assert.Equal(yesterday.Date, (DateTime)dialog.RestoreStreakSickDayButton.Tag);

            // Applying a sick day to yesterday restores the streak
            _streakService.UseSickDay(yesterday);
            var summaryAfter = _streakService.CalculateStreak(today, entries);
            Assert.Equal(2, summaryAfter.CurrentStreak);
        });
    }

    [Fact]
    public void StreakDialog_MonthCycleControls_PlacedOnDedicatedLineBelowColorCodedText()
    {
        RunInSta(() =>
        {
            var today = new DateTime(2026, 11, 2);
            var entries = new List<TimeEntry>
            {
                new() { ProjectName = "Project Alpha", StartTime = today.AddHours(9), EndTime = today.AddHours(12) }
            };

            var dialog = new StreakDialog(entries, _streakService);

            Assert.NotNull(dialog.TimelineSummaryStatusText);
            Assert.NotNull(dialog.CycleMonthButton);
            Assert.NotNull(dialog.CyclePrevMonthButton);
            Assert.NotNull(dialog.CycleNextMonthButton);

            var textParent = dialog.TimelineSummaryStatusText.Parent;
            var buttonParent = dialog.CycleMonthButton.Parent;
            Assert.NotNull(textParent);
            Assert.NotNull(buttonParent);
            Assert.NotSame(textParent, buttonParent);
            Assert.IsType<System.Windows.Controls.StackPanel>(textParent);
            Assert.IsType<System.Windows.Controls.StackPanel>(buttonParent);
        });
    }

    [Fact]
    public void StreakDialog_CycleMonthButton_HasStylingAndTransitionsBetween14DayAndMonthViews()
    {
        RunInSta(() =>
        {
            var today = new DateTime(2026, 11, 2);
            var entries = new List<TimeEntry>
            {
                new() { ProjectName = "Project Alpha", StartTime = today.AddHours(9), EndTime = today.AddHours(12) }
            };

            var dialog = new StreakDialog(entries, _streakService);

            // In 14-days view:
            dialog.Show14DaysView();
            Assert.NotNull(dialog.CycleMonthButton.Style);
            Assert.Contains("Cycle Month", dialog.CycleMonthButton.Content?.ToString() ?? "");

            // In Month view:
            dialog.ShowMonthView();
            Assert.NotNull(dialog.CycleMonthButton.Style);
            Assert.Contains("Viewing", dialog.CycleMonthButton.Content?.ToString() ?? "");
        });
    }

    [Fact]
    public void GoldStars_CalculatesOneStarPerHourWorked()
    {
        var profile = new StreakProfile();

        // 0 hours -> 0 stars
        Assert.Equal(0, profile.CalculateTotalGoldStarsEarned(0));
        Assert.Equal(0, profile.GetAvailableGoldStars(0));

        // 29.5 hours -> 29 stars
        Assert.Equal(29, profile.CalculateTotalGoldStarsEarned(29.5));
        Assert.Equal(29, profile.GetAvailableGoldStars(29.5));

        // 30.0 hours -> 30 stars
        Assert.Equal(30, profile.CalculateTotalGoldStarsEarned(30.0));
        Assert.Equal(30, profile.GetAvailableGoldStars(30.0));

        // 95.8 hours -> 95 stars
        Assert.Equal(95, profile.CalculateTotalGoldStarsEarned(95.8));
        Assert.Equal(95, profile.GetAvailableGoldStars(95.8));
    }

    [Fact]
    public void BuyRestDay_CannotBuyWhenRestDaysAreMaxedAtThree()
    {
        var profile = new StreakProfile
        {
            MonthlySickDaysAllowance = 3,
            UsedSickDates = new List<DateTime>() // 0 used -> 3 available (max)
        };

        double totalHours = 100; // 100 gold stars available
        Assert.Equal(100, profile.GetAvailableGoldStars(totalHours));
        Assert.Equal(3, profile.GetRemainingSickDaysForMonth(2026, 11));

        bool canBuy = profile.CanBuyRestDay(totalHours, 2026, 11, out var reason);
        Assert.False(canBuy);
        Assert.Contains("maxed out", reason, StringComparison.OrdinalIgnoreCase);

        bool bought = profile.BuyRestDay(totalHours, new DateTime(2026, 11, 2));
        Assert.False(bought);
        Assert.Equal(0, profile.GoldStarsSpent);
        Assert.Equal(100, profile.GetAvailableGoldStars(totalHours));
    }

    [Fact]
    public void BuyRestDay_CannotBuyWhenInsufficientGoldStars()
    {
        var profile = new StreakProfile
        {
            MonthlySickDaysAllowance = 3,
            UsedSickDates = new List<DateTime> { new DateTime(2026, 11, 1) } // 1 used -> 2 remaining
        };

        double totalHours = 29.5; // only 29 gold stars (< 30 cost)
        Assert.Equal(29, profile.GetAvailableGoldStars(totalHours));
        Assert.Equal(2, profile.GetRemainingSickDaysForMonth(2026, 11));

        bool canBuy = profile.CanBuyRestDay(totalHours, 2026, 11, out var reason);
        Assert.False(canBuy);
        Assert.Contains("Insufficient Gold Stars", reason, StringComparison.OrdinalIgnoreCase);

        bool bought = profile.BuyRestDay(totalHours, new DateTime(2026, 11, 2));
        Assert.False(bought);
        Assert.Equal(0, profile.GoldStarsSpent);
    }

    [Fact]
    public void BuyRestDay_SuccessfullyDeductsThirtyStarsAndIncreasesRestDaysUpToThree()
    {
        var profile = new StreakProfile
        {
            MonthlySickDaysAllowance = 3,
            UsedSickDates = new List<DateTime>
            {
                new DateTime(2026, 11, 1),
                new DateTime(2026, 11, 2)
            } // 2 used -> 1 remaining
        };

        double totalHours = 65.0; // 65 stars earned
        Assert.Equal(65, profile.GetAvailableGoldStars(totalHours));
        Assert.Equal(1, profile.GetRemainingSickDaysForMonth(2026, 11));

        // 1st purchase (1 -> 2)
        bool canBuy1 = profile.CanBuyRestDay(totalHours, 2026, 11, out _);
        Assert.True(canBuy1);

        bool bought1 = profile.BuyRestDay(totalHours, new DateTime(2026, 11, 3));
        Assert.True(bought1);
        Assert.Equal(30, profile.GoldStarsSpent);
        Assert.Equal(35, profile.GetAvailableGoldStars(totalHours)); // 65 - 30 = 35
        Assert.Equal(2, profile.GetRemainingSickDaysForMonth(2026, 11)); // 1 + 1 = 2

        // 2nd purchase (2 -> 3)
        bool canBuy2 = profile.CanBuyRestDay(totalHours, 2026, 11, out _);
        Assert.True(canBuy2);

        bool bought2 = profile.BuyRestDay(totalHours, new DateTime(2026, 11, 3));
        Assert.True(bought2);
        Assert.Equal(60, profile.GoldStarsSpent);
        Assert.Equal(5, profile.GetAvailableGoldStars(totalHours)); // 65 - 60 = 5
        Assert.Equal(3, profile.GetRemainingSickDaysForMonth(2026, 11)); // 2 + 1 = 3 (Max!)

        // 3rd purchase attempt (already at 3 max and balance is 5 < 30) -> blocked
        bool canBuy3 = profile.CanBuyRestDay(totalHours, 2026, 11, out var reason3);
        Assert.False(canBuy3);
        Assert.Contains("maxed out", reason3, StringComparison.OrdinalIgnoreCase);

        bool bought3 = profile.BuyRestDay(totalHours, new DateTime(2026, 11, 3));
        Assert.False(bought3);
    }

    [Fact]
    public void StreakService_CalculateStreak_PopulatesGoldStarsAndRestDayShopSummary()
    {
        var today = new DateTime(2026, 11, 2);
        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "Proj", StartTime = today.AddHours(9), EndTime = today.AddHours(17) }, // 8h
            new() { ProjectName = "Proj", StartTime = today.AddDays(-1).AddHours(9), EndTime = today.AddDays(-1).AddHours(17) }, // 8h
            new() { ProjectName = "Proj", StartTime = today.AddDays(-2).AddHours(9), EndTime = today.AddDays(-2).AddHours(23) }  // 14h -> Total 30h
        };

        var summary = _streakService.CalculateStreak(today, entries);

        Assert.Equal(30.0, summary.TotalHoursWorked);
        Assert.Equal(30, summary.TotalGoldStarsEarned);
        Assert.Equal(0, summary.GoldStarsSpent);
        Assert.Equal(30, summary.GoldStarsBalance);
        Assert.Equal(3, summary.AvailableRestDays);
        Assert.Equal(3, summary.MaxRestDays);
        Assert.False(summary.CanBuyRestDay); // already maxed at 3
        Assert.Contains("maxed out", summary.BuyRestDayStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StreakDialog_DisplaysGoldStarsAndRestDayShop()
    {
        RunInSta(() =>
        {
            var today = new DateTime(2026, 11, 2);
            var entries = new List<TimeEntry>
            {
                new() { ProjectName = "P", StartTime = today.AddHours(9), EndTime = today.AddHours(19) } // 10h
            };

            var dialog = new StreakDialog(entries, _streakService);

            Assert.NotNull(dialog.GoldStarsBalanceTextBlock);
            Assert.NotNull(dialog.GoldStarsShopBalanceText);
            Assert.NotNull(dialog.BuyRestDayButton);
            Assert.NotNull(dialog.BuyRestDayHintTextBlock);

            Assert.Contains("10", dialog.GoldStarsBalanceTextBlock.Text);
            Assert.Contains("10", dialog.GoldStarsShopBalanceText.Text);
        });
    }

    private static T? FindVisualChild<T>(System.Windows.DependencyObject? parent) where T : System.Windows.DependencyObject
    {
        if (parent == null) return null;

        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        if (count == 0 && parent is System.Windows.FrameworkElement fe)
        {
            // If visual tree not created yet, check LogicalTreeHelper or FrameworkElement children
            foreach (var logicalChild in System.Windows.LogicalTreeHelper.GetChildren(fe))
            {
                if (logicalChild is T match) return match;
                if (logicalChild is System.Windows.DependencyObject depObj)
                {
                    var found = FindVisualChild<T>(depObj);
                    if (found != null) return found;
                }
            }
        }

        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
