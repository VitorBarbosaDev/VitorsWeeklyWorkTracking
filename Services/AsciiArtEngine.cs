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
            "cafe" => RenderCafeScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
            "coffeejazz" => RenderCoffeeJazzScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
            "icecream" => RenderIceCreamScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
            "metro" => RenderMetroScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
            "rocket" => RenderRocketScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
            "cat" => RenderCatScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
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
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
        sb.AppendLine("  ──────────────────────────────────────────────────────────");
        
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
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
        sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
        sb.AppendLine("  ──────────────────────────────────────────────────────────");

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

    #region Scene 4: ☕ Rainy Window Coffee (4-Pane Lattice & Foggy Glass)

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
            sb.AppendLine("   +-------+-------+   🌧️ \\ \\   ☕  RAINY REST BREAK");
            sb.AppendLine("   | \\ \\   |  \\ \\  |   Watching raindrops trickle down the glass...");
            sb.AppendLine("   +-------+-------+   Cozy lo-fi beats, warm tea, peaceful thoughts.");
            sb.AppendLine("   | [☕]  | ~~~~~ |   Let yourself rest, breathe, and unwind.");
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🌧️ Relaxing by the cozy 4-pane rainy window with warm tea. Take a deep breath.",
                BadgeText = "☕ RAIN BREAK",
                MiniLine = "[🌧️ ☕ 4-Pane Rainy Window Rest & Tea]"
            };
        }

        if (isGoalReached)
        {
            sb.AppendLine("   +-------+-------+    ✨ ☕ ✨  GOOD JOB!");
            sb.AppendLine("   | ✨GOOD| JOB!✨|    Finger-written on the steamy 4-pane glass!");
            sb.AppendLine("   +-------+-------+    Warm golden bokeh glowing softly outside!");
            sb.AppendLine("   | ( ( ( | ♡ ♡ ♡ |    Great focus session completed! 🌧️ ✨");
            sb.AppendLine("   | [☕]  | ~~~~~ |");
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "☕ 'Good Job!' finger-written on the steamy rainy window! Enjoy your warm brew! ✨",
                BadgeText = "☕ GOOD JOB!",
                MiniLine = "[☕ 100% • ✨ Good Job! ✨]"
            };
        }

        string steam = (f % 2 == 0) ? "( ( (" : ") ) )";
        string fogTop = progress switch
        {
            < 0.25 => "| \\ \\   |  \\ \\  |",
            < 0.50 => "| \\~\\   |  \\~\\  |",
            < 0.75 => "| ~~~   |  ~~~  |",
            _ => "| ☁️☁️   |  ☁️☁️  |"
        };

        string fogBottom = progress switch
        {
            < 0.25 => $"| {steam} |       |",
            < 0.50 => $"| {steam} |  ~ ~  |",
            < 0.75 => $"| [☕]  | ~~~~~ |",
            _ => $"| [☕]  | ☁️☁️☁️ |"
        };

        int pct = (int)(progress * 100);

        sb.AppendLine($"   +-------+-------+    🌧️ Rain trickling down 4-pane window");
        sb.AppendLine($"   {fogTop}    Steam rising from mug: {steam}");
        sb.AppendLine($"   +-------+-------+    Glass condensation fogging up: {pct}%");
        sb.AppendLine($"   {fogBottom}    Steady focus flowing like the gentle rain...");
        sb.AppendLine("  ──────────────────────────────────────────────────────────");

        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = $"☕ Cozy coffee steaming by 4-pane window • Glass fogging up • {pct}% focus",
            BadgeText = $"☕ {pct}% FOGGY",
            MiniLine = $"[☕ {pct}% Steamy • 🌧️ 4-Pane Window]"
        };
    }

    #endregion

    #region Scene: 🎷 Coffee Jazz Window (Autumn Lake, Latte & Lo-Fi Jazz)

    private static RenderedArtScene RenderCoffeeJazzScene(
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
            sb.AppendLine("  ┌──────────────┬───┐   🎷 ~ ♫ ~ ♪  CHILL JAZZ BREAK");
            sb.AppendLine("  │ 🍂  🌲 🌲 🍁 │ 🍂│   Golden autumn leaves gently drifting over the lake...");
            sb.AppendLine("  │ ~ ~ ~ ~ ~ ~ ~│   │   Sip warm cinnamon latte, lean back & enjoy the melody.");
            sb.AppendLine("  ├──────────────┴───┤   Soothing saxophone chords • Rest your mind.");
            sb.AppendLine("  │   ( (   [☕]  🎷 │");
            sb.AppendLine("  └──────────────────┘");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🎷 Relaxing by the lakeside window with warm latte and soothing jazz chords.",
                BadgeText = "🎷 JAZZ BREAK",
                MiniLine = "[🎷 ☕ Autumn Lakeside Jazz Rest 🍂]"
            };
        }

        if (isGoalReached)
        {
            string sparkles = (f % 2 == 0) ? "✨  ♫  ✨  ♪  ✨" : "♫  ✨  ♪  ✨  ♫";
            sb.AppendLine($"  ┌──────────────┬───┐    {sparkles}");
            sb.AppendLine("  │ 🍂  🌲 🌲 🍁 │ 🍂│    🏆 TARGET REACHED! PERFECT VIBES!");
            sb.AppendLine("  │ ~ ~ ~ ~ ~ ~ ~│   │    Golden sunset glow reflecting on calm water!");
            sb.AppendLine("  ├──────────────┴───┤    Finished session in style with warm latte & jazz!");
            sb.AppendLine("  │   ( (   [☕]  🎷 │");
            sb.AppendLine("  └──────────────────┘");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🎷 Perfect session completed! Golden sunset glow over the autumn lake! ✨",
                BadgeText = "🎷 TARGET REACHED!",
                MiniLine = "[🎷 100% • ☕ Golden Sunset Jazz! ✨]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine("  ┌──────────────┬───┐   🎷 Lo-Fi Coffee Jazz");
            sb.AppendLine("  │ 🍂  🌲 🌲 🍁 │ 🍂│   Autumn lakeside window view");
            sb.AppendLine("  │ ~ ~ ~ ~ ~ ~ ~│   │   Latte on wooden table & drifting sparkles");
            sb.AppendLine("  ├──────────────┴───┤   Start timer to tune into the focus groove!");
            sb.AppendLine("  │   ( (   [☕]     │");
            sb.AppendLine("  └──────────────────┘");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🎷 Autumn lakeside window is calm. Warm latte ready on the table. Start tracking!",
                BadgeText = "🎷 JAZZ READY",
                MiniLine = "[🎷 Autumn Window & Latte • 0%]"
            };
        }

        // Active tracking
        int pct = (int)(progress * 100);
        string note = (f % 4) switch
        {
            0 => "♫ ~ ✨",
            1 => "✨ ~ ♪",
            2 => "♪ ~ ♫",
            _ => "✨ ~ 🎷"
        };

        string steam = (f % 2 == 0) ? "( (" : ") )";
        string lakeDusk = progress switch
        {
            < 0.33 => "│ ~ ~ ~ ~ ~ ~ ~│   │",
            < 0.66 => "│ ~ 🍂 ~ ~ ✨ ~│   │",
            _ => "│ ✨ ~ 🌅 ~ 🍂 │   │"
        };

        sb.AppendLine($"  ┌──────────────┬───┐   🎷 {pct}% Lo-Fi Jazz Flow  {note}");
        sb.AppendLine($"  │ 🍂  🌲 🌲 🍁 │ 🍂│   Autumn breeze rustling amber trees...");
        sb.AppendLine($"  {lakeDusk}   Golden lake reflections drifting by");
        sb.AppendLine("  ├──────────────┴───┤   Sipping warm latte in deep focus");
        sb.AppendLine($"  │   {steam}   [☕]     │");
        sb.AppendLine("  └──────────────────┘");

        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = $"🎷 Lo-Fi jazz & autumn lake breeze • Warm latte • {pct}% focus groove",
            BadgeText = $"🎷 {pct}% JAZZ FLOW",
            MiniLine = $"[🎷 {pct}% • ☕ Lakeside Jazz 🍂]"
        };
    }

    #endregion

    #region Scene: 🍦 Pastel Ice Cream Truck

    private static RenderedArtScene RenderIceCreamScene(
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
            sb.AppendLine("       🌳  🏖️  🍦  SWEET ICE CREAM BREAK  🍧");
            sb.AppendLine("      /|\\  _o     Enjoying a chilled sundae under the shady tree...");
            sb.AppendLine("      / \\ |/| [🍨] Cool strawberry scoop & lemonade refresh! 🍓");
            sb.AppendLine("     =====|_|===============================================");
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🍧 Relaxing under the park tree enjoying a chilled ice cream sundae.",
                BadgeText = "🍨 SUNDAE REST",
                MiniLine = "[🌳 🏖️ 🍨 Ice Cream Sundae Break]"
            };
        }

        if (isGoalReached)
        {
            string sparks = (f % 2 == 0) ? "✨ 💖 ✨" : "💖 ✨ 💖";
            sb.AppendLine($"    🍦 [~ICE CREAM~] 🍧      {sparks}  PARTY TIME!");
            sb.AppendLine("   .-------------------.    \\o/   \\o/   \\o/   \\o/  🐶");
            sb.AppendLine("   | 🍓 🍫 🍦 🍧 🍨 🍭 |    /|\\   /|\\   /|\\   /|\\  /|\\");
            sb.AppendLine("   `-(o)-----------(o)-'    / \\   / \\   / \\   / \\  / \\");
            sb.AppendLine("  ==========================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🍦 YAY! ICE CREAM PARTY! All the kids got their favorite treats! Target reached! 🎉",
                BadgeText = "🍨 ICE CREAM PARTY!",
                MiniLine = "[🍦 100% • 🍨 Ice Cream Party! 🎉]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine("    🍦 [~ICE CREAM~] 🍧       🌳 Park Shade");
            sb.AppendLine("   .-------------------.     /|\\   Truck is parked & ready!");
            sb.AppendLine("   | [OPEN]  🍦 🍨 🍫 |     / \\   Start timer to serve treats!");
            sb.AppendLine("   `-(o)-----------(o)-'");
            sb.AppendLine("  ==========================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🍦 Pastel ice cream truck is open and waiting for kids to arrive. Start tracking!",
                BadgeText = "🍦 TRUCK READY",
                MiniLine = "[🍦 Ice Cream Truck Open • 0%]"
            };
        }

        // Active tracking: kids queueing up as progress advances
        int pct = (int)(progress * 100);
        string kidsLine = progress switch
        {
            < 0.25 => "              🚶 👦",
            < 0.50 => "        🚶 👧   👦[🍦]",
            < 0.75 => "  🚶 👶   👧[🍧]  👦[🍦] 🐶",
            _ => " 👶[🍭] 👧[🍧] 👦[🍦] 🐶 💖"
        };

        string truckRoof = (f % 2 == 0) ? "    🍦 [~ICE CREAM~] 🍧" : "    🍧 [~ICE CREAM~] 🍦";

        sb.AppendLine($"{truckRoof}      🌳 {pct}% Focus Progress");
        sb.AppendLine("   .-------------------.    Serving yummy colorful scoops...");
        sb.AppendLine("   | [SERVE] 🍓 🍫 🍦  |");
        sb.AppendLine($"   `-(o)-----------(o)-'{kidsLine}");
        sb.AppendLine("  ==========================================================");

        string queueDesc = progress switch
        {
            < 0.25 => "1st kid walking up to order strawberry scoop",
            < 0.50 => "2 kids lined up enjoying strawberry & mint scoops",
            < 0.75 => "3 kids & puppy excited for rainbow sundaes",
            _ => "Line of happy kids enjoying ice creams together"
        };

        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = $"🍦 {queueDesc} • {pct}% to Ice Cream Party!",
            BadgeText = $"🍦 {pct}% SERVED",
            MiniLine = $"[🍦 {pct}% • 🍨 Kids Queueing]"
        };
    }

    #endregion

    #region Scene: 🎧 Tokyo Metro Lo-Fi Girl

    private static RenderedArtScene RenderMetroScene(
        int frameTick,
        double progress,
        bool isTracking,
        bool isGoalReached,
        bool isRestPhase,
        string context)
    {
        var sb = new StringBuilder();
        int f = frameTick % 4;

        string[] movingSkyline = {
            "⚡ 🗼 ── 🏢 ──► [新宿] 💨",
            "💨 ── 🏢 ── 🗼 [渋谷] ⚡",
            "⚡ 🗼 ── 🏙️ ──► [秋葉原] 💨",
            "💨 ── 🏙️ ── 🗼 [原宿] ⚡"
        };

        if (isRestPhase)
        {
            string restSky = (f % 2 == 0) ? "🗼 🌃 🏢 ~~~ 🌃" : "🌃 🗼 🌃 ~~~ 🏢";
            sb.AppendLine("   [ 🟢 YAMANOTE LINE 🚇 ]   🎧 🍵 TOKYO REST BREAK");
            sb.AppendLine($"   | {restSky} |  Sipping warm canned Royal Milk Tea 🍵");
            sb.AppendLine("   | [🟩 💺 🟩 💺 🟩] |  Plush velvet metro seat... Catnap 💤");
            sb.AppendLine("   | 🎧( ᴗ ᴗ)z Z 🧃 |  Rest your eyes and breathe.");
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🍵 Resting on the plush Tokyo Metro seat with warm royal milk tea and lo-fi beats.",
                BadgeText = "🎧 METRO REST",
                MiniLine = "[🚇 🎧 🍵 Tokyo Metro Rest & Tea]"
            };
        }

        if (isGoalReached)
        {
            string notes = (f % 2 == 0) ? "♪ ♫ ♩ 💖" : "♫ ♪ 💖 ♬";
            sb.AppendLine("   [ 🌟 DESTINATION: TARGET STATION 🌟 ]   ✨ ARRIVED!");
            sb.AppendLine($"   | 🗼 🏙️ 🏬 🚉 ✨ |  {notes}  Goal Completed!");
            sb.AppendLine("   | [🟩 💺 🟩 💺 🟩] |  v(^_^)v Victory peace sign on train!");
            sb.AppendLine("   | 🎧(^o^)v ✨    |  Smooth ride, outstanding focus!");
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🎧 Train arrived at target destination! Girl waves peace sign from plush metro seat! ✨",
                BadgeText = "🚉 ARRIVED!",
                MiniLine = "[🚇 100% • 🎧 Arrived at Station! ✨]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine("   [ 🟢 SHIBUYA ──► TARGET STATION ]   🚇 Tokyo Metro");
            sb.AppendLine("   | 🗼 🏙️  🏢  🌃  |  Putting on headphones...");
            sb.AppendLine("   | [🟩 💺 🟩 💺 🟩] |  Plush emerald bucket seat ready ♪");
            sb.AppendLine("   | 🎧(._. ) 🎒    |  Ready to start the journey!");
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🎧 Boarding Tokyo Metro, cozy on the plush velvet seats, lo-fi beats ready. Start timer to begin!",
                BadgeText = "🎧 READY TO RIDE",
                MiniLine = "[🚇 Tokyo Metro Ready • 0%]"
            };
        }

        // Active tracking
        int pct = (int)(progress * 100);
        string[] notePatterns = { "♪   ", " ♫  ", "  ♩ ", "   ♬" };
        string musicNote = notePatterns[f];

        string cityLights = movingSkyline[f];
        string girlHead = (f % 2 == 0) ? "🎧(^_^)♪" : "🎧(^.^)♩";

        sb.AppendLine($"   [ 🟢 EXPRESS YAMANOTE • NEXT: FOCUS STATION ──► {pct}% ]");
        sb.AppendLine($"   | {cityLights} |  Music: {musicNote} Lo-Fi chill beats");
        sb.AppendLine($"   | [🟩 💺 🟩 💺 🟩] |  Tokyo skyline flying past the window! ⚡");
        sb.AppendLine($"   |  {girlHead} 🎒   |  Deep in the focus zone on plush seat...");
        sb.AppendLine("  ──────────────────────────────────────────────────────────");

        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = $"🎧 Express train flying past Tokyo skyline • Deep in the focus zone • {pct}%",
            BadgeText = $"🎧 {pct}% EXPRESS",
            MiniLine = $"[🚇 ⚡ {pct}% • 🏙️ Flying Past Tokyo]"
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
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
            sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
        sb.AppendLine("  ──────────────────────────────────────────────────────────");

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
