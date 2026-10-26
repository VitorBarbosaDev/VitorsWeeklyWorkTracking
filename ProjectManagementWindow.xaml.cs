using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;

namespace VitorsWeeklyWorkTracking;

public partial class ProjectManagementWindow : Window
{
    private readonly ProjectStorage _storage;
    private readonly List<Project> _projects;
    private readonly List<Border> _swatchElements = new();

    public ProjectManagementWindow(ProjectStorage storage, List<Project> projects)
    {
        InitializeComponent();
        WindowTitleBarHelper.ApplyThemeTitleBar(this);
        _storage = storage;
        _projects = projects;
        KeyDown += ProjectManagementWindow_KeyDown;
        BuildPaletteSwatches();
        RefreshLists();
        if (ActiveProjectsListBox.Items.Count > 0)
        {
            ActiveProjectsListBox.SelectedIndex = 0;
        }
        else
        {
            UpdateColorSection(null);
        }
    }

    private void ProjectManagementWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2)
        {
            RenameProject();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && !NewProjectTextBox.IsFocused && !CustomHexTextBox.IsFocused)
        {
            DeleteProject();
            e.Handled = true;
        }
    }

    private void BuildPaletteSwatches()
    {
        PaletteSwatchesPanel.Children.Clear();
        _swatchElements.Clear();

        foreach (var (hex, name) in ProjectColorHelper.CuratedPalette)
        {
            var brush = CreateBrush(hex);
            var swatchBorder = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(5),
                Background = brush,
                BorderBrush = TryFindResource("Theme.CardBorder") as Brush ?? Brushes.Gray,
                BorderThickness = new Thickness(1.5),
                Margin = new Thickness(3),
                Cursor = Cursors.Hand,
                ToolTip = $"{name} ({hex})",
                Tag = hex
            };

            swatchBorder.MouseLeftButtonUp += (s, e) =>
            {
                if (s is Border b && b.Tag is string colorHex)
                {
                    SetSelectedProjectColor(colorHex);
                }
            };

            _swatchElements.Add(swatchBorder);
            PaletteSwatchesPanel.Children.Add(swatchBorder);
        }
    }

    private Project? GetSelectedProject()
    {
        if (ProjectsTabControl == null) return null;
        if (ProjectsTabControl.SelectedIndex <= 0)
            return (ActiveProjectsListBox?.SelectedItem as Project) ?? (InactiveProjectsListBox?.SelectedItem as Project);
        return (InactiveProjectsListBox?.SelectedItem as Project) ?? (ActiveProjectsListBox?.SelectedItem as Project);
    }

    private void UpdateColorSection(Project? project)
    {
        if (project == null)
        {
            ColorTargetLabel.Text = "Select a project above to customize its color (Default: Grey)";
            CurrentColorIndicator.Background = Brushes.Transparent;
            CurrentColorHexLabel.Text = "—";
            CustomHexTextBox.Text = string.Empty;
            CustomHexTextBox.IsEnabled = false;
            ApplyCustomHexButton.IsEnabled = false;
            ResetColorButton.IsEnabled = false;
            PaletteSwatchesPanel.IsEnabled = false;
            UpdateSwatchHighlights(null);
            return;
        }

        CustomHexTextBox.IsEnabled = true;
        ApplyCustomHexButton.IsEnabled = true;
        PaletteSwatchesPanel.IsEnabled = true;
        ResetColorButton.IsEnabled = project.HasCustomColor;

        var displayHex = project.DisplayColorHex;
        ColorTargetLabel.Text = $"Color for: {project.Name}";
        CurrentColorIndicator.Background = project.Brush;
        CurrentColorHexLabel.Text = project.HasCustomColor ? $"{displayHex} (Custom)" : $"{displayHex} (Default Grey)";
        CustomHexTextBox.Text = displayHex;

        UpdateSwatchHighlights(displayHex);
    }

    private void UpdateSwatchHighlights(string? activeHex)
    {
        foreach (var border in _swatchElements)
        {
            var hex = border.Tag as string;
            bool isMatch = !string.IsNullOrEmpty(activeHex) && string.Equals(hex, activeHex, StringComparison.OrdinalIgnoreCase);
            if (isMatch)
            {
                border.BorderBrush = TryFindResource("Theme.Foreground") as Brush ?? Brushes.White;
                border.BorderThickness = new Thickness(2.5);
            }
            else
            {
                border.BorderBrush = TryFindResource("Theme.CardBorder") as Brush ?? Brushes.Gray;
                border.BorderThickness = new Thickness(1.5);
            }
        }
    }

    private void SetSelectedProjectColor(string? hex)
    {
        var selected = GetSelectedProject();
        if (selected == null)
        {
            ColorTargetLabel.Text = "Select a project above to customize its color";
            return;
        }

        selected.ColorHex = string.IsNullOrWhiteSpace(hex) ? null : ProjectColorHelper.NormalizeHex(hex);
        ProjectColorHelper.SetProjectColor(selected.Name, selected.ColorHex);
        _storage.Save(_projects);

        var selectedName = selected.Name;
        RefreshLists();
        ReselectProject(selectedName);
    }

    private void ReselectProject(string name)
    {
        if (ProjectsTabControl.SelectedIndex <= 0)
        {
            ActiveProjectsListBox.SelectedItem = _projects.FirstOrDefault(p => p.IsActive && p.Name == name)
                ?? _projects.FirstOrDefault(p => p.Name == name);
        }
        else
        {
            InactiveProjectsListBox.SelectedItem = _projects.FirstOrDefault(p => !p.IsActive && p.Name == name)
                ?? _projects.FirstOrDefault(p => p.Name == name);
        }
        UpdateColorSection(GetSelectedProject());
    }

    private void ProjectListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateColorSection(GetSelectedProject());
    }

    private void ProjectsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl)
        {
            UpdateColorSection(GetSelectedProject());
        }
    }

    private void ProjectListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        RenameProject();
    }

    private void RenameProjectButton_Click(object sender, RoutedEventArgs e)
    {
        RenameProject();
    }

    private void RenameProject()
    {
        var selected = GetSelectedProject();
        if (selected == null)
        {
            ThemedMessageBox.ShowInfo(this, "Please select a project to rename.", "Rename Project");
            return;
        }

        string oldName = selected.Name;
        string? newName = ThemedMessageBox.Prompt(
            this,
            $"Enter a new name for project \"{oldName}\":",
            "Rename Project",
            oldName,
            validator: val =>
            {
                if (string.IsNullOrWhiteSpace(val))
                    return "Project name cannot be empty.";
                if (!val.Trim().Equals(oldName, StringComparison.OrdinalIgnoreCase) &&
                    _projects.Any(p => p.Name.Equals(val.Trim(), StringComparison.OrdinalIgnoreCase)))
                    return $"A project named \"{val.Trim()}\" already exists.";
                return null;
            });

        if (string.IsNullOrWhiteSpace(newName) || newName.Trim().Equals(oldName, StringComparison.Ordinal))
            return;

        newName = newName.Trim();

        // Create pre-update backup before bulk renaming project across multiple files
        BackupService.Instance.CreateBackup("Pre-Update (Project Rename)", $"Before renaming project '{oldName}' to '{newName}'");

        selected.Name = newName;

        if (selected.Milestones != null)
        {
            foreach (var m in selected.Milestones)
            {
                m.ProjectName = newName;
            }
        }

        if (selected.HasCustomColor)
        {
            ProjectColorHelper.SetProjectColor(newName, selected.ColorHex);
            ProjectColorHelper.RemoveProjectColor(oldName);
        }

        _storage.Save(_projects);

        // Update time entries
        try
        {
            var entriesPath = StoragePathHelper.GetFilePath("time-entries.json");
            if (File.Exists(entriesPath))
            {
                var json = File.ReadAllText(entriesPath);
                var entries = JsonSerializer.Deserialize<List<TimeEntry>>(json);
                if (entries != null)
                {
                    bool modified = false;
                    foreach (var entry in entries)
                    {
                        if (string.Equals(entry.ProjectName, oldName, StringComparison.OrdinalIgnoreCase))
                        {
                            entry.ProjectName = newName;
                            modified = true;
                        }
                    }
                    if (modified)
                    {
                        var newJson = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(entriesPath, newJson);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ProjectManagement.RenameProject.TimeEntries");
        }

        // Update calendar plans
        try
        {
            var plansPath = StoragePathHelper.GetFilePath("calendar-plans.json");
            if (File.Exists(plansPath))
            {
                var json = File.ReadAllText(plansPath);
                var plans = JsonSerializer.Deserialize<List<PlannedWorkItem>>(json);
                if (plans != null)
                {
                    bool modified = false;
                    foreach (var plan in plans)
                    {
                        if (string.Equals(plan.ProjectName, oldName, StringComparison.OrdinalIgnoreCase))
                        {
                            plan.ProjectName = newName;
                            modified = true;
                        }
                    }
                    if (modified)
                    {
                        var newJson = JsonSerializer.Serialize(plans, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(plansPath, newJson);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StoragePathHelper.LogError(ex, "ProjectManagement.RenameProject.CalendarPlans");
        }

        RefreshLists();
        ReselectProject(newName);
    }

    private void DeleteProjectButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteProject();
    }

    private void DeleteProject()
    {
        var selected = GetSelectedProject();
        if (selected == null)
        {
            ThemedMessageBox.ShowInfo(this, "Please select a project to delete.", "Delete Project");
            return;
        }

        string projectName = selected.Name;
        int entryCount = 0;
        int planCount = 0;

        try
        {
            var entriesPath = StoragePathHelper.GetFilePath("time-entries.json");
            if (File.Exists(entriesPath))
            {
                var json = File.ReadAllText(entriesPath);
                var entries = JsonSerializer.Deserialize<List<TimeEntry>>(json);
                if (entries != null)
                {
                    entryCount = entries.Count(entry => string.Equals(entry.ProjectName, projectName, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
        catch { }

        try
        {
            var plansPath = StoragePathHelper.GetFilePath("calendar-plans.json");
            if (File.Exists(plansPath))
            {
                var json = File.ReadAllText(plansPath);
                var plans = JsonSerializer.Deserialize<List<PlannedWorkItem>>(json);
                if (plans != null)
                {
                    planCount = plans.Count(p => string.Equals(p.ProjectName, projectName, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
        catch { }

        string message;
        if (entryCount > 0 || planCount > 0)
        {
            message = $"Project \"{projectName}\" has {entryCount} recorded time entries and {planCount} calendar plans.\n\nDeleting it removes it from your project list (historical time entries and plans will retain their project name in logs).\n\nAre you sure you want to delete this project?";
        }
        else
        {
            message = $"Are you sure you want to delete project \"{projectName}\"?";
        }

        bool confirmed = ThemedMessageBox.ConfirmDelete(
            this,
            message,
            $"Delete Project: {projectName}",
            "Delete Project",
            "Cancel");

        if (!confirmed) return;

        BackupService.Instance.CreateBackup("Pre-Update (Project Delete)", $"Before deleting project '{projectName}'");

        _projects.Remove(selected);
        ProjectColorHelper.RemoveProjectColor(projectName);
        _storage.Save(_projects);

        RefreshLists();
        UpdateColorSection(GetSelectedProject());
    }

    private void SyncProjectsButton_Click(object sender, RoutedEventArgs e)
    {
        var added = _storage.SyncProjects(_projects);
        if (added.Count == 0)
        {
            ThemedMessageBox.ShowInfo(this, "All projects from your time entries and calendar plans are already in your project list.", "Sync Projects");
            return;
        }

        RefreshLists();
        var names = string.Join(", ", added.Select(p => $"\"{p.Name}\""));
        ThemedMessageBox.ShowSuccess(this, $"Successfully imported {added.Count} project(s) from your data:\n\n{names}", "Projects Synced");
    }

    private void ApplyCustomHexButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyCustomHex();
    }

    private void CustomHexTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyCustomHex();
        }
    }

    private void ApplyCustomHex()
    {
        var input = CustomHexTextBox.Text.Trim();
        if (!ProjectColorHelper.IsValidHex(input))
        {
            ThemedMessageBox.ShowWarning(this, "Please enter a valid HEX color code (e.g., #3B82F6 or 3B82F6).", "Invalid Color Code");
            return;
        }

        SetSelectedProjectColor(input);
    }

    private void OpenColorWheelButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedProject();
        if (selected == null)
        {
            ThemedMessageBox.ShowWarning(this, "Please select a project first to choose a color.", "Select Project");
            return;
        }

        var currentColor = selected.ColorHex ?? ProjectColorHelper.DefaultProjectColorHex;
        var chosenHex = ColorPickerDialog.PickColor(this, currentColor, $"🎨 Color for '{selected.Name}'");
        if (!string.IsNullOrWhiteSpace(chosenHex))
        {
            SetSelectedProjectColor(chosenHex);
        }
    }

    private void ResetColorButton_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedProjectColor(null);
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
        ProjectsTabControl.SelectedIndex = 0;
    }

    private void ProjectMilestonesButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedProject();
        if (selected == null)
        {
            ThemedMessageBox.ShowInfo(this, "Please select an active project first to manage its milestones.", "Select Project");
            return;
        }

        var dialog = new EditMilestoneDialog(selected.Name, DateTime.Today.AddDays(14), _projects)
        {
            Owner = this
        };
        if (dialog.ShowDialog() == true && dialog.ResultMilestone != null)
        {
            MilestoneService.Instance.AddMilestone(dialog.ResultMilestone);
            ThemedMessageBox.ShowSuccess(this, $"Milestone \"{dialog.ResultMilestone.Title}\" saved for project \"{selected.Name}\".", "Milestone Saved");
        }
    }

    private void ArchiveProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (ActiveProjectsListBox.SelectedItem is Project selected)
        {
            selected.IsActive = false;
            _storage.Save(_projects);
            RefreshLists();
            UpdateColorSection(GetSelectedProject());
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
            UpdateColorSection(GetSelectedProject());
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

    private static SolidColorBrush CreateBrush(string hex)
    {
        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
        catch
        {
            var fallback = new SolidColorBrush(Colors.DodgerBlue);
            fallback.Freeze();
            return fallback;
        }
    }
}
