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
            sb.AppendLine("      (☁️)            🌳             🛋️");
            sb.AppendLine("    .-~~~~-.         /|\\           _o /~~\\");
            sb.AppendLine("   (  ~~~~  )       / | \\         /| |    |  ☕ Rest Break!");
            sb.AppendLine("    `-....-'         ||           / | |____|  Hydrate & stretch");
            sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine($"      (☁️)             {hearts}     🏠");
            sb.AppendLine("    .-~~~~-.                     _ /\\ _");
            sb.AppendLine("   (  ~~~~  )               \\o/ /      \\ |~|");
            sb.AppendLine("  __o `-....-'              /| /________\\|_|");
            sb.AppendLine("(_)/(_)                     / \\|  __    |");
            sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine("      (☁️)             🌲             🏠");
            sb.AppendLine("    .-~~~~-.          /\\            _ /\\ _");
            sb.AppendLine("   (  ~~~~  )        /  \\          /      \\ |~|");
            sb.AppendLine("  __o `-....-'        ||          /________\\|_|");
            sb.AppendLine("(_)/(_) Ready to ride...          |  __    |");
            sb.AppendLine(" ─────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🚴 Ready at the starting point. Start the timer to begin the cycling journey home!",
                BadgeText = "🚴 READY TO RIDE",
                MiniLine = "[🚴 Start Line ---> 🏠 Home]"
            };
        }

        // Active tracking: Boy pedaling towards home
        int boyPos = Math.Clamp((int)(progress * 18), 0, 18);
        string pad = new string(' ', boyPos);
        string gap = new string(' ', 18 - boyPos);
        int cloudShift = (frameTick / 2) % 6;
        string cloudPad = new string(' ', cloudShift);
        string cloudGap = new string(' ', 14 - cloudShift);

        string[] pedalFrames0 = { "  __o ", " _ \\<_", "(_)/(_)" };
        string[] pedalFrames1 = { "  __o ", " _ `\\_", "(_)/(_)" };
        string[] pedalFrames2 = { "  __o ", " _ /\\_", "(_)/(_)" };

        string[] boyFrame = (f % 3) switch
        {
            0 => pedalFrames0,
            1 => pedalFrames1,
            _ => pedalFrames2
        };

        sb.AppendLine($" {cloudPad}(☁️){cloudGap}      🌲             🏠");
        sb.AppendLine($" {pad}{boyFrame[0]}{gap}   /\\            _ /\\ _");
        sb.AppendLine($" {pad}{boyFrame[1]}{gap}  /  \\          /      \\ |~|");
        sb.AppendLine($" {pad}{boyFrame[2]}{gap}   ||          /________\\|_|");
        sb.AppendLine(" ─────────────────────────────────────────────");
        
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
            sb.AppendLine("      ✦    *      🛰️ SPACE STATION LOUNGE");
            sb.AppendLine("   +          .  ┌─────────────────────────┐");
            sb.AppendLine("      *   .      │ 👨‍🚀 🧋 Zero-G Break...  │");
            sb.AppendLine("   .        ✦    │ Recharge your mind! ☕  │");
            sb.AppendLine(" ────────────────┴─────────────────────────┴──");

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
            sb.AppendLine("      ✦   *   .    ✨ 🌕 LUNAR SURFACE ✨");
            sb.AppendLine("                        _ /\\ _");
            sb.AppendLine("            🚩   \\o/   ( 🌕 )  MISSION SUCCESS!");
            sb.AppendLine("            |    /|     `--'   Landed on Moon! 🚀");
            sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine("   🌍 EARTH               ✦            🌕 MOON");
            sb.AppendLine("    /\\                                  _ /\\ _");
            sb.AppendLine("   |==|  🚀 Ready on pad...            ( 🌕 )");
            sb.AppendLine("  /____\\                                `--'");
            sb.AppendLine(" ─────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🚀 Rocket is fueled on the launchpad, waiting for countdown sequence.",
                BadgeText = "🚀 READY ON PAD",
                MiniLine = "[🌍 Rocket Launchpad ---> 🌕 Moon]"
            };
        }

        int rPos = Math.Clamp((int)(progress * 16), 0, 16);
        string pad = new string(' ', rPos);
        string gap = new string(' ', 16 - rPos);
        string flame = (f % 2 == 0) ? "==>" : " =>";

        int eventType = ((frameTick / 4) + (int)(progress * 10)) % 4;
        string topSky = eventType switch
        {
            0 => "   ✦   🛸(・ω・)ノ   .          ✦        🌕 MOON",
            1 => "   ✦    🪨  *  ☄️   .          ✦        🌕 MOON",
            2 => "   ✦   🛰️ [===]     .          ✦        🌕 MOON",
            _ => "   ✦   ☄️ ======>   .          ✦        🌕 MOON"
        };

        sb.AppendLine(topSky);
        sb.AppendLine($" {pad}  | \\{gap}                    _ /\\ _");
        sb.AppendLine($" {pad}{flame}[=> {gap}   *     .        ( 🌕 )");
        sb.AppendLine($" {pad}  | /{gap}                     `--'");
        sb.AppendLine(" ─────────────────────────────────────────────");

        int pct = (int)(progress * 100);
        string story = eventType switch
        {
            0 => $"🚀 Friendly alien UFO waves hello in zero-G • {pct}% to lunar landing!",
            1 => $"🚀 Navigating sparkling asteroid belt with crystal ore • {pct}% to Moon!",
            2 => $"🚀 Deep space telemetry synced with orbital satellite • {pct}% to Moon!",
            _ => $"🚀 Rocket racing alongside glowing cosmic comet • {pct}% to Moon!"
        };

        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = story,
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
            sb.AppendLine($"    /\\_/\\    {zzz}   ☕ Cozy Cat Nap");
            sb.AppendLine("   (= -.- =)         Curled up on pillow");
            sb.AppendLine("    (  \"  )          Rest & stretch! 🐾");
            sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine("    /\\_/\\    🐟 🥛  ✨ 💖 💖 ✨");
            sb.AppendLine("   (= ^ω^ =) Nom nom! Delicious fish!");
            sb.AppendLine("    ( > < )  Purr-fect session! 💖");
            sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine("                                        🐟 🥛");
            sb.AppendLine("    /\\_/\\     🧶                        ┌──┐");
            sb.AppendLine("   ( ='.' )~                            └──┘");
            sb.AppendLine("    > ^ <   Ready to focus... 🐾");
            sb.AppendLine(" ─────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🐱 Kitty is stretching and waiting to start the adventure towards the fish bowl!",
                BadgeText = "🐱 READY TO POUNCE",
                MiniLine = "[🐱 Ready ---> 🐟 Treat]"
            };
        }

        int cPos = Math.Clamp((int)(progress * 16), 0, 16);
        string pad = new string(' ', cPos);
        string gap = new string(' ', 16 - cPos);
        string paws = (f % 2 == 0) ? "  > ^ < " : "  < ^ > ";
        string yarn = (f % 2 == 0) ? "🧶~" : "~🧶";

        sb.AppendLine("                                        🐟 🥛");
        sb.AppendLine($" {pad} /\\_/\\   {yarn}{gap}           ┌──┐");
        sb.AppendLine($" {pad}( ='.' )~{gap}                └──┘");
        sb.AppendLine($" {pad}{paws}{gap}");
        sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine("  +-------+-------+  🌧️ \\ \\  ☕ RAIN BREAK");
            sb.AppendLine("  | \\ \\   |  \\ \\  |  Watching raindrops fall...");
            sb.AppendLine("  +-------+-------+  Lo-fi beats & warm tea 🌿");
            sb.AppendLine("  | [☕]  | ~~~~~ |  Rest, breathe, and unwind");
            sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine("  +-------+-------+  ✨ ☕ ✨ GOOD JOB!");
            sb.AppendLine("  | ✨GOOD| JOB!✨|  Finger-written on glass!");
            sb.AppendLine("  +-------+-------+  Warm bokeh lights outside");
            sb.AppendLine("  | [☕]  | ♡ ♡ ♡ |  Focus complete! 🌧️ ✨");
            sb.AppendLine(" ─────────────────────────────────────────────");

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

        sb.AppendLine("  +-------+-------+  🌧️ Rain trickling down");
        sb.AppendLine($"  {fogTop}  Steam from mug: {steam}");
        sb.AppendLine($"  +-------+-------+  Condensation: {pct}%");
        sb.AppendLine($"  {fogBottom}  Deep lo-fi focus...");
        sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine("  ┌──────────────┬───┐  🎷 ~ ♫ ~ ♪ JAZZ BREAK");
            sb.AppendLine("  │ 🍂  🌲 🌲 🍁 │ 🍂│  Autumn leaves drifting...");
            sb.AppendLine("  │ ~ ~ ~ ~ ~ ~ ~│   │  Sip latte & enjoy melody 🍂");
            sb.AppendLine("  ├──────────────┴───┤  Soothing sax • Rest eyes");
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
            string sparkles = (f % 2 == 0) ? "✨ ♫ ✨ ♪ ✨" : "♫ ✨ ♪ ✨ ♫";
            sb.AppendLine($"  ┌──────────────┬───┐  {sparkles}");
            sb.AppendLine("  │ 🍂  🌲 🌲 🍁 │ 🍂│  🏆 TARGET REACHED!");
            sb.AppendLine("  │ ~ ~ ~ ~ ~ ~ ~│   │  Golden lake sunset glow!");
            sb.AppendLine("  ├──────────────┴───┤  Finished session with jazz!");
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
            sb.AppendLine("  ┌──────────────┬───┐  🎷 Lo-Fi Coffee Jazz");
            sb.AppendLine("  │ 🍂  🌲 🌲 🍁 │ 🍂│  Autumn lakeside view");
            sb.AppendLine("  │ ~ ~ ~ ~ ~ ~ ~│   │  Latte on table ready ☕");
            sb.AppendLine("  ├──────────────┴───┤  Start timer to tune in!");
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

        sb.AppendLine($"  ┌──────────────┬───┐  🎷 {pct}% Jazz Flow {note}");
        sb.AppendLine($"  │ 🍂  🌲 🌲 🍁 │ 🍂│  Autumn breeze rustling");
        sb.AppendLine($"  {lakeDusk}  Reflections on water");
        sb.AppendLine("  ├──────────────┴───┤  Sipping warm latte ☕");
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

        // Vendor man in the truck window — friendly kaomoji face, tips hat / waves as frames tick.
        string vendorFace = (f % 2 == 0) ? "👨‍🍳(•ᴗ•)づ" : "👨‍🍳(•ᴗ•) ";

        if (isRestPhase)
        {
            sb.AppendLine("   ☀️ 🌤️ ☁️      🌳🌳      🍦 SWEET SUNDAE BREAK 🍧");
            sb.AppendLine("  🎈       🪑🧺   (o)(o)    Enjoying a chilled sundae...");
            sb.AppendLine("  🌼🌼    \\(≧▽≦)/ [🍨🍓]   Strawberry scoop refresh! 🍓");
            sb.AppendLine(" ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🍧 Relaxing on the picnic blanket under the park tree enjoying a chilled ice cream sundae.",
                BadgeText = "🍨 SUNDAE REST",
                MiniLine = "[🌳 🏖️ 🍨 Ice Cream Sundae Break]"
            };
        }

        if (isGoalReached)
        {
            string sparks = (f % 2 == 0) ? "✨ 💖 ✨" : "💖 ✨ 💖";
            sb.AppendLine($"  ☀️  🍦 [~ICE CREAM~] 🍧  🎉  {sparks} PARTY!  🎈");
            sb.AppendLine(" .-------------------.   \\(^▽^)/ \\(≧▽≦)/ \\(★‿★)/  🐶🍖");
            sb.AppendLine($" | {vendorFace} 🍓🍫🍦🍧🍨 |    🍦       🍧       🍨     (^ω^)");
            sb.AppendLine(" `-(o)-----------(o)-'   🌳🌼    🌳🌼    🌳🌼    🌳🌼");
            sb.AppendLine(" =========================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🍦 YAY! ICE CREAM PARTY! Every kid got a cone from the vendor and the puppy got a treat too! 🎉",
                BadgeText = "🍨 ICE CREAM PARTY!",
                MiniLine = "[🍦 100% • 🍨 Ice Cream Party! 🎉]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine("   ☀️  🍦 [~ICE CREAM~] 🍧   🌳 Park Shade  🐦");
            sb.AppendLine("  .-------------------.  🪑     Truck is open!");
            sb.AppendLine($"  | {vendorFace} [OPEN] |  🌼🌼   Start timer to bring the kids!");
            sb.AppendLine("  `-(o)-----------(o)-'");
            sb.AppendLine(" =========================================================");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🍦 Pastel ice cream truck is open, vendor is ready and waiting for kids to arrive. Start tracking!",
                BadgeText = "🍦 TRUCK READY",
                MiniLine = "[🍦 Ice Cream Truck Open • 0%]"
            };
        }

        // Active tracking: kids continuously cycle through walking up, getting served, and
        // walking off-screen — driven by frameTick so the caption stays in sync with the
        // graphics scene's looping animation instead of being tied to overall session progress.
        int pct = (int)(progress * 100);
        string truckRoof = (f % 2 == 0) ? "  🍦 [~ICE CREAM~] 🍧" : "  🍧 [~ICE CREAM~] 🍦";

        // Kaomoji faces for each stage of a kid's visit, in the same "face + accessory" style
        // used for the girl on the metro (e.g. 🎧(^_^)♪).
        const string kidWalkingIn = "🚶(o.o)";
        const string kidAtCounterWaiting = "(o.o)?";
        string kidGettingCone = (f % 2 == 0) ? "(≧▽≦)ノ" : "(★‿★)ノ";
        const string kidHappyLeaving = "\\(^▽^)/🍦";

        // Mirrors the walk-in / serve / walk-off cycle timing used by the graphics scene so both
        // renderers describe the same moment.
        const int enterFrames = 55;
        const int serveFrames = 35;
        const int exitFrames = 70;
        const int coneRevealFrame = enterFrames + (int)(serveFrames * 0.4);
        const int cycleFrames = enterFrames + serveFrames + exitFrames;

        int localFrame = ((frameTick % cycleFrames) + cycleFrames) % cycleFrames;

        string sceneLine;
        string queueDesc;

        if (localFrame < enterFrames)
        {
            sceneLine = $" {kidWalkingIn}                                   ";
            queueDesc = "A kid spots the truck and walks up the park path";
        }
        else if (localFrame < coneRevealFrame)
        {
            sceneLine = $"      {kidAtCounterWaiting}   ordering a scoop...            ";
            queueDesc = "A kid is at the counter ordering their favorite scoop";
        }
        else if (localFrame < enterFrames + serveFrames)
        {
            sceneLine = $"      {kidGettingCone}🍦   vendor hands over the cone!  ";
            queueDesc = "The vendor hands the kid a freshly served cone";
        }
        else
        {
            sceneLine = $" {kidHappyLeaving}  ›››  see you tomorrow!         ";
            queueDesc = "The kid walks off happily with their treat as the next one arrives";
        }

        sb.AppendLine($"{truckRoof}   ☀️ 🌳  {pct}% Progress");
        sb.AppendLine(" .-------------------.  Serving sweet treats...   🪑");
        sb.AppendLine($" | {vendorFace} 🍓🍫🍦 |");
        sb.AppendLine($" `-(o)-----------(o)-'{sceneLine}");
        sb.AppendLine(" =========================================================");

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

        if (isRestPhase)
        {
            string restSky = (f % 2 == 0) ? "🗼 🌃 🏢 🌃" : "🌃 🗼 🌃 🏢";
            sb.AppendLine(" [ 🟢 YAMANOTE LINE 🚇 ]  🎧 🍵 TOKYO REST");
            sb.AppendLine($" | {restSky} |  Warm Royal Milk Tea 🍵");
            sb.AppendLine(" | [🟩 💺 🟩 💺 🟩] |  Plush metro seat catnap 💤");
            sb.AppendLine(" | 🎧( ᴗ ᴗ)z Z 🧃 |  Breathe and relax.");
            sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine(" [ 🌟 TARGET STATION 🌟 ]  ✨ ARRIVED!");
            sb.AppendLine($" | 🗼 🏙️ 🏬 🚉 ✨ |  {notes} Goal Met!");
            sb.AppendLine(" | [🟩 💺 🟩 💺 🟩] |  v(^_^)v Victory sign!");
            sb.AppendLine(" | 🎧(^o^)v ✨    |  Outstanding focus!");
            sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine(" [ 🟢 SHIBUYA ──► DEST ]  🚇 Tokyo Metro");
            sb.AppendLine(" | 🗼 🏙️  🏢  🌃 |  Putting on headphones...");
            sb.AppendLine(" | [🟩 💺 🟩 💺 🟩] |  Plush emerald seat ready ♪");
            sb.AppendLine(" | 🎧(._. ) 🎒    |  Start timer to ride!");
            sb.AppendLine(" ─────────────────────────────────────────────");

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
        string[] notePatterns = { "♪  ", "♫  ", "♩  ", "♬  " };
        string musicNote = notePatterns[f];
        string[] movingSkyline = {
            "⚡ 🗼 ─ 🏢 ─► [新宿]",
            "💨 ─ 🏢 ─ 🗼 [渋谷]",
            "⚡ 🗼 ─ 🏙️ ─► [秋葉]",
            "💨 ─ 🏙️ ─ 🗼 [原宿]"
        };
        string cityLights = movingSkyline[f];
        string girlHead = (f % 2 == 0) ? "🎧(^_^)♪" : "🎧(^.^)♩";

        sb.AppendLine($" [ 🟢 YAMANOTE • FOCUS ──► {pct}% ]");
        sb.AppendLine($" | {cityLights} |  Music: {musicNote} Lo-Fi beats");
        sb.AppendLine(" | [🟩 💺 🟩 💺 🟩] |  Tokyo skyline flying ⚡");
        sb.AppendLine($" |  {girlHead} 🎒   |  Deep focus zone ♪");
        sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine("    🏃‍♂️ 🥤 HYDRATION & RECOVERY ZONE");
            sb.AppendLine("   _o     Cooling down on grass...");
            sb.AppendLine("  /|\\     Drink water & catch breath! 💧");
            sb.AppendLine("  / \\");
            sb.AppendLine(" ─────────────────────────────────────────────");

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
            sb.AppendLine("    🏆 🎊 FINISH LINE CROSSED! 🎊 🏆");
            sb.AppendLine("   \\o/  [ 🏁 FINISH RIBBON BROKEN! ]");
            sb.AppendLine("   /|\\  Gold medal focus sprint! 🥇");
            sb.AppendLine("   / \\");
            sb.AppendLine(" ─────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🏆 CHAMPION! Broken the finish line ribbon in record time! Fantastic! 🎊",
                BadgeText = "🏆 FINISH LINE!",
                MiniLine = "[🏆 Finished Marathon Sprint! 🎊]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine("   🏃 Marathon Track   🚩 25%   🚩 50%   🏁 FINISH");
            sb.AppendLine("   \\o                  Ready at start block...");
            sb.AppendLine("   /|\\                 Start timer to sprint!");
            sb.AppendLine("   / \\");
            sb.AppendLine(" ─────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🏃 Ready at the starting blocks. Start the timer to sprint towards the finish line!",
                BadgeText = "🏃 READY TO SPRINT",
                MiniLine = "[🏃 Start Line ---> 🏁 Finish]"
            };
        }

        int rPos = Math.Clamp((int)(progress * 18), 0, 18);
        string pad = new string(' ', rPos);
        string gap = new string(' ', 18 - rPos);

        string[] runner0 = { "  o  ", " /|\\ ", " / \\ " };
        string[] runner1 = { "  o  ", " /|/ ", " / \\ " };
        string[] runner2 = { " \\o  ", "  |\\ ", " / \\ " };

        var frame = (f % 3) switch
        {
            0 => runner0,
            1 => runner1,
            _ => runner2
        };

        sb.AppendLine("                    🚩 25%   🚩 50%   🏁 FINISH");
        sb.AppendLine($" {pad}{frame[0]}{gap}           |");
        sb.AppendLine($" {pad}{frame[1]}{gap}         [---]");
        sb.AppendLine($" {pad}{frame[2]}{gap}           |");
        sb.AppendLine(" ─────────────────────────────────────────────");

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
        int pct = (int)(progress * 100);

        string moodFace = isRestPhase
            ? "( - . - ) z Z Z"
            : isGoalReached
                ? "\\(^ヮ^)/ ✨💖"
                : isTracking
                    ? (f % 2 == 0 ? "( • ̀ω•́ )و ✧" : "( ^ ω ^ ) ✨")
                    : "( • ‿ • ) 💬";

        int hpBars = Math.Clamp(happiness / 20, 0, 5);
        string hpDisplay = new string('■', hpBars) + new string('□', 5 - hpBars);

        sb.AppendLine(" ╭─────────────────────────╮");
        sb.AppendLine($" │ 👾 {moodFace,-18} │  ⭐ Lvl {level} ({currentXp}%)");
        sb.AppendLine($" │ HP: [{hpDisplay}]  {pct,3}% │  💖 Happy: {happiness}%");
        sb.AppendLine(" ╰─────────────────────────╯");

        string story = isRestPhase
            ? "👾 Pet is sleeping peacefully with a nightcap. Rest well & recharge!"
            : isGoalReached
                ? "💖 QUEST COMPLETE! Pet leveled up & celebrated with star candy!"
                : isTracking
                    ? $"👾 Focus companion cheering with sparkle eyes! Pet Level {level} • {pct}% done!"
                    : "👾 Focus pet is waiting on the console screen for the next adventure!";

        string badge = isRestPhase ? "👾 PET SLEEPING" : isGoalReached ? "💖 QUEST COMPLETE" : isTracking ? $"👾 PET LVL {level}" : "👾 PET READY";

        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = story,
            BadgeText = badge,
            MiniLine = $"[👾 Pet Lvl {level} • 💖 {happiness}% • {pct}%]"
        };
    }

    #endregion
}
