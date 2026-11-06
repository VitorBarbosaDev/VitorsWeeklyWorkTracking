using System;
using System.Collections.Generic;
using System.Linq;
using VitorsWeeklyWorkTracking.Models;

namespace VitorsWeeklyWorkTracking.Services;

/// <summary>
/// Service managing streak calculation, tier evaluation, sick days, holidays, and excluded days.
/// </summary>
public class StreakService
{
    private readonly StreakStorage _storage;
    private readonly CalendarPlanStorage _planStorage;

    public StreakProfile Profile { get; private set; }

    public event Action? StreakUpdated;

    private static StreakService? _instance;
    public static StreakService Instance => _instance ??= new StreakService();

    public static void SetInstanceForTesting(StreakService? instance)
    {
        _instance = instance;
    }

    public static void ResetInstance()
    {
        _instance = null;
    }

    public StreakService(StreakStorage? storage = null, CalendarPlanStorage? planStorage = null)
    {
        _storage = storage ?? StreakStorage.Instance;
        _planStorage = planStorage ?? CalendarPlanStorage.Instance;
        Profile = _storage.Load();

        _storage.StreakDataChanged += () =>
        {
            Profile = _storage.Load();
            StreakUpdated?.Invoke();
        };
    }

    public void Reload()
    {
        Profile = _storage.Load();
        StreakUpdated?.Invoke();
    }

    public void SaveProfile()
    {
        _storage.Save(Profile);
        StreakUpdated?.Invoke();
    }

    public bool IsEnabled
    {
        get => Profile.Enabled;
        set
        {
            if (Profile.Enabled != value)
            {
                Profile.Enabled = value;
                SaveProfile();
            }
        }
    }

    public void SetExcludedDays(IEnumerable<DayOfWeek> days)
    {
        Profile.ExcludedDays = days.Distinct().ToList();
        SaveProfile();
    }

    public void AddHoliday(DateTime date, string description)
    {
        Profile.Holidays ??= new List<HolidayItem>();
        Profile.UsedSickDates ??= new List<DateTime>();
        Profile.RevokedSickDates ??= new List<DateTime>();

        var dateOnly = date.Date;

        // Automatically refund and release any sick day that may have been applied to this date
        Profile.UsedSickDates.RemoveAll(d => d.Date == dateOnly);
        Profile.RevokedSickDates.RemoveAll(d => d.Date == dateOnly);

        var desc = string.IsNullOrWhiteSpace(description) ? "Scheduled Holiday / Vacation" : description.Trim();
        var existing = Profile.Holidays.FirstOrDefault(h => h.Date.Date == dateOnly);
        if (existing != null)
        {
            existing.Description = desc;
        }
        else
        {
            Profile.Holidays.Add(new HolidayItem
            {
                Date = dateOnly,
                Description = desc
            });
        }
        SaveProfile();
    }

    /// <summary>
    /// Adds or updates pre-notified holidays for all dates within the specified date range (inclusive).
    /// </summary>
    public int AddHolidayRange(DateTime startDate, DateTime endDate, string description)
    {
        Profile.Holidays ??= new List<HolidayItem>();
        Profile.UsedSickDates ??= new List<DateTime>();
        Profile.RevokedSickDates ??= new List<DateTime>();

        var start = startDate.Date <= endDate.Date ? startDate.Date : endDate.Date;
        var end = startDate.Date <= endDate.Date ? endDate.Date : startDate.Date;
        var desc = string.IsNullOrWhiteSpace(description) ? "Scheduled Holiday / Vacation" : description.Trim();

        int affectedCount = 0;
        for (var current = start; current <= end; current = current.AddDays(1))
        {
            Profile.UsedSickDates.RemoveAll(d => d.Date == current);
            Profile.RevokedSickDates.RemoveAll(d => d.Date == current);

            var existing = Profile.Holidays.FirstOrDefault(h => h.Date.Date == current);
            if (existing != null)
            {
                existing.Description = desc;
            }
            else
            {
                Profile.Holidays.Add(new HolidayItem
                {
                    Date = current,
                    Description = desc
                });
            }
            affectedCount++;
        }

        SaveProfile();
        return affectedCount;
    }

