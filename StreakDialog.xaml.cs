using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class StreakDialog : Window
{
    private readonly StreakService _streakService;
    private readonly List<TimeEntry> _entries;
    private bool _isInitializing = true;
    private DateTime _selectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private bool _showingMonthView = false;

    public DateTime SelectedMonth => _selectedMonth;
    public bool ShowingMonthView => _showingMonthView;

    public StreakDialog() : this(null, null)
    {
    }

    public StreakDialog(List<TimeEntry>? entries = null, StreakService? streakService = null)
    {
        InitializeComponent();

        _streakService = streakService ?? StreakService.Instance;
        _entries = entries ?? new TimeEntryStorage().Load();

        HolidayDatePicker.SelectedDate = DateTime.Today.AddDays(1);
        HolidayEndDatePicker.SelectedDate = DateTime.Today.AddDays(7);
        SickDatePicker.SelectedDate = DateTime.Today;
        SickDatePicker.DisplayDateStart = DateTime.Today.AddDays(-30);
        SickDatePicker.DisplayDateEnd = DateTime.Today;

        LoadProfileToUI();
        RefreshDashboard();
        UpdateHolidayRangePreview();

        _isInitializing = false;
    }

    private void LoadProfileToUI()
    {
        var profile = _streakService.Profile;

        StreakEnabledCheckBox.IsChecked = profile.Enabled;
        AutoApplySickDaysCheckBox.IsChecked = profile.AutoApplySickDays;

        var excluded = profile.ExcludedDays ?? new List<DayOfWeek>();
        ExcludeMondayCheckBox.IsChecked = excluded.Contains(DayOfWeek.Monday);
        ExcludeTuesdayCheckBox.IsChecked = excluded.Contains(DayOfWeek.Tuesday);
        ExcludeWednesdayCheckBox.IsChecked = excluded.Contains(DayOfWeek.Wednesday);
        ExcludeThursdayCheckBox.IsChecked = excluded.Contains(DayOfWeek.Thursday);
        ExcludeFridayCheckBox.IsChecked = excluded.Contains(DayOfWeek.Friday);
        ExcludeSaturdayCheckBox.IsChecked = excluded.Contains(DayOfWeek.Saturday);
        ExcludeSundayCheckBox.IsChecked = excluded.Contains(DayOfWeek.Sunday);

        RefreshHolidaysList();
        RefreshSickDaysList();
    }

    internal void RefreshDashboard()
    {
        var summary = _streakService.CalculateStreak(DateTime.Today, _entries);

        CurrentStreakTextBlock.Text = summary.IsEnabled ? $"🔥 {summary.CurrentStreak} Days" : "🔥 Off";
        LongestStreakTextBlock.Text = $"🏆 {summary.LongestStreak} Days";

        TodayTierTextBlock.Text = summary.TodayTier switch
        {
            StreakTier.Gold => "🥇 Gold Tier",
            StreakTier.Silver => "🥈 Silver Tier",
            StreakTier.Bronze => "🥉 Bronze Tier",
            _ => summary.TodayStatus switch
            {
                StreakDayStatus.SickDay => "💊 Sick Day",
                StreakDayStatus.Holiday => "🌴 Holiday",
                StreakDayStatus.Excluded => "🏖️ Rest Day",
                _ => "⚪ Pending"
            }
        };

        int remainingSick = _streakService.GetRemainingSickDays(DateTime.Today);
        SickDaysRemainingTextBlock.Text = $"🛡️ {remainingSick} Available";
        SickDaysStatusDetailText.Text = $"Available now: {remainingSick} (monthly allowance plus returned sick days)";

        GoldStarsBalanceTextBlock.Text = $"⭐ {summary.GoldStarsBalance} Stars";
        GoldStarsShopBalanceText.Text = $"⭐ {summary.GoldStarsBalance} Gold Stars";
        GoldStarsEarnedSubtitleText.Text = $"Earn 1 Gold Star for each hour worked. Total worked: {summary.TotalHoursWorked:0.#} hrs ({summary.TotalGoldStarsEarned} ⭐ earned, {summary.GoldStarsSpent} ⭐ spent).";

        BuyRestDayButton.IsEnabled = summary.CanBuyRestDay;
        BuyRestDayHintTextBlock.Text = summary.BuyRestDayStatus;

        BronzeCountTextBlock.Text = $"{summary.BronzeDaysCount} Bronze days";
        SilverCountTextBlock.Text = $"{summary.SilverDaysCount} Silver days";
        GoldCountTextBlock.Text = $"{summary.GoldDaysCount} Gold days";

        RefreshTimelineView();
        RefreshSickDaysList();

        FooterStatusTextBlock.Text = summary.IsEnabled
            ? (summary.CurrentStreak > 0
                ? $"🔥 Streak active! {summary.CurrentStreak} day(s) running — keep it up!"
                : "🔥 No active streak today. Save progress to start your streak!")
            : "🔥 Streak tracking is currently disabled.";
    }

    private void BuyRestDayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_streakService.BuyRestDay(_entries, DateTime.Today))
        {
            RefreshDashboard();
            RefreshSickDaysList();
            ThemedMessageBox.Show(this,
                "You successfully purchased 1 Rest Day for 30 Gold Stars! Your rest day protection is active.",
                "⭐ Rest Day Purchased!",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            _streakService.CanBuyRestDay(_entries.Sum(x => x.Duration.TotalHours), DateTime.Today, out var reason);
            ThemedMessageBox.Show(this,
                reason,
                "Unable to Purchase Rest Day",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public void RefreshTimelineView()
    {
        SelectedMonthTopTextBlock.Text = _selectedMonth.ToString("MMM yyyy");

        if (!_showingMonthView)
        {
            var summary = _streakService.CalculateStreak(DateTime.Today, _entries);
            TimelineTitleTextBlock.Text = "📅 Recent 14-Day Streak Timeline";
            ActiveViewBadgeText.Text = "Last 14 Days";
            ActiveViewBadgeBorder.Background = TryFindResource("Theme.Primary") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.SlateBlue;
            Show14DaysTimelineButton.Style = (TryFindResource("PrimaryButton") ?? TryFindResource(typeof(Button))) as Style;
            CycleMonthButton.Content = $"🗓️ Cycle Month ({_selectedMonth:MMM yyyy})";
            CycleMonthButton.Style = (TryFindResource("SecondaryButton") ?? TryFindResource(typeof(Button))) as Style;
            TimelineSummaryStatusText.Text = "Color coded: 🥇 Gold | 🥈 Silver | 🥉 Bronze | 🏖️ Rest | 🌴 Holiday | 💊 Sick Day";

            RecentDaysItemsControl.ItemsSource = null;
            RecentDaysItemsControl.ItemsSource = summary.RecentDays;
        }
        else
        {
            var monthSummary = _streakService.GetMonthStreakSummary(_selectedMonth, _entries, DateTime.Today);
            TimelineTitleTextBlock.Text = $"🗓️ Streak History — {monthSummary.MonthName}";
            ActiveViewBadgeText.Text = $"{monthSummary.ActiveDaysCount} Active Days";
            ActiveViewBadgeBorder.Background = TryFindResource("Theme.Success") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.SeaGreen;
            Show14DaysTimelineButton.Style = (TryFindResource("GhostButton") ?? TryFindResource(typeof(Button))) as Style;
            CycleMonthButton.Content = $"🗓️ Viewing {monthSummary.MonthShortName}";
            CycleMonthButton.Style = (TryFindResource("PrimaryButton") ?? TryFindResource(typeof(Button))) as Style;
            TimelineSummaryStatusText.Text = $"{monthSummary.MonthName}: {monthSummary.SummaryText} — {monthSummary.ExcludedDaysCount} rest, {monthSummary.HolidayDaysCount} holiday, {monthSummary.SickDaysCount} sick, {monthSummary.MissedDaysCount} missed";

            RecentDaysItemsControl.ItemsSource = null;
            RecentDaysItemsControl.ItemsSource = monthSummary.Days;
        }
    }

    public void Show14DaysView()
    {
        _showingMonthView = false;
        RefreshTimelineView();
    }

    public void ShowMonthView(DateTime? month = null)
    {
        _showingMonthView = true;
        if (month.HasValue)
        {
            _selectedMonth = new DateTime(month.Value.Year, month.Value.Month, 1);
        }
        RefreshTimelineView();
    }

    public void CycleToPreviousMonth()
    {
        _showingMonthView = true;
        _selectedMonth = _selectedMonth.AddMonths(-1);
        RefreshTimelineView();
    }

    public void CycleToNextMonth()
    {
        _showingMonthView = true;
        _selectedMonth = _selectedMonth.AddMonths(1);
        RefreshTimelineView();
    }

    private void Show14DaysTimelineButton_Click(object sender, RoutedEventArgs e)
    {
        Show14DaysView();
    }

    private void PrevMonthButton_Click(object sender, RoutedEventArgs e)
    {
        CycleToPreviousMonth();
    }

    private void NextMonthButton_Click(object sender, RoutedEventArgs e)
    {
        CycleToNextMonth();
    }

    private void CycleMonthButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_showingMonthView)
        {
            ShowMonthView();
        }
        else
        {
            CycleToPreviousMonth();
        }
    }

    internal void RefreshHolidaysList()
    {
        var holidays = _streakService.Profile.Holidays?.OrderBy(h => h.Date).ToList() ?? new List<HolidayItem>();
        HolidaysListBox.ItemsSource = null;
        HolidaysListBox.ItemsSource = holidays;
        HolidaysCountTextBlock.Text = $"Scheduled Holidays ({holidays.Count}):";
        ClearAllHolidaysButton.Visibility = holidays.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    internal void RefreshSickDaysList()
    {
        var summary = _streakService.CalculateStreak(DateTime.Today, _entries);

        // 1. Show missed day restoration card if a missed day broke the current streak
        if (summary.MissedStreakDay.HasValue && summary.RemainingSickDaysThisMonth > 0)
        {
            var missedDate = summary.MissedStreakDay.Value;
            MissedDayRestorationCard.Visibility = Visibility.Visible;
            string dayDesc = missedDate.Date == DateTime.Today.AddDays(-1).Date ? "Yesterday" : $"{missedDate:dddd, MMM d}";
            string restoredMsg = summary.PotentialRestoredStreak > 0
                ? $"You missed work on {dayDesc} ({missedDate:yyyy-MM-dd}), which broke your streak. Using a sick day will restore your streak to {summary.PotentialRestoredStreak} day{(summary.PotentialRestoredStreak == 1 ? "" : "s")}!"
                : $"You missed work on {dayDesc} ({missedDate:yyyy-MM-dd}). Using a sick day will protect this day and restore your streak!";

            MissedDayRestorationDetailsText.Text = restoredMsg;
            RestoreStreakSickDayButton.Content = $"💊 Use Sick Day ({missedDate:MMM d})";
            RestoreStreakSickDayButton.Tag = missedDate;
        }
        else
        {
            MissedDayRestorationCard.Visibility = Visibility.Collapsed;
        }

        // 2. Show missed days within the last 30 days
        var missedDays = summary.RecentMissedDays ?? new List<StreakDayEvaluation>();
        MissedDaysListBox.ItemsSource = null;
        MissedDaysListBox.ItemsSource = missedDays;
        MissedDaysListBox.Visibility = missedDays.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        NoMissedDaysTextBlock.Visibility = missedDays.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // 3. Show recorded sick days (within the last 30 days or where work was logged)
        var thirtyDaysAgo = DateTime.Today.AddDays(-30);
        var usedDates = _streakService.Profile.UsedSickDates?
            .Where(d => d.Date >= thirtyDaysAgo || _streakService.EvaluateDate(d, _entries, DateTime.Today, allowAutoApplySickDay: false).Status == StreakDayStatus.Active)
            .OrderByDescending(d => d)
            .ToList() ?? new List<DateTime>();

        UsedSickDatesListBox.ItemsSource = null;
        UsedSickDatesListBox.ItemsSource = usedDates;
    }

    private void StreakEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _streakService.IsEnabled = StreakEnabledCheckBox.IsChecked == true;
        RefreshDashboard();
    }

    private void ExcludedDays_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        var days = new List<DayOfWeek>();
        if (ExcludeMondayCheckBox.IsChecked == true) days.Add(DayOfWeek.Monday);
        if (ExcludeTuesdayCheckBox.IsChecked == true) days.Add(DayOfWeek.Tuesday);
        if (ExcludeWednesdayCheckBox.IsChecked == true) days.Add(DayOfWeek.Wednesday);
        if (ExcludeThursdayCheckBox.IsChecked == true) days.Add(DayOfWeek.Thursday);
        if (ExcludeFridayCheckBox.IsChecked == true) days.Add(DayOfWeek.Friday);
        if (ExcludeSaturdayCheckBox.IsChecked == true) days.Add(DayOfWeek.Saturday);
        if (ExcludeSundayCheckBox.IsChecked == true) days.Add(DayOfWeek.Sunday);

        _streakService.SetExcludedDays(days);
        RefreshDashboard();
    }

    private void AutoApplySickDays_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _streakService.Profile.AutoApplySickDays = AutoApplySickDaysCheckBox.IsChecked == true;
        _streakService.SaveProfile();
        RefreshDashboard();
    }

    private void HolidayDateRangeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        bool isRange = HolidayDateRangeCheckBox.IsChecked == true;
        HolidayStartLabel.Visibility = isRange ? Visibility.Visible : Visibility.Collapsed;
        HolidayEndDateContainer.Visibility = isRange ? Visibility.Visible : Visibility.Collapsed;
        HolidayRangeDurationText.Visibility = isRange ? Visibility.Visible : Visibility.Collapsed;
        HolidayQuickPresetsPanel.Visibility = isRange ? Visibility.Visible : Visibility.Collapsed;

        if (isRange)
        {
            var start = HolidayDatePicker.SelectedDate ?? DateTime.Today.AddDays(1);
            if (HolidayEndDatePicker.SelectedDate == null || HolidayEndDatePicker.SelectedDate < start)
            {
                HolidayEndDatePicker.SelectedDate = start.AddDays(6); // Default to 1 week
            }
        }

        UpdateHolidayRangePreview();
    }

    private void HolidayDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateHolidayRangePreview();
    }

    private void HolidayQuickPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int days))
        {
            HolidayDateRangeCheckBox.IsChecked = true;
            var start = HolidayDatePicker.SelectedDate ?? DateTime.Today.AddDays(1);
            HolidayDatePicker.SelectedDate = start;
            HolidayEndDatePicker.SelectedDate = start.AddDays(Math.Max(0, days - 1));
            UpdateHolidayRangePreview();
        }
    }

    private void UpdateHolidayRangePreview()
    {
        if (HolidayDateRangeCheckBox.IsChecked == true)
        {
            var start = HolidayDatePicker.SelectedDate ?? DateTime.Today.AddDays(1);
            var end = HolidayEndDatePicker.SelectedDate ?? start;
            var startNorm = start <= end ? start : end;
            var endNorm = start <= end ? end : start;

            int daysCount = (endNorm.Date - startNorm.Date).Days + 1;
            HolidayRangeDurationText.Text = $" ({daysCount} day{(daysCount == 1 ? "" : "s")}: {startNorm:ddd, MMM d} – {endNorm:ddd, MMM d})";
            AddHolidayButton.Content = daysCount > 1 ? $"➕ Add Range ({daysCount}d)" : "➕ Add Holiday";
        }
        else
        {
            AddHolidayButton.Content = "➕ Add Holiday";
        }
    }

    private void AddHolidayButton_Click(object sender, RoutedEventArgs e)
    {
        var description = HolidayDescriptionTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            description = "Scheduled Holiday / Vacation";
        }

        if (HolidayDateRangeCheckBox.IsChecked == true)
        {
            var startDate = HolidayDatePicker.SelectedDate ?? DateTime.Today.AddDays(1);
            var endDate = HolidayEndDatePicker.SelectedDate ?? startDate;
            _streakService.AddHolidayRange(startDate, endDate, description);
        }
        else
        {
            var date = HolidayDatePicker.SelectedDate ?? DateTime.Today.AddDays(1);
            _streakService.AddHoliday(date, description);
        }

        HolidayDescriptionTextBox.Clear();
        RefreshHolidaysList();
        RefreshDashboard();
    }

    private void DeleteHolidayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string holidayId)
        {
            _streakService.RemoveHolidayById(holidayId);
            RefreshHolidaysList();
            RefreshDashboard();
        }
    }

    private void ClearAllHolidaysButton_Click(object sender, RoutedEventArgs e)
    {
        int count = _streakService.Profile.Holidays?.Count ?? 0;
        if (count == 0) return;

        var result = ThemedMessageBox.Show(
            this,
            $"Are you sure you want to remove all {count} scheduled holiday day(s)?",
            "Clear Scheduled Holidays",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _streakService.ClearAllHolidays();
            RefreshHolidaysList();
            RefreshDashboard();
        }
    }

    private void RestoreStreakSickDayButton_Click(object sender, RoutedEventArgs e)
    {
        if (RestoreStreakSickDayButton.Tag is DateTime missedDate)
        {
            ApplySickDayForDate(missedDate);
        }
    }

    private void ApplySickDayToDateButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DateTime date)
        {
            ApplySickDayForDate(date);
        }
    }

    private void ApplySickDayButton_Click(object sender, RoutedEventArgs e)
    {
        var date = SickDatePicker.SelectedDate ?? DateTime.Today;
        if (date < DateTime.Today.AddDays(-30) || date > DateTime.Today)
        {
            ThemedMessageBox.Show(
                this,
                "Sick days can only be applied to dates within the last 30 days.",
                "Invalid Date Range",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        ApplySickDayForDate(date);
    }

    private void ApplySickDayForDate(DateTime date)
    {
        if (_streakService.TryUseSickDay(date, _entries, out var reason))
        {
            RefreshDashboard();
            RefreshSickDaysList();
            return;
        }

        ThemedMessageBox.Show(
            this,
            reason,
            "Unable to Apply Sick Day",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void RevokeSickDayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DateTime date)
        {
            if (_streakService.WouldRevokingSickDayBreakStreak(date, _entries, DateTime.Today))
            {
                var result = ThemedMessageBox.Show(
                    this,
                    $"Revoking the sick day for {date:MMMM d, yyyy} will break your current streak. You can apply a sick day to this date again later to restore it. Continue?",
                    "Revoke Sick Day and Break Streak?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            if (_streakService.RemoveSickDay(date))
            {
                RefreshDashboard();
                RefreshSickDaysList();
            }
        }
    }

    private void PreviewCelebrationButton_Click(object sender, RoutedEventArgs e)
    {
        var summary = _streakService.CalculateStreak(DateTime.Today, _entries);
        var previewTier = summary.TodayTier != StreakTier.None ? summary.TodayTier : StreakTier.Gold;
        var dialog = new StreakTierAchievedDialog(previewTier, summary)
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void PreviewMilestoneButton_Click(object sender, RoutedEventArgs e)
    {
        var summary = _streakService.CalculateStreak(DateTime.Today, _entries);
        int milestoneDays = summary.CurrentStreak >= 30 ? summary.CurrentStreak : 30;
        var milestone = StreakMilestoneHelper.GetMilestone(milestoneDays) ?? StreakMilestoneHelper.GetMilestone(30)!;
        var dialog = new StreakMilestoneAchievedDialog(milestone, summary)
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
