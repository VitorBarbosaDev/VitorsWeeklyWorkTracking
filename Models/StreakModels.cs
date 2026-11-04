using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking.Models;

/// <summary>
/// Streak tiers awarded based on daily accomplishments.
/// Bronze: Work done and progress saved.
/// Silver: Completed at least one project goal for the day.
/// Gold: Completed all goals for the day.
/// </summary>
public enum StreakTier
{
    None = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3
}

/// <summary>
/// Status of a specific calendar day in the streak system.
/// </summary>
public enum StreakDayStatus
{
    Missed = 0,
    Active = 1,
    Excluded = 2,
    Holiday = 3,
    SickDay = 4,
    Future = 5
}

/// <summary>
/// Pre-notified holiday or vacation day that preserves user streaks.
/// </summary>
public class HolidayItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;

    public override string ToString() => $"{Date:yyyy-MM-dd}: {Description}";
}

/// <summary>
/// Record of a purchased rest day using Gold Stars currency.
/// </summary>
public class RestDayPurchase
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime PurchasedAt { get; set; } = DateTime.Now;
    public int CostInGoldStars { get; set; } = 30;
}

/// <summary>
/// User configuration and persistent state for the Duolingo-style Streak system.
/// </summary>
public class StreakProfile
{
    /// <summary>
    /// Master toggle to enable or disable the Streak system.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Excluded days of the week that the user does not want to work (e.g., Saturday, Sunday).
    /// Missing work on excluded days will NOT break the user's streak.
    /// </summary>
    public List<DayOfWeek> ExcludedDays { get; set; } = new()
    {
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    };

    /// <summary>
    /// Scheduled / pre-notified holidays or vacations where streak is preserved.
    /// </summary>
    public List<HolidayItem> Holidays { get; set; } = new();

    /// <summary>
    /// Number of sick days / rest days granted per calendar month (default 3, refreshes monthly).
    /// </summary>
    public int MonthlySickDaysAllowance { get; set; } = 3;

    /// <summary>
    /// Maximum number of available rest days a user can hold (max capacity is 3).
    /// </summary>
    public const int MaxRestDaysLimit = 3;

    /// <summary>
    /// Cost in Gold Stars to purchase 1 Rest Day (30 Gold Stars).
    /// </summary>
    public const int RestDayCostInGoldStars = 30;

    /// <summary>
    /// Dates where a sick day / rest day was used to preserve / restore the streak.
    /// </summary>
    public List<DateTime> UsedSickDates { get; set; } = new();

    /// <summary>
    /// Total Gold Stars spent by the user (e.g., buying rest days).
    /// </summary>
    public int GoldStarsSpent { get; set; } = 0;

    /// <summary>
    /// Optional bonus Gold Stars awarded.
    /// </summary>
    public int BonusGoldStars { get; set; } = 0;

    /// <summary>
    /// History of purchased rest days.
    /// </summary>
    public List<RestDayPurchase> RestDayPurchases { get; set; } = new();

    /// <summary>
    /// Whether to automatically apply an available sick day when a workday is missed.
    /// </summary>
    public bool AutoApplySickDays { get; set; } = true;

    /// <summary>
    /// Milestone streak day counts that have already triggered a celebration popup notification.
    /// </summary>
    public List<int> CelebratedMilestoneDays { get; set; } = new();

    /// <summary>
    /// Calculates total Gold Stars earned: 1 Gold Star for each hour worked.
    /// </summary>
    public int CalculateTotalGoldStarsEarned(double totalHoursWorked)
    {
        if (totalHoursWorked <= 0) return BonusGoldStars;
        return (int)Math.Floor(totalHoursWorked) + BonusGoldStars;
    }

    /// <summary>
    /// Gets current available Gold Stars balance (Total earned minus Total spent).
    /// </summary>
    public int GetAvailableGoldStars(double totalHoursWorked)
    {
        int totalEarned = CalculateTotalGoldStarsEarned(totalHoursWorked);
        return Math.Max(0, totalEarned - GoldStarsSpent);
    }

    /// <summary>
    /// Gets the count of rest days purchased in a specific calendar month.
    /// </summary>
    public int GetPurchasedRestDaysCountForMonth(int year, int month)
    {
        if (RestDayPurchases == null) return 0;
        return RestDayPurchases.Count(p => p.PurchasedAt.Year == year && p.PurchasedAt.Month == month);
    }