    public bool RemoveHoliday(DateTime date)
    {
        var dateOnly = date.Date;
        if (Profile.Holidays == null) return false;
        var removed = Profile.Holidays.RemoveAll(h => h.Date.Date == dateOnly) > 0;
        if (removed)
        {
            SaveProfile();
        }
        return removed;
    }

    /// <summary>
    /// Removes all pre-notified holidays within the specified date range (inclusive).
    /// </summary>
    public int RemoveHolidayRange(DateTime startDate, DateTime endDate)
    {
        if (Profile.Holidays == null || Profile.Holidays.Count == 0) return 0;
        var start = startDate.Date <= endDate.Date ? startDate.Date : endDate.Date;
        var end = startDate.Date <= endDate.Date ? endDate.Date : startDate.Date;

        int removed = Profile.Holidays.RemoveAll(h => h.Date.Date >= start && h.Date.Date <= end);
        if (removed > 0)
        {
            SaveProfile();
        }
        return removed;
    }

    public void ClearAllHolidays()
    {
        if (Profile.Holidays != null && Profile.Holidays.Count > 0)
        {
            Profile.Holidays.Clear();
            SaveProfile();
        }
    }

    public bool RemoveHolidayById(string id)
    {
        if (Profile.Holidays == null) return false;
        var removed = Profile.Holidays.RemoveAll(h => h.Id == id) > 0;
        if (removed)
        {
            SaveProfile();
        }
        return removed;
    }

    public bool UseSickDay(DateTime date)
    {
        var dateOnly = date.Date;
        if (Profile.UseSickDay(dateOnly))
        {
            SaveProfile();
            return true;
        }
        return false;
    }

