using System;
using System.Linq;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class CyclingSceneTests
{
    [Fact]
    public void AvailableScenes_ContainsCyclingScene()
    {
        var scene = ArtSceneOption.AvailableScenes.FirstOrDefault(s => s.Id == "cycling");

        Assert.NotNull(scene);
        Assert.Equal("cycling", scene.Id);
        Assert.Equal("Boy Cycling Home", scene.Name);
        Assert.Equal("🚴", scene.Icon);
    }

    [Fact]
    public void AsciiArtEngine_RendersCycling_RestPhase()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "cycling",
            frameTick: 0,
            progressFraction: 0.5,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: true,
            contextDetails: "Rest Break");

        Assert.NotNull(rendered);
        Assert.Contains("Rest Break", rendered.AsciiArt);
        Assert.Equal("☕ REST BREAK", rendered.BadgeText);
        Assert.Contains("relaxing", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersCycling_ActiveTracking()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "cycling",
            frameTick: 0,
            progressFraction: 0.45,
            isTracking: true,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Focus Journey");

        Assert.NotNull(rendered);
        Assert.Equal("🚴 45% TO HOME", rendered.BadgeText);
        Assert.Contains("45%", rendered.StoryText);
    }

    [Fact]
    public void AsciiArtEngine_RendersCycling_GoalReached()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "cycling",
            frameTick: 0,
            progressFraction: 1.0,
            isTracking: false,
            isGoalReached: true,
            isRestPhase: false,
            contextDetails: "Home");

        Assert.NotNull(rendered);
        Assert.Equal("🏠 ARRIVED HOME!", rendered.BadgeText);
        Assert.Contains("HOME", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersCycling_GoalReached_AnimatedFrames()
    {
        var frame1 = AsciiArtEngine.Render(
            sceneId: "cycling",
            frameTick: 0,
            progressFraction: 1.0,
            isTracking: false,
            isGoalReached: true,
            isRestPhase: false,
            contextDetails: "Home Celebration");

        var frame2 = AsciiArtEngine.Render(
            sceneId: "cycling",
            frameTick: 30,
            progressFraction: 1.0,
            isTracking: false,
            isGoalReached: true,
            isRestPhase: false,
            contextDetails: "Home Celebration");

        Assert.NotNull(frame1);
        Assert.NotNull(frame2);
        Assert.Equal("🏠 ARRIVED HOME!", frame1.BadgeText);
        Assert.Equal("🏠 ARRIVED HOME!", frame2.BadgeText);
        Assert.False(string.IsNullOrWhiteSpace(frame1.AsciiArt));
        Assert.False(string.IsNullOrWhiteSpace(frame2.AsciiArt));
    }
    [Fact]
    public void AsciiArtEngine_RendersCycling_InitialIdle()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "cycling",
            frameTick: 0,
            progressFraction: 0.0,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Ready");

        Assert.NotNull(rendered);
        Assert.Equal("🚴 READY TO RIDE", rendered.BadgeText);
        Assert.Contains("starting point", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }
}