    public bool IsMilestoneCelebrated(int days)
    {
        return CelebratedMilestoneDays != null && CelebratedMilestoneDays.Contains(days);
    }

    public void MarkMilestoneCelebrated(int days)
    {
        CelebratedMilestoneDays ??= new List<int>();
        if (!CelebratedMilestoneDays.Contains(days))
        {
            CelebratedMilestoneDays.Add(days);
        }
    }

    public int GetUsedSickDaysCountForMonth(int year, int month)
    {
        if (UsedSickDates == null) return 0;
        return UsedSickDates.Count(d => d.Year == year && d.Month == month);
    }

    public int GetRemainingSickDaysForMonth(int year, int month)
    {
        int used = GetUsedSickDaysCountForMonth(year, month);
        int purchased = GetPurchasedRestDaysCountForMonth(year, month);
        int net = MonthlySickDaysAllowance - used + purchased;
        return Math.Clamp(net, 0, MaxRestDaysLimit);
    }

    public bool HasRemainingSickDay(int year, int month)
    {
        return GetRemainingSickDaysForMonth(year, month) > 0;
    }

    /// <summary>
    /// Checks whether the user can buy a rest day.
    /// Returns false if rest days are already maxed out at 3, or if the user has insufficient Gold Stars (&lt; 30).
    /// </summary>
    public bool CanBuyRestDay(double totalHoursWorked, int year, int month, out string reason)
    {
        int remaining = GetRemainingSickDaysForMonth(year, month);
        if (remaining >= MaxRestDaysLimit)
        {
            reason = $"Rest days are already maxed out ({remaining} / {MaxRestDaysLimit}).";
            return false;
        }

        int availableStars = GetAvailableGoldStars(totalHoursWorked);
        if (availableStars < RestDayCostInGoldStars)
        {
            int needed = RestDayCostInGoldStars - availableStars;
            reason = $"Insufficient Gold Stars (⭐ {availableStars} / {RestDayCostInGoldStars}). Work {needed} more hour{(needed == 1 ? "" : "s")} to earn enough Gold Stars.";
            return false;
        }

        reason = $"Purchase 1 Rest Day for ⭐ {RestDayCostInGoldStars} Gold Stars (You have ⭐ {availableStars}).";
        return true;
    }

    /// <summary>
    /// Buys 1 rest day using 30 Gold Stars if available and not maxed out at 3.
    /// </summary>
    public bool BuyRestDay(double totalHoursWorked, DateTime? purchaseDate = null)
    {
        var date = (purchaseDate ?? DateTime.Today).Date;
        if (!CanBuyRestDay(totalHoursWorked, date.Year, date.Month, out _))
        {
            return false;
        }

        GoldStarsSpent += RestDayCostInGoldStars;
        RestDayPurchases ??= new List<RestDayPurchase>();
        RestDayPurchases.Add(new RestDayPurchase
        {
            PurchasedAt = date,
            CostInGoldStars = RestDayCostInGoldStars
        });

        return true;
    }

    public bool IsExcludedDay(DateTime date)
    {
        return ExcludedDays != null && ExcludedDays.Contains(date.DayOfWeek);
    }

    public bool IsHoliday(DateTime date, out string? holidayName)
    {
        if (Holidays == null || Holidays.Count == 0)
        {
            holidayName = null;
            return false;
        }

        var match = Holidays.FirstOrDefault(h => h.Date.Date == date.Date);
        if (match != null)
        {
            holidayName = string.IsNullOrWhiteSpace(match.Description) ? "Holiday" : match.Description;
            return true;
        }

        holidayName = null;
        return false;
    }

    public bool IsSickDay(DateTime date)
    {
        if (UsedSickDates == null || UsedSickDates.Count == 0) return false;
        return UsedSickDates.Any(d => d.Date == date.Date);
    }

    public bool UseSickDay(DateTime date)
    {
        var dateOnly = date.Date;
        UsedSickDates ??= new List<DateTime>();
        if (IsSickDay(dateOnly)) return true;

        if (!HasRemainingSickDay(dateOnly.Year, dateOnly.Month))
        {
            return false;
        }

        UsedSickDates.Add(dateOnly);
        return true;
    }

