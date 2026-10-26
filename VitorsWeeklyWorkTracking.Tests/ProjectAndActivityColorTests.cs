using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

[Collection("StorageTests")]
public class ProjectAndActivityColorTests
{
    [Fact]
    public void ProjectColorHelper_DefaultAndCustomColor_BehavesCorrectly()
    {
        ProjectColorHelper.ClearCustomColors();

        var defaultHex = ProjectColorHelper.GetProjectColorHex("Test Game Dev");
        Assert.Equal(ProjectColorHelper.DefaultProjectColorHex, defaultHex);
        Assert.False(ProjectColorHelper.HasCustomColor("Test Game Dev"));

        // Set custom color
        ProjectColorHelper.SetProjectColor("Test Game Dev", "#FF5722");
        Assert.True(ProjectColorHelper.HasCustomColor("Test Game Dev"));
        Assert.Equal("#FF5722", ProjectColorHelper.GetProjectColorHex("Test Game Dev"));
        Assert.Equal("#FF5722", ProjectColorHelper.GetProjectColorHex("  test game dev  "));

        // Reset custom color
        ProjectColorHelper.SetProjectColor("Test Game Dev", null);
        Assert.False(ProjectColorHelper.HasCustomColor("Test Game Dev"));
        Assert.Equal(ProjectColorHelper.DefaultProjectColorHex, ProjectColorHelper.GetProjectColorHex("Test Game Dev"));
    }

    [Fact]
    public void ActivityColorHelper_DefaultAndCustomColor_BehavesCorrectly()
    {
        ActivityColorHelper.ClearCustomColors();

        var defaultHex = ActivityColorHelper.GetActivityColorHex("Sound Design");
        Assert.Equal(ActivityColorHelper.DefaultActivityColorHex, defaultHex);
        Assert.False(ActivityColorHelper.HasCustomColor("Sound Design"));

        // Set custom color
        ActivityColorHelper.SetActivityColor("Sound Design", "#8B5CF6");
        Assert.True(ActivityColorHelper.HasCustomColor("Sound Design"));
        Assert.Equal("#8B5CF6", ActivityColorHelper.GetActivityColorHex("Sound Design"));
        Assert.Equal("#8B5CF6", ActivityColorHelper.GetActivityColorHex("  sound design  "));

        // Reset custom color
        ActivityColorHelper.SetActivityColor("Sound Design", "");
        Assert.False(ActivityColorHelper.HasCustomColor("Sound Design"));
        Assert.Equal(ActivityColorHelper.DefaultActivityColorHex, ActivityColorHelper.GetActivityColorHex("Sound Design"));
    }

    [Theory]
    [InlineData("#2563EB", true, "#2563EB")]
    [InlineData("2563EB", true, "#2563EB")]
    [InlineData("#abc", true, "#AABBCC")]
    [InlineData("abc", true, "#AABBCC")]
    [InlineData("#10B981", true, "#10B981")]
    [InlineData("#INVALID", false, "")]
    [InlineData("", false, "")]
    [InlineData(null, false, "")]
    public void HexValidationAndNormalization_ValidatesAndNormalizesAccurately(string? input, bool expectedValid, string expectedNormalized)
    {
        bool projectValid = ProjectColorHelper.IsValidHex(input);
        bool activityValid = ActivityColorHelper.IsValidHex(input);

        Assert.Equal(expectedValid, projectValid);
        Assert.Equal(expectedValid, activityValid);

        if (expectedValid)
        {
            Assert.Equal(expectedNormalized, ProjectColorHelper.NormalizeHex(input!));
            Assert.Equal(expectedNormalized, ActivityColorHelper.NormalizeHex(input!));
        }
    }

    [Fact]
    public void ProjectAndActivityModels_ColorProperties_WorkCorrectly()
    {
        ProjectColorHelper.ClearCustomColors();
        ActivityColorHelper.ClearCustomColors();

        var project = new Project
        {
            Name = "Vitor App",
            ColorHex = "#E11D48"
        };
        ProjectColorHelper.RegisterProjects(new[] { project });

        Assert.True(project.HasCustomColor);
        Assert.Equal("#E11D48", project.DisplayColorHex);
        Assert.NotNull(project.Brush);

        var activity = new ActivityItem
        {
            Name = "UI Design",
            ColorHex = "#06B6D4"
        };
        ActivityColorHelper.RegisterActivities(new[] { activity });

        Assert.True(activity.HasCustomColor);
        Assert.Equal("#06B6D4", activity.DisplayColorHex);
        Assert.NotNull(activity.Brush);
    }

