using System;
using System.Linq;
using VitorsWeeklyWorkTracking.Models;
using VitorsWeeklyWorkTracking.Services;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class LumberjackSceneTests
{
    [Fact]
    public void AvailableScenes_ContainsLumberjackScene()
    {
        var scene = ArtSceneOption.AvailableScenes.FirstOrDefault(s => s.Id == "lumberjack");

        Assert.NotNull(scene);
        Assert.Equal("lumberjack", scene.Id);
        Assert.Equal("Lumberjack Wood Chopping", scene.Name);
        Assert.Equal("🪓", scene.Icon);
        Assert.Contains("timber", scene.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("van", scene.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersLumberjack_InitialIdleState()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "lumberjack",
            frameTick: 0,
            progressFraction: 0.0,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Focus Session");

        Assert.NotNull(rendered);
        Assert.Contains("/|\\", rendered.AsciiArt);
        Assert.Contains("van", rendered.AsciiArt, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("🪓 READY TO CHOP", rendered.BadgeText);
        Assert.Contains("forest", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersLumberjack_ActiveTrackingProgress()
    {
        // 40% progress (2 trees chopped)
        var rendered = AsciiArtEngine.Render(
            sceneId: "lumberjack",
            frameTick: 2,
            progressFraction: 0.40,
            isTracking: true,
            isGoalReached: false,
            isRestPhase: false,
            contextDetails: "Development");

        Assert.NotNull(rendered);
        Assert.Contains("_|_", rendered.AsciiArt);
        Assert.Contains("40% CHOPPING", rendered.BadgeText);
        Assert.Contains("2/5 trees", rendered.StoryText);
        Assert.Contains("40%", rendered.MiniLine);
    }

    [Fact]
    public void AsciiArtEngine_RendersLumberjack_GoalReachedState()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "lumberjack",
            frameTick: 0,
            progressFraction: 1.0,
            isTracking: false,
            isGoalReached: true,
            isRestPhase: false,
            contextDetails: "Development");

        Assert.NotNull(rendered);
        Assert.Contains("TIMBER", rendered.AsciiArt);
        Assert.Contains("VAN FULL PACKED!", rendered.BadgeText);
        Assert.Contains("packed", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("home", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AsciiArtEngine_RendersLumberjack_RestPhase()
    {
        var rendered = AsciiArtEngine.Render(
            sceneId: "lumberjack",
            frameTick: 1,
            progressFraction: 0.5,
            isTracking: false,
            isGoalReached: false,
            isRestPhase: true,
            contextDetails: "Rest");

        Assert.NotNull(rendered);
        Assert.Contains("🔥", rendered.AsciiArt);
        Assert.Contains("Campfire", rendered.AsciiArt);
        Assert.Equal("☕ CAMPFIRE REST", rendered.BadgeText);
        Assert.Contains("campfire", rendered.StoryText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SoundEffectManager_HasLumberjackCheerVariations()
    {
        var variations = SoundEffectManager.GetVariationsForScene("lumberjack");

        Assert.Equal(3, variations.Count);
        Assert.Contains(variations, v => v.Id == "cheer_lumberjack_chop");
        Assert.Contains(variations, v => v.Id == "cheer_lumberjack_timber");
        Assert.Contains(variations, v => v.Id == "cheer_lumberjack_van");
    }

    [Fact]
    public void WorkRestSettings_DefaultEnabledVariations_IncludesLumberjackSounds()
    {
        var settings = new WorkRestSettings();

        Assert.True(settings.IsVariationEnabled("cheer_lumberjack_chop"));
        Assert.True(settings.IsVariationEnabled("cheer_lumberjack_timber"));
        Assert.True(settings.IsVariationEnabled("cheer_lumberjack_van"));
    }
}