    public bool RemoveSickDay(DateTime date)
    {
        var dateOnly = date.Date;
        if (UsedSickDates == null) return false;
        return UsedSickDates.RemoveAll(d => d.Date == dateOnly) > 0;
    }
}

/// <summary>
/// Evaluation result for a specific calendar date.
/// </summary>
public class StreakDayEvaluation
{
    public DateTime Date { get; set; }
    public StreakDayStatus Status { get; set; } = StreakDayStatus.Missed;
    public StreakTier Tier { get; set; } = StreakTier.None;
    public double ActualHours { get; set; }
    public double PlannedHours { get; set; }
    public int CompletedProjectGoalsCount { get; set; }
    public int TotalPlannedProjectGoalsCount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool CanApplySickDay { get; set; }
    public bool HasWork { get; set; }

    public string TierEmoji => Tier switch
    {
        StreakTier.Gold => "🥇",
        StreakTier.Silver => "🥈",
        StreakTier.Bronze => "🥉",
        _ => Status switch
        {
            StreakDayStatus.SickDay => "💊",
            StreakDayStatus.Holiday => "🌴",
            StreakDayStatus.Excluded => "🏖️",
            StreakDayStatus.Missed => "❄️",
            _ => "⚪"
        }
    };

    public string TierName => Tier switch
    {
        StreakTier.Gold => "Gold",
        StreakTier.Silver => "Silver",
        StreakTier.Bronze => "Bronze",
        _ => Status switch
        {
            StreakDayStatus.SickDay => "Sick Day (Protected)",
            StreakDayStatus.Holiday => "Holiday (Protected)",
            StreakDayStatus.Excluded => "Rest Day (Excluded)",
            StreakDayStatus.Missed => "Missed Day",
            _ => "Pending"
        }
    };

    public string ShortDescription => Tier switch
    {
        StreakTier.Gold => "🥇 Gold: All goals completed",
        StreakTier.Silver => "🥈 Silver: Project goal completed",
        StreakTier.Bronze => "🥉 Bronze: Work logged and saved",
        _ => Status switch
        {
            StreakDayStatus.SickDay => "💊 Sick Day: Streak preserved",
            StreakDayStatus.Holiday => $"🌴 Holiday ({Reason}): Streak preserved",
            StreakDayStatus.Excluded => "🏖️ Excluded Rest Day: Streak preserved",
            StreakDayStatus.Missed => "❄️ Missed: No work recorded",
            _ => "⚪ Future / In Progress"
        }
    };

    public string TierBadgeText => Tier switch
    {
        StreakTier.Gold => "GOLD",
        StreakTier.Silver => "SILVER",
        StreakTier.Bronze => "BRONZE",
        _ => Status switch
        {
            StreakDayStatus.SickDay => "SICK DAY",
            StreakDayStatus.Holiday => "HOLIDAY",
            StreakDayStatus.Excluded => "REST DAY",
            StreakDayStatus.Missed => "MISSED",
            _ => "PENDING"
        }
    };

    [JsonIgnore]
    public Brush CardBackground => StreakColorHelper.GetEvaluationCardBackground(this);

    [JsonIgnore]
    public Brush CardBorderBrush => StreakColorHelper.GetEvaluationBorderBrush(this);

    [JsonIgnore]
    public Thickness CardBorderThickness => StreakColorHelper.GetEvaluationBorderThickness(this);

    [JsonIgnore]
    public Brush CardForeground => StreakColorHelper.GetEvaluationForeground(this);

    [JsonIgnore]
    public Brush CardMutedForeground => StreakColorHelper.GetEvaluationMutedForeground(this);

    [JsonIgnore]
    public Brush BadgeBackground => StreakColorHelper.GetEvaluationBadgeBackground(this);

    [JsonIgnore]
    public Brush BadgeForeground => StreakColorHelper.GetEvaluationBadgeForeground(this);

    [JsonIgnore]
    public string BorderColorHex => StreakColorHelper.GetEvaluationBorderHex(Tier, Status);

    [JsonIgnore]
    public string BgColorHex => StreakColorHelper.GetEvaluationBgHex(Tier, Status);
}

