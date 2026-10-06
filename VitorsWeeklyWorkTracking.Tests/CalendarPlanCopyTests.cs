using System;
using System.Collections.Generic;
using System.Linq;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class CalendarPlanCopyTests
{
    [Fact]
    public void CopyPlanToSubsequentDays_WithSingleDay_CreatesOneCopyOnNextDay()
    {
        var sourcePlan = new PlannedWorkItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Date = new DateTime(2026, 10, 5), // Monday
            ProjectName = "Project Alpha",
            Activity = "Development",
            PlannedHours = 4.5,
            Note = "Implement copy feature"
        };

        var created = CalendarPlanStorage.Instance.CopyPlanToSubsequentDays(sourcePlan, 1);

        Assert.NotNull(created);
        Assert.Single(created);

        var copy = created[0];
        Assert.NotEqual(sourcePlan.Id, copy.Id);
        Assert.Equal(new DateTime(2026, 10, 6), copy.Date);
        Assert.Equal(sourcePlan.ProjectName, copy.ProjectName);
        Assert.Equal(sourcePlan.Activity, copy.Activity);
        Assert.Equal(sourcePlan.PlannedHours, copy.PlannedHours);
        Assert.Equal(sourcePlan.Note, copy.Note);

        // Cleanup
        CalendarPlanStorage.Instance.DeletePlan(copy.Id);
    }

    [Fact]
    public void CopyPlanToSubsequentDays_WithMultipleDays_CreatesConsecutiveCopies()
    {
        var sourcePlan = new PlannedWorkItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Date = new DateTime(2026, 10, 5), // Monday
            ProjectName = "Client Website",
            Activity = "Design",
            PlannedHours = 3.0,
            Note = "Sprint tasks"
        };

        const int daysToExtend = 5;
        var created = CalendarPlanStorage.Instance.CopyPlanToSubsequentDays(sourcePlan, daysToExtend);

        Assert.NotNull(created);
        Assert.Equal(daysToExtend, created.Count);

        // Verify all IDs are unique
        var distinctIds = created.Select(c => c.Id).Distinct().ToList();
        Assert.Equal(daysToExtend, distinctIds.Count);
        Assert.DoesNotContain(sourcePlan.Id, distinctIds);

        // Verify dates are consecutive D+1, D+2, D+3, D+4, D+5
        for (int i = 0; i < daysToExtend; i++)
        {
            var expectedDate = sourcePlan.Date.AddDays(i + 1);
            Assert.Equal(expectedDate, created[i].Date);
            Assert.Equal("Client Website", created[i].ProjectName);
            Assert.Equal("Design", created[i].Activity);
            Assert.Equal(3.0, created[i].PlannedHours);
            Assert.Equal("Sprint tasks", created[i].Note);
        }

        // Cleanup
        foreach (var c in created)
        {
            CalendarPlanStorage.Instance.DeletePlan(c.Id);
        }
    }

    [Fact]
    public void CopyPlanToSubsequentDays_AcrossMonthAndYearBoundaries_CalculatesCorrectDates()
    {
        var sourcePlan = new PlannedWorkItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Date = new DateTime(2026, 12, 30),
            ProjectName = "End of Year Review",
            Activity = "Management",
            PlannedHours = 2.0,
            Note = "Closing year tasks"
        };

        var created = CalendarPlanStorage.Instance.CopyPlanToSubsequentDays(sourcePlan, 4);

        Assert.Equal(4, created.Count);
        Assert.Equal(new DateTime(2026, 12, 31), created[0].Date);
        Assert.Equal(new DateTime(2027, 1, 1), created[1].Date);
        Assert.Equal(new DateTime(2027, 1, 2), created[2].Date);
        Assert.Equal(new DateTime(2027, 1, 3), created[3].Date);

        // Cleanup
        foreach (var c in created)
        {
            CalendarPlanStorage.Instance.DeletePlan(c.Id);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void CopyPlanToSubsequentDays_WithZeroOrNegativeDays_ReturnsEmptyList(int invalidDays)
    {
        var sourcePlan = new PlannedWorkItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Date = new DateTime(2026, 10, 5),
            ProjectName = "Test Project",
            PlannedHours = 2.0
        };

        var created = CalendarPlanStorage.Instance.CopyPlanToSubsequentDays(sourcePlan, invalidDays);

        Assert.NotNull(created);
        Assert.Empty(created);
    }

    [Fact]
    public void CopyPlanToSubsequentDays_WithNullPlan_ReturnsEmptyList()
    {
        var created = CalendarPlanStorage.Instance.CopyPlanToSubsequentDays(null!, 3);

        Assert.NotNull(created);
        Assert.Empty(created);
    }

    [Fact]
    public void CopyPlanToSubsequentDays_PersistsInStorageAndCanBeQueried()
    {
        var testDate = new DateTime(2028, 6, 10);
        var sourcePlan = new PlannedWorkItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Date = testDate,
            ProjectName = "Storage Query Test",
            Activity = "Testing",
            PlannedHours = 5.0,
            Note = "Check query retrieval"
        };

        var created = CalendarPlanStorage.Instance.CopyPlanToSubsequentDays(sourcePlan, 2);

        // Query by date
        var day1Plans = CalendarPlanStorage.Instance.GetPlansForDate(testDate.AddDays(1));
        Assert.Contains(day1Plans, p => p.Id == created[0].Id);

        var day2Plans = CalendarPlanStorage.Instance.GetPlansForDate(testDate.AddDays(2));
        Assert.Contains(day2Plans, p => p.Id == created[1].Id);

        // Query by range
        var rangePlans = CalendarPlanStorage.Instance.GetPlansForRange(testDate, testDate.AddDays(2));
        Assert.Contains(rangePlans, p => p.Id == created[0].Id);
        Assert.Contains(rangePlans, p => p.Id == created[1].Id);

        // Cleanup
        foreach (var c in created)
        {
            CalendarPlanStorage.Instance.DeletePlan(c.Id);
        }
    }

    [Fact]
    public void CopyPlanToSubsequentDays_WithDecimalHoursAndEmptyOptionals_CopiesAccurately()
    {
        var sourcePlan = new PlannedWorkItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Date = new DateTime(2026, 11, 2),
            ProjectName = "Quick Sync",
            Activity = "",
            PlannedHours = 0.75,
            Note = ""
        };

        var created = CalendarPlanStorage.Instance.CopyPlanToSubsequentDays(sourcePlan, 3);

        Assert.Equal(3, created.Count);
        foreach (var copy in created)
        {
            Assert.Equal("Quick Sync", copy.ProjectName);
            Assert.Equal("", copy.Activity);
            Assert.Equal(0.75, copy.PlannedHours);
            Assert.Equal("", copy.Note);
            Assert.Equal("45m", copy.FormattedHours);
        }

        // Cleanup
        foreach (var c in created)
        {
            CalendarPlanStorage.Instance.DeletePlan(c.Id);
        }
    }

    [Fact]
    public void CopyPlanToSubsequentDays_LargeExtensionRange_GeneratesAllDaysCorrectly()
    {
        var sourcePlan = new PlannedWorkItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Date = new DateTime(2026, 3, 1),
            ProjectName = "Long Milestone",
            Activity = "Research",
            PlannedHours = 2.5,
            Note = "Daily standup and research"
        };

        const int daysCount = 30;
        var created = CalendarPlanStorage.Instance.CopyPlanToSubsequentDays(sourcePlan, daysCount);

        Assert.Equal(daysCount, created.Count);
        Assert.Equal(new DateTime(2026, 3, 2), created.First().Date);
        Assert.Equal(new DateTime(2026, 3, 31), created.Last().Date);

        // Verify no duplicate IDs
        Assert.Equal(daysCount, created.Select(p => p.Id).Distinct().Count());

        // Cleanup
        foreach (var c in created)
        {
            CalendarPlanStorage.Instance.DeletePlan(c.Id);
        }
    }
}
