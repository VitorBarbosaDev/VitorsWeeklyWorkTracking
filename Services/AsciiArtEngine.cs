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
            "lumberjack" => RenderLumberjackScene(frameTick, progressFraction, isTracking, isGoalReached, isRestPhase, contextDetails),
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
            sb.AppendLine(" ☕ ───[ 🍃 SCENIC PARK MEADOW REST ]─── 🌳 ──────────── [ 🛋️ ☕ REST BREAK ] ── 🧋 ");
            sb.AppendLine("   ☀️   (☁️)        /\\_    /\\__       (☁️)        🌳 BIG SHADE TREE  🍃         ");
            sb.AppendLine("     .-~~~~-.    /   \\  /      \\                /~~~\\  /~~~~\\       ☕ Rest Break ");
            sb.AppendLine("    (  ~~~~  )  /_____\\/________\\              (  ~~~)( ~~~  )     Hydrate & stretch");
            sb.AppendLine("    __o                                         \\___/  \\____/   _o  /~~~\\       ");
            sb.AppendLine("   _ \\<_   Bike parked in meadow shade...         ||     ||     /| | |   | 🧋    ");
            sb.AppendLine("  (_)/(_) ════════════════════════════════════════||═════||═════/ | | |___|──────");
            sb.AppendLine(" 🌸🌷🌼. . . . 🌸🌷🌼. . . . 🌸🌷🌼. . . . 🌸🌷🌼. . . . 🌸🌷🌼. . . . 🌸🌷🌼");

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
            string smoke = (f % 2 == 0) ? "( ) ) " : " ( ( )";
            sb.AppendLine(" 🏡 ───[ ✨ 🏠 ARRIVED HOME! WELCOME BACK! 🏠 ✨ ]─── 🎉 ──────────────── 🌻 ");
            sb.AppendLine($"   ☀️   (☁️)        /\\_    /\\__        🌳           🌲     (☁️)  {smoke} _ /\\_ _  ");
            sb.AppendLine("     .-~~~~-.    /   \\  /      \\     /|\\         /|\\           (____)/       \\ |~|");
            sb.AppendLine($"    (  ~~~~  )  /_____\\/________\\   / | \\       / | \\          {hearts} /_________\\_|_");
            sb.AppendLine("                                    ||          ||        \\o/  |  _   [✨]|  ");
            sb.AppendLine("   🎉 Great focus session! Boy & puppy reunited at home!  /|\\  | | |  🐶  |  ");
            sb.AppendLine("  ═══════════════════════════════════════════════════(_)/(_) / \\ ┴─┴────────┴──");
            sb.AppendLine(" 🌻🌻🌻. . . . 🌻🌻🌻. . . . 🌻🌻🌻. . . . 🌻🌻🌻. . . . 🌻🌻🌻. . . . 🌻🌻🌻");

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
            sb.AppendLine(" 🚴 ───[ 🚴 COUNTRYSIDE CYCLING JOURNEY ]─── 🌳 ─────────── [ 🏠 COZY HOME ] ─── 🏡 ");
            sb.AppendLine("   ☀️   (☁️)        /\\_    /\\__        🌳           🌲        (☁️)     _ /\\_ _  ");
            sb.AppendLine("     .-~~~~-.    /   \\  /      \\     /|\\         /|\\               /       \\ |~|");
            sb.AppendLine("    (  ~~~~  )  /_____\\/________\\   / | \\       / | \\             /_________\\_|_");
            sb.AppendLine("     __o                            ||          ||               |  _   [ ] |  ");
            sb.AppendLine("   _ \\<_   Ready to pedal home... 🍃                             | | |  🐶  |  ");
            sb.AppendLine("  (_)/(_) ═══════════════════════════════════════════════════════┴─┴────────┴──");
            sb.AppendLine(" 🌾🌾 [🏁 25%] 🌾🌾🌾 [🏁 50%] 🌾🌾🌾 [🏁 75%] 🌾🌾🌾 [🏡 HOME 100%] 🌾🌾");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🚴 Ready at the starting point. Start the timer to begin the cycling journey home!",
                BadgeText = "🚴 READY TO RIDE",
                MiniLine = "[🚴 Start Line ---> 🏠 Home]"
            };
        }

        // Active tracking: Boy pedaling smoothly towards home
        int maxMove = 42;
        int boyPos = Math.Clamp((int)(progress * maxMove), 0, maxMove);
        string padSpaces = new string(' ', boyPos);
        string padRoad = new string('═', boyPos);
        string gapSpaces = new string(' ', maxMove - boyPos);
        string gapRoad = new string('─', maxMove - boyPos);
        string fixedGap = new string(' ', 15);
        string fixedRoad = new string('─', 14);

        int cloudShift = (frameTick / 2) % 8;
        string cloudPad = new string(' ', cloudShift);
        string cloudGap = new string(' ', 8 - cloudShift);
        string bird = (f % 2 == 0) ? "🕊️ " : " 🕊️";

        string[] pedalFrames0 = { "    __o  ", "  _ \\<_  ", "=(_)/(_)" };
        string[] pedalFrames1 = { "    __o  ", "~ _ `\\_  ", " (_)/(_) " };
        string[] pedalFrames2 = { "    __o  ", "  _ /\\_  ", "~(_)/(_) " };

        string[] boyFrame = (f % 3) switch
        {
            0 => pedalFrames0,
            1 => pedalFrames1,
            _ => pedalFrames2
        };

        sb.AppendLine(" 🚴 ───[ 🚴 COUNTRYSIDE CYCLING JOURNEY ]─── 🌳 ─────────── [ 🏠 COZY HOME ] ─── 🏡 ");
        sb.AppendLine($"   ☀️  {cloudPad}(☁️){cloudGap}   /\\_    /\\__        🌳           🌲        {bird}    _ /\\_ _  ");
        sb.AppendLine("     .-~~~~-.    /   \\  /      \\     /|\\         /|\\               /       \\ |~|");
        sb.AppendLine("    (  ~~~~  )  /_____\\/________\\   / | \\       / | \\             /_________\\_|_");
        sb.AppendLine($"{padSpaces}{boyFrame[0]}{gapSpaces}{fixedGap}|  _   [ ] |  ");
        sb.AppendLine($"{padSpaces}{boyFrame[1]}{gapSpaces}{fixedGap}| | |  🐶  |  ");
        sb.AppendLine($"{padRoad}{boyFrame[2]}{gapRoad}{fixedRoad}┴─┴────────┴──");
        sb.AppendLine(" 🌾🌾 [🏁 25%] 🌾🌾🌾 [🏁 50%] 🌾🌾🌾 [🏁 75%] 🌾🌾🌾 [🏡 HOME 100%] 🌾🌾");

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
            sb.AppendLine(" ✦    *     .    🛰️ ORBITAL SPACE STATION LOUNGE    .     ✦   ☕ ZERO-G REST  ");
            sb.AppendLine("   .    🪐        ✦        *        .       ✦         *     _ /\\ _     ");
            sb.AppendLine("  ┌────────────────────────────────────────────────────────┐   ( 🌕 )     ");
            sb.AppendLine("  │ 👨‍🚀 🧋 Zero-G Chill Lounge • Floating astronaut latte... │    `--'      ");
            sb.AppendLine("  │ Relax, breathe, recharge orbital telemetry ✨           │              ");
            sb.AppendLine("  └────────────────────────────────────────────────────────┘              ");
            sb.AppendLine(" [🛰️ SPACE LOUNGE] ────────────────────────────────────────── [🌕 MOON ORBIT] ");
            sb.AppendLine("  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *");

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
            sb.AppendLine(" ✦    *     .    ✨ 🌕 LUNAR SURFACE LANDING ✨   .     ✦   🏆 MISSION COMPLETE");
            sb.AppendLine("   .    🪐        ✦        *        .       ✦         *     _ /\\ _     ");
            sb.AppendLine("                                            🚩   \\o/   ( 🌕 )     ");
            sb.AppendLine("       🚀 Touchdown on Lunar Moon Base!     |    /|     `--'      ");
            sb.AppendLine("       Flag planted! Outstanding focus!          / \\   Lunar Base ");
            sb.AppendLine("                                                                          ");
            sb.AppendLine(" [🌍 EARTH COMPLETED] ═══════════════════════════════════════ [🌕 LUNAR BASE 🏁]");
            sb.AppendLine("  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *");

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
            sb.AppendLine(" ✦    *     .    🌌 DEEP COSMOS EXPLORATION 🛰️    .     ✦      *    🌕 LUNAR   ");
            sb.AppendLine("   .    🪐        ✦        *        ☄️ ──►       .         *     _ /\\ _     ");
            sb.AppendLine(" 🌍 EARTH PAD            ✦               ✦               ✦     ( 🌕 )     ");
            sb.AppendLine("    /\\                                                          `--'      ");
            sb.AppendLine("   |==|  🚀 Ready on pad... Countdown standing by...                       ");
            sb.AppendLine("  /____\\                                                                  ");
            sb.AppendLine(" [🌍 EARTH BASE] ───►───────────►───────────►───────────►──── [🌕 MOON TARGET] ");
            sb.AppendLine("  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🚀 Rocket is fueled on the launchpad, waiting for countdown sequence.",
                BadgeText = "🚀 READY ON PAD",
                MiniLine = "[🌍 Rocket Launchpad ---> 🌕 Moon]"
            };
        }

        int rPos = Math.Clamp((int)(progress * 44), 0, 44);
        string pad = new string(' ', rPos);
        string gap = new string(' ', 44 - rPos);
        string flame = (f % 2 == 0) ? "==>" : " =>";

        int eventType = ((frameTick / 4) + (int)(progress * 10)) % 4;
        string topSky = eventType switch
        {
            0 => " ✦    *   🛸(・ω・)ノ   .    🌌 DEEP COSMOS EXPLORATION 🛰️    .     ✦   🌕 LUNAR",
            1 => " ✦    *    🪨  *  ☄️   .    🌌 DEEP COSMOS EXPLORATION 🛰️    .     ✦   🌕 LUNAR",
            2 => " ✦    *   🛰️ [===]     .    🌌 DEEP COSMOS EXPLORATION 🛰️    .     ✦   🌕 LUNAR",
            _ => " ✦    *   ☄️ ======>   .    🌌 DEEP COSMOS EXPLORATION 🛰️    .     ✦   🌕 LUNAR"
        };

        sb.AppendLine(topSky);
        sb.AppendLine("   .    🪐        ✦        *        ☄️ ──►       .         *     _ /\\ _     ");
        sb.AppendLine($" {pad}   | \\{gap}    ✦               ✦        ( 🌕 )     ");
        sb.AppendLine($" {pad}{flame}[=> {gap}                                  `--'      ");
        sb.AppendLine($" {pad}   | /{gap}                                                ");
        sb.AppendLine($" {pad}      {gap}                                                ");
        sb.AppendLine(" [🌍 EARTH BASE] ───►───────────►───────────►───────────►──── [🌕 MOON TARGET] ");
        sb.AppendLine("  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *  .  ✦  .  *");

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
            sb.AppendLine(" 🛋️ ───[ 🐾 COZY LIVING ROOM ]─── 🪴 ─────────── 🐱 [ ☕ COZY CAT NAP ] ─── 🧶 ");
            sb.AppendLine("   |  🪟 [☁️ 🌙]         🐱 Scratch Post       📦 Box        🥛 Milk Bowl | ");
            sb.AppendLine($"   |    |~|~|    {zzz}      |    |               |===|          ┌──────┐  | ");
            sb.AppendLine("   |   /\\_/\\                |    |                              │      │  | ");
            sb.AppendLine("   |  (= -.- =)  Curled up on velvet pillow...                  │ 🐟🥛 │  | ");
            sb.AppendLine("   |   (  \"  )   Rest well, stretch paws & recharge 🐾          └──────┘  | ");
            sb.AppendLine("   |                                                                      | ");
            sb.AppendLine(" ──┴──────────────────────────────────────────────────────────────────────┴─");

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
            sb.AppendLine(" 🛋️ ───[ 🐾 COZY LIVING ROOM ]─── 🪴 ─────────── 🐟 [ 💖 PURR-FECT FEAST! ] ─ 🧶");
            sb.AppendLine("   |  🪟 [☁️ ☀️]         🐱 Scratch Post       📦 Box   ✨ 💖 💖 ✨        | ");
            sb.AppendLine("   |    |~|~|               |    |               |===|   /\\_/\\   ┌──────┐  | ");
            sb.AppendLine("   |                        |    |                      (= ^ω^ =)│      │  | ");
            sb.AppendLine("   |                                                     ( > < ) │ 🐟🥛 │  | ");
            sb.AppendLine("   |  Nom nom! Delicious salmon treat feast! Purr-fect! 💖       └──────┘  | ");
            sb.AppendLine("   |                                                                      | ");
            sb.AppendLine(" ──┴──────────────────────────────────────────────────────────────────────┴─");

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
            sb.AppendLine(" 🛋️ ───[ 🐾 COZY LIVING ROOM ]─── 🪴 ─────────── 🐈 [ 🐟 TREAT CORNER ] ─── 🧶 ");
            sb.AppendLine("   |  🪟 [☁️ ☀️]         🐱 Scratch Post       📦 Box        🥛 Milk Bowl | ");
            sb.AppendLine("   |    |~|~|               |    |               |===|          ┌──────┐  | ");
            sb.AppendLine("   |                        |    |                              │      │  | ");
            sb.AppendLine("   |   /\\_/\\     🧶                                             │ 🐟🥛 │  | ");
            sb.AppendLine("   |  ( ='.' )~                                                 └──────┘  | ");
            sb.AppendLine("   |   > ^ <   Ready to focus... 🐾                                       | ");
            sb.AppendLine(" ──┴──────────────────────────────────────────────────────────────────────┴─");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🐱 Kitty is stretching and waiting to start the adventure towards the fish bowl!",
                BadgeText = "🐱 READY TO POUNCE",
                MiniLine = "[🐱 Ready ---> 🐟 Treat]"
            };
        }

        int cPos = Math.Clamp((int)(progress * 42), 0, 42);
        string pad = new string(' ', cPos);
        string gap = new string(' ', 42 - cPos);
        string paws = (f % 2 == 0) ? "  > ^ < " : "  < ^ > ";
        string yarn = (f % 2 == 0) ? "🧶~" : "~🧶";

        sb.AppendLine(" 🛋️ ───[ 🐾 COZY LIVING ROOM ]─── 🪴 ─────────── 🐈 [ 🐟 TREAT CORNER ] ─── 🧶 ");
        sb.AppendLine("   |  🪟 [☁️ ☀️]         🐱 Scratch Post       📦 Box        🥛 Milk Bowl | ");
        sb.AppendLine("   |    |~|~|               |    |               |===|          ┌──────┐  | ");
        sb.AppendLine("   |                        |    |                              │      │  | ");
        sb.AppendLine($"   | {pad} /\\_/\\   {yarn}{gap}           │      │  | ");
        sb.AppendLine($"   | {pad}( ='.' )~{gap}                 │ 🐟🥛 │  | ");
        sb.AppendLine($"   | {pad}{paws}{gap}                   └──────┘  | ");
        sb.AppendLine(" ──┴──►────────────────►───────────────►───────────────►──────────────────┴─");

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
            sb.AppendLine(" ╭─────────────────[ ☕ RAINY WINDOW COZY CAFE ]───────────────────╮  ☕ RAIN BREAK  ");
            sb.AppendLine(" │  +─────────────+─────────────+  │  Outside: 🌧️ 🌧️ 🌧️     │  Cozy Rest Break");
            sb.AppendLine(" │  | \\ \\   \\ \\   | \\ \\   \\ \\   |  │  Watching raindrops   │  Lo-fi beats 🌿 ");
            sb.AppendLine(" │  +─────────────+─────────────+  │  Warm tea & silence   │  Rest eyes 🛋️    ");
            sb.AppendLine(" │  |   ( ( (     |   ~ ~ ~ ~   |  │  Breathe deeply...    │  Warm cup ☕    ");
            sb.AppendLine(" │  |   [☕]      |   ~~~~~~~   |  │  Table: [☕ Tea] 🍰   │  Unwind mind ✨ ");
            sb.AppendLine(" │  +─────────────+─────────────+  │  Peaceful moments     │                 ");
            sb.AppendLine(" ╰─────────────────────────────────┴───────────────────────┴─────────────────╯");

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
            sb.AppendLine(" ╭─────────────────[ ☕ RAINY WINDOW COZY CAFE ]───────────────────╮  ✨ ☕ GOOD JOB! ");
            sb.AppendLine(" │  +─────────────+─────────────+  │  Outside: 🌧️ ✨ 🌧️     │  🏆 SESSION DONE");
            sb.AppendLine(" │  | ✨ GOOD ✨  | ✨ JOB! ✨  |  │  Finger-written glass │  Warm bokeh ✨  ");
            sb.AppendLine(" │  +─────────────+─────────────+  │  Session complete!    │  Celebration 🥐 ");
            sb.AppendLine(" │  |   ( ( (     |   ♡ ♡ ♡ ♡   |  │  Warm latte perfect   │  Delicious ☕   ");
            sb.AppendLine(" │  |   [☕]      |   ♡ ♡ ♡ ♡   |  │  Table: [☕ Mug] 🍰 🥐│  Great focus!   ");
            sb.AppendLine(" │  +─────────────+─────────────+  │  Fog art glowing      │                 ");
            sb.AppendLine(" ╰─────────────────────────────────┴───────────────────────┴─────────────────╯");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "☕ 'Good Job!' finger-written on the steamy rainy window! Enjoy your warm brew! ✨",
                BadgeText = "☕ GOOD JOB!",
                MiniLine = "[☕ 100% • ✨ Good Job! ✨]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine(" ╭─────────────────[ ☕ RAINY WINDOW COZY CAFE ]───────────────────╮  🌧️ Lo-Fi Chill ");
            sb.AppendLine(" │  +─────────────+─────────────+  │  Outside: 🌧️ 🌧️ 🌧️     │  Warm Cafe Vibe ");
            sb.AppendLine(" │  | \\ \\   \\ \\   | \\ \\   \\ \\   |  │  Rain trickling down  │  Lo-fi beats 🎵 ");
            sb.AppendLine(" │  +─────────────+─────────────+  │  Ready to focus...    │  Cozy corner 🌿 ");
            sb.AppendLine(" │  |   ( ( (     |             |  │  Fresh brew steaming  │  Fresh roast ☕ ");
            sb.AppendLine(" │  |   [☕]      |             |  │  Table: [☕ Mug] 🥐   │  Start timer!   ");
            sb.AppendLine(" │  +─────────────+─────────────+  │  Window clear glass   │                 ");
            sb.AppendLine(" ╰─────────────────────────────────┴───────────────────────┴─────────────────╯");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "☕ Cozy 4-pane rainy window is calm with steaming coffee. Start tracking to build focus!",
                BadgeText = "☕ CAFE READY",
                MiniLine = "[☕ Cozy Cafe Window Ready • 0%]"
            };
        }

        string steam = (f % 2 == 0) ? "( ( (" : ") ) )";
        string fogTop = progress switch
        {
            < 0.25 => "| \\ \\   \\ \\   | \\ \\   \\ \\   |",
            < 0.50 => "| \\~\\   \\~\\   | \\~\\   \\~\\   |",
            < 0.75 => "| ~~~   ~~~   | ~~~   ~~~   |",
            _ =>      "| ☁️☁️   ☁️☁️   | ☁️☁️   ☁️☁️   |"
        };

        string fogBottom1 = progress switch
        {
            < 0.25 => $"|   {steam}     |             |",
            < 0.50 => $"|   {steam}     |   ~ ~ ~ ~   |",
            < 0.75 => $"|   {steam}     |   ~~~~~~~   |",
            _ =>      $"|   {steam}     |   ☁️☁️☁️☁️   |"
        };

        string fogBottom2 = progress switch
        {
            < 0.25 => "|   [☕]      |             |",
            < 0.50 => "|   [☕]      |   ~ ~ ~ ~   |",
            < 0.75 => "|   [☕]      |   ~~~~~~~   |",
            _ =>      "|   [☕]      |   ☁️☁️☁️☁️   |"
        };

        int pct = (int)(progress * 100);

        sb.AppendLine(" ╭─────────────────[ ☕ RAINY WINDOW COZY CAFE ]───────────────────╮  🌧️ Rain Fog ");
        sb.AppendLine(" │  +─────────────+─────────────+  │  Outside: 🌧️ 🌧️ 🌧️     │  Warm Cafe Vibe ");
        sb.AppendLine($" │  {fogTop}  │  Rain trickling down  │  Lo-fi beats 🎵 ");
        sb.AppendLine($" │  +─────────────+─────────────+  │  Condensation: {pct,3}%   │  Cozy corner 🌿 ");
        sb.AppendLine($" │  {fogBottom1}  │  Steam from mug: {steam}│  Fresh roast ☕ ");
        sb.AppendLine($" │  {fogBottom2}  │  Table: [☕ Mug] 🥐 🍰 │  Deep focus...  ");
        sb.AppendLine(" │  +─────────────+─────────────+  │  Glass fogging up...  │                 ");
        sb.AppendLine(" ╰─────────────────────────────────┴───────────────────────┴─────────────────╯");

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
            sb.AppendLine(" ╭─────────────────[ 🎷 AUTUMN LAKESIDE JAZZ WINDOW ]──────────────────╮  🎷 JAZZ BREAK");
            sb.AppendLine(" │ 🍂  🌲 🌲 🍁   ⛰️  🌲 🍁   🌲 │ 🍂│  Soundtrack: Smooth Jazz Sax Solo 🎷 ☕ │ ");
            sb.AppendLine(" │ 🍁    🍂     🌲 🌲    🍂   │   │  Autumn leaves drifting on calm lake 🍂 │ ");
            sb.AppendLine(" │ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ │ 🍂│  Sip latte & enjoy mellow melody 🍂 │ ");
            sb.AppendLine(" ├───────────────────────────────┴───┴─────────────────────────────────────────┤ ");
            sb.AppendLine(" │  Wooden Table:   ( (      [☕ Latte]     🎷 Saxophone     📻 Lo-Fi Radio    │ ");
            sb.AppendLine(" ╰─────────────────────────────────────────────────────────────────────────────╯ ");
            sb.AppendLine("  🍁 🍂 🍁 🍂 🍁     Rest eyes, breathe deeply, and enjoy the jazz vibes 🍂 🍁 🍂 ");

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
            sb.AppendLine($" ╭─────────────────[ 🎷 AUTUMN LAKESIDE JAZZ WINDOW ]──────────────────╮  {sparkles}");
            sb.AppendLine(" │ 🍂  🌲 🌲 🍁   ⛰️  🌲 🍁   🌲 │ 🍂│  🏆 TARGET REACHED! Golden sunset glow! │ ");
            sb.AppendLine(" │ 🍁    🍂     🌲 🌲    🍂   │   │  Golden lake reflections & jazz harmony│ ");
            sb.AppendLine(" │ ✨ ~ 🌅 ~ 🍂 ~ ✨ ~ 🌅 ~ ✨ │ 🍂│  Perfect session completed with jazz! ✨│ ");
            sb.AppendLine(" ├───────────────────────────────┴───┴─────────────────────────────────────────┤ ");
            sb.AppendLine(" │  Wooden Table:   ( (      [☕ Latte]     🎷 Saxophone     📻 Lo-Fi Radio    │ ");
            sb.AppendLine(" ╰─────────────────────────────────────────────────────────────────────────────╯ ");
            sb.AppendLine("  🍁 🍂 🍁 🍂 🍁     Drifting Golden Autumn Sparkle Dust ✨ ✨ ✨     🍁 🍂 🍁 🍂 🍁");

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
            sb.AppendLine(" ╭─────────────────[ 🎷 AUTUMN LAKESIDE JAZZ WINDOW ]──────────────────╮  🍂 ☕ ");
            sb.AppendLine(" │ 🍂  🌲 🌲 🍁   ⛰️  🌲 🍁   🌲 │ 🍂│  Soundtrack: Smooth Jazz Trio 🎷 🎹 🎻 │ ");
            sb.AppendLine(" │ 🍁    🍂     🌲 🌲    🍂   │   │  Autumn lakeside view & drifting leaves │ ");
            sb.AppendLine(" │ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ │ 🍂│  Warm latte on table ready ☕       │ ");
            sb.AppendLine(" ├───────────────────────────────┴───┴─────────────────────────────────────────┤ ");
            sb.AppendLine(" │  Wooden Table:   ( (      [☕ Latte]     🎷 Saxophone     📻 Lo-Fi Radio    │ ");
            sb.AppendLine(" ╰─────────────────────────────────────────────────────────────────────────────╯ ");
            sb.AppendLine("  🍁 🍂 🍁 🍂 🍁     Start timer to tune into the mellow jazz vibe 🎷     🍂 🍁 🍂 ");

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
            < 0.33 => "│ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ │",
            < 0.66 => "│ ~ 🍂 ~ ~ ✨ ~ ~ 🍂 ~ ~ ✨ ~ │",
            _ =>      "│ ✨ ~ 🌅 ~ 🍂 ~ ✨ ~ 🌅 ~ 🍂 │"
        };

        sb.AppendLine($" ╭─────────────────[ 🎷 AUTUMN LAKESIDE JAZZ WINDOW ]──────────────────╮  🎷 {pct}% Jazz");
        sb.AppendLine($" │ 🍂  🌲 🌲 🍁   ⛰️  🌲 🍁   🌲 │ 🍂│  Soundtrack: {pct}% Jazz Flow {note,-8}     │ ");
        sb.AppendLine(" │ 🍁    🍂     🌲 🌲    🍂   │   │  Autumn breeze rustling through pines  │ ");
        sb.AppendLine($" {lakeDusk} 🍂│  Reflections on shimmering water       │ ");
        sb.AppendLine(" ├───────────────────────────────┴───┴─────────────────────────────────────────┤ ");
        sb.AppendLine($" │  Wooden Table:   {steam}       [☕ Latte]     🎷 Saxophone     📻 Lo-Fi Radio    │ ");
        sb.AppendLine(" ╰─────────────────────────────────────────────────────────────────────────────╯ ");
        sb.AppendLine("  🍁 🍂 🍁 🍂 🍁     Drifting Golden Autumn Leaves in the Breeze 🍂     🍁 🍂 🍁 🍂 ");

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
            sb.AppendLine(" ☀️  🌤️     (☁️)             🎈       [ 🍦 SWEET SUNDAE BREAK 🍧 ]       🌸 🌳 ");
            sb.AppendLine("   🌳      🍦 Vanilla • 🍓 Berry • 🍫 Choc • 🍵 Matcha • 🍧 Sorbet        🌳  ");
            sb.AppendLine("  ┌────────────────────────────────────────────────┐  🧺 Picnic lawn break... ");
            sb.AppendLine("  │  [🍦 SWEET TREATS]  👨‍🍳(•ᴗ•)  [🍨 🍧 🍦 🍰] │  Enjoying chilled sundae!  ");
            sb.AppendLine("  │  [🚐 ICE CREAM TRUCK]   🪑🧺   (o)(o)          │  \\(≧▽≦)/ [🍨🍓] Fresh! 🍓");
            sb.AppendLine("  └───(O)────────────(O)───────────────────────────┘  Strawberry scoop refresh");
            sb.AppendLine(" 🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸");
            sb.AppendLine("  🧺 Picnic Bench: [🍧 Sundae]  🎈 Balloons drifting   🥤 Cold Strawberry Shake ");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🍓 Enjoying a chilled strawberry sundae under shady park trees. Sweet rest break!",
                BadgeText = "🍦 SUNDAE BREAK",
                MiniLine = "[🍦 🍨 🍓 Sweet Sundae Break in Park]"
            };
        }

        if (isGoalReached)
        {
            sb.AppendLine(" 🎉  🎈  🌟    🎉 🍦 ICE CREAM PARTY! 🍨 🎈 🌟    🎉  🎈  🌟     🌸 🌳 ");
            sb.AppendLine("   🌳      🍦 Free Waffle Cones! 🍨 Giant Sundae Party! 🍰 Sprinkles!     🌳  ");
            sb.AppendLine("  ┌────────────────────────────────────────────────┐  🎉 ALL ORDERS SERVED!   ");
            sb.AppendLine("  │  [🍦 SWEET TREATS]  👩‍🍳(•ᴗ•)  [🍨 🍧 🍦 🍰] │  😋 😋 😋 Happy treats!  ");
            sb.AppendLine("  │  [🚐 ICE CREAM TRUCK]   \\(^o^)/  \\(^o^)/  \\(^o^)/│  Free scoops for everyone");
            sb.AppendLine("  └───(O)────────────(O)───────────────────────────┘  Best focus party ever!✨");
            sb.AppendLine(" 🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸");
            sb.AppendLine("  🧺 Picnic Bench: [🍧 Sundae]  🎈 Balloons drifting   🥤 Cold Strawberry Shake ");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🎉 ICE CREAM PARTY! All delicious orders served! Children cheering with waffle cone treats! 🍨✨",
                BadgeText = "🍦 ICE CREAM PARTY!",
                MiniLine = "[🍦 100% • 🍨 Ice Cream Party! 🎉]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine(" ☀️  🌤️     (☁️)             🎈       [ 🍦 PASTEL ICE CREAM TRUCK 🍨 ]   🌸 🌳 ");
            sb.AppendLine("   🌳      🍦 Vanilla • 🍓 Berry • 🍫 Choc • 🍵 Matcha • 🍧 Sorbet        🌳  ");
            sb.AppendLine("  ┌────────────────────────────────────────────────┐                          ");
            sb.AppendLine($"  │  [🍦 SWEET TREATS]  {vendorFace} [🍨 🍧 🍦 🍰] │  Ready to scoop flavors! ");
            sb.AppendLine("  │  [🚐 ICE CREAM TRUCK]                          │  Start timer to open stand");
            sb.AppendLine("  └───(O)────────────(O)───────────────────────────┘  Park picnic lawn ready  ");
            sb.AppendLine(" 🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸");
            sb.AppendLine("  🧺 Picnic Bench: [🍧 Sundae]  🎈 Balloons drifting   🥤 Cold Strawberry Shake ");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🍦 Pastel ice cream truck is open, vendor is ready and waiting for kids to arrive. Start tracking!",
                BadgeText = "🍦 TRUCK READY",
                MiniLine = "[🍦 Ice Cream Stand Ready • 0%]"
            };
        }

        // Active tracking
        int pct = (int)(progress * 100);
        int queueCount = Math.Clamp((int)(progress * 6) + 1, 1, 6);
        string kidsQueue = queueCount switch
        {
            1 => "🚶 (•‿•)   1 kid in line",
            2 => "🚶🚶 (•‿•) (^-^)  2 kids in line",
            3 => "🚶🚶🚶 (•‿•) (ᵔᵕᵔ) (^-^)  3 kids in line",
            4 => "🚶🚶🚶🚶 (•‿•) (ᵔᵕᵔ) (^-^) (•ᴗ•)  4 kids",
            5 => "🚶🚶🚶🚶🚶 (•‿•) (ᵔᵕᵔ) (^-^) (•ᴗ•) (≧▽≦)",
            _ => "🚶🚶🚶🚶🚶🚶 (•‿•) (ᵔᵕᵔ) (^-^) (•ᴗ•) (≧▽≦) (^o^)"
        };
        string queueDesc = $"{queueCount} kids in line";

        sb.AppendLine(" ☀️  🌤️     (☁️)             🎈       [ 🍦 PASTEL ICE CREAM TRUCK 🍨 ]   🌸 🌳 ");
        sb.AppendLine("   🌳      🍦 Vanilla • 🍓 Berry • 🍫 Choc • 🍵 Matcha • 🍧 Sorbet        🌳  ");
        sb.AppendLine("  ┌────────────────────────────────────────────────┐  ICE CREAM STAND FOCUS   ");
        sb.AppendLine($"  │  [🍦 SWEET TREATS]  {vendorFace} [🍨 🍧 🍦 🍰] │  Orders: {queueDesc,-15} ");
        sb.AppendLine($"  │  [🚐 ICE CREAM TRUCK]   {kidsQueue,-23}│  Focus Progress: {pct,3}%   ");
        sb.AppendLine("  └───(O)────────────(O)───────────────────────────┘  Park walkway treats 🍨  ");
        sb.AppendLine(" 🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸🌼🌸");
        sb.AppendLine("  🧺 Picnic Bench: [🍧 Sundae]  🎈 Balloons drifting   🥤 Cold Strawberry Shake ");

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
            sb.AppendLine(" ╭─────────────────[ 🟢 YAMANOTE LINE 🚇 • 🎧 🍵 TOKYO REST ]─────────────╮ ");
            sb.AppendLine($" │ {restSky} |  Warm Royal Milk Tea 🍵  •  Plush velvet metro catnap 💤 │ ");
            sb.AppendLine(" │ 🗼 🏙️  🏢  🌃 |  Lo-Fi beats soothing... Breathe and relax gently...     │ ");
            sb.AppendLine(" ├──────┬─────────┬─────────┬─────────┬─────────┬─────────┬──────┤ ");
            sb.AppendLine(" │  💺  │   [🟩 💺 🟩 💺 🟩]   │   🎧( ᴗ ᴗ)z Z 🧃 │   [🟩 💺 🟩 💺 🟩]   │  💺  │ ");
            sb.AppendLine(" │  🧃 Royal Milk Tea 🍵     📱 Metro Pass: OK      🎧 Lo-Fi Beats: REST    │ ");
            sb.AppendLine(" ╰────────────────────────────────────────────────────────────────────────╯ ");
            sb.AppendLine(" ───► [REST STATION 🍵] ─────────────────────────────────────────────────────── ");

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
            sb.AppendLine(" ╭─────────────────[ 🌟 TARGET STATION 🌟 • ✨ ARRIVED! ]──────────────────╮ ");
            sb.AppendLine($" │ 🗼 🏙️ 🏬 🚉 ✨ |  {notes} Goal Met! Destination reached! Outstanding! │ ");
            sb.AppendLine(" │ 🗼 🏙️  🏢  🌃 |  Victory sign v(^_^)v • Superb focus sprint session! ✨   │ ");
            sb.AppendLine(" ├──────┬─────────┬─────────┬─────────┬─────────┬─────────┬──────┤ ");
            sb.AppendLine(" │  💺  │   [🟩 💺 🟩 💺 🟩]   │   🎧(^o^)v ✨    │   [🟩 💺 🟩 💺 🟩]   │  💺  │ ");
            sb.AppendLine(" │  🧃 Royal Milk Tea 🍵     📱 Metro Pass: 100%    🎧 Track Complete! ✨   │ ");
            sb.AppendLine(" ╰────────────────────────────────────────────────────────────────────────╯ ");
            sb.AppendLine(" [🏁 SHIBUYA] ══════════► [SHINJUKU] ══════════► [AKIHABARA TARGET STATION ✨] ");

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
            sb.AppendLine(" ╭─────────────────[ 🟢 SHIBUYA ──► DEST • 🚇 TOKYO METRO ]────────────────╮ ");
            sb.AppendLine(" │ 🗼 Tokyo Tower  🌃 [新宿] 🏢 🏢  ⚡ [渋谷] 🏙️ 🏬  🌃 [秋葉原] 🗼 │ ");
            sb.AppendLine(" │ 🗼 🏙️  🏢  🌃 |  Putting on headphones... Lo-Fi beats ready ♪           │ ");
            sb.AppendLine(" ├──────┬─────────┬─────────┬─────────┬─────────┬─────────┬──────┤ ");
            sb.AppendLine(" │  💺  │   [🟩 💺 🟩 💺 🟩]   │   🎧(._. ) 🎒    │   [🟩 💺 🟩 💺 🟩]   │  💺  │ ");
            sb.AppendLine(" │  🧃 Royal Milk Tea 🍵     📱 Metro Pass: OK      🎧 Noise Cancel: READY  │ ");
            sb.AppendLine(" ╰────────────────────────────────────────────────────────────────────────╯ ");
            sb.AppendLine(" ───► [🟢 SHIBUYA ──► DEST] ────► [新宿] ────────► [池袋] ────────► [秋葉原 🏁] ──");

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
            "⚡ 🗼 ─ 🏢 ─► [新宿] 🏙️ 🏬",
            "💨 ─ 🏢 ─ 🗼 [渋谷] 🌃 🏬",
            "⚡ 🗼 ─ 🏙️ ─► [秋葉] 🏢 🌃",
            "💨 ─ 🏙️ ─ 🗼 [原宿] 🏙️ 🏢"
        };
        string cityLights = movingSkyline[f];
        string girlHead = (f % 2 == 0) ? "🎧(^_^)♪" : "🎧(^.^)♩";

        sb.AppendLine($" ╭─────────────────[ 🟢 YAMANOTE • FOCUS ──► {pct,3}% ]──────────────────╮ ");
        sb.AppendLine($" │ {cityLights,-25} |  Music: {musicNote} Lo-Fi chillhop beats ♪  │ ");
        sb.AppendLine(" │ 🗼 🏙️  🏢  🌃 |  Tokyo skyline flying past at express speed ⚡           │ ");
        sb.AppendLine(" ├──────┬─────────┬─────────┬─────────┬─────────┬─────────┬──────┤ ");
        sb.AppendLine($" │  💺  │   [🟩 💺 🟩 💺 🟩]   │    {girlHead} 🎒  │   [🟩 💺 🟩 💺 🟩]   │  💺  │ ");
        sb.AppendLine($" │  🧃 Royal Milk Tea 🍵     📱 Express Track: {pct,3}%  🎧 Deep Focus Zone ♪    │ ");
        sb.AppendLine(" ╰────────────────────────────────────────────────────────────────────────╯ ");
        sb.AppendLine(" ───► [渋谷] ──────────► [新宿] ──────────► [池袋] ──────────► [秋葉原 🏁] ─── ");

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
            sb.AppendLine(" ☀️  🌤️       🏃‍♂️ 🥤 HYDRATION & RECOVERY ZONE 🥤      🚩 REST TENT ⛺     ");
            sb.AppendLine("  👏 🥳 👏   🌲             🌲             🌲             🌲     [ 🥤 WATER STAND ] ");
            sb.AppendLine(" ═══════════════════════════════════════════════════════════════[ 💧 COOL DOWN ]   ");
            sb.AppendLine("   _o                  Cooling down on grass in the shade...           |    ");
            sb.AppendLine("  /|\\                  Drink water & catch breath! 💧                [---]  ");
            sb.AppendLine("  / \\                  Recharge muscles for next sprint!               |    ");
            sb.AppendLine(" ───────────────────────────────────────────────────────────────────────────");
            sb.AppendLine(" [⛺ RECOVERY TENT] ─── 💧 Hydrate ─── 🧊 Cool Down ─── 🧘 Stretch & Relax ─── ");

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
            sb.AppendLine(" ☀️  🏆 🎊 FINISH LINE CROSSED! 🎊 🏆      🚩 25%     🚩 50%     🚩 75%     🏁 100% ");
            sb.AppendLine("  👏 🥳 👏   🌲             🌲             🌲             🌲     [ 🏆 GOLD PODIUM ] ");
            sb.AppendLine(" ═══════════════════════════════════════════════════════════════[ 🏁 FINISH RIBBON ]");
            sb.AppendLine("                                                                \\o/  [ 🏁 BROKEN! ] ");
            sb.AppendLine("   CHAMPION! Broken finish line in record time! 🥇              /|\\  Gold medal! 🥇 ");
            sb.AppendLine("   Outstanding focus sprint! Gold trophy won! 🏆                / \\                 ");
            sb.AppendLine(" ═══════════════════════════════════════════════════════════════════════════════════");
            sb.AppendLine(" [🏆 GOLD MEDAL CHAMPION 🥇] ═══════════════════════════════ [🎉 CELEBRATION 🎊] ");

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
            sb.AppendLine(" ☀️  🌤️       🏟️ MARATHON STADIUM 🏃‍♂️      🚩 25%     🚩 50%     🚩 75%     🏁 FINISH");
            sb.AppendLine("  👏 🥳 👏   🌲             🌲             🌲             🌲     [ 🏆 GOLD PODIUM ] ");
            sb.AppendLine(" ═══════════════════════════════════════════════════════════════[ 🏁 FINISH LINE ] ");
            sb.AppendLine("   \\o                  Ready at start blocks... Ready to sprint!       |    ");
            sb.AppendLine("   /|\\                 Tying running shoe laces...                   [---]  ");
            sb.AppendLine("   / \\                 Start timer to sprint!                          |    ");
            sb.AppendLine(" ───────────────────────────────────────────────────────────────────────────");
            sb.AppendLine(" [🚩 START BLOCK] ──► 10km ──► 20km ──► 30km ──► 40km ──► [🏆 FINISH MEDAL 🥇] ");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🏃 Ready at the starting blocks. Start the timer to sprint towards the finish line!",
                BadgeText = "🏃 READY TO SPRINT",
                MiniLine = "[🏃 Start Line ---> 🏁 Finish]"
            };
        }

        int rPos = Math.Clamp((int)(progress * 46), 0, 46);
        string pad = new string(' ', rPos);
        string gap = new string(' ', 46 - rPos);

        string[] runner0 = { "  o  ", " /|\\ ", " / \\ " };
        string[] runner1 = { "  o  ", " /|/ ", " / \\ " };
        string[] runner2 = { " \\o  ", "  |\\ ", " / \\ " };

        var frame = (f % 3) switch
        {
            0 => runner0,
            1 => runner1,
            _ => runner2
        };

        sb.AppendLine(" ☀️  🌤️       🏟️ MARATHON STADIUM 🏃‍♂️      🚩 25%     🚩 50%     🚩 75%     🏁 FINISH");
        sb.AppendLine("  👏 🥳 👏   🌲             🌲             🌲             🌲     [ 🏆 GOLD PODIUM ] ");
        sb.AppendLine(" ═══════════════════════════════════════════════════════════════[ 🏁 FINISH LINE ] ");
        sb.AppendLine($" {pad}{frame[0]}{gap}        |    ");
        sb.AppendLine($" {pad}{frame[1]}{gap}      [---]  ");
        sb.AppendLine($" {pad}{frame[2]}{gap}        |    ");
        sb.AppendLine(" ───────────────────────────────────────────────────────────────────────────");
        sb.AppendLine(" [🚩 START BLOCK] ──► 10km ──► 20km ──► 30km ──► 40km ──► [🏆 FINISH MEDAL 🥇] ");

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
        int barCount = Math.Clamp((int)(progress * 16), 0, 16);
        string progressBar = new string('█', barCount) + new string('░', 16 - barCount);

        if (isRestPhase)
        {
            sb.AppendLine(" ╔═══════════════════════[ 👾 VIRTUAL FOCUS PET TAMAGOTCHI ]═══════════════════════╗ ");
            sb.AppendLine($" ║  ⭐ Level {level,-2} ({currentXp,2}% XP)     💖 Happiness: {happiness,3}%     💤 Energy: [{hpDisplay}] (Recharge)║ ");
            sb.AppendLine(" ║  ┌────────────────────────────────────────────────────────────────────────┐  ║ ");
            sb.AppendLine(" ║  │   👾  ( - . - ) z Z Z   Pet is sleeping peacefully in nightcap... 💤   │  ║ ");
            sb.AppendLine($" ║  │   Status: Cozy nap recharging energy... Focus XP: {currentXp,2}%                     │  ║ ");
            sb.AppendLine(" ║  └────────────────────────────────────────────────────────────────────────┘  ║ ");
            sb.AppendLine(" ║     [ 🍎 FEED ]          [ 🎮 PLAY ]          [ 🧹 CLEAN ]          [ 💤 REST ]    ║ ");
            sb.AppendLine(" ╚══════════════════════════════════════════════════════════════════════════════════╝ ");
        }
        else if (isGoalReached)
        {
            sb.AppendLine(" ╔═══════════════════════[ 👾 VIRTUAL FOCUS PET TAMAGOTCHI ]═══════════════════════╗ ");
            sb.AppendLine($" ║  ⭐ Level {level,-2} ({currentXp,2}% XP)     💖 Happiness: {happiness,3}%     ✨ Energy: [■■■■■] (MAX)   ║ ");
            sb.AppendLine(" ║  ┌────────────────────────────────────────────────────────────────────────┐  ║ ");
            sb.AppendLine(" ║  │   💖  \\(^ヮ^)/ ✨💖   QUEST COMPLETE! Level up & star candy party! ✨   │  ║ ");
            sb.AppendLine(" ║  │   Status: Quest complete! Outstanding! Progress: [████████████████] 100% │  ║ ");
            sb.AppendLine(" ║  └────────────────────────────────────────────────────────────────────────┘  ║ ");
            sb.AppendLine(" ║     [ 🍎 FEED ]          [ 🎮 PLAY ]          [ 🧹 CLEAN ]          [ 💤 REST ]    ║ ");
            sb.AppendLine(" ╚══════════════════════════════════════════════════════════════════════════════════╝ ");
        }
        else if (!isTracking)
        {
            sb.AppendLine(" ╔═══════════════════════[ 👾 VIRTUAL FOCUS PET TAMAGOTCHI ]═══════════════════════╗ ");
            sb.AppendLine($" ║  ⭐ Level {level,-2} ({currentXp,2}% XP)     💖 Happiness: {happiness,3}%     💎 Energy: [{hpDisplay}]       ║ ");
            sb.AppendLine(" ║  ┌────────────────────────────────────────────────────────────────────────┐  ║ ");
            sb.AppendLine(" ║  │   👾  ( • ‿ • ) 💬   Focus pet is ready on console for adventure!       │  ║ ");
            sb.AppendLine(" ║  │   Status: Ready for adventure!       Progress: [░░░░░░░░░░░░░░░░]   0% │  ║ ");
            sb.AppendLine(" ║  └────────────────────────────────────────────────────────────────────────┘  ║ ");
            sb.AppendLine(" ║     [ 🍎 FEED ]          [ 🎮 PLAY ]          [ 🧹 CLEAN ]          [ 💤 REST ]    ║ ");
            sb.AppendLine(" ╚══════════════════════════════════════════════════════════════════════════════════╝ ");
        }
        else
        {
            sb.AppendLine(" ╔═══════════════════════[ 👾 VIRTUAL FOCUS PET TAMAGOTCHI ]═══════════════════════╗ ");
            sb.AppendLine($" ║  ⭐ Level {level,-2} ({currentXp,2}% XP)     💖 Happiness: {happiness,3}%     💎 Energy: [{hpDisplay}]       ║ ");
            sb.AppendLine(" ║  ┌────────────────────────────────────────────────────────────────────────┐  ║ ");
            sb.AppendLine($" ║  │   👾  {moodFace,-18}   Cheering your focus sprint! Keep going! ✨     │  ║ ");
            sb.AppendLine($" ║  │   Status: Deep focus zone           Progress: [{progressBar}] {pct,3}% │  ║ ");
            sb.AppendLine(" ║  └────────────────────────────────────────────────────────────────────────┘  ║ ");
            sb.AppendLine(" ║     [ 🍎 FEED ]          [ 🎮 PLAY ]          [ 🧹 CLEAN ]          [ 💤 REST ]    ║ ");
            sb.AppendLine(" ╚══════════════════════════════════════════════════════════════════════════════════╝ ");
        }

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

    #region Scene 7: 🌲 Lumberjack Wood Chopping

    private static RenderedArtScene RenderLumberjackScene(
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
            sb.AppendLine(" 🌲 ───[ 🌲 PINE FOREST TIMBER SPRINT ]─── ⛰️ ────────── [ ☕ CAMPFIRE REST ] ── 🪵 ");
            sb.AppendLine("   ⛰️   ⛰️\\_   (☁️)             🔥             🚐             [ 🪵 LOG YARD ]   ");
            sb.AppendLine("    .-~~~~-.                 ( ) )          [===]o-o                          ");
            sb.AppendLine("   (  ~~~~  )               (_____)         ||   Cozy Campfire!               ");
            sb.AppendLine("    _o  ☕                   _|_            ||   Rest & warm up by the van    ");
            sb.AppendLine("    /|\\  Sipping hot campfire coffee...                                       ");
            sb.AppendLine(" ─────────────────────────────────────────────────────────────────────────────");
            sb.AppendLine("  🌲 🪵 🪵   🪓 [Chopping Block]     🪵 🪵 🪵 Firewood Stack      🚐 Timber Transport ");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "☕ Lumberjack is relaxing by the warm campfire sipping hot coffee. Rest and recharge!",
                BadgeText = "☕ CAMPFIRE REST",
                MiniLine = "[🔥 ☕ Lumberjack Rest by Campfire]"
            };
        }

        if (isGoalReached)
        {
            string sparkles = (f % 2 == 0) ? "✨ 🌲 TIMBER! FOREST CLEARED! 🌲 ✨" : "🌟 🌲 TIMBER! VAN PACKED! 🌲 🌟";
            sb.AppendLine($" 🌲 ───[ {sparkles} ]─── 🪵 ");
            sb.AppendLine("   ⛰️   ⛰️\\_   (☁️)           (☁️)             🌲             [ 🪵 LOG YARD ]   ");
            sb.AppendLine("   _|_   _|_   _|_   _|_   _|_                     [========] 💨              ");
            sb.AppendLine("   (o)   (o)   (o)   (o)   (o)                     [======] 🚐                ");
            sb.AppendLine("   \\o/  All trees chopped!                          o---o-o  Ready home!       ");
            sb.AppendLine("   /|\\  Van packed full of timber! Ready to drive home!                       ");
            sb.AppendLine(" ─────────────────────────────────────────────────────────────────────────────");
            sb.AppendLine("  🌲 🪵 🪵   🪓 [Chopping Block]     🪵 🪵 🪵 Firewood Stack      🚐 Timber Transport ");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🚚 TIMBER! All trees chopped down! Van packed to the roof with firewood ready to drive home! 🎉",
                BadgeText = "🚚 VAN FULL PACKED!",
                MiniLine = "[🌲 Cleared Forest! Van Packed Full of Wood! 🎉]"
            };
        }

        if (!isTracking)
        {
            sb.AppendLine(" 🌲 ───[ 🌲 PINE FOREST TIMBER SPRINT ]─── ⛰️ ────────── [ 🚐 LUMBER VAN ] ─── 🪵 ");
            sb.AppendLine("   ⛰️   ⛰️\\_   (☁️)           (☁️)             🌲             [ 🪵 LOG YARD ]   ");
            sb.AppendLine("   /|\\   /|\\   /|\\   /|\\   /|\\                     [___]                      ");
            sb.AppendLine("   /|\\   /|\\   /|\\   /|\\   /|\\                     [   ] 🚐                   ");
            sb.AppendLine("   ||    ||    ||    ||    ||                      o-o-o                      ");
            sb.AppendLine("  _o/P Full forest to chop...                       Van empty & ready!        ");
            sb.AppendLine("  /|\\  Start timer to swing the axe!                                          ");
            sb.AppendLine(" ─────────────────────────────────────────────────────────────────────────────");

            return new RenderedArtScene
            {
                AsciiArt = sb.ToString(),
                StoryText = "🌲 Lumberjack stands ready in the dense pine forest. Start the timer to begin chopping wood!",
                BadgeText = "🌲 READY TO CHOP",
                MiniLine = "[🌲 Full Forest ---> 🚐 Timber Van]"
            };
        }

        // Active tracking: Trees getting chopped down (0 to 5)
        int totalTrees = 5;
        int choppedCount = Math.Clamp((int)(progress * totalTrees), 0, totalTrees);

        string vanLoadTop = choppedCount switch
        {
            0 => " [___]  ",
            1 => " [=__]  ",
            2 => " [==_]  ",
            3 => " [===]  ",
            4 => " [====] ",
            _ => " [=====]"
        };

        string vanLoadBottom = choppedCount switch
        {
            0 => " [   ] 🚐",
            1 => " [=  ] 🚐",
            2 => " [== ] 🚐",
            3 => " [===] 🚐",
            4 => " [====]🚐",
            _ => " [=====]🚐"
        };

        // Lumberjack axe swing frames
        string[] axeFrames0 = { "  _o/P  ", "  /|\\   ", "  / \\   " };
        string[] axeFrames1 = { "  _o|P  ", "  /|    ", "  / \\   " };
        string[] axeFrames2 = { "  _o\\P* ", "  /|    ", "  / \\   " };

        string[] jackFrame = (f % 3) switch
        {
            0 => axeFrames0,
            1 => axeFrames1,
            _ => axeFrames2
        };

        // Render trees and stumps
        var treeLine1 = new StringBuilder();
        var treeLine2 = new StringBuilder();
        var treeLine3 = new StringBuilder();

        for (int i = 0; i < totalTrees; i++)
        {
            if (i < choppedCount)
            {
                treeLine1.Append("       ");
                treeLine2.Append("  _|_  ");
                treeLine3.Append("  (o)  ");
            }
            else
            {
                treeLine1.Append("  /|\\  ");
                treeLine2.Append("  /|\\  ");
                treeLine3.Append("  ||   ");
            }
        }

        int pct = (int)(progress * 100);

        sb.AppendLine($" 🌲 ───[ 🌲 PINE FOREST TIMBER SPRINT • {pct,3}% ]─── ⛰️ ──────── [ 🚐 LUMBER VAN ] ─── 🪵 ");
        sb.AppendLine("   ⛰️   ⛰️\\_   (☁️)           (☁️)             🌲             [ 🪵 LOG YARD ]   ");
        sb.AppendLine($" {treeLine1}              {vanLoadTop}");
        sb.AppendLine($" {treeLine2}              {vanLoadBottom}");
        sb.AppendLine($" {treeLine3}                o---o-o-o");
        sb.AppendLine($" {jackFrame[0]} Wood chips flying! ({choppedCount}/5 trees down) • Van: {pct}% Full");
        sb.AppendLine($" {jackFrame[1]} Chopping timber logs for the van...");
        sb.AppendLine(" ─────────────────────────────────────────────────────────────────────────────");

        return new RenderedArtScene
        {
            AsciiArt = sb.ToString(),
            StoryText = $"🌲 Chopping forest • {choppedCount}/5 trees chopped down • Van {pct}% loaded with firewood!",
            BadgeText = $"🌲 {pct}% CHOPPING",
            MiniLine = $"[🌲 {choppedCount}/5 Trees Cut • 🚐 Van {pct}% Full]"
        };
    }

    #endregion
}
