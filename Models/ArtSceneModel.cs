using System;
using System.Collections.Generic;

namespace VitorsWeeklyWorkTracking.Models;

public class ArtSceneOption : IEquatable<ArtSceneOption>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string DisplayName => $"{Icon} {Name}";

    public override string ToString() => DisplayName;

    public bool Equals(ArtSceneOption? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as ArtSceneOption);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Id ?? string.Empty);

    public static readonly List<ArtSceneOption> AvailableScenes = new()
    {
        new() { Id = "cycling", Name = "Boy Cycling Home", Icon = "🚴", Description = "Cute boy pedals his bicycle with puppy in basket across a scenic meadow trail towards his cozy home." },
        new() { Id = "cafe", Name = "Rainy Window Coffee", Icon = "☕", Description = "Zoomed-in 4-pane rainy window where steam rises from a cozy mug, fog builds on the glass, and reveals 'Good Job!' at the end." },
        new() { Id = "coffeejazz", Name = "Coffee Jazz Window", Icon = "🎷", Description = "Chill 'playlist cover' view: an autumn lakeside window beside a latte on a wooden table, with drifting golden sparkle dust." },
        new() { Id = "icecream", Name = "Pastel Ice Cream Truck", Icon = "🍦", Description = "Cute pastel ice cream truck where children happily walk up to order delicious treats as focus time advances." },
        new() { Id = "metro", Name = "Tokyo Metro Lo-Fi Girl", Icon = "🎧", Description = "Chill anime girl listening to lo-fi beats with headphones on the Tokyo metro passing glowing city lights." },
        new() { Id = "cat", Name = "Playful Focus Kitty", Icon = "🐱", Description = "Kawaii fluffy kitten plays with yarn and scampers towards delicious fish treats." },
        new() { Id = "rocket", Name = "Space Rocket Launch", Icon = "🚀", Description = "Chunky cute rocket blasts off through pastel stars to land on the Moon." },
        new() { Id = "runner", Name = "Chibi Marathon Runner", Icon = "🏃", Description = "Cute energetic runner with flying sneakers sprinting to the golden trophy finish line." },
        new() { Id = "tamagotchi", Name = "Focus Pet Tamagotchi", Icon = "👾", Description = "Kawaii virtual pocket pet that gains XP and smiles with glittering eyes as you focus." },
        new() { Id = "lumberjack", Name = "Lumberjack Wood Chopping", Icon = "🌲", Description = "Hardworking lumberjack chops down a dense pine forest tree by tree, packing his timber van full of firewood logs to take home!" }
    };
}
