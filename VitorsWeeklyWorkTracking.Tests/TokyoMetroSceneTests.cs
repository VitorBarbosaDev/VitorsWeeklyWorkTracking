using System;
using System.Linq;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class TokyoMetroSceneTests
{
    [Fact]
    public void AvailableScenes_ContainsMetroScene()
    {
        var scene = ArtSceneOption.AvailableScenes.FirstOrDefault(s => s.Id == "metro");

        Assert.NotNull(scene);
        Assert.Equal("metro", scene.Id);
        Assert.Equal("Tokyo Metro Lo-Fi Girl", scene.Name);
        Assert.Equal("🎧", scene.Icon);
    }

    [Fact]
    public void AsciiArtEngine_RendersMetro_RestPhase()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "metro",
            frameTick: 0,
            progressFraction: 0.5,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: true,
            contextDetails: "Tokyo Tea Break");

        Assert.NotNull(rendered);
        Assert.Contains("YAMANOTE LINE", rendered.AsciiArt);
        Assert.Equal("🎧 METRO REST", rendered.BadgeText);
        Assert.Contains("lo-fi", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersMetro_ActiveTracking()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "metro",
            frameTick: 0,
            progressFraction: 0.75,
            isTracking: true,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Express Focus");

        Assert.NotNull(rendered);
        Assert.Equal("🎧 75% EXPRESS", rendered.BadgeText);
        Assert.Contains("75%", rendered.StoryText);
        Assert.Contains("skyline", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersMetro_GoalReached()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "metro",
            frameTick: 0,
            progressFraction: 1.0,
            isTracking: false,
            isGoalReached: true,
            isRestPhase: false,
            contextDetails: "Destination Reached");

        Assert.NotNull(rendered);
        Assert.Equal("🚉 ARRIVED!", rendered.BadgeText);
        Assert.Contains("destination", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersMetro_InitialIdle()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "metro",
            frameTick: 0,
            progressFraction: 0.0,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Ready");

        Assert.NotNull(rendered);
        Assert.Equal("🎧 READY TO RIDE", rendered.BadgeText);
        Assert.Contains("Tokyo Metro", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0.20, "20%")]
    [InlineData(0.50, "50%")]
    [InlineData(0.90, "90%")]
    public void AsciiArtEngine_RendersMetro_ProgressStages(double progress, string expectedText)
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "metro",
            frameTick: 12,
            progressFraction: progress,
            isTracking: true,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Focus");

        Assert.NotNull(rendered);
        Assert.Contains(expectedText, rendered.BadgeText);
    }
}
