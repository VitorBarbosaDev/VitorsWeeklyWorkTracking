using System;
using System.Linq;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class CoffeeJazzSceneTests
{
    [Fact]
    public void AvailableScenes_ContainsCoffeeJazzScene()
    {
        var scene = ArtSceneOption.AvailableScenes.FirstOrDefault(s => s.Id == "coffeejazz");

        Assert.NotNull(scene);
        Assert.Equal("coffeejazz", scene.Id);
        Assert.Equal("Coffee Jazz Window", scene.Name);
        Assert.Equal("🎷", scene.Icon);
    }

    [Fact]
    public void AsciiArtEngine_RendersCoffeeJazz_RestPhase()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "coffeejazz",
            frameTick: 0,
            progressFraction: 0.5,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: true,
            contextDetails: "Jazz Break");

        Assert.NotNull(rendered);
        Assert.Contains("JAZZ BREAK", rendered.AsciiArt);
        Assert.Equal("🎷 JAZZ BREAK", rendered.BadgeText);
        Assert.Contains("latte", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("jazz", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersCoffeeJazz_ActiveTracking()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "coffeejazz",
            frameTick: 0,
            progressFraction: 0.60,
            isTracking: true,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Jazz Flow");

        Assert.NotNull(rendered);
        Assert.Equal("🎷 60% JAZZ FLOW", rendered.BadgeText);
        Assert.Contains("60%", rendered.StoryText);
        Assert.Contains("latte", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersCoffeeJazz_GoalReached()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "coffeejazz",
            frameTick: 0,
            progressFraction: 1.0,
            isTracking: false,
            isGoalReached: true,
            isRestPhase: false,
            contextDetails: "Target Reached");

        Assert.NotNull(rendered);
        Assert.Equal("🎷 TARGET REACHED!", rendered.BadgeText);
        Assert.Contains("sunset", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("completed", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersCoffeeJazz_InitialIdle()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "coffeejazz",
            frameTick: 0,
            progressFraction: 0.0,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Ready");

        Assert.NotNull(rendered);
        Assert.Equal("🎷 JAZZ READY", rendered.BadgeText);
        Assert.Contains("latte", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0.15, "15%")]
    [InlineData(0.50, "50%")]
    [InlineData(0.85, "85%")]
    public void AsciiArtEngine_RendersCoffeeJazz_ProgressStages(double progress, string expectedText)
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "coffeejazz",
            frameTick: 10,
            progressFraction: progress,
            isTracking: true,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Focus");

        Assert.NotNull(rendered);
        Assert.Contains(expectedText, rendered.BadgeText);
        Assert.Contains("latte", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersCoffeeJazz_AnimatedFrames()
    {
        var frame1 = AsciiArtEngine.Render("coffeejazz", frameTick: 0, 0.5, true, false, false, "Jazz Flow");
        var frame2 = AsciiArtEngine.Render("coffeejazz", frameTick: 50, 0.5, true, false, false, "Jazz Flow");

        Assert.NotNull(frame1);
        Assert.NotNull(frame2);
        Assert.Equal("🎷 50% JAZZ FLOW", frame1.BadgeText);
        Assert.Equal("🎷 50% JAZZ FLOW", frame2.BadgeText);
    }
}
