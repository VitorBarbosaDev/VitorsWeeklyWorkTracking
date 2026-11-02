using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class WelcomeBackDialog : Window
{
    public bool RequestOpenPlanner { get; private set; } = false;

    public class DisplayGoalItem
    {
        public string ProjectName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public double PlannedHours { get; set; }
        public string FormattedDuration => TimeFormatHelper.FormatHoursWithUnit(PlannedHours);
        public Visibility HasNoteVisibility => string.IsNullOrWhiteSpace(Note) ? Visibility.Collapsed : Visibility.Visible;
    }

    public WelcomeBackDialog(string? userName, List<PlannedWorkItem> todayGoals)
        : this(userName, todayGoals, null, null)
    {
    }

    public WelcomeBackDialog(string? userName, List<PlannedWorkItem> todayGoals, List<ProjectMilestone>? milestones)
        : this(userName, todayGoals, milestones, null)
    {
    }

    public WelcomeBackDialog(string? userName, List<PlannedWorkItem> todayGoals, List<ProjectMilestone>? milestones, StreakSummary? streakSummary)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        SetupGreeting(userName);
        SetupDate();
        SetupStreak(streakSummary);
        SetupMilestones(milestones);
        SetupGoals(todayGoals);
    }

    private void SetupStreak(StreakSummary? summary)
    {
        summary ??= StreakService.Instance.CalculateStreak(DateTime.Today, new TimeEntryStorage().Load());

        if (summary == null || !summary.IsEnabled)
        {
            StreakPanel.Visibility = Visibility.Collapsed;
            return;
        }

        StreakPanel.Visibility = Visibility.Visible;

        int currentStreak = summary.CurrentStreak;
        int longestStreak = summary.LongestStreak;
        var todayTier = summary.TodayTier;
        var todayStatus = summary.TodayStatus;

        StreakCountTextBlock.Text = $"{currentStreak} Day{(currentStreak == 1 ? "" : "s")}";
        StreakBestTextBlock.Text = $"🏆 Best: {longestStreak} Day{(longestStreak == 1 ? "" : "s")}";

        bool isDoneToday = todayTier != StreakTier.None || todayStatus == StreakDayStatus.Active;

        if (isDoneToday)
        {
            ApplyFieryDoneTodayStyling(summary);
        }
        else if (todayStatus == StreakDayStatus.Holiday || todayStatus == StreakDayStatus.SickDay || todayStatus == StreakDayStatus.Excluded)
        {
            ApplyProtectedDayStyling(summary);
        }
        else
        {
            ApplyPendingDayStyling(summary);
        }
    }

    private void ApplyFieryDoneTodayStyling(StreakSummary summary)
    {
        bool isDark = ThemeManager.CurrentTheme != AppTheme.ModernLight;
        Brush fieryBg = isDark
            ? new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#2A1205"),
                (Color)ColorConverter.ConvertFromString("#150A02"),
                new Point(0, 0), new Point(1, 1))
            : new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#FFFBEB"),
                (Color)ColorConverter.ConvertFromString("#FEF3C7"),
                new Point(0, 0), new Point(1, 1));

        StreakPanel.Background = fieryBg;
        StreakPanel.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
        StreakPanel.BorderThickness = new Thickness(1.5);

        StreakIconTextBlock.Text = "🔥";
        StreakTitleTextBlock.Text = "Daily Streak Active";
        StreakTitleTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
        StreakCountTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));

        string badgeText;
        Color badgeColor;

        switch (summary.TodayTier)
        {
            case StreakTier.Gold:
                badgeText = "🥇 Gold Completed";
                badgeColor = (Color)ColorConverter.ConvertFromString("#D97706");
                StreakDetailsTextBlock.Text = "🔥 Gold Tier conquered! All goals completed for today. Streak burning bright!";
                break;
            case StreakTier.Silver:
                badgeText = "🥈 Silver Completed";
                badgeColor = (Color)ColorConverter.ConvertFromString("#0284C7");
                StreakDetailsTextBlock.Text = "🔥 Silver Tier unlocked! Scheduled project goal completed today.";
                break;
            case StreakTier.Bronze:
                badgeText = "🥉 Bronze Saved";
                badgeColor = (Color)ColorConverter.ConvertFromString("#B45309");
                StreakDetailsTextBlock.Text = "🔥 Bronze Tier earned! Work progress saved and streak preserved.";
                break;
            default:
                badgeText = "🔥 Active Today";
                badgeColor = (Color)ColorConverter.ConvertFromString("#EA580C");
                StreakDetailsTextBlock.Text = "🔥 Work recorded! Your streak is burning bright for today.";
                break;
        }

        StreakStatusBadgeBorder.Background = new SolidColorBrush(badgeColor);
        StreakStatusBadgeTextBlock.Text = badgeText;
        StreakStatusBadgeTextBlock.Foreground = Brushes.White;
        StreakDetailsTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDark ? "#FED7AA" : "#9A3412"));
    }

    private void ApplyProtectedDayStyling(StreakSummary summary)
    {
        StreakPanel.Background = (Brush)FindResource("Theme.CardBackgroundAlt");
        StreakPanel.BorderBrush = (Brush)FindResource("Theme.CardBorder");
        StreakPanel.BorderThickness = new Thickness(1);

        StreakIconTextBlock.Text = summary.TodayStatus == StreakDayStatus.SickDay ? "💊" : summary.TodayStatus == StreakDayStatus.Holiday ? "🌴" : "🏖️";
        StreakTitleTextBlock.Text = "Daily Streak Protected";
        StreakTitleTextBlock.Foreground = (Brush)FindResource("Theme.Foreground");
        StreakCountTextBlock.Foreground = (Brush)FindResource("Theme.Foreground");

        StreakStatusBadgeBorder.Background = (Brush)FindResource("Theme.Primary");
        StreakStatusBadgeTextBlock.Text = "🛡️ Protected Today";
        StreakStatusBadgeTextBlock.Foreground = Brushes.White;

        string statusName = summary.TodayStatus switch
        {
            StreakDayStatus.SickDay => "Sick Day",
            StreakDayStatus.Holiday => "Holiday",
            StreakDayStatus.Excluded => "Rest Day",
            _ => "Protected"
        };
        StreakDetailsTextBlock.Text = $"Streak safely preserved today ({statusName}). Enjoy your time!";
        StreakDetailsTextBlock.Foreground = (Brush)FindResource("Theme.ForegroundMuted");
    }

    private void ApplyPendingDayStyling(StreakSummary summary)
    {
        StreakPanel.Background = (Brush)FindResource("Theme.CardBackground");
        StreakPanel.BorderBrush = (Brush)FindResource("Theme.CardBorder");
        StreakPanel.BorderThickness = new Thickness(1);

        StreakIconTextBlock.Text = "🔥";
        StreakTitleTextBlock.Text = "Daily Streak";
        StreakTitleTextBlock.Foreground = (Brush)FindResource("Theme.Foreground");
        StreakCountTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));

        StreakStatusBadgeBorder.Background = (Brush)FindResource("Theme.CardBackgroundAlt");
        StreakStatusBadgeBorder.BorderBrush = (Brush)FindResource("Theme.CardBorder");
        StreakStatusBadgeBorder.BorderThickness = new Thickness(1);
        StreakStatusBadgeTextBlock.Text = "⏳ Pending Today";
        StreakStatusBadgeTextBlock.Foreground = (Brush)FindResource("Theme.ForegroundMuted");

        StreakDetailsTextBlock.Text = summary.CurrentStreak > 0
            ? $"Log and save work today to keep your {summary.CurrentStreak}-day streak alive!"
            : "Log and save work today to start building your daily streak!";
        StreakDetailsTextBlock.Foreground = (Brush)FindResource("Theme.ForegroundMuted");
    }

    private void SetupGreeting(string? userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
        {
            GreetingTextBlock.Text = $"👋 Welcome back, {userName.Trim()}!";
        }
        else
        {
            GreetingTextBlock.Text = "👋 Welcome back!";
        }
    }

    private void SetupDate()
    {
        var now = DateTime.Today;
        DateTextBlock.Text = now.ToString("dddd, dd MMMM yyyy");
    }

    private void SetupMilestones(List<ProjectMilestone>? milestones)
    {
        var activeMilestones = milestones ?? MilestoneService.Instance.GetActiveReminders();
        if (activeMilestones != null && activeMilestones.Count > 0)
        {
            MilestonesPanel.Visibility = Visibility.Visible;
            MilestonesCountTextBlock.Text = $"{activeMilestones.Count} upcoming";
            MilestonesItemsControl.ItemsSource = activeMilestones;
        }
        else
        {
            MilestonesPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void SetupGoals(List<PlannedWorkItem> todayGoals)
    {
        if (todayGoals != null && todayGoals.Count > 0)
        {
            GoalsPanel.Visibility = Visibility.Visible;
            NoGoalsPanel.Visibility = Visibility.Collapsed;

            var items = todayGoals.Select(g => new DisplayGoalItem
            {
                ProjectName = string.IsNullOrWhiteSpace(g.ProjectName) ? "General Work" : g.ProjectName,
                Note = g.Note ?? string.Empty,
                PlannedHours = g.PlannedHours
            }).ToList();

            GoalsItemsControl.ItemsSource = items;

            double totalHours = todayGoals.Sum(g => g.PlannedHours);
            TotalPlannedTextBlock.Text = $"Total: {TimeFormatHelper.FormatHoursWithUnit(totalHours, "0 mins")} ({todayGoals.Count} task{(todayGoals.Count == 1 ? "" : "s")})";

            string[] motivationalQuotes = new[]
            {
                "💡 Focus on one goal at a time and celebrate your progress along the way!",
                "✨ You've got clear goals scheduled. Let's make today a fantastic and productive day!",
                "🎯 Step by step, minute by minute — you're ready to crush today's roadmap!",
                "🚀 Steady pace, clear mind. Let's get things done today!"
            };
            int index = Math.Abs(DateTime.Today.DayOfYear) % motivationalQuotes.Length;
            MotivationTextBlock.Text = motivationalQuotes[index];
        }
        else
        {
            GoalsPanel.Visibility = Visibility.Collapsed;
            NoGoalsPanel.Visibility = Visibility.Visible;
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OpenPlannerButton_Click(object sender, RoutedEventArgs e)
    {
        RequestOpenPlanner = true;
        DialogResult = true;
        Close();
    }

    private void PlanDayButton_Click(object sender, RoutedEventArgs e)
    {
        RequestOpenPlanner = true;
        DialogResult = true;
        Close();
    }
}
