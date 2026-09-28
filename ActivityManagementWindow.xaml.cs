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
        _storage = storage;
        _activities = activities;
        RefreshLists();
    }

    private void RefreshLists()
    {
        ActiveActivitiesListBox.ItemsSource = null;
        ActiveActivitiesListBox.ItemsSource = _activities.Where(a => a.IsActive).OrderBy(a => a.Name).ToList();

        InactiveActivitiesListBox.ItemsSource = null;
        InactiveActivitiesListBox.ItemsSource = _activities.Where(a => !a.IsActive).OrderBy(a => a.Name).ToList();
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
            MessageBox.Show("Please enter an activity name.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_activities.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("An activity with this name already exists.", "Duplicate Activity", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show("Please select an active activity to archive.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Please select an inactive activity to activate.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