/// <summary>
/// Monthly streak evaluation and statistics for a given calendar month.
/// </summary>
public class StreakMonthSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime MonthDate => new(Year, Month, 1);
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    public string MonthShortName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    public List<StreakDayEvaluation> Days { get; set; } = new();

    public int TotalDays => Days.Count;
    public int ActiveDaysCount => Days.Count(d => d.Status == StreakDayStatus.Active);
    public int GoldDaysCount => Days.Count(d => d.Tier == StreakTier.Gold);
    public int SilverDaysCount => Days.Count(d => d.Tier == StreakTier.Silver);
    public int BronzeDaysCount => Days.Count(d => d.Tier == StreakTier.Bronze);
    public int SickDaysCount => Days.Count(d => d.Status == StreakDayStatus.SickDay);
    public int HolidayDaysCount => Days.Count(d => d.Status == StreakDayStatus.Holiday);
    public int ExcludedDaysCount => Days.Count(d => d.Status == StreakDayStatus.Excluded);
    public int MissedDaysCount => Days.Count(d => d.Status == StreakDayStatus.Missed && d.Date < DateTime.Today);

    public string SummaryText =>
        $"{ActiveDaysCount} Active Day{(ActiveDaysCount == 1 ? "" : "s")} ({GoldDaysCount} 🥇, {SilverDaysCount} 🥈, {BronzeDaysCount} 🥉)";

    public string DetailedSummaryText =>
        $"{MonthName}: {ActiveDaysCount} active, {ExcludedDaysCount} rest, {HolidayDaysCount} holiday, {SickDaysCount} sick, {MissedDaysCount} missed";
}

/// <summary>
/// Summary metrics for the streak system.
/// </summary>
public class StreakSummary
{
    public bool IsEnabled { get; set; } = true;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalActiveDays { get; set; }
    public int BronzeDaysCount { get; set; }
    public int SilverDaysCount { get; set; }
    public int GoldDaysCount { get; set; }

    public StreakTier TodayTier { get; set; } = StreakTier.None;
    public StreakDayStatus TodayStatus { get; set; } = StreakDayStatus.Missed;
    public int RemainingSickDaysThisMonth { get; set; } = 3;

    public int GoldStarsBalance { get; set; }
    public int TotalGoldStarsEarned { get; set; }
    public int GoldStarsSpent { get; set; }
    public double TotalHoursWorked { get; set; }
    public int AvailableRestDays { get; set; } = 3;
    public int MaxRestDays { get; set; } = 3;
    public bool CanBuyRestDay { get; set; }
    public string BuyRestDayStatus { get; set; } = string.Empty;

    public DateTime? MissedStreakDay { get; set; }
    public int PotentialRestoredStreak { get; set; }
    public List<StreakDayEvaluation> RecentMissedDays { get; set; } = new();

    public List<StreakDayEvaluation> RecentDays { get; set; } = new();

    public string DisplayBadge
    {
        get
        {
            if (!IsEnabled) return "🔥 Streak: Off";
            if (CurrentStreak == 0) return "🔥 0";
            return TodayTier switch
            {
                StreakTier.Gold => $"🔥 {CurrentStreak} 🥇",
                StreakTier.Silver => $"🔥 {CurrentStreak} 🥈",
                StreakTier.Bronze => $"🔥 {CurrentStreak} 🥉",
                _ => $"🔥 {CurrentStreak}"
            };
        }
    }

    public string FormattedStreakHeadline
    {
        get
        {
            if (!IsEnabled) return "Streak tracking is disabled in settings.";
            if (CurrentStreak == 0) return "Start working today to build your streak!";
            if (CurrentStreak == 1) return $"🔥 1 Day Streak! ({TodayTier})";
            return $"🔥 {CurrentStreak} Day Streak! (Best: {LongestStreak})";
        }
    }
}