    [Fact]
    public void ActivityColorHelper_GetActivityBadgeBrushes_ReturnsValidBrushesForLightAndDark()
    {
        var (bgLight, borderLight, fgLight) = ActivityColorHelper.GetActivityBadgeBrushes("Coding", isDark: false);
        Assert.NotNull(bgLight);
        Assert.NotNull(borderLight);
        Assert.NotNull(fgLight);

        var (bgDark, borderDark, fgDark) = ActivityColorHelper.GetActivityBadgeBrushes("Coding", isDark: true);
        Assert.NotNull(bgDark);
        Assert.NotNull(borderDark);
        Assert.NotNull(fgDark);
    }

    [Fact]
    public void TimeEntryAndPlannedWorkItem_ExposeProjectAndActivityBrushes()
    {
        var entry = new TimeEntry
        {
            ProjectName = "Project X",
            Activity = "Art 3D",
            StartTime = DateTime.Today.AddHours(9),
            EndTime = DateTime.Today.AddHours(11)
        };

        Assert.NotNull(entry.ProjectBrush);
        Assert.NotNull(entry.ActivityBrush);

        var plan = new PlannedWorkItem
        {
            ProjectName = "Project X",
            Activity = "Art 3D",
            PlannedHours = 2.0
        };

        Assert.NotNull(plan.ProjectBrush);
        Assert.NotNull(plan.ActivityBrush);
        Assert.NotNull(plan.ActivityBadgeBackground);
        Assert.NotNull(plan.ActivityBadgeBorder);
        Assert.NotNull(plan.ActivityBadgeForeground);
    }

    [Fact]
    public void ProjectStorage_SyncProjects_DiscoversPreExistingProjectsAndRemovesDummyPlaceholders()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"proj_sync_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var projectsFile = Path.Combine(tempDir, "projects.json");
            var initialProjects = new List<Project>
            {
                new Project { Name = "Alpha", IsActive = true },
                new Project { Name = "Beta", IsActive = true, ColorHex = "#F59E0B" }
            };

            var storage = new ProjectStorage(projectsFile);

            var mockEntries = new List<TimeEntry>
            {
                new TimeEntry { ProjectName = "Curragh Racer", Activity = "Coding", StartTime = DateTime.Today, EndTime = DateTime.Today.AddHours(1) },
                new TimeEntry { ProjectName = "Fun Work Tracker", Activity = "Coding", StartTime = DateTime.Today, EndTime = DateTime.Today.AddHours(2) }
            };
            var mockPlans = new List<PlannedWorkItem>
            {
                new PlannedWorkItem { ProjectName = "Blog/Site", Activity = "Coding", PlannedHours = 3 }
            };

            var added = storage.SyncProjects(initialProjects, mockEntries, mockPlans);

            Assert.Equal(3, added.Count);
            Assert.Contains(initialProjects, p => p.Name == "Curragh Racer");
            Assert.Contains(initialProjects, p => p.Name == "Fun Work Tracker");
            Assert.Contains(initialProjects, p => p.Name == "Blog/Site");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ActivityStorage_SyncActivities_DiscoversPreExistingActivitiesAndRemovesDummyPlaceholders()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"act_sync_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var activitiesFile = Path.Combine(tempDir, "activities.json");
            var initialActivities = new List<ActivityItem>
            {
                new ActivityItem { Name = "Coding", IsActive = true },
                new ActivityItem { Name = "Marketing", IsActive = true, ColorHex = "#EC4899" }
            };

            var storage = new ActivityStorage(activitiesFile);

            var mockEntries = new List<TimeEntry>
            {
                new TimeEntry { ProjectName = "Curragh Racer", Activity = "Art", StartTime = DateTime.Today, EndTime = DateTime.Today.AddHours(1) },
                new TimeEntry { ProjectName = "Fun Work Tracker", Activity = "Planning / Admin", StartTime = DateTime.Today, EndTime = DateTime.Today.AddHours(2) }
            };
            var mockPlans = new List<PlannedWorkItem>
            {
                new PlannedWorkItem { ProjectName = "Blog/Site", Activity = "Writing", PlannedHours = 3 }
            };

