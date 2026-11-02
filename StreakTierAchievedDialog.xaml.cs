using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class StreakTierAchievedDialog : Window
{
    private readonly DispatcherTimer _autoDismissTimer = new();
    private int _remainingSeconds = 25;
    private bool _isClosing = false;

    public bool RequestOpenStreakDialog { get; private set; } = false;
    public int RemainingSeconds => _remainingSeconds;
    public int AutoDismissSeconds { get; set; } = 25;
    public StreakTier AchievedTier { get; }

    public StreakTierAchievedDialog(StreakTier tier, StreakSummary? streakSummary = null, WorkRestSettings? settings = null)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        AchievedTier = tier;
        DateTextBlock.Text = DateTime.Today.ToString("dddd, dd MMMM yyyy");

        int currentStreak = streakSummary?.CurrentStreak ?? 1;
        int longestStreak = streakSummary?.LongestStreak ?? Math.Max(1, currentStreak);
        double actualHours = streakSummary?.RecentDays?.Find(d => d.Date.Date == DateTime.Today)?.ActualHours ?? 0.0;

        CurrentStreakTextBlock.Text = $"{currentStreak} Day{(currentStreak == 1 ? "" : "s")}";
        LongestStreakTextBlock.Text = $"{longestStreak} Day{(longestStreak == 1 ? "" : "s")}";
        TrackedHoursTextBlock.Text = $"{actualHours:F1} hrs";

        ApplyTierVisuals(tier, currentStreak, actualHours);

        _remainingSeconds = AutoDismissSeconds;
        UpdateAutoDismissText();

        _autoDismissTimer.Interval = TimeSpan.FromSeconds(1);
        _autoDismissTimer.Tick += AutoDismissTimer_Tick;

        Loaded += (s, e) =>
        {
            _remainingSeconds = AutoDismissSeconds;
            UpdateAutoDismissText();
            _autoDismissTimer.Start();
        };

        Unloaded += (s, e) =>
        {
            _autoDismissTimer.Stop();
        };
    }

    public StreakTierAchievedDialog(StreakTier tier, int currentStreak, int longestStreak, double trackedHours = 0.0)
        : this(tier, new StreakSummary
        {
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
            TodayTier = tier
        })
    {
        TrackedHoursTextBlock.Text = $"{trackedHours:F1} hrs";
    }

    private void ApplyTierVisuals(StreakTier tier, int currentStreak, double hours)
    {
        switch (tier)
        {
            case StreakTier.Gold:
                TierBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                TierBadgeTextBlock.Text = "🥇 GOLD TIER ACHIEVED!";
                CelebrationEmojiTextBlock.Text = "👑";
                StreakHeadlineTextBlock.Text = currentStreak > 1
                    ? $"🔥 {currentStreak} Day Gold Streak Crushed!"
                    : "🔥 Gold Tier Unlocked Today!";
                TierSubtitleTextBlock.Text = "Flawless Performance: All goals completed for today!";
                TierSubtitleTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                TierDescriptionTextBlock.Text = "Incredible work! You hit all your project goals and daily target hours.";
                RewardCriteriaTextBlock.Text = "🥇 Gold Tier: Complete all planned tasks & daily hours (or work without scheduled goals)";
                NextTierHintTextBlock.Text = "🌟 You have achieved the highest daily tier! Keep your streak burning tomorrow.";
                AsciiArtBannerTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"));
                AsciiArtBannerTextBlock.Text =
                    "      .---.       \n" +
                    "     /_____\\      \n" +
                    "    (  👑  )   ✨ \n" +
                    "   .-'-----'-.    \n" +
                    "  [  GOLD TIER ]  \n" +
                    "   '---------'    ";
                break;

            case StreakTier.Silver:
                TierBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                TierBadgeTextBlock.Text = "🥈 SILVER TIER ACHIEVED!";
                CelebrationEmojiTextBlock.Text = "🚀";
                StreakHeadlineTextBlock.Text = currentStreak > 1
                    ? $"🔥 {currentStreak} Day Streak & Project Goal Won!"
                    : "🔥 Silver Tier Unlocked Today!";
                TierSubtitleTextBlock.Text = "Goal Conquered: Completed 1+ scheduled project goal!";
                TierSubtitleTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
                TierDescriptionTextBlock.Text = "Great milestone progress! You completed at least one planned project goal.";
                RewardCriteriaTextBlock.Text = "🥈 Silver Tier: Complete at least one scheduled project goal";
                NextTierHintTextBlock.Text = "💡 Complete all remaining goals and match daily hours to achieve Gold! 🥇";
                AsciiArtBannerTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8"));
                AsciiArtBannerTextBlock.Text =
                    "      /\\          \n" +
                    "     /  \\     ✨  \n" +
                    "    / 🥈 \\        \n" +
                    "   /______\\       \n" +
                    "  [ SILVER TIER ] \n" +
                    "   '----------'   ";
                break;

            case StreakTier.Bronze:
            default:
                TierBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                TierBadgeTextBlock.Text = "🥉 BRONZE TIER ACHIEVED!";
                CelebrationEmojiTextBlock.Text = "🔥";
                StreakHeadlineTextBlock.Text = currentStreak > 1
                    ? $"🔥 {currentStreak} Day Streak Burning Strong!"
                    : "🔥 1 Day Streak Started!";
                TierSubtitleTextBlock.Text = "Work Recorded: Daily progress saved successfully!";
                TierSubtitleTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CD7F32"));
                TierDescriptionTextBlock.Text = "Daily habit active! You logged work today and kept your streak alive.";
                RewardCriteriaTextBlock.Text = "🥉 Bronze Tier: Log work and save progress for today";
                NextTierHintTextBlock.Text = "💡 Complete any scheduled project goal to unlock Silver Tier! 🥈";
                AsciiArtBannerTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FB923C"));
                AsciiArtBannerTextBlock.Text =
                    "       (          \n" +
                    "      )\\ )    ✨  \n" +
                    "     (()/(        \n" +
                    "     /(_))_   🥉  \n" +
                    "  [ BRONZE TIER ] \n" +
                    "   '----------'   ";
                break;
        }
    }

    public void TickAutoDismiss()
    {
        if (_isClosing) return;
        _remainingSeconds--;
        UpdateAutoDismissText();
        if (_remainingSeconds <= 0)
        {
            StartFadeOutAndClose();
        }
    }

    public void TriggerAutoDismiss()
    {
        _remainingSeconds = 0;
        StartFadeOutAndClose();
    }

    private void AutoDismissTimer_Tick(object? sender, EventArgs e)
    {
        TickAutoDismiss();
    }

    private void UpdateAutoDismissText()
    {
        if (AutoDismissTextBlock != null)
        {
            AutoDismissTextBlock.Text = $"⏱️ Fading out in {_remainingSeconds}s";
        }
        if (KeepWorkingButton != null)
        {
            KeepWorkingButton.Content = $"⚡ Keep Crushing It ({_remainingSeconds}s)";
        }
    }

    private void StartFadeOutAndClose()
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoDismissTimer.Stop();

        try
        {
            var fadeAnimation = new DoubleAnimation
            {
                From = this.Opacity,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(1.2)
            };
            fadeAnimation.Completed += (s, e) =>
            {
                try { Close(); } catch { }
            };
            this.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        }
        catch
        {
            try { Close(); } catch { }
        }
    }

    private void KeepWorkingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoDismissTimer.Stop();
        Close();
    }

    private void OpenStreakDashboardButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoDismissTimer.Stop();
        RequestOpenStreakDialog = true;
        Close();
    }
}