/// <summary>
/// Big streak milestone award (1 Month, 2 Months, 3 Months, 6 Months, 1 Year, and post-1-year milestones).
/// </summary>
public class StreakMilestone
{
    public int Days { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🔥";
    public string AsciiArt { get; set; } = string.Empty;
}

/// <summary>
/// Helper for calculating and recognizing big streak milestones.
/// </summary>
public static class StreakMilestoneHelper
{
    public static readonly IReadOnlyList<int> StandardMilestoneDays = new List<int>
    {
        30, 60, 90, 180, 365,
        455, 545, 635, 730,
        820, 910, 1000, 1095,
        1185, 1275, 1365, 1460,
        1550, 1640, 1730, 1825
    };

    public static bool IsMilestoneStreak(int days, out StreakMilestone? milestone)
    {
        milestone = GetMilestone(days);
        return milestone != null;
    }

    public static StreakMilestone? GetMilestone(int days)
    {
        if (days <= 0) return null;

        return days switch
        {
            30 => new StreakMilestone
            {
                Days = 30,
                Title = "1 Month Streak",
                Badge = "🏆 1 MONTH MILESTONE",
                Headline = "🔥 30 Days of Unstoppable Consistency!",
                Description = "A full month of daily momentum! You have built a solid foundation of relentless focus.",
                Emoji = "🏆",
                AsciiArt =
                    "      .---.          🔥          .---.      \n" +
                    "     /_____\\       (🔥 🔥)      /_____\\     \n" +
                    "    (  🏆  )    ( 🔥 👑 🔥 )   (  🏆  )    \n" +
                    "   .-'-----'-.   '--------'  .-'-----'-.   \n" +
                    "  [  1 MONTH  ]             [ CONSISTENT]  \n" +
                    "   '---------'               '---------'   "
            },
            60 => new StreakMilestone
            {
                Days = 60,
                Title = "2 Months Streak",
                Badge = "⚡ 2 MONTHS MILESTONE",
                Headline = "🔥 60 Days of High Focus & Habit Mastery!",
                Description = "Two consecutive months of dedication! Your daily habit is forged in fire.",
                Emoji = "⚡",
                AsciiArt =
                    "      /\\             🔥             /\\      \n" +
                    "     /  \\          (🔥 🔥)         /  \\     \n" +
                    "    / ⚡ \\      ( 🔥 👑 🔥 )      / ⚡ \\    \n" +
                    "   /______\\      '--------'      /______\\   \n" +
                    "  [  2 MONTHS ]                 [ POWERFUL ]\n" +
                    "   '---------'                   '---------' "
            },
            90 => new StreakMilestone
            {
                Days = 90,
                Title = "3 Months Streak",
                Badge = "🌟 3 MONTHS (1 QUARTER)",
                Headline = "🔥 90 Days of Unbroken Daily Momentum!",
                Description = "A complete quarter of daily excellence! You're performing at an extraordinary level.",
                Emoji = "🌟",
                AsciiArt =
                    "      .---.          ✨          .---.      \n" +
                    "     /_____\\       (🔥 🔥)      /_____\\     \n" +
                    "    (  🌟  )    ( 🔥 👑 🔥 )   (  🌟  )    \n" +
                    "   .-'-----'-.   '--------'  .-'-----'-.   \n" +
                    "  [  1 QUARTER]             [ 90 DAYS  ]   \n" +
                    "   '---------'               '---------'   "
            },
            180 => new StreakMilestone
            {
                Days = 180,
                Title = "6 Months Streak",
                Badge = "💎 6 MONTHS (HALF YEAR)",
                Headline = "🔥 Half a Year of Burning Dedication!",
                Description = "Six months without missing a beat! An elite display of discipline and stamina.",
                Emoji = "💎",
                AsciiArt =
                    "      /\\             💎             /\\      \n" +
                    "     /  \\          (🔥 🔥)         /  \\     \n" +
                    "    / 💎 \\      ( 🔥 👑 🔥 )      / 💎 \\    \n" +
                    "   /______\\      '--------'      /______\\   \n" +
                    "  [  HALF YEAR]                 [ 180 DAYS ]\n" +
                    "   '---------'                   '---------' "
            },
            365 => new StreakMilestone
            {
                Days = 365,
                Title = "1 Year Streak",
                Badge = "👑 1 YEAR STREAK LEGEND",
                Headline = "🔥 365 Days of Mastery & Pure Legend!",
                Description = "One entire year of uninterrupted consistency! You have achieved true legendary status.",
                Emoji = "👑",
                AsciiArt =
                    "      👑             🔥             👑      \n" +
                    "    (_____)        (🔥 🔥)        (_____)   \n" +
                    "   ( 1 YEAR)    ( 🔥 👑 🔥 )     ( LEGEND)  \n" +
                    "  .-'-----'-.    '--------'    .-'-----'-.  \n" +
                    " [ 365 DAYS! ]                [ UNSTOPPABLE]\n" +
                    "  '---------'                  '---------'  "
            },
            _ => GetPostYearMilestone(days)
        };
    }

