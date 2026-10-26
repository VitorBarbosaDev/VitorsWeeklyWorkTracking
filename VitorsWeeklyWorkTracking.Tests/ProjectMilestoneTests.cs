using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

[Collection("StorageTests")]
public class ProjectMilestoneTests
{
    private static (string tempDir, string dataDir) SetupSandbox()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "MilestoneTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        StoragePathHelper.OverrideAppDataFolder = tempDir;
        var dataDir = StoragePathHelper.GetDataFolder();
        return (tempDir, dataDir);
    }

    private static void CleanupSandbox(string tempDir)
    {
        StoragePathHelper.OverrideAppDataFolder = null;
        if (Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup lock issues on temp dirs
            }
        }
    }

    [Fact]
    public void ProjectMilestone_CountdownAndUrgencyCalculations_AreAccurate()
    {
        // 1. Future milestone (29 days remaining)
        var m29 = new ProjectMilestone
        {
            Title = "Demo Release",
            ProjectName = "Game Engine",
            TargetDate = DateTime.Today.AddDays(29)
        };
        Assert.Equal(29, m29.DaysRemaining);
        Assert.Equal("29 days remaining", m29.CountdownText);
        Assert.Equal("29 days remaining till Demo Release", m29.DailyReminderText);
        Assert.Equal("Upcoming", m29.UrgencyLevel);
        Assert.Contains("29d left", m29.UrgencyBadgeText);

        // 2. Soon milestone (5 days remaining)
        var m5 = new ProjectMilestone
        {
            Title = "Alpha Feature Freeze",
            ProjectName = "Game Engine",
            TargetDate = DateTime.Today.AddDays(5)
        };
        Assert.Equal(5, m5.DaysRemaining);
        Assert.Equal("5 days remaining", m5.CountdownText);
        Assert.Equal("5 days remaining till Alpha Feature Freeze", m5.DailyReminderText);
        Assert.Equal("Soon", m5.UrgencyLevel);

        // 3. Tomorrow (1 day remaining)
        var m1 = new ProjectMilestone
        {
            Title = "Code Review",
            ProjectName = "Core Library",
            TargetDate = DateTime.Today.AddDays(1)
        };
        Assert.Equal(1, m1.DaysRemaining);
        Assert.Equal("1 day remaining (tomorrow!)", m1.CountdownText);
        Assert.Equal("1 day remaining till Code Review (tomorrow)", m1.DailyReminderText);
        Assert.Equal("Tomorrow", m1.UrgencyLevel);
        Assert.Equal("⏰ Tomorrow", m1.UrgencyBadgeText);

        // 4. Due Today (0 days)
        var mToday = new ProjectMilestone
        {
            Title = "Client Demo",
            ProjectName = "Web Portal",
            TargetDate = DateTime.Today
        };
        Assert.Equal(0, mToday.DaysRemaining);
        Assert.Equal("Due today!", mToday.CountdownText);
        Assert.Equal("Due today till Client Demo!", mToday.DailyReminderText);
        Assert.Equal("Today", mToday.UrgencyLevel);
        Assert.Equal("🔥 Due Today", mToday.UrgencyBadgeText);

        // 5. Overdue (3 days overdue)
        var mOverdue = new ProjectMilestone
        {
            Title = "Submission Deadline",
            ProjectName = "Contest Entry",
            TargetDate = DateTime.Today.AddDays(-3)
        };
        Assert.Equal(-3, mOverdue.DaysRemaining);
        Assert.Equal("3 days overdue", mOverdue.CountdownText);
        Assert.Equal("3 days overdue for Submission Deadline", mOverdue.DailyReminderText);
        Assert.Equal("Overdue", mOverdue.UrgencyLevel);
        Assert.Equal("🚨 3d Overdue", mOverdue.UrgencyBadgeText);

        // 6. Completed
        var mCompleted = new ProjectMilestone
        {
            Title = "Initial Kickoff",
            ProjectName = "Game Engine",
            TargetDate = DateTime.Today.AddDays(-10),
            IsCompleted = true,
            CompletedAt = DateTime.Today.AddDays(-10)
        };
        Assert.Equal("Completed", mCompleted.CountdownText);
        Assert.Equal("Initial Kickoff (Completed)", mCompleted.DailyReminderText);
        Assert.Equal("Completed", mCompleted.UrgencyLevel);
        Assert.Equal("✅ Completed", mCompleted.UrgencyBadgeText);
    }

    [Fact]
    public void MilestoneService_AddUpdateDeleteAndQuery_WorksCorrectly()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            var projectStorage = new ProjectStorage();
            projectStorage.Save(new List<Project>
            {
                new Project { Name = "Game Engine", IsActive = true },
                new Project { Name = "Web Portal", IsActive = true }
            });

            var service = new MilestoneService(projectStorage);

            bool eventFired = false;
            service.MilestonesChanged += () => eventFired = true;

            // 1. Add Milestone
            var m1 = new ProjectMilestone
            {
                ProjectName = "Game Engine",
                Title = "Demo Release",
                TargetDate = DateTime.Today.AddDays(29),
                Description = "Initial playable demo"
            };
            service.AddMilestone(m1);
            Assert.True(eventFired);

            var all = service.GetAllMilestones();
            Assert.Single(all);
            Assert.Equal("Demo Release", all[0].Title);
            Assert.Equal("Game Engine", all[0].ProjectName);

            // 2. Add second milestone to same project
            eventFired = false;
            var m2 = new ProjectMilestone
            {
                ProjectName = "Game Engine",
                Title = "Final Launch",
                TargetDate = DateTime.Today.AddDays(90)
            };
            service.AddMilestone(m2);
            Assert.True(eventFired);

            var gameMilestones = service.GetMilestonesForProject("Game Engine");
            Assert.Equal(2, gameMilestones.Count);

            // 3. Update milestone
            eventFired = false;
            m1.Title = "Demo Release v1.0";
            m1.TargetDate = DateTime.Today.AddDays(30);
            bool updated = service.UpdateMilestone(m1);
            Assert.True(updated);
            Assert.True(eventFired);

            var retrieved = service.GetMilestoneById(m1.Id);
            Assert.NotNull(retrieved);
            Assert.Equal("Demo Release v1.0", retrieved.Title);
            Assert.Equal(30, retrieved.DaysRemaining);

            // 4. Toggle completion
            eventFired = false;
            bool toggled = service.ToggleMilestoneCompletion(m1.Id);
            Assert.True(toggled);
            Assert.True(eventFired);
            retrieved = service.GetMilestoneById(m1.Id);
            Assert.NotNull(retrieved);
            Assert.True(retrieved.IsCompleted);
            Assert.NotNull(retrieved.CompletedAt);

            // 5. Delete milestone
            eventFired = false;
            bool deleted = service.DeleteMilestone(m2.Id);
            Assert.True(deleted);
            Assert.True(eventFired);

            var remaining = service.GetAllMilestones();
            Assert.Single(remaining);
            Assert.Equal(m1.Id, remaining[0].Id);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void MilestoneService_QueueMode_ReturnsOnlyNextMilestoneUntilCompleted()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            var projectStorage = new ProjectStorage();
            projectStorage.Save(new List<Project>
            {
                new Project { Name = "RPG Project", IsActive = true, ShowOnlyNextMilestone = true },
                new Project { Name = "Mobile App", IsActive = true, ShowOnlyNextMilestone = true }
            });

            var service = new MilestoneService(projectStorage);

            // Add 3 milestones for RPG Project
            var rpgDemo = new ProjectMilestone
            {
                ProjectName = "RPG Project",
                Title = "Demo Release",
                TargetDate = DateTime.Today.AddDays(29),
                Order = 0
            };
            var rpgBeta = new ProjectMilestone
            {
                ProjectName = "RPG Project",
                Title = "Closed Beta",
                TargetDate = DateTime.Today.AddDays(60),
                Order = 1
            };
            var rpgLaunch = new ProjectMilestone
            {
                ProjectName = "RPG Project",
                Title = "Full Release",
                TargetDate = DateTime.Today.AddDays(120),
                Order = 2
            };

            service.AddMilestone(rpgDemo);
            service.AddMilestone(rpgBeta);
            service.AddMilestone(rpgLaunch);

            // Add 2 milestones for Mobile App
            var appAlpha = new ProjectMilestone
            {
                ProjectName = "Mobile App",
                Title = "Alpha Build",
                TargetDate = DateTime.Today.AddDays(14),
                Order = 0
            };
            var appStore = new ProjectMilestone
            {
                ProjectName = "Mobile App",
                Title = "Store Submission",
                TargetDate = DateTime.Today.AddDays(45),
                Order = 1
            };

            service.AddMilestone(appAlpha);
            service.AddMilestone(appStore);

            // 1. In Queue Mode (forceNextOnly = true):
            // Exactly 2 milestones should be returned (1 per project: Alpha Build and Demo Release)
            var queuedReminders = service.GetActiveReminders(forceNextOnly: true);
            Assert.Equal(2, queuedReminders.Count);
            Assert.Equal("Alpha Build", queuedReminders[0].Title); // 14 days
            Assert.Equal("Demo Release", queuedReminders[1].Title); // 29 days

            // 2. In Show All Mode (forceNextOnly = false):
            // All 5 milestones should be returned
            var allReminders = service.GetActiveReminders(forceNextOnly: false);
            Assert.Equal(5, allReminders.Count);

            // 3. Mark Mobile App Alpha Build as completed
            service.ToggleMilestoneCompletion(appAlpha.Id);

            // Now in Queue Mode, Mobile App automatically advances to Store Submission!
            queuedReminders = service.GetActiveReminders(forceNextOnly: true);
            Assert.Equal(2, queuedReminders.Count);
            Assert.Equal("Demo Release", queuedReminders[0].Title); // 29 days (RPG Project)
            Assert.Equal("Store Submission", queuedReminders[1].Title); // 45 days (Mobile App)

            // 4. Mark RPG Demo Release as completed
            service.ToggleMilestoneCompletion(rpgDemo.Id);

            // Now in Queue Mode, RPG Project automatically advances to Closed Beta!
            queuedReminders = service.GetActiveReminders(forceNextOnly: true);
            Assert.Equal(2, queuedReminders.Count);
            Assert.Equal("Store Submission", queuedReminders[0].Title); // 45 days (Mobile App)
            Assert.Equal("Closed Beta", queuedReminders[1].Title); // 60 days (RPG Project)
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void MilestoneService_ReorderMilestones_UpdatesQueueOrder()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            var projectStorage = new ProjectStorage();
            projectStorage.Save(new List<Project>
            {
                new Project { Name = "Website", IsActive = true }
            });

            var service = new MilestoneService(projectStorage);

            var m1 = new ProjectMilestone { ProjectName = "Website", Title = "Wireframes", TargetDate = DateTime.Today.AddDays(10), Order = 0 };
            var m2 = new ProjectMilestone { ProjectName = "Website", Title = "Prototypes", TargetDate = DateTime.Today.AddDays(20), Order = 1 };
            var m3 = new ProjectMilestone { ProjectName = "Website", Title = "Deployment", TargetDate = DateTime.Today.AddDays(30), Order = 2 };

            service.AddMilestone(m1);
            service.AddMilestone(m2);
            service.AddMilestone(m3);

            // Move Deployment up by 1 position (should become index 1)
            bool moved = service.ReorderMilestone("Website", m3.Id, -1);
            Assert.True(moved);

            var ordered = service.GetMilestonesForProject("Website");
            Assert.Equal("Wireframes", ordered[0].Title);
            Assert.Equal("Deployment", ordered[1].Title);
            Assert.Equal("Prototypes", ordered[2].Title);

            // Move Wireframes down by 1 position
            moved = service.ReorderMilestone("Website", m1.Id, 1);
            Assert.True(moved);

            ordered = service.GetMilestonesForProject("Website");
            Assert.Equal("Deployment", ordered[0].Title);
            Assert.Equal("Wireframes", ordered[1].Title);
            Assert.Equal("Prototypes", ordered[2].Title);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void ProjectPersistence_PreservesMilestonesAndQueueSettings()
    {
        var (tempDir, dataDir) = SetupSandbox();
        try
        {
            var projectStorage = new ProjectStorage();
            var originalProjects = new List<Project>
            {
                new Project
                {
                    Name = "Game Architecture",
                    IsActive = true,
                    ShowOnlyNextMilestone = false,
                    Milestones = new List<ProjectMilestone>
                    {
                        new ProjectMilestone
                        {
                            ProjectName = "Game Architecture",
                            Title = "Alpha Target 1",
                            TargetDate = DateTime.Today.AddDays(15),
                            Order = 0,
                            Description = "First target"
                        },
                        new ProjectMilestone
                        {
                            ProjectName = "Game Architecture",
                            Title = "Alpha Target 2",
                            TargetDate = DateTime.Today.AddDays(45),
                            Order = 1,
                            IsCompleted = true
                        }
                    }
                }
            };

            projectStorage.Save(originalProjects);

            // Reload from storage
            var loaded = projectStorage.Load();
            Assert.Single(loaded);
            Assert.Equal("Game Architecture", loaded[0].Name);
            Assert.False(loaded[0].ShowOnlyNextMilestone);
            Assert.Equal(2, loaded[0].Milestones.Count);
            Assert.Equal("Alpha Target 1", loaded[0].Milestones[0].Title);
            Assert.Equal("Alpha Target 2", loaded[0].Milestones[1].Title);
            Assert.True(loaded[0].Milestones[1].IsCompleted);
        }
        finally
        {
            CleanupSandbox(tempDir);
        }
    }

    [Fact]
    public void ProjectMilestone_UIViewsAndTheming_RenderAndColorCodeAccurately()
    {
        var (tempDir, dataDir) = SetupSandbox();
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                    ThemeManager.Initialize();
                }

                ThemeManager.ApplyTheme(AppTheme.ModernLight);
                FocusCompanionStorage.Save(new WorkRestSettings { WelcomePopupEnabled = false, LastWelcomePopupDate = DateTime.Today });

                var projectStorage = new ProjectStorage();
                var sampleProjects = new List<Project>
                {
                    new Project
                    {
                        Name = "Game Engine",
                        IsActive = true,
                        Milestones = new List<ProjectMilestone>
                        {
                            new ProjectMilestone
                            {
                                Title = "Alpha Release",
                                TargetDate = DateTime.Today.AddDays(1), // 1 day left -> Red
                                IsCompleted = false
                            }
                        }
                    },
                    new Project { Name = "Web Portal", IsActive = true }
                };
                projectStorage.Save(sampleProjects);

                // 1. WelcomeBackDialog with Milestones
                var milestones = new List<ProjectMilestone>
                {
                    new ProjectMilestone
                    {
                        ProjectName = "Game Engine",
                        Title = "Demo Release",
                        TargetDate = DateTime.Today.AddDays(29)
                    },
                    new ProjectMilestone
                    {
                        ProjectName = "Web Portal",
                        Title = "Client Review",
                        TargetDate = DateTime.Today.AddDays(1)
                    }
                };

                var goals = new List<PlannedWorkItem>
                {
                    new PlannedWorkItem
                    {
                        ProjectName = "Game Engine",
                        PlannedHours = 3.5,
                        Date = DateTime.Today,
                        Note = "Implement shader system"
                    }
                };

                var welcomeDialog = new WelcomeBackDialog("Vitor", goals, milestones);
                Assert.Equal(System.Windows.Visibility.Visible, welcomeDialog.MilestonesPanel.Visibility);
                Assert.Equal(System.Windows.Visibility.Visible, welcomeDialog.GoalsPanel.Visibility);
                Assert.Equal("2 upcoming", welcomeDialog.MilestonesCountTextBlock.Text);
                welcomeDialog.Close();

                // 2. EditMilestoneDialog (New & Existing)
                var newDlg = new EditMilestoneDialog("Game Engine", DateTime.Today.AddDays(29), sampleProjects);
                Assert.Equal("Add Project Milestone", newDlg.HeaderTitleTextBlock.Text);
                Assert.Contains("29 days remaining", newDlg.CountdownPreviewTextBlock.Text);
                Assert.Equal("Mark as completed", newDlg.CompletedCheckBox.Content);
                Assert.DoesNotContain("✅", newDlg.CompletedCheckBox.Content.ToString());
                Assert.DoesNotContain("✔", newDlg.CompletedCheckBox.Content.ToString());
                newDlg.Close();

                var existingDlg = new EditMilestoneDialog(sampleProjects[0].Milestones[0], sampleProjects);
                Assert.Equal("Edit Project Milestone", existingDlg.HeaderTitleTextBlock.Text);
                Assert.Equal("Alpha Release", existingDlg.TitleTextBox.Text);
                Assert.Contains("1 day remaining", existingDlg.CountdownPreviewTextBlock.Text);
                // 1 day remaining preview must be Red
                if (existingDlg.CountdownPreviewTextBlock.Foreground is System.Windows.Media.SolidColorBrush scb1)
                {
                    Assert.True(scb1.Color.R > 200, "1 day remaining preview must be Red");
                }
                existingDlg.Close();

                // 3. EditMilestoneDialog Countdown Preview with different dates
                var overdueDlg = new EditMilestoneDialog("Game Engine", DateTime.Today.AddDays(-2), sampleProjects);
                Assert.Contains("overdue", overdueDlg.CountdownPreviewTextBlock.Text);
                if (overdueDlg.CountdownPreviewTextBlock.Foreground is System.Windows.Media.SolidColorBrush scbOverdue)
                {
                    Assert.True(scbOverdue.Color.R > 200, "Overdue preview must be Red");
                }
                overdueDlg.Close();

                var todayDlg = new EditMilestoneDialog("Game Engine", DateTime.Today, sampleProjects);
                Assert.Contains("Due today", todayDlg.CountdownPreviewTextBlock.Text);
                if (todayDlg.CountdownPreviewTextBlock.Foreground is System.Windows.Media.SolidColorBrush scbToday)
                {
                    Assert.True(scbToday.Color.R > 200, "Due today preview must be Red");
                }
                todayDlg.Close();

                // 4. MainWindow Placement, Card Appearance & Color Coding
                var mainWindow = new MainWindow();
                try
                {
                    // Card is at Row 7 (near bottom, right above KPI summary)
                    Assert.Equal(7, System.Windows.Controls.Grid.GetRow(mainWindow.ProjectMilestonesCard));
                    Assert.Equal(System.Windows.Visibility.Visible, mainWindow.ProjectMilestonesCard.Visibility);

                    // Cube structure
                    Assert.Single(mainWindow.ProjectMilestonesContainer.Children);
                    var cardBorder = Assert.IsType<System.Windows.Controls.Border>(mainWindow.ProjectMilestonesContainer.Children[0]);
                    Assert.Equal(205, cardBorder.Width);
                    Assert.True(cardBorder.MinHeight >= 115);

                    // 1-day remaining card border is Red
                    if (cardBorder.BorderBrush is System.Windows.Media.SolidColorBrush borderBrush)
                    {
                        Assert.True(borderBrush.Color.R > 200, "1-day remaining milestone card border must be Red");
                    }

                    // Button has no pre-ticked emoji
                    var innerGrid = Assert.IsType<System.Windows.Controls.Grid>(cardBorder.Child);
                    var completeBtn = innerGrid.Children.OfType<System.Windows.Controls.Button>().FirstOrDefault();
                    Assert.NotNull(completeBtn);
                    Assert.Equal("○ Mark Complete", completeBtn.Content);

                    // 5. Dynamic Theme Switching
                    ThemeManager.ApplyTheme(AppTheme.PitchBlack);
                    Assert.Single(mainWindow.ProjectMilestonesContainer.Children);
                    var pitchCard = Assert.IsType<System.Windows.Controls.Border>(mainWindow.ProjectMilestonesContainer.Children[0]);
                    Assert.NotNull(pitchCard.BorderBrush);

                    ThemeManager.ApplyTheme(AppTheme.CyberpunkSynthwave);
                    Assert.Single(mainWindow.ProjectMilestonesContainer.Children);
                    var cyberCard = Assert.IsType<System.Windows.Controls.Border>(mainWindow.ProjectMilestonesContainer.Children[0]);
                    Assert.NotNull(cyberCard.BorderBrush);

                    // 6. Complete Milestone -> Collapses Dashboard Card
                    MilestoneService.Instance.ToggleMilestoneCompletion(sampleProjects[0].Milestones[0].Id);
                    Assert.Equal(System.Windows.Visibility.Collapsed, mainWindow.ProjectMilestonesCard.Visibility);
                }
                finally
                {
                    mainWindow.Close();
                    ThemeManager.ApplyTheme(AppTheme.ModernLight);
                }
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
            finally
            {
                CleanupSandbox(tempDir);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadException != null)
        {
            throw new Exception("STA test execution failed", threadException);
        }
    }
}