            var added = storage.SyncActivities(initialActivities, mockEntries, mockPlans);

            Assert.Equal(3, added.Count);
            Assert.Contains(initialActivities, a => a.Name == "Art");
            Assert.Contains(initialActivities, a => a.Name == "Planning / Admin");
            Assert.Contains(initialActivities, a => a.Name == "Writing");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ProjectStorage_FiltersOutTestPlaceholdersFromProjectsList()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"proj_filter_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;

        try
        {
            var projectsFile = Path.Combine(tempDir, "projects.json");
            var initialProjects = new List<Project>
            {
                new Project { Name = "Quick Sync", IsActive = true },
                new Project { Name = "Long Milestone", IsActive = true },
                new Project { Name = "Alpha", IsActive = true },
                new Project { Name = "Beta", IsActive = true },
                new Project { Name = "Blender", IsActive = true }
            };

            var storage = new ProjectStorage(projectsFile);
            storage.Save(initialProjects);

            var loaded = storage.Load();
            Assert.Contains(loaded, p => p.Name == "Blender");
            Assert.DoesNotContain(loaded, p => p.Name == "Quick Sync");
            Assert.DoesNotContain(loaded, p => p.Name == "Long Milestone");
            Assert.DoesNotContain(loaded, p => p.Name == "Alpha");
            Assert.DoesNotContain(loaded, p => p.Name == "Beta");
        }
        finally
        {
            StoragePathHelper.OverrideAppDataFolder = null;
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ProjectManagementWindow_STA_CanSelectProjectAndChangeColor()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var tempFile = Path.Combine(Path.GetTempPath(), $"projects_test_{Guid.NewGuid():N}.json");
                var projects = new List<Project>
                {
                    new Project { Name = "Curragh Racer", IsActive = true },
                    new Project { Name = "Fun Work Tracker", IsActive = true, ColorHex = "#F59E0B" }
                };

                var storage = new ProjectStorage(tempFile);
                var window = new ProjectManagementWindow(storage, projects);

                Assert.NotNull(window.ActiveProjectsListBox);
                Assert.NotNull(window.PaletteSwatchesPanel);
                Assert.True(window.PaletteSwatchesPanel.Children.Count > 10);

                // Select Curragh Racer
                window.ActiveProjectsListBox.SelectedIndex = 0;
                var curragh = window.ActiveProjectsListBox.SelectedItem as Project;
                Assert.NotNull(curragh);
                Assert.Equal("Curragh Racer", curragh.Name);

                // Apply custom hex
                window.CustomHexTextBox.Text = "#10B981";
                window.ApplyCustomHexButton.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

                Assert.Equal("#10B981", curragh.ColorHex);
                Assert.True(curragh.HasCustomColor);
                Assert.Equal("#10B981", ProjectColorHelper.GetProjectColorHex("Curragh Racer"));

                // Reset color to grey
                window.ResetColorButton.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Assert.Null(curragh.ColorHex);
                Assert.False(curragh.HasCustomColor);
                Assert.Equal(ProjectColorHelper.DefaultProjectColorHex, ProjectColorHelper.GetProjectColorHex("Curragh Racer"));

                window.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void ActivityManagementWindow_STA_CanSelectActivityAndChangeColor()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var tempFile = Path.Combine(Path.GetTempPath(), $"activities_test_{Guid.NewGuid():N}.json");
                var activities = new List<ActivityItem>
                {
                    new ActivityItem { Name = "Coding", IsActive = true },
                    new ActivityItem { Name = "Game Dev", IsActive = true, ColorHex = "#EC4899" }
                };

                var storage = new ActivityStorage(tempFile);
                var window = new ActivityManagementWindow(storage, activities);

                Assert.NotNull(window.ActiveActivitiesListBox);
                Assert.NotNull(window.PaletteSwatchesPanel);
                Assert.True(window.PaletteSwatchesPanel.Children.Count > 10);

                // Select Coding
                window.ActiveActivitiesListBox.SelectedIndex = 0;
                var coding = window.ActiveActivitiesListBox.SelectedItem as ActivityItem;
                Assert.NotNull(coding);
                Assert.Equal("Coding", coding.Name);

                // Apply custom hex
                window.CustomHexTextBox.Text = "#3B82F6";
                window.ApplyCustomHexButton.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

                Assert.Equal("#3B82F6", coding.ColorHex);
                Assert.True(coding.HasCustomColor);
                Assert.Equal("#3B82F6", ActivityColorHelper.GetActivityColorHex("Coding"));

                // Reset color
                window.ResetColorButton.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Assert.Null(coding.ColorHex);
                Assert.False(coding.HasCustomColor);
                Assert.Equal(ActivityColorHelper.DefaultActivityColorHex, ActivityColorHelper.GetActivityColorHex("Coding"));

                window.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void ColorPickerDialog_ColorConversions_And_Normalization_WorkAccurately()
    {
        // Test Normalization
        Assert.Equal("#3B82F6", ColorPickerDialog.NormalizeHex("#3B82F6"));
        Assert.Equal("#3B82F6", ColorPickerDialog.NormalizeHex("3B82F6"));
        Assert.Equal("#FFFFFF", ColorPickerDialog.NormalizeHex("#fff"));
        Assert.Equal("#FFFFFF", ColorPickerDialog.NormalizeHex("fff"));
        Assert.Null(ColorPickerDialog.NormalizeHex("not-a-color"));
        Assert.Null(ColorPickerDialog.NormalizeHex(""));

        // Test RGB to HSV to RGB round-trip for primary colors
        var pureRed = Color.FromRgb(255, 0, 0);
        ColorPickerDialog.RgbToHsv(pureRed.R, pureRed.G, pureRed.B, out double h, out double s, out double v);
        Assert.Equal(0, h);
        Assert.Equal(1.0, s);
        Assert.Equal(1.0, v);

        var convertedBack = ColorPickerDialog.HsvToRgb(h, s, v);
        Assert.Equal(pureRed.R, convertedBack.R);
        Assert.Equal(pureRed.G, convertedBack.G);
        Assert.Equal(pureRed.B, convertedBack.B);

        // Test Green
        var pureGreen = Color.FromRgb(0, 255, 0);
        ColorPickerDialog.RgbToHsv(pureGreen.R, pureGreen.G, pureGreen.B, out h, out s, out v);
        Assert.Equal(120, h, 0.1);
        Assert.Equal(1.0, s);
        Assert.Equal(1.0, v);

        // Test Blue
        var pureBlue = Color.FromRgb(0, 0, 255);
        ColorPickerDialog.RgbToHsv(pureBlue.R, pureBlue.G, pureBlue.B, out h, out s, out v);
        Assert.Equal(240, h, 0.1);
        Assert.Equal(1.0, s);
        Assert.Equal(1.0, v);
    }

    [Fact]
    public void ColorPickerDialog_STA_InitializesAndSelectsColors()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var dialog = new ColorPickerDialog("#EF4444", "🎨 Test Custom Color");
                Assert.NotNull(dialog);
                Assert.Equal("#EF4444", dialog.SelectedColorHex);

                // Simulate entering a custom HEX
                dialog.HexInputTextBox.Text = "#10B981";
                Assert.Equal("#10B981", dialog.SelectedColorHex);

                // Simulate Reset to Default
                dialog.ResetDefaultButton.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Assert.Equal(ProjectColorHelper.DefaultProjectColorHex, dialog.SelectedColorHex);

                dialog.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void PieChartControl_STA_RespectsCustomSliceColorsForProjectsAndActivities()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var customBrush1 = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // #EF4444
                var customBrush2 = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981

                var chart = new Controls.PieChartControl();
                var slices = new List<Controls.PieSliceItem>
                {
                    new Controls.PieSliceItem
                    {
                        Label = "Blender",
                        Value = 3600,
                        Color = customBrush1
                    },
                    new Controls.PieSliceItem
                    {
                        Label = "Curragh Racer",
                        Value = 7200,
                        Color = customBrush2
                    }
                };

                chart.SetData(slices, "Test Custom Colors");

                // Slices must retain their assigned colors
                Assert.Equal(2, chart.Slices.Count);
                Assert.Equal(customBrush1, chart.Slices[0].Color);
                Assert.Equal(customBrush2, chart.Slices[1].Color);
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void MainWindow_STA_ProjectAndActivityComboBoxes_AreNonEditable()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                FocusCompanionStorage.Save(new WorkRestSettings { WelcomePopupEnabled = false, LastWelcomePopupDate = DateTime.Today });

                var window = new MainWindow();
                Assert.False(window.ProjectComboBox.IsEditable, "ProjectComboBox must not be editable for new time entries");
                Assert.False(window.ActivityComboBox.IsEditable, "ActivityComboBox must not be editable for new time entries");
                window.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void EditTaskDialog_STA_PopulatesProjectAndActivityDropdowns_WithProperModelsAndColors()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var entry = new TimeEntry
                {
                    ProjectName = "Game Engine",
                    Activity = "Coding",
                    Description = "Implementing feature",
                    StartTime = DateTime.Today.AddHours(9),
                    EndTime = DateTime.Today.AddHours(11)
                };

                var projects = new List<Project>
                {
                    new Project { Name = "Game Engine", IsActive = true, ColorHex = "#2563EB" },
                    new Project { Name = "Web Portal", IsActive = true, ColorHex = "#10B981" },
                    new Project { Name = "Old Archive", IsActive = false }
                };

                var activities = new List<ActivityItem>
                {
                    new ActivityItem { Name = "Coding", IsActive = true, ColorHex = "#3B82F6" },
                    new ActivityItem { Name = "Design", IsActive = true, ColorHex = "#EC4899" },
                    new ActivityItem { Name = "Retired Act", IsActive = false }
                };

                var dialog = new EditTaskDialog(entry, projects, activities);

                // 1. ProjectComboBox ItemsSource
                var projectItems = Assert.IsAssignableFrom<IEnumerable<Project>>(dialog.ProjectComboBox.ItemsSource)?.ToList();
                Assert.NotNull(projectItems);
                Assert.Equal(2, projectItems.Count);
                Assert.Equal("Game Engine", projectItems[0].Name);
                Assert.NotNull(projectItems[0].Brush);

                var selectedProject = Assert.IsType<Project>(dialog.ProjectComboBox.SelectedItem);
                Assert.Equal("Game Engine", selectedProject.Name);
                Assert.Equal("Game Engine", dialog.ProjectComboBox.Text);

                // 2. ActivityComboBox ItemsSource
                var activityItems = Assert.IsAssignableFrom<IEnumerable<ActivityItem>>(dialog.ActivityComboBox.ItemsSource)?.ToList();
                Assert.NotNull(activityItems);
                Assert.Equal(2, activityItems.Count);
                Assert.Equal("Coding", activityItems[0].Name);
                Assert.NotNull(activityItems[0].Brush);

                var selectedActivity = Assert.IsType<ActivityItem>(dialog.ActivityComboBox.SelectedItem);
                Assert.Equal("Coding", selectedActivity.Name);
                Assert.Equal("Coding", dialog.ActivityComboBox.Text);

                // 3. Inputs
                Assert.Equal("Implementing feature", dialog.DescriptionTextBox.Text);

                dialog.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }

