using System.Windows;
using System.Windows.Input;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class ProjectManagementWindow : Window
{
    private readonly ProjectStorage _storage;
    private readonly List<Project> _projects;

    public ProjectManagementWindow(ProjectStorage storage, List<Project> projects)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);
        _storage = storage;
        _projects = projects;
        RefreshLists();
    }

    private void RefreshLists()
    {
        var active = _projects.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
        ActiveProjectsListBox.ItemsSource = null;
        ActiveProjectsListBox.ItemsSource = active;
        ActiveProjectsEmptyState.Visibility = active.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var inactive = _projects.Where(p => !p.IsActive).OrderBy(p => p.Name).ToList();
        InactiveProjectsListBox.ItemsSource = null;
        InactiveProjectsListBox.ItemsSource = inactive;
        InactiveProjectsEmptyState.Visibility = inactive.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddProjectButton_Click(object sender, RoutedEventArgs e)
    {
        AddProject();
    }

    private void NewProjectTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddProject();
        }
    }

    private void AddProject()
    {
        var name = NewProjectTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ThemedMessageBox.ShowWarning(this, "Please enter a project name.", "Invalid Name");
            return;
        }

        if (_projects.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            ThemedMessageBox.ShowWarning(this, "A project with this name already exists.", "Duplicate Project");
            return;
        }

        var newProject = new Project
        {
            Name = name,
            IsActive = true
        };

        _projects.Add(newProject);
        _storage.Save(_projects);

        NewProjectTextBox.Clear();
        RefreshLists();
        ActiveProjectsListBox.SelectedItem = newProject;
    }

    private void ArchiveProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (ActiveProjectsListBox.SelectedItem is Project selected)
        {
            selected.IsActive = false;
            _storage.Save(_projects);
            RefreshLists();
        }
        else
        {
            ThemedMessageBox.ShowInfo(this, "Please select an active project to archive.", "No Selection");
        }
    }

    private void ActivateProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (InactiveProjectsListBox.SelectedItem is Project selected)
        {
            selected.IsActive = true;
            _storage.Save(_projects);
            RefreshLists();
        }
        else
        {
            ThemedMessageBox.ShowInfo(this, "Please select an inactive project to activate.", "No Selection");
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