    private static StreakMilestone? GetPostYearMilestone(int days)
    {
        if (days < 365) return null;

        // Post-1-year milestones: every 3 months (quarterly: ~90-91d), 6 months (~180-182d), and year (365d)
        if (days == 450 || days == 455 || days == 456)
        {
            return CreatePostYearMilestone(days, "1 Year 3 Months Streak", "🔥 15 MONTHS", "🔥 15 Months of Flaming Momentum!", "Over a year and a quarter of pure consistency!");
        }
        if (days == 540 || days == 545 || days == 548)
        {
            return CreatePostYearMilestone(days, "1 Year 6 Months Streak", "💎 18 MONTHS", "🔥 1.5 Years of Unrivaled Focus!", "A year and a half of burning progress!");
        }
        if (days == 630 || days == 635 || days == 638)
        {
            return CreatePostYearMilestone(days, "1 Year 9 Months Streak", "⚡ 21 MONTHS", "🔥 21 Months of Elite Commitment!", "Approaching two full years of continuous habit mastery!");
        }
        if (days == 730)
        {
            return CreatePostYearMilestone(days, "2 Years Streak", "👑 2 YEARS LEGEND", "🔥 2 Years of Pure Excellence!", "730 days of unmatched discipline and dedication!");
        }
        if (days == 820 || days == 821)
        {
            return CreatePostYearMilestone(days, "2 Years 3 Months Streak", "🔥 27 MONTHS", "🔥 2 Years & 3 Months Streak!", "Still burning strong past 27 months!");
        }
        if (days == 910 || days == 912)
        {
            return CreatePostYearMilestone(days, "2 Years 6 Months Streak", "💎 30 MONTHS", "🔥 2.5 Years of Daily Mastery!", "An incredible 30 months of unbroken consistency!");
        }
        if (days == 1000 || days == 1003)
        {
            return CreatePostYearMilestone(days, "2 Years 9 Months Streak", "⚡ 33 MONTHS", "🔥 2 Years & 9 Months Streak!", "1000+ days of extraordinary willpower!");
        }
        if (days == 1095)
        {
            return CreatePostYearMilestone(days, "3 Years Streak", "👑 3 YEARS LEGEND", "🔥 3 Full Years of Daily Greatness!", "Over a thousand days of legendary focus!");
        }

        int years = days / 365;
        int remainingDays = days % 365;
        if (remainingDays == 0 && years >= 2)
        {
            return CreatePostYearMilestone(days, $"{years} Years Streak", $"👑 {years} YEARS LEGEND", $"🔥 {years} Full Years of Mastery!", $"{days} days of unstoppable daily progress!");
        }

        return null;
    }

    private static StreakMilestone CreatePostYearMilestone(int days, string title, string badge, string headline, string desc)
    {
        return new StreakMilestone
        {
            Days = days,
            Title = title,
            Badge = badge,
            Headline = headline,
            Description = desc,
            Emoji = "👑",
            AsciiArt =
                "      👑             🔥             👑      \n" +
                "    (_____)        (🔥 🔥)        (_____)   \n" +
                $"   ({badge,-8})   ( 🔥 👑 🔥 )   ({days} DAYS) \n" +
                "  .-'-----'-.    '--------'    .-'-----'-.  \n" +
                " [  LEGENDARY ]               [ UNSTOPPABLE]\n" +
                "  '---------'                  '---------'  "
        };
    }

    public static StreakMilestone? GetNextUpcomingMilestone(int currentStreak)
    {
        foreach (var milestoneDays in StandardMilestoneDays)
        {
            if (milestoneDays > currentStreak)
            {
                return GetMilestone(milestoneDays);
            }
        }
        return null;
    }
}
