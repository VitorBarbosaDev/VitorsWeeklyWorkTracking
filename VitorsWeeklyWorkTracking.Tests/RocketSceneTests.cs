using System;
using System.Linq;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class RocketSceneTests
{
    [Fact]
    public void AvailableScenes_ContainsRocketScene()
    {
        var scene = ArtSceneOption.AvailableScenes.FirstOrDefault(s => s.Id == "rocket");

        Assert.NotNull(scene);
        Assert.Equal("rocket", scene.Id);
        Assert.Equal("Space Rocket Launch", scene.Name);
        Assert.Equal("🚀", scene.Icon);
    }

    [Fact]
    public void AsciiArtEngine_RendersRocket_RestPhase()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "rocket",
            frameTick: 0,
            progressFraction: 0.5,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: true,
            contextDetails: "Zero-G Chill Lounge");

        Assert.NotNull(rendered);
        Assert.Contains("Zero-G", rendered.AsciiArt);
        Assert.Equal("☕ ZERO-G REST", rendered.BadgeText);
    }

    [Fact]
    public void AsciiArtEngine_RendersRocket_ActiveTracking()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "rocket",
            frameTick: 0,
            progressFraction: 0.65,
            isTracking: true,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Deep Space Cruise");

        Assert.NotNull(rendered);
        Assert.Equal("🚀 65% IN ORBIT", rendered.BadgeText);
        Assert.Contains("65%", rendered.StoryText);
    }

    [Fact]
    public void AsciiArtEngine_RendersRocket_GoalReached()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "rocket",
            frameTick: 0,
            progressFraction: 1.0,
            isTracking: false,
            isGoalReached: true,
            isRestPhase: false,
            contextDetails: "Moon Landing");

        Assert.NotNull(rendered);
        Assert.Equal("🌕 MISSION COMPLETE!", rendered.BadgeText);
        Assert.Contains("Lunar", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }
}
