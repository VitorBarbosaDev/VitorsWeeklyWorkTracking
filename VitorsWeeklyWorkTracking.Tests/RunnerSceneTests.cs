using System;
using System.Linq;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class RunnerSceneTests
{
    [Fact]
    public void AvailableScenes_ContainsRunnerScene()
    {
        var scene = ArtSceneOption.AvailableScenes.FirstOrDefault(s => s.Id == "runner");

        Assert.NotNull(scene);
        Assert.Equal("runner", scene.Id);
        Assert.Equal("Chibi Marathon Runner", scene.Name);
        Assert.Equal("🏃", scene.Icon);
    }

    [Fact]
    public void AsciiArtEngine_RendersRunner_RestPhase()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "runner",
            frameTick: 0,
            progressFraction: 0.5,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: true,
            contextDetails: "Rest Break");

        Assert.NotNull(rendered);
        Assert.Contains("HYDRATION", rendered.AsciiArt);
        Assert.Equal("🥤 RECOVERY REST", rendered.BadgeText);
        Assert.Contains("hydrating", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersRunner_GoalReached()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "runner",
            frameTick: 0,
            progressFraction: 1.0,
            isTracking: false,
            isGoalReached: true,
            isRestPhase: false,
            contextDetails: "Focus Marathon");

        Assert.NotNull(rendered);
        Assert.Contains("FINISH LINE", rendered.AsciiArt);
        Assert.Equal("🏆 FINISH LINE!", rendered.BadgeText);
        Assert.Contains("CHAMPION", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersRunner_ActiveTracking()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "runner",
            frameTick: 0,
            progressFraction: 0.75,
            isTracking: true,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Sprinting");

        Assert.NotNull(rendered);
        Assert.Equal("🏃 75% SPRINT", rendered.BadgeText);
        Assert.Contains("75%", rendered.StoryText);
    }

    [Fact]
    public void AsciiArtEngine_RendersRunner_InitialIdle()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "runner",
            frameTick: 0,
            progressFraction: 0.0,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Idle");

        Assert.NotNull(rendered);
        Assert.Equal("🏃 READY TO SPRINT", rendered.BadgeText);
        Assert.Contains("blocks", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }
}