    public bool TryUseSickDay(DateTime date, List<TimeEntry>? allEntries, out string reason)
    {
        var dateOnly = date.Date;
        if (Profile.IsSickDay(dateOnly))
        {
            reason = string.Empty;
            return true;
        }

        if (Profile.IsExcludedDay(dateOnly))
        {
            reason = $"{dateOnly:MMMM d, yyyy} is marked as a regular rest day, so a sick day can't be applied.";
            return false;
        }

        if (Profile.IsHoliday(dateOnly, out var holidayName))
        {
            reason = $"{dateOnly:MMMM d, yyyy} is marked as a holiday{(string.IsNullOrWhiteSpace(holidayName) ? "" : $" ({holidayName})")}, so a sick day can't be applied.";
            return false;
        }

        var daySummary = _planStorage.CalculateDailySummary(dateOnly, allEntries ?? new List<TimeEntry>());
        if (daySummary.ActualHours > 0 || daySummary.EntryCount > 0)
        {
            reason = $"Work is already recorded on {dateOnly:MMMM d, yyyy}, so a sick day can't be applied.";
            return false;
        }

        if (!Profile.HasRemainingSickDay(dateOnly.Year, dateOnly.Month))
        {
            reason = $"You have no sick days available to apply to {dateOnly:MMMM yyyy}.";
            return false;
        }

        if (!UseSickDay(dateOnly))
        {
            reason = $"A sick day could not be applied to {dateOnly:MMMM d, yyyy}. Please check your available balance and try again.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool RemoveSickDay(DateTime date)
    {
        var dateOnly = date.Date;
        if (Profile.RemoveSickDay(dateOnly))
        {
            SaveProfile();
            return true;
        }
        return false;
    }

    public int GetRemainingSickDays(DateTime date)
    {
        return Profile.GetRemainingSickDaysForMonth(date.Year, date.Month);
    }

    public int GetAvailableGoldStars(double totalHoursWorked)
    {
        return Profile.GetAvailableGoldStars(totalHoursWorked);
    }

    public int GetTotalGoldStarsEarned(double totalHoursWorked)
    {
        return Profile.CalculateTotalGoldStarsEarned(totalHoursWorked);
    }

    public bool CanBuyRestDay(double totalHoursWorked, DateTime? date, out string reason)
    {
        var targetDate = (date ?? DateTime.Today).Date;
        return Profile.CanBuyRestDay(totalHoursWorked, targetDate.Year, targetDate.Month, out reason);
    }

    public bool BuyRestDay(double totalHoursWorked, DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.Today).Date;
        if (Profile.BuyRestDay(totalHoursWorked, targetDate))
        {
            SaveProfile();
            return true;
        }
        return false;
    }

    public bool BuyRestDay(List<TimeEntry>? entries, DateTime? date = null)
    {
        var entriesList = entries ?? new TimeEntryStorage().Load();
        double totalHours = entriesList.Sum(e => e.Duration.TotalHours);
        return BuyRestDay(totalHours, date);
    }

    /// <summary>
    /// Checks if the current streak matches a big milestone that has not yet been celebrated via popup.
    /// </summary>
    public StreakMilestone? CheckForUncelebratedMilestone(StreakSummary summary)
    {
        if (!Profile.Enabled || summary.CurrentStreak <= 0) return null;

        if (StreakMilestoneHelper.IsMilestoneStreak(summary.CurrentStreak, out var milestone) && milestone != null)
        {
            if (!Profile.IsMilestoneCelebrated(milestone.Days))
            {
                return milestone;
            }
        }

        return null;
    }

    /// <summary>
    /// Records that a big streak milestone has been acknowledged and celebrated.
    /// </summary>
    public void MarkMilestoneCelebrated(int milestoneDays)
    {
        Profile.MarkMilestoneCelebrated(milestoneDays);
        SaveProfile();
    }

    /// <summary>
    /// Evaluates a specific date for streak tier and day status.
    /// Bronze: Work done and progress saved when scheduled goals were not met.
    /// Silver: Completed at least one planned project goal.
    /// Gold: Completed all goals for the day, or worked and saved progress when no goals were planned.
    /// </summary>
    public StreakDayEvaluation EvaluateDate(DateTime date, List<TimeEntry>? allEntries, DateTime? todayOverride = null, bool allowAutoApplySickDay = true)
    {
        var dateOnly = date.Date;
        var today = (todayOverride ?? DateTime.Today).Date;
        var entriesList = allEntries ?? new List<TimeEntry>();

        var daySummary = _planStorage.CalculateDailySummary(dateOnly, entriesList);
        var dayPlans = _planStorage.GetPlansForDate(dateOnly);
        var plannedProjects = _planStorage.GetAllPlannedProjectsForDate(dateOnly);

        bool hasWork = daySummary.ActualHours > 0 || daySummary.EntryCount > 0;

        if (hasWork)
        {
            int completedProjectsCount = 0;
            int totalPlannedProjectsCount = plannedProjects.Count;

            foreach (var proj in plannedProjects)
            {
                var projSummary = _planStorage.CalculateProjectDailySummary(proj, dateOnly, entriesList);
                if (projSummary.PlannedHours > 0 && projSummary.ActualHours >= projSummary.PlannedHours)
                {
                    completedProjectsCount++;
                }
            }

            bool hasGoals = dayPlans.Count > 0 || daySummary.PlannedHours > 0 || totalPlannedProjectsCount > 0;
            StreakTier tier;
            string reason;

            if (!hasGoals)
            {
                // If they have no projects or anything planned at all for today, give them Gold because they are not using the planning system
                tier = StreakTier.Gold;
                reason = "Worked and saved progress (No planned goals scheduled)";
            }
            else
            {
                bool allProjectGoalsMet = totalPlannedProjectsCount > 0 && completedProjectsCount == totalPlannedProjectsCount;
                bool dailyPlannedHoursMet = daySummary.PlannedHours > 0 && daySummary.ActualHours >= daySummary.PlannedHours;
                bool allTasksCompleted = dayPlans.Count > 0 && dayPlans.All(p => p.IsCompleted);

                if ((allProjectGoalsMet && (daySummary.PlannedHours <= 0 || dailyPlannedHoursMet)) ||
                    (dailyPlannedHoursMet && (totalPlannedProjectsCount == 0 || allProjectGoalsMet)) ||
                    (allTasksCompleted && dayPlans.Count > 0 && dailyPlannedHoursMet))
                {
                    tier = StreakTier.Gold;
                    reason = "Completed all scheduled goals for the day";
                }
                else if (completedProjectsCount >= 1 || (dayPlans.Count > 0 && dayPlans.Any(p => p.IsCompleted)))
                {
                    tier = StreakTier.Silver;
                    reason = $"Completed {completedProjectsCount} of {totalPlannedProjectsCount} project goal(s)";
                }
                else
                {
                    tier = StreakTier.Bronze;
                    reason = "Worked and saved progress";
                }
            }

            return new StreakDayEvaluation
            {
                Date = dateOnly,
                Status = StreakDayStatus.Active,
                Tier = tier,
                ActualHours = daySummary.ActualHours,
                PlannedHours = daySummary.PlannedHours,
                CompletedProjectGoalsCount = completedProjectsCount,
                TotalPlannedProjectGoalsCount = totalPlannedProjectsCount,
                Reason = reason,
                Details = $"{daySummary.ActualHours:0.##} hrs worked, {completedProjectsCount}/{totalPlannedProjectsCount} project goals met",
                HasWork = hasWork
            };
        }

        // No work recorded on this date:
        if (dateOnly > today)
        {
            return new StreakDayEvaluation
            {
                Date = dateOnly,
                Status = StreakDayStatus.Future,
                Tier = StreakTier.None,
                Reason = "Future date",
                HasWork = false
            };
        }

        if (Profile.IsExcludedDay(dateOnly))
        {
            return new StreakDayEvaluation
            {
                Date = dateOnly,
                Status = StreakDayStatus.Excluded,
                Tier = StreakTier.None,
                Reason = $"Rest day ({dateOnly.DayOfWeek})",
                HasWork = false
            };
        }

        if (Profile.IsHoliday(dateOnly, out var holidayName))
        {
            return new StreakDayEvaluation
            {
                Date = dateOnly,
                Status = StreakDayStatus.Holiday,
                Tier = StreakTier.None,
                Reason = holidayName ?? "Pre-notified Holiday",
                HasWork = false
            };
        }

        if (Profile.IsSickDay(dateOnly))
        {
            return new StreakDayEvaluation
            {
                Date = dateOnly,
                Status = StreakDayStatus.SickDay,
                Tier = StreakTier.None,
                Reason = "Sick Day (Protected)",
                HasWork = false
            };
        }

        // Auto apply sick day if configured and within monthly limit
        if (allowAutoApplySickDay &&
            Profile.AutoApplySickDays &&
            dateOnly < today &&
            !(Profile.RevokedSickDates?.Any(d => d.Date == dateOnly) ?? false) &&
            Profile.HasMonthlySickDay(dateOnly.Year, dateOnly.Month))
        {
            Profile.UseSickDay(dateOnly, allowReturnedCredit: false);
            SaveProfile();

            return new StreakDayEvaluation
            {
                Date = dateOnly,
                Status = StreakDayStatus.SickDay,
                Tier = StreakTier.None,
                Reason = "Sick Day (Auto-applied)",
                HasWork = false
            };
        }

        return new StreakDayEvaluation
        {
            Date = dateOnly,
            Status = dateOnly == today ? StreakDayStatus.Future : StreakDayStatus.Missed,
            Tier = StreakTier.None,
            Reason = dateOnly == today ? "Pending today's work" : "Missed workday",
            HasWork = false
        };
    }

    public bool WouldRevokingSickDayBreakStreak(DateTime date, List<TimeEntry>? allEntries, DateTime currentDate)
    {
        var dateOnly = date.Date;
        var today = currentDate.Date;
        if (dateOnly >= today || !Profile.IsSickDay(dateOnly))
        {
            return false;
        }

        var entries = allEntries ?? new List<TimeEntry>();
        var cursor = today.AddDays(-1);
        var safetyLimit = today.AddYears(-5);

        while (cursor >= dateOnly && cursor >= safetyLimit)
        {
            var evaluation = EvaluateDate(cursor, entries, today, allowAutoApplySickDay: false);
            if (cursor == dateOnly)
            {
                return evaluation.Status == StreakDayStatus.SickDay;
            }

            if (evaluation.Status != StreakDayStatus.Active &&
                evaluation.Status != StreakDayStatus.Excluded &&
                evaluation.Status != StreakDayStatus.Holiday &&
                evaluation.Status != StreakDayStatus.SickDay)
            {
                return false;
            }

            cursor = cursor.AddDays(-1);
        }

        return false;
    }

    /// <summary>
    /// Calculates the full Streak summary including current streak, longest streak, tier counts, and recent history.
    /// </summary>
    public StreakSummary CalculateStreak(DateTime currentDate, List<TimeEntry>? allEntries)
    {
        var today = currentDate.Date;
        var entriesList = allEntries ?? new List<TimeEntry>();
        double totalHours = entriesList.Sum(e => e.Duration.TotalHours);
        int totalGoldStarsEarned = Profile.CalculateTotalGoldStarsEarned(totalHours);
        int goldStarsBalance = Profile.GetAvailableGoldStars(totalHours);
        int availableRestDays = Profile.GetRemainingSickDaysForMonth(today.Year, today.Month);
        bool canBuyRestDay = Profile.CanBuyRestDay(totalHours, today.Year, today.Month, out var buyStatusReason);

        var summary = new StreakSummary
        {
            IsEnabled = Profile.Enabled,
            RemainingSickDaysThisMonth = availableRestDays,
            AvailableRestDays = availableRestDays,
            MaxRestDays = StreakProfile.MaxRestDaysLimit,
            TotalHoursWorked = totalHours,
            TotalGoldStarsEarned = totalGoldStarsEarned,
            GoldStarsSpent = Profile.GoldStarsSpent,
            GoldStarsBalance = goldStarsBalance,
            CanBuyRestDay = canBuyRestDay,
            BuyRestDayStatus = buyStatusReason
        };

        if (!Profile.Enabled)
        {
            return summary;
        }

        var todayEval = EvaluateDate(today, entriesList, today);
        summary.TodayTier = todayEval.Tier;
        summary.TodayStatus = todayEval.Status;

        // 1. Calculate Current Streak
        int currentStreak = 0;
        DateTime cursor;

        if (todayEval.Status == StreakDayStatus.Active)
        {
            currentStreak = 1;
            cursor = today.AddDays(-1);
        }
        else if (todayEval.Status == StreakDayStatus.Excluded || todayEval.Status == StreakDayStatus.Holiday || todayEval.Status == StreakDayStatus.SickDay)
        {
            // Today is protected, streak is alive from yesterday
            cursor = today.AddDays(-1);
        }
        else
        {
            // Today has no work yet (pending)
            cursor = today.AddDays(-1);
        }

        // Step backwards from cursor
        DateTime? firstMissedStreakDate = null;
        while (true)
        {
            var eval = EvaluateDate(cursor, entriesList, today);
            if (eval.Status == StreakDayStatus.Active)
            {
                currentStreak++;
                cursor = cursor.AddDays(-1);
            }
            else if (eval.Status == StreakDayStatus.Excluded || eval.Status == StreakDayStatus.Holiday || eval.Status == StreakDayStatus.SickDay)
            {
                // Streak is preserved over rest days / holidays / sick days
                cursor = cursor.AddDays(-1);
            }
            else
            {
                // Missed day -> Streak breaks
                firstMissedStreakDate = cursor;
                break;
            }

            // Safety limit to avoid infinite loops if dates go way back
            if (cursor < today.AddYears(-5))
            {
                break;
            }
        }

        summary.CurrentStreak = currentStreak;

        // Calculate potential restored streak if the breaking missed day is restored with a sick day
        if (firstMissedStreakDate.HasValue && firstMissedStreakDate.Value >= today.AddDays(-30))
        {
            int potentialStreak = currentStreak;
            var testCursor = firstMissedStreakDate.Value.AddDays(-1);

            while (testCursor >= today.AddYears(-5))
            {
                var testEval = EvaluateDate(testCursor, entriesList, today);
                if (testEval.Status == StreakDayStatus.Active)
                {
                    potentialStreak++;
                    testCursor = testCursor.AddDays(-1);
                }
                else if (testEval.Status == StreakDayStatus.Excluded || testEval.Status == StreakDayStatus.Holiday || testEval.Status == StreakDayStatus.SickDay)
                {
                    testCursor = testCursor.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            if (potentialStreak > currentStreak)
            {
                summary.MissedStreakDay = firstMissedStreakDate.Value;
                summary.PotentialRestoredStreak = potentialStreak;
            }
        }

        // 2. Calculate Longest Streak and Historical Tier Counts
        var allDates = new HashSet<DateTime>();
        if (allEntries != null)
        {
            foreach (var e in allEntries)
            {
                var effDate = _planStorage.GetEffectiveEntryDate(e);
                allDates.Add(effDate.Date);
            }
        }

        var allPlans = _planStorage.Load();
        if (allPlans != null)
        {
            foreach (var p in allPlans)
            {
                allDates.Add(p.Date.Date);
            }
        }

        if (Profile.UsedSickDates != null)
        {
            foreach (var d in Profile.UsedSickDates)
            {
                allDates.Add(d.Date);
            }
        }

        if (Profile.Holidays != null)
        {
            foreach (var h in Profile.Holidays)
            {
                allDates.Add(h.Date.Date);
            }
        }

        int bronzeCount = 0;
        int silverCount = 0;
        int goldCount = 0;
        int totalActiveDays = 0;
        int maxStreak = 0;

        if (allDates.Count > 0)
        {
            var earliestDate = allDates.Min();
            if (earliestDate > today) earliestDate = today;

            int runActive = 0;

            for (var d = earliestDate; d <= today; d = d.AddDays(1))
            {
                var eval = EvaluateDate(d, entriesList, today);

                if (eval.Status == StreakDayStatus.Active)
                {
                    runActive++;
                    totalActiveDays++;

                    if (eval.Tier == StreakTier.Gold) goldCount++;
                    else if (eval.Tier == StreakTier.Silver) silverCount++;
                    else if (eval.Tier == StreakTier.Bronze) bronzeCount++;

                    if (runActive > maxStreak)
                    {
                        maxStreak = runActive;
                    }
                }
                else if (eval.Status == StreakDayStatus.Excluded || eval.Status == StreakDayStatus.Holiday || eval.Status == StreakDayStatus.SickDay)
                {
                    // Preserves streak chain
                }
                else
                {
                    // Missed day
                    if (d < today)
                    {
                        runActive = 0;
                    }
                }
            }
        }

        summary.LongestStreak = Math.Max(maxStreak, currentStreak);
        summary.TotalActiveDays = totalActiveDays;
        summary.BronzeDaysCount = bronzeCount;
        summary.SilverDaysCount = silverCount;
        summary.GoldDaysCount = goldCount;

        // 3. Build Recent Days (Past 14 days) and Recent Missed Days (Past 30 days)
        var recent = new List<StreakDayEvaluation>();
        var recentMissed = new List<StreakDayEvaluation>();

        for (int i = 13; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var eval = EvaluateDate(day, entriesList, today);
            eval.CanApplySickDay = (eval.Status == StreakDayStatus.Missed && day < today && summary.RemainingSickDaysThisMonth > 0);
            recent.Add(eval);
        }
        summary.RecentDays = recent;

        for (int i = 29; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var eval = EvaluateDate(day, entriesList, today);
            eval.CanApplySickDay = (eval.Status == StreakDayStatus.Missed && day < today && summary.RemainingSickDaysThisMonth > 0);
            if (eval.Status == StreakDayStatus.Missed && day < today)
            {
                recentMissed.Add(eval);
            }
        }
        summary.RecentMissedDays = recentMissed;

        return summary;
    }

    /// <summary>
    /// Gets all missed workdays within the specified number of days back (defaults to 30 days).
    /// </summary>
    public List<StreakDayEvaluation> GetRecentMissedDays(DateTime currentDate, List<TimeEntry>? allEntries, int daysBack = 30)
    {
        var summary = CalculateStreak(currentDate, allEntries);
        return summary.RecentMissedDays;
    }

    /// <summary>
    /// Evaluates and generates a complete streak summary for all days of a specific calendar month.
    /// </summary>
    public StreakMonthSummary GetMonthStreakSummary(DateTime monthDate, List<TimeEntry>? allEntries, DateTime? currentDate = null)
    {
        var today = (currentDate ?? DateTime.Today).Date;
        var entriesList = allEntries ?? new List<TimeEntry>();
        int year = monthDate.Year;
        int month = monthDate.Month;
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int remainingSick = GetRemainingSickDays(today);

        var days = new List<StreakDayEvaluation>(daysInMonth);
        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            var eval = EvaluateDate(date, entriesList, today);
            eval.CanApplySickDay = (eval.Status == StreakDayStatus.Missed && date < today && remainingSick > 0);
            days.Add(eval);
        }

        return new StreakMonthSummary
        {
            Year = year,
            Month = month,
            Days = days
        };
    }

    /// <summary>
    /// Gets all day evaluations for a specific calendar month.
    /// </summary>
    public List<StreakDayEvaluation> GetMonthDays(DateTime monthDate, List<TimeEntry>? allEntries, DateTime? currentDate = null)
    {
        return GetMonthStreakSummary(monthDate, allEntries, currentDate).Days;
    }
}
