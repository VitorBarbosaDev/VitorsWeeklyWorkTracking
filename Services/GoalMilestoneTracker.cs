using System;
using System.Collections.Generic;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

public class GoalMilestoneTracker
{
    private readonly CalendarPlanStorage _planStorage;

    public WorkRestSettings? Settings { get; set; }
    public bool HasShownDailyGoalNotification { get; set; } = false;
    public bool HasShownWeeklyGoalNotification { get; set; } = false;
    public HashSet<string> ShownDailyProjectGoals { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ShownWeeklyProjectGoals { get; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime? LastCheckDate { get; set; } = null;

    public event Action<DailyGoalSummary, double>? DailyGoalAchieved;
    public event Action<WeeklyGoalSummary, double>? WeeklyGoalAchieved;
    public event Action<ProjectGoalSummary, double>? ProjectGoalAchieved;

    public GoalMilestoneTracker(CalendarPlanStorage? planStorage = null, WorkRestSettings? settings = null)
    {
        _planStorage = planStorage ?? CalendarPlanStorage.Instance;
        Settings = settings;
    }

    public void Initialize(DateTime date, List<TimeEntry> entries)
    {
        LastCheckDate = date.Date;

        var initToday = _planStorage.CalculateDailySummary(date, entries);
        HasShownDailyGoalNotification = initToday.PlannedHours > 0 && initToday.ActualHours >= initToday.PlannedHours;

        var initWeek = _planStorage.CalculateWeeklySummary(date, entries);
        HasShownWeeklyGoalNotification = initWeek.TotalPlannedHours > 0 && initWeek.TotalActualHours >= initWeek.TotalPlannedHours;

        ShownDailyProjectGoals.Clear();
        var todayProjects = _planStorage.GetAllPlannedProjectsForDate(date);
        foreach (var proj in todayProjects)
        {
            var projSummary = _planStorage.CalculateProjectDailySummary(proj, date, entries);
            if (projSummary.PlannedHours > 0 && projSummary.ActualHours >= projSummary.PlannedHours)
            {
                ShownDailyProjectGoals.Add(proj);
            }
        }

        ShownWeeklyProjectGoals.Clear();
        var weekProjects = _planStorage.GetAllPlannedProjectsForWeek(date);
        foreach (var proj in weekProjects)
        {
            var projSummary = _planStorage.CalculateProjectWeeklySummary(proj, date, entries);
            if (projSummary.PlannedHours > 0 && projSummary.ActualHours >= projSummary.PlannedHours)
            {
                ShownWeeklyProjectGoals.Add(proj);
            }
        }
    }

    public void CheckMilestones(DateTime date, List<TimeEntry> entries, TimeSpan activeTrackingElapsed, string? activeProjectName = null)
    {
        var dateOnly = date.Date;

        if (LastCheckDate.HasValue && dateOnly != LastCheckDate.Value)
        {
            HasShownDailyGoalNotification = false;
            ShownDailyProjectGoals.Clear();
            if (dateOnly.DayOfWeek == DayOfWeek.Monday)
            {
                HasShownWeeklyGoalNotification = false;
                ShownWeeklyProjectGoals.Clear();
            }
        }
        LastCheckDate = dateOnly;

        var todaySummary = _planStorage.CalculateDailySummary(dateOnly, entries);
        var weeklySummary = _planStorage.CalculateWeeklySummary(dateOnly, entries);

        double activeTodayHours = todaySummary.ActualHours + activeTrackingElapsed.TotalHours;
        double activeWeeklyHours = weeklySummary.TotalActualHours + activeTrackingElapsed.TotalHours;

        int totalPlannedDaysInWeek = _planStorage.GetPlannedDaysCountForWeek(dateOnly);
        bool isWeekPlannedForOnlyOneDay = totalPlannedDaysInWeek <= 1 || weeklySummary.TotalPlannedHours <= todaySummary.PlannedHours + 0.0001;

        var todayProjects = _planStorage.GetAllPlannedProjectsForDate(dateOnly);
        var weekProjects = _planStorage.GetAllPlannedProjectsForWeek(dateOnly);

        // Prioritize active project if specified, then today's planned projects, then week's planned projects
        var candidateProjects = new List<string>();
        if (!string.IsNullOrWhiteSpace(activeProjectName))
        {
            candidateProjects.Add(activeProjectName);
        }
        foreach (var p in todayProjects)
        {
            if (!candidateProjects.Contains(p, StringComparer.OrdinalIgnoreCase))
                candidateProjects.Add(p);
        }
        foreach (var p in weekProjects)
        {
            if (!candidateProjects.Contains(p, StringComparer.OrdinalIgnoreCase))
                candidateProjects.Add(p);
        }

        // 1. Check Multi-Day Project Weekly Goals
        foreach (var proj in candidateProjects)
        {
            var projDailySummary = _planStorage.CalculateProjectDailySummary(proj, dateOnly, entries);
            var projWeeklySummary = _planStorage.CalculateProjectWeeklySummary(proj, dateOnly, entries);

            bool isActiveProj = !string.IsNullOrWhiteSpace(activeProjectName) && proj.Equals(activeProjectName, StringComparison.OrdinalIgnoreCase);
            double projActiveWeeklyHours = projWeeklySummary.ActualHours + (isActiveProj ? activeTrackingElapsed.TotalHours : 0);

            int projDaysInWeek = _planStorage.GetPlannedDaysCountForProjectInWeek(proj, dateOnly);
            bool isProjSingleDay = projDaysInWeek <= 1 || projWeeklySummary.PlannedHours <= projDailySummary.PlannedHours + 0.0001;

            if (!isProjSingleDay && projWeeklySummary.PlannedHours > 0 && projActiveWeeklyHours >= projWeeklySummary.PlannedHours)
            {
                if (!ShownWeeklyProjectGoals.Contains(proj))
                {
                    ShownWeeklyProjectGoals.Add(proj);
                    ShownDailyProjectGoals.Add(proj);

                    if (todaySummary.PlannedHours > 0 && activeTodayHours >= todaySummary.PlannedHours)
                    {
                        HasShownDailyGoalNotification = true;
                    }
                    if (weeklySummary.TotalPlannedHours > 0 && activeWeeklyHours >= weeklySummary.TotalPlannedHours)
                    {
                        HasShownWeeklyGoalNotification = true;
                    }

                    projWeeklySummary.ActualHours = projActiveWeeklyHours;
                    if (ShouldShowProjectGoalNotification)
                    {
                        ProjectGoalAchieved?.Invoke(projWeeklySummary, projActiveWeeklyHours);
                    }
                    return;
                }
            }
        }

        // 2. Check Overall Weekly Goal milestone (only for multi-day planned weeks)
        if (!isWeekPlannedForOnlyOneDay && weeklySummary.TotalPlannedHours > 0 && activeWeeklyHours >= weeklySummary.TotalPlannedHours)
        {
            if (!HasShownWeeklyGoalNotification)
            {
                HasShownWeeklyGoalNotification = true;
                if (todaySummary.PlannedHours > 0 && activeTodayHours >= todaySummary.PlannedHours)
                {
                    HasShownDailyGoalNotification = true;
                }
                if (!string.IsNullOrWhiteSpace(activeProjectName))
                {
                    ShownDailyProjectGoals.Add(activeProjectName);
                    ShownWeeklyProjectGoals.Add(activeProjectName);
                }

                weeklySummary.TotalActualHours = activeWeeklyHours;
                if (ShouldShowWeeklyGoalNotification)
                {
                    WeeklyGoalAchieved?.Invoke(weeklySummary, activeWeeklyHours);
                }
                return;
            }
        }
        else if (isWeekPlannedForOnlyOneDay && todaySummary.PlannedHours > 0 && activeTodayHours >= todaySummary.PlannedHours)
        {
            // If week is only planned for one day, weekly goal is automatically considered met when daily goal is met, without showing a weekly popup
            HasShownWeeklyGoalNotification = true;
        }

        // 3. Check Project Daily Goals
        foreach (var proj in candidateProjects)
        {
            var projDailySummary = _planStorage.CalculateProjectDailySummary(proj, dateOnly, entries);
            var projWeeklySummary = _planStorage.CalculateProjectWeeklySummary(proj, dateOnly, entries);

            bool isActiveProj = !string.IsNullOrWhiteSpace(activeProjectName) && proj.Equals(activeProjectName, StringComparison.OrdinalIgnoreCase);
            double projActiveDailyHours = projDailySummary.ActualHours + (isActiveProj ? activeTrackingElapsed.TotalHours : 0);

            int projDaysInWeek = _planStorage.GetPlannedDaysCountForProjectInWeek(proj, dateOnly);
            bool isProjSingleDay = projDaysInWeek <= 1 || projWeeklySummary.PlannedHours <= projDailySummary.PlannedHours + 0.0001;

            if (projDailySummary.PlannedHours > 0 && projActiveDailyHours >= projDailySummary.PlannedHours)
            {
                if (!ShownDailyProjectGoals.Contains(proj))
                {
                    ShownDailyProjectGoals.Add(proj);
                    if (isProjSingleDay)
                    {
                        ShownWeeklyProjectGoals.Add(proj);
                    }

                    if (todaySummary.PlannedHours > 0 && activeTodayHours >= todaySummary.PlannedHours)
                    {
                        HasShownDailyGoalNotification = true;
                        if (isWeekPlannedForOnlyOneDay)
                        {
                            HasShownWeeklyGoalNotification = true;
                        }
                    }

                    projDailySummary.ActualHours = projActiveDailyHours;
                    if (ShouldShowProjectGoalNotification)
                    {
                        ProjectGoalAchieved?.Invoke(projDailySummary, projActiveDailyHours);
                    }
                    return;
                }
            }
        }

        // 4. Check Overall Daily Goal milestone
        if (todaySummary.PlannedHours > 0 && activeTodayHours >= todaySummary.PlannedHours)
        {
            if (!HasShownDailyGoalNotification)
            {
                HasShownDailyGoalNotification = true;
                if (isWeekPlannedForOnlyOneDay)
                {
                    HasShownWeeklyGoalNotification = true;
                }

                todaySummary.ActualHours = activeTodayHours;
                if (ShouldShowDailyGoalNotification)
                {
                    DailyGoalAchieved?.Invoke(todaySummary, activeTodayHours);
                }
                return;
            }
        }
    }

    private bool ShouldShowProjectGoalNotification =>
        Settings == null || (Settings.GoalNotificationsEnabled && Settings.ProjectGoalNotificationEnabled);

    private bool ShouldShowWeeklyGoalNotification =>
        Settings == null || (Settings.GoalNotificationsEnabled && Settings.WeeklyGoalNotificationEnabled);

    private bool ShouldShowDailyGoalNotification =>
        Settings == null || (Settings.GoalNotificationsEnabled && Settings.DailyGoalNotificationEnabled);

    public void ResetIfPlansOrEntriesDecreased(
        DailyGoalSummary todaySummary,
        WeeklyGoalSummary weeklySummary,
        List<TimeEntry>? entries = null,
        TimeSpan activeTrackingElapsed = default,
        string? activeProjectName = null)
    {
        if (todaySummary.ActualHours < todaySummary.PlannedHours)
        {
            HasShownDailyGoalNotification = false;
        }
        else if (todaySummary.PlannedHours > 0 && todaySummary.ActualHours >= todaySummary.PlannedHours)
        {
            HasShownDailyGoalNotification = true;
        }

        if (weeklySummary.TotalActualHours < weeklySummary.TotalPlannedHours)
        {
            HasShownWeeklyGoalNotification = false;
        }
        else if (weeklySummary.TotalPlannedHours > 0 && weeklySummary.TotalActualHours >= weeklySummary.TotalPlannedHours)
        {
            HasShownWeeklyGoalNotification = true;
        }

        if (entries != null)
        {
            var toRemoveDaily = new List<string>();
            foreach (var proj in ShownDailyProjectGoals)
            {
                var projSummary = _planStorage.CalculateProjectDailySummary(proj, todaySummary.Date, entries);
                double actualHours = projSummary.ActualHours;
                if (!string.IsNullOrWhiteSpace(activeProjectName) && proj.Equals(activeProjectName, StringComparison.OrdinalIgnoreCase))
                {
                    actualHours += activeTrackingElapsed.TotalHours;
                }

                if (projSummary.PlannedHours <= 0 || actualHours < projSummary.PlannedHours)
                {
                    toRemoveDaily.Add(proj);
                }
            }
            foreach (var r in toRemoveDaily) ShownDailyProjectGoals.Remove(r);

            var toRemoveWeekly = new List<string>();
            foreach (var proj in ShownWeeklyProjectGoals)
            {
                var projSummary = _planStorage.CalculateProjectWeeklySummary(proj, weeklySummary.WeekStartDate, entries);
                double actualHours = projSummary.ActualHours;
                if (!string.IsNullOrWhiteSpace(activeProjectName) && proj.Equals(activeProjectName, StringComparison.OrdinalIgnoreCase))
                {
                    actualHours += activeTrackingElapsed.TotalHours;
                }

                if (projSummary.PlannedHours <= 0 || actualHours < projSummary.PlannedHours)
                {
                    toRemoveWeekly.Add(proj);
                }
            }
            foreach (var r in toRemoveWeekly) ShownWeeklyProjectGoals.Remove(r);
        }
    }

    public void ResetDaily()
    {
        HasShownDailyGoalNotification = false;
        ShownDailyProjectGoals.Clear();
    }

    public void ResetWeekly()
    {
        HasShownWeeklyGoalNotification = false;
        ShownWeeklyProjectGoals.Clear();
    }
}
