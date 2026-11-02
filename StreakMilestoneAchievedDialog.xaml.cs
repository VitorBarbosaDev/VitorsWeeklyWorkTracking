using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class StreakMilestoneAchievedDialog : Window
{
    private readonly DispatcherTimer _autoDismissTimer = new();
    private int _remainingSeconds = 25;
    private bool _isClosing = false;

    public bool RequestOpenStreakDialog { get; private set; } = false;
    public int RemainingSeconds => _remainingSeconds;
    public int AutoDismissSeconds { get; set; } = 25;
    public StreakMilestone Milestone { get; }

    public StreakMilestoneAchievedDialog(StreakMilestone milestone, StreakSummary? streakSummary = null, WorkRestSettings? settings = null)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        Milestone = milestone;
        DateTextBlock.Text = DateTime.Today.ToString("dddd, dd MMMM yyyy");

        int currentStreak = streakSummary?.CurrentStreak ?? milestone.Days;
        int longestStreak = streakSummary?.LongestStreak ?? Math.Max(milestone.Days, currentStreak);
        int totalDays = streakSummary?.TotalActiveDays ?? currentStreak;

        CurrentStreakTextBlock.Text = $"{currentStreak} Day{(currentStreak == 1 ? "" : "s")}";
        LongestStreakTextBlock.Text = $"{longestStreak} Day{(longestStreak == 1 ? "" : "s")}";
        TotalDaysTextBlock.Text = $"{totalDays} Day{(totalDays == 1 ? "" : "s")}";

        ApplyMilestoneVisuals(milestone, currentStreak);

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

    public StreakMilestoneAchievedDialog(int milestoneDays, int longestStreak = 0, int totalDays = 0)
        : this(StreakMilestoneHelper.GetMilestone(milestoneDays) ?? new StreakMilestone
        {
            Days = milestoneDays,
            Title = $"{milestoneDays} Days Streak Milestone",
            Badge = $"🔥 {milestoneDays} DAYS",
            Headline = $"🔥 {milestoneDays} Days of Fiery Consistency!",
            Description = "Incredible streak milestone achieved!",
            Emoji = "👑"
        }, new StreakSummary
        {
            CurrentStreak = milestoneDays,
            LongestStreak = Math.Max(milestoneDays, longestStreak),
            TotalActiveDays = Math.Max(milestoneDays, totalDays)
        })
    {
    }

    private void ApplyMilestoneVisuals(StreakMilestone milestone, int currentStreak)
    {
        MilestoneBadgeTextBlock.Text = milestone.Badge;
        StreakHeadlineTextBlock.Text = milestone.Headline;
        MilestoneSubtitleTextBlock.Text = milestone.Title;
        MilestoneDescriptionTextBlock.Text = milestone.Description;
        CelebrationEmojiTextBlock.Text = milestone.Emoji;

        if (!string.IsNullOrWhiteSpace(milestone.AsciiArt))
        {
            AsciiArtBannerTextBlock.Text = milestone.AsciiArt;
        }
        else
        {
            AsciiArtBannerTextBlock.Text =
                "      👑             🔥             👑      \n" +
                "    (_____)        (🔥 🔥)        (_____)   \n" +
                $"   ( {milestone.Days} DAYS)   ( 🔥 👑 🔥 )   ( LEGEND) \n" +
                "  .-'-----'-.    '--------'    .-'-----'-.  \n" +
                " [  MILESTONE ]               [ UNSTOPPABLE]\n" +
                "  '---------'                  '---------'  ";
        }

        var nextMilestone = StreakMilestoneHelper.GetNextUpcomingMilestone(currentStreak);
        if (nextMilestone != null)
        {
            int daysRemaining = nextMilestone.Days - currentStreak;
            NextMilestoneHintTextBlock.Text = $"Next Milestone: {nextMilestone.Title} ({nextMilestone.Days} Days — {daysRemaining} day{(daysRemaining == 1 ? "" : "s")} to go) {nextMilestone.Emoji}";
        }
        else
        {
            NextMilestoneHintTextBlock.Text = "🌟 You have conquered the pinnacle of streak milestones! Keep your flame burning forever.";
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
        if (KeepBurningButton != null)
        {
            KeepBurningButton.Content = $"Keep Burning 🔥 ({_remainingSeconds}s)";
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

    private void KeepBurningButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoDismissTimer.Stop();
        DialogResult = true;
        Close();
    }

    private void OpenStreakDashboardButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;
        _autoDismissTimer.Stop();
        RequestOpenStreakDialog = true;
        DialogResult = true;
        Close();
    }
}
