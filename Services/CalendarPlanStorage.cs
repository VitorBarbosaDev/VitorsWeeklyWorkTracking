using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class CalendarPlanStorage
{
    private readonly string _filePath;
    private static CalendarPlanStorage? _instance;
    public static CalendarPlanStorage Instance => _instance ??= new CalendarPlanStorage();

    public static void SetInstanceForTesting(CalendarPlanStorage? instance)
    {
        _instance = instance;
    }

    public static void ResetInstance()
    {
        _instance = null;
    }

    public void InvalidateCache()
    {
        lock (_lock)
        {
            _cachedPlans = null;
            PlansChanged?.Invoke();
        }
    }

    public event Action? PlansChanged;
    private readonly object _lock = new();
    private List<PlannedWorkItem>? _cachedPlans;

    public CalendarPlanStorage(string? filePath = null)
    {
        _filePath = filePath ?? StoragePathHelper.GetFilePath("calendar-plans.json");
    }

    public static void SanitizePlanItem(PlannedWorkItem? plan)
    {
        if (plan == null) return;
        if (plan.PlannedHours > 0)
        {
            double mins = Math.Round(plan.PlannedHours * 60.0);
            if (mins < 1.0) mins = 1.0;
            plan.PlannedHours = mins / 60.0;
        }
    }

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
                if (!File.Exists(_filePath))
                {
                    _cachedPlans = new List<PlannedWorkItem>();
                    return new List<PlannedWorkItem>(_cachedPlans);
                }

                string json = File.ReadAllText(_filePath);
                var items = JsonSerializer.Deserialize<List<PlannedWorkItem>>(json) ?? new List<PlannedWorkItem>();
                foreach (var item in items)
                {
                    SanitizePlanItem(item);
                }
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
            if (File.Exists(_filePath))
            {
                BackupService.Instance.CreateAutoBackupIfNeeded("Auto (Calendar Plans)");
            }

            foreach (var p in plans)
            {
                SanitizePlanItem(p);
            }
            _cachedPlans = new List<PlannedWorkItem>(plans);
            try
            {
                string json = JsonSerializer.Serialize(plans, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    StoragePathHelper.EnsureDirectoryExists(dir);
                }

                File.WriteAllText(_filePath, json);
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
        SanitizePlanItem(plan);
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

    public bool HasPlannedTaskForProject(string? projectName, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(projectName)) return false;
        var dateOnly = date.Date;
        return GetPlansForDate(dateOnly)
            .Any(p => p.ProjectName.Equals(projectName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public DateTime GetEffectiveEntryDate(TimeEntry? entry)
    {
        if (entry == null) return DateTime.Today;
        var startDate = entry.StartTime.Date;
        var endDate = (entry.EndTime != default && entry.EndTime >= entry.StartTime)
            ? entry.EndTime.Date
            : startDate;

        if (startDate == endDate)
        {
            return startDate;
        }

        // Tracking spans across midnight / day boundary:
        // If start date already has a task for that project, attribute to start date.
        // Otherwise, attribute to end date.
        if (HasPlannedTaskForProject(entry.ProjectName, startDate))
        {
            return startDate;
        }

        return endDate;
    }

    public DateTime GetEffectiveTrackingDate(DateTime? startTime, DateTime currentDate, string? projectName)
    {
        if (!startTime.HasValue) return currentDate.Date;
        var startDate = startTime.Value.Date;
        var endDate = currentDate.Date;

        if (startDate == endDate)
        {
            return startDate;
        }

        // Active tracking across midnight:
        // If start date already has a task for that project, attribute to start date.
        // Otherwise, attribute to current/new day.
        if (HasPlannedTaskForProject(projectName, startDate))
        {
            return startDate;
        }

        return endDate;
    }

    public DailyGoalSummary CalculateDailySummary(DateTime date, List<TimeEntry> allEntries)
    {
        var dateOnly = date.Date;
        var dayPlans = GetPlansForDate(dateOnly);
        foreach (var p in dayPlans)
        {
            p.ActualHours = CalculatePlanItemActualHours(p, allEntries);
            p.IsCompleted = IsPlanItemCompleted(p, allEntries);
        }
        double plannedHours = dayPlans.Sum(p => p.PlannedHours);

        var dayEntries = allEntries
            .Where(e => GetEffectiveEntryDate(e) == dateOnly)
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
            .Where(e =>
            {
                var effDate = GetEffectiveEntryDate(e);
                return effDate >= monday && effDate <= sunday;
            })
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

    public List<string> GetAllPlannedProjectsForDate(DateTime date)
    {
        return GetPlansForDate(date)
            .Select(p => p.ProjectName)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();
    }

    public List<string> GetAllPlannedProjectsForWeek(DateTime dateInWeek)
    {
        var (monday, sunday) = GetWeekBounds(dateInWeek);
        return GetPlansForRange(monday, sunday)
            .Select(p => p.ProjectName)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();
    }

    public int GetPlannedDaysCountForWeek(DateTime dateInWeek)
    {
        var (monday, sunday) = GetWeekBounds(dateInWeek);
        return GetPlansForRange(monday, sunday)
            .Where(p => p.PlannedHours > 0)
            .Select(p => p.Date.Date)
            .Distinct()
            .Count();
    }

    public int GetPlannedDaysCountForProjectInWeek(string projectName, DateTime dateInWeek)
    {
        var (monday, sunday) = GetWeekBounds(dateInWeek);
        return GetPlansForRange(monday, sunday)
            .Where(p => p.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase) && p.PlannedHours > 0)
            .Select(p => p.Date.Date)
            .Distinct()
            .Count();
    }

    public double CalculatePlanItemActualHours(PlannedWorkItem plan, List<TimeEntry> allEntries)
    {
        if (plan == null) return 0;
        var dateOnly = plan.Date.Date;

        var matchingEntries = allEntries
            .Where(e => GetEffectiveEntryDate(e) == dateOnly && e.ProjectName.Equals(plan.ProjectName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!string.IsNullOrWhiteSpace(plan.Activity))
        {
            var activityEntries = matchingEntries
                .Where(e => e.Activity.Equals(plan.Activity, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (activityEntries.Count > 0)
            {
                return TimeSpan.FromTicks(activityEntries.Sum(e => e.Duration.Ticks)).TotalHours;
            }
        }

        return TimeSpan.FromTicks(matchingEntries.Sum(e => e.Duration.Ticks)).TotalHours;
    }

    public bool IsPlanItemCompleted(PlannedWorkItem plan, List<TimeEntry> allEntries)
    {
        if (plan == null || plan.PlannedHours <= 0) return false;
        double actualHours = CalculatePlanItemActualHours(plan, allEntries);
        return actualHours >= plan.PlannedHours;
    }

    public ProjectGoalSummary CalculateProjectDailySummary(string projectName, DateTime date, List<TimeEntry> allEntries)
    {
        var dateOnly = date.Date;
        var dayPlans = GetPlansForDate(dateOnly)
            .Where(p => p.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var p in dayPlans)
        {
            p.ActualHours = CalculatePlanItemActualHours(p, allEntries);
            p.IsCompleted = IsPlanItemCompleted(p, allEntries);
        }
        double plannedHours = dayPlans.Sum(p => p.PlannedHours);

        var dayEntries = allEntries
            .Where(e => GetEffectiveEntryDate(e) == dateOnly && e.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var totalDuration = TimeSpan.FromTicks(dayEntries.Sum(e => e.Duration.Ticks));
        double actualHours = totalDuration.TotalHours;

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

        return new ProjectGoalSummary
        {
            ProjectName = projectName,
            Date = dateOnly,
            IsWeekly = false,
            PlannedHours = plannedHours,
            ActualHours = actualHours,
            PlannedItems = dayPlans,
            ActivitiesSummary = activities.Count > 0 ? string.Join(", ", activities) : (dayPlans.Count > 0 && dayPlans.Any(p => !string.IsNullOrWhiteSpace(p.Activity)) ? string.Join(", ", dayPlans.Where(p => !string.IsNullOrWhiteSpace(p.Activity)).Select(p => p.Activity).Distinct()) : "None"),
            TasksSummary = tasks.Count > 0 ? string.Join(", ", tasks) : (dayPlans.Count > 0 && dayPlans.Any(p => !string.IsNullOrWhiteSpace(p.Note)) ? string.Join("; ", dayPlans.Where(p => !string.IsNullOrWhiteSpace(p.Note)).Select(p => p.Note)) : "None")
        };
    }

    public ProjectGoalSummary CalculateProjectWeeklySummary(string projectName, DateTime dateInWeek, List<TimeEntry> allEntries)
    {
        var (monday, sunday) = GetWeekBounds(dateInWeek);
        var weekPlans = GetPlansForRange(monday, sunday)
            .Where(p => p.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var p in weekPlans)
        {
            p.ActualHours = CalculatePlanItemActualHours(p, allEntries);
            p.IsCompleted = IsPlanItemCompleted(p, allEntries);
        }
        double plannedHours = weekPlans.Sum(p => p.PlannedHours);

        var weekEntries = allEntries
            .Where(e =>
            {
                var effDate = GetEffectiveEntryDate(e);
                return effDate >= monday && effDate <= sunday && e.ProjectName.Equals(projectName, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        var totalDuration = TimeSpan.FromTicks(weekEntries.Sum(e => e.Duration.Ticks));
        double actualHours = totalDuration.TotalHours;

        var activities = weekEntries
            .Select(e => e.Activity)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct()
            .ToList();

        var tasks = weekEntries
            .Select(e => e.Description)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct()
            .ToList();

        return new ProjectGoalSummary
        {
            ProjectName = projectName,
            Date = monday,
            IsWeekly = true,
            PlannedHours = plannedHours,
            ActualHours = actualHours,
            PlannedItems = weekPlans,
            ActivitiesSummary = activities.Count > 0 ? string.Join(", ", activities) : (weekPlans.Count > 0 && weekPlans.Any(p => !string.IsNullOrWhiteSpace(p.Activity)) ? string.Join(", ", weekPlans.Where(p => !string.IsNullOrWhiteSpace(p.Activity)).Select(p => p.Activity).Distinct()) : "None"),
            TasksSummary = tasks.Count > 0 ? string.Join(", ", tasks) : (weekPlans.Count > 0 && weekPlans.Any(p => !string.IsNullOrWhiteSpace(p.Note)) ? string.Join("; ", weekPlans.Where(p => !string.IsNullOrWhiteSpace(p.Note)).Select(p => p.Note)) : "None")
        };
    }
}
