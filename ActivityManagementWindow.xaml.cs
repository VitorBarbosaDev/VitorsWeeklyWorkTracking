using System.Windows;
using System.Windows.Input;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class ActivityManagementWindow : Window
{
    private readonly ActivityStorage _storage;
    private readonly List<ActivityItem> _activities;

    public ActivityManagementWindow(ActivityStorage storage, List<ActivityItem> activities)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);
        _storage = storage;
        _activities = activities;
        RefreshLists();
    }

    private void RefreshLists()
    {
        var active = _activities.Where(a => a.IsActive).OrderBy(a => a.Name).ToList();
        ActiveActivitiesListBox.ItemsSource = null;
        ActiveActivitiesListBox.ItemsSource = active;
        ActiveActivitiesEmptyState.Visibility = active.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var inactive = _activities.Where(a => !a.IsActive).OrderBy(a => a.Name).ToList();
        InactiveActivitiesListBox.ItemsSource = null;
        InactiveActivitiesListBox.ItemsSource = inactive;
        InactiveActivitiesEmptyState.Visibility = inactive.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddActivityButton_Click(object sender, RoutedEventArgs e)
    {
        AddActivity();
    }

    private void NewActivityTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddActivity();
        }
    }

    private void AddActivity()
    {
        var name = NewActivityTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ThemedMessageBox.ShowWarning(this, "Please enter an activity name.", "Invalid Name");
            return;
        }

        if (_activities.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            ThemedMessageBox.ShowWarning(this, "An activity with this name already exists.", "Duplicate Activity");
            return;
        }

        var newActivity = new ActivityItem
        {
            Name = name,
            IsActive = true
        };

        _activities.Add(newActivity);
        _storage.Save(_activities);

        NewActivityTextBox.Clear();
        RefreshLists();
        ActiveActivitiesListBox.SelectedItem = newActivity;
    }

    private void ArchiveActivityButton_Click(object sender, RoutedEventArgs e)
    {
        if (ActiveActivitiesListBox.SelectedItem is ActivityItem selected)
        {
            selected.IsActive = false;
            _storage.Save(_activities);
            RefreshLists();
        }
        else
        {
            ThemedMessageBox.ShowInfo(this, "Please select an active activity to archive.", "No Selection");
        }
    }

    private void ActivateActivityButton_Click(object sender, RoutedEventArgs e)
    {
        if (InactiveActivitiesListBox.SelectedItem is ActivityItem selected)
        {
            selected.IsActive = true;
            _storage.Save(_activities);
            RefreshLists();
        }
        else
        {
            ThemedMessageBox.ShowInfo(this, "Please select an inactive activity to activate.", "No Selection");
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
