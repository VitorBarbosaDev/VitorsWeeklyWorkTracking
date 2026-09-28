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
        ActiveProjectsListBox.ItemsSource = null;
        ActiveProjectsListBox.ItemsSource = _projects.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();

        InactiveProjectsListBox.ItemsSource = null;
        InactiveProjectsListBox.ItemsSource = _projects.Where(p => !p.IsActive).OrderBy(p => p.Name).ToList();
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
            MessageBox.Show("Please enter a project name.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_projects.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("A project with this name already exists.", "Duplicate Project", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show("Please select an active project to archive.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Please select an inactive project to activate.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
