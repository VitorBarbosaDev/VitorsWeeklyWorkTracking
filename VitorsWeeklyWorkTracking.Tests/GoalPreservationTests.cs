using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

[Collection("StorageTests")]
public class GoalPreservationTests : IDisposable
{
    private readonly string _testFolder;

    public GoalPreservationTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), "VitorsTest_GoalPreservation_" + Guid.NewGuid().ToString("N"));
        StoragePathHelper.OverrideAppDataFolder = _testFolder;
        Directory.CreateDirectory(_testFolder);
    }

    public void Dispose()
    {
        StoragePathHelper.OverrideAppDataFolder = null;
        if (Directory.Exists(_testFolder))
        {
            Directory.Delete(_testFolder, true);
        }
    }

    [Fact]
    public void Load_DoesNotRemoveDummyProjectIfUsedInPlans()
    {
        // Setup: A project that is considered "dummy" (e.g., General Freelance)
        // It is in the projects list but has NO custom color.
        // BUT it is used in a plan.
        
        var projectsFile = StoragePathHelper.GetFilePath("projects.json");
        var plansFile = StoragePathHelper.GetFilePath("calendar-plans.json");
        
        var initialProjects = new List<Project>
        {
            new Project { Name = "General Freelance", IsActive = true, ColorHex = null },
            new Project { Name = "Real Project", IsActive = true, ColorHex = "#FF0000" }
        };
        File.WriteAllText(projectsFile, JsonSerializer.Serialize(initialProjects));
        
        var initialPlans = new List<PlannedWorkItem>
        {
            new PlannedWorkItem { ProjectName = "General Freelance", Date = DateTime.Today, PlannedHours = 2.0 },
            new PlannedWorkItem { ProjectName = "Real Project", Date = DateTime.Today, PlannedHours = 1.0 }
        };
        File.WriteAllText(plansFile, JsonSerializer.Serialize(initialPlans));
        
        // Act: Load projects
        var storage = new ProjectStorage();
        var loaded = storage.Load();
        
        // Assert: "General Freelance" should still be there because it is used in plans
        Assert.Contains(loaded, p => p.Name == "General Freelance");
    }

    [Fact]
    public void Load_DoesNotRemoveDummyActivityIfUsedInPlans()
    {
        // Setup: An activity that is considered "dummy" (e.g., Coding)
        // It is in the activities list but has NO custom color.
        // BUT it is used in a plan.
        
        var activitiesFile = StoragePathHelper.GetFilePath("activities.json");
        var plansFile = StoragePathHelper.GetFilePath("calendar-plans.json");
        
        var initialActivities = new List<ActivityItem>
        {
            new ActivityItem { Name = "Coding", IsActive = true, ColorHex = null },
            new ActivityItem { Name = "Real Activity", IsActive = true, ColorHex = "#00FF00" }
        };
        File.WriteAllText(activitiesFile, JsonSerializer.Serialize(initialActivities));
        
        var initialPlans = new List<PlannedWorkItem>
        {
            new PlannedWorkItem { Activity = "Coding", Date = DateTime.Today, PlannedHours = 1.0 },
            new PlannedWorkItem { Activity = "Real Activity", Date = DateTime.Today, PlannedHours = 0.5 }
        };
        File.WriteAllText(plansFile, JsonSerializer.Serialize(initialPlans));
        
        // Act: Load activities
        var storage = new ActivityStorage();
        var loaded = storage.Load();
        
        // Assert: "Coding" should still be there because it is used in plans
        Assert.Contains(loaded, a => a.Name == "Coding");
    }
}
