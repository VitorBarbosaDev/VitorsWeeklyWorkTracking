using System;
using System.Collections.Generic;

namespace VitorsWeeklyWorkTracking.Models;

public class ArtSceneOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string DisplayName => $"{Icon} {Name}";

    public override string ToString() => DisplayName;

    public static List<ArtSceneOption> AvailableScenes => new()
    {
        new() { Id = "cycling", Name = "Boy Cycling Home", Icon = "🚴", Description = "Boy pedals his bicycle across the countryside towards home as the timer counts down." },
        new() { Id = "rocket", Name = "Space Rocket Launch", Icon = "🚀", Description = "Rocket blasts off through stars and asteroids to land on the Moon." },
        new() { Id = "cat", Name = "Playful Focus Kitty", Icon = "🐱", Description = "Cute kitten explores the path to reach a bowl of delicious fish." },
        new() { Id = "cafe", Name = "Cozy Lo-Fi Cafe", Icon = "☕", Description = "Warm study desk with steaming coffee filling up and stacking books." },
        new() { Id = "runner", Name = "Marathon Runner", Icon = "🏃", Description = "Athlete runs a marathon sprint to burst through the finish line ribbon." },
        new() { Id = "tamagotchi", Name = "Focus Pet Tamagotchi", Icon = "👾", Description = "Cute interactive digital companion that gains XP and levels up as you focus." }
    };
}
