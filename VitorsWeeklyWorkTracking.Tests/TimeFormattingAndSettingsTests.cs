using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class TimeFormattingAndSettingsTests
{
    [Theory]
    [InlineData(0.0, "0m")]
    [InlineData(0.8, "48m")]
    [InlineData(0.5, "30m")]
    [InlineData(0.25, "15m")]
    [InlineData(0.75, "45m")]
    [InlineData(1.0, "1h")]
    [InlineData(1.5, "1h 30m")]
    [InlineData(2.0, "2h")]
    [InlineData(2.25, "2h 15m")]
    [InlineData(-0.5, "0m")]
    public void TimeFormatHelper_FormatHours_FormatsCorrectly(double hours, string expected)
    {
        var result = TimeFormatHelper.FormatHours(hours);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.8, "—", "48 mins")]
    [InlineData(0.5, "—", "30 mins")]
    [InlineData(0.0, "—", "—")]
    [InlineData(0.0, "0 mins", "0 mins")]
    [InlineData(1.0, "—", "1.0 hrs")]
    [InlineData(1.5, "—", "1.5 hrs")]
    [InlineData(2.5, "—", "2.5 hrs")]
    public void TimeFormatHelper_FormatHoursWithUnit_FormatsCorrectly(double hours, string fallback, string expected)
    {
        var result = TimeFormatHelper.FormatHoursWithUnit(hours, fallback);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.8, "48 mins left")]
    [InlineData(0.5, "30 mins left")]
    [InlineData(1.5, "1.5 hrs left")]
    [InlineData(0.0, "0 mins left")]
    public void TimeFormatHelper_FormatRemainingHours_FormatsCorrectly(double hours, string expected)
    {
        var result = TimeFormatHelper.FormatRemainingHours(hours);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PlannedWorkItem_FormattedHours_ShowsMinutesUnderOneHour()
    {
        var plan = new PlannedWorkItem
        {
            ProjectName = "Project Alpha",
            PlannedHours = 0.8
        };

        Assert.Equal("48m", plan.FormattedHours);

        plan.PlannedHours = 0.0;
        Assert.Equal("0m", plan.FormattedHours);

        plan.PlannedHours = 1.0;
        Assert.Equal("1h", plan.FormattedHours);

        plan.PlannedHours = 1.75;
        Assert.Equal("1h 45m", plan.FormattedHours);
    }

    [Fact]
    public void ProjectGoalComparison_FormattedPlannedAndActual_UseCleanMinuteFormatting()
    {
        var pc = new ProjectGoalComparison
        {
            ProjectName = "Game Dev",
            PlannedHours = 0.8,
            ActualHours = 0.0
        };

        Assert.Equal("48m", pc.FormattedPlanned);
        Assert.Equal("0m", pc.FormattedActual);
        Assert.False(pc.IsGoalMet);

        pc.ActualHours = 0.8;
        Assert.Equal("48m", pc.FormattedActual);
        Assert.True(pc.IsGoalMet);
    }

    [Fact]
    public void PlannedTaskItemViewModel_ShowsMinutesForSubHourPlans()
    {
        var plan = new PlannedWorkItem
        {
            ProjectName = "Client Work",
            PlannedHours = 0.8
        };

        var vm = new PlannedTaskItemViewModel
        {
            Plan = plan,
            ActualHours = 0.5,
            RewardIcon = "⭐"
        };

        Assert.Equal("48m", vm.FormattedPlanned);
        Assert.Equal("30m", vm.FormattedActual);
        Assert.False(vm.IsCompleted);
        Assert.Contains("30m / 48m", vm.StatusText);

        vm.ActualHours = 0.8;
        Assert.True(vm.IsCompleted);
        Assert.Contains("⭐ Completed", vm.StatusText);
    }

    [Fact]
    public void DailyAndWeeklyGoalSummary_FormattedHours_DisplaysMinutesCorrectly()
    {
        var daily = new DailyGoalSummary
        {
            Date = DateTime.Today,
            PlannedHours = 0.8,
            ActualHours = 0.8
        };

        Assert.Equal("48 mins", daily.FormattedPlannedHours);
        Assert.Equal("48 mins", daily.FormattedActualHours);
        Assert.True(daily.IsGoalMet);

        var weekly = new WeeklyGoalSummary
        {
            WeekStartDate = DateTime.Today,
            WeekEndDate = DateTime.Today.AddDays(6),
            TotalPlannedHours = 0.8,
            TotalActualHours = 0.5
        };

        Assert.Equal("48 mins", weekly.FormattedPlannedHours);
        Assert.Equal("30 mins", weekly.FormattedActualHours);
        Assert.Equal("18 mins left", weekly.FormattedRemainingHours);
        Assert.False(weekly.IsGoalMet);
    }

    [Fact]
    public void WorkRestSettings_DefaultGoalNotificationPreferences_AreEnabled()
    {
        var settings = new WorkRestSettings();

        Assert.True(settings.GoalNotificationsEnabled);
        Assert.True(settings.DailyGoalNotificationEnabled);
        Assert.True(settings.WeeklyGoalNotificationEnabled);
        Assert.True(settings.ProjectGoalNotificationEnabled);
        Assert.Equal("Star", settings.CompletionRewardIcon);
        Assert.Equal("⭐", settings.RewardIconEmoji);
    }

    [Theory]
    [InlineData("Star", "⭐")]
    [InlineData("Dinosaur", "🦖")]
    [InlineData("Brontosaurus", "🦕")]
    [InlineData("Trophy", "🏆")]
    [InlineData("Medal", "🏅")]
    [InlineData("Rocket", "🚀")]
    [InlineData("Target", "🎯")]
    [InlineData("Crown", "👑")]
    [InlineData("Checkmark", "✅")]
    public void RewardIconHelper_ReturnsCorrectEmojiForOptions(string key, string expectedEmoji)
    {
        var emoji = RewardIconHelper.GetIconEmoji(key);
        Assert.Equal(expectedEmoji, emoji);
    }

    [Theory]
    [InlineData("1m", 1.0 / 60.0)]
    [InlineData("1 min", 1.0 / 60.0)]
    [InlineData("1 mins", 1.0 / 60.0)]
    [InlineData("1 minute", 1.0 / 60.0)]
    [InlineData("0.01", 1.0 / 60.0)]
    [InlineData("0.01h", 1.0 / 60.0)]
    [InlineData("0.01667", 1.0 / 60.0)]
    [InlineData("30m", 0.5)]
    [InlineData("45 mins", 0.75)]
    [InlineData("1h", 1.0)]
    [InlineData("1.5h", 1.5)]
    [InlineData("1h 30m", 1.5)]
    [InlineData("1 hr 30 mins", 1.5)]
    [InlineData("1:30", 1.5)]
    [InlineData("0:45", 0.75)]
    [InlineData("0:01", 1.0 / 60.0)]
    [InlineData("0.5", 0.5)]
    [InlineData("0.25", 0.25)]
    [InlineData("4.0", 4.0)]
    public void TimeFormatHelper_TryParseDurationHours_ParsesValidInputs(string input, double expectedHours)
    {
        bool success = TimeFormatHelper.TryParseDurationHours(input, out double hours);
        Assert.True(success);
        Assert.Equal(expectedHours, hours, precision: 6);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("0")]
    [InlineData("-1h")]
    public void TimeFormatHelper_TryParseDurationHours_RejectsInvalidInputs(string input)
    {
        bool success = TimeFormatHelper.TryParseDurationHours(input, out _);
        Assert.False(success);
    }

    [Fact]
    public void RewardColorHelper_ReturnsValidBrushes_ForAllAvailableColors()
    {
        Assert.NotEmpty(RewardColorHelper.AvailableColors);

        foreach (var option in RewardColorHelper.AvailableColors)
        {
            var (bg, border, text) = RewardColorHelper.GetRewardBrushes(option.Key);
            Assert.NotNull(bg);
            Assert.NotNull(border);
            Assert.NotNull(text);
        }
    }

    [Fact]
    public void WorkRestSettings_CompletionRewardColor_DefaultsToThemeDefault()
    {
        var settings = new WorkRestSettings();
        Assert.Equal("ThemeDefault", settings.CompletionRewardColor);
    }

    [Fact]
    public void PlannedWorkItem_RewardAndStatusProperties_FormatCorrectly()
    {
        var planToday = new PlannedWorkItem
        {
            Date = DateTime.Today,
            ProjectName = "Alpha",
            PlannedHours = 1.0,
            ActualHours = 0.0,
            IsCompleted = false
        };

        Assert.Equal("Alpha", planToday.FormattedProjectWithReward);
        Assert.Equal("⚪ Not Started Yet", planToday.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.NotStarted, planToday.StatusCategory);

        var planFuture = new PlannedWorkItem
        {
            Date = DateTime.Today.AddDays(2),
            ProjectName = "Alpha",
            PlannedHours = 2.0,
            ActualHours = 0.0,
            IsCompleted = false
        };
        Assert.Equal("⚪ Not Started Yet", planFuture.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.NotStarted, planFuture.StatusCategory);

        var planPastUnstarted = new PlannedWorkItem
        {
            Date = DateTime.Today.AddDays(-2),
            ProjectName = "Alpha",
            PlannedHours = 2.0,
            ActualHours = 0.0,
            IsCompleted = false
        };
        Assert.Equal("❌ Goal Not Met", planPastUnstarted.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.GoalNotMet, planPastUnstarted.StatusCategory);

        var planPastPartial = new PlannedWorkItem
        {
            Date = DateTime.Today.AddDays(-1),
            ProjectName = "Alpha",
            PlannedHours = 2.0,
            ActualHours = 1.0,
            IsCompleted = false
        };
        Assert.Equal("❌ Goal Not Met (50%)", planPastPartial.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.GoalNotMet, planPastPartial.StatusCategory);

        var planTodayInProgress = new PlannedWorkItem
        {
            Date = DateTime.Today,
            ProjectName = "Alpha",
            PlannedHours = 2.0,
            ActualHours = 1.0,
            IsCompleted = false
        };
        Assert.Equal("⏳ In Progress (50%)", planTodayInProgress.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.InProgress, planTodayInProgress.StatusCategory);

        var planCompleted = new PlannedWorkItem
        {
            Date = DateTime.Today,
            ProjectName = "Alpha",
            PlannedHours = 1.0,
            ActualHours = 1.0,
            IsCompleted = true,
            RewardIcon = "🦖"
        };

        // Completed planned tasks do not prefix an emoji to their project title, but include it in their status text
        Assert.Equal("Alpha", planCompleted.FormattedProjectWithReward);
        Assert.Equal("🦖 Goal Met", planCompleted.CompletionStatusText);
        Assert.Equal(GoalStatusCategory.GoalMet, planCompleted.StatusCategory);
    }

    [Fact]
    public void WorkRestSettings_GoalMilestoneSoundVolume_DefaultsTo75Percent()
    {
        var settings = new WorkRestSettings();
        Assert.Equal(75, settings.GoalMilestoneSoundVolume);
        Assert.Equal(75, settings.SoundVolume);
    }

    [Theory]
    [InlineData(SoundEffectType.DailyGoalAchieved, 90, 40, 90)]
    [InlineData(SoundEffectType.WeeklyGoalAchieved, 85, 30, 85)]
    [InlineData(SoundEffectType.GoalAchieved, 70, 20, 70)]
    [InlineData(SoundEffectType.Start, 90, 40, 40)]
    [InlineData(SoundEffectType.End, 90, 40, 40)]
    [InlineData(SoundEffectType.BreakStart, 90, 40, 40)]
    [InlineData(SoundEffectType.BreakEnd, 90, 40, 40)]
    [InlineData(SoundEffectType.Cheer, 90, 40, 40)]
    public void SoundEffectManager_GetEffectiveVolume_ReturnsAppropriateVolumePerEventType(
        SoundEffectType eventType,
        int milestoneVolume,
        int masterVolume,
        int expectedEffectiveVolume)
    {
        var settings = new WorkRestSettings
        {
            SoundAlertEnabled = true,
            GoalMilestoneSoundVolume = milestoneVolume,
            SoundVolume = masterVolume
        };

        var effectiveVolume = SoundEffectManager.GetEffectiveVolume(eventType, settings);
        Assert.Equal(expectedEffectiveVolume, effectiveVolume);
    }

    [Fact]
    public void SoundEffectManager_GetEffectiveVolume_WhenSoundAlertDisabled_ReturnsZero()
    {
        var settings = new WorkRestSettings
        {
            SoundAlertEnabled = false,
            GoalMilestoneSoundVolume = 80,
            SoundVolume = 60
        };

        Assert.Equal(0, SoundEffectManager.GetEffectiveVolume(SoundEffectType.DailyGoalAchieved, settings));
        Assert.Equal(0, SoundEffectManager.GetEffectiveVolume(SoundEffectType.WeeklyGoalAchieved, settings));
        Assert.Equal(0, SoundEffectManager.GetEffectiveVolume(SoundEffectType.GoalAchieved, settings));
        Assert.Equal(0, SoundEffectManager.GetEffectiveVolume(SoundEffectType.Start, settings));
    }

    [Fact]
    public void SoundEffectManager_GoalMilestoneVolume_IsIndependentFromMasterVolume()
    {
        // Case 1: Master volume is muted (0), but Goal Milestone fanfare is active (80)
        var settingsMutedMaster = new WorkRestSettings
        {
            SoundAlertEnabled = true,
            SoundVolume = 0,
            GoalMilestoneSoundVolume = 80
        };

        Assert.Equal(80, SoundEffectManager.GetEffectiveVolume(SoundEffectType.DailyGoalAchieved, settingsMutedMaster));
        Assert.Equal(80, SoundEffectManager.GetEffectiveVolume(SoundEffectType.WeeklyGoalAchieved, settingsMutedMaster));
        Assert.Equal(80, SoundEffectManager.GetEffectiveVolume(SoundEffectType.GoalAchieved, settingsMutedMaster));
        Assert.Equal(0, SoundEffectManager.GetEffectiveVolume(SoundEffectType.Start, settingsMutedMaster));
        Assert.Equal(0, SoundEffectManager.GetEffectiveVolume(SoundEffectType.End, settingsMutedMaster));

        // Case 2: Goal Milestone fanfare is muted (0), but Master timer volume is active (60)
        var settingsMutedMilestone = new WorkRestSettings
        {
            SoundAlertEnabled = true,
            SoundVolume = 60,
            GoalMilestoneSoundVolume = 0
        };

        Assert.Equal(0, SoundEffectManager.GetEffectiveVolume(SoundEffectType.DailyGoalAchieved, settingsMutedMilestone));
        Assert.Equal(0, SoundEffectManager.GetEffectiveVolume(SoundEffectType.WeeklyGoalAchieved, settingsMutedMilestone));
        Assert.Equal(0, SoundEffectManager.GetEffectiveVolume(SoundEffectType.GoalAchieved, settingsMutedMilestone));
        Assert.Equal(60, SoundEffectManager.GetEffectiveVolume(SoundEffectType.Start, settingsMutedMilestone));
        Assert.Equal(60, SoundEffectManager.GetEffectiveVolume(SoundEffectType.End, settingsMutedMilestone));
    }

    [Fact]
    public void SoundEffectManager_GenerateWavBytesForVariation_GeneratesValidAudioData()
    {
        var wavBytesFull = SoundEffectManager.GenerateWavBytesForVariation("daily_triumph", 100);
        var wavBytesHalf = SoundEffectManager.GenerateWavBytesForVariation("daily_triumph", 50);
        var wavBytesZero = SoundEffectManager.GenerateWavBytesForVariation("daily_triumph", 0);

        Assert.NotEmpty(wavBytesFull);
        Assert.NotEmpty(wavBytesHalf);
        Assert.NotEmpty(wavBytesZero);
        Assert.Equal(wavBytesFull.Length, wavBytesHalf.Length);
        Assert.Equal(wavBytesFull.Length, wavBytesZero.Length);

        // At 0 volume, all PCM sample bytes (after 44-byte WAV header) should be silent (0)
        var pcmSamplesZero = wavBytesZero.Skip(44).ToArray();
        Assert.All(pcmSamplesZero, b => Assert.Equal(0, b));

        // At full volume, PCM samples should contain non-zero audio content
        var pcmSamplesFull = wavBytesFull.Skip(44).ToArray();
        Assert.Contains(pcmSamplesFull, b => b != 0);
    }

    [Fact]
    public void WorkRestSettings_SerializationAndDeserialization_PreservesGoalMilestoneSoundVolume()
    {
        var settings = new WorkRestSettings
        {
            SoundVolume = 50,
            GoalMilestoneSoundVolume = 92,
            GoalNotificationsEnabled = true,
            CompletionRewardIcon = "Trophy"
        };

        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<WorkRestSettings>(json);

        Assert.NotNull(restored);
        Assert.Equal(50, restored.SoundVolume);
        Assert.Equal(92, restored.GoalMilestoneSoundVolume);
        Assert.Equal("Trophy", restored.CompletionRewardIcon);
    }

    [Fact]
    public void WorkRestSettings_LegacyJsonWithoutMilestoneVolume_DefaultsTo75()
    {
        var legacyJson = """
        {
            "IntervalModeEnabled": true,
            "FocusMinutes": 25,
            "SoundVolume": 60,
            "SoundAlertEnabled": true
        }
        """;

        var restored = JsonSerializer.Deserialize<WorkRestSettings>(legacyJson);

        Assert.NotNull(restored);
        Assert.Equal(60, restored.SoundVolume);
        Assert.Equal(75, restored.GoalMilestoneSoundVolume);
        Assert.False(restored.ForceWelcomePopupOnNextLaunch);
    }

    [Fact]
    public void WorkRestSettings_ForceWelcomePopupOnNextLaunch_DefaultsToFalse()
    {
        var settings = new WorkRestSettings();
        Assert.False(settings.ForceWelcomePopupOnNextLaunch);
        Assert.True(settings.WelcomePopupEnabled);
        Assert.Null(settings.LastWelcomePopupDate);
    }

    [Fact]
    public void WorkRestSettings_ShouldShowWelcomePopup_FirstLaunch_ReturnsTrue()
    {
        var settings = new WorkRestSettings
        {
            WelcomePopupEnabled = true,
            LastWelcomePopupDate = null,
            ForceWelcomePopupOnNextLaunch = false
        };

        var today = new DateTime(2026, 10, 22);
        Assert.True(settings.ShouldShowWelcomePopup(today));
    }

    [Fact]
    public void WorkRestSettings_ShouldShowWelcomePopup_SameDayWithoutForce_ReturnsFalse()
    {
        var today = new DateTime(2026, 10, 22);
        var settings = new WorkRestSettings
        {
            WelcomePopupEnabled = true,
            LastWelcomePopupDate = today,
            ForceWelcomePopupOnNextLaunch = false
        };

        Assert.False(settings.ShouldShowWelcomePopup(today));
    }

    [Fact]
    public void WorkRestSettings_ShouldShowWelcomePopup_SameDayWithForce_ReturnsTrue()
    {
        var today = new DateTime(2026, 10, 22);
        var settings = new WorkRestSettings
        {
            WelcomePopupEnabled = true,
            LastWelcomePopupDate = today,
            ForceWelcomePopupOnNextLaunch = true
        };

        // Even though popup was already shown today, forced flag causes it to show again
        Assert.True(settings.ShouldShowWelcomePopup(today));
    }

    [Fact]
    public void WorkRestSettings_ShouldShowWelcomePopup_Disabled_ReturnsFalseEvenWhenForced()
    {
        var today = new DateTime(2026, 10, 22);
        var settings = new WorkRestSettings
        {
            WelcomePopupEnabled = false,
            LastWelcomePopupDate = today,
            ForceWelcomePopupOnNextLaunch = true
        };

        Assert.False(settings.ShouldShowWelcomePopup(today));
    }

    [Fact]
    public void WorkRestSettings_MarkWelcomePopupShown_ClearsForceFlagAndUpdatesDate()
    {
        var today = new DateTime(2026, 10, 22);
        var settings = new WorkRestSettings
        {
            WelcomePopupEnabled = true,
            LastWelcomePopupDate = null,
            ForceWelcomePopupOnNextLaunch = true
        };

        Assert.True(settings.ShouldShowWelcomePopup(today));

        settings.MarkWelcomePopupShown(today);

        Assert.False(settings.ForceWelcomePopupOnNextLaunch);
        Assert.Equal(today.Date, settings.LastWelcomePopupDate);

        // Next launch on the same day should now return false (consumed)
        Assert.False(settings.ShouldShowWelcomePopup(today));

        // Launch on the next day should return true (daily popup)
        var tomorrow = today.AddDays(1);
        Assert.True(settings.ShouldShowWelcomePopup(tomorrow));
    }

    [Fact]
    public void WorkRestSettings_SerializationAndDeserialization_PreservesForceWelcomePopupOnNextLaunch()
    {
        var settings = new WorkRestSettings
        {
            UserName = "Vitor",
            WelcomePopupEnabled = true,
            ForceWelcomePopupOnNextLaunch = true
        };

        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<WorkRestSettings>(json);

        Assert.NotNull(restored);
        Assert.True(restored.ForceWelcomePopupOnNextLaunch);
        Assert.Equal("Vitor", restored.UserName);
        Assert.True(restored.WelcomePopupEnabled);
    }
}
