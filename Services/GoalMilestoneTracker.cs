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

    private readonly HashSet<DateTime> _dailyGoalsByDate = new();
    private readonly Dictionary<DateTime, HashSet<string>> _dailyProjectGoalsByDate = new();
    private readonly HashSet<DateTime> _weeklyGoalsByWeek = new();
    private readonly Dictionary<DateTime, HashSet<string>> _weeklyProjectGoalsByWeek = new();

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

        _dailyGoalsByDate.Clear();
        _dailyProjectGoalsByDate.Clear();
        _weeklyGoalsByWeek.Clear();
        _weeklyProjectGoalsByWeek.Clear();
        ShownDailyProjectGoals.Clear();
        ShownWeeklyProjectGoals.Clear();

        var (monday, sunday) = CalendarPlanStorage.GetWeekBounds(date);
        var weeklySummary = _planStorage.CalculateWeeklySummary(date, entries);

        if (weeklySummary.TotalPlannedHours > 0 && weeklySummary.TotalActualHours >= weeklySummary.TotalPlannedHours)
        {
            _weeklyGoalsByWeek.Add(monday);
        }

        var weekProjects = _planStorage.GetAllPlannedProjectsForWeek(date);
        var shownWeekProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var proj in weekProjects)
        {
            var projSummary = _planStorage.CalculateProjectWeeklySummary(proj, date, entries);
            if (projSummary.PlannedHours > 0 && projSummary.ActualHours >= projSummary.PlannedHours)
            {
                shownWeekProjects.Add(proj);
            }
        }
        _weeklyProjectGoalsByWeek[monday] = shownWeekProjects;

        for (var d = monday; d <= sunday; d = d.AddDays(1))
        {
            var daySummary = _planStorage.CalculateDailySummary(d, entries);
            if (daySummary.PlannedHours > 0 && daySummary.ActualHours >= daySummary.PlannedHours)
            {
                _dailyGoalsByDate.Add(d);
            }

            var dayProjects = _planStorage.GetAllPlannedProjectsForDate(d);
            var shownDayProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var proj in dayProjects)
            {
                var projSummary = _planStorage.CalculateProjectDailySummary(proj, d, entries);
                if (projSummary.PlannedHours > 0 && projSummary.ActualHours >= projSummary.PlannedHours)
                {
                    shownDayProjects.Add(proj);
                }
            }
            _dailyProjectGoalsByDate[d] = shownDayProjects;
        }

        LoadStateForDate(date.Date, entries);
    }

    private void SaveCurrentStateToDate(DateTime d)
    {
        var dateKey = d.Date;
        var (weekMonday, _) = CalendarPlanStorage.GetWeekBounds(dateKey);

        if (HasShownDailyGoalNotification)
            _dailyGoalsByDate.Add(dateKey);
        else
            _dailyGoalsByDate.Remove(dateKey);

        if (!_dailyProjectGoalsByDate.TryGetValue(dateKey, out var daySet))
        {
            daySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _dailyProjectGoalsByDate[dateKey] = daySet;
        }
        daySet.Clear();
        foreach (var p in ShownDailyProjectGoals)
            daySet.Add(p);

        if (HasShownWeeklyGoalNotification)
            _weeklyGoalsByWeek.Add(weekMonday);
        else
            _weeklyGoalsByWeek.Remove(weekMonday);

        if (!_weeklyProjectGoalsByWeek.TryGetValue(weekMonday, out var weekSet))
        {
            weekSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _weeklyProjectGoalsByWeek[weekMonday] = weekSet;
        }
        weekSet.Clear();
        foreach (var p in ShownWeeklyProjectGoals)
            weekSet.Add(p);
    }

    private void LoadStateForDate(DateTime d, List<TimeEntry> entries)
    {
        var dateKey = d.Date;
        var (weekMonday, _) = CalendarPlanStorage.GetWeekBounds(dateKey);

        // Daily
        if (_dailyGoalsByDate.Contains(dateKey))
        {
            HasShownDailyGoalNotification = true;
        }
        else if (!_dailyProjectGoalsByDate.ContainsKey(dateKey))
        {
            var daySummary = _planStorage.CalculateDailySummary(dateKey, entries);
            HasShownDailyGoalNotification = daySummary.PlannedHours > 0 && daySummary.ActualHours >= daySummary.PlannedHours;
            if (HasShownDailyGoalNotification)
                _dailyGoalsByDate.Add(dateKey);
        }
        else
        {
            HasShownDailyGoalNotification = false;
        }

        ShownDailyProjectGoals.Clear();
        if (_dailyProjectGoalsByDate.TryGetValue(dateKey, out var savedDayProjects))
        {
            foreach (var p in savedDayProjects)
                ShownDailyProjectGoals.Add(p);
        }
        else
        {
            var daySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dayProjects = _planStorage.GetAllPlannedProjectsForDate(dateKey);
            foreach (var proj in dayProjects)
            {
                var projSummary = _planStorage.CalculateProjectDailySummary(proj, dateKey, entries);
                if (projSummary.PlannedHours > 0 && projSummary.ActualHours >= projSummary.PlannedHours)
                {
                    ShownDailyProjectGoals.Add(proj);
                    daySet.Add(proj);
                }
            }
            _dailyProjectGoalsByDate[dateKey] = daySet;
        }

        // Weekly
        if (_weeklyGoalsByWeek.Contains(weekMonday))
        {
            HasShownWeeklyGoalNotification = true;
        }
        else if (!_weeklyProjectGoalsByWeek.ContainsKey(weekMonday))
        {
            var weekSummary = _planStorage.CalculateWeeklySummary(dateKey, entries);
            HasShownWeeklyGoalNotification = weekSummary.TotalPlannedHours > 0 && weekSummary.TotalActualHours >= weekSummary.TotalPlannedHours;
            if (HasShownWeeklyGoalNotification)
                _weeklyGoalsByWeek.Add(weekMonday);
        }
        else
        {
            HasShownWeeklyGoalNotification = false;
        }

        ShownWeeklyProjectGoals.Clear();
        if (_weeklyProjectGoalsByWeek.TryGetValue(weekMonday, out var savedWeekProjects))
        {
            foreach (var p in savedWeekProjects)
                ShownWeeklyProjectGoals.Add(p);
        }
        else
        {
            var weekSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var weekProjects = _planStorage.GetAllPlannedProjectsForWeek(dateKey);
            foreach (var proj in weekProjects)
            {
                var projSummary = _planStorage.CalculateProjectWeeklySummary(proj, dateKey, entries);
                if (projSummary.PlannedHours > 0 && projSummary.ActualHours >= projSummary.PlannedHours)
                {
                    ShownWeeklyProjectGoals.Add(proj);
                    weekSet.Add(proj);
                }
            }
            _weeklyProjectGoalsByWeek[weekMonday] = weekSet;
        }
    }

    public void CheckMilestones(DateTime date, List<TimeEntry> entries, TimeSpan activeTrackingElapsed, string? activeProjectName = null, DateTime? activeStartTime = null)
    {
        var targetDate = activeStartTime.HasValue
            ? _planStorage.GetEffectiveTrackingDate(activeStartTime.Value, date, activeProjectName)
            : date.Date;
        var dateOnly = targetDate.Date;

        if (LastCheckDate.HasValue && dateOnly != LastCheckDate.Value)
        {
            SaveCurrentStateToDate(LastCheckDate.Value);
            LoadStateForDate(dateOnly, entries);
        }
        else if (!LastCheckDate.HasValue)
        {
            LoadStateForDate(dateOnly, entries);
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

                    SaveCurrentStateToDate(dateOnly);

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

                SaveCurrentStateToDate(dateOnly);

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
            SaveCurrentStateToDate(dateOnly);
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

                    SaveCurrentStateToDate(dateOnly);

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

                SaveCurrentStateToDate(dateOnly);

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
        var (weekMonday, _) = CalendarPlanStorage.GetWeekBounds(weeklySummary.WeekStartDate);

        // Weekly flag
        if (weeklySummary.TotalActualHours < weeklySummary.TotalPlannedHours)
        {
            _weeklyGoalsByWeek.Remove(weekMonday);
            if (LastCheckDate.HasValue)
            {
                var (curMon, _) = CalendarPlanStorage.GetWeekBounds(LastCheckDate.Value);
                if (curMon == weekMonday)
                {
                    HasShownWeeklyGoalNotification = false;
                }
            }
            else
            {
                HasShownWeeklyGoalNotification = false;
            }
        }
        else if (weeklySummary.TotalPlannedHours > 0 && weeklySummary.TotalActualHours >= weeklySummary.TotalPlannedHours)
        {
            _weeklyGoalsByWeek.Add(weekMonday);
            if (LastCheckDate.HasValue)
            {
                var (curMon, _) = CalendarPlanStorage.GetWeekBounds(LastCheckDate.Value);
                if (curMon == weekMonday)
                {
                    HasShownWeeklyGoalNotification = true;
                }
            }
            else
            {
                HasShownWeeklyGoalNotification = true;
            }
        }

        // Daily flags
        var allDaysToCheck = new Dictionary<DateTime, DailyGoalSummary>();
        if (weeklySummary.DailySummaries != null)
        {
            foreach (var d in weeklySummary.DailySummaries)
            {
                allDaysToCheck[d.Date.Date] = d;
            }
        }
        allDaysToCheck[todaySummary.Date.Date] = todaySummary;

        var activeDate = LastCheckDate ?? todaySummary.Date.Date;

        foreach (var (dateKey, daySummary) in allDaysToCheck)
        {
            double dayActual = daySummary.ActualHours;
            if (dateKey == activeDate && daySummary != todaySummary)
            {
                dayActual += activeTrackingElapsed.TotalHours;
            }

            if (dayActual < daySummary.PlannedHours)
            {
                _dailyGoalsByDate.Remove(dateKey);
                if (LastCheckDate.HasValue && LastCheckDate.Value == dateKey)
                {
                    HasShownDailyGoalNotification = false;
                }
                else if (!LastCheckDate.HasValue && dateKey == todaySummary.Date.Date)
                {
                    HasShownDailyGoalNotification = false;
                }
            }
            else if (daySummary.PlannedHours > 0 && dayActual >= daySummary.PlannedHours)
            {
                _dailyGoalsByDate.Add(dateKey);
                if (LastCheckDate.HasValue && LastCheckDate.Value == dateKey)
                {
                    HasShownDailyGoalNotification = true;
                }
                else if (!LastCheckDate.HasValue && dateKey == todaySummary.Date.Date)
                {
                    HasShownDailyGoalNotification = true;
                }
            }

            if (entries != null)
            {
                if (!_dailyProjectGoalsByDate.TryGetValue(dateKey, out var dayProjects))
                {
                    if (LastCheckDate.HasValue && LastCheckDate.Value == dateKey && ShownDailyProjectGoals.Count > 0)
                    {
                        dayProjects = new HashSet<string>(ShownDailyProjectGoals, StringComparer.OrdinalIgnoreCase);
                        _dailyProjectGoalsByDate[dateKey] = dayProjects;
                    }
                    else if (!LastCheckDate.HasValue && dateKey == todaySummary.Date.Date && ShownDailyProjectGoals.Count > 0)
                    {
                        dayProjects = new HashSet<string>(ShownDailyProjectGoals, StringComparer.OrdinalIgnoreCase);
                        _dailyProjectGoalsByDate[dateKey] = dayProjects;
                    }
                }

                if (dayProjects != null)
                {
                    var toRemove = new List<string>();
                    foreach (var proj in dayProjects)
                    {
                        var projSummary = _planStorage.CalculateProjectDailySummary(proj, dateKey, entries);
                        double actualHours = projSummary.ActualHours;
                        if (!string.IsNullOrWhiteSpace(activeProjectName) && proj.Equals(activeProjectName, StringComparison.OrdinalIgnoreCase) && dateKey == activeDate)
                        {
                            actualHours += activeTrackingElapsed.TotalHours;
                        }

                        if (projSummary.PlannedHours <= 0 || actualHours < projSummary.PlannedHours)
                        {
                            toRemove.Add(proj);
                        }
                    }
                    foreach (var r in toRemove) dayProjects.Remove(r);

                    if (LastCheckDate.HasValue && LastCheckDate.Value == dateKey)
                    {
                        ShownDailyProjectGoals.Clear();
                        foreach (var p in dayProjects) ShownDailyProjectGoals.Add(p);
                    }
                    else if (!LastCheckDate.HasValue && dateKey == todaySummary.Date.Date)
                    {
                        ShownDailyProjectGoals.Clear();
                        foreach (var p in dayProjects) ShownDailyProjectGoals.Add(p);
                    }
                }
            }
        }

        if (entries != null)
        {
            if (!_weeklyProjectGoalsByWeek.TryGetValue(weekMonday, out var weekProjects))
            {
                if (ShownWeeklyProjectGoals.Count > 0)
                {
                    weekProjects = new HashSet<string>(ShownWeeklyProjectGoals, StringComparer.OrdinalIgnoreCase);
                    _weeklyProjectGoalsByWeek[weekMonday] = weekProjects;
                }
            }

            if (weekProjects != null)
            {
                var toRemove = new List<string>();
                foreach (var proj in weekProjects)
                {
                    var projSummary = _planStorage.CalculateProjectWeeklySummary(proj, weekMonday, entries);
                    double actualHours = projSummary.ActualHours;
                    if (!string.IsNullOrWhiteSpace(activeProjectName) && proj.Equals(activeProjectName, StringComparison.OrdinalIgnoreCase))
                    {
                        actualHours += activeTrackingElapsed.TotalHours;
                    }

                    if (projSummary.PlannedHours <= 0 || actualHours < projSummary.PlannedHours)
                    {
                        toRemove.Add(proj);
                    }
                }
                foreach (var r in toRemove) weekProjects.Remove(r);

                if (LastCheckDate.HasValue)
                {
                    var (curMon, _) = CalendarPlanStorage.GetWeekBounds(LastCheckDate.Value);
                    if (curMon == weekMonday)
                    {
                        ShownWeeklyProjectGoals.Clear();
                        foreach (var p in weekProjects) ShownWeeklyProjectGoals.Add(p);
                    }
                }
                else
                {
                    ShownWeeklyProjectGoals.Clear();
                    foreach (var p in weekProjects) ShownWeeklyProjectGoals.Add(p);
                }
            }
        }
    }

    public void ResetDaily()
    {
        HasShownDailyGoalNotification = false;
        ShownDailyProjectGoals.Clear();
        if (LastCheckDate.HasValue)
        {
            _dailyGoalsByDate.Remove(LastCheckDate.Value.Date);
            _dailyProjectGoalsByDate.Remove(LastCheckDate.Value.Date);
        }
    }

    public void ResetWeekly()
    {
        HasShownWeeklyGoalNotification = false;
        ShownWeeklyProjectGoals.Clear();
        if (LastCheckDate.HasValue)
        {
            var (weekMonday, _) = CalendarPlanStorage.GetWeekBounds(LastCheckDate.Value);
            _weeklyGoalsByWeek.Remove(weekMonday);
            _weeklyProjectGoalsByWeek.Remove(weekMonday);
        }
    }
}
