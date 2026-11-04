using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class UpdatePersistenceTests : IDisposable
{
    private readonly string _testBaseDir;

    public UpdatePersistenceTests()
    {
        _testBaseDir = Path.Combine(Path.GetTempPath(), $"update_persistence_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testBaseDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testBaseDir))
            {
                Directory.Delete(_testBaseDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public void Holidays_PreservedAndMigrated_FromLegacyDirectory_AndPreservesStreak()
    {
        var legacyDir = Path.Combine(_testBaseDir, "LegacyInstall", "Data");
        var activeDir = Path.Combine(_testBaseDir, "ActiveInstall", "Data");
        Directory.CreateDirectory(legacyDir);
        Directory.CreateDirectory(activeDir);

        // User had scheduled holidays in legacy install
        var legacyProfile = new StreakProfile
        {
            Enabled = true,
            Holidays = new List<HolidayItem>
            {
                new() { Date = new DateTime(2026, 11, 10), Description = "Company Retreat" },
                new() { Date = new DateTime(2026, 11, 11), Description = "Veteran's Day" },
                new() { Date = new DateTime(2026, 12, 25), Description = "Christmas" }
            },
            GoldStarsSpent = 60,
            BonusGoldStars = 10,
            UsedSickDates = new List<DateTime> { new DateTime(2026, 11, 2) },
            ReturnedSickDays = 1
        };

        var legacyFilePath = Path.Combine(legacyDir, "streaks.json");
        File.WriteAllText(legacyFilePath, JsonSerializer.Serialize(legacyProfile));

        // Active directory has a fresh/empty profile (as created on new update install)
        var activeProfile = new StreakProfile();
        var activeFilePath = Path.Combine(activeDir, "streaks.json");

        // Merge from legacy directories
        bool merged = StreakStorage.MergeFromCandidateLegacyStreaks(activeProfile, new[] { legacyDir });

        Assert.True(merged);
        Assert.Equal(3, activeProfile.Holidays.Count);
        Assert.Contains(activeProfile.Holidays, h => h.Date == new DateTime(2026, 11, 10) && h.Description == "Company Retreat");
        Assert.Contains(activeProfile.Holidays, h => h.Date == new DateTime(2026, 11, 11) && h.Description == "Veteran's Day");
        Assert.Contains(activeProfile.Holidays, h => h.Date == new DateTime(2026, 12, 25) && h.Description == "Christmas");
        Assert.Equal(60, activeProfile.GoldStarsSpent);
        Assert.Equal(10, activeProfile.BonusGoldStars);
        Assert.Single(activeProfile.UsedSickDates);
        Assert.Equal(1, activeProfile.ReturnedSickDays);

        // Verify that the streak service evaluates holiday correctly and keeps streak intact
        var tempPlanFile = Path.Combine(_testBaseDir, "plans.json");
        var planStorage = new CalendarPlanStorage(tempPlanFile);
        var activeStorage = new StreakStorage(activeFilePath);
        activeStorage.Save(activeProfile);

        var streakService = new StreakService(activeStorage, planStorage);

        var holidayDate = new DateTime(2026, 11, 10);
        var eval = streakService.EvaluateDate(holidayDate, new List<TimeEntry>(), holidayDate);

        Assert.Equal(StreakDayStatus.Holiday, eval.Status);
        Assert.Equal("Company Retreat", eval.Reason);

        // Test streak preservation across the holiday
        var dayBefore = new DateTime(2026, 11, 9); // Monday
        var dayAfter = new DateTime(2026, 11, 12); // Thursday
        var entries = new List<TimeEntry>
        {
            new() { ProjectName = "Project A", StartTime = dayBefore.AddHours(9), EndTime = dayBefore.AddHours(12) },
            new() { ProjectName = "Project A", StartTime = dayAfter.AddHours(9), EndTime = dayAfter.AddHours(12) }
        };

        var streakSummary = streakService.CalculateStreak(dayAfter, entries);
        // Nov 9 (work), Nov 10 (holiday - preserved), Nov 11 (holiday - preserved), Nov 12 (work) -> streak is preserved and equals 2 active days
        Assert.Equal(2, streakSummary.CurrentStreak);

        // Verify that without holiday preservation, streak would have broken on Nov 10 resulting in streak of 1
        activeProfile.Holidays.Clear();
        activeStorage.Save(activeProfile);
        streakService.Profile.AutoApplySickDays = false;
        var brokenStreakSummary = streakService.CalculateStreak(dayAfter, entries);
        Assert.Equal(1, brokenStreakSummary.CurrentStreak);
    }

    [Fact]
    public void SickDaysAndGoldStarsSpent_PreservedAndMerged_FromLegacyVersions()
    {
        var legacyDir = Path.Combine(_testBaseDir, "LegacyV1");
        Directory.CreateDirectory(legacyDir);

        var legacyStreakJson = """
        {
            "Enabled": true,
            "MonthlySickDaysAllowance": 4,
            "UsedSickDates": ["2026-10-15T00:00:00", "2026-10-20T00:00:00"],
            "RevokedSickDates": ["2026-09-05T00:00:00"],
            "ReturnedSickDays": 2,
            "GoldStarsSpent": 90,
            "BonusGoldStars": 15,
            "RestDayPurchases": [
                {
                    "Id": "purchase_1",
                    "PurchasedAt": "2026-10-01T12:00:00",
                    "CostInGoldStars": 30
                },
                {
                    "Id": "purchase_2",
                    "PurchasedAt": "2026-10-15T14:00:00",
                    "CostInGoldStars": 30
                },
                {
                    "Id": "purchase_3",
                    "PurchasedAt": "2026-10-28T09:30:00",
                    "CostInGoldStars": 30
                }
            ],
            "Holidays": [
                {
                    "Date": "2026-11-26T00:00:00",
                    "Description": "Thanksgiving"
                }
            ]
        }
        """;

        File.WriteAllText(Path.Combine(legacyDir, "streaks.json"), legacyStreakJson);

        var targetProfile = new StreakProfile();
        bool merged = StreakStorage.MergeFromCandidateLegacyStreaks(targetProfile, new[] { legacyDir });

        Assert.True(merged);
        Assert.Equal(4, targetProfile.MonthlySickDaysAllowance);
        Assert.Equal(2, targetProfile.UsedSickDates.Count);
        Assert.Contains(new DateTime(2026, 10, 15), targetProfile.UsedSickDates);
        Assert.Contains(new DateTime(2026, 10, 20), targetProfile.UsedSickDates);
        Assert.Single(targetProfile.RevokedSickDates);
        Assert.Contains(new DateTime(2026, 9, 5), targetProfile.RevokedSickDates);
        Assert.Equal(2, targetProfile.ReturnedSickDays);
        Assert.Equal(90, targetProfile.GoldStarsSpent);
        Assert.Equal(15, targetProfile.BonusGoldStars);
        Assert.Equal(3, targetProfile.RestDayPurchases.Count);
        Assert.Single(targetProfile.Holidays);
        Assert.Equal("Thanksgiving", targetProfile.Holidays[0].Description);
    }

    [Fact]
    public void WorkRestSettings_AllTogglesAndPreferences_PreservedAndMerged()
    {
        var legacyDir = Path.Combine(_testBaseDir, "LegacySettingsDir");
        Directory.CreateDirectory(legacyDir);

        var legacySettings = new WorkRestSettings
        {
            UserName = "Vitor Developer",
            FocusMinutes = 45,
            ShortBreakMinutes = 10,
            LongBreakMinutes = 20,
            CyclesBeforeLongBreak = 3,
            IntervalModeEnabled = true,
            SoundAlertEnabled = false,
            SoundVolume = 40,
            GoalMilestoneSoundVolume = 60,
            SoundOnStartEnabled = false,
            SoundOnEndEnabled = false,
            SoundOnGoalAchievedEnabled = false,
            SoundOnDailyGoalAchievedEnabled = false,
            SoundOnWeeklyGoalAchievedEnabled = false,
            SoundOnBreakStartEnabled = false,
            SoundOnBreakEndEnabled = false,
            SoundOnCheerEnabled = false,
            CompanionVisible = false,
            CompanionExpanded = false,
            MiniWidgetEnabled = false,
            MiniWidgetCorner = "TopLeft",
            MiniWidgetTargetMonitor = "Secondary",
            MiniWidgetMonitorIndex = 1,
            WelcomePopupEnabled = false,
            GoalNotificationsEnabled = false,
            DailyGoalNotificationEnabled = false,
            WeeklyGoalNotificationEnabled = false,
            ProjectGoalNotificationEnabled = false,
            MilestoneRemindersEnabled = false,
            MilestoneDisplayMode = "All",
            CompletionRewardIcon = "Trophy",
            CompletionRewardColor = "#FFD700",
            AutoAdvancePhases = true,
            FocusXp = 1250,
            FocusStreak = 12,
            TotalSessionsCompleted = 45,
            SelectedSceneId = "lumberjack",
            ArtMode = "ascii"
        };

        File.WriteAllText(Path.Combine(legacyDir, "focus-settings.json"), JsonSerializer.Serialize(legacySettings));

        var targetSettings = new WorkRestSettings(); // default values
        bool merged = FocusCompanionStorage.MergeFromCandidateLegacySettings(targetSettings, new[] { legacyDir });

        Assert.True(merged);
        Assert.Equal("Vitor Developer", targetSettings.UserName);
        Assert.Equal(45, targetSettings.FocusMinutes);
        Assert.Equal(10, targetSettings.ShortBreakMinutes);
        Assert.Equal(20, targetSettings.LongBreakMinutes);
        Assert.Equal(3, targetSettings.CyclesBeforeLongBreak);
        Assert.True(targetSettings.IntervalModeEnabled);
        Assert.False(targetSettings.SoundAlertEnabled);
        Assert.Equal(40, targetSettings.SoundVolume);
        Assert.Equal(60, targetSettings.GoalMilestoneSoundVolume);
        Assert.False(targetSettings.SoundOnStartEnabled);
        Assert.False(targetSettings.SoundOnEndEnabled);
        Assert.False(targetSettings.SoundOnGoalAchievedEnabled);
        Assert.False(targetSettings.SoundOnDailyGoalAchievedEnabled);
        Assert.False(targetSettings.SoundOnWeeklyGoalAchievedEnabled);
        Assert.False(targetSettings.SoundOnBreakStartEnabled);
        Assert.False(targetSettings.SoundOnBreakEndEnabled);
        Assert.False(targetSettings.SoundOnCheerEnabled);
        Assert.False(targetSettings.CompanionVisible);
        Assert.False(targetSettings.CompanionExpanded);
        Assert.False(targetSettings.MiniWidgetEnabled);
        Assert.Equal("TopLeft", targetSettings.MiniWidgetCorner);
        Assert.Equal("Secondary", targetSettings.MiniWidgetTargetMonitor);
        Assert.Equal(1, targetSettings.MiniWidgetMonitorIndex);
        Assert.False(targetSettings.WelcomePopupEnabled);
        Assert.False(targetSettings.GoalNotificationsEnabled);
        Assert.False(targetSettings.DailyGoalNotificationEnabled);
        Assert.False(targetSettings.WeeklyGoalNotificationEnabled);
        Assert.False(targetSettings.ProjectGoalNotificationEnabled);
        Assert.False(targetSettings.MilestoneRemindersEnabled);
        Assert.Equal("All", targetSettings.MilestoneDisplayMode);
        Assert.Equal("Trophy", targetSettings.CompletionRewardIcon);
        Assert.Equal("#FFD700", targetSettings.CompletionRewardColor);
        Assert.True(targetSettings.AutoAdvancePhases);
        Assert.Equal(1250, targetSettings.FocusXp);
        Assert.Equal(12, targetSettings.FocusStreak);
        Assert.Equal(45, targetSettings.TotalSessionsCompleted);
        Assert.Equal("lumberjack", targetSettings.SelectedSceneId);
        Assert.Equal("ascii", targetSettings.ArtMode);
    }

    [Fact]
    public void BackupService_IncludesStreaksJson_InMonitoredFiles()
    {
        Assert.Contains("streaks.json", BackupService.MonitoredDataFiles);
    }

    [Fact]
    public void StreakStorage_DeserializeProfile_HandlesAliasesAndStringHolidays()
    {
        var legacyJson = """
        {
            "streakEnabled": true,
            "holidays": [
                "2026-07-04",
                { "date": "2026-12-31", "description": "New Year's Eve" }
            ],
            "spentGoldStars": 30,
            "bonusStars": 5,
            "usedSickDays": ["2026-08-10"],
            "returnedSickCredits": 1
        }
        """;

        var profile = StreakStorage.DeserializeProfile(legacyJson);

        Assert.Equal(2, profile.Holidays.Count);
        Assert.Contains(profile.Holidays, h => h.Date == new DateTime(2026, 7, 4));
        Assert.Contains(profile.Holidays, h => h.Date == new DateTime(2026, 12, 31) && h.Description == "New Year's Eve");
        Assert.Equal(30, profile.GoldStarsSpent);
        Assert.Equal(5, profile.BonusGoldStars);
        Assert.Single(profile.UsedSickDates);
        Assert.Equal(new DateTime(2026, 8, 10), profile.UsedSickDates[0]);
        Assert.Equal(1, profile.ReturnedSickDays);
    }
}
