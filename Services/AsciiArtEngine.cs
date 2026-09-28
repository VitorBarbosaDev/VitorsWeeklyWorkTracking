using System;
using System.Text;

namespace VitorsWeeklyWorkTracking.Services;

public class RenderedArtScene
{
    public string AsciiArt { get; set; } = string.Empty;
    public string StoryText { get; set; } = string.Empty;
    public string BadgeText { get; set; } = string.Empty;
    public string MiniLine { get; set; } = string.Empty;
}

public static class AsciiArtEngine
{
    public static RenderedArtScene Render(
        string sceneId,
        int frameTick,
        double progressFraction,
        bool isTracking,
        bool isGoalReached,
        bool isRestPhase,
        string contextDetails,
        int focusXp = 0,
        int petHappiness = 100)
    {
        progressFraction = Math.Clamp(progressFraction, 0.0, 1.0);

        return sceneId switch
        {
            "rocket" => RenderRocketScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
            "cat" => RenderCatScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
            "cafe" => RenderCafeScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
            "runner" => RenderRunnerScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
            "tamagotchi" => RenderTamagotchiScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails, focusXp, petHappiness),
            _ => RenderCyclingScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails)
        };
    }

    #region Scene 1: 🚴 Boy Cycling Home

    private static RenderedArtScene RenderCyclingScene(
        int frameTick,
        double progress,
        bool isTracking,
        bool isGoalReached,
        bool isRestPhase,
        string context)
    {
        var sb = new StringBuilder();
        int f = frameTick % 4;

        if (isRestPhase)
        {
            sb.AppendLine("           (☁️)             🌳             🛋️");
            sb.AppendLine("         .-~~~~-.          /|\\           _o /~~\\");
            sb.AppendLine("        (  ~~~~  )        / | \\         /| |    |   ☕ Rest Break! Ahhh...");
            sb.AppendLine("         `-....-'          ||           / | |____|   Hydrate & relax your eyes.");
            sb.AppendLine("  ==========================================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "☕ Boy is relaxing on a shady park bench sipping cool lemonade. Rest well!",
                BadgeText = "☕ REST BREAK",
                MiniLine = "[🌳 🛋️ ☕ Boy Resting on Bench]"
            };
        }

        if (isGoalReached)
        {
            string hearts = (f % 2 == 0) ? "✨  💖  ✨" : "💖  ✨  💖";
            sb.AppendLine($"           (☁️)                      {hearts}     🏠");
            sb.AppendLine("         .-~~~~-.                               _ /\\ _");
            sb.AppendLine("        (  ~~~~  )                         \\o/ /      \\  |~| (♨️)");
            sb.AppendLine("   __o   `-....-'                          /| /________\\ |_|");
            sb.AppendLine(" _ \\<_                                     / \\|  __    |   ");
            sb.AppendLine("(_)/(_)                                       | |__|   |   ");
            sb.AppendLine("==========================================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🏠 I'M HOME! The boy arrived home right on time! Outstanding focus session! 🎉",
                BadgeText = "🏠 ARRIVED HOME!",
                MiniLine = "[🏠 Boy Arrived Home! Target Reached! 🎉]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine("       (☁️)               🌲              🌲             🏠");
            sb.AppendLine("     .-~~~~-.            /\\              /\\            _ /\\ _");
            sb.AppendLine("    (  ~~~~  )          /  \\            /  \\          /      \\  |~|");
            sb.AppendLine("     `-....-'            ||              ||          /________\\ |_|");
            sb.AppendLine("   __o                                               |  __    |    ");
            sb.AppendLine(" _ \\<_   Ready to pedal...                           | |__|   |    ");
            sb.AppendLine("(_)/(_)                                              |________|    ");
            sb.AppendLine("==========================================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🚴 Ready at the starting point. Start the timer to begin the cycling journey home!",
                BadgeText = "🚴 READY TO RIDE",
                MiniLine = "[🚴 Start Line ---> 🏠 Home]"
            };
        }

        // Active tracking: Boy pedaling towards home
        int totalTrackWidth = 44;
        int boyPos = Math.Clamp((int)(progress * totalTrackWidth), 0, totalTrackWidth);
        string leftPadding = new string(' ', Math.Max(0, boyPos));

        string[] pedalFrames0 = { "  __o ", " _ \\<_", "(_)/(_)" };
        string[] pedalFrames1 = { "  __o ", " _ `\\_", "(_)/(_)" };
        string[] pedalFrames2 = { "  __o ", " _ /\\_", "(_)/(_)" };

        string[] boyFrame = (f % 3) switch
        {
            0 => pedalFrames0,
            1 => pedalFrames1,
            _ => pedalFrames2
        };

        // Cloud animation
        int cloudShift = (frameTick / 2) % 30;
        string cloudPad = new string(' ', cloudShift);

        sb.AppendLine($"{cloudPad}(☁️)                          🌲              🏠");
        sb.AppendLine("                                            /\\             _ /\\ _");
        sb.AppendLine($"{leftPadding}{boyFrame[0]}                      /  \\           /      \\  |~|");
        sb.AppendLine($"{leftPadding}{boyFrame[1]}                       ||           /________\\ |_|");
        sb.AppendLine($"{leftPadding}{boyFrame[2]}                                    |  __    |   ");
        sb.AppendLine("==========================================================================");
        
        int percentInt = (int)(progress * 100.0);
        double kmRemaining = Math.Max(0.0, (1.0 - progress) * 10.0);

        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = $"🚴 Boy is pedaling steadily • {percentInt}% journey completed • {kmRemaining:F1} km to home!",
            BadgeText = $"🚴 {percentInt}% TO HOME",
            MiniLine = $"[🚴 {new string('·', Math.Clamp((int)(progress * 10), 0, 10))}--> 🏠 {percentInt}%]"
        };
    }

    #endregion

    #region Scene 2: 🚀 Space Odyssey

    private static RenderedArtScene RenderRocketScene(
        int frameTick,
        double progress,
        bool isTracking,
        bool isGoalReached,
        bool isRestPhase,
        string context)
    {
        var sb = new StringBuilder();
        int f = frameTick % 4;

        if (isRestPhase)
        {
            sb.AppendLine("       .    ✦     *         🛰️ SPACE STATION LOUNGE");
            sb.AppendLine("   +             .      ┌───────────────────────────────┐");
            sb.AppendLine("       *    .           │ 👨‍🚀 🧋 Floating in zero-G... │");
            sb.AppendLine("   .         ✦          │ Relaxing & recharging minds!  │");
            sb.AppendLine("────────────────────────┴───────────────────────────────┴─────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🛰️ Astronaut is sipping bubble tea in the orbital observation lounge.",
                BadgeText = "☕ ZERO-G REST",
                MiniLine = "[🛰️ 👨‍🚀 🧋 Zero-G Rest Break]"
            };
        }

        if (isGoalReached)
        {
            sb.AppendLine("       .   ✦   *   .    ✨ 🌕 LUNAR SURFACE ✨");
            sb.AppendLine("                       _ /\\ _");
            sb.AppendLine("               🚩   \\o/ ( 🌕 )   ✦ MISSION ACCOMPLISHED!");
            sb.AppendLine("               |    /|   `--'    Rocket landed safely on the Moon!");
            sb.AppendLine("   ==========[===]========================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🌕 TOUCHDOWN! Lunar orbit reached! Flag planted successfully! 🚀✨",
                BadgeText = "🌕 MISSION COMPLETE!",
                MiniLine = "[🌕 🚩 Rocket Landed on Moon! 🚀]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine("   🌍 EARTH BASE           .    ✦    *            🌕 MOON BASE");
            sb.AppendLine("    /\\                                              _ /\\ _");
            sb.AppendLine("   |==|  🚀 Ready on pad...   *        .           ( 🌕 )");
            sb.AppendLine("  /____\\                                            `--'");
            sb.AppendLine("  ======                                            ========");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🚀 Rocket is fueled on the launchpad, waiting for countdown sequence.",
                BadgeText = "🚀 READY ON PAD",
                MiniLine = "[🌍 Rocket Launchpad ---> 🌕 Moon]"
            };
        }

        int totalSpace = 38;
        int rocketPos = Math.Clamp((int)(progress * totalSpace), 0, totalSpace);
        string pad = new string(' ', rocketPos);

        string flame = (f % 2 == 0) ? "==>>" : " =>>";
        sb.AppendLine("   .      ✦        *         .          ✦       *        🌕");
        sb.AppendLine($"{pad}   | \\                                       _ /\\ _");
        sb.AppendLine($"{pad}{flame}[====>   *   .   ✦   +                      ( 🌕 )");
        sb.AppendLine($"{pad}   | /                                        `--'");
        sb.AppendLine("──────────────────────────────────────────────────────────────────────────");

        int pct = (int)(progress * 100);
        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = $"🚀 Rocket cruising at hyperspeed through the cosmos • {pct}% to lunar landing!",
            BadgeText = $"🚀 {pct}% IN ORBIT",
            MiniLine = $"[🚀 {new string('~', Math.Clamp((int)(progress * 10), 0, 10))}> 🌕 {pct}%]"
        };
    }

    #endregion

    #region Scene 3: 🐱 Cute Kitty & Fish

    private static RenderedArtScene RenderCatScene(
        int frameTick,
        double progress,
        bool isTracking,
        bool isGoalReached,
        bool isRestPhase,
        string context)
    {
        var sb = new StringBuilder();
        int f = frameTick % 4;

        if (isRestPhase)
        {
            string zzz = (f % 2 == 0) ? "z  Z  Z" : "  z  Z  Z";
            sb.AppendLine($"      /\\_/\\    {zzz}    ☕ Cat Nap & Rest Time");
            sb.AppendLine("     (=-.-=)            Curled up peacefully on a soft pillow.");
            sb.AppendLine("      (   )             Rest your eyes and stretch your paws! 🐾");
            sb.AppendLine("  ==========================================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🐱 Kitty is curled up having a sweet cozy nap. Take a relaxing break!",
                BadgeText = "🐱 COZY CAT NAP",
                MiniLine = "[🐱 z Z Z Kitty Sleeping... Rest Time]"
            };
        }

        if (isGoalReached)
        {
            sb.AppendLine("      /\\_/\\    🐟  🥛  ✨ 💖 💖 ✨");
            sb.AppendLine("     (=^ω^=)  Nom nom nom! Delicious fish feast!");
            sb.AppendLine("      ( > < )  Purr-fect focus session! Kitty is so proud of you! 💖");
            sb.AppendLine("  ==========================================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🐟 Kitty reached the feast! Eating happily with cheerful purrs! (=^･ω･^=) 💖",
                BadgeText = "🐟 PURR-FECT FEAST!",
                MiniLine = "[🐱 🐟 Feast Complete! Purr-fect! 💖]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine("      /\\_/\\               🧶              🐟 🥛");
            sb.AppendLine("     ( o.o ) ~                                 ┌──┐");
            sb.AppendLine("      > ^ <    Waiting to pounce...            └──┘");
            sb.AppendLine("  ==========================================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🐱 Kitty is stretching and waiting to start the adventure towards the fish bowl!",
                BadgeText = "🐱 READY TO POUNCE",
                MiniLine = "[🐱 Ready ---> 🐟 Treat]"
            };
        }

        int catPos = Math.Clamp((int)(progress * 42), 0, 42);
        string pad = new string(' ', catPos);
        string paws = (f % 2 == 0) ? " > ^ < " : " < ^ > ";

        sb.AppendLine("                                            🧶              🐟 🥛");
        sb.AppendLine($"{pad} /\\_/\\                                       ┌──┐");
        sb.AppendLine($"{pad}( o.o ) ~                                    └──┘");
        sb.AppendLine($"{pad}{paws}");
        sb.AppendLine("==========================================================================");

        int pct = (int)(progress * 100);
        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = $"🐱 Kitty is scampering along the path • {pct}% to the yummy fish treat!",
            BadgeText = $"🐱 {pct}% TO FISH",
            MiniLine = $"[🐱 {new string('.', Math.Clamp((int)(progress * 10), 0, 10))}> 🐟 {pct}%]"
        };
    }

    #endregion

    #region Scene 4: ☕ Cozy Lo-Fi Cafe

    private static RenderedArtScene RenderCafeScene(
        int frameTick,
        double progress,
        bool isTracking,
        bool isGoalReached,
        bool isRestPhase,
        string context)
    {
        var sb = new StringBuilder();
        int f = frameTick % 4;

        if (isRestPhase)
        {
            sb.AppendLine("     🎧 🌧️  LO-FI CHILL CORNER           ☕ Warm Herbal Tea");
            sb.AppendLine("    .-------.   Listening to relaxing rain beats...");
            sb.AppendLine("    | ~~~~~ |   Stretch your shoulders, take 3 deep breaths,");
            sb.AppendLine("    `-------'   and let your mind wander peacefully.");
            sb.AppendLine("  ==========================================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🎧 Chilling by the rainy window with lo-fi music. Breathe deep and recharge.",
                BadgeText = "☕ LO-FI BREAK",
                MiniLine = "[🎧 ☕ Lo-Fi Relax & Tea Break]"
            };
        }

        if (isGoalReached)
        {
            sb.AppendLine("        ( ( (     ✨ 👑 ✨  GOLDEN BREW READY!");
            sb.AppendLine("       ) ) )      .-------.  Fresh aromatic coffee poured!");
            sb.AppendLine("     .-------.    | ★★★★★ |] You've earned this delicious warm cup!");
            sb.AppendLine("     | ===== |]   | ~~~~~ |  Great job conquering this study session! 🌟");
            sb.AppendLine("     `-------'    `-------'");
            sb.AppendLine("  ==========================================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "☕ Fresh roast coffee is brewed to perfection! Time for a warm, fragrant sip! ✨",
                BadgeText = "☕ BREW READY!",
                MiniLine = "[☕ 100% Brewed! Coffee Ready! 🌟]"
            };
        }

        string steam = (f % 2 == 0) ? " ( ( ( " : "  ) ) )";
        string fillLevel = progress switch
        {
            < 0.25 => "       ",
            < 0.50 => " .___. ",
            < 0.75 => " |~~~| ",
            _ => " |===| "
        };

        int bookCount = Math.Max(1, (int)(progress * 6));
        string bookStack = new string('=', Math.Min(6, bookCount));

        sb.AppendLine($"      {steam}            💡 Study Desk      📚 Books Stacked: [{bookStack}]");
        sb.AppendLine("    .-------.              ♫ ♬ ♩             Vinyl Record Spinning 💿");
        sb.AppendLine($"    |{fillLevel}|]   Dripping fresh espresso...");
        sb.AppendLine("    `-------'            Steady focus brewing knowledge...");
        sb.AppendLine("  ==========================================================================");

        int pct = (int)(progress * 100);
        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = $"☕ Artisan coffee brewing • {pct}% filled • {bookCount} chapters conquered!",
            BadgeText = $"☕ {pct}% BREWED",
            MiniLine = $"[☕ {pct}% Brewed • 📖 Books: {bookCount}]"
        };
    }

    #endregion

    #region Scene 5: 🏃 Marathon Runner

    private static RenderedArtScene RenderRunnerScene(
        int frameTick,
        double progress,
        bool isTracking,
        bool isGoalReached,
        bool isRestPhase,
        string context)
    {
        var sb = new StringBuilder();
        int f = frameTick % 4;

        if (isRestPhase)
        {
            sb.AppendLine("       🏃‍♂️ 🥤  HYDRATION & RECOVERY ZONE");
            sb.AppendLine("      _o      Cooling down muscles on the grass...");
            sb.AppendLine("     /|\\      Drink plenty of water and catch your breath! 💧");
            sb.AppendLine("     / \\");
            sb.AppendLine("  ==========================================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🥤 Athlete is hydrating and stretching at the recovery tent.",
                BadgeText = "🥤 RECOVERY REST",
                MiniLine = "[🏃‍♂️ 🥤 Hydrate & Stretch Break]"
            };
        }

        if (isGoalReached)
        {
            sb.AppendLine("       🏆 🎊  FINISH LINE CROSSED!  🎊 🏆");
            sb.AppendLine("      \\o/  [ 🏁 FINISH TAPE BROKEN! ]");
            sb.AppendLine("      /|\\   Gold Medal Focus Sprint! Champion effort!");
            sb.AppendLine("      / \\");
            sb.AppendLine("  ==========================================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🏆 CHAMPION! Broken the finish line ribbon in record time! Fantastic! 🎊",
                BadgeText = "🏆 FINISH LINE!",
                MiniLine = "[🏆 Finished Marathon Sprint! 🎊]"
            };
        }

        int runPos = Math.Clamp((int)(progress * 42), 0, 42);
        string pad = new string(' ', runPos);

        string[] runner0 = { "  o  ", " /|\\ ", " / \\ " };
        string[] runner1 = { "  o  ", " /|/ ", " / \\ " };
        string[] runner2 = { " \\o  ", "  |\\ ", " / \\ " };

        var frame = (f % 3) switch
        {
            0 => runner0,
            1 => runner1,
            _ => runner2
        };

        sb.AppendLine("                                            🚩 25%   🚩 50%   🚩 75%  🏁 FINISH");
        sb.AppendLine($"{pad}{frame[0]}");
        sb.AppendLine($"{pad}{frame[1]}");
        sb.AppendLine($"{pad}{frame[2]}");
        sb.AppendLine("==========================================================================");

        int pct = (int)(progress * 100);
        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = $"🏃 Athlete sprinting strong • {pct}% of marathon completed • Sprinting for gold!",
            BadgeText = $"🏃 {pct}% SPRINT",
            MiniLine = $"[🏃 {new string('-', Math.Clamp((int)(progress * 10), 0, 10))}> 🏁 {pct}%]"
        };
    }

    #endregion

    #region Scene 6: 👾 Tamagotchi & Focus Pet

    private static RenderedArtScene RenderTamagotchiScene(
        int frameTick,
        double progress,
        bool isTracking,
        bool isGoalReached,
        bool isRestPhase,
        string context,
        int focusXp,
        int happiness)
    {
        var sb = new StringBuilder();
        int f = frameTick % 4;
        int level = Math.Max(1, (focusXp / 100) + 1);
        int currentXp = focusXp % 100;

        string moodFace = isRestPhase
            ? "( - . - ) z Z Z"
            : (isGoalReached
                ? "( ★ ω ★ ) ✨💖"
                : (isTracking
                    ? ((f % 2 == 0) ? "( ◕ ‿ ◕ ) 💖" : "( ^ ▽ ^ ) ✨")
                    : "( ・ ‿ ・ )"));

        string hearts = new string('♥', Math.Clamp(happiness / 20, 1, 5));

        sb.AppendLine($"    ┌──────────────────┐    🐾 FOCUS PET QUEST   Level {level}");
        sb.AppendLine($"    │   {moodFace,-14} │    XP: [{currentXp:D2}/100] • Total Focus XP: {focusXp}");
        sb.AppendLine($"    │   <)     (\\      │    Happiness: {hearts,-6} ({happiness}%)");
        sb.AppendLine($"    │    (,,)-(,,)     │    {(isRestPhase ? "☕ Snack & Playtime!" : (isGoalReached ? "🎉 Level Master!" : "✨ Focus Power Boost!"))}");
        sb.AppendLine("    └──────────────────┘");

        int pct = (int)(progress * 100);
        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = isRestPhase
                ? "🐾 Pet is happily snacking on treats during your rest break!"
                : (isGoalReached
                    ? $"🎉 Level {level} Pet is celebrating! +25 Focus XP Earned!"
                    : $"👾 Pet is cheering you on! {pct}% target progress • Keep going!"),
            BadgeText = $"👾 LVL {level} PET",
            MiniLine = $"[👾 Lvl {level} Pet • {pct}% • {hearts}]"
        };
    }

    #endregion
}
