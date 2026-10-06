using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class CalendarPlanStorage
{
    private static readonly string FilePath = StoragePathHelper.GetFilePath("calendar-plans.json");
    private static CalendarPlanStorage? _instance;
    public static CalendarPlanStorage Instance => _instance ??= new CalendarPlanStorage();

    public event Action? PlansChanged;
    private readonly object _lock = new();
    private List<PlannedWorkItem>? _cachedPlans;

    public List<PlannedWorkItem> Load()
    {
        lock (_lock)
        {
            if (_cachedPlans != null)
            {
                return new List<PlannedWorkItem>(_cachedPlans);
            }

            try
            {
                if (!File.Exists(FilePath))
                {
                    _cachedPlans = new List<PlannedWorkItem>();
                    return new List<PlannedWorkItem>(_cachedPlans);
                }

                string json = File.ReadAllText(FilePath);
                var items = JsonSerializer.Deserialize<List<PlannedWorkItem>>(json) ?? new List<PlannedWorkItem>();
                _cachedPlans = items;
                return new List<PlannedWorkItem>(_cachedPlans);
            }
            catch (Exception ex)
            {
                StoragePathHelper.LogError(ex, "CalendarPlanStorage.Load");
                _cachedPlans = new List<PlannedWorkItem>();
                return new List<PlannedWorkItem>(_cachedPlans);
            }
        }
    }

    public void Save(List<PlannedWorkItem> plans)
    {
        lock (_lock)
        {
            _cachedPlans = new List<PlannedWorkItem>(plans);
            try
            {
                string json = JsonSerializer.Serialize(plans, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    StoragePathHelper.EnsureDirectoryExists(dir);
                }

                File.WriteAllText(FilePath, json);
                PlansChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StoragePathHelper.LogError(ex, "CalendarPlanStorage.Save");
            }
        }
    }

    public void AddOrUpdatePlan(PlannedWorkItem plan)
    {
        var plans = Load();
        int existingIndex = plans.FindIndex(p => p.Id == plan.Id);
        if (existingIndex >= 0)
        {
            plans[existingIndex] = plan;
        }
        else
        {
            plans.Add(plan);
        }
        Save(plans);
    }

    public bool DeletePlan(string planId)
    {
        var plans = Load();
        int count = plans.RemoveAll(p => p.Id == planId);
        if (count > 0)
        {
            Save(plans);
            return true;
        }
        return false;
    }

    public List<PlannedWorkItem> CopyPlanToSubsequentDays(PlannedWorkItem sourcePlan, int numberOfDays)
    {
        if (sourcePlan == null || numberOfDays <= 0)
        {
            return new List<PlannedWorkItem>();
        }

        var plans = Load();
        var createdPlans = new List<PlannedWorkItem>();

        for (int i = 1; i <= numberOfDays; i++)
        {
            var targetDate = sourcePlan.Date.Date.AddDays(i);
            var copiedItem = new PlannedWorkItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Date = targetDate,
                ProjectName = sourcePlan.ProjectName,
                Activity = sourcePlan.Activity,
                PlannedHours = sourcePlan.PlannedHours,
                Note = sourcePlan.Note,
                CreatedAt = DateTime.Now
            };
            plans.Add(copiedItem);
            createdPlans.Add(copiedItem);
        }

        Save(plans);
        return createdPlans;
    }

    public List<PlannedWorkItem> GetPlansForDate(DateTime date)
    {
        return Load()
            .Where(p => p.Date.Date == date.Date)
            .OrderBy(p => p.ProjectName)
            .ToList();
    }

    public List<PlannedWorkItem> GetPlansForRange(DateTime start, DateTime end)
    {
        return Load()
            .Where(p => p.Date.Date >= start.Date && p.Date.Date <= end.Date)
            .OrderBy(p => p.Date)
            .ThenBy(p => p.ProjectName)
            .ToList();
    }

    public static (DateTime Monday, DateTime Sunday) GetWeekBounds(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = date.Date.AddDays(-diff);
        var sunday = monday.AddDays(6);
        return (monday, sunday);
    }

    public DailyGoalSummary CalculateDailySummary(DateTime date, List<TimeEntry> allEntries)
    {
        var dateOnly = date.Date;
        var dayPlans = GetPlansForDate(dateOnly);
        double plannedHours = dayPlans.Sum(p => p.PlannedHours);

        var dayEntries = allEntries
            .Where(e => e.StartTime.Date == dateOnly)
            .ToList();

        var totalDuration = TimeSpan.FromTicks(dayEntries.Sum(e => e.Duration.Ticks));
        double actualHours = totalDuration.TotalHours;

        var projects = dayEntries
            .Select(e => e.ProjectName)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .ToList();

        var activities = dayEntries
            .Select(e => e.Activity)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct()
            .ToList();

        var tasks = dayEntries
            .Select(e => e.Description)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct()
            .ToList();

        return new DailyGoalSummary
        {
            Date = dateOnly,
            PlannedHours = plannedHours,
            ActualHours = actualHours,
            EntryCount = dayEntries.Count,
            ProjectsSummary = projects.Count > 0 ? string.Join(", ", projects) : (dayPlans.Count > 0 ? string.Join(", ", dayPlans.Select(p => p.ProjectName).Distinct()) : "None"),
            ActivitiesSummary = activities.Count > 0 ? string.Join(", ", activities) : "None",
            TasksSummary = tasks.Count > 0 ? string.Join(", ", tasks) : (dayPlans.Count > 0 ? string.Join("; ", dayPlans.Where(p => !string.IsNullOrWhiteSpace(p.Note)).Select(p => p.Note)) : "None"),
            PlannedItems = dayPlans
        };
    }

    public WeeklyGoalSummary CalculateWeeklySummary(DateTime dateInWeek, List<TimeEntry> allEntries)
    {
        var (monday, sunday) = GetWeekBounds(dateInWeek);
        var weekPlans = GetPlansForRange(monday, sunday);
        double totalPlannedHours = weekPlans.Sum(p => p.PlannedHours);

        var weekEntries = allEntries
            .Where(e => e.StartTime.Date >= monday && e.StartTime.Date <= sunday)
            .ToList();

        var totalActualDuration = TimeSpan.FromTicks(weekEntries.Sum(e => e.Duration.Ticks));
        double totalActualHours = totalActualDuration.TotalHours;

        var dailySummaries = new List<DailyGoalSummary>();
        for (int i = 0; i < 7; i++)
        {
            var currentDay = monday.AddDays(i);
            dailySummaries.Add(CalculateDailySummary(currentDay, allEntries));
        }

        // Project comparisons for this week
        var allProjectNames = weekPlans.Select(p => p.ProjectName)
            .Union(weekEntries.Select(e => e.ProjectName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var projectComparisons = new List<ProjectGoalComparison>();
        foreach (var proj in allProjectNames)
        {
            double planned = weekPlans.Where(p => p.ProjectName.Equals(proj, StringComparison.OrdinalIgnoreCase)).Sum(p => p.PlannedHours);
            var projEntries = weekEntries.Where(e => e.ProjectName.Equals(proj, StringComparison.OrdinalIgnoreCase)).ToList();
            double actual = projEntries.Sum(e => e.Duration.TotalHours);

            projectComparisons.Add(new ProjectGoalComparison
            {
                ProjectName = proj,
                PlannedHours = planned,
                ActualHours = actual
            });
        }

        return new WeeklyGoalSummary
        {
            WeekStartDate = monday,
            WeekEndDate = sunday,
            TotalPlannedHours = totalPlannedHours,
            TotalActualHours = totalActualHours,
            DailySummaries = dailySummaries,
            ProjectComparisons = projectComparisons
        };
    }
}
