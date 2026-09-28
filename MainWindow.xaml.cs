using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Microsoft.Win32;
using System.IO;

namespace VitorsWeeklyWorkTracking;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly TimeEntryStorage _storage = new();
    private readonly ProjectStorage _projectStorage = new();
    private readonly ActivityStorage _activityStorage = new();
    private List<TimeEntry> _entries = new();
    private List<Project> _projects = new();
    private List<ActivityItem> _activities = new();
    private readonly System.Windows.Threading.DispatcherTimer _timer = new();

    private DateTime? _currentStartTime;
    private DateTime? _pendingStartTime;
    private DateTime? _pendingEndTime;
    private List<TimeEntry> _filteredEntries = new();
    private bool _isInitializing = true;

    public MainWindow()
    {
        InitializeComponent();

        _entries = _storage.Load();
        _projects = _projectStorage.Load();
        _activities = _activityStorage.Load();

        InitializeThemeSelector();

        FilterStartDatePicker.SelectedDate = DateTime.Today;
        FilterEndDatePicker.SelectedDate = DateTime.Today;

        RefreshProjectsDropdown();
        RefreshActivitiesDropdown();
        RefreshFilterProjectsDropdown();
        RefreshFilterActivitiesDropdown();

        _isInitializing = false;
        RefreshGrid();

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateStatus();
    }

    private void InitializeThemeSelector()
    {
        ThemeComboBox.ItemsSource = ThemeManager.AvailableThemes;
        ThemeComboBox.SelectedItem = ThemeManager.AvailableThemes.FirstOrDefault(t => t.Theme == ThemeManager.CurrentTheme) 
                                     ?? ThemeManager.AvailableThemes[0];
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        if (ThemeComboBox.SelectedItem is ThemeOption option)
        {
            ThemeManager.ApplyTheme(option.Theme);
        }
    }

    private void RefreshActivitiesDropdown(string? selectActivityName = null)
    {
        var activeActivities = _activities.Where(a => a.IsActive).OrderBy(a => a.Name).ToList();
        ActivityComboBox.ItemsSource = null;
        ActivityComboBox.ItemsSource = activeActivities;

        if (!string.IsNullOrEmpty(selectActivityName))
        {
            var match = activeActivities.FirstOrDefault(a => a.Name == selectActivityName);
            if (match != null)
            {
                ActivityComboBox.SelectedItem = match;
            }
            else
            {
                ActivityComboBox.Text = selectActivityName;
            }
        }
        else if (ActivityComboBox.SelectedItem == null && activeActivities.Count > 0)
        {
            ActivityComboBox.SelectedIndex = 0;
        }
    }

    private void RefreshProjectsDropdown(string? selectProjectName = null)
    {
        var activeProjects = _projects.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
        ProjectComboBox.ItemsSource = null;
        ProjectComboBox.ItemsSource = activeProjects;

        if (!string.IsNullOrEmpty(selectProjectName))
        {
            ProjectComboBox.SelectedItem = activeProjects.FirstOrDefault(p => p.Name == selectProjectName);
        }
        else if (ProjectComboBox.SelectedItem == null && activeProjects.Count > 0)
        {
            ProjectComboBox.SelectedIndex = 0;
        }
    }

    private void RefreshFilterProjectsDropdown(string? selectedFilter = null)
    {
        var previousSelection = selectedFilter ?? FilterProjectComboBox.SelectedItem?.ToString();

        FilterProjectComboBox.Items.Clear();
        FilterProjectComboBox.Items.Add("All Projects");

        var projectNames = _projects.Select(p => p.Name)
            .Union(_entries.Select(e => e.ProjectName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        foreach (var name in projectNames)
        {
            FilterProjectComboBox.Items.Add(name);
        }

        if (!string.IsNullOrEmpty(previousSelection) && FilterProjectComboBox.Items.Contains(previousSelection))
        {
            FilterProjectComboBox.SelectedItem = previousSelection;
        }
        else
        {
            FilterProjectComboBox.SelectedIndex = 0;
        }
    }

    private void RefreshFilterActivitiesDropdown(string? selectedFilter = null)
    {
        var previousSelection = selectedFilter ?? FilterActivityComboBox.SelectedItem?.ToString();

        FilterActivityComboBox.Items.Clear();
        FilterActivityComboBox.Items.Add("All Activities");

        var activities = _activities.Select(a => a.Name)
            .Union(_entries.Select(e => e.Activity))
            .Where(act => !string.IsNullOrWhiteSpace(act))
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        foreach (var act in activities)
        {
            FilterActivityComboBox.Items.Add(act);
        }

        if (!string.IsNullOrEmpty(previousSelection) && FilterActivityComboBox.Items.Contains(previousSelection))
        {
            FilterActivityComboBox.SelectedItem = previousSelection;
        }
        else
        {
            FilterActivityComboBox.SelectedIndex = 0;
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _currentStartTime = DateTime.Now;
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        _timer.Start();
        UpdateStatus();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStartTime == null)
            return;

        _pendingStartTime = _currentStartTime.Value;
        _pendingEndTime = DateTime.Now;
        _currentStartTime = null;

        _timer.Stop();
        StatusTextBlock.Text = "Status: Review entry — save or discard.";

        StopButton.IsEnabled = false;
        SaveEntryButton.Visibility = Visibility.Visible;
        DiscardButton.Visibility = Visibility.Visible;
    }

    private void SaveEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingStartTime == null || _pendingEndTime == null)
            return;

        var selectedProject = ProjectComboBox.SelectedItem as Project;
        var projectName = selectedProject?.Name ?? ProjectComboBox.Text?.Trim() ?? string.Empty;
        var selectedActivity = ActivityComboBox.SelectedItem as ActivityItem;
        var activity = selectedActivity?.Name ?? ActivityComboBox.Text?.Trim() ?? ActivityComboBox.SelectedItem?.ToString() ?? string.Empty;

        var entry = new TimeEntry
        {
            ProjectName = projectName,
            Activity = activity,
            Description = DescriptionTextBox.Text,
            Note = NoteTextBox.Text,
            StartTime = _pendingStartTime.Value,
            EndTime = _pendingEndTime.Value
        };

        _entries.Add(entry);
        _storage.Save(_entries);

        RefreshFilterProjectsDropdown();
        RefreshFilterActivitiesDropdown();
        ClearPendingState();
        RefreshGrid();
    }

    private void DiscardButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPendingState();
    }

    private void ClearPendingState()
    {
        _pendingStartTime = null;
        _pendingEndTime = null;

        SaveEntryButton.Visibility = Visibility.Collapsed;
        DiscardButton.Visibility = Visibility.Collapsed;
        StartButton.IsEnabled = true;

        DescriptionTextBox.Clear();
        NoteTextBox.Clear();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (_currentStartTime != null)
        {
            var elapsed = DateTime.Now - _currentStartTime.Value;
            var projectName = (ProjectComboBox.SelectedItem as Project)?.Name ?? "Active Session";
            var selectedActivity = ActivityComboBox.SelectedItem as ActivityItem;
            var activity = selectedActivity?.Name ?? ActivityComboBox.Text?.Trim() ?? ActivityComboBox.SelectedItem?.ToString() ?? "";
            var activitySuffix = !string.IsNullOrEmpty(activity) ? $" [{activity}]" : "";
            StatusTextBlock.Text = $"🔴 RECORDING: {projectName}{activitySuffix} ({elapsed:hh\\:mm\\:ss})";
        }
        else
        {
            StatusTextBlock.Text = "Status: Idle";
        }
    }

    private void RefreshGrid()
    {
        if (_isInitializing)
            return;

        var startDate = FilterStartDatePicker.SelectedDate;
        var endDate = FilterEndDatePicker.SelectedDate;

        var selectedProject = FilterProjectComboBox.SelectedItem?.ToString();
        var isAllProjects = string.IsNullOrEmpty(selectedProject) || selectedProject == "All Projects";

        var selectedActivity = FilterActivityComboBox.SelectedItem?.ToString();
        var isAllActivities = string.IsNullOrEmpty(selectedActivity) || selectedActivity == "All Activities";

        var query = _entries.AsEnumerable();

        if (startDate.HasValue)
        {
            var startDateTime = startDate.Value.Date;
            query = query.Where(e => e.StartTime >= startDateTime);
        }

        if (endDate.HasValue)
        {
            var endDateTime = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(e => e.StartTime <= endDateTime);
        }

        if (!isAllProjects)
        {
            query = query.Where(e => e.ProjectName.Equals(selectedProject, StringComparison.OrdinalIgnoreCase));
        }

        if (!isAllActivities)
        {
            query = query.Where(e => e.Activity.Equals(selectedActivity, StringComparison.OrdinalIgnoreCase));
        }

        _filteredEntries = query.OrderByDescending(e => e.StartTime).ToList();

        EntriesDataGrid.ItemsSource = null;
        EntriesDataGrid.ItemsSource = _filteredEntries;

        UpdateSummaryStats();
    }

    private void UpdateSummaryStats()
    {
        var totalTicks = _filteredEntries.Sum(e => e.Duration.Ticks);
        var totalDuration = TimeSpan.FromTicks(totalTicks);
        var sessionCount = _filteredEntries.Count;

        StatsTotalTimeTextBlock.Text = $"{(int)totalDuration.TotalHours}h {totalDuration.Minutes:D2}m {totalDuration.Seconds:D2}s";
        StatsTotalSessionsTextBlock.Text = $"{sessionCount} session{(sessionCount == 1 ? "" : "s")}";

        // Top Project
        var topProject = _filteredEntries
            .GroupBy(e => string.IsNullOrWhiteSpace(e.ProjectName) ? "(Unassigned)" : e.ProjectName)
            .OrderByDescending(g => g.Sum(x => x.Duration.Ticks))
            .FirstOrDefault();

        if (topProject != null && totalTicks > 0)
        {
            var topTicks = topProject.Sum(x => x.Duration.Ticks);
            var percentage = ((double)topTicks / totalTicks) * 100.0;
            StatsTopProjectTextBlock.Text = $"{topProject.Key} ({percentage:F0}%)";
        }
        else if (topProject != null)
        {
            StatsTopProjectTextBlock.Text = topProject.Key;
        }
        else
        {
            StatsTopProjectTextBlock.Text = "None";
        }

        // Top Activity
        var topActivity = _filteredEntries
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Activity) ? "(Unassigned)" : e.Activity)
            .OrderByDescending(g => g.Sum(x => x.Duration.Ticks))
            .FirstOrDefault();

        if (topActivity != null && totalTicks > 0)
        {
            var topActTicks = topActivity.Sum(x => x.Duration.Ticks);
            var percentage = ((double)topActTicks / totalTicks) * 100.0;
            StatsTopActivityTextBlock.Text = $"{topActivity.Key} ({percentage:F0}%)";
        }
        else if (topActivity != null)
        {
            StatsTopActivityTextBlock.Text = topActivity.Key;
        }
        else
        {
            StatsTopActivityTextBlock.Text = "None";
        }
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        RefreshGrid();
    }

    private void TodayButton_Click(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;
        FilterStartDatePicker.SelectedDate = DateTime.Today;
        FilterEndDatePicker.SelectedDate = DateTime.Today;
        _isInitializing = false;
        RefreshGrid();
    }

    private void ShowAllButton_Click(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;
        FilterStartDatePicker.SelectedDate = null;
        FilterEndDatePicker.SelectedDate = null;
        FilterProjectComboBox.SelectedIndex = 0;
        FilterActivityComboBox.SelectedIndex = 0;
        _isInitializing = false;
        RefreshGrid();
    }

    private void AddNewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var currentlySelected = (ProjectComboBox.SelectedItem as Project)?.Name;
        var currentFilterSelected = FilterProjectComboBox.SelectedItem?.ToString();
        var currentActivityFilterSelected = FilterActivityComboBox.SelectedItem?.ToString();

        var dialog = new ProjectManagementWindow(_projectStorage, _projects)
        {
            Owner = this
        };

        dialog.ShowDialog();
        RefreshProjectsDropdown(currentlySelected);
        RefreshFilterProjectsDropdown(currentFilterSelected);
        RefreshFilterActivitiesDropdown(currentActivityFilterSelected);
        RefreshGrid();
    }

    private void AddNewActivityButton_Click(object sender, RoutedEventArgs e)
    {
        var currentlySelected = (ActivityComboBox.SelectedItem as ActivityItem)?.Name ?? ActivityComboBox.Text?.Trim();
        var currentFilterSelected = FilterActivityComboBox.SelectedItem?.ToString();

        var dialog = new ActivityManagementWindow(_activityStorage, _activities)
        {
            Owner = this
        };

        dialog.ShowDialog();
        RefreshActivitiesDropdown(currentlySelected);
        RefreshFilterActivitiesDropdown(currentFilterSelected);
        RefreshGrid();
    }

    private void AnalyticsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AnalyticsWindow(_entries, _projects, _activities)
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void DeleteEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TimeEntry entry)
        {
            DeleteTimeEntry(entry);
        }
    }

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesDataGrid.SelectedItem is TimeEntry selectedEntry)
        {
            DeleteTimeEntry(selectedEntry);
        }
        else
        {
            MessageBox.Show("Please select a task from the list to delete.", "No Task Selected", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void EntriesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && EntriesDataGrid.SelectedItem is TimeEntry selectedEntry)
        {
            e.Handled = true;
            DeleteTimeEntry(selectedEntry);
        }
    }

    private void DeleteTimeEntry(TimeEntry entry)
    {
        var taskDesc = !string.IsNullOrWhiteSpace(entry.Description)
            ? $"\"{entry.Description}\""
            : (!string.IsNullOrWhiteSpace(entry.ProjectName) ? $"entry for {entry.ProjectName}" : "this task");

        var result = MessageBox.Show(
            $"Are you sure you want to delete {taskDesc} ({entry.StartTime:g} - {entry.Duration:hh\\:mm\\:ss})?",
            "Confirm Delete Task",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _entries.Remove(entry);
            _storage.Save(_entries);
            RefreshFilterProjectsDropdown(FilterProjectComboBox.SelectedItem?.ToString());
            RefreshFilterActivitiesDropdown(FilterActivityComboBox.SelectedItem?.ToString());
            RefreshGrid();
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var timestamp = DateTime.Now.ToString("dd-MM-yyyy");
        var defaultFileName = $"{timestamp}_freelance-time-export.csv";

        var dialog = new SaveFileDialog
        {
            FileName = defaultFileName,
            DefaultExt = ".csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            InitialDirectory = AppContext.BaseDirectory
        };

        if (dialog.ShowDialog() != true)
            return;

        CsvExportService.Export(_filteredEntries, dialog.FileName);
        MessageBox.Show($"Export complete:\n{dialog.FileName}");
    }
}

