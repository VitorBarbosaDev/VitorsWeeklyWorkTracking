using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using VitorsWeeklyWorkTracking.Controls;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class AnalyticsWindow : Window
{
    private readonly List<TimeEntry> _allEntries;
    private readonly List<Project> _allProjects;
    private readonly List<ActivityItem> _allActivities;
    private bool _isInitializing = true;

    private List<TimeEntry> _currentFilteredEntries = new();
    private List<ProjectAnalyticsItem> _currentProjectAnalytics = new();
    private List<ActivityAnalyticsItem> _currentActivityAnalytics = new();
    private List<TaskAnalyticsItem> _currentTaskAnalytics = new();
    private List<DailyAnalyticsItem> _currentDailyAnalytics = new();

    public AnalyticsWindow(List<TimeEntry> entries, List<Project> projects, List<ActivityItem>? activities = null)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);

        _allEntries = entries ?? new List<TimeEntry>();
        _allProjects = projects ?? new List<Project>();
        _allActivities = activities ?? new List<ActivityItem>();

        PopulateProjectFilter();
        PopulateActivityFilter();
        SyncChartPreferenceControls();
        SetPeriodWeek(0); // This week

        _isInitializing = false;
        RecalculateAnalytics();
    }

    private void PopulateProjectFilter()
    {
        ProjectFilterComboBox.Items.Clear();
        ProjectFilterComboBox.Items.Add("All Projects");

        var projectNames = _allProjects.Select(p => p.Name)
            .Union(_allEntries.Select(e => e.ProjectName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        foreach (var name in projectNames)
        {
            ProjectFilterComboBox.Items.Add(name);
        }

        ProjectFilterComboBox.SelectedIndex = 0;
    }

    private void PopulateActivityFilter()
    {
        ActivityFilterComboBox.Items.Clear();
        ActivityFilterComboBox.Items.Add("All Activities");

        var activities = (_allActivities.Count > 0 ? _allActivities.Select(a => a.Name) : ActivityStorage.DefaultActivities)
            .Union(_allEntries.Select(e => e.Activity))
            .Where(act => !string.IsNullOrWhiteSpace(act))
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        foreach (var act in activities)
        {
            ActivityFilterComboBox.Items.Add(act);
        }

        ActivityFilterComboBox.SelectedIndex = 0;
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }

    private void SetPeriodWeek(int weekOffset)
    {
        var currentMonday = GetStartOfWeek(DateTime.Today);
        var targetMonday = currentMonday.AddDays(weekOffset * 7);
        var targetSunday = targetMonday.AddDays(6);

        _isInitializing = true;
        StartDatePicker.SelectedDate = targetMonday;
        EndDatePicker.SelectedDate = targetSunday;
        _isInitializing = false;

        if (weekOffset == 0)
        {
            PeriodComboBox.SelectedIndex = 0; // "This Week"
        }
        else if (weekOffset == -1)
        {
            PeriodComboBox.SelectedIndex = 1; // "Last Week"
        }
        else
        {
            PeriodComboBox.SelectedIndex = 5; // "Custom Range"
        }
    }

    private void PeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || PeriodComboBox.SelectedItem is not ComboBoxItem selectedItem)
            return;

        var period = selectedItem.Content.ToString();
        _isInitializing = true;

        var today = DateTime.Today;

        switch (period)
        {
            case "This Week":
                var thisMonday = GetStartOfWeek(today);
                StartDatePicker.SelectedDate = thisMonday;
                EndDatePicker.SelectedDate = thisMonday.AddDays(6);
                break;

            case "Last Week":
                var lastMonday = GetStartOfWeek(today).AddDays(-7);
                StartDatePicker.SelectedDate = lastMonday;
                EndDatePicker.SelectedDate = lastMonday.AddDays(6);
                break;

            case "This Month":
                var firstOfMonth = new DateTime(today.Year, today.Month, 1);
                StartDatePicker.SelectedDate = firstOfMonth;
                EndDatePicker.SelectedDate = firstOfMonth.AddMonths(1).AddDays(-1);
                break;

            case "Last Month":
                var firstOfLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                StartDatePicker.SelectedDate = firstOfLastMonth;
                EndDatePicker.SelectedDate = firstOfLastMonth.AddMonths(1).AddDays(-1);
                break;

            case "All Time":
                StartDatePicker.SelectedDate = _allEntries.Count > 0 ? _allEntries.Min(x => x.StartTime).Date : today.AddMonths(-1);
                EndDatePicker.SelectedDate = today;
                break;

            case "Custom Range":
                break;
        }

        _isInitializing = false;
        RecalculateAnalytics();
    }

    private void PrevWeekButton_Click(object sender, RoutedEventArgs e)
    {
        var currentStart = StartDatePicker.SelectedDate ?? DateTime.Today;
        var currentMonday = GetStartOfWeek(currentStart);
        var targetMonday = currentMonday.AddDays(-7);

        _isInitializing = true;
        StartDatePicker.SelectedDate = targetMonday;
        EndDatePicker.SelectedDate = targetMonday.AddDays(6);
        PeriodComboBox.SelectedIndex = 5; // Custom
        _isInitializing = false;

        RecalculateAnalytics();
    }

    private void NextWeekButton_Click(object sender, RoutedEventArgs e)
    {
        var currentStart = StartDatePicker.SelectedDate ?? DateTime.Today;
        var currentMonday = GetStartOfWeek(currentStart);
        var targetMonday = currentMonday.AddDays(7);

        _isInitializing = true;
        StartDatePicker.SelectedDate = targetMonday;
        EndDatePicker.SelectedDate = targetMonday.AddDays(6);
        PeriodComboBox.SelectedIndex = 5; // Custom
        _isInitializing = false;

        RecalculateAnalytics();
    }

    private void CurrentWeekButton_Click(object sender, RoutedEventArgs e)
    {
        SetPeriodWeek(0);
        RecalculateAnalytics();
    }

    private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        RecalculateAnalytics();
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        RecalculateAnalytics();
    }

    private void SyncChartPreferenceControls()
    {
        if (ChartStyleComboBox != null)
        {
            ChartStyleComboBox.SelectedIndex = PieChartControl.GlobalChartStyle == ChartStyle.Donut ? 0 : 1;
        }

        if (ChartPaletteComboBox != null)
        {
            ChartPaletteComboBox.SelectedIndex = PieChartControl.GlobalChartPalette switch
            {
                ChartPalette.NeonCyber => 1,
                ChartPalette.PastelCandy => 2,
                ChartPalette.OceanBreeze => 3,
                ChartPalette.SunsetWarmth => 4,
                ChartPalette.ForestNature => 5,
                _ => 0
            };
        }
    }

    private void ChartStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || ChartStyleComboBox == null)
            return;

        var style = ChartStyleComboBox.SelectedIndex == 1 ? ChartStyle.SolidPie : ChartStyle.Donut;
        PieChartControl.SetGlobalPreferences(style, PieChartControl.GlobalChartPalette);
    }

    private void ChartPaletteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || ChartPaletteComboBox == null)
            return;

        var palette = ChartPaletteComboBox.SelectedIndex switch
        {
            1 => ChartPalette.NeonCyber,
            2 => ChartPalette.PastelCandy,
            3 => ChartPalette.OceanBreeze,
            4 => ChartPalette.SunsetWarmth,
            5 => ChartPalette.ForestNature,
            _ => ChartPalette.ModernVivid
        };

        PieChartControl.SetGlobalPreferences(PieChartControl.GlobalChartStyle, palette);
    }

    private void RecalculateAnalytics()
    {
        var startDate = StartDatePicker.SelectedDate ?? DateTime.MinValue;
        var endDate = (EndDatePicker.SelectedDate ?? DateTime.MaxValue).Date.AddDays(1).AddTicks(-1);

        var selectedProject = ProjectFilterComboBox.SelectedItem?.ToString();
        var isAllProjects = string.IsNullOrEmpty(selectedProject) || selectedProject == "All Projects";

        var selectedActivity = ActivityFilterComboBox.SelectedItem?.ToString();
        var isAllActivities = string.IsNullOrEmpty(selectedActivity) || selectedActivity == "All Activities";

        DateRangeLabel.Text = $"Showing: {startDate:dd MMM yyyy} - {EndDatePicker.SelectedDate ?? startDate:dd MMM yyyy}";

        _currentFilteredEntries = _allEntries
            .Where(e => e.StartTime >= startDate && e.StartTime <= endDate)
            .Where(e => isAllProjects || e.ProjectName.Equals(selectedProject, StringComparison.OrdinalIgnoreCase))
            .Where(e => isAllActivities || e.Activity.Equals(selectedActivity, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.StartTime)
            .ToList();

        // Totals
        var totalDurationTicks = _currentFilteredEntries.Sum(e => e.Duration.Ticks);
        var totalDuration = TimeSpan.FromTicks(totalDurationTicks);
        var totalSessions = _currentFilteredEntries.Count;

        TotalTimeTextBlock.Text = $"{(int)totalDuration.TotalHours}h {totalDuration.Minutes:D2}m {totalDuration.Seconds:D2}s";
        TotalSessionsTextBlock.Text = $"{totalSessions} session{(totalSessions == 1 ? "" : "s")}";

        var totalDays = Math.Max(1, ((EndDatePicker.SelectedDate ?? startDate).Date - startDate.Date).Days + 1);
        var averageDailySeconds = totalDuration.TotalSeconds / totalDays;
        var avgDuration = TimeSpan.FromSeconds(averageDailySeconds);
        DailyAverageTextBlock.Text = $"{(int)avgDuration.TotalHours}h {avgDuration.Minutes:D2}m / day";

        // Project Breakdown
        _currentProjectAnalytics = _currentFilteredEntries
            .GroupBy(e => string.IsNullOrWhiteSpace(e.ProjectName) ? "(Unassigned)" : e.ProjectName)
            .Select(g =>
            {
                var duration = TimeSpan.FromTicks(g.Sum(x => x.Duration.Ticks));
                var percentage = totalDurationTicks > 0
                    ? ((double)duration.Ticks / totalDurationTicks) * 100.0
                    : 0.0;

                return new ProjectAnalyticsItem
                {
                    ProjectName = g.Key,
                    TotalDuration = duration,
                    Percentage = percentage,
                    EntryCount = g.Count()
                };
            })
            .OrderByDescending(p => p.TotalDuration)
            .ToList();

        ProjectsDataGrid.ItemsSource = null;
        ProjectsDataGrid.ItemsSource = _currentProjectAnalytics;

        var projectPieSlices = _currentProjectAnalytics.Select(p => new PieSliceItem
        {
            Label = p.ProjectName,
            Value = p.TotalDuration.TotalSeconds,
            FormattedValue = p.FormattedDuration,
            Percentage = p.Percentage
        }).ToList();

        ProjectPieChart.SetData(projectPieSlices, "Project Breakdown");
        OverviewProjectPieChart.SetData(projectPieSlices, "Projects Time Share");

        // Top Project
        var topProj = _currentProjectAnalytics.FirstOrDefault();
        if (topProj != null)
        {
            TopProjectTextBlock.Text = $"{topProj.ProjectName} ({topProj.FormattedPercentage})";
        }
        else
        {
            TopProjectTextBlock.Text = "None";
        }

        // Activity Breakdown
        _currentActivityAnalytics = _currentFilteredEntries
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Activity) ? "(Unassigned)" : e.Activity)
            .Select(g =>
            {
                var duration = TimeSpan.FromTicks(g.Sum(x => x.Duration.Ticks));
                var percentage = totalDurationTicks > 0
                    ? ((double)duration.Ticks / totalDurationTicks) * 100.0
                    : 0.0;

                return new ActivityAnalyticsItem
                {
                    Activity = g.Key,
                    TotalDuration = duration,
                    Percentage = percentage,
                    EntryCount = g.Count()
                };
            })
            .OrderByDescending(a => a.TotalDuration)
            .ToList();

        ActivitiesDataGrid.ItemsSource = null;
        ActivitiesDataGrid.ItemsSource = _currentActivityAnalytics;

        var activityPieSlices = _currentActivityAnalytics.Select(a => new PieSliceItem
        {
            Label = a.Activity,
            Value = a.TotalDuration.TotalSeconds,
            FormattedValue = a.FormattedDuration,
            Percentage = a.Percentage
        }).ToList();

        ActivityPieChart.SetData(activityPieSlices, "Activity Breakdown");
        OverviewActivityPieChart.SetData(activityPieSlices, "Activities Time Share");

        // Top Activity
        var topAct = _currentActivityAnalytics.FirstOrDefault();
        if (topAct != null)
        {
            TopActivityTextBlock.Text = $"{topAct.Activity} ({topAct.FormattedPercentage})";
        }
        else
        {
            TopActivityTextBlock.Text = "None";
        }

        // Task Breakdown
        _currentTaskAnalytics = _currentFilteredEntries
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Description) ? "(No Description)" : e.Description.Trim())
            .Select(g =>
            {
                var duration = TimeSpan.FromTicks(g.Sum(x => x.Duration.Ticks));
                var percentage = totalDurationTicks > 0
                    ? ((double)duration.Ticks / totalDurationTicks) * 100.0
                    : 0.0;

                var projects = string.Join(", ", g.Select(x => string.IsNullOrWhiteSpace(x.ProjectName) ? "(Unassigned)" : x.ProjectName).Distinct());
                var activities = string.Join(", ", g.Select(x => string.IsNullOrWhiteSpace(x.Activity) ? "(Unassigned)" : x.Activity).Distinct());

                return new TaskAnalyticsItem
                {
                    Description = g.Key,
                    ProjectName = projects,
                    Activity = activities,
                    TotalDuration = duration,
                    Percentage = percentage,
                    EntryCount = g.Count()
                };
            })
            .OrderByDescending(t => t.TotalDuration)
            .ToList();

        TasksDataGrid.ItemsSource = null;
        TasksDataGrid.ItemsSource = _currentTaskAnalytics;

        var taskPieSlices = _currentTaskAnalytics.Select(t => new PieSliceItem
        {
            Label = t.Description,
            Value = t.TotalDuration.TotalSeconds,
            FormattedValue = t.FormattedDuration,
            Percentage = t.Percentage
        }).ToList();

        TaskPieChart.SetData(taskPieSlices, "Tasks Breakdown");
        OverviewTaskPieChart.SetData(taskPieSlices, "Tasks Time Share");

        // Daily Breakdown
        _currentDailyAnalytics = _currentFilteredEntries
            .GroupBy(e => e.StartTime.Date)
            .Select(g =>
            {
                var duration = TimeSpan.FromTicks(g.Sum(x => x.Duration.Ticks));
                var projectSummaries = g.GroupBy(x => string.IsNullOrWhiteSpace(x.ProjectName) ? "(Unassigned)" : x.ProjectName)
                    .Select(pg => $"{pg.Key} ({(int)pg.Sum(p => p.Duration.TotalHours)}h {TimeSpan.FromTicks(pg.Sum(p => p.Duration.Ticks)).Minutes:D2}m)");

                var activitySummaries = g.GroupBy(x => string.IsNullOrWhiteSpace(x.Activity) ? "(Unassigned)" : x.Activity)
                    .Select(ag => $"{ag.Key} ({(int)ag.Sum(p => p.Duration.TotalHours)}h {TimeSpan.FromTicks(ag.Sum(p => p.Duration.Ticks)).Minutes:D2}m)");

                var taskSummaries = g.Select(x =>
                {
                    var desc = string.IsNullOrWhiteSpace(x.Description) ? "(No description)" : x.Description;
                    var act = string.IsNullOrWhiteSpace(x.Activity) ? "" : $" [{x.Activity}]";
                    return $"{desc}{act} ({(int)x.Duration.TotalHours}h {x.Duration.Minutes:D2}m)";
                });

                return new DailyAnalyticsItem
                {
                    Date = g.Key,
                    TotalDuration = duration,
                    EntryCount = g.Count(),
                    ProjectsSummary = string.Join(", ", projectSummaries),
                    ActivitiesSummary = string.Join(", ", activitySummaries),
                    TasksSummary = string.Join(", ", taskSummaries)
                };
            })
            .OrderByDescending(d => d.Date)
            .ToList();

        DailyDataGrid.ItemsSource = null;
        DailyDataGrid.ItemsSource = _currentDailyAnalytics;

        // Detailed Entries Log
        EntriesDataGrid.ItemsSource = null;
        EntriesDataGrid.ItemsSource = _currentFilteredEntries;
    }

    private void ExportReportButton_Click(object sender, RoutedEventArgs e)
    {
        var timestamp = DateTime.Now.ToString("dd-MM-yyyy");
        var defaultFileName = $"{timestamp}_analytics-report.csv";

        var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            DefaultExt = ".csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            InitialDirectory = AppContext.BaseDirectory
        };

        if (dialog.ShowDialog() != true)
            return;

        var sb = new StringBuilder();

        sb.AppendLine("=== Fun Time Tracking Analytics Report ===");
        sb.AppendLine($"Date Range,{DateRangeLabel.Text}");
        sb.AppendLine($"Project Filter,{ProjectFilterComboBox.SelectedItem}");
        sb.AppendLine($"Activity Filter,{ActivityFilterComboBox.SelectedItem}");
        sb.AppendLine($"Total Time,{TotalTimeTextBlock.Text}");
        sb.AppendLine($"Total Sessions,{TotalSessionsTextBlock.Text}");
        sb.AppendLine($"Daily Average,{DailyAverageTextBlock.Text}");
        sb.AppendLine($"Top Project,{TopProjectTextBlock.Text}");
        sb.AppendLine($"Top Activity,{TopActivityTextBlock.Text}");
        sb.AppendLine();

        sb.AppendLine("=== Project Breakdown ===");
        sb.AppendLine("Project,DurationFormatted,TotalHours,Sessions,SharePercentage");
        foreach (var item in _currentProjectAnalytics)
        {
            sb.AppendLine($"\"{Escape(item.ProjectName)}\",\"{item.FormattedDuration}\",{item.TotalHours:F2},{item.EntryCount},{item.Percentage:F2}%");
        }
        sb.AppendLine();

        sb.AppendLine("=== Activity Breakdown ===");
        sb.AppendLine("Activity,DurationFormatted,TotalHours,Sessions,SharePercentage");
        foreach (var item in _currentActivityAnalytics)
        {
            sb.AppendLine($"\"{Escape(item.Activity)}\",\"{item.FormattedDuration}\",{item.TotalHours:F2},{item.EntryCount},{item.Percentage:F2}%");
        }
        sb.AppendLine();

        sb.AppendLine("=== Task Breakdown ===");
        sb.AppendLine("Task,Project,Activity,DurationFormatted,TotalHours,Sessions,SharePercentage");
        foreach (var item in _currentTaskAnalytics)
        {
            sb.AppendLine($"\"{Escape(item.Description)}\",\"{Escape(item.ProjectName)}\",\"{Escape(item.Activity)}\",\"{item.FormattedDuration}\",{item.TotalHours:F2},{item.EntryCount},{item.Percentage:F2}%");
        }
        sb.AppendLine();

        sb.AppendLine("=== Daily Breakdown ===");
        sb.AppendLine("Date,DayOfWeek,DurationFormatted,TotalHours,Sessions,Projects,Activities,Tasks");
        foreach (var item in _currentDailyAnalytics)
        {
            sb.AppendLine($"\"{item.FormattedDate}\",\"{item.DayOfWeekName}\",\"{item.FormattedDuration}\",{item.TotalHours:F2},{item.EntryCount},\"{Escape(item.ProjectsSummary)}\",\"{Escape(item.ActivitiesSummary)}\",\"{Escape(item.TasksSummary)}\"");
        }
        sb.AppendLine();

        sb.AppendLine("=== Work Log Entries ===");
        sb.AppendLine("Date,Project,Activity,Description,StartTime,EndTime,Duration,Note");
        foreach (var entry in _currentFilteredEntries)
        {
            sb.AppendLine($"\"{entry.StartTime:yyyy-MM-dd}\",\"{Escape(entry.ProjectName)}\",\"{Escape(entry.Activity)}\",\"{Escape(entry.Description)}\",\"{entry.StartTime:HH:mm:ss}\",\"{entry.EndTime:HH:mm:ss}\",\"{entry.Duration:hh\\:mm\\:ss}\",\"{Escape(entry.Note)}\"");
        }

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show($"Analytics report saved to:\n{dialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string Escape(string value)
    {
        return value.Replace("\"", "\"\"");
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