    [Fact]
    public void EditTaskDialog_STA_HandlesInactiveOrCustomProjectAndActivity_Gracefully()
    {
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var entry = new TimeEntry
                {
                    ProjectName = "Custom One-off Project",
                    Activity = "Special Task",
                    Description = "Custom entry",
                    StartTime = DateTime.Today.AddHours(14),
                    EndTime = DateTime.Today.AddHours(15)
                };

                var projects = new List<Project>
                {
                    new Project { Name = "Game Engine", IsActive = true }
                };

                var activities = new List<ActivityItem>
                {
                    new ActivityItem { Name = "Coding", IsActive = true }
                };

                var dialog = new EditTaskDialog(entry, projects, activities);

                // Should insert custom project into items and select it
                var projectItems = Assert.IsAssignableFrom<IEnumerable<Project>>(dialog.ProjectComboBox.ItemsSource)?.ToList();
                Assert.NotNull(projectItems);
                Assert.Contains(projectItems, p => p.Name == "Custom One-off Project");
                Assert.Equal("Custom One-off Project", (dialog.ProjectComboBox.SelectedItem as Project)?.Name);

                // Should insert custom activity into items and select it
                var activityItems = Assert.IsAssignableFrom<IEnumerable<ActivityItem>>(dialog.ActivityComboBox.ItemsSource)?.ToList();
                Assert.NotNull(activityItems);
                Assert.Contains(activityItems, a => a.Name == "Special Task");
                Assert.Equal("Special Task", (dialog.ActivityComboBox.SelectedItem as ActivityItem)?.Name);

                dialog.Close();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test thread threw an exception", threadException);
        }
    }
}
