using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace VitorsWeeklyWorkTracking.Controls;

public partial class VisualCompanionControl : UserControl
{
    private readonly DispatcherTimer _animTimer = new();
    private int _frameTick = 0;

    public bool IsMiniMode { get; set; } = false;
    public string SceneId { get; set; } = "cycling";
    public double ProgressFraction { get; set; } = 0.0;
    public bool IsTracking { get; set; } = false;
    public bool IsGoalReached { get; set; } = false;
    public bool IsRestPhase { get; set; } = false;
    public string ContextDetails { get; set; } = string.Empty;
    public int FocusXp { get; set; } = 0;
    public int PetHappiness { get; set; } = 100;

    private int _cheerAnimationTicks = 0;
    private int _goalReachedStartTick = -1;

    public event Action? Clicked;

    public VisualCompanionControl()
    {
        InitializeComponent();

        _animTimer.Interval = TimeSpan.FromMilliseconds(75);
        _animTimer.Tick += (s, e) =>
        {
            _frameTick++;
            if (_cheerAnimationTicks > 0) _cheerAnimationTicks--;
            InvalidateVisual();
        };
        _animTimer.Start();
    }

    public void TriggerCheer()
    {
        _cheerAnimationTicks = 25; // ~2 seconds of extra sparkles and cheer bounce
        InvalidateVisual();
    }

    public void UpdateState(
        string sceneId,
        double progressFraction,
        bool isTracking,
        bool isGoalReached,
        bool isRestPhase,
        string contextDetails,
        int focusXp = 0,
        int petHappiness = 100)
    {
        SceneId = string.IsNullOrEmpty(sceneId) ? "cycling" : sceneId;
        ProgressFraction = Math.Clamp(progressFraction, 0.0, 1.0);
        IsTracking = isTracking;

        // Track exactly when the goal-reached state first becomes true so end-of-session
        // sequences (e.g. the ice cream truck's end-of-shift drive-away) can play once in order
        // instead of jumping to a random point based on the shared ambient frame counter.
        if (isGoalReached && !IsGoalReached)
        {
            _goalReachedStartTick = _frameTick;
        }
        else if (!isGoalReached)
        {
            _goalReachedStartTick = -1;
        }

        IsGoalReached = isGoalReached;
        IsRestPhase = isRestPhase;
        ContextDetails = contextDetails;
        FocusXp = focusXp;
        PetHappiness = petHappiness;

        InvalidateVisual();
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        e.Handled = true;
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        TriggerCheer();
        Clicked?.Invoke();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        TriggerCheer();
        Clicked?.Invoke();
        e.Handled = true;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth > 10 ? ActualWidth : (IsMiniMode ? 280 : 600);
        double h = ActualHeight > 10 ? ActualHeight : (IsMiniMode ? 65 : 140);

        if (w <= 0 || h <= 0) return;

        // Clip to rounded border area
        var clipRect = new Rect(0, 0, w, h);
        var clipGeom = new RectangleGeometry(clipRect, 8, 8);
        dc.PushClip(clipGeom);

        switch (SceneId)
        {
            case "cafe":
                RenderCafeScene(dc, w, h);
                break;
            case "coffeejazz":
                RenderCoffeeJazzScene(dc, w, h);
                break;
            case "icecream":
                RenderIceCreamScene(dc, w, h);
                break;
            case "metro":
                RenderMetroScene(dc, w, h);
                break;
            case "rocket":
                RenderRocketScene(dc, w, h);
                break;
            case "cat":
                RenderCatScene(dc, w, h);
                break;
            case "runner":
                RenderRunnerScene(dc, w, h);
                break;
            case "tamagotchi":
                RenderTamagotchiScene(dc, w, h);
                break;
            case "cycling":
            default:
                RenderCyclingScene(dc, w, h);
                break;
        }

        // Cheer overlay celebration matching the active scene theme
        if (_cheerAnimationTicks > 0)
        {
            RenderSceneCelebration(dc, w, h);
        }

        dc.Pop(); // Pop clip
    }

    #region Helper Drawing Methods & Celebrations

    private static FormattedText CreateText(string text, double size, Brush brush, FontWeight? weight = null)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI, sans-serif"), FontStyles.Normal, weight ?? FontWeights.Normal, FontStretches.Normal),
            size,
            brush,
            1.0);
    }

    private void RenderSceneCelebration(DrawingContext dc, double w, double h)
    {
        double cheerProgress = Math.Clamp(1.0 - (_cheerAnimationTicks / 28.0), 0.0, 1.0);
        double cheerFade = _cheerAnimationTicks <= 7 ? (_cheerAnimationTicks / 7.0) : 1.0;

        switch (SceneId)
        {
            case "icecream":
                RenderIceCreamCelebration(dc, w, h, cheerProgress, cheerFade);
                break;
            case "metro":
                RenderMetroCelebration(dc, w, h, cheerProgress, cheerFade);
                break;
            case "cycling":
                RenderCyclingCelebration(dc, w, h, cheerProgress, cheerFade);
                break;
            case "cafe":
                RenderCafeCelebration(dc, w, h, cheerProgress, cheerFade);
                break;
            case "coffeejazz":
                RenderCoffeeJazzCelebration(dc, w, h, cheerProgress, cheerFade);
                break;
            case "rocket":
                RenderRocketCelebration(dc, w, h, cheerProgress, cheerFade);
                break;
            case "cat":
                RenderCatCelebration(dc, w, h, cheerProgress, cheerFade);
                break;
            case "runner":
                RenderRunnerCelebration(dc, w, h, cheerProgress, cheerFade);
                break;
            case "tamagotchi":
                RenderTamagotchiCelebration(dc, w, h, cheerProgress, cheerFade);
                break;
            default:
                RenderDefaultCelebration(dc, w, h, cheerProgress, cheerFade);
                break;
        }
    }

    private void RenderIceCreamCelebration(DrawingContext dc, double w, double h, double cheerProgress, double cheerFade)
    {
        // 1. Shimmering pastel rainbow ribbon across the sky
        if (!IsMiniMode)
        {
            var rainbowBrush = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new(Color.FromArgb((byte)(110 * cheerFade), 255, 140, 170), 0.0),
                    new(Color.FromArgb((byte)(110 * cheerFade), 255, 215, 110), 0.35),
                    new(Color.FromArgb((byte)(110 * cheerFade), 130, 225, 200), 0.70),
                    new(Color.FromArgb((byte)(110 * cheerFade), 195, 175, 255), 1.0)
                },
                new Point(0, 0),
                new Point(1, 0));

            var arcGeom = new PathGeometry();
            var arcFig = new PathFigure { StartPoint = new Point(0, h * 0.40) };
            arcFig.Segments.Add(new QuadraticBezierSegment(new Point(w * 0.5, h * 0.05 - (cheerProgress * 15)), new Point(w, h * 0.40), true));
            arcFig.Segments.Add(new LineSegment(new Point(w, h * 0.40 + 8), true));
            arcFig.Segments.Add(new QuadraticBezierSegment(new Point(w * 0.5, h * 0.05 - (cheerProgress * 15) + 8), new Point(0, h * 0.40 + 8), true));
            arcFig.IsClosed = true;
            arcGeom.Figures.Add(arcFig);
            dc.DrawGeometry(rainbowBrush, null, arcGeom);
        }

        // 2. Pastel Sprinkles Confetti Blast
        var rand = new Random(333 + (_frameTick / 2));
        var sprinkleColors = new[]
        {
            Color.FromRgb(255, 130, 160), Color.FromRgb(255, 220, 95), Color.FromRgb(120, 225, 195),
            Color.FromRgb(175, 150, 245), Color.FromRgb(255, 170, 120), Color.FromRgb(255, 255, 255)
        };

        int sprinkleCount = IsMiniMode ? 14 : 26;
        for (int i = 0; i < sprinkleCount; i++)
        {
            double angle = (i / (double)sprinkleCount) * Math.PI * 1.8 - 0.4;
            double speed = 25.0 + (rand.NextDouble() * (IsMiniMode ? 60 : 120));
            double burstDist = speed * Math.Sin(cheerProgress * Math.PI * 0.5);
            double sx = (w * 0.45) + (Math.Cos(angle) * burstDist);
            double sy = (h * 0.55) - (Math.Sin(angle) * burstDist * 0.75) + (cheerProgress * cheerProgress * 45);

            var spColor = sprinkleColors[i % sprinkleColors.Length];
            var spBrush = new SolidColorBrush(Color.FromArgb((byte)(240 * cheerFade), spColor.R, spColor.G, spColor.B));

            if (i % 3 == 0)
            {
                DrawSparkle(dc, sx, sy, (IsMiniMode ? 3.0 : 4.5) * cheerFade, spBrush);
            }
            else
            {
                dc.PushTransform(new RotateTransform((_frameTick * 5.0) + (i * 35), sx, sy));
                dc.DrawRoundedRectangle(spBrush, null, new Rect(sx - 1.5, sy - 3.5, 3.0, 7.0), 1.0, 1.0);
                dc.Pop();
            }
        }

        // 3. Floating Ice Cream Icons & Treats
        var treatIcons = new[] { "🍦", "🍓", "🍨", "🍒", "🍧" };
        int treatCount = IsMiniMode ? 3 : 5;
        for (int t = 0; t < treatCount; t++)
        {
            double tx = (w * 0.15) + (t * (w * 0.70 / Math.Max(1, treatCount - 1))) + (Math.Sin((_frameTick * 0.1) + t) * 6);
            double ty = (h * 0.75) - (cheerProgress * (h * 0.65)) + (Math.Cos((_frameTick * 0.12) + t) * 4);
            double scaleTreat = IsMiniMode ? 10 : 14;

            var haloBrush = new RadialGradientBrush(Color.FromArgb((byte)(120 * cheerFade), 255, 235, 200), Color.FromArgb(0, 255, 235, 200));
            dc.DrawEllipse(haloBrush, null, new Point(tx + 4, ty + 4), scaleTreat * 1.3, scaleTreat * 1.3);

            var ft = CreateText(treatIcons[t % treatIcons.Length], scaleTreat, new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 255, 255, 255)), FontWeights.Bold);
            dc.DrawText(ft, new Point(tx - (ft.Width * 0.5), ty - (ft.Height * 0.5)));
        }

        // 4. Celebration Floating Banner
        string msg = IsMiniMode ? "🍦 SWEET PROGRESS! 🎉" : "✨ 🍦 SWEET FOCUS PROGRESS! 🍨 ✨";
        var bg = new LinearGradientBrush(Color.FromArgb((byte)(235 * cheerFade), 255, 215, 230), Color.FromArgb((byte)(235 * cheerFade), 220, 245, 240), new Point(0, 0), new Point(1, 1));
        var border = new Pen(new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), 255, 120, 160)), 1.4);
        var textBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 110, 45, 75));
        DrawCelebrationBanner(dc, w, h, msg, bg, border, textBrush, cheerProgress, cheerFade);
    }

    private void RenderMetroCelebration(DrawingContext dc, double w, double h, double cheerProgress, double cheerFade)
    {
        // 1. Cyberpunk Neon Bokeh Discs floating in background
        var neonColors = new[]
        {
            Color.FromRgb(0, 240, 255),   // Cyan
            Color.FromRgb(255, 0, 135),   // Hot Magenta
            Color.FromRgb(160, 40, 255),  // Electric Violet
            Color.FromRgb(255, 230, 0)    // Neon Yellow
        };

        var rand = new Random(555 + (_frameTick / 2));
        int bokehCount = IsMiniMode ? 6 : 12;
        for (int i = 0; i < bokehCount; i++)
        {
            double bx = (i / (double)bokehCount) * w + (Math.Sin((_frameTick * 0.08) + i) * 15);
            double by = (h * 0.65) - (cheerProgress * (h * 0.55)) + (rand.NextDouble() * 20);
            double br = (IsMiniMode ? 6 : 12) + (rand.NextDouble() * 6);
            var col = neonColors[i % neonColors.Length];

            var bokehBrush = new RadialGradientBrush(Color.FromArgb((byte)(160 * cheerFade), col.R, col.G, col.B), Color.FromArgb(0, col.R, col.G, col.B));
            dc.DrawEllipse(bokehBrush, null, new Point(bx, by), br, br);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)(190 * cheerFade), 255, 255, 255)), null, new Point(bx, by), br * 0.25, br * 0.25);
        }

        // 2. Animated Jumping Equalizer Wavebars along window
        if (!IsMiniMode)
        {
            int eqBars = 16;
            double eqW = w * 0.40;
            double eqStartX = w * 0.08;
            double eqY = h * 0.62;
            double barWidth = (eqW / eqBars) * 0.65;

            for (int b = 0; b < eqBars; b++)
            {
                double barH = (Math.Sin((_frameTick * 0.4) + (b * 0.7)) * 0.5 + 0.5) * (18 * cheerFade);
                double barX = eqStartX + (b * (eqW / eqBars));
                var eqGrad = new LinearGradientBrush(Color.FromArgb((byte)(220 * cheerFade), 0, 240, 255), Color.FromArgb((byte)(220 * cheerFade), 255, 0, 135), new Point(0, 1), new Point(0, 0));
                dc.DrawRoundedRectangle(eqGrad, null, new Rect(barX, eqY - barH, barWidth, barH + 2), 1.5, 1.5);
            }
        }

        // 3. Floating Neon Music Notes (🎧 ♫ 🎵 ♩)
        var notes = new[] { "🎧", "♫", "🎵", "♩", "✨" };
        for (int n = 0; n < (IsMiniMode ? 3 : 5); n++)
        {
            double nx = (w * 0.60) + (Math.Sin((_frameTick * 0.12) + n) * (w * 0.18));
            double ny = (h * 0.65) - (cheerProgress * (h * 0.60)) + (n * (IsMiniMode ? 6 : 10));
            var ft = CreateText(notes[n % notes.Length], IsMiniMode ? 10 : 13, new SolidColorBrush(Color.FromArgb((byte)(240 * cheerFade), 0, 240, 255)), FontWeights.Bold);
            dc.DrawText(ft, new Point(nx, ny));
        }

        // 4. Cyberpunk Neon Floating Badge
        string msg = IsMiniMode ? "🎧 LO-FI FLOW! ⚡" : "⚡ 🎧 LO-FI GROOVE! TOKYO BEATS 🚉 ✨";
        var bg = new LinearGradientBrush(Color.FromArgb((byte)(240 * cheerFade), 25, 20, 50), Color.FromArgb((byte)(240 * cheerFade), 45, 15, 65), new Point(0, 0), new Point(1, 1));
        var border = new Pen(new SolidColorBrush(Color.FromArgb((byte)(240 * cheerFade), 0, 240, 255)), 1.5);
        var textBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 255, 255, 255));
        DrawCelebrationBanner(dc, w, h, msg, bg, border, textBrush, cheerProgress, cheerFade);
    }

    private void RenderCyclingCelebration(DrawingContext dc, double w, double h, double cheerProgress, double cheerFade)
    {
        // 1. Golden Sunburst Rays flashing from top-right
        double sunX = w * 0.88;
        double sunY = h * 0.20;
        int rayCount = IsMiniMode ? 8 : 14;
        for (int r = 0; r < rayCount; r++)
        {
            double angle = (r * Math.PI * 2.0 / rayCount) + (_frameTick * 0.03);
            double r1 = IsMiniMode ? 14 : 24;
            double r2 = r1 + (cheerProgress * (IsMiniMode ? 40 : 80));
            var rayPen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(160 * cheerFade), 255, 225, 90)), 2.0);
            dc.DrawLine(rayPen, new Point(sunX + (Math.Cos(angle) * r1), sunY + (Math.Sin(angle) * r1)), new Point(sunX + (Math.Cos(angle) * r2), sunY + (Math.Sin(angle) * r2)));
        }

        // 2. Swirling Wind Streaks & Floating Leaves / Petals
        var leafColors = new[]
        {
            Color.FromRgb(255, 180, 70),  // Golden amber
            Color.FromRgb(255, 140, 170), // Cherry blossom pink
            Color.FromRgb(140, 215, 110), // Meadow green
            Color.FromRgb(255, 110, 60)   // Autumn red
        };

        int leafCount = IsMiniMode ? 6 : 12;
        for (int i = 0; i < leafCount; i++)
        {
            double lx = (w * 0.05) + ((_frameTick * 4.5 + (i * 45)) % (w + 40)) - 20;
            double ly = (h * 0.40) + (Math.Sin((_frameTick * 0.15) + i) * (h * 0.22));
            var lBrush = new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), leafColors[i % leafColors.Length].R, leafColors[i % leafColors.Length].G, leafColors[i % leafColors.Length].B));
            DrawLeaf(dc, lx, ly, (IsMiniMode ? 3.5 : 5.5) * cheerFade, (_frameTick * 0.1) + i, lBrush);
        }

        // 3. Golden Starbursts
        for (int s = 0; s < (IsMiniMode ? 4 : 8); s++)
        {
            double sx = (w * 0.15) + (s * (w * 0.70 / (IsMiniMode ? 4 : 8)));
            double sy = (h * 0.35) + (Math.Sin((_frameTick * 0.2) + s) * 12);
            DrawStar5(dc, sx, sy, 5.0 * cheerFade, 2.5 * cheerFade, new SolidColorBrush(Color.FromArgb((byte)(230 * cheerFade), 255, 220, 60)));
        }

        // 4. Floating Banner
        string msg = IsMiniMode ? "🚴 SPEED BOOST! 🌟" : "✨ 🚴 SPEED BOOST! KEEP PEDALING! 🌟 ✨";
        var bg = new LinearGradientBrush(Color.FromArgb((byte)(235 * cheerFade), 255, 240, 195), Color.FromArgb((byte)(235 * cheerFade), 255, 205, 120), new Point(0, 0), new Point(1, 1));
        var border = new Pen(new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), 225, 150, 30)), 1.4);
        var textBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 85, 45, 10));
        DrawCelebrationBanner(dc, w, h, msg, bg, border, textBrush, cheerProgress, cheerFade);
    }

    private void RenderCafeCelebration(DrawingContext dc, double w, double h, double cheerProgress, double cheerFade)
    {
        // 1. Rising Glowing Fireplace Embers
        var emberColors = new[]
        {
            Color.FromRgb(255, 200, 80),  // Gold
            Color.FromRgb(255, 120, 50),  // Orange
            Color.FromRgb(255, 70, 60)    // Fiery red
        };

        var rand = new Random(777 + (_frameTick / 2));
        int emberCount = IsMiniMode ? 10 : 20;
        for (int e = 0; e < emberCount; e++)
        {
            double ex = (w * 0.20) + (rand.NextDouble() * (w * 0.60)) + (Math.Sin((_frameTick * 0.1) + e) * 8);
            double ey = (h * 0.85) - (cheerProgress * (h * 0.75)) + (rand.NextDouble() * 15);
            var col = emberColors[e % emberColors.Length];
            var eBrush = new SolidColorBrush(Color.FromArgb((byte)(210 * cheerFade), col.R, col.G, col.B));
            DrawSparkle(dc, ex, ey, (IsMiniMode ? 2.5 : 4.0) * cheerFade, eBrush);
        }

        // 2. Warm Steam Hearts rising from cocoa mug
        for (int sh = 0; sh < (IsMiniMode ? 2 : 4); sh++)
        {
            double hx = (w * 0.48) + (Math.Sin((_frameTick * 0.12) + sh) * 12);
            double hy = (h * 0.60) - (cheerProgress * (h * 0.50)) - (sh * (IsMiniMode ? 8 : 14));
            DrawHeart(dc, hx, hy, (IsMiniMode ? 4.0 : 6.5) * cheerFade, new SolidColorBrush(Color.FromArgb((byte)(200 * cheerFade), 255, 150, 180)));
        }

        // 3. Floating Banner
        string msg = IsMiniMode ? "🔥 STAY COZY! ☕" : "✨ 🔥 COZY HEARTH & WARM FOCUS! ☕ ✨";
        var bg = new LinearGradientBrush(Color.FromArgb((byte)(235 * cheerFade), 95, 45, 30), Color.FromArgb((byte)(235 * cheerFade), 60, 25, 20), new Point(0, 0), new Point(1, 1));
        var border = new Pen(new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), 255, 165, 80)), 1.4);
        var textBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 255, 235, 195));
        DrawCelebrationBanner(dc, w, h, msg, bg, border, textBrush, cheerProgress, cheerFade);
    }

    private void RenderCoffeeJazzCelebration(DrawingContext dc, double w, double h, double cheerProgress, double cheerFade)
    {
        // 1. Concentric Melodic Soundwave Ripples from warm latte cup
        double waveX = w * 0.35;
        double waveY = h * 0.65;
        for (int r = 1; r <= 3; r++)
        {
            double waveR = (r * (IsMiniMode ? 14 : 26)) + (cheerProgress * (IsMiniMode ? 30 : 60));
            double waveAlpha = Math.Clamp(1.0 - (cheerProgress * 0.9), 0.0, 1.0) * cheerFade;
            var wavePen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(160 * waveAlpha), 255, 200, 100)), 1.5);
            dc.DrawEllipse(null, wavePen, new Point(waveX, waveY), waveR, waveR * 0.6);
        }

        // 2. Floating Neon Jazz Musical Notes (🎷 🎵 🎶 ♩ ♪ ♫)
        var jazzNotes = new[] { "🎷", "🎵", "🎶", "♩", "♪", "♫" };
        var noteColors = new[] { Color.FromRgb(255, 195, 80), Color.FromRgb(215, 130, 255), Color.FromRgb(100, 235, 220) };
        for (int j = 0; j < (IsMiniMode ? 4 : 7); j++)
        {
            double jx = (w * 0.15) + (j * (w * 0.70 / (IsMiniMode ? 4 : 7))) + (Math.Sin((_frameTick * 0.14) + j) * 8);
            double jy = (h * 0.70) - (cheerProgress * (h * 0.60)) + (Math.Cos((_frameTick * 0.16) + j) * 6);
            var col = noteColors[j % noteColors.Length];
            var ft = CreateText(jazzNotes[j % jazzNotes.Length], IsMiniMode ? 10 : 14, new SolidColorBrush(Color.FromArgb((byte)(240 * cheerFade), col.R, col.G, col.B)), FontWeights.Bold);
            dc.DrawText(ft, new Point(jx, jy));
        }

        // 3. Floating Banner
        string msg = IsMiniMode ? "🎷 JAZZY VIBES! ☕" : "✨ 🎷 SMOOTH JAZZ FOCUS VIBES! ☕ ✨";
        var bg = new LinearGradientBrush(Color.FromArgb((byte)(240 * cheerFade), 45, 25, 60), Color.FromArgb((byte)(240 * cheerFade), 25, 15, 35), new Point(0, 0), new Point(1, 1));
        var border = new Pen(new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), 255, 190, 80)), 1.4);
        var textBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 255, 235, 200));
        DrawCelebrationBanner(dc, w, h, msg, bg, border, textBrush, cheerProgress, cheerFade);
    }

    private void RenderRocketCelebration(DrawingContext dc, double w, double h, double cheerProgress, double cheerFade)
    {
        // 1. Rocket Booster Mega Flare Blast
        double flareX = w * 0.50;
        double flareY = h * 0.65;
        var flareBrush = new RadialGradientBrush(Color.FromArgb((byte)(220 * cheerFade), 255, 240, 120), Color.FromArgb(0, 255, 80, 20));
        dc.DrawEllipse(flareBrush, null, new Point(flareX, flareY), (IsMiniMode ? 35 : 70) * (cheerProgress + 0.3), IsMiniMode ? 20 : 40);

        // 2. Streaking Shooting Stars with comet tails
        for (int st = 0; st < (IsMiniMode ? 3 : 6); st++)
        {
            double startX = (w * 0.1) + (st * (w * 0.85 / (IsMiniMode ? 3 : 6)));
            double startY = (h * 0.15) + (st * 10);
            double dist = cheerProgress * (IsMiniMode ? 50 : 100);
            double sx = startX + dist;
            double sy = startY + (dist * 0.5);

            var tailPen = new Pen(new LinearGradientBrush(Color.FromArgb(0, 0, 240, 255), Color.FromArgb((byte)(220 * cheerFade), 255, 255, 255), new Point(0, 0), new Point(1, 1)), 1.8);
            dc.DrawLine(tailPen, new Point(sx - (20 * cheerFade), sy - (10 * cheerFade)), new Point(sx, sy));
            DrawStar5(dc, sx, sy, 4.5 * cheerFade, 2.0 * cheerFade, new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 255, 255, 255)));
        }

        // 3. Floating Banner
        string msg = IsMiniMode ? "🚀 WARP SPEED! 🌟" : "✨ 🚀 WARP SPEED ENGAGED! TO THE STARS! 🌌 ✨";
        var bg = new LinearGradientBrush(Color.FromArgb((byte)(240 * cheerFade), 15, 20, 50), Color.FromArgb((byte)(240 * cheerFade), 10, 10, 30), new Point(0, 0), new Point(1, 1));
        var border = new Pen(new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), 0, 230, 255)), 1.5);
        var textBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 255, 255, 255));
        DrawCelebrationBanner(dc, w, h, msg, bg, border, textBrush, cheerProgress, cheerFade);
    }

    private void RenderCatCelebration(DrawingContext dc, double w, double h, double cheerProgress, double cheerFade)
    {
        // 1. Cute Pastel Cat Paw Prints walking across
        var pawBrush = new SolidColorBrush(Color.FromArgb((byte)(210 * cheerFade), 255, 160, 190));
        for (int p = 0; p < (IsMiniMode ? 4 : 7); p++)
        {
            double px = (w * 0.12) + (p * (w * 0.76 / (IsMiniMode ? 4 : 7)));
            double py = (h * 0.42) + (Math.Sin((p * 1.2) + (_frameTick * 0.1)) * (h * 0.12));
            DrawPawPrint(dc, px, py, (IsMiniMode ? 0.75 : 1.1) * cheerFade, pawBrush);
        }

        // 2. Expanding Purring Heart Rings
        for (int hr = 1; hr <= (IsMiniMode ? 2 : 4); hr++)
        {
            double hx = (w * 0.50) + (Math.Sin((_frameTick * 0.1) + hr) * 20);
            double hy = (h * 0.65) - (cheerProgress * (h * 0.55)) - (hr * (IsMiniMode ? 7 : 12));
            DrawHeart(dc, hx, hy, (IsMiniMode ? 4.5 : 7.0) * cheerFade, new SolidColorBrush(Color.FromArgb((byte)(210 * cheerFade), 255, 120, 160)));
        }

        // 3. Floating Banner
        string msg = IsMiniMode ? "🐾 PURR-FECT FOCUS! 💕" : "✨ 🐾 PURR-FECT FOCUS! KITTY CHEERS! 💕 ✨";
        var bg = new LinearGradientBrush(Color.FromArgb((byte)(235 * cheerFade), 255, 230, 240), Color.FromArgb((byte)(235 * cheerFade), 255, 205, 220), new Point(0, 0), new Point(1, 1));
        var border = new Pen(new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), 255, 130, 170)), 1.4);
        var textBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 120, 40, 70));
        DrawCelebrationBanner(dc, w, h, msg, bg, border, textBrush, cheerProgress, cheerFade);
    }

    private void RenderRunnerCelebration(DrawingContext dc, double w, double h, double cheerProgress, double cheerFade)
    {
        // 1. Dynamic Electric Speed Lines & Lightning Sparks
        var speedPen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(200 * cheerFade), 255, 220, 60)), 1.8);
        for (int l = 0; l < (IsMiniMode ? 4 : 8); l++)
        {
            double lx = ((_frameTick * 6.0 + (l * 40)) % (w + 60)) - 30;
            double ly = (h * 0.30) + (l * (h * 0.08));
            dc.DrawLine(speedPen, new Point(lx, ly), new Point(lx + (IsMiniMode ? 20 : 40), ly));
        }

        // 2. Gold Starbursts & Confetti Explosion
        for (int g = 0; g < (IsMiniMode ? 4 : 8); g++)
        {
            double gx = (w * 0.15) + (g * (w * 0.70 / (IsMiniMode ? 4 : 8)));
            double gy = (h * 0.65) - (cheerProgress * (h * 0.55)) + (Math.Sin((_frameTick * 0.2) + g) * 8);
            DrawStar5(dc, gx, gy, 5.5 * cheerFade, 2.5 * cheerFade, new SolidColorBrush(Color.FromArgb((byte)(240 * cheerFade), 255, 215, 50)));
        }

        // 3. Floating Banner
        string msg = IsMiniMode ? "⚡ POWER SPRINT! 🏆" : "✨ ⚡ POWER SPRINT! UNSTOPPABLE! 🏆 ✨";
        var bg = new LinearGradientBrush(Color.FromArgb((byte)(240 * cheerFade), 255, 140, 40), Color.FromArgb((byte)(240 * cheerFade), 220, 50, 40), new Point(0, 0), new Point(1, 1));
        var border = new Pen(new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), 255, 230, 80)), 1.5);
        var textBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 255, 255, 255));
        DrawCelebrationBanner(dc, w, h, msg, bg, border, textBrush, cheerProgress, cheerFade);
    }

    private void RenderTamagotchiCelebration(DrawingContext dc, double w, double h, double cheerProgress, double cheerFade)
    {
        byte fadeByte = (byte)(255 * cheerFade);
        double haloPulse = 0.88 + (Math.Sin((_frameTick * 0.24) + (cheerProgress * Math.PI * 2.0)) * 0.12);
        var haloBrush = new RadialGradientBrush(
            Color.FromArgb((byte)(145 * cheerFade), 255, 232, 150),
            Color.FromArgb(0, 255, 232, 150));
        dc.DrawEllipse(haloBrush, null, new Point(w * 0.5, h * 0.58), w * (IsMiniMode ? 0.26 : 0.38) * haloPulse, h * (IsMiniMode ? 0.15 : 0.24) * haloPulse);

        for (int s = 0; s < (IsMiniMode ? 4 : 7); s++)
        {
            double sx = (s % 2 == 0) ? (w * 0.16) : (w * 0.84);
            double sy = h * (0.22 + (s * 0.07));
            double ex = w * 0.5 + ((s - 3) * (IsMiniMode ? 10 : 16));
            double ey = h * (0.18 + (Math.Sin((_frameTick * 0.08) + s) * 0.015));
            var streamerPen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(120 * cheerFade), (byte)(255 - (s * 14)), (byte)(150 + (s * 10)), (byte)(210 + (s * 5)))), IsMiniMode ? 1.4 : 2.2)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            var streamGeom = new PathGeometry();
            var streamFig = new PathFigure { StartPoint = new Point(sx, sy) };
            streamFig.Segments.Add(new QuadraticBezierSegment(new Point((sx + ex) * 0.5, sy - (IsMiniMode ? 6 : 10)), new Point(ex, ey), true));
            streamGeom.Figures.Add(streamFig);
            dc.DrawGeometry(null, streamerPen, streamGeom);
        }

        double pxSize = IsMiniMode ? 2.0 : 3.0;
        for (int ph = 0; ph < (IsMiniMode ? 4 : 6); ph++)
        {
            double px = (w * 0.15) + (ph * (w * 0.70 / ((IsMiniMode ? 4 : 6) - 1)));
            double py = (h * 0.78) - (Math.Floor(cheerProgress * 14.0) * (h * 0.035)) - (ph * (IsMiniMode ? 5 : 8));
            DrawPixelHeart(dc, px, py, pxSize, new SolidColorBrush(Color.FromArgb((byte)(235 * cheerFade), 255, 90, 150)));
        }

        Color[] confettiColors =
        {
            Color.FromRgb(255, 225, 90),
            Color.FromRgb(255, 140, 200),
            Color.FromRgb(120, 240, 255),
            Color.FromRgb(170, 255, 170)
        };

        for (int i = 0; i < (IsMiniMode ? 10 : 20); i++)
        {
            double x = (w * 0.08) + (((i * 23.0) + (_frameTick * 2.4)) % (w * 0.84));
            double drop = ((_frameTick * 1.75) + (i * 15.0) + (cheerProgress * 90.0)) % (h * 0.85);
            double y = (h * 0.12) + drop;
            Color color = confettiColors[i % confettiColors.Length];

            dc.PushTransform(new RotateTransform((i * 29) + (_frameTick * 5.0), x, y));
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), color.R, color.G, color.B)), null, new Rect(x - (IsMiniMode ? 1.1 : 1.5), y - (IsMiniMode ? 3.0 : 4.5), IsMiniMode ? 2.2 : 3.0, IsMiniMode ? 6.0 : 9.0));
            dc.Pop();

            if (!IsMiniMode && (i % 5 == 0))
            {
                DrawSparkle(dc, x, y - 6, 2.4 * cheerFade, new SolidColorBrush(Color.FromArgb((byte)(170 * cheerFade), 255, 248, 180)));
            }
        }

        for (int i = 0; i < (IsMiniMode ? 4 : 8); i++)
        {
            double starX = w * (0.12 + (0.10 * i)) + (Math.Sin((_frameTick * 0.08) + i) * (IsMiniMode ? 2.0 : 4.0));
            double starY = h * 0.28 + (Math.Cos((_frameTick * 0.09) + i) * (IsMiniMode ? 4.0 : 7.0));
            DrawSparkle(dc, starX, starY, (IsMiniMode ? 2.0 : 3.2) * cheerFade, new SolidColorBrush(Color.FromArgb((byte)(210 * cheerFade), 255, 235, 120)));
        }

        if (!IsMiniMode)
        {
            DrawUltraKawaiiPet(dc, new Point(w * 0.5, h * 0.73), 0.55, TamagotchiPetState.Goal, true);
        }

        double badgeBounce = Math.Sin(cheerProgress * Math.PI) * (IsMiniMode ? 4 : 7);
        Rect badgeRect = new Rect((w * 0.5) - (IsMiniMode ? 24 : 34), (h * 0.47) - badgeBounce, IsMiniMode ? 48 : 68, IsMiniMode ? 18 : 24);
        var badgeBrush = new LinearGradientBrush(
            Color.FromArgb((byte)(235 * cheerFade), 70, 44, 116),
            Color.FromArgb((byte)(235 * cheerFade), 36, 24, 76),
            new Point(0, 0),
            new Point(0, 1));
        var badgePen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), 255, 220, 110)), IsMiniMode ? 1.2 : 1.6);
        dc.DrawRoundedRectangle(badgeBrush, badgePen, badgeRect, 6, 6);
        DrawStar5(dc, badgeRect.Left + (badgeRect.Width * 0.22), badgeRect.Top + (badgeRect.Height * 0.5), IsMiniMode ? 4.0 : 5.6, IsMiniMode ? 1.8 : 2.6, new SolidColorBrush(Color.FromArgb((byte)(230 * cheerFade), 255, 220, 80)));
        DrawStar5(dc, badgeRect.Right - (badgeRect.Width * 0.22), badgeRect.Top + (badgeRect.Height * 0.5), IsMiniMode ? 4.0 : 5.6, IsMiniMode ? 1.8 : 2.6, new SolidColorBrush(Color.FromArgb((byte)(230 * cheerFade), 255, 220, 80)));
        var badgeText = CreateText(IsMiniMode ? "+10 XP" : "+10 HAPPINESS", IsMiniMode ? 9.5 : 11.5, new SolidColorBrush(Color.FromArgb(fadeByte, 255, 245, 190)), FontWeights.Bold);
        dc.DrawText(badgeText, new Point(badgeRect.Left + ((badgeRect.Width - badgeText.Width) * 0.5), badgeRect.Top + ((badgeRect.Height - badgeText.Height) * 0.5)));

        string msg = IsMiniMode ? "★ LEVEL UP! ★" : "★ LEVEL UP! PET HAPPINESS +10 ★";
        var bg = new LinearGradientBrush(Color.FromArgb((byte)(245 * cheerFade), 36, 34, 66), Color.FromArgb((byte)(245 * cheerFade), 16, 18, 34), new Point(0, 0), new Point(1, 1));
        var border = new Pen(new SolidColorBrush(Color.FromArgb((byte)(230 * cheerFade), 255, 220, 80)), 2.0);
        var textBrush = new SolidColorBrush(Color.FromArgb(fadeByte, 255, 240, 150));
        DrawCelebrationBanner(dc, w, h, msg, bg, border, textBrush, cheerProgress, cheerFade);
    }

    private void RenderDefaultCelebration(DrawingContext dc, double w, double h, double cheerProgress, double cheerFade)
    {
        var rand = new Random(42 + (_frameTick / 2));
        for (int i = 0; i < (IsMiniMode ? 8 : 16); i++)
        {
            double sx = rand.NextDouble() * w;
            double sy = (h * 0.75) - (cheerProgress * (h * 0.65)) + (rand.NextDouble() * 15);
            double size = (3 + rand.Next(4)) * cheerFade;
            var color = Color.FromRgb((byte)rand.Next(220, 255), (byte)rand.Next(180, 255), (byte)rand.Next(100, 255));
            DrawSparkle(dc, sx, sy, size, new SolidColorBrush(Color.FromArgb((byte)(240 * cheerFade), color.R, color.G, color.B)));
        }

        string msg = IsMiniMode ? "✨ GREAT FOCUS! ✨" : "✨ FANTASTIC PROGRESS! KEEP GOING! ✨";
        var bg = new LinearGradientBrush(Color.FromArgb((byte)(235 * cheerFade), 40, 50, 70), Color.FromArgb((byte)(235 * cheerFade), 20, 25, 40), new Point(0, 0), new Point(1, 1));
        var border = new Pen(new SolidColorBrush(Color.FromArgb((byte)(220 * cheerFade), 255, 215, 80)), 1.4);
        var textBrush = new SolidColorBrush(Color.FromArgb((byte)(255 * cheerFade), 255, 255, 255));
        DrawCelebrationBanner(dc, w, h, msg, bg, border, textBrush, cheerProgress, cheerFade);
    }

    private void DrawCelebrationBanner(DrawingContext dc, double w, double h, string text, Brush bgBrush, Pen borderPen, Brush textBrush, double cheerProgress, double cheerFade)
    {
        double bounce = Math.Sin(cheerProgress * Math.PI) * (IsMiniMode ? 4 : 8);
        double bannerW = IsMiniMode ? Math.Min(w - 16, 210) : Math.Min(w - 40, 320);
        double bannerH = IsMiniMode ? 20 : 26;
        double bx = (w - bannerW) * 0.5;
        double by = (IsMiniMode ? 4 : 8) + (8 - bounce);

        dc.DrawRoundedRectangle(bgBrush, borderPen, new Rect(bx, by, bannerW, bannerH), 5, 5);

        var ft = CreateText(text, IsMiniMode ? 10 : 12.5, textBrush, FontWeights.Bold);
        dc.DrawText(ft, new Point(bx + ((bannerW - ft.Width) * 0.5), by + ((bannerH - ft.Height) * 0.5)));
    }

    private static void DrawSparkle(DrawingContext dc, double x, double y, double size, Brush brush)
    {
        var geom = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(x, y - size) };
        fig.Segments.Add(new QuadraticBezierSegment(new Point(x + (size * 0.2), y - (size * 0.2)), new Point(x + size, y), true));
        fig.Segments.Add(new QuadraticBezierSegment(new Point(x + (size * 0.2), y + (size * 0.2)), new Point(x, y + size), true));
        fig.Segments.Add(new QuadraticBezierSegment(new Point(x - (size * 0.2), y + (size * 0.2)), new Point(x - size, y), true));
        fig.Segments.Add(new QuadraticBezierSegment(new Point(x - (size * 0.2), y - (size * 0.2)), new Point(x, y - size), true));
        geom.Figures.Add(fig);
        dc.DrawGeometry(brush, null, geom);
    }

    private static void DrawHeart(DrawingContext dc, double x, double y, double size, Brush brush)
    {
        var geom = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(x, y + (size * 0.35)) };
        fig.Segments.Add(new BezierSegment(new Point(x - (size * 0.7), y - (size * 0.4)), new Point(x - (size * 0.4), y - size), new Point(x, y - (size * 0.4)), true));
        fig.Segments.Add(new BezierSegment(new Point(x + (size * 0.4), y - size), new Point(x + (size * 0.7), y - (size * 0.4)), new Point(x, y + (size * 0.35)), true));
        geom.Figures.Add(fig);
        dc.DrawGeometry(brush, null, geom);
    }

    private static void DrawStar5(DrawingContext dc, double cx, double cy, double rOuter, double rInner, Brush brush, Pen? pen = null)
    {
        var geom = new PathGeometry();
        var fig = new PathFigure();
        for (int i = 0; i < 10; i++)
        {
            double angle = (i * Math.PI / 5.0) - (Math.PI * 0.5);
            double r = (i % 2 == 0) ? rOuter : rInner;
            var pt = new Point(cx + (Math.Cos(angle) * r), cy + (Math.Sin(angle) * r));
            if (i == 0)
                fig.StartPoint = pt;
            else
                fig.Segments.Add(new LineSegment(pt, true));
        }
        fig.IsClosed = true;
        geom.Figures.Add(fig);
        dc.DrawGeometry(brush, pen, geom);
    }

    private static void DrawPawPrint(DrawingContext dc, double x, double y, double scale, Brush brush)
    {
        dc.DrawEllipse(brush, null, new Point(x, y + (2 * scale)), 4.5 * scale, 3.5 * scale);
        dc.DrawEllipse(brush, null, new Point(x - (4 * scale), y - (1.5 * scale)), 1.6 * scale, 1.8 * scale);
        dc.DrawEllipse(brush, null, new Point(x - (1.5 * scale), y - (4 * scale)), 1.7 * scale, 2.0 * scale);
        dc.DrawEllipse(brush, null, new Point(x + (1.5 * scale), y - (4 * scale)), 1.7 * scale, 2.0 * scale);
        dc.DrawEllipse(brush, null, new Point(x + (4 * scale), y - (1.5 * scale)), 1.6 * scale, 1.8 * scale);
    }

    private static void DrawLeaf(DrawingContext dc, double x, double y, double size, double angle, Brush brush)
    {
        dc.PushTransform(new RotateTransform(angle * 180.0 / Math.PI, x, y));
        var geom = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(x, y - size) };
        fig.Segments.Add(new QuadraticBezierSegment(new Point(x + (size * 0.7), y), new Point(x, y + size), true));
        fig.Segments.Add(new QuadraticBezierSegment(new Point(x - (size * 0.7), y), new Point(x, y - size), true));
        geom.Figures.Add(fig);
        dc.DrawGeometry(brush, null, geom);
        dc.Pop();
    }

    private static void DrawPixelHeart(DrawingContext dc, double x, double y, double pxSize, Brush brush)
    {
        int[,] heartMatrix =
        {
            { 0, 1, 1, 0, 1, 1, 0 },
            { 1, 1, 1, 1, 1, 1, 1 },
            { 1, 1, 1, 1, 1, 1, 1 },
            { 0, 1, 1, 1, 1, 1, 0 },
            { 0, 0, 1, 1, 1, 0, 0 },
            { 0, 0, 0, 1, 0, 0, 0 }
        };

        double startX = x - (3.5 * pxSize);
        double startY = y - (3.0 * pxSize);

        for (int r = 0; r < 6; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                if (heartMatrix[r, c] == 1)
                {
                    dc.DrawRectangle(brush, null, new Rect(startX + (c * pxSize), startY + (r * pxSize), pxSize, pxSize));
                }
            }
        }
    }

    private static Color LerpColor(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return Color.FromRgb(
            (byte)(a.R + ((b.R - a.R) * t)),
            (byte)(a.G + ((b.G - a.G) * t)),
            (byte)(a.B + ((b.B - a.B) * t)));
    }

    #endregion

    #region 🚴 Scene 1: Cute Boy & Puppy Cycling Home

    private void RenderCyclingScene(DrawingContext dc, double w, double h)
    {
        double trailY = h * 0.70;
        double trailHeight = h - trailY;

        // 1. Sky Gradient that gradually warms from a fresh morning blue into a golden dusk glow
        // as the boy gets closer to home - reinforcing the "cycling home" journey feel.
        double duskT = Math.Clamp(ProgressFraction, 0.0, 1.0);
        var skyBrush = new LinearGradientBrush(
            LerpColor(Color.FromRgb(92, 172, 248), Color.FromRgb(255, 150, 120), duskT * 0.75),
            LerpColor(Color.FromRgb(248, 238, 222), Color.FromRgb(255, 205, 160), duskT * 0.85),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(skyBrush, null, new Rect(0, 0, w, trailY));

        // 2. Smiling Sun with Soft Rotating Sunbeams & Rosy Cheeks
        double sunX = w * 0.88;
        double sunY = h * 0.20;
        double sunR = IsMiniMode ? 14 : 22;
        var sunGlow = new RadialGradientBrush(Color.FromArgb(210, 255, 238, 140), Color.FromArgb(0, 255, 210, 80));
        dc.DrawEllipse(sunGlow, null, new Point(sunX, sunY), sunR * 2.4, sunR * 2.4);

        // Sunbeams
        if (!IsMiniMode)
        {
            double rayRot = (_frameTick * 0.02);
            for (int r = 0; r < 8; r++)
            {
                double angle = rayRot + (r * Math.PI / 4.0);
                double r1 = sunR * 1.3;
                double r2 = sunR * 1.8;
                var beamPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 235, 120)), 2);
                dc.DrawLine(beamPen, new Point(sunX + (Math.Cos(angle) * r1), sunY + (Math.Sin(angle) * r1)), new Point(sunX + (Math.Cos(angle) * r2), sunY + (Math.Sin(angle) * r2)));
            }
        }

        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 224, 75)), null, new Point(sunX, sunY), sunR, sunR);
        // Sun face
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(60, 40, 20)), null, new Point(sunX - (sunR * 0.35), sunY - (sunR * 0.1)), 1.5, 2);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(60, 40, 20)), null, new Point(sunX + (sunR * 0.35), sunY - (sunR * 0.1)), 1.5, 2);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(170, 255, 120, 130)), null, new Point(sunX - (sunR * 0.5), sunY + (sunR * 0.22)), 2.8, 1.6);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(170, 255, 120, 130)), null, new Point(sunX + (sunR * 0.5), sunY + (sunR * 0.22)), 2.8, 1.6);

        // 3. Fluffy Drifting Clouds
        double cloudOffset1 = ((_frameTick * 0.35) % (w + 140)) - 70;
        double cloudOffset2 = (((_frameTick * 0.22) + (w * 0.5)) % (w + 140)) - 70;
        DrawCuteCloud(dc, cloudOffset1, h * 0.15, IsMiniMode ? 16 : 26);
        DrawCuteCloud(dc, cloudOffset2, h * 0.26, IsMiniMode ? 14 : 22);

        // 4. Distant Lavender Ridge & Soft Rolling Green Hills (Layered Depth)
        var ridge = new PathGeometry();
        var rf = new PathFigure { StartPoint = new Point(0, trailY) };
        rf.Segments.Add(new QuadraticBezierSegment(new Point(w * 0.3, trailY - (h * 0.45)), new Point(w * 0.65, trailY), true));
        rf.Segments.Add(new QuadraticBezierSegment(new Point(w * 0.85, trailY - (h * 0.32)), new Point(w, trailY), true));
        rf.Segments.Add(new LineSegment(new Point(w, trailY), true));
        rf.Segments.Add(new LineSegment(new Point(0, trailY), true));
        ridge.Figures.Add(rf);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(165, 195, 235)), null, ridge);

        // Mid Green Hill with cute distant windmill
        var midHill = new PathGeometry();
        var mhf = new PathFigure { StartPoint = new Point(0, trailY) };
        mhf.Segments.Add(new QuadraticBezierSegment(new Point(w * 0.25, trailY - (h * 0.28)), new Point(w * 0.55, trailY), true));
        mhf.Segments.Add(new QuadraticBezierSegment(new Point(w * 0.8, trailY - (h * 0.22)), new Point(w, trailY), true));
        mhf.Segments.Add(new LineSegment(new Point(w, trailY), true));
        mhf.Segments.Add(new LineSegment(new Point(0, trailY), true));
        midHill.Figures.Add(mhf);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(120, 205, 130)), null, midHill);

        // Distant cute windmill on the hill
        if (!IsMiniMode)
        {
            DrawCuteDistantWindmill(dc, w * 0.25, trailY - (h * 0.26));
        }

        // Near Meadow Hill
        var nearHill = new PathGeometry();
        var nhf = new PathFigure { StartPoint = new Point(0, trailY) };
        nhf.Segments.Add(new QuadraticBezierSegment(new Point(w * 0.42, trailY - (h * 0.15)), new Point(w * 0.88, trailY), true));
        nhf.Segments.Add(new LineSegment(new Point(w, trailY), true));
        nhf.Segments.Add(new LineSegment(new Point(0, trailY), true));
        nearHill.Figures.Add(nhf);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(145, 222, 145)), null, nearHill);

        // Wildflowers on the grass
        DrawWildflowers(dc, w, trailY);

        // Fluttering butterflies drifting over the meadow while the boy is pedaling
        if (!IsMiniMode)
        {
            DrawFlutteringButterflies(dc, w, trailY);
        }

        // 5. Picturesque Countryside Trail (Warm earth/sand with soft grass borders - NO harsh asphalt line!)
        var trailBrush = new LinearGradientBrush(
            Color.FromRgb(242, 226, 198),
            Color.FromRgb(220, 200, 168),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(trailBrush, null, new Rect(0, trailY, w, trailHeight));

        // Lush grass fringes along top & bottom of trail
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(130, 215, 120)), null, new Rect(0, trailY, w, 5));
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(115, 195, 105)), null, new Rect(0, h - 4, w, 4));

        // Cute rustic cobblestones/pebbles scattered along path
        DrawPathCobblestones(dc, w, trailY, trailHeight);

        // Cute wooden fence posts along path edge
        if (!IsMiniMode)
        {
            DrawRusticFencePosts(dc, w, trailY);
        }

        // 6. Charming Fairy Cottage on Right
        double houseW = IsMiniMode ? 52 : 88;
        double houseH = IsMiniMode ? 44 : 74;
        double houseX = w - houseW - (IsMiniMode ? 8 : 16);
        double houseY = trailY - houseH + 4;
        DrawCuteCottage(dc, houseX, houseY, houseW, houseH);

        // Cherry Blossom Tree with Falling Petals
        if (!IsMiniMode)
        {
            DrawCherryBlossomTree(dc, houseX - 30, trailY, 52);
        }

        // 7. Boy & Puppy Cycling / Goal / Rest
        double startX = IsMiniMode ? 14 : 26;
        double endX = houseX - (IsMiniMode ? 22 : 42);
        double currentX = startX + (ProgressFraction * (endX - startX));

        if (IsRestPhase)
        {
            DrawRestPicnic(dc, w * 0.45, trailY, IsMiniMode);
        }
        else if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawCelebrationBoyAndPuppy(dc, endX + (IsMiniMode ? 8 : 16), trailY, IsMiniMode);
        }
        else
        {
            double bounce = IsTracking ? Math.Sin(_frameTick * 0.85) * (IsMiniMode ? 1.2 : 2.2) : 0;
            if (IsTracking && !IsMiniMode)
            {
                DrawDustPuffs(dc, currentX, trailY + bounce);
            }
            DrawCuteCyclingBoyAndPuppy(dc, currentX, trailY + bounce, IsMiniMode, IsTracking);
        }
    }

    private void DrawCuteDistantWindmill(DrawingContext dc, double x, double y)
    {
        // Tower
        var towerBrush = new SolidColorBrush(Color.FromRgb(245, 240, 230));
        var towerPen = new Pen(new SolidColorBrush(Color.FromRgb(170, 160, 150)), 0.8);
        var tower = new PathGeometry();
        var tf = new PathFigure { StartPoint = new Point(x - 5, y + 16) };
        tf.Segments.Add(new LineSegment(new Point(x - 3, y), true));
        tf.Segments.Add(new LineSegment(new Point(x + 3, y), true));
        tf.Segments.Add(new LineSegment(new Point(x + 5, y + 16), true));
        tower.Figures.Add(tf);
        dc.DrawGeometry(towerBrush, towerPen, tower);

        // Cap & Hub
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(215, 85, 75)), null, new Point(x, y), 3.5, 3.5);

        // Rotating blades
        double rot = _frameTick * 0.05;
        var bladeBrush = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
        var bladePen = new Pen(new SolidColorBrush(Color.FromRgb(140, 110, 80)), 0.8);

        for (int i = 0; i < 4; i++)
        {
            double a = rot + (i * Math.PI / 2.0);
            Point tip = new Point(x + (Math.Cos(a) * 14), y + (Math.Sin(a) * 14));
            dc.DrawLine(bladePen, new Point(x, y), tip);
            dc.DrawEllipse(bladeBrush, null, new Point(x + (Math.Cos(a) * 8), y + (Math.Sin(a) * 8)), 2.5, 1.2);
        }
    }

    private void DrawPathCobblestones(DrawingContext dc, double w, double trailY, double trailHeight)
    {
        var rand = new Random(555);
        int stoneCount = IsMiniMode ? 8 : 18;
        var stoneBrush = new SolidColorBrush(Color.FromArgb(90, 195, 175, 145));

        for (int i = 0; i < stoneCount; i++)
        {
            double sx = rand.NextDouble() * w;
            double sy = trailY + 8 + (rand.NextDouble() * (trailHeight - 16));
            double sr = 2.0 + (rand.NextDouble() * 3.0);
            dc.DrawEllipse(stoneBrush, null, new Point(sx, sy), sr, sr * 0.6);
        }
    }

    private void DrawRusticFencePosts(DrawingContext dc, double w, double trailY)
    {
        var postBrush = new SolidColorBrush(Color.FromRgb(160, 115, 75));
        var railPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 140, 95, 60)), 1.2);

        double spacing = 55;
        double prevX = -20;
        double prevTopY = trailY - 10;

        for (double fx = 15; fx < w - 85; fx += spacing)
        {
            double topY = trailY - 12;
            // Post
            dc.DrawRoundedRectangle(postBrush, null, new Rect(fx - 2, topY, 4, 14), 1, 1);
            // Rail connecting to prev post
            if (prevX > 0)
            {
                dc.DrawLine(railPen, new Point(prevX, prevTopY + 3), new Point(fx, topY + 3));
                dc.DrawLine(railPen, new Point(prevX, prevTopY + 8), new Point(fx, topY + 8));
            }
            prevX = fx;
            prevTopY = topY;
        }
    }

    private void DrawCuteCloud(DrawingContext dc, double x, double y, double r)
    {
        var cloudBrush = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255));
        dc.DrawEllipse(cloudBrush, null, new Point(x, y), r * 1.3, r * 0.8);
        dc.DrawEllipse(cloudBrush, null, new Point(x + (r * 0.6), y - (r * 0.3)), r * 0.9, r * 0.7);
        dc.DrawEllipse(cloudBrush, null, new Point(x - (r * 0.5), y), r * 0.8, r * 0.6);
    }

    private void DrawWildflowers(DrawingContext dc, double w, double groundY)
    {
        var rand = new Random(1337);
        int flowerCount = IsMiniMode ? 12 : 25;
        for (int i = 0; i < flowerCount; i++)
        {
            double fx = rand.NextDouble() * (w * 0.8);
            double fy = groundY - 6 - (rand.NextDouble() * (IsMiniMode ? 12 : 25));
            Color c = (i % 3 == 0) ? Color.FromRgb(255, 140, 180) : (i % 3 == 1 ? Color.FromRgb(255, 225, 90) : Color.FromRgb(255, 255, 255));
            dc.DrawEllipse(new SolidColorBrush(c), null, new Point(fx, fy), 2, 2);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 200, 60)), null, new Point(fx, fy), 0.8, 0.8);
        }
    }

    private void DrawFlutteringButterflies(DrawingContext dc, double w, double groundY)
    {
        var butterflyColors = new[]
        {
            Color.FromRgb(255, 205, 90),
            Color.FromRgb(255, 140, 190),
            Color.FromRgb(150, 210, 255)
        };

        for (int i = 0; i < 3; i++)
        {
            double travel = ((_frameTick * (0.5 + (i * 0.15))) + (i * 140)) % (w + 60);
            double bx = travel - 30;
            double by = (groundY * (0.32 + (i * 0.14))) + (Math.Sin((_frameTick * 0.12) + (i * 2)) * 10);
            double flap = Math.Sin(_frameTick * 0.5 + i) * 2.5 + 3;

            var wingBrush = new SolidColorBrush(butterflyColors[i % butterflyColors.Length]);
            dc.DrawEllipse(wingBrush, null, new Point(bx - 2, by), flap, 3.2);
            dc.DrawEllipse(wingBrush, null, new Point(bx + 2, by), flap, 3.2);
        }
    }

    private void DrawDustPuffs(DrawingContext dc, double bikeX, double groundY)
    {
        for (int i = 0; i < 3; i++)
        {
            double age = (_frameTick * 2.4 + (i * 9)) % 30;
            double px = bikeX - (14 + age);
            double py = groundY + 3 - (age * 0.12);
            double pr = 1.5 + (age * 0.12);
            byte alpha = (byte)Math.Clamp(90 - (age * 2.6), 0, 90);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(alpha, 225, 205, 175)), null, new Point(px, py), pr, pr * 0.6);
        }
    }

    private void DrawCherryBlossomTree(DrawingContext dc, double x, double groundY, double height)
    {
        // Trunk
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(120, 80, 50)), null, new Rect(x - 3.5, groundY - height, 7, height));
        // Soft Pink Foliage
        var blossomBrush = new SolidColorBrush(Color.FromRgb(255, 175, 205));
        dc.DrawEllipse(blossomBrush, null, new Point(x, groundY - height - 12), height * 0.55, height * 0.45);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 195, 220)), null, new Point(x - 6, groundY - height - 16), height * 0.38, height * 0.32);

        // Falling Petals
        for (int i = 0; i < 4; i++)
        {
            double petalX = x + ((_frameTick * (1.2 + i * 0.3) + (i * 18)) % 45) - 10;
            double petalY = (groundY - height) + ((_frameTick * 0.8 + (i * 20)) % height);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 160, 195)), null, new Point(petalX, petalY), 2, 1.2);
        }
    }

    private void DrawCuteCottage(DrawingContext dc, double x, double y, double w, double h)
    {
        double wallH = h * 0.58;
        double wallY = y + (h - wallH);

        // Cobblestone Chimney with Heart-Shaped Smoke
        double chimneyX = x + (w * 0.16);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(185, 85, 75)), null, new Rect(chimneyX, y + (h * 0.06), w * 0.16, h * 0.40), 2, 2);

        if (IsTracking || IsGoalReached)
        {
            for (int s = 0; s < 3; s++)
            {
                double smokeOffset = (_frameTick * 1.3 + (s * 14)) % 36;
                double smokeY = y - smokeOffset;
                double smokeSize = 3.5 + (smokeOffset * 0.22);
                double smokeX = chimneyX + 6 + (Math.Sin((_frameTick * 0.1) + s) * 4);
                DrawHeart(dc, smokeX, smokeY, smokeSize, new SolidColorBrush(Color.FromArgb((byte)(150 - smokeOffset * 3), 255, 195, 215)));
            }
        }

        // Cottage Wall (Warm Cream / Stucco)
        var wallBrush = new LinearGradientBrush(Color.FromRgb(255, 246, 230), Color.FromRgb(238, 222, 198), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(wallBrush, new Pen(new SolidColorBrush(Color.FromRgb(150, 125, 95)), 1.2), new Rect(x, wallY, w, wallH), 4, 4);

        // Storybook Curved Red Roof
        var roofPath = new PathGeometry();
        var roofFig = new PathFigure { StartPoint = new Point(x - (w * 0.12), wallY) };
        roofFig.Segments.Add(new QuadraticBezierSegment(new Point(x + (w * 0.5), y - 3), new Point(x + w + (w * 0.12), wallY), true));
        roofPath.Figures.Add(roofFig);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(228, 88, 76)), new Pen(new SolidColorBrush(Color.FromRgb(165, 45, 35)), 1.5), roofPath);

        // Roof Scallop Shingle Texture
        if (!IsMiniMode)
        {
            var shinglePen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1.0);
            dc.DrawLine(shinglePen, new Point(x + 5, wallY - 8), new Point(x + w - 5, wallY - 8));
        }

        // Arched Window with Warm Golden Lamp Glow
        double winW = w * 0.28;
        double winH = wallH * 0.48;
        double winX = x + (w * 0.12);
        double winY = wallY + (wallH * 0.16);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 240, 140)), new Pen(new SolidColorBrush(Color.FromRgb(140, 110, 80)), 1.2), new Rect(winX, winY, winW, winH), winW * 0.5, winW * 0.5);
        // Window Crossbars
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(150, 115, 80)), 0.8), new Point(winX + (winW * 0.5), winY), new Point(winX + (winW * 0.5), winY + winH));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(150, 115, 80)), 0.8), new Point(winX, winY + (winH * 0.5)), new Point(winX + winW, winY + (winH * 0.5)));

        // Window Flower Box with Blossoms
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(140, 85, 50)), null, new Rect(winX - 2, winY + winH - 3, winW + 4, 5), 1, 1);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 105, 150)), null, new Point(winX + (winW * 0.25), winY + winH - 2), 2, 2);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 220, 70)), null, new Point(winX + (winW * 0.5), winY + winH - 2), 2, 2);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 105, 150)), null, new Point(winX + (winW * 0.75), winY + winH - 2), 2, 2);

        // Round Cute Wooden Door
        double doorW = w * 0.28;
        double doorH = wallH * 0.68;
        double doorX = x + (w * 0.58);
        double doorY = wallY + (wallH - doorH);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(155, 90, 50)), new Pen(new SolidColorBrush(Color.FromRgb(110, 60, 30)), 1), new Rect(doorX, doorY, doorW, doorH), doorW * 0.5, 0);
        // Heart Door Wreath ♡
        DrawHeart(dc, doorX + (doorW * 0.5), doorY + (doorH * 0.35), 3, new SolidColorBrush(Color.FromRgb(255, 120, 160)));
        // Golden knob
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 220, 70)), null, new Point(doorX + (doorW * 0.82), doorY + (doorH * 0.58)), 2, 2);

        // Cute Mailbox on Post (Left of Cottage)
        if (!IsMiniMode)
        {
            double mbX = x - 12;
            double mbY = wallY + wallH - 16;
            // Post
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(140, 95, 60)), null, new Rect(mbX + 3, mbY + 6, 3, 10));
            // Box
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(240, 240, 245)), new Pen(new SolidColorBrush(Color.FromRgb(160, 160, 170)), 0.8), new Rect(mbX, mbY, 11, 7), 3, 2);
            // Red flag
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(240, 60, 50)), 1.5), new Point(mbX + 9, mbY + 4), new Point(mbX + 9, mbY - 2));
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(240, 60, 50)), null, new Rect(mbX + 9, mbY - 2, 3, 2));
        }
    }

    private void DrawCuteCyclingBoyAndPuppy(DrawingContext dc, double x, double groundY, bool isMini, bool isPedaling)
    {
        double scale = isMini ? 0.68 : 1.0;
        double wheelRadius = 12 * scale;
        double wheelDist = 34 * scale;

        Point rearHub = new Point(x, groundY - wheelRadius);
        Point frontHub = new Point(x + wheelDist, groundY - wheelRadius);
        Point crank = new Point(x + (wheelDist * 0.48), groundY - wheelRadius + (2 * scale));
        Point seatPost = new Point(x + (wheelDist * 0.32), groundY - (wheelRadius * 2.1));
        Point headTube = new Point(x + (wheelDist * 0.85), groundY - (wheelRadius * 2.3));
        Point handleBar = new Point(x + (wheelDist * 0.88), groundY - (wheelRadius * 2.8));

        // 1. Bicycle Wheels
        DrawCuteWheel(dc, rearHub, wheelRadius, isPedaling);
        DrawCuteWheel(dc, frontHub, wheelRadius, isPedaling);

        // 2. Cute Pastel Bicycle Frame (Mint Aqua)
        var framePen = new Pen(new SolidColorBrush(Color.FromRgb(48, 195, 185)), 2.6 * scale);
        dc.DrawLine(framePen, rearHub, crank);
        dc.DrawLine(framePen, crank, seatPost);
        dc.DrawLine(framePen, seatPost, rearHub);
        dc.DrawLine(framePen, crank, headTube);
        dc.DrawLine(framePen, seatPost, headTube);
        dc.DrawLine(framePen, headTube, frontHub);
        dc.DrawLine(framePen, headTube, handleBar);

        // Handlebars with cute golden bell & colorful streamers
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(45, 50, 55)), 3 * scale), handleBar, new Point(handleBar.X - (4 * scale), handleBar.Y - (2 * scale)));
        // Little golden bell
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 220, 75)), null, new Point(handleBar.X - (2 * scale), handleBar.Y - (3 * scale)), 1.8 * scale, 1.8 * scale);

        if (isPedaling && !isMini)
        {
            double streamerWave = Math.Sin(_frameTick * 0.6) * 3;
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 120, 180)), 1.5), handleBar, new Point(handleBar.X - 10, handleBar.Y + streamerWave));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 220, 80)), 1.5), handleBar, new Point(handleBar.X - 8, handleBar.Y + 3 + streamerWave));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(120, 220, 255)), 1.5), handleBar, new Point(handleBar.X - 9, handleBar.Y - 2 + streamerWave));
        }

        // Saddle
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(140, 80, 50)), 3.8 * scale), new Point(seatPost.X - (8 * scale), seatPost.Y - (2 * scale)), new Point(seatPost.X + (5 * scale), seatPost.Y - (2 * scale)));

        // 3. FRONT BASKET & CUTE PUPPY 🐶
        Point basketPos = new Point(handleBar.X + (2 * scale), handleBar.Y + (2 * scale));
        double basketW = 14 * scale;
        double basketH = 10 * scale;
        // Wicker basket
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(218, 168, 108)), new Pen(new SolidColorBrush(Color.FromRgb(150, 100, 50)), 1), new Rect(basketPos.X, basketPos.Y, basketW, basketH), 2, 2);
        // Yellow flower on basket
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 225, 80)), null, new Point(basketPos.X + basketW - (2 * scale), basketPos.Y + (3 * scale)), 2 * scale, 2 * scale);

        // Puppy in basket!
        double pupBounce = isPedaling ? Math.Sin(_frameTick * 0.9) * (1.5 * scale) : 0;
        Point pupHead = new Point(basketPos.X + (basketW * 0.5), basketPos.Y - (4 * scale) + pupBounce);
        // Fluffy Head
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(248, 198, 132)), null, pupHead, 5.8 * scale, 5.2 * scale);
        // Floppy ears that bounce
        double earFlap = isPedaling ? Math.Sin(_frameTick * 0.8) * 2 * scale : 0;
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(195, 138, 78)), null, new Point(pupHead.X - (5.2 * scale), pupHead.Y + (1 * scale) + earFlap), 2.6 * scale, 4.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(195, 138, 78)), null, new Point(pupHead.X + (5.2 * scale), pupHead.Y + (1 * scale) + earFlap), 2.6 * scale, 4.2 * scale);
        // Puppy cute face
        dc.DrawEllipse(new SolidColorBrush(Colors.Black), null, new Point(pupHead.X - (2 * scale), pupHead.Y - (0.5 * scale)), 1.1 * scale, 1.1 * scale);
        dc.DrawEllipse(new SolidColorBrush(Colors.Black), null, new Point(pupHead.X + (2 * scale), pupHead.Y - (0.5 * scale)), 1.1 * scale, 1.1 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(70, 45, 30)), null, new Point(pupHead.X, pupHead.Y + (1.5 * scale)), 1.2 * scale, 0.8 * scale);
        // Puppy pink smiling tongue
        if (isPedaling)
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 120, 145)), null, new Point(pupHead.X, pupHead.Y + (2.6 * scale)), 1 * scale, 1.2 * scale);
        }
        // Puppy blush
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(140, 255, 130, 150)), null, new Point(pupHead.X - (3.5 * scale), pupHead.Y + (1.8 * scale)), 1.8 * scale, 1 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(140, 255, 130, 150)), null, new Point(pupHead.X + (3.5 * scale), pupHead.Y + (1.8 * scale)), 1.8 * scale, 1 * scale);
        // Puppy cute paws on basket rim
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(248, 198, 132)), null, new Point(basketPos.X + (3.5 * scale), basketPos.Y + (1 * scale)), 2 * scale, 1.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(248, 198, 132)), null, new Point(basketPos.X + basketW - (3.5 * scale), basketPos.Y + (1 * scale)), 2 * scale, 1.5 * scale);

        // 4. CHIBI BOY CYCLIST
        double pedalAngle = isPedaling ? (_frameTick * 0.45) : 0;
        double crankLen = 5 * scale;
        Point pedalPoint = new Point(crank.X + (Math.Cos(pedalAngle) * crankLen), crank.Y + (Math.Sin(pedalAngle) * crankLen));

        Point hip = new Point(seatPost.X + (1 * scale), seatPost.Y - (6 * scale));
        Point shoulder = new Point(seatPost.X + (13 * scale), seatPost.Y - (22 * scale));
        Point head = new Point(shoulder.X + (2 * scale), shoulder.Y - (9 * scale));

        // Pedaling Legs & Cute Sneakers
        var legPen = new Pen(new SolidColorBrush(Color.FromRgb(45, 80, 150)), 3 * scale);
        Point knee = new Point(hip.X + (10 * scale) + (Math.Sin(pedalAngle) * 3 * scale), hip.Y + (8 * scale));
        dc.DrawLine(legPen, hip, knee);
        dc.DrawLine(legPen, knee, pedalPoint);
        // Cute red sneakers rotating on pedal
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 95, 85)), null, new Rect(pedalPoint.X - (3.5 * scale), pedalPoint.Y - (1.5 * scale), 7 * scale, 3.5 * scale), 1.5 * scale, 1.5 * scale);

        // Cute Bright Hoodie / Shirt
        var shirtPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 105, 95)), 6.8 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(shirtPen, hip, shoulder);

        // Arm reaching forward
        var armPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 205, 165)), 2.8 * scale);
        dc.DrawLine(armPen, shoulder, handleBar);

        // Chibi Cute Face
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 180)), null, head, 6.5 * scale, 6.5 * scale);
        // Cute anime eye with catchlight
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(40, 30, 25)), null, new Point(head.X + (3 * scale), head.Y - (0.5 * scale)), 1.4 * scale, 1.8 * scale);
        dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(head.X + (3.5 * scale), head.Y - (1.2 * scale)), 0.6 * scale, 0.6 * scale);
        // Rosy blush spot
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(170, 255, 120, 140)), null, new Point(head.X + (2 * scale), head.Y + (2.2 * scale)), 2.5 * scale, 1.5 * scale);

        // Cute Tilted Bicycle Helmet / Cap
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 218, 65)), null, new Point(head.X - (0.5 * scale), head.Y - (3.5 * scale)), 7.2 * scale, 5.5 * scale);
        // Helmet Visor
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 180, 20)), 2.5 * scale), new Point(head.X, head.Y - (2 * scale)), new Point(head.X + (9 * scale), head.Y - (2 * scale)));
    }

    private void DrawCuteWheel(DrawingContext dc, Point hub, double radius, bool isSpinning)
    {
        // Tire
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(45, 48, 55)), 2.5), hub, radius, radius);
        // Rim (Pastel accent)
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(210, 235, 245)), 1.2), hub, radius - 1.5, radius - 1.5);

        // Spokes
        double spokeOffset = isSpinning ? (_frameTick * 0.45) : 0;
        var spokePen = new Pen(new SolidColorBrush(Color.FromRgb(185, 195, 205)), 0.8);
        for (int i = 0; i < 4; i++)
        {
            double angle = spokeOffset + (i * Math.PI / 2.0);
            Point p1 = new Point(hub.X + (Math.Cos(angle) * (radius - 2)), hub.Y + (Math.Sin(angle) * (radius - 2)));
            Point p2 = new Point(hub.X - (Math.Cos(angle) * (radius - 2)), hub.Y - (Math.Sin(angle) * (radius - 2)));
            dc.DrawLine(spokePen, p1, p2);
        }

        // Center hub
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 60)), null, hub, 2.5, 2.5);
    }

    private void DrawCelebrationBoyAndPuppy(DrawingContext dc, double x, double groundY, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;
        Point boyPos = new Point(x, groundY - (30 * scale));

        // Cheering Boy \o/
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 180)), null, new Point(boyPos.X, boyPos.Y - (8 * scale)), 7 * scale, 7 * scale);
        // Happy Closed Eyes (^.^)
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(40, 30, 25)), 1.2 * scale), new Point(boyPos.X - (4 * scale), boyPos.Y - (8 * scale)), new Point(boyPos.X - (1 * scale), boyPos.Y - (10 * scale)));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(40, 30, 25)), 1.2 * scale), new Point(boyPos.X + (1 * scale), boyPos.Y - (10 * scale)), new Point(boyPos.X + (4 * scale), boyPos.Y - (8 * scale)));
        // Blush & Face
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 120, 140)), null, new Point(boyPos.X - (3 * scale), boyPos.Y - (5 * scale)), 2.5 * scale, 1.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 120, 140)), null, new Point(boyPos.X + (3 * scale), boyPos.Y - (5 * scale)), 2.5 * scale, 1.5 * scale);
        // Helmet
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 60)), null, new Point(boyPos.X, boyPos.Y - (11 * scale)), 7.5 * scale, 5 * scale);

        // Torso
        var shirtPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 105, 95)), 7 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(shirtPen, new Point(boyPos.X, boyPos.Y - (2 * scale)), new Point(boyPos.X, boyPos.Y + (12 * scale)));

        // Raised arms \o/
        var armPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 205, 165)), 3 * scale);
        dc.DrawLine(armPen, new Point(boyPos.X, boyPos.Y + (2 * scale)), new Point(boyPos.X - (10 * scale), boyPos.Y - (10 * scale)));
        dc.DrawLine(armPen, new Point(boyPos.X, boyPos.Y + (2 * scale)), new Point(boyPos.X + (10 * scale), boyPos.Y - (10 * scale)));

        // Jumping Puppy Beside Boy with Heart Bubbles 🐶💖
        Point pupPos = new Point(boyPos.X + (18 * scale), groundY - (14 * scale) - (Math.Sin(_frameTick * 0.8) * 6 * scale));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(248, 198, 132)), null, pupPos, 7 * scale, 6 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(195, 138, 78)), null, new Point(pupPos.X - (4 * scale), pupPos.Y - (4 * scale)), 3 * scale, 4 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(195, 138, 78)), null, new Point(pupPos.X + (4 * scale), pupPos.Y - (4 * scale)), 3 * scale, 4 * scale);
        // Puppy happy heart
        DrawHeart(dc, pupPos.X, pupPos.Y - (10 * scale), 4 * scale, new SolidColorBrush(Color.FromRgb(255, 105, 150)));

        // Celebration Confetti
        if (!isMini)
        {
            var rand = new Random(333 + (_frameTick / 3));
            for (int c = 0; c < 6; c++)
            {
                double cx = boyPos.X - 25 + rand.Next(60);
                double cy = boyPos.Y - 20 + rand.Next(35);
                DrawSparkle(dc, cx, cy, 3, new SolidColorBrush(Color.FromRgb((byte)rand.Next(200, 255), (byte)rand.Next(150, 255), (byte)rand.Next(100, 255))));
            }
        }

        // Banner
        string text = isMini ? "★ HOME! ★" : "🎉 WELCOME HOME! 🏡✨";
        var ft = CreateText(text, isMini ? 10 : 13, new SolidColorBrush(Color.FromRgb(255, 230, 80)), FontWeights.Bold);
        dc.DrawText(ft, new Point(boyPos.X - (ft.Width / 2), boyPos.Y - (30 * scale)));
    }

    private void DrawRestPicnic(DrawingContext dc, double x, double groundY, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;

        // Picnic Blanket (Checkered Red/White)
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(240, 110, 100)), null, new Rect(x - (24 * scale), groundY - (6 * scale), 48 * scale, 6 * scale), 2, 2);

        // Relaxing boy and puppy cuddled together
        Point boyHead = new Point(x - (8 * scale), groundY - (20 * scale));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 180)), null, boyHead, 6 * scale, 6 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 120, 140)), null, new Point(boyHead.X, boyHead.Y + (1 * scale)), 2 * scale, 1.2 * scale);

        // Puppy sleeping beside boy
        Point pup = new Point(x + (8 * scale), groundY - (10 * scale));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(248, 198, 132)), null, pup, 7 * scale, 5 * scale);

        // Floating rest tea & "z Z"
        string zText = (_frameTick % 20 < 10) ? "🍵 z Z" : "🍵 Z z";
        var ft = CreateText(zText, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(255, 220, 90)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x + (14 * scale), groundY - (28 * scale)));
    }

    #endregion

    #region 🚀 Scene 2: Cute Space Rocket Odyssey

    private void RenderRocketScene(DrawingContext dc, double w, double h)
    {
        // 1. Dreamy Galaxy Gradient (Velvet Indigo -> Deep Nebula Magenta)
        var spaceBrush = new LinearGradientBrush(
            Color.FromRgb(12, 10, 32),
            Color.FromRgb(35, 18, 55),
            new Point(0, 0),
            new Point(1, 1));
        dc.DrawRectangle(spaceBrush, null, new Rect(0, 0, w, h));

        // Soft Nebula Glow Clouds
        var nebulaBrush = new RadialGradientBrush(Color.FromArgb(45, 230, 80, 180), Color.FromArgb(0, 120, 40, 160));
        dc.DrawEllipse(nebulaBrush, null, new Point(w * 0.55, h * 0.4), w * 0.4, h * 0.6);

        // 2. Twinkling Pastel Stars
        var starRand = new Random(101);
        int starCount = IsMiniMode ? 28 : 60;
        for (int i = 0; i < starCount; i++)
        {
            double sx = starRand.NextDouble() * w;
            double sy = starRand.NextDouble() * h;
            double pulse = Math.Sin((_frameTick * 0.25) + i) * 0.5 + 0.5;
            byte alpha = (byte)(110 + (pulse * 145));

            if (i % 6 == 0)
            {
                var starBrush = new SolidColorBrush(Color.FromArgb(alpha, 255, 235, 140));
                DrawSparkle(dc, sx, sy, 3.5, starBrush);
            }
            else
            {
                var dotBrush = new SolidColorBrush(Color.FromArgb(alpha, (byte)(i % 2 == 0 ? 255 : 180), 240, 255));
                dc.DrawEllipse(dotBrush, null, new Point(sx, sy), 1.2, 1.2);
            }
        }

        // 3. Distant Saturn-like Ringed Planet in Deep Space
        double saturnR = IsMiniMode ? 9 : 16;
        DrawDistantSaturnPlanet(dc, w * 0.82, h * 0.20, saturnR, IsMiniMode);

        // 4. Streaking Space Comet Event
        DrawStreakingSpaceComet(dc, w, h, IsMiniMode);

        // 5. Launch Planet Earth on Left (0%)
        double earthRadius = IsMiniMode ? 28 : 55;
        Point earthCenter = new Point(0, h * 0.5);
        var earthBrush = new RadialGradientBrush(Color.FromRgb(85, 190, 255), Color.FromRgb(25, 80, 180));
        dc.DrawEllipse(earthBrush, new Pen(new SolidColorBrush(Color.FromArgb(120, 150, 220, 255)), 2), earthCenter, earthRadius, earthRadius);
        // Cute green continent & cloud swirls
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(80, 210, 125)), null, new Point(earthRadius * 0.42, (h * 0.5) - (earthRadius * 0.2)), earthRadius * 0.35, earthRadius * 0.25);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), null, new Point(earthRadius * 0.35, (h * 0.5) + (earthRadius * 0.15)), earthRadius * 0.28, earthRadius * 0.15);

        // 6. Moon Destination on Right (100%)
        double moonRadius = IsMiniMode ? 26 : 50;
        Point moonCenter = new Point(w, h * 0.5);
        var moonBrush = new RadialGradientBrush(Color.FromRgb(255, 250, 220), Color.FromRgb(210, 205, 175));
        dc.DrawEllipse(moonBrush, new Pen(new SolidColorBrush(Color.FromArgb(140, 255, 245, 180)), 2), moonCenter, moonRadius, moonRadius);
        // Moon Craters
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(190, 185, 155)), null, new Point(w - (moonRadius * 0.5), (h * 0.5) - (moonRadius * 0.3)), moonRadius * 0.2, moonRadius * 0.2);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(195, 190, 160)), null, new Point(w - (moonRadius * 0.65), (h * 0.5) + (moonRadius * 0.25)), moonRadius * 0.25, moonRadius * 0.25);

        // Trajectory flight line
        var trajPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 180, 220, 255)), 1.5) { DashStyle = DashStyles.Dash };
        dc.DrawLine(trajPen, new Point(earthRadius + 5, h * 0.5), new Point(w - moonRadius - 5, h * 0.5));

        // 7. Dynamic Asteroid Field (Tumbling space rocks with craters & crystal ores)
        DrawAsteroidField(dc, w, h, IsMiniMode);

        // 8. Orbital Satellite Relay Probe
        DrawOrbitalSatellite(dc, w, h, IsMiniMode);

        // 9. Cute Alien UFO Flyby & Waving Alien Encounter
        DrawAlienUfoEncounter(dc, w, h, IsMiniMode);

        // 10. Chunky Cute Rocket
        double startX = earthRadius + (IsMiniMode ? 10 : 20);
        double endX = w - moonRadius - (IsMiniMode ? 14 : 26);
        double rocketX = startX + (ProgressFraction * (endX - startX));
        double rocketY = (h * 0.5) + (Math.Sin(_frameTick * 0.2) * (IsMiniMode ? 2.5 : 4.5));

        if (IsRestPhase)
        {
            DrawCuteZeroGLounge(dc, w * 0.5, h * 0.5, IsMiniMode);
        }
        else if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawCuteMoonLanding(dc, w - moonRadius - (IsMiniMode ? 10 : 18), h * 0.5, IsMiniMode);
        }
        else
        {
            DrawChunkyChibiRocket(dc, rocketX, rocketY, IsMiniMode, IsTracking);
        }
    }

    private void DrawDistantSaturnPlanet(DrawingContext dc, double x, double y, double r, bool isMini)
    {
        // 1. Back planetary ring (drawn behind planet body)
        var backRingPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 200, 160)), isMini ? 1.6 : 3.0);
        var backRingGeom = new PathGeometry();
        var brf = new PathFigure { StartPoint = new Point(x - (r * 1.7), y + (r * 0.2)) };
        brf.Segments.Add(new QuadraticBezierSegment(new Point(x, y - (r * 0.9)), new Point(x + (r * 1.7), y - (r * 0.4)), false));
        backRingGeom.Figures.Add(brf);
        dc.DrawGeometry(null, backRingPen, backRingGeom);

        // 2. Planet Body (Lavender / Peach gradient)
        var planetBrush = new LinearGradientBrush(
            Color.FromRgb(220, 180, 240),
            Color.FromRgb(255, 160, 150),
            new Point(0, 0),
            new Point(1, 1));
        dc.DrawEllipse(planetBrush, null, new Point(x, y), r, r);

        // Planet atmospheric stripe bands
        var bandBrush = new SolidColorBrush(Color.FromArgb(45, 140, 70, 160));
        dc.DrawRectangle(bandBrush, null, new Rect(x - (r * 0.85), y - (r * 0.25), r * 1.7, r * 0.22));
        dc.DrawRectangle(bandBrush, null, new Rect(x - (r * 0.75), y + (r * 0.15), r * 1.5, r * 0.18));

        // 3. Front planetary ring (drawn across front of planet)
        var frontRingPen = new Pen(new SolidColorBrush(Color.FromArgb(170, 255, 215, 175)), isMini ? 1.8 : 3.2);
        var frontRingGeom = new PathGeometry();
        var frf = new PathFigure { StartPoint = new Point(x - (r * 1.7), y + (r * 0.2)) };
        frf.Segments.Add(new QuadraticBezierSegment(new Point(x, y + (r * 0.9)), new Point(x + (r * 1.7), y - (r * 0.4)), false));
        frontRingGeom.Figures.Add(frf);
        dc.DrawGeometry(null, frontRingPen, frontRingGeom);

        // Tiny pastel moonlet orbiting nearby
        double moonletOrbit = (_frameTick * 0.04);
        double mx = x + (Math.Cos(moonletOrbit) * (r * 2.1));
        double my = y + (Math.Sin(moonletOrbit) * (r * 0.75));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 235, 190)), null, new Point(mx, my), isMini ? 1.1 : 1.8, isMini ? 1.1 : 1.8);
    }

    private void DrawStreakingSpaceComet(DrawingContext dc, double w, double h, bool isMini)
    {
        // Periodic comet streak passing through deep space every ~160 ticks
        int cycle = (_frameTick % 160);
        if (cycle > 50) return;

        double progress = cycle / 50.0;
        double startX = w * 0.85;
        double startY = -20;
        double endX = -40;
        double endY = h * 0.75;

        double curX = startX + (progress * (endX - startX));
        double curY = startY + (progress * (endY - startY));
        double tailLen = (isMini ? 35 : 70) * (1.0 - (progress * 0.3));

        double dx = endX - startX;
        double dy = endY - startY;
        double angle = Math.Atan2(dy, dx);
        double tailX = curX - (Math.Cos(angle) * tailLen);
        double tailY = curY - (Math.Sin(angle) * tailLen);

        double alpha = Math.Sin(progress * Math.PI);
        byte tailAlpha = (byte)(180 * alpha);
        byte headAlpha = (byte)(240 * alpha);

        // Long gradient comet tail
        var tailBrush = new LinearGradientBrush(
            Color.FromArgb(0, 80, 200, 255),
            Color.FromArgb(tailAlpha, 200, 245, 255),
            new Point(0, 0),
            new Point(1, 1));
        var tailPen = new Pen(tailBrush, isMini ? 2.0 : 3.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(tailPen, new Point(tailX, tailY), new Point(curX, curY));

        // Thin core streamer
        var corePen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(220 * alpha), 255, 255, 255)), isMini ? 1.0 : 1.8);
        dc.DrawLine(corePen, new Point(curX - (Math.Cos(angle) * tailLen * 0.5), curY - (Math.Sin(angle) * tailLen * 0.5)), new Point(curX, curY));

        // Comet glowing head & sparkles
        var cometHeadBrush = new RadialGradientBrush(Color.FromArgb(headAlpha, 255, 255, 255), Color.FromArgb(0, 100, 220, 255));
        dc.DrawEllipse(cometHeadBrush, null, new Point(curX, curY), isMini ? 4 : 8, isMini ? 4 : 8);
        DrawSparkle(dc, curX, curY, (isMini ? 3.0 : 5.5) * alpha, new SolidColorBrush(Color.FromArgb((byte)(255 * alpha), 255, 245, 180)));
    }

    private enum SpaceAsteroidType { CrystalAmethyst, RockyCrater, GoldOre }

    private void DrawAsteroidField(DrawingContext dc, double w, double h, bool isMini)
    {
        double scale = isMini ? 0.65 : 1.0;

        // Asteroid 1: Upper Space Rock with glowing purple amethyst crystal veins!
        double a1BaseX = w * 0.36;
        double a1BaseY = h * 0.22;
        double a1Bob = Math.Sin((_frameTick * 0.05) + 1.2) * (isMini ? 2 : 4);
        double a1Drift = Math.Cos((_frameTick * 0.03)) * (isMini ? 3 : 6);
        DrawSingleAsteroid(dc, a1BaseX + a1Drift, a1BaseY + a1Bob, 11 * scale, 0.4 + (_frameTick * 0.012), SpaceAsteroidType.CrystalAmethyst, isMini);

        // Asteroid 2: Lower-right Space Rock with cratered rocky surface
        double a2BaseX = w * 0.66;
        double a2BaseY = h * 0.74;
        double a2Bob = Math.Cos((_frameTick * 0.06) + 2.5) * (isMini ? 2 : 4);
        double a2Drift = Math.Sin((_frameTick * 0.04) + 1.0) * (isMini ? 3 : 5);
        DrawSingleAsteroid(dc, a2BaseX + a2Drift, a2BaseY + a2Bob, 9.5 * scale, -0.6 - (_frameTick * 0.015), SpaceAsteroidType.RockyCrater, isMini);

        // Asteroid 3: Golden Ore Micro-asteroid drifting in the background
        if (!isMini)
        {
            double a3BaseX = w * 0.18;
            double a3BaseY = h * 0.78;
            double a3Bob = Math.Sin((_frameTick * 0.07)) * 3;
            DrawSingleAsteroid(dc, a3BaseX, a3BaseY + a3Bob, 6.0, 0.2 + (_frameTick * 0.02), SpaceAsteroidType.GoldOre, false);
        }
    }

    private void DrawSingleAsteroid(DrawingContext dc, double cx, double cy, double r, double rot, SpaceAsteroidType type, bool isMini)
    {
        var geom = new PathGeometry();
        int vertices = 7;
        double[] radOffsets = { 1.0, 0.82, 1.15, 0.90, 1.08, 0.78, 1.12 };

        var fig = new PathFigure();
        for (int v = 0; v < vertices; v++)
        {
            double angle = rot + (v * (Math.PI * 2.0 / vertices));
            double curR = r * radOffsets[v % radOffsets.Length];
            double px = cx + (Math.Cos(angle) * curR);
            double py = cy + (Math.Sin(angle) * curR);

            if (v == 0)
            {
                fig.StartPoint = new Point(px, py);
            }
            else
            {
                fig.Segments.Add(new LineSegment(new Point(px, py), true));
            }
        }
        fig.IsClosed = true;
        geom.Figures.Add(fig);

        Color topColor = type switch
        {
            SpaceAsteroidType.CrystalAmethyst => Color.FromRgb(150, 138, 168),
            SpaceAsteroidType.GoldOre => Color.FromRgb(165, 140, 110),
            _ => Color.FromRgb(140, 132, 145)
        };
        Color btmColor = type switch
        {
            SpaceAsteroidType.CrystalAmethyst => Color.FromRgb(85, 75, 105),
            SpaceAsteroidType.GoldOre => Color.FromRgb(95, 75, 55),
            _ => Color.FromRgb(75, 68, 80)
        };

        var rockBrush = new LinearGradientBrush(topColor, btmColor, new Point(0, 0), new Point(0.7, 1));
        var rockPen = new Pen(new SolidColorBrush(Color.FromArgb(160, (byte)(btmColor.R * 0.6), (byte)(btmColor.G * 0.6), (byte)(btmColor.B * 0.6))), 1.0);
        dc.DrawGeometry(rockBrush, rockPen, geom);

        // Craters on surface
        double c1x = cx - (r * 0.25 * Math.Cos(rot));
        double c1y = cy - (r * 0.25 * Math.Sin(rot));
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, (byte)(btmColor.R * 0.5), (byte)(btmColor.G * 0.5), (byte)(btmColor.B * 0.5))), null, new Point(c1x, c1y), r * 0.28, r * 0.24);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), 0.8), new Point(c1x - 0.5, c1y - 0.5), r * 0.26, r * 0.22);

        // Embedded glowing crystal or gold ore glints
        if (type == SpaceAsteroidType.CrystalAmethyst)
        {
            double crystalX = cx + (r * 0.32 * Math.Cos(rot + 1.8));
            double crystalY = cy + (r * 0.32 * Math.Sin(rot + 1.8));
            var crystalBrush = new SolidColorBrush(Color.FromRgb(215, 130, 255));
            dc.DrawEllipse(crystalBrush, null, new Point(crystalX, crystalY), r * 0.22, r * 0.22);
            DrawSparkle(dc, crystalX, crystalY, isMini ? 1.8 : 3.0, new SolidColorBrush(Color.FromRgb(245, 190, 255)));
        }
        else if (type == SpaceAsteroidType.GoldOre && !isMini)
        {
            double goldX = cx + (r * 0.35 * Math.Cos(rot + 2.2));
            double goldY = cy + (r * 0.35 * Math.Sin(rot + 2.2));
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 220, 80)), null, new Point(goldX, goldY), r * 0.20, r * 0.20);
            DrawSparkle(dc, goldX, goldY, 2.2, new SolidColorBrush(Color.FromRgb(255, 245, 160)));
        }
    }

    private void DrawAlienUfoEncounter(DrawingContext dc, double w, double h, bool isMini)
    {
        double scale = isMini ? 0.65 : 0.95;

        // UFO floats smoothly in an orbit path
        double ufoX = (w * 0.52) + (Math.Sin(_frameTick * 0.05) * (w * (isMini ? 0.12 : 0.16)));
        double ufoY = (h * 0.26) + (Math.Cos(_frameTick * 0.07) * (isMini ? 4.0 : 7.0));
        double ufoTilt = Math.Sin(_frameTick * 0.05) * 5.0;

        dc.PushTransform(new RotateTransform(ufoTilt, ufoX, ufoY));

        // 1. Neon Tractor Beam Cone (pulsing translucent lime/cyan glow)
        double beamPulse = (Math.Sin(_frameTick * 0.18) * 0.15) + 0.85;
        var beamBrush = new LinearGradientBrush(
            Color.FromArgb((byte)(55 * beamPulse), 100, 255, 210),
            Color.FromArgb(0, 60, 230, 160),
            new Point(0.5, 0),
            new Point(0.5, 1));
        var beamGeom = new PathGeometry();
        var bf = new PathFigure { StartPoint = new Point(ufoX - (8 * scale), ufoY + (4 * scale)) };
        bf.Segments.Add(new LineSegment(new Point(ufoX - (22 * scale), ufoY + (38 * scale)), true));
        bf.Segments.Add(new LineSegment(new Point(ufoX + (22 * scale), ufoY + (38 * scale)), true));
        bf.Segments.Add(new LineSegment(new Point(ufoX + (8 * scale), ufoY + (4 * scale)), true));
        bf.IsClosed = true;
        beamGeom.Figures.Add(bf);
        dc.DrawGeometry(beamBrush, null, beamGeom);

        // 2. Translucent Glass Cockpit Bubble
        double domeR = 10 * scale;
        Point domeCenter = new Point(ufoX, ufoY - (4 * scale));
        var domeBrush = new LinearGradientBrush(
            Color.FromArgb(220, 180, 245, 255),
            Color.FromArgb(140, 90, 200, 240),
            new Point(0, 0),
            new Point(1, 1));
        dc.DrawEllipse(domeBrush, new Pen(new SolidColorBrush(Color.FromArgb(180, 140, 225, 255)), 1.0 * scale), domeCenter, domeR, domeR * 0.95);

        // 3. Cute Little Kawaii Green Alien Pilot! 👽
        Point alienHead = new Point(ufoX, ufoY - (4 * scale));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(130, 235, 110)), null, alienHead, 5.2 * scale, 4.8 * scale);

        // Little antenna with glowing bulb
        double antWave = Math.Sin(_frameTick * 0.22) * (1.2 * scale);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(100, 205, 80)), 1.2 * scale), new Point(ufoX, alienHead.Y - (4.8 * scale)), new Point(ufoX + antWave, alienHead.Y - (9 * scale)));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 235, 90)), null, new Point(ufoX + antWave, alienHead.Y - (9 * scale)), 1.8 * scale, 1.8 * scale);

        // Big kawaii dark eyes with catchlights
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(24, 32, 28)), null, new Point(ufoX - (2.2 * scale), alienHead.Y - (0.4 * scale)), 1.6 * scale, 2.0 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(24, 32, 28)), null, new Point(ufoX + (2.2 * scale), alienHead.Y - (0.4 * scale)), 1.6 * scale, 2.0 * scale);
        dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(ufoX - (2.6 * scale), alienHead.Y - (1.0 * scale)), 0.6 * scale, 0.6 * scale);
        dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(ufoX + (1.8 * scale), alienHead.Y - (1.0 * scale)), 0.6 * scale, 0.6 * scale);

        // Cute blushing cheeks
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(170, 255, 120, 160)), null, new Point(ufoX - (3.4 * scale), alienHead.Y + (1.6 * scale)), 1.2 * scale, 0.7 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(170, 255, 120, 160)), null, new Point(ufoX + (3.4 * scale), alienHead.Y + (1.6 * scale)), 1.2 * scale, 0.7 * scale);

        // Alien waving hand `(・ω・)ノ`
        double waveY = Math.Sin(_frameTick * 0.4) * (1.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(130, 235, 110)), null, new Point(ufoX + (4.8 * scale), alienHead.Y + (0.5 * scale) + waveY), 1.3 * scale, 1.3 * scale);

        // Glass reflection arc
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), null, new Point(domeCenter.X - (3.5 * scale), domeCenter.Y - (3.5 * scale)), 2.4 * scale, 1.2 * scale);

        // 4. Metallic Saucer Hull Disc
        double saucerRx = 20 * scale;
        double saucerRy = 7.0 * scale;
        var saucerBrush = new LinearGradientBrush(
            Color.FromRgb(215, 242, 245),
            Color.FromRgb(125, 168, 178),
            new Point(0, 0),
            new Point(0, 1));
        var saucerPen = new Pen(new SolidColorBrush(Color.FromRgb(95, 130, 140)), 1.2 * scale);
        dc.DrawEllipse(saucerBrush, saucerPen, new Point(ufoX, ufoY + (2 * scale)), saucerRx, saucerRy);

        // Inner hull rim
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(170, 210, 218)), null, new Point(ufoX, ufoY + (1 * scale)), saucerRx * 0.75, saucerRy * 0.6);

        // 5. Pulsing Multi-color Perimeter LED Nav Lights
        Color[] ledColors = { Color.FromRgb(110, 255, 140), Color.FromRgb(255, 235, 80), Color.FromRgb(100, 225, 255), Color.FromRgb(255, 120, 210) };
        for (int l = 0; l < 4; l++)
        {
            double lx = ufoX - (saucerRx * 0.72) + (l * (saucerRx * 1.44 / 3.0));
            double ly = ufoY + (3.2 * scale) + (Math.Sin(l * 0.8) * 1.2 * scale);
            int colorIdx = (_frameTick / 6 + l) % ledColors.Length;
            var ledBrush = new SolidColorBrush(ledColors[colorIdx]);
            dc.DrawEllipse(ledBrush, null, new Point(lx, ly), 1.8 * scale, 1.8 * scale);
            DrawSparkle(dc, lx, ly, 1.6 * scale, ledBrush);
        }

        dc.Pop(); // Pop UFO tilt transform
    }

    private void DrawOrbitalSatellite(DrawingContext dc, double w, double h, bool isMini)
    {
        double scale = isMini ? 0.60 : 0.85;

        // Satellite drifts gently in the lower-left space region
        double satX = (w * 0.28) + (Math.Cos(_frameTick * 0.04) * (isMini ? 4 : 8));
        double satY = (h * 0.76) + (Math.Sin(_frameTick * 0.05) * (isMini ? 3 : 5));
        double satTilt = -18.0 + (Math.Sin(_frameTick * 0.03) * 6.0);

        dc.PushTransform(new RotateTransform(satTilt, satX, satY));

        // 1. Solar Panels (Left and Right Wings with blue grid)
        double wingW = 14 * scale;
        double wingH = 8 * scale;
        var solarBrush = new LinearGradientBrush(Color.FromRgb(70, 140, 225), Color.FromRgb(30, 80, 160), new Point(0, 0), new Point(1, 1));
        var solarPen = new Pen(new SolidColorBrush(Color.FromRgb(180, 210, 255)), 0.8 * scale);

        // Left wing
        dc.DrawRectangle(solarBrush, solarPen, new Rect(satX - (18 * scale), satY - (wingH * 0.5), wingW, wingH));
        dc.DrawLine(solarPen, new Point(satX - (11 * scale), satY - (wingH * 0.5)), new Point(satX - (11 * scale), satY + (wingH * 0.5)));

        // Right wing
        dc.DrawRectangle(solarBrush, solarPen, new Rect(satX + (4 * scale), satY - (wingH * 0.5), wingW, wingH));
        dc.DrawLine(solarPen, new Point(satX + (11 * scale), satY - (wingH * 0.5)), new Point(satX + (11 * scale), satY + (wingH * 0.5)));

        // 2. Gold Foil Satellite Main Bus Core
        double coreSize = 8 * scale;
        var coreBrush = new LinearGradientBrush(Color.FromRgb(255, 220, 90), Color.FromRgb(205, 155, 45), new Point(0, 0), new Point(1, 1));
        dc.DrawRectangle(coreBrush, new Pen(new SolidColorBrush(Color.FromRgb(165, 120, 30)), 1.0 * scale), new Rect(satX - (coreSize * 0.5), satY - (coreSize * 0.5), coreSize, coreSize));

        // 3. Communications Dish & Blinking Telemetry Beacon
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(230, 235, 245)), new Pen(new SolidColorBrush(Color.FromRgb(140, 150, 170)), 0.8 * scale), new Point(satX, satY - (6 * scale)), 3.5 * scale, 2.0 * scale);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(210, 215, 230)), 1.0 * scale), new Point(satX, satY - (6 * scale)), new Point(satX, satY - (11 * scale)));

        // Blinking beacon light
        bool beaconBlink = (_frameTick % 20) < 10;
        var beaconColor = beaconBlink ? Color.FromRgb(255, 80, 80) : Color.FromRgb(120, 30, 30);
        dc.DrawEllipse(new SolidColorBrush(beaconColor), null, new Point(satX, satY - (11 * scale)), 1.5 * scale, 1.5 * scale);
        if (beaconBlink)
        {
            DrawSparkle(dc, satX, satY - (11 * scale), 2.2 * scale, new SolidColorBrush(Color.FromRgb(255, 160, 160)));
        }

        dc.Pop(); // Pop satellite tilt
    }

    private void DrawChunkyChibiRocket(DrawingContext dc, double x, double y, bool isMini, bool isThrusting)
    {
        double scale = isMini ? 0.68 : 1.0;
        double rocketLen = 42 * scale;
        double rocketH = 22 * scale;

        // 1. Dynamic Thrust Flame
        if (isThrusting)
        {
            double flameLen = (22 + (Math.Sin(_frameTick * 1.6) * 7)) * scale;
            var flameGeom = new PathGeometry();
            var flameFig = new PathFigure { StartPoint = new Point(x - (rocketLen * 0.45), y - (rocketH * 0.35)) };
            flameFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.45) - flameLen, y), true));
            flameFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.45), y + (rocketH * 0.35)), true));
            flameGeom.Figures.Add(flameFig);

            var flameBrush = new LinearGradientBrush(Color.FromRgb(255, 105, 60), Color.FromRgb(255, 225, 75), new Point(0, 0), new Point(1, 0));
            dc.DrawGeometry(flameBrush, null, flameGeom);

            // Core inner cyan flame
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(120, 245, 255)), null, new Point(x - (rocketLen * 0.45) - (flameLen * 0.35), y), 3.5 * scale, 3.5 * scale);

            // Trailing star sparks
            for (int s = 0; s < 3; s++)
            {
                double sparkX = x - (rocketLen * 0.45) - flameLen - ((_frameTick * 2 + (s * 8)) % 25);
                double sparkY = y + (Math.Sin(_frameTick * 0.5 + s) * 6 * scale);
                DrawSparkle(dc, sparkX, sparkY, 2.5 * scale, new SolidColorBrush(Color.FromRgb(255, 235, 120)));
            }
        }

        // 2. Chunky Rocket Body (Creamy White Capsule)
        var bodyBrush = new LinearGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(230, 240, 250), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(bodyBrush, new Pen(new SolidColorBrush(Color.FromRgb(180, 200, 225)), 1.2), new Rect(x - (rocketLen * 0.45), y - (rocketH * 0.5), rocketLen * 0.72, rocketH), 6 * scale, 6 * scale);

        // 3. Cute Pastel Coral Nose Cone
        var noseGeom = new PathGeometry();
        var noseFig = new PathFigure { StartPoint = new Point(x + (rocketLen * 0.27), y - (rocketH * 0.5)) };
        noseFig.Segments.Add(new QuadraticBezierSegment(new Point(x + (rocketLen * 0.58), y), new Point(x + (rocketLen * 0.27), y + (rocketH * 0.5)), true));
        noseGeom.Figures.Add(noseFig);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(255, 110, 105)), null, noseGeom);

        // 4. Cute Pastel Fins
        var finBrush = new SolidColorBrush(Color.FromRgb(255, 110, 105));
        var topFin = new PathGeometry();
        var tfFig = new PathFigure { StartPoint = new Point(x - (rocketLen * 0.45), y - (rocketH * 0.5)) };
        tfFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.6), y - (rocketH * 0.95)), true));
        tfFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.15), y - (rocketH * 0.5)), true));
        topFin.Figures.Add(tfFig);
        dc.DrawGeometry(finBrush, null, topFin);

        var btmFin = new PathGeometry();
        var bfFig = new PathFigure { StartPoint = new Point(x - (rocketLen * 0.45), y + (rocketH * 0.5)) };
        bfFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.6), y + (rocketH * 0.95)), true));
        bfFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.15), y + (rocketH * 0.5)), true));
        btmFin.Figures.Add(bfFig);
        dc.DrawGeometry(finBrush, null, btmFin);

        // 5. Big Glass Viewport with Cute Astronaut Waving Inside! 👨‍🚀🐱
        Point porthole = new Point(x - (1 * scale), y);
        double portR = 7 * scale;
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(85, 205, 255)), new Pen(new SolidColorBrush(Color.FromRgb(60, 80, 120)), 1.5 * scale), porthole, portR, portR);

        // Astronaut Head inside viewport
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 220, 185)), null, porthole, 4.2 * scale, 4.2 * scale);
        // Blushing cheeks
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 110, 140)), null, new Point(porthole.X + (1 * scale), porthole.Y + (1 * scale)), 1.5 * scale, 0.9 * scale);
        // Waving hand
        double handWave = Math.Sin(_frameTick * 0.7) * (1.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new Point(porthole.X + (3.5 * scale), porthole.Y - (1 * scale) + handWave), 1.5 * scale, 1.5 * scale);

        // Glass reflection glint
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), null, new Point(porthole.X - (2.5 * scale), porthole.Y - (2.5 * scale)), 2 * scale, 2 * scale);
    }

    private void DrawCuteMoonLanding(DrawingContext dc, double x, double y, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;

        DrawChunkyChibiRocket(dc, x - (12 * scale), y, isMini, false);

        // Planted Flagpole
        Point flagPole = new Point(x + (16 * scale), y - (20 * scale));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(235, 235, 245)), 2 * scale), flagPole, new Point(flagPole.X, flagPole.Y + (28 * scale)));

        // Flag banner with golden star
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(255, 105, 95)), null, new Rect(flagPole.X, flagPole.Y, 16 * scale, 10 * scale));
        DrawSparkle(dc, flagPole.X + (8 * scale), flagPole.Y + (5 * scale), 3.5 * scale, new SolidColorBrush(Color.FromRgb(255, 225, 75)));

        string text = isMini ? "★ MOON BASE! ★" : "🚀 MISSION ACCOMPLISHED! 🌕✨";
        var ft = CreateText(text, isMini ? 10 : 13, new SolidColorBrush(Color.FromRgb(255, 230, 80)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.45), y - (36 * scale)));
    }

    private void DrawCuteZeroGLounge(DrawingContext dc, double x, double y, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;
        double floatBob = Math.Sin(_frameTick * 0.18) * 4 * scale;

        // Floating Cute Astronaut
        Point astroCenter = new Point(x, y + floatBob);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(250, 252, 255)), new Pen(new SolidColorBrush(Color.FromRgb(120, 160, 200)), 1.8 * scale), astroCenter, 15 * scale, 15 * scale);
        // Visor
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 60)), null, new Point(astroCenter.X + (2 * scale), astroCenter.Y), 7 * scale, 6 * scale);

        // Floating Boba / Milk Tea Drink 🧋
        Point boba = new Point(astroCenter.X + (18 * scale), astroCenter.Y - (4 * scale) - floatBob);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(220, 255, 210, 175)), new Pen(new SolidColorBrush(Colors.White), 1.2), new Rect(boba.X, boba.Y, 9 * scale, 13 * scale), 2, 2);
        // Straw
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 105, 150)), 1.8 * scale), new Point(boba.X + (4 * scale), boba.Y), new Point(boba.X + (7 * scale), boba.Y - (6 * scale)));

        string text = isMini ? "☕ Zero-G Rest" : "🧋 Zero-G Chill Lounge • Take a sip! ✨";
        var ft = CreateText(text, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(160, 230, 255)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.5), y - (26 * scale)));
    }

    #endregion

    #region 🐱 Scene 3: Ultra-Kawaii Focus Kitty

    private void RenderCatScene(DrawingContext dc, double w, double h)
    {
        bool isMini = IsMiniMode;
        double floorY = h * (isMini ? 0.74 : 0.76);
        double warmth = ProgressFraction;

        Color wallTop = LerpColor(Color.FromRgb(246, 238, 252), Color.FromRgb(255, 236, 220), warmth);
        Color wallBottom = LerpColor(Color.FromRgb(236, 224, 242), Color.FromRgb(250, 218, 202), warmth);
        var wallBrush = new LinearGradientBrush(wallTop, wallBottom, new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(wallBrush, null, new Rect(0, 0, w, floorY));

        var ambientGlow = new RadialGradientBrush(
            Color.FromArgb((byte)(isMini ? 70 : 105), 255, (byte)(225 + (20 * warmth)), (byte)(205 + (20 * warmth))),
            Color.FromArgb(0, 255, 230, 215));
        dc.DrawEllipse(ambientGlow, null, new Point(w * 0.28, floorY * 0.16), w * 0.34, floorY * 0.28);

        double panelY = floorY * (isMini ? 0.70 : 0.66);
        dc.DrawRectangle(
            new LinearGradientBrush(Color.FromArgb(32, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), new Point(0, 0), new Point(0, 1)),
            null,
            new Rect(0, 0, w, panelY));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(110, 184, 152, 144)), 1.4), new Point(0, panelY), new Point(w, panelY));

        // Window on Left
        Rect windowRect = new Rect(isMini ? 8 : 14, isMini ? 8 : 14, w * (isMini ? 0.26 : 0.28), floorY * (isMini ? 0.48 : 0.54));
        DrawCatWindowView(dc, windowRect, warmth, isMini);
        DrawCatWindowFrame(dc, windowRect, isMini);

        // Cat Tree in background on left near window
        double treeX = isMini ? w * 0.14 : w * 0.15;
        DrawCatTreeFurniture(dc, treeX, floorY, isMini, IsRestPhase || IsGoalReached);

        if (!isMini)
        {
            DrawCatWallFrames(dc, w, floorY);
            DrawCatBookshelf(dc, w * 0.48, panelY - 32, w * 0.22, floorY * 0.22);
            DrawCatHangingPlant(dc, w * 0.42, 6, 1.0);
            DrawCatHangingPlant(dc, w * 0.78, 8, 0.84);
            DrawCatLampGlow(dc, w * 0.92, panelY - 28, 1.0);
        }
        else
        {
            DrawCatHangingPlant(dc, w * 0.75, 6, 0.64);
        }

        DrawCatPottedPlant(dc, isMini ? w * 0.34 : w * 0.36, floorY, isMini ? 0.7 : 1.0);

        // Floor
        var floorBrush = new LinearGradientBrush(
            LerpColor(Color.FromRgb(224, 178, 140), Color.FromRgb(205, 145, 110), warmth * 0.6),
            LerpColor(Color.FromRgb(184, 128, 96), Color.FromRgb(156, 102, 74), warmth * 0.6),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(floorBrush, null, new Rect(0, floorY, w, h - floorY));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(136, 88, 62)), isMini ? 1.2 : 2), new Point(0, floorY), new Point(w, floorY));

        if (!isMini)
        {
            for (double plankX = 0; plankX < w; plankX += 30)
            {
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(46, 120, 76, 52)), 0.8), new Point(plankX, floorY + 1), new Point(plankX, h));
            }
        }

        // Cozy Rug in middle
        double rugCx = isMini ? w * 0.54 : w * 0.56;
        double rugCy = floorY + ((h - floorY) * 0.46);
        double rugRx = w * (isMini ? 0.20 : 0.22);
        double rugRy = (h - floorY) * (isMini ? 0.30 : 0.35);
        var rugBrush = new RadialGradientBrush(Color.FromRgb(255, 232, 240), Color.FromRgb(232, 186, 210));
        dc.DrawEllipse(rugBrush, new Pen(new SolidColorBrush(Color.FromRgb(212, 154, 184)), 1.2), new Point(rugCx, rugCy), rugRx, rugRy);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(130, 255, 248, 252)), isMini ? 0.8 : 1.2), new Point(rugCx, rugCy), rugRx * 0.74, rugRy * 0.74);

        // Feeding Station on Right
        double feedingX = w - (isMini ? 42 : 68);
        double feedingY = floorY - (isMini ? 12 : 18);
        DrawCuteFishBowl(dc, feedingX, feedingY, isMini);

        // Toys & Dust Motes
        DrawCatToyArea(dc, rugCx, floorY, isMini, IsTracking);
        DrawCatDustMotes(dc, w, floorY, isMini);

        if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawCatCelebrationPennants(dc, w, floorY, isMini);
        }
        else if (IsRestPhase && !isMini)
        {
            var restGlow = new RadialGradientBrush(Color.FromArgb(55, 190, 180, 255), Color.FromArgb(0, 190, 180, 255));
            dc.DrawEllipse(restGlow, null, new Point(windowRect.X + (windowRect.Width * 0.55), windowRect.Y + (windowRect.Height * 0.32)), 48, 38);
        }

        // Cat Position along floor - starts further back in the room (near left edge)
        double catStartX = isMini ? 22 : 32;
        double catEndX = feedingX - (isMini ? 24 : 34);
        double catX = catStartX + (ProgressFraction * Math.Max(0, catEndX - catStartX));

        if (IsRestPhase)
        {
            double perchY = floorY - (isMini ? 44 : 68);
            DrawCuteSleepingKitty(dc, treeX, perchY, isMini);
        }
        else if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawCuteFeastingKitty(dc, feedingX - (isMini ? 6 : 10), floorY, isMini);
        }
        else
        {
            DrawCuteWalkingKittyWithYarn(dc, catX, floorY, isMini, IsTracking);
        }

        string sceneText = (IsGoalReached || ProgressFraction >= 0.999)
            ? (isMini ? "🐾 100%" : "🐾 Goal reached • deluxe snack time")
            : IsRestPhase
                ? (isMini ? "💤 Rest" : "💤 Rest phase • sleepy perch break")
                : IsTracking
                    ? (isMini ? $"🐱 {(int)(ProgressFraction * 100)}%" : $"🐱 Focus kitty patrol • {(int)(ProgressFraction * 100)}%")
                    : (isMini ? "🐱 Ready" : "🐱 Cozy room ready for a focus sprint");

        var badgeBrush = new SolidColorBrush(Color.FromArgb(210, 255, 248, 244));
        var badgeBorder = new Pen(new SolidColorBrush(Color.FromArgb(120, 206, 162, 150)), 1.0);
        var ft = CreateText(sceneText, isMini ? 8.5 : 10.5, new SolidColorBrush(Color.FromRgb(108, 68, 66)), FontWeights.SemiBold);
        double badgeW = ft.Width + (isMini ? 10 : 16);
        double badgeH = ft.Height + (isMini ? 4 : 6);
        double badgeX = w - badgeW - (isMini ? 6 : 12);
        double badgeY = isMini ? 4 : 8;
        dc.DrawRoundedRectangle(badgeBrush, badgeBorder, new Rect(badgeX, badgeY, badgeW, badgeH), 5, 5);
        dc.DrawText(ft, new Point(badgeX + ((badgeW - ft.Width) * 0.5), badgeY + ((badgeH - ft.Height) * 0.5)));
    }

    private void DrawCatWindowView(DrawingContext dc, Rect rect, double warmth, bool isMini)
    {
        var skyBrush = new LinearGradientBrush(
            IsRestPhase
                ? LerpColor(Color.FromRgb(120, 150, 205), Color.FromRgb(182, 150, 225), warmth * 0.6)
                : LerpColor(Color.FromRgb(172, 215, 250), Color.FromRgb(255, 208, 168), warmth),
            IsRestPhase
                ? LerpColor(Color.FromRgb(212, 210, 245), Color.FromRgb(242, 218, 250), warmth * 0.6)
                : LerpColor(Color.FromRgb(240, 245, 255), Color.FromRgb(255, 234, 205), warmth),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(skyBrush, null, rect);

        dc.PushClip(new RectangleGeometry(rect, 8, 8));

        var glowBrush = new RadialGradientBrush(
            Color.FromArgb((byte)(IsRestPhase ? 120 : 145), IsRestPhase ? (byte)210 : (byte)255, IsRestPhase ? (byte)225 : (byte)230, IsRestPhase ? (byte)255 : (byte)190),
            Color.FromArgb(0, 255, 240, 210));
        dc.DrawEllipse(glowBrush, null, new Point(rect.Left + (rect.Width * 0.78), rect.Top + (rect.Height * 0.24)), rect.Width * 0.35, rect.Height * 0.24);

        var hillBack = new PathGeometry();
        var backFig = new PathFigure { StartPoint = new Point(rect.Left - 10, rect.Bottom - (rect.Height * 0.24)) };
        backFig.Segments.Add(new BezierSegment(
            new Point(rect.Left + (rect.Width * 0.16), rect.Bottom - (rect.Height * 0.38)),
            new Point(rect.Left + (rect.Width * 0.34), rect.Bottom - (rect.Height * 0.18)),
            new Point(rect.Left + (rect.Width * 0.56), rect.Bottom - (rect.Height * 0.30)), true));
        backFig.Segments.Add(new BezierSegment(
            new Point(rect.Left + (rect.Width * 0.72), rect.Bottom - (rect.Height * 0.42)),
            new Point(rect.Right - (rect.Width * 0.14), rect.Bottom - (rect.Height * 0.18)),
            new Point(rect.Right + 12, rect.Bottom - (rect.Height * 0.28)), true));
        backFig.Segments.Add(new LineSegment(new Point(rect.Right + 12, rect.Bottom + 10), true));
        backFig.Segments.Add(new LineSegment(new Point(rect.Left - 10, rect.Bottom + 10), true));
        backFig.IsClosed = true;
        hillBack.Figures.Add(backFig);
        dc.DrawGeometry(new SolidColorBrush(IsRestPhase ? Color.FromRgb(130, 148, 190) : Color.FromRgb(150, 188, 170)), null, hillBack);

        var hillFront = new PathGeometry();
        var frontFig = new PathFigure { StartPoint = new Point(rect.Left - 10, rect.Bottom - (rect.Height * 0.14)) };
        frontFig.Segments.Add(new BezierSegment(
            new Point(rect.Left + (rect.Width * 0.18), rect.Bottom - (rect.Height * 0.24)),
            new Point(rect.Left + (rect.Width * 0.38), rect.Bottom - (rect.Height * 0.06)),
            new Point(rect.Left + (rect.Width * 0.52), rect.Bottom - (rect.Height * 0.18)), true));
        frontFig.Segments.Add(new BezierSegment(
            new Point(rect.Left + (rect.Width * 0.72), rect.Bottom - (rect.Height * 0.32)),
            new Point(rect.Right - (rect.Width * 0.06), rect.Bottom - (rect.Height * 0.02)),
            new Point(rect.Right + 10, rect.Bottom - (rect.Height * 0.12)), true));
        frontFig.Segments.Add(new LineSegment(new Point(rect.Right + 10, rect.Bottom + 10), true));
        frontFig.Segments.Add(new LineSegment(new Point(rect.Left - 10, rect.Bottom + 10), true));
        frontFig.IsClosed = true;
        hillFront.Figures.Add(frontFig);
        dc.DrawGeometry(new SolidColorBrush(IsRestPhase ? Color.FromRgb(108, 128, 168) : Color.FromRgb(114, 164, 124)), null, hillFront);

        int cloudCount = isMini ? 2 : 4;
        for (int i = 0; i < cloudCount; i++)
        {
            double cx = rect.Left - 24 + (((_frameTick * (0.22 + (i * 0.04))) + (i * rect.Width * 0.36)) % (rect.Width + 52));
            double cy = rect.Top + (rect.Height * (0.17 + (i * 0.12))) + (Math.Sin((_frameTick * 0.03) + i) * 2);
            double rx = (isMini ? 12 : 18) + (i * 2);
            double ry = rx * 0.52;
            var cloudBrush = new SolidColorBrush(Color.FromArgb((byte)(isMini ? 140 : 170), 255, 255, 255));
            dc.DrawEllipse(cloudBrush, null, new Point(cx, cy), rx, ry);
            dc.DrawEllipse(cloudBrush, null, new Point(cx + (rx * 0.45), cy - (ry * 0.25)), rx * 0.72, ry * 0.78);
            dc.DrawEllipse(cloudBrush, null, new Point(cx - (rx * 0.48), cy + (ry * 0.04)), rx * 0.62, ry * 0.68);
        }

        if (!isMini)
        {
            for (int i = 0; i < 10; i++)
            {
                double sparkleX = rect.Left + ((i * 27 + (_frameTick * (i % 2 == 0 ? 1.3 : 0.8))) % Math.Max(24, rect.Width + 20));
                double sparkleY = rect.Top + 10 + ((i * 19) % (int)Math.Max(12, rect.Height * 0.58));
                double size = 1.2 + ((i % 3) * 0.7);
                var sparkleBrush = new SolidColorBrush(Color.FromArgb((byte)(55 + ((i % 3) * 20)), 255, 245, 220));
                dc.DrawEllipse(sparkleBrush, null, new Point(sparkleX, sparkleY), size, size);
            }
        }

        var curtainBrush = new LinearGradientBrush(Color.FromArgb(140, 255, 250, 250), Color.FromArgb(15, 255, 250, 250), new Point(0, 0), new Point(1, 0));
        dc.DrawRectangle(curtainBrush, null, new Rect(rect.Left, rect.Top, rect.Width * 0.10, rect.Height));
        dc.DrawRectangle(curtainBrush, null, new Rect(rect.Right - (rect.Width * 0.12), rect.Top, rect.Width * 0.12, rect.Height));

        dc.Pop();
    }

    private void DrawCatWindowFrame(DrawingContext dc, Rect rect, bool isMini)
    {
        var frameBrush = new LinearGradientBrush(Color.FromRgb(255, 248, 242), Color.FromRgb(220, 198, 188), new Point(0, 0), new Point(1, 1));
        var framePen = new Pen(new SolidColorBrush(Color.FromRgb(184, 150, 142)), isMini ? 1.5 : 2.4);
        dc.DrawRoundedRectangle(frameBrush, framePen, rect, 8, 8);
        dc.DrawLine(framePen, new Point(rect.Left + (rect.Width * 0.5), rect.Top + 2), new Point(rect.Left + (rect.Width * 0.5), rect.Bottom - 2));
        dc.DrawLine(framePen, new Point(rect.Left + 2, rect.Top + (rect.Height * 0.54)), new Point(rect.Right - 2, rect.Top + (rect.Height * 0.54)));
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)), null, new Rect(rect.Left + (rect.Width * 0.52), rect.Top + 2, rect.Width * 0.16, rect.Height - 4));

        if (!isMini)
        {
            double sillY = rect.Bottom + 4;
            dc.DrawRoundedRectangle(
                new LinearGradientBrush(Color.FromRgb(214, 164, 128), Color.FromRgb(168, 112, 82), new Point(0, 0), new Point(0, 1)),
                new Pen(new SolidColorBrush(Color.FromRgb(126, 82, 58)), 1),
                new Rect(rect.Left - 4, sillY, rect.Width + 8, 10),
                3,
                3);
        }
    }

    private void DrawCatWallFrames(DrawingContext dc, double w, double floorY)
    {
        var frameFill = new SolidColorBrush(Color.FromArgb(180, 255, 248, 244));
        var framePen = new Pen(new SolidColorBrush(Color.FromRgb(186, 154, 148)), 1.1);

        Rect fishFrame = new Rect(w * 0.56, floorY * 0.18, 38, 30);
        dc.DrawRoundedRectangle(frameFill, framePen, fishFrame, 4, 4);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(112, 196, 255)), null, new Point(fishFrame.X + 18, fishFrame.Y + 15), 10, 5.5);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 120)), null, new Point(fishFrame.X + 22, fishFrame.Y + 13), 3, 2);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(40, 40, 40)), null, new Point(fishFrame.X + 25, fishFrame.Y + 14), 0.8, 0.8);

        Rect pawFrame = new Rect(w * 0.68, floorY * 0.15, 34, 34);
        dc.DrawRoundedRectangle(frameFill, framePen, pawFrame, 4, 4);
        DrawPawPrint(dc, pawFrame.X + 17, pawFrame.Y + 18, 0.8, new SolidColorBrush(Color.FromRgb(220, 150, 182)));
    }

    private void DrawCatBookshelf(DrawingContext dc, double x, double y, double w, double h)
    {
        var shelfBrush = new LinearGradientBrush(Color.FromRgb(188, 138, 102), Color.FromRgb(142, 92, 62), new Point(0, 0), new Point(0, 1));
        var shelfPen = new Pen(new SolidColorBrush(Color.FromRgb(112, 72, 48)), 1.0);
        dc.DrawRoundedRectangle(shelfBrush, shelfPen, new Rect(x, y, w, h), 4, 4);
        dc.DrawLine(shelfPen, new Point(x + 4, y + (h * 0.48)), new Point(x + w - 4, y + (h * 0.48)));

        double[] widths = { 7, 6, 8, 5, 9, 7 };
        double bx = x + 6;
        for (int i = 0; i < widths.Length; i++)
        {
            double bookH = 18 + ((i % 3) * 4);
            if (bx + widths[i] > x + w - 6) break;
            var bookBrush = new SolidColorBrush(i % 3 == 0 ? Color.FromRgb(252, 174, 184) : (i % 3 == 1 ? Color.FromRgb(152, 214, 198) : Color.FromRgb(255, 221, 142)));
            dc.DrawRoundedRectangle(bookBrush, new Pen(new SolidColorBrush(Color.FromArgb(70, 100, 70, 60)), 0.7), new Rect(bx, y + (h * 0.48) - bookH, widths[i], bookH), 1.5, 1.5);
            bx += widths[i] + 3;
        }

        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 238, 182)), null, new Point(x + (w * 0.72), y + (h * 0.24)), 5, 5);
        DrawSparkle(dc, x + (w * 0.84), y + (h * 0.18), 2.0, new SolidColorBrush(Color.FromRgb(255, 245, 205)));

        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(104, 168, 92)), null, new Point(x + (w * 0.22), y + (h * 0.70)), 8, 6);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(126, 194, 112)), null, new Point(x + (w * 0.18), y + (h * 0.62)), 7, 5);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(214, 170, 132)), null, new Rect(x + (w * 0.12), y + (h * 0.74), 12, 7), 2, 2);
    }

    private void DrawCatHangingPlant(DrawingContext dc, double x, double topY, double scale)
    {
        var stringPen = new Pen(new SolidColorBrush(Color.FromRgb(168, 140, 130)), 0.9 * scale);
        dc.DrawLine(stringPen, new Point(x, topY), new Point(x - (6 * scale), topY + (12 * scale)));
        dc.DrawLine(stringPen, new Point(x, topY), new Point(x + (6 * scale), topY + (12 * scale)));
        dc.DrawRoundedRectangle(
            new LinearGradientBrush(Color.FromRgb(236, 182, 146), Color.FromRgb(204, 142, 112), new Point(0, 0), new Point(0, 1)),
            new Pen(new SolidColorBrush(Color.FromRgb(162, 110, 86)), 0.8 * scale),
            new Rect(x - (7 * scale), topY + (12 * scale), 14 * scale, 8 * scale),
            3 * scale,
            3 * scale);

        for (int i = -2; i <= 2; i++)
        {
            double lx = x + (i * 3.2 * scale);
            double len = (10 + ((i + 2) % 3) * 4) * scale;
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(84, 148, 82)), 1.1 * scale), new Point(lx, topY + (18 * scale)), new Point(lx + (Math.Sin((_frameTick * 0.05) + i) * 2.0 * scale), topY + (18 * scale) + len));
            DrawLeaf(dc, lx - (1.5 * scale), topY + (22 * scale) + (len * 0.25), 3.2 * scale, -0.5, new SolidColorBrush(Color.FromRgb(120, 196, 112)));
            DrawLeaf(dc, lx + (1.5 * scale), topY + (26 * scale) + (len * 0.55), 3.6 * scale, 0.4, new SolidColorBrush(Color.FromRgb(94, 170, 98)));
        }
    }

    private void DrawCatLampGlow(DrawingContext dc, double x, double y, double scale)
    {
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(214, 190, 164)), new Pen(new SolidColorBrush(Color.FromRgb(170, 140, 122)), 0.9), new Rect(x - (11 * scale), y, 22 * scale, 16 * scale), 6 * scale, 6 * scale);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(182, 152, 130)), null, new Rect(x - (1.8 * scale), y + (16 * scale), 3.6 * scale, 14 * scale));
        dc.DrawEllipse(new RadialGradientBrush(Color.FromArgb(120, 255, 225, 176), Color.FromArgb(0, 255, 225, 176)), null, new Point(x, y + (10 * scale)), 24 * scale, 20 * scale);
    }

    private void DrawCatPottedPlant(DrawingContext dc, double x, double floorY, double scale)
    {
        double potY = floorY - (10 * scale);
        dc.DrawRoundedRectangle(
            new LinearGradientBrush(Color.FromRgb(222, 172, 136), Color.FromRgb(188, 124, 98), new Point(0, 0), new Point(0, 1)),
            new Pen(new SolidColorBrush(Color.FromRgb(156, 100, 74)), 0.8 * scale),
            new Rect(x - (9 * scale), potY, 18 * scale, 11 * scale),
            3 * scale,
            3 * scale);
        for (int i = 0; i < 5; i++)
        {
            double leafX = x + ((i - 2) * 3.2 * scale);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(86, 148, 80)), 1.0 * scale), new Point(x, potY + 1), new Point(leafX, potY - ((10 + ((i + 1) % 3) * 4) * scale)));
            DrawLeaf(dc, leafX, potY - ((12 + ((i + 2) % 3) * 4) * scale), 4.0 * scale, i % 2 == 0 ? -0.5 : 0.5, new SolidColorBrush(i % 2 == 0 ? Color.FromRgb(118, 194, 114) : Color.FromRgb(92, 168, 96)));
        }
    }

    private void DrawCatTreeFurniture(DrawingContext dc, double x, double floorY, bool isMini, bool showCozyGlow)
    {
        double scale = isMini ? 0.72 : 1.0;
        double baseW = 46 * scale;
        double baseH = 8 * scale;
        double postW = 8 * scale;
        double condoW = 32 * scale;
        double condoH = 24 * scale;
        double condoTopY = floorY - (42 * scale);
        double topPerchY = floorY - (68 * scale);

        // Shadow & Base
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(55, 35, 22, 18)), null, new Point(x, floorY + (2.5 * scale)), baseW * 0.55, 4.5 * scale);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(212, 176, 148)), new Pen(new SolidColorBrush(Color.FromRgb(166, 126, 98)), 1.0 * scale), new Rect(x - (baseW * 0.5), floorY - baseH, baseW, baseH), 4 * scale, 4 * scale);

        // Sisal Posts
        var sisalBrush = new LinearGradientBrush(Color.FromRgb(218, 192, 150), Color.FromRgb(178, 146, 106), new Point(0, 0), new Point(1, 0));
        dc.DrawRoundedRectangle(sisalBrush, new Pen(new SolidColorBrush(Color.FromRgb(142, 116, 80)), 0.8 * scale), new Rect(x - (14 * scale), floorY - (40 * scale), postW, 32 * scale), 3 * scale, 3 * scale);
        dc.DrawRoundedRectangle(sisalBrush, new Pen(new SolidColorBrush(Color.FromRgb(142, 116, 80)), 0.8 * scale), new Rect(x + (6 * scale), floorY - (66 * scale), postW, 58 * scale), 3 * scale, 3 * scale);

        for (int i = 0; i < (isMini ? 3 : 5); i++)
        {
            double sy = floorY - (36 * scale) + (i * (6 * scale));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(140, 126, 94, 60)), 1.0 * scale), new Point(x - (14 * scale), sy), new Point(x - (6 * scale), sy));
        }

        // Condo Box
        var condoBrush = new LinearGradientBrush(Color.FromRgb(250, 218, 202), Color.FromRgb(230, 186, 166), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(condoBrush, new Pen(new SolidColorBrush(Color.FromRgb(196, 146, 126)), 1.0 * scale), new Rect(x - (condoW * 0.5), condoTopY, condoW, condoH), 5 * scale, 5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(170, 120, 96)), null, new Point(x, condoTopY + (condoH * 0.58)), 7 * scale, 7 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(150, 94, 62, 48)), null, new Point(x, condoTopY + (condoH * 0.58)), 4.2 * scale, 4.2 * scale);

        // Perch Platform & Cushion
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(242, 210, 192)), new Pen(new SolidColorBrush(Color.FromRgb(194, 150, 126)), 0.8 * scale), new Rect(x - (18 * scale), floorY - (42 * scale), 36 * scale, 5 * scale), 2.5 * scale, 2.5 * scale);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(248, 220, 212)), new Pen(new SolidColorBrush(Color.FromRgb(194, 150, 126)), 0.8 * scale), new Rect(x - (22 * scale), topPerchY, 44 * scale, 6 * scale), 3 * scale, 3 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(240, 200, 222)), null, new Point(x, topPerchY + (3.0 * scale)), 16 * scale, 4.0 * scale);

        // Swinging Pom-Pom Toy
        double pomSwing = Math.Sin(_frameTick * 0.11) * (isMini ? 2.0 : 4.0);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(176, 142, 138)), 1.0 * scale), new Point(x + (12 * scale), topPerchY + (6 * scale)), new Point(x + (12 * scale) + pomSwing, topPerchY + (18 * scale)));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 154, 192)), null, new Point(x + (12 * scale) + pomSwing, topPerchY + (22 * scale)), 3.5 * scale, 3.5 * scale);

        if (showCozyGlow && !isMini)
        {
            var cozyGlow = new RadialGradientBrush(Color.FromArgb(70, 255, 214, 176), Color.FromArgb(0, 255, 214, 176));
            dc.DrawEllipse(cozyGlow, null, new Point(x, topPerchY + 6), 34, 20);
        }
    }

    private void DrawCatToyArea(DrawingContext dc, double rugCx, double floorY, bool isMini, bool isTracking)
    {
        double scale = isMini ? 0.72 : 1.0;
        double mouseX = rugCx - (isMini ? 16 : 28);
        double mouseY = floorY - (isMini ? 4 : 6);

        // Cute Little Gray Felt Mouse Toy
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(194, 196, 204)), null, new Point(mouseX, mouseY), 5.5 * scale, 3.6 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(238, 186, 198)), null, new Point(mouseX - (3.6 * scale), mouseY - (1.6 * scale)), 1.6 * scale, 1.6 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(238, 186, 198)), null, new Point(mouseX - (1.0 * scale), mouseY - (2.6 * scale)), 1.6 * scale, 1.6 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(50, 40, 44)), null, new Point(mouseX - (4.5 * scale), mouseY + (0.5 * scale)), 0.7 * scale, 0.7 * scale);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(160, 142, 150)), 1.0 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, new Point(mouseX + (5.5 * scale), mouseY), new Point(mouseX + (11 * scale), mouseY - (2 * scale)));

        // Feather Wand Toy on side
        double wandBaseX = rugCx + (isMini ? 10 : 20);
        double wandBaseY = floorY - (isMini ? 2 : 3);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(160, 116, 90)), 1.8 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, new Point(wandBaseX, wandBaseY), new Point(wandBaseX + (18 * scale), wandBaseY - (10 * scale)));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(214, 180, 162)), 0.9 * scale), new Point(wandBaseX + (18 * scale), wandBaseY - (10 * scale)), new Point(wandBaseX + (23 * scale), wandBaseY - (15 * scale) - (isTracking ? Math.Sin(_frameTick * 0.25) * 2.5 * scale : 0)));
        DrawLeaf(dc, wandBaseX + (24 * scale), wandBaseY - (15 * scale) - (isTracking ? Math.Sin(_frameTick * 0.25) * 2.5 * scale : 0), 4.5 * scale, -0.2, new SolidColorBrush(Color.FromRgb(128, 218, 202)));
        DrawLeaf(dc, wandBaseX + (27 * scale), wandBaseY - (13 * scale) - (isTracking ? Math.Sin(_frameTick * 0.25) * 2.5 * scale : 0), 3.8 * scale, 0.7, new SolidColorBrush(Color.FromRgb(255, 174, 198)));
    }

    private void DrawCatDustMotes(DrawingContext dc, double w, double floorY, bool isMini)
    {
        int moteCount = isMini ? 6 : 14;
        for (int i = 0; i < moteCount; i++)
        {
            double x = ((i * 37) + (_frameTick * (0.22 + ((i % 3) * 0.04)))) % Math.Max(24, w + 20);
            double y = 12 + ((i * 17) % (int)Math.Max(18, floorY - 18)) + (Math.Sin((_frameTick * 0.05) + i) * (isMini ? 1.5 : 3.0));
            double size = (i % 4 == 0) ? (isMini ? 1.4 : 2.0) : (isMini ? 0.8 : 1.2);
            var moteBrush = new SolidColorBrush(Color.FromArgb((byte)(i % 4 == 0 ? 60 : 36), 255, 250, 235));
            dc.DrawEllipse(moteBrush, null, new Point(x, y), size, size);

            if (!isMini && i % 5 == 0)
            {
                DrawSparkle(dc, x + 4, y - 2, 1.8, new SolidColorBrush(Color.FromArgb(70, 255, 244, 220)));
            }
        }
    }

    private void DrawCatCelebrationPennants(DrawingContext dc, double w, double floorY, bool isMini)
    {
        if (isMini) return;

        var stringPen = new Pen(new SolidColorBrush(Color.FromArgb(110, 164, 120, 112)), 0.9);
        double leftX = w * 0.38;
        double rightX = w - 18;
        double topY = floorY * 0.12;
        dc.DrawLine(stringPen, new Point(leftX, topY), new Point(rightX, topY + 4));

        Color[] pennantColors =
        {
            Color.FromRgb(255, 174, 188),
            Color.FromRgb(255, 224, 138),
            Color.FromRgb(150, 218, 198),
            Color.FromRgb(184, 170, 246)
        };

        int idx = 0;
        for (double px = leftX + 10; px < rightX - 6; px += 20)
        {
            double py = topY + 2 + (Math.Sin((_frameTick * 0.06) + idx) * 1.5);
            var pennant = new PathGeometry();
            var fig = new PathFigure { StartPoint = new Point(px - 5, py) };
            fig.Segments.Add(new LineSegment(new Point(px + 5, py), true));
            fig.Segments.Add(new LineSegment(new Point(px, py + 10), true));
            fig.IsClosed = true;
            pennant.Figures.Add(fig);
            dc.DrawGeometry(new SolidColorBrush(pennantColors[idx % pennantColors.Length]), null, pennant);
            idx++;
        }
    }

    private void DrawCuteFishBowl(DrawingContext dc, double x, double y, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;

        // Feeding Mat
        dc.DrawRoundedRectangle(
            new LinearGradientBrush(Color.FromArgb(190, 255, 238, 232), Color.FromArgb(100, 255, 238, 232), new Point(0, 0), new Point(0, 1)),
            new Pen(new SolidColorBrush(Color.FromArgb(110, 218, 168, 156)), 0.8 * scale),
            new Rect(x - (10 * scale), y + (12 * scale), 38 * scale, 9 * scale),
            4 * scale,
            4 * scale);

        // Ceramic Cat-Ear Food Bowl
        var foodBrush = new LinearGradientBrush(Color.FromRgb(255, 232, 112), Color.FromRgb(242, 182, 52), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(foodBrush, new Pen(new SolidColorBrush(Color.FromRgb(190, 132, 28)), 1.0 * scale), new Rect(x, y + (5 * scale), 17 * scale, 11 * scale), 4.5 * scale, 4.5 * scale);

        // Golden Fish Treat
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(128, 208, 255)), null, new Point(x + (8.5 * scale), y + (4.5 * scale)), 5.5 * scale, 3.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(40, 52, 62)), null, new Point(x + (11.5 * scale), y + (4.0 * scale)), 0.8 * scale, 0.8 * scale);

        // Water Dish beside it
        dc.DrawRoundedRectangle(
            new LinearGradientBrush(Color.FromRgb(215, 238, 250), Color.FromRgb(160, 202, 236), new Point(0, 0), new Point(0, 1)),
            new Pen(new SolidColorBrush(Color.FromRgb(130, 172, 206)), 0.9 * scale),
            new Rect(x + (19 * scale), y + (7 * scale), 13 * scale, 9 * scale),
            4 * scale,
            4 * scale);
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), null, new Rect(x + (21 * scale), y + (9 * scale), 2.8 * scale, 5 * scale));

        DrawSparkle(dc, x + (9 * scale), y - (2 * scale), 2.8 * scale, new SolidColorBrush(Color.FromRgb(255, 236, 130)));
    }

    private void DrawCuteWalkingKittyWithYarn(DrawingContext dc, double x, double floorY, bool isMini, bool isWalking)
    {
        double scale = isMini ? 0.72 : 1.0;
        double stride = isWalking ? Math.Sin(_frameTick * 0.32) * (5.0 * scale) : 0;
        double bob = isWalking ? Math.Abs(Math.Sin(_frameTick * 0.32)) * (2.2 * scale) : 0;
        double catY = floorY - (19 * scale) - bob;

        Color baseCol = Color.FromRgb(250, 166, 80);
        Color shadowCol = Color.FromRgb(216, 118, 42);
        Color highlightCol = Color.FromRgb(255, 215, 155);
        Color creamCol = Color.FromRgb(255, 248, 240);
        Color pinkCol = Color.FromRgb(255, 170, 188);

        // 1. Yarn Ball ahead of cat
        double yarnBounce = isWalking ? Math.Abs(Math.Sin(_frameTick * 0.36)) * (2.0 * scale) : 0;
        double yarnX = x + (25 * scale);
        double yarnY = floorY - (6.5 * scale) - yarnBounce;

        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(65, 80, 50, 42)), null, new Point(yarnX, floorY + (1.2 * scale)), 6.5 * scale, 2.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 110, 168)), new Pen(new SolidColorBrush(Color.FromRgb(220, 72, 136)), 0.8 * scale), new Point(yarnX, yarnY), 6.5 * scale, 6.5 * scale);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 184, 212)), 1.1 * scale), new Point(yarnX - (3.5 * scale), yarnY - (2 * scale)), new Point(yarnX + (3.5 * scale), yarnY + (3 * scale)));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 184, 212)), 1.1 * scale), new Point(yarnX - (4 * scale), yarnY + (1 * scale)), new Point(yarnX + (3 * scale), yarnY - (3.5 * scale)));

        // Yarn thread connecting to kitty's front paw
        var threadGeom = new PathGeometry();
        var threadFig = new PathFigure { StartPoint = new Point(x + (12 * scale), catY + (5 * scale)) };
        threadFig.Segments.Add(new QuadraticBezierSegment(new Point(x + (18 * scale), floorY + (1 * scale)), new Point(yarnX - (4 * scale), yarnY + (1 * scale)), true));
        threadGeom.Figures.Add(threadFig);
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(255, 150, 190)), 1.0 * scale), threadGeom);

        // 2. Ground Shadow under Kitty
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(50, 40, 25, 15)), null, new Point(x, floorY + (1.0 * scale)), 20 * scale, 3.8 * scale);

        // 3. Back Legs (Left-Rear & Left-Front in shadow layer)
        var backLegPen = new Pen(new SolidColorBrush(shadowCol), 3.6 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        Point backHip = new Point(x - (8 * scale), catY + (5 * scale));
        Point backFoot = new Point(x - (8 * scale) - (stride * 0.7), floorY - (isWalking && stride > 0 ? (stride * 0.3) : 0));
        dc.DrawLine(backLegPen, backHip, backFoot);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(backFoot.X, backFoot.Y), 2.2 * scale, 1.4 * scale);

        Point backShoulder = new Point(x + (6 * scale), catY + (5 * scale));
        Point frontBackFoot = new Point(x + (6 * scale) + (stride * 0.7), floorY - (isWalking && stride < 0 ? (-stride * 0.3) : 0));
        dc.DrawLine(backLegPen, backShoulder, frontBackFoot);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(frontBackFoot.X, frontBackFoot.Y), 2.2 * scale, 1.4 * scale);

        // 4. Fluffy S-curved Tail
        double tailWiggle = Math.Sin(_frameTick * 0.18) * (4.0 * scale);
        Point tailStart = new Point(x - (13 * scale), catY + (1.5 * scale));
        Point tailMid = new Point(x - (22 * scale) + tailWiggle, catY - (8 * scale));
        Point tailEnd = new Point(x - (16 * scale) + (tailWiggle * 1.3), catY - (17 * scale));
        var tailGeom = new PathGeometry();
        var tailFig = new PathFigure { StartPoint = tailStart };
        tailFig.Segments.Add(new QuadraticBezierSegment(tailMid, tailEnd, true));
        tailGeom.Figures.Add(tailFig);
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(shadowCol), 5.0 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, tailGeom);
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(baseCol), 4.0 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, tailGeom);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, tailEnd, 2.0 * scale, 2.0 * scale);

        // 5. Chubby Body
        Point bodyCenter = new Point(x - (2 * scale), catY);
        dc.DrawEllipse(new SolidColorBrush(shadowCol), null, new Point(bodyCenter.X - (1.0 * scale), bodyCenter.Y + (1.2 * scale)), 14.5 * scale, 10.0 * scale);
        dc.DrawEllipse(new SolidColorBrush(baseCol), null, bodyCenter, 14.5 * scale, 10.0 * scale);
        dc.DrawEllipse(new SolidColorBrush(highlightCol), null, new Point(bodyCenter.X + (1 * scale), bodyCenter.Y - (3.2 * scale)), 8 * scale, 3.8 * scale);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(bodyCenter.X + (3.5 * scale), bodyCenter.Y + (2.0 * scale)), 9.0 * scale, 5.5 * scale);

        // Tabby Stripes on Back
        for (int s = 0; s < 3; s++)
        {
            double sx = bodyCenter.X - (6 * scale) + (s * 5.5 * scale);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(130, 180, 90, 40)), 1.5 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, new Point(sx, catY - (5 * scale)), new Point(sx + (1.8 * scale), catY + (2 * scale)));
        }

        // 6. Front Legs (Right-Rear & Right-Front in foreground)
        var frontLegPen = new Pen(new SolidColorBrush(baseCol), 3.6 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        Point frontHip = new Point(x - (4 * scale), catY + (6 * scale));
        Point rearFrontFoot = new Point(x - (4 * scale) + (stride * 0.7), floorY - (isWalking && stride < 0 ? (-stride * 0.3) : 0));
        dc.DrawLine(frontLegPen, frontHip, rearFrontFoot);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(rearFrontFoot.X, rearFrontFoot.Y), 2.2 * scale, 1.4 * scale);

        Point frontShoulder = new Point(x + (10 * scale), catY + (5 * scale));
        Point frontFrontFoot = new Point(x + (10 * scale) - (stride * 0.7), floorY - (isWalking && stride > 0 ? (stride * 0.3) : 0));
        dc.DrawLine(frontLegPen, frontShoulder, frontFrontFoot);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(frontFrontFoot.X, frontFrontFoot.Y), 2.2 * scale, 1.4 * scale);

        // 7. Chubby Cute Head
        Point head = new Point(x + (13 * scale), catY - (5 * scale) + (bob * 0.4));
        dc.DrawEllipse(new SolidColorBrush(shadowCol), null, new Point(head.X - (0.8 * scale), head.Y + (0.8 * scale)), 10.0 * scale, 9.0 * scale);
        dc.DrawEllipse(new SolidColorBrush(baseCol), null, head, 10.0 * scale, 9.0 * scale);
        dc.DrawEllipse(new SolidColorBrush(baseCol), null, new Point(head.X + (2.5 * scale), head.Y + (2.2 * scale)), 7.0 * scale, 5.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(head.X + (3.2 * scale), head.Y + (2.6 * scale)), 6.0 * scale, 4.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(highlightCol), null, new Point(head.X - (1.5 * scale), head.Y - (3.0 * scale)), 4.8 * scale, 2.4 * scale);

        // 8. Ears
        double earTwitch = (isWalking ? Math.Sin(_frameTick * 0.22) : Math.Sin(_frameTick * 0.08)) * (1.2 * scale);

        // Left/Back Ear
        var leftEar = new PathGeometry();
        var lf = new PathFigure { StartPoint = new Point(head.X - (5.5 * scale), head.Y - (2.5 * scale)) };
        lf.Segments.Add(new LineSegment(new Point(head.X - (2.5 * scale), head.Y - (12.5 * scale) - earTwitch), true));
        lf.Segments.Add(new LineSegment(new Point(head.X + (1.2 * scale), head.Y - (4.5 * scale)), true));
        lf.IsClosed = true;
        leftEar.Figures.Add(lf);
        dc.DrawGeometry(new SolidColorBrush(baseCol), null, leftEar);

        var leftInner = new PathGeometry();
        var lif = new PathFigure { StartPoint = new Point(head.X - (4.2 * scale), head.Y - (3.6 * scale)) };
        lif.Segments.Add(new LineSegment(new Point(head.X - (2.5 * scale), head.Y - (10.0 * scale) - (earTwitch * 0.6)), true));
        lif.Segments.Add(new LineSegment(new Point(head.X - (0.2 * scale), head.Y - (5.0 * scale)), true));
        lif.IsClosed = true;
        leftInner.Figures.Add(lif);
        dc.DrawGeometry(new SolidColorBrush(pinkCol), null, leftInner);

        // Right/Front Ear
        var rightEar = new PathGeometry();
        var rf = new PathFigure { StartPoint = new Point(head.X + (1.0 * scale), head.Y - (4.5 * scale)) };
        rf.Segments.Add(new LineSegment(new Point(head.X + (4.8 * scale), head.Y - (12.0 * scale) + earTwitch), true));
        rf.Segments.Add(new LineSegment(new Point(head.X + (8.5 * scale), head.Y - (2.8 * scale)), true));
        rf.IsClosed = true;
        rightEar.Figures.Add(rf);
        dc.DrawGeometry(new SolidColorBrush(baseCol), null, rightEar);

        var rightInner = new PathGeometry();
        var rif = new PathFigure { StartPoint = new Point(head.X + (2.4 * scale), head.Y - (5.0 * scale)) };
        rif.Segments.Add(new LineSegment(new Point(head.X + (4.6 * scale), head.Y - (9.6 * scale) + (earTwitch * 0.6)), true));
        rif.Segments.Add(new LineSegment(new Point(head.X + (6.8 * scale), head.Y - (4.0 * scale)), true));
        rif.IsClosed = true;
        rightInner.Figures.Add(rif);
        dc.DrawGeometry(new SolidColorBrush(pinkCol), null, rightInner);

        // White fluff tuft at front ear base
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(head.X + (2.8 * scale), head.Y - (3.8 * scale)), 1.4 * scale, 1.4 * scale);

        // 9. Expressive Anime Eyes (3/4 Profile)
        bool blink = (_frameTick % 46) < 3;
        Point leftEye = new Point(head.X + (1.2 * scale), head.Y + (0.5 * scale));
        Point rightEye = new Point(head.X + (6.6 * scale), head.Y + (0.3 * scale));

        if (blink)
        {
            var blinkPen = new Pen(new SolidColorBrush(Color.FromRgb(55, 32, 24)), 1.3 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            dc.DrawLine(blinkPen, new Point(leftEye.X - (1.4 * scale), leftEye.Y), new Point(leftEye.X + (1.4 * scale), leftEye.Y));
            dc.DrawLine(blinkPen, new Point(rightEye.X - (1.4 * scale), rightEye.Y), new Point(rightEye.X + (1.4 * scale), rightEye.Y));
        }
        else
        {
            // Dark cocoa pupil
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(46, 28, 22)), null, leftEye, 2.1 * scale, 2.6 * scale);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(46, 28, 22)), null, rightEye, 2.1 * scale, 2.6 * scale);

            // Amber-gold shiny iris lower crescent
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(245, 166, 35)), null, new Point(leftEye.X, leftEye.Y + (0.7 * scale)), 1.1 * scale, 1.1 * scale);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(245, 166, 35)), null, new Point(rightEye.X, rightEye.Y + (0.7 * scale)), 1.1 * scale, 1.1 * scale);

            // Large catchlight
            dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(leftEye.X - (0.6 * scale), leftEye.Y - (0.8 * scale)), 0.75 * scale, 0.75 * scale);
            dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(rightEye.X - (0.6 * scale), rightEye.Y - (0.8 * scale)), 0.75 * scale, 0.75 * scale);

            // Secondary small catchlight
            dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(leftEye.X + (0.6 * scale), leftEye.Y + (0.7 * scale)), 0.35 * scale, 0.35 * scale);
            dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(rightEye.X + (0.6 * scale), rightEye.Y + (0.7 * scale)), 0.35 * scale, 0.35 * scale);
        }

        // 10. Cute Pink Nose & :3 Smile
        Point nose = new Point(head.X + (3.8 * scale), head.Y + (2.8 * scale));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 115, 145)), null, nose, 1.1 * scale, 0.8 * scale);

        var mouthPen = new Pen(new SolidColorBrush(Color.FromRgb(95, 48, 32)), 0.9 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(mouthPen, new Point(nose.X, nose.Y + (0.8 * scale)), new Point(nose.X, nose.Y + (1.8 * scale)));
        dc.DrawLine(mouthPen, new Point(nose.X, nose.Y + (1.8 * scale)), new Point(nose.X - (1.6 * scale), nose.Y + (2.5 * scale)));
        dc.DrawLine(mouthPen, new Point(nose.X, nose.Y + (1.8 * scale)), new Point(nose.X + (1.8 * scale), nose.Y + (2.4 * scale)));

        // Rosy Blushing Cheeks
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(140, 255, 160, 180)), null, new Point(head.X + (0.2 * scale), head.Y + (3.8 * scale)), 1.8 * scale, 1.1 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(140, 255, 160, 180)), null, new Point(head.X + (8.0 * scale), head.Y + (3.5 * scale)), 1.8 * scale, 1.1 * scale);

        // Delicate Whiskers
        var whiskerPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 100, 70, 60)), 0.8 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        // Forward whiskers
        dc.DrawLine(whiskerPen, new Point(head.X + (6.2 * scale), head.Y + (3.4 * scale)), new Point(head.X + (11.5 * scale), head.Y + (2.6 * scale)));
        dc.DrawLine(whiskerPen, new Point(head.X + (6.0 * scale), head.Y + (4.3 * scale)), new Point(head.X + (11.2 * scale), head.Y + (5.2 * scale)));
        // Backward whiskers
        dc.DrawLine(whiskerPen, new Point(head.X + (1.6 * scale), head.Y + (3.6 * scale)), new Point(head.X - (3.2 * scale), head.Y + (2.8 * scale)));
        dc.DrawLine(whiskerPen, new Point(head.X + (1.8 * scale), head.Y + (4.4 * scale)), new Point(head.X - (3.0 * scale), head.Y + (5.3 * scale)));

        // Walking Dust Puffs
        if (isWalking && !isMini)
        {
            for (int i = 0; i < 3; i++)
            {
                double puffX = x - (18 + (i * 5));
                double puffY = floorY - (3 + (i * 2)) + (Math.Sin((_frameTick * 0.20) + i) * 1.5);
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)(50 - (i * 12)), 255, 250, 245)), null, new Point(puffX, puffY), 3.5 - i, 2.0 - (i * 0.4));
            }
        }
    }

    private void DrawCuteFeastingKitty(DrawingContext dc, double x, double floorY, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;
        double crunchBob = Math.Sin(_frameTick * 0.22) * (1.2 * scale);

        Color baseCol = Color.FromRgb(250, 166, 80);
        Color shadowCol = Color.FromRgb(216, 118, 42);
        Color creamCol = Color.FromRgb(255, 248, 240);
        Color pinkCol = Color.FromRgb(255, 170, 188);

        // Ground shadow
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(55, 38, 22, 18)), null, new Point(x, floorY + (1.2 * scale)), 20 * scale, 3.8 * scale);

        // Sitting Body
        Point body = new Point(x, floorY - (13 * scale));
        dc.DrawEllipse(new SolidColorBrush(shadowCol), null, new Point(body.X - (1.2 * scale), body.Y + (1.2 * scale)), 14 * scale, 9.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(baseCol), null, body, 14 * scale, 9.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(body.X + (1 * scale), body.Y + (2.0 * scale)), 8.0 * scale, 5.0 * scale);

        // Tucked hind paws
        var tuckedLegPen = new Pen(new SolidColorBrush(baseCol), 3.8 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(tuckedLegPen, new Point(body.X - (8 * scale), body.Y + (4 * scale)), new Point(body.X - (10 * scale), floorY - (0.8 * scale)));
        dc.DrawLine(tuckedLegPen, new Point(body.X + (8 * scale), body.Y + (4 * scale)), new Point(body.X + (10 * scale), floorY - (0.8 * scale)));
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(body.X - (10 * scale), floorY), 2.2 * scale, 1.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(body.X + (10 * scale), floorY), 2.2 * scale, 1.2 * scale);

        // Front Paws resting happily
        var frontPawPen = new Pen(new SolidColorBrush(creamCol), 3.0 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(frontPawPen, new Point(body.X - (2.5 * scale), body.Y + (2.0 * scale)), new Point(body.X - (4 * scale), floorY - (4 * scale)));
        dc.DrawLine(frontPawPen, new Point(body.X + (2.0 * scale), body.Y + (2.0 * scale)), new Point(body.X + (3.5 * scale), floorY - (4 * scale)));

        // Happy Curled Tail
        var tailPen = new Pen(new SolidColorBrush(baseCol), 4.2 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var tailGeom = new PathGeometry();
        var tailFig = new PathFigure { StartPoint = new Point(body.X - (11 * scale), body.Y + (2 * scale)) };
        tailFig.Segments.Add(new QuadraticBezierSegment(new Point(body.X - (20 * scale), body.Y - (8 * scale)), new Point(body.X - (12 * scale), body.Y - (14 * scale)), true));
        tailGeom.Figures.Add(tailFig);
        dc.DrawGeometry(null, tailPen, tailGeom);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(body.X - (12 * scale), body.Y - (14 * scale)), 1.8 * scale, 1.8 * scale);

        // Chubby Head
        Point head = new Point(x + (6 * scale), floorY - (22 * scale) + crunchBob);
        dc.DrawEllipse(new SolidColorBrush(shadowCol), null, new Point(head.X - (0.8 * scale), head.Y + (0.8 * scale)), 9.2 * scale, 8.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(baseCol), null, head, 9.2 * scale, 8.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(head.X + (1.2 * scale), head.Y + (2.6 * scale)), 5.6 * scale, 3.8 * scale);

        // Ears with proper inner ear layers
        var leftEar = new PathGeometry();
        var lf = new PathFigure { StartPoint = new Point(head.X - (5.0 * scale), head.Y - (2.2 * scale)) };
        lf.Segments.Add(new LineSegment(new Point(head.X - (2.2 * scale), head.Y - (11.0 * scale)), true));
        lf.Segments.Add(new LineSegment(new Point(head.X + (0.2 * scale), head.Y - (4.0 * scale)), true));
        lf.IsClosed = true;
        leftEar.Figures.Add(lf);
        dc.DrawGeometry(new SolidColorBrush(baseCol), null, leftEar);

        var leftInner = new PathGeometry();
        var lif = new PathFigure { StartPoint = new Point(head.X - (4.0 * scale), head.Y - (3.2 * scale)) };
        lif.Segments.Add(new LineSegment(new Point(head.X - (2.2 * scale), head.Y - (9.0 * scale)), true));
        lif.Segments.Add(new LineSegment(new Point(head.X - (0.8 * scale), head.Y - (4.4 * scale)), true));
        lif.IsClosed = true;
        leftInner.Figures.Add(lif);
        dc.DrawGeometry(new SolidColorBrush(pinkCol), null, leftInner);

        var rightEar = new PathGeometry();
        var rf = new PathFigure { StartPoint = new Point(head.X + (1.2 * scale), head.Y - (4.0 * scale)) };
        rf.Segments.Add(new LineSegment(new Point(head.X + (4.4 * scale), head.Y - (10.5 * scale)), true));
        rf.Segments.Add(new LineSegment(new Point(head.X + (7.4 * scale), head.Y - (2.6 * scale)), true));
        rf.IsClosed = true;
        rightEar.Figures.Add(rf);
        dc.DrawGeometry(new SolidColorBrush(baseCol), null, rightEar);

        var rightInner = new PathGeometry();
        var rif = new PathFigure { StartPoint = new Point(head.X + (2.4 * scale), head.Y - (4.6 * scale)) };
        rif.Segments.Add(new LineSegment(new Point(head.X + (4.4 * scale), head.Y - (8.6 * scale)), true));
        rif.Segments.Add(new LineSegment(new Point(head.X + (6.0 * scale), head.Y - (3.4 * scale)), true));
        rif.IsClosed = true;
        rightInner.Figures.Add(rif);
        dc.DrawGeometry(new SolidColorBrush(pinkCol), null, rightInner);

        // Happy Closed Eyes ^ ^
        var happyPen = new Pen(new SolidColorBrush(Color.FromRgb(65, 38, 28)), 1.2 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(happyPen, new Point(head.X - (1.6 * scale), head.Y + (0.2 * scale)), new Point(head.X - (0.2 * scale), head.Y - (0.8 * scale)));
        dc.DrawLine(happyPen, new Point(head.X - (0.2 * scale), head.Y - (0.8 * scale)), new Point(head.X + (1.2 * scale), head.Y + (0.2 * scale)));
        dc.DrawLine(happyPen, new Point(head.X + (3.6 * scale), head.Y + (0.2 * scale)), new Point(head.X + (5.0 * scale), head.Y - (0.8 * scale)));
        dc.DrawLine(happyPen, new Point(head.X + (5.0 * scale), head.Y - (0.8 * scale)), new Point(head.X + (6.4 * scale), head.Y + (0.2 * scale)));

        // Cute Pink Nose & Licking Tongue
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 120, 150)), null, new Point(head.X + (2.4 * scale), head.Y + (2.4 * scale)), 1.0 * scale, 0.7 * scale);
        dc.DrawLine(happyPen, new Point(head.X + (2.4 * scale), head.Y + (3.0 * scale)), new Point(head.X + (2.4 * scale), head.Y + (4.0 * scale)));
        dc.DrawLine(happyPen, new Point(head.X + (2.4 * scale), head.Y + (4.0 * scale)), new Point(head.X + (1.0 * scale), head.Y + (4.6 * scale)));
        dc.DrawLine(happyPen, new Point(head.X + (2.4 * scale), head.Y + (4.0 * scale)), new Point(head.X + (3.8 * scale), head.Y + (4.5 * scale)));

        // Little cute pink tongue
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 110, 145)), null, new Point(head.X + (2.4 * scale), head.Y + (5.2 * scale)), 1.2 * scale, 1.4 * scale);

        // Rosy Blushing Cheeks
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(150, 255, 160, 180)), null, new Point(head.X - (1.8 * scale), head.Y + (3.2 * scale)), 1.8 * scale, 1.0 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(150, 255, 160, 180)), null, new Point(head.X + (6.0 * scale), head.Y + (3.0 * scale)), 1.8 * scale, 1.0 * scale);

        // Floating Hearts & Sparkles
        for (int h = 0; h < (isMini ? 2 : 4); h++)
        {
            double hx = x + (h * 7 * scale);
            double hy = floorY - (26 * scale) - (((_frameTick * 0.8) + (h * 8)) % (isMini ? 14 : 22));
            DrawHeart(dc, hx, hy, (isMini ? 2.8 : 4.0) * scale, new SolidColorBrush(Color.FromArgb(210, 255, 96, 148)));
        }

        for (int c = 0; c < (isMini ? 3 : 5); c++)
        {
            double crumbX = x + (7 * scale) + (Math.Sin((_frameTick * 0.12) + c) * 5 * scale);
            double crumbY = floorY - (10 * scale) - (c * 2.0 * scale);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 214, 114)), null, new Point(crumbX, crumbY), 1.2 * scale, 1.0 * scale);
        }

        string text = isMini ? "💖 Yum!" : "💖 Purrr... deluxe feast!";
        var ft = CreateText(text, isMini ? 9.0 : 11.2, new SolidColorBrush(Color.FromRgb(255, 92, 148)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.25), floorY - (38 * scale)));
    }

    private void DrawCuteSleepingKitty(DrawingContext dc, double x, double floorY, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;
        double breathe = Math.Sin(_frameTick * 0.10) * (1.2 * scale);

        Color baseCol = Color.FromRgb(250, 166, 80);
        Color shadowCol = Color.FromRgb(216, 118, 42);
        Color creamCol = Color.FromRgb(255, 248, 240);
        Color pinkCol = Color.FromRgb(255, 170, 188);

        // Curled Body
        Point body = new Point(x - (2 * scale), floorY - (3 * scale) + breathe);
        dc.DrawEllipse(new SolidColorBrush(shadowCol), null, new Point(body.X - (1.2 * scale), body.Y + (1.0 * scale)), 14.5 * scale, 9.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(baseCol), null, body, 14.5 * scale, 9.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(body.X + (2.0 * scale), body.Y + (2.5 * scale)), 7.5 * scale, 4.2 * scale);

        // Curled Fluffy Tail around Body
        var tailPen = new Pen(new SolidColorBrush(baseCol), 4.2 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var tailGeom = new PathGeometry();
        var tf = new PathFigure { StartPoint = new Point(body.X - (12 * scale), body.Y + (1 * scale)) };
        tf.Segments.Add(new QuadraticBezierSegment(new Point(body.X - (18 * scale), body.Y - (10 * scale)), new Point(body.X - (2 * scale), body.Y - (9 * scale)), true));
        tailGeom.Figures.Add(tf);
        dc.DrawGeometry(null, tailPen, tailGeom);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(body.X - (2 * scale), body.Y - (9 * scale)), 1.8 * scale, 1.8 * scale);

        // Tucked Head resting on front paws
        Point head = new Point(x + (8 * scale), floorY - (4 * scale) + breathe);
        dc.DrawEllipse(new SolidColorBrush(baseCol), null, head, 7.8 * scale, 7.0 * scale);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(head.X + (1.2 * scale), head.Y + (2.2 * scale)), 4.8 * scale, 3.2 * scale);

        // Paws tucked under chin
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(head.X - (1.0 * scale), head.Y + (4.8 * scale)), 2.6 * scale, 1.6 * scale);
        dc.DrawEllipse(new SolidColorBrush(creamCol), null, new Point(head.X + (3.0 * scale), head.Y + (4.8 * scale)), 2.6 * scale, 1.6 * scale);

        // Relaxed Ears
        var ear1 = new PathGeometry();
        var e1 = new PathFigure { StartPoint = new Point(head.X - (4.8 * scale), head.Y - (1.8 * scale)) };
        e1.Segments.Add(new LineSegment(new Point(head.X - (2.4 * scale), head.Y - (9.0 * scale)), true));
        e1.Segments.Add(new LineSegment(new Point(head.X + (0.2 * scale), head.Y - (3.4 * scale)), true));
        e1.IsClosed = true;
        ear1.Figures.Add(e1);
        dc.DrawGeometry(new SolidColorBrush(baseCol), null, ear1);

        var ear2 = new PathGeometry();
        var e2 = new PathFigure { StartPoint = new Point(head.X + (0.4 * scale), head.Y - (3.4 * scale)) };
        e2.Segments.Add(new LineSegment(new Point(head.X + (3.4 * scale), head.Y - (8.6 * scale)), true));
        e2.Segments.Add(new LineSegment(new Point(head.X + (6.0 * scale), head.Y - (2.4 * scale)), true));
        e2.IsClosed = true;
        ear2.Figures.Add(e2);
        dc.DrawGeometry(new SolidColorBrush(baseCol), null, ear2);

        // Sleeping Eyes ⌒ ⌒
        var sleepPen = new Pen(new SolidColorBrush(Color.FromRgb(75, 45, 35)), 1.1 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(sleepPen, new Point(head.X - (0.5 * scale), head.Y + (0.4 * scale)), new Point(head.X + (1.4 * scale), head.Y + (0.4 * scale)));
        dc.DrawLine(sleepPen, new Point(head.X + (3.0 * scale), head.Y), new Point(head.X + (4.8 * scale), head.Y));
        dc.DrawEllipse(new SolidColorBrush(pinkCol), null, new Point(head.X + (1.8 * scale), head.Y + (2.0 * scale)), 0.9 * scale, 0.6 * scale);

        // Floating Zzz's
        string text = (_frameTick % 24 < 12) ? "💤 z Z z" : "💤 Z z Z";
        var ft = CreateText(text, isMini ? 9.0 : 11.5, new SolidColorBrush(Color.FromRgb(160, 126, 228)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x + (10 * scale), floorY - (18 * scale)));
    }

    #endregion

    #region ☕ Scene 4: 🌧️ Zoomed-in Rainy Window Cafe (Foggy Glass & Prominent Coffee Mug)

        private void RenderCafeScene(DrawingContext dc, double w, double h)
    {
        bool isMini = IsMiniMode;
        bool isGoal = IsGoalReached || ProgressFraction >= 0.999;
        bool isTrackingScene = IsTracking && !IsRestPhase && !isGoal;

        double sillH = isMini ? 12 : 22;
        double sillY = h - sillH;
        double windowH = h - sillH;

        var outdoorBrush = new LinearGradientBrush(
            Color.FromRgb(14, 18, 34),
            Color.FromRgb(26, 32, 52),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(outdoorBrush, null, new Rect(0, 0, w, h));

        DrawCafeWindowBackdropDetails(dc, w, windowH);

        if (!isMini)
        {
            if (isTrackingScene)
            {
                DrawCafeFocusLampReflection(dc, w, windowH);
            }
            else if (IsRestPhase)
            {
                DrawCafeRestGlow(dc, w, windowH, new Point(w * 0.24, windowH * 0.72), 110, 80);
            }
        }

        DrawRainyBokeh(dc, w, h);
        DrawFallingRainStreaks(dc, w, h);

        var sillBrush = new LinearGradientBrush(
            Color.FromRgb(62, 38, 24),
            Color.FromRgb(40, 22, 14),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(sillBrush, null, new Rect(0, sillY, w, sillH));

        var bevelPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 145, 100, 70)), isMini ? 1.0 : 1.5);
        dc.DrawLine(bevelPen, new Point(0, sillY), new Point(w, sillY));

        if (!isMini)
        {
            DrawCafeSillCandle(dc, w * 0.18, sillY, IsRestPhase);
            DrawMiniSucculent(dc, 28, sillY);
            DrawFairyLights(dc, w, sillY);
            DrawLoFiVinylPlayer(dc, 52, sillY);

            if (isTrackingScene)
            {
                DrawCafeFocusJournal(dc, w * 0.24, sillY);
            }
            else if (IsRestPhase)
            {
                DrawCafeRestBookAndCat(dc, w * 0.24, sillY);
            }
        }

        if (IsTracking || IsRestPhase)
        {
            DrawCafeLoFiMusicNotes(dc, w, sillY);
        }

        double mugW = isMini ? 46 : 84;
        double mugH = isMini ? 42 : 78;
        double mugX = w - mugW - (isMini ? 18 : 60);
        double mugY = sillY - mugH + (isMini ? 3 : 5);

        double saucerW = mugW * 1.35;
        double saucerH = isMini ? 7 : 12;
        double saucerX = mugX - ((saucerW - mugW) * 0.5);
        double saucerY = sillY - (saucerH * 0.4);

        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(90, 15, 8, 4)), null, new Point(saucerX + (saucerW * 0.5), saucerY + saucerH - 1), saucerW * 0.52, saucerH * 0.45);

        var saucerBrush = new LinearGradientBrush(
            Color.FromRgb(252, 248, 240),
            Color.FromRgb(224, 212, 196),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRoundedRectangle(saucerBrush, new Pen(new SolidColorBrush(Color.FromRgb(175, 160, 142)), 1.2), new Rect(saucerX, saucerY, saucerW, saucerH), saucerH * 0.5, saucerH * 0.5);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(50, 120, 100, 80)), null, new Point(saucerX + (saucerW * 0.5), saucerY + (saucerH * 0.45)), saucerW * 0.35, saucerH * 0.25);

        if (!isMini)
        {
            DrawCafeSaucerProps(dc, saucerX, saucerY, saucerW, saucerH, sillY);
        }

        DrawAestheticRainyCoffeeMug(dc, mugX, mugY, mugW, mugH, isMini);
        DrawMugSteamWisps(dc, mugX + (mugW * 0.5), mugY, mugW, mugH, isMini);

        DrawFourPaneWindowGrid(dc, w, windowH);

        double fogLevel = Math.Clamp(0.12 + (ProgressFraction * 0.78), 0.12, 0.90);
        DrawWindowFogLayer(dc, w, windowH, fogLevel, mugX, mugY);
        DrawWindowGlassDroplets(dc, w, windowH, fogLevel);

        var warmAmbient = new RadialGradientBrush(
            Color.FromArgb(IsRestPhase ? (byte)55 : (byte)35, 255, 205, 120),
            Color.FromArgb(0, 255, 180, 80));
        dc.DrawEllipse(warmAmbient, null, new Point(w * 0.3, h * 0.6), w * 0.5, h * 0.45);

        if (isGoal)
        {
            DrawCafeGoalCelebrationAmbience(dc, w, windowH, mugX + (mugW * 0.5), mugY + (mugH * 0.15));
        }

        if (isGoal)
        {
            DrawTaskCompleteFingerWriting(dc, w, windowH, mugX, isMini);
        }
        else if (IsRestPhase)
        {
            DrawRestFingerWriting(dc, w, windowH, mugX, isMini);
        }
        else if (IsTracking)
        {
            DrawFogProgressHint(dc, w, windowH, mugX, ProgressFraction, isMini);
        }
    }

    private void DrawFourPaneWindowGrid(DrawingContext dc, double w, double windowH)
    {
        double midX = w * 0.48;
        double midY = windowH * 0.48;
        double barThickness = IsMiniMode ? 3.5 : 6.0;

        var woodBrush = new LinearGradientBrush(
            Color.FromRgb(46, 26, 16),
            Color.FromRgb(28, 14, 8),
            new Point(0, 0),
            new Point(1, 1));
        var bevelPen = new Pen(new SolidColorBrush(Color.FromArgb(110, 140, 95, 65)), 1.0);

        // Vertical Mullion dividing left and right panes
        dc.DrawRectangle(woodBrush, bevelPen, new Rect(midX - (barThickness * 0.5), 0, barThickness, windowH));

        // Horizontal Mullion dividing top and bottom panes
        dc.DrawRectangle(woodBrush, bevelPen, new Rect(0, midY - (barThickness * 0.5), w, barThickness));

        // Outer window frame border (top, left, right)
        double frameT = IsMiniMode ? 2.5 : 5.0;
        dc.DrawRectangle(woodBrush, bevelPen, new Rect(0, 0, w, frameT));
        dc.DrawRectangle(woodBrush, bevelPen, new Rect(0, 0, frameT, windowH));
        dc.DrawRectangle(woodBrush, bevelPen, new Rect(w - frameT, 0, frameT, windowH));
    }

    private void DrawRainyBokeh(DrawingContext dc, double w, double h)
    {
        var bokehPoints = new (double xPct, double yPct, double radius, Color color)[]
        {
            (0.18, 0.35, IsMiniMode ? 16 : 32, Color.FromArgb(75, 255, 185, 90)),   // Warm Amber Streetlight
            (0.38, 0.55, IsMiniMode ? 20 : 40, Color.FromArgb(65, 255, 130, 160)),  // Soft Rose Neon
            (0.62, 0.28, IsMiniMode ? 14 : 28, Color.FromArgb(70, 120, 220, 255)),  // Pale Cyan City Light
            (0.82, 0.48, IsMiniMode ? 18 : 36, Color.FromArgb(80, 255, 210, 110)),  // Golden Shop Glow
            (0.50, 0.20, IsMiniMode ? 12 : 24, Color.FromArgb(55, 200, 160, 255))   // Violet Haze
        };

        foreach (var b in bokehPoints)
        {
            double bx = w * b.xPct;
            double by = h * b.yPct;
            double pulse = Math.Sin((_frameTick * 0.15) + b.xPct * 10) * 0.2 + 0.8;
            double currentR = b.radius * pulse;

            var radialBrush = new RadialGradientBrush(b.color, Color.FromArgb(0, b.color.R, b.color.G, b.color.B));
            dc.DrawEllipse(radialBrush, null, new Point(bx, by), currentR, currentR);
        }
    }

    private void DrawFallingRainStreaks(DrawingContext dc, double w, double h)
    {
        var rainPen = new Pen(new SolidColorBrush(Color.FromArgb(75, 190, 220, 255)), 1.2);
        var rand = new Random(777);
        int streakCount = IsMiniMode ? 22 : 48;

        for (int i = 0; i < streakCount; i++)
        {
            double rx = (rand.NextDouble() * (w + 40)) - 20;
            double ry = (rand.NextDouble() * h);
            double fallOffset = (_frameTick * 5.5 + (i * 17)) % (h + 30);
            double currentY = (ry + fallOffset) % h;
            double len = 8 + (rand.NextDouble() * 14);

            dc.DrawLine(rainPen, new Point(rx, currentY), new Point(rx - 3, currentY + len));
        }
    }

    private void DrawMiniSucculent(DrawingContext dc, double x, double sillY)
    {
        // Cute mini terracotta pot
        double potW = 18;
        double potH = 14;
        double potY = sillY - potH + 2;

        var potBrush = new LinearGradientBrush(Color.FromRgb(215, 110, 75), Color.FromRgb(175, 80, 50), new Point(0, 0), new Point(1, 0));
        dc.DrawRoundedRectangle(potBrush, new Pen(new SolidColorBrush(Color.FromRgb(140, 60, 35)), 1), new Rect(x, potY, potW, potH), 2, 2);

        // Pot rim
        dc.DrawRoundedRectangle(potBrush, null, new Rect(x - 2, potY, potW + 4, 4), 1, 1);

        // Cute succulent leaves
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(110, 185, 125)), null, new Point(x + 5, potY - 2), 4, 6);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(125, 205, 140)), null, new Point(x + (potW * 0.5), potY - 5), 5, 7);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(110, 185, 125)), null, new Point(x + potW - 5, potY - 2), 4, 6);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(160, 225, 175)), null, new Point(x + (potW * 0.5), potY - 3), 3, 4);
    }

    /// <summary>
    /// Small retro turntable spinning a vinyl record on the sill - the classic
    /// "lo-fi beats to relax/study to" visual anchor.
    /// </summary>
        private void DrawLoFiVinylPlayer(DrawingContext dc, double x, double sillY)
    {
        double baseW = 26;
        double baseH = 8;
        double baseY = sillY - baseH + 1;

        var baseBrush = new LinearGradientBrush(Color.FromRgb(150, 100, 65), Color.FromRgb(110, 70, 42), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(baseBrush, new Pen(new SolidColorBrush(Color.FromRgb(85, 52, 30)), 1), new Rect(x, baseY, baseW, baseH), 2, 2);

        double discR = 8.5;
        Point discCenter = new Point(x + (baseW * 0.42), baseY - 1);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(25, 22, 28)), new Pen(new SolidColorBrush(Color.FromRgb(60, 55, 65)), 0.8), discCenter, discR, discR * 0.55);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(95, 180, 180, 190)), 0.6), discCenter, discR * 0.72, discR * 0.39);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(75, 140, 145, 165)), 0.5), discCenter, discR * 0.50, discR * 0.28);

        double spin = _frameTick * 0.14;
        for (int g = 0; g < 4; g++)
        {
            double a = spin + (g * (Math.PI * 2 / 4));
            Point p = new Point(discCenter.X + (Math.Cos(a) * discR * 0.65), discCenter.Y + (Math.Sin(a) * discR * 0.65 * 0.55));
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(90, 220, 220, 230)), null, p, 1.1, 0.7);
        }

        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(235, 170, 110)), null, discCenter, discR * 0.28, discR * 0.16);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(70, 45, 28)), null, discCenter, 0.8, 0.8);

        double armLift = Math.Sin(_frameTick * 0.08) * 0.6;
        var armPen = new Pen(new SolidColorBrush(Color.FromRgb(210, 200, 190)), 1.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(armPen, new Point(x + baseW - 3, baseY - 4), new Point(discCenter.X + 3.5, discCenter.Y - 1.2 + armLift));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(210, 200, 190)), null, new Point(x + baseW - 3, baseY - 4), 1.6, 1.6);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(85, 80, 76)), null, new Point(discCenter.X + 3.5, discCenter.Y - 1.2 + armLift), 0.9, 0.9);

        for (int i = 0; i < 3; i++)
        {
            double barH = 1.5 + ((Math.Sin((_frameTick * 0.22) + i) * 0.5 + 0.5) * 2.2);
            double bx = x + 17 + (i * 2.5);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 190, 110)), null, new Rect(bx, baseY + baseH - barH - 1, 1.3, barH), 0.5, 0.5);
        }
    }

    /// <summary>
    /// Softly floating lo-fi music notes drifting up past the foggy glass, reinforcing the
    /// "chill study/relax music mix" atmosphere the whole cafe scene is going for.
    /// </summary>
    private void DrawCafeLoFiMusicNotes(DrawingContext dc, double w, double sillY)
    {
        string[] notes = { "♪", "♫", "♩" };
        var noteColors = new[] { Color.FromRgb(255, 210, 150), Color.FromRgb(255, 235, 200), Color.FromRgb(230, 190, 255) };

        for (int n = 0; n < (IsMiniMode ? 1 : 3); n++)
        {
            double riseOffset = ((_frameTick * 1.0) + (n * 40)) % 90;
            double nx = (IsMiniMode ? w * 0.18 : 90) + (Math.Sin((_frameTick * 0.1) + n) * 8);
            double ny = sillY - riseOffset;

            if (ny < 6) continue;

            double alpha = Math.Clamp(1.0 - (riseOffset / 90.0), 0.05, 0.75);
            var nBrush = new SolidColorBrush(Color.FromArgb((byte)(220 * alpha), noteColors[n % noteColors.Length].R, noteColors[n % noteColors.Length].G, noteColors[n % noteColors.Length].B));
            var ft = CreateText(notes[n % notes.Length], IsMiniMode ? 9 : 12, nBrush, FontWeights.Bold);
            dc.DrawText(ft, new Point(nx, ny));
        }
    }

        private void DrawFairyLights(DrawingContext dc, double w, double sillY)
    {
        var wirePen = new Pen(new SolidColorBrush(Color.FromArgb(120, 80, 60, 40)), 0.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var bulbColors = new Color[]
        {
            Color.FromRgb(255, 220, 120),
            Color.FromRgb(255, 160, 180),
            Color.FromRgb(160, 230, 255),
            Color.FromRgb(255, 230, 140),
            Color.FromRgb(200, 170, 255)
        };

        double startX = 60;
        double endX = w - 160;
        if (endX <= startX) return;

        double spacing = 45;
        int idx = 0;
        double prevX = startX;
        double prevY = sillY + 3;

        for (double fx = startX + spacing; fx <= endX; fx += spacing)
        {
            double wave = Math.Sin((_frameTick * 0.08) + (idx * 0.55));
            double bulbY = sillY + 3 + (wave * 1.1);
            double dipY = sillY + 6 + (Math.Sin((_frameTick * 0.05) + idx) * 1.5);

            var geom = new PathGeometry();
            var fig = new PathFigure { StartPoint = new Point(prevX, prevY) };
            fig.Segments.Add(new QuadraticBezierSegment(new Point((prevX + fx) * 0.5, dipY), new Point(fx, bulbY), true));
            geom.Figures.Add(fig);
            dc.DrawGeometry(null, wirePen, geom);

            Color bulbCol = bulbColors[idx % bulbColors.Length];
            double wavePulse = (Math.Sin((_frameTick * 0.14) - (idx * 0.8)) + 1.0) * 0.5;
            double pulse = 0.55 + (wavePulse * 0.55);
            var glowBrush = new RadialGradientBrush(
                Color.FromArgb((byte)(175 * pulse), bulbCol.R, bulbCol.G, bulbCol.B),
                Color.FromArgb(0, bulbCol.R, bulbCol.G, bulbCol.B));
            dc.DrawEllipse(glowBrush, null, new Point(fx, bulbY), 8 + (pulse * 2), 8 + (pulse * 2));
            dc.DrawEllipse(new SolidColorBrush(bulbCol), null, new Point(fx, bulbY), 2.1, 2.8);
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(78, 58, 48)), null, new Rect(fx - 1.2, bulbY - 5.1, 2.4, 1.8));

            prevX = fx;
            prevY = bulbY;
            idx++;
        }
    }

    private void DrawWindowFogLayer(DrawingContext dc, double w, double h, double fogLevel, double mugX, double mugY)
    {
        // Overall Frosted Glass Mist Gradient
        byte baseAlpha = (byte)(fogLevel * 155);
        var fogBrush = new LinearGradientBrush(
            Color.FromArgb((byte)(baseAlpha * 0.8), 210, 225, 240),
            Color.FromArgb(baseAlpha, 230, 240, 250),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(fogBrush, null, new Rect(0, 0, w, h));

        // Extra Dense Warm Steam Plume radiating from the Coffee Mug
        double steamRadius = IsMiniMode ? 55 : 100;
        var mugSteamGlow = new RadialGradientBrush(
            Color.FromArgb((byte)(fogLevel * 180), 255, 248, 235),
            Color.FromArgb(0, 220, 235, 250));
        dc.DrawEllipse(mugSteamGlow, null, new Point(mugX, mugY - 10), steamRadius * 1.4, steamRadius);

        // Subtle Condensation Texture (Gentle misty puffs across the glass)
        if (!IsMiniMode)
        {
            var rand = new Random(123);
            for (int p = 0; p < 8; p++)
            {
                double px = rand.NextDouble() * w;
                double py = rand.NextDouble() * h;
                double pr = 20 + rand.Next(35);
                double pulse = Math.Sin((_frameTick * 0.08) + p) * 0.1 + 0.9;
                var puffBrush = new RadialGradientBrush(
                    Color.FromArgb((byte)(fogLevel * 35 * pulse), 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255));
                dc.DrawEllipse(puffBrush, null, new Point(px, py), pr, pr);
            }
        }
    }

    private void DrawWindowGlassDroplets(DrawingContext dc, double w, double h, double fogLevel)
    {
        var rand = new Random(404);
        int dropCount = IsMiniMode ? 12 : 28;

        for (int i = 0; i < dropCount; i++)
        {
            double dx = rand.NextDouble() * w;
            double baseSpeed = 0.35 + (rand.NextDouble() * 0.7);
            double dropY = ((_frameTick * baseSpeed) + (i * 24)) % (h - 10);
            double dropR = 1.5 + (rand.NextDouble() * 2.2);

            // Water droplet
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(170, 220, 245, 255)), null, new Point(dx, dropY), dropR, dropR * 1.3);
            // Glint shine
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)), null, new Point(dx - (dropR * 0.3), dropY - (dropR * 0.3)), dropR * 0.4, dropR * 0.4);

            // Trickle trail cutting a clear path through the foggy condensation
            if (i % 2 == 0)
            {
                var trailPen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(50 + (fogLevel * 90)), 180, 220, 255)), 1.0);
                dc.DrawLine(trailPen, new Point(dx, Math.Max(0, dropY - 20)), new Point(dx, dropY));
            }
        }
    }

        private void DrawMugSteamWisps(DrawingContext dc, double centerX, double mugTopY, double mugW, double mugH, bool isMini)
    {
        if (!(IsTracking || IsGoalReached || IsRestPhase))
        {
            return;
        }

        bool isGoal = IsGoalReached || ProgressFraction >= 0.999;
        double scale = isMini ? 0.6 : 1.0;
        int wispCount = isMini ? 3 : (isGoal ? 6 : 5);

        for (int s = 0; s < wispCount; s++)
        {
            double maxRise = isMini ? 35 : (IsRestPhase ? 68 : (isGoal ? 85 : 76));
            double speed = IsRestPhase ? 0.75 + (s * 0.14) : 1.0 + (s * 0.22);
            double steamOffset = (_frameTick * speed + (s * 16)) % maxRise;
            double steamY = mugTopY - steamOffset;
            double wave = Math.Sin((_frameTick * 0.18) + (s * 1.2)) * ((IsRestPhase ? 4.5 : 6.5) * scale);
            double alphaFrac = Math.Clamp(1.0 - (steamOffset / maxRise), 0.05, 0.90);

            byte alpha = (byte)(155 * alphaFrac);
            var steamBrush = new SolidColorBrush(Color.FromArgb(alpha, 255, 246, 235));
            double wispX = centerX + wave + ((s - (wispCount * 0.5)) * (8.5 * scale));
            double wispRadiusX = (4.4 + (steamOffset * 0.08)) * scale;
            double wispRadiusY = (2.8 + (steamOffset * 0.05)) * scale;

            dc.DrawEllipse(steamBrush, null, new Point(wispX, steamY), wispRadiusX, wispRadiusY);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)(110 * alphaFrac), 255, 252, 246)), null, new Point(wispX + (wispRadiusX * 0.35), steamY - (wispRadiusY * 0.4)), wispRadiusX * 0.55, wispRadiusY * 0.45);

            if (!isMini)
            {
                var curlPen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(70 * alphaFrac), 255, 245, 235)), 1.1) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                var steamGeom = new PathGeometry();
                var sf = new PathFigure { StartPoint = new Point(wispX - (3 * scale), steamY + (2 * scale)) };
                sf.Segments.Add(new BezierSegment(
                    new Point(wispX - (8 * scale), steamY - (4 * scale)),
                    new Point(wispX + (7 * scale), steamY - (10 * scale)),
                    new Point(wispX + (2 * scale), steamY - (15 * scale)),
                    true));
                steamGeom.Figures.Add(sf);
                dc.DrawGeometry(null, curlPen, steamGeom);
            }

            if (isGoal && alphaFrac > 0.42 && s % 2 == 0)
            {
                DrawHeart(dc, wispX + (Math.Sin((_frameTick * 0.08) + s) * 3), steamY - (7 * scale), (isMini ? 3.2 : 5.2) * alphaFrac, new SolidColorBrush(Color.FromArgb((byte)(115 * alphaFrac), 255, 214, 226)));
            }
        }
    }

        private void DrawAestheticRainyCoffeeMug(DrawingContext dc, double x, double y, double w, double h, bool isMini)
    {
        double scale = isMini ? 0.55 : 1.0;
        bool isGoal = IsGoalReached || ProgressFraction >= 0.999;

        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 20, 10, 5)), null, new Point(x + (w * 0.5), y + h), w * 0.48, 5 * scale);

        double handleW = 16 * scale;
        double handleH = 38 * scale;
        double handleX = x + w - (4 * scale);
        double handleY = y + (h * 0.22);

        var handlePen = new Pen(new LinearGradientBrush(
            Color.FromRgb(254, 250, 244),
            Color.FromRgb(215, 202, 186),
            new Point(0, 0),
            new Point(1, 1)), 6.0 * scale)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        var handleGeom = new PathGeometry();
        var hf = new PathFigure { StartPoint = new Point(handleX, handleY) };
        hf.Segments.Add(new BezierSegment(
            new Point(handleX + handleW + (12 * scale), handleY + (handleH * 0.1)),
            new Point(handleX + handleW + (12 * scale), handleY + (handleH * 0.9)),
            new Point(handleX, handleY + handleH),
            true));
        handleGeom.Figures.Add(hf);
        dc.DrawGeometry(null, handlePen, handleGeom);

        var mugBrush = new LinearGradientBrush(
            Color.FromRgb(255, 252, 246),
            Color.FromRgb(230, 218, 202),
            new Point(0, 0),
            new Point(1, 0));
        var mugPen = new Pen(new SolidColorBrush(Color.FromRgb(180, 168, 150)), 1.4 * scale);

        var bodyGeom = new PathGeometry();
        var bf = new PathFigure { StartPoint = new Point(x + (3 * scale), y + (10 * scale)) };
        bf.Segments.Add(new LineSegment(new Point(x + (6 * scale), y + h - (12 * scale)), true));
        bf.Segments.Add(new QuadraticBezierSegment(new Point(x + (7 * scale), y + h), new Point(x + (18 * scale), y + h), true));
        bf.Segments.Add(new LineSegment(new Point(x + w - (18 * scale), y + h), true));
        bf.Segments.Add(new QuadraticBezierSegment(new Point(x + w - (7 * scale), y + h), new Point(x + w - (6 * scale), y + h - (12 * scale)), true));
        bf.Segments.Add(new LineSegment(new Point(x + w - (3 * scale), y + (10 * scale)), true));
        bf.IsClosed = true;
        bodyGeom.Figures.Add(bf);
        dc.DrawGeometry(mugBrush, mugPen, bodyGeom);

        var glossBrush = new LinearGradientBrush(
            Color.FromArgb(130, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255),
            new Point(0, 0),
            new Point(1, 0));
        dc.DrawRoundedRectangle(glossBrush, null, new Rect(x + (9 * scale), y + (12 * scale), 8 * scale, h - (22 * scale)), 4 * scale, 4 * scale);

        if (!isMini)
        {
            for (int i = 0; i < 9; i++)
            {
                double speckX = x + (w * 0.18) + ((i * 7.7) % (w * 0.58));
                double speckY = y + (h * 0.20) + (((i * 11.0) % 20));
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(65, 160, 142, 120)), null, new Point(speckX, speckY), 0.9, 0.9);
            }
        }

        double heartY = y + (h * 0.55);
        DrawHeart(dc, x + (w * 0.48), heartY, 7 * scale, new SolidColorBrush(Color.FromRgb(215, 115, 95)));
        if (!isMini)
        {
            DrawSparkle(dc, x + (w * 0.48) + 14, heartY - 6, 2.5, new SolidColorBrush(Color.FromRgb(235, 180, 90)));
        }

        double rimH = 16 * scale;
        double rimCenterY = y + (2 * scale) + (rimH * 0.5);
        Point rimCenter = new Point(x + (w * 0.5), rimCenterY);

        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(65, 42, 28)), new Pen(new SolidColorBrush(Color.FromRgb(165, 150, 135)), 1.2 * scale), rimCenter, (w * 0.47), rimH * 0.5);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(85, 255, 255, 255)), null, new Point(rimCenter.X - (w * 0.10), rimCenter.Y - (rimH * 0.12)), w * 0.12, rimH * 0.12);

        double fillFraction = Math.Max(0.20, ProgressFraction);
        double liquidDepth = (1.0 - fillFraction) * (isMini ? 4.0 : 8.0);
        Point liquidCenter = new Point(rimCenter.X, rimCenter.Y + liquidDepth);
        var liquidClip = new EllipseGeometry(rimCenter, w * 0.45, rimH * 0.46);
        dc.PushClip(liquidClip);

        var coffeeBrush = new LinearGradientBrush(
            Color.FromRgb(68, 32, 16),
            Color.FromRgb(46, 20, 10),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawEllipse(coffeeBrush, null, liquidCenter, w * 0.44, rimH * 0.42);

        var cremaBrush = new RadialGradientBrush(
            Color.FromRgb(225, 178, 118),
            Color.FromRgb(108, 52, 28));
        dc.DrawEllipse(cremaBrush, null, liquidCenter, w * 0.40, rimH * 0.34);

        var foamPen = new Pen(new SolidColorBrush(Color.FromRgb(252, 238, 218)), isMini ? 1.0 : 1.4)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        double latteW = w * 0.24;
        double latteH = rimH * 0.55;

        if (IsRestPhase)
        {
            for (int i = 0; i < 4; i++)
            {
                double spread = 1.0 - (i * 0.18);
                var arc = new PathGeometry();
                var af = new PathFigure { StartPoint = new Point(liquidCenter.X - (latteW * spread), liquidCenter.Y + (i * 0.4)) };
                af.Segments.Add(new QuadraticBezierSegment(new Point(liquidCenter.X, liquidCenter.Y - (latteH * spread)), new Point(liquidCenter.X + (latteW * spread), liquidCenter.Y + (i * 0.4)), true));
                arc.Figures.Add(af);
                dc.DrawGeometry(null, foamPen, arc);
            }
            dc.DrawLine(foamPen, new Point(liquidCenter.X, liquidCenter.Y - latteH), new Point(liquidCenter.X, liquidCenter.Y + (latteH * 0.65)));
        }
        else
        {
            DrawHeart(dc, liquidCenter.X, liquidCenter.Y + (isGoal ? -0.6 : 0), (w * 0.18) * (isGoal ? 1.1 : 1.0), new SolidColorBrush(Color.FromRgb(255, 242, 225)));
            DrawHeart(dc, liquidCenter.X, liquidCenter.Y + (isGoal ? -0.4 : 0), (w * 0.10), new SolidColorBrush(Color.FromRgb(150, 82, 44)));
            if (!isMini)
            {
                var sideArc = new PathGeometry();
                var sf = new PathFigure { StartPoint = new Point(liquidCenter.X - (w * 0.24), liquidCenter.Y + 1.0) };
                sf.Segments.Add(new QuadraticBezierSegment(new Point(liquidCenter.X, liquidCenter.Y - (rimH * 0.45)), new Point(liquidCenter.X + (w * 0.24), liquidCenter.Y + 1.0), true));
                sideArc.Figures.Add(sf);
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(180, 248, 232, 212)), 1.1), sideArc);
            }
        }

        dc.Pop();
    }

    private void DrawTaskCompleteFingerWriting(DrawingContext dc, double w, double h, double mugX, bool isMini)
    {
        double centerX = isMini ? (w * 0.38) : Math.Max(w * 0.36, (mugX * 0.48));
        Point center = new Point(centerX, h * 0.46);

        // Clear Finger-Wiped Backdrop (Revealing warm glowing lights behind condensation)
        double glowW = isMini ? 150 : 290;
        double glowH = isMini ? 45 : 75;
        var wipedGlow = new RadialGradientBrush(
            Color.FromArgb(95, 255, 225, 140),
            Color.FromArgb(0, 255, 200, 80));
        dc.DrawEllipse(wipedGlow, null, center, glowW * 0.5, glowH * 0.5);

        // Finger-Drawn Heart ♡ Beside Text
        double heartSize = isMini ? 7 : 13;
        DrawHeart(dc, center.X - (isMini ? 55 : 115), center.Y - 2, heartSize, new SolidColorBrush(Color.FromRgb(255, 140, 175)));
        DrawHeart(dc, center.X + (isMini ? 55 : 115), center.Y - 2, heartSize, new SolidColorBrush(Color.FromRgb(255, 140, 175)));

        // Main Finger-Wiped "Good Job!" Text
        string title = isMini ? "✨ Good Job! ✨" : "✨ GOOD JOB! ✨";
        var titleFt = CreateText(
            title,
            isMini ? 11 : 18,
            new SolidColorBrush(Color.FromRgb(255, 238, 150)),
            FontWeights.Bold);

        // Shadow / Glow outline
        var shadowFt = CreateText(
            title,
            isMini ? 11 : 18,
            new SolidColorBrush(Color.FromArgb(180, 255, 180, 80)),
            FontWeights.Bold);
        dc.DrawText(shadowFt, new Point(center.X - (titleFt.Width * 0.5) + 1, center.Y - (titleFt.Height * 0.5) + 1));
        dc.DrawText(titleFt, new Point(center.X - (titleFt.Width * 0.5), center.Y - (titleFt.Height * 0.5)));

        // Subtitle / Celebration on Full view
        if (!isMini)
        {
            string sub = "☕ Fresh Warm Brew Ready • Outstanding Focus! 🌟";
            var subFt = CreateText(sub, 10.5, new SolidColorBrush(Color.FromRgb(255, 248, 220)), FontWeights.SemiBold);
            dc.DrawText(subFt, new Point(center.X - (subFt.Width * 0.5), center.Y + (titleFt.Height * 0.5) + 3));

            // Floating Celebration Sparkles in condensation
            var rand = new Random(888 + (_frameTick / 3));
            for (int s = 0; s < 5; s++)
            {
                double sx = center.X - 100 + rand.Next(200);
                double sy = center.Y - 22 + rand.Next(50);
                DrawSparkle(dc, sx, sy, 3.5, new SolidColorBrush(Color.FromRgb(255, 235, 140)));
            }
        }
    }

    private void DrawRestFingerWriting(DrawingContext dc, double w, double h, double mugX, bool isMini)
    {
        double centerX = isMini ? (w * 0.38) : Math.Max(w * 0.36, (mugX * 0.48));
        Point center = new Point(centerX, h * 0.46);

        // Clear Wiped Soft Oval
        var wipedGlow = new RadialGradientBrush(
            Color.FromArgb(75, 255, 225, 140),
            Color.FromArgb(0, 255, 210, 100));
        dc.DrawEllipse(wipedGlow, null, center, isMini ? 75 : 135, isMini ? 22 : 38);

        string text = isMini ? "🌧️ ☕ Rest & Breathe" : "🌧️ ☕ Rainy Rest Time • Breathe & Relax ✨";
        var ft = CreateText(text, isMini ? 10.5 : 13.5, new SolidColorBrush(Color.FromRgb(255, 230, 160)), FontWeights.Bold);
        dc.DrawText(ft, new Point(center.X - (ft.Width * 0.5), center.Y - (ft.Height * 0.5)));
    }

    private void DrawFogProgressHint(DrawingContext dc, double w, double h, double mugX, double progress, bool isMini)
    {
        int pct = (int)(progress * 100);
        double centerX = isMini ? (w * 0.38) : Math.Max(w * 0.36, (mugX * 0.48));
        Point center = new Point(centerX, h * 0.42);

        double pulse = Math.Sin(_frameTick * 0.15) * 0.2 + 0.8;
        string text = isMini ? $"☕ {pct}%" : $"☕ {pct}% Brewed • Condensation Mist Rising...";
        var ft = CreateText(text, isMini ? 9.5 : 11.5, new SolidColorBrush(Color.FromArgb((byte)(190 * pulse), 255, 240, 200)), FontWeights.SemiBold);
        dc.DrawText(ft, new Point(center.X - (ft.Width * 0.5), center.Y - (ft.Height * 0.5)));
    }


    private void DrawCafeWindowBackdropDetails(DrawingContext dc, double w, double windowH)
    {
        double skylineY = windowH * 0.74;
        var silhouetteBrush = new SolidColorBrush(Color.FromArgb(110, 32, 28, 40));
        var windowBrush = new SolidColorBrush(Color.FromArgb(95, 255, 214, 122));

        double[] widths = { 40, 28, 34, 22, 45, 30 };
        double[] heights = { 0.22, 0.18, 0.26, 0.16, 0.24, 0.20 };
        double bx = -8;
        for (int i = 0; i < widths.Length; i++)
        {
            double bw = widths[i];
            double bh = windowH * heights[i];
            double by = skylineY - bh;
            dc.DrawRectangle(silhouetteBrush, null, new Rect(bx, by, bw, bh));

            if (!IsMiniMode)
            {
                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 2; col++)
                    {
                        if (((row + col + i) % 2) == 0)
                        {
                            dc.DrawRectangle(windowBrush, null, new Rect(bx + 5 + (col * 8), by + 6 + (row * 7), 3, 2));
                        }
                    }
                }
            }

            bx += bw + 14;
            if (bx > w + 10) break;
        }

        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(80, 58, 40, 34)), null, new Rect(w * 0.08, windowH * 0.58, w * 0.24, 16), 8, 8);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(70, 72, 48, 34)), null, new Point(w * 0.23, windowH * 0.56), 10, 5);

        if (IsMiniMode)
        {
            return;
        }

        var stemPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 68, 112, 84)), 1.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        double hangX = w * 0.17;
        dc.DrawLine(stemPen, new Point(hangX, 0), new Point(hangX, 44));
        for (int i = 0; i < 5; i++)
        {
            double ly = 14 + (i * 8);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 68, 118, 92)), null, new Point(hangX - 6 - (i % 2), ly), 5, 2.7);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 78, 132, 102)), null, new Point(hangX + 6 + (i % 2), ly + 1), 5, 2.7);
        }

        double shelfX = w * 0.67;
        double shelfY = windowH * 0.20;
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(110, 86, 58, 42)), 2.0), new Point(shelfX, shelfY), new Point(shelfX + 60, shelfY));
        for (int i = 0; i < 5; i++)
        {
            double bookX = shelfX + 4 + (i * 10);
            double bookH = 12 + (i % 3) * 4;
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(95, (byte)(145 + i * 12), (byte)(95 + i * 10), (byte)(82 + i * 6))), null, new Rect(bookX, shelfY - bookH, 7, bookH));
        }

        double lampGlow = Math.Sin(_frameTick * 0.11) * 0.18 + 0.82;
        Point lampCenter = new Point(w * 0.30, windowH * 0.70);
        var boothGlow = new RadialGradientBrush(Color.FromArgb((byte)(70 * lampGlow), 255, 204, 130), Color.FromArgb(0, 255, 204, 130));
        dc.DrawEllipse(boothGlow, null, lampCenter, 42, 28);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(105, 72, 42, 28)), null, new Rect(lampCenter.X - 8, lampCenter.Y + 4, 16, 8), 2, 2);
    }

    private void DrawCafeFocusLampReflection(DrawingContext dc, double w, double windowH)
    {
        Point lampPoint = new Point(w * 0.24, 18);
        double glowPulse = Math.Sin(_frameTick * 0.10) * 0.12 + 0.88;
        var coneBrush = new LinearGradientBrush(
            Color.FromArgb((byte)(80 * glowPulse), 255, 216, 145),
            Color.FromArgb(0, 255, 216, 145),
            new Point(0.5, 0),
            new Point(0.5, 1));

        var coneGeom = new PathGeometry();
        var cf = new PathFigure { StartPoint = new Point(lampPoint.X - 10, lampPoint.Y + 5) };
        cf.Segments.Add(new LineSegment(new Point(lampPoint.X + 10, lampPoint.Y + 5), true));
        cf.Segments.Add(new LineSegment(new Point(lampPoint.X + 58, windowH * 0.82), true));
        cf.Segments.Add(new LineSegment(new Point(lampPoint.X - 44, windowH * 0.82), true));
        cf.IsClosed = true;
        coneGeom.Figures.Add(cf);
        dc.DrawGeometry(coneBrush, null, coneGeom);

        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(145, 90, 62, 42)), null, new Rect(lampPoint.X - 10, lampPoint.Y - 2, 20, 7), 2, 2);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(140, 96, 72, 58)), 1.1), new Point(lampPoint.X, 0), new Point(lampPoint.X, lampPoint.Y - 1));
    }

    private void DrawCafeSillCandle(DrawingContext dc, double x, double sillY, bool isRestPhase)
    {
        double flamePulse = Math.Sin(_frameTick * 0.22) * 0.18 + 0.82;
        Point candleCenter = new Point(x, sillY - 5);
        var glow = new RadialGradientBrush(
            Color.FromArgb((byte)((isRestPhase ? 105 : 82) * flamePulse), 255, 205, 128),
            Color.FromArgb(0, 255, 205, 128));
        dc.DrawEllipse(glow, null, candleCenter, 18, 12);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(150, 220, 190, 160)), new Pen(new SolidColorBrush(Color.FromArgb(120, 180, 152, 128)), 0.8), new Rect(x - 5, sillY - 9, 10, 7), 2, 2);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 238, 210)), null, new Point(x, sillY - 8), 3.4, 1.1);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 208, 92)), null, new Point(x, sillY - 12.5), 2.1, 3.3);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 244, 210)), null, new Point(x, sillY - 13.3), 0.9, 1.7);
    }

    private void DrawCafeSaucerProps(DrawingContext dc, double saucerX, double saucerY, double saucerW, double saucerH, double sillY)
    {
        double spoonY = saucerY + (saucerH * 0.18);
        var spoonPen = new Pen(new SolidColorBrush(Color.FromRgb(210, 208, 202)), 1.8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(spoonPen, new Point(saucerX + (saucerW * 0.70), spoonY), new Point(saucerX + saucerW + 10, spoonY + 8));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(228, 224, 214)), null, new Point(saucerX + (saucerW * 0.68), spoonY), 4.0, 2.2);

        double biscuitX = saucerX + (saucerW * 0.12);
        double biscuitY = sillY - 8;
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(213, 176, 118)), new Pen(new SolidColorBrush(Color.FromRgb(170, 126, 84)), 0.8), new Rect(biscuitX, biscuitY, 18, 10), 2, 2);
        for (int i = 0; i < 4; i++)
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(120, 72, 40)), null, new Point(biscuitX + 4 + (i * 4), biscuitY + 3 + ((i % 2) * 2)), 0.9, 0.9);
        }

        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(248, 244, 236)), new Pen(new SolidColorBrush(Color.FromRgb(222, 214, 205)), 0.6), new Rect(biscuitX + 22, biscuitY + 1.5, 7, 7), 1.2, 1.2);
    }

    private void DrawCafeFocusJournal(DrawingContext dc, double x, double sillY)
    {
        double journalY = sillY - 13;
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(228, 214, 186)), new Pen(new SolidColorBrush(Color.FromRgb(168, 146, 112)), 0.9), new Rect(x, journalY, 28, 11), 2, 2);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(236, 224, 198)), null, new Rect(x + 2, journalY + 1, 12, 9), 1.5, 1.5);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(242, 228, 202)), null, new Rect(x + 14, journalY + 1, 12, 9), 1.5, 1.5);
        var linePen = new Pen(new SolidColorBrush(Color.FromArgb(90, 120, 96, 80)), 0.6);
        for (int i = 0; i < 3; i++)
        {
            double ly = journalY + 3 + (i * 2.5);
            dc.DrawLine(linePen, new Point(x + 4, ly), new Point(x + 11, ly));
            dc.DrawLine(linePen, new Point(x + 17, ly), new Point(x + 24, ly));
        }

        var pencilPen = new Pen(new SolidColorBrush(Color.FromRgb(230, 182, 78)), 1.6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(pencilPen, new Point(x + 24, journalY - 2), new Point(x + 31, journalY + 10));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(88, 58, 42)), null, new Point(x + 31, journalY + 10), 1.0, 1.0);
    }

    private void DrawCafeRestBookAndCat(DrawingContext dc, double x, double sillY)
    {
        double bookY = sillY - 11;
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(204, 156, 104)), new Pen(new SolidColorBrush(Color.FromRgb(156, 112, 76)), 0.8), new Rect(x - 10, bookY + 1, 20, 8), 2, 2);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(240, 228, 205)), null, new Rect(x - 8, bookY + 2, 16, 6), 1.3, 1.3);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(80, 136, 106, 84)), 0.5), new Point(x, bookY + 2), new Point(x, bookY + 8));

        var catBrush = new SolidColorBrush(Color.FromRgb(92, 78, 70));
        dc.DrawEllipse(catBrush, null, new Point(x + 18, sillY - 6), 9, 5.5);
        dc.DrawEllipse(catBrush, null, new Point(x + 27, sillY - 8), 4.5, 4.2);

        var earGeom = new PathGeometry();
        var ef = new PathFigure { StartPoint = new Point(x + 24, sillY - 11) };
        ef.Segments.Add(new LineSegment(new Point(x + 26.5, sillY - 15), true));
        ef.Segments.Add(new LineSegment(new Point(x + 28.2, sillY - 10.8), true));
        ef.IsClosed = true;
        earGeom.Figures.Add(ef);
        dc.DrawGeometry(catBrush, null, earGeom);

        var earGeom2 = new PathGeometry();
        var ef2 = new PathFigure { StartPoint = new Point(x + 28.2, sillY - 10.8) };
        ef2.Segments.Add(new LineSegment(new Point(x + 30.5, sillY - 15), true));
        ef2.Segments.Add(new LineSegment(new Point(x + 32.3, sillY - 11.0), true));
        ef2.IsClosed = true;
        earGeom2.Figures.Add(ef2);
        dc.DrawGeometry(catBrush, null, earGeom2);

        var tailPen = new Pen(catBrush, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var tailGeom = new PathGeometry();
        var tf = new PathFigure { StartPoint = new Point(x + 11, sillY - 5) };
        tf.Segments.Add(new QuadraticBezierSegment(new Point(x + 4, sillY - 12), new Point(x + 8, sillY - 2), true));
        tailGeom.Figures.Add(tf);
        dc.DrawGeometry(null, tailPen, tailGeom);
    }

    private void DrawCafeRestGlow(DrawingContext dc, double w, double windowH, Point center, double glowW, double glowH)
    {
        var glow = new RadialGradientBrush(Color.FromArgb(82, 255, 214, 146), Color.FromArgb(0, 255, 214, 146));
        dc.DrawEllipse(glow, null, center, glowW * 0.5, glowH * 0.5);
    }

    private void DrawCafeGoalCelebrationAmbience(DrawingContext dc, double w, double windowH, double centerX, double centerY)
    {
        var burst = new RadialGradientBrush(Color.FromArgb(85, 255, 226, 150), Color.FromArgb(0, 255, 226, 150));
        dc.DrawEllipse(burst, null, new Point(centerX, centerY + 18), 74, 52);

        for (int i = 0; i < (IsMiniMode ? 5 : 12); i++)
        {
            double sx = (w * 0.12) + ((i * 31.0) % (w * 0.76));
            double sy = (windowH * 0.16) + (((i * 23.0) + (_frameTick * 0.8)) % (windowH * 0.54));
            Color col = i % 3 == 0 ? Color.FromRgb(255, 229, 140) : (i % 3 == 1 ? Color.FromRgb(255, 174, 205) : Color.FromRgb(170, 228, 255));
            DrawSparkle(dc, sx, sy, IsMiniMode ? 2.1 : 3.3, new SolidColorBrush(Color.FromArgb(185, col.R, col.G, col.B)));
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(110, col.R, col.G, col.B)), null, new Point(sx + 5, sy + 3), 1.6, 2.2);
        }
    }

    #endregion

    #region 🎷 Scene: Coffee Jazz Window (Playlist Cover)

    /// <summary>
    /// A "playlist cover" scene (like a chill lo-fi/jazz YouTube stream thumbnail) that fills the
    /// whole companion canvas: an autumn lakeside window view above a latte on a warm wooden
    /// table, with drifting golden sparkle dust.
    /// </summary>
        private void RenderCoffeeJazzScene(DrawingContext dc, double w, double h)
    {
        bool isMini = IsMiniMode;
        var cardRect = new Rect(0, 0, w, h);

        double viewH = h * 0.60;
        DrawCoffeeJazzLakeView(dc, cardRect, viewH, isMini);
        DrawCoffeeJazzWindowFrame(dc, cardRect, viewH, isMini);
        DrawCoffeeJazzGlassReflections(dc, cardRect, viewH, isMini);
        DrawCoffeeJazzTable(dc, cardRect, viewH, isMini);
        DrawCoffeeJazzSparkles(dc, cardRect, isMini);

        if (IsRestPhase)
        {
            DrawCoffeeJazzRestAccent(dc, cardRect, viewH, isMini);
        }
        else if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawCoffeeJazzGoalAccent(dc, cardRect, viewH, isMini);
        }
    }

        private void DrawCoffeeJazzLakeView(DrawingContext dc, Rect card, double viewH, bool isMini)
    {
        bool isGoal = IsGoalReached || ProgressFraction >= 0.999;
        double dusk = ProgressFraction;

        Color skyTop = LerpColor(Color.FromRgb(140, 185, 222), Color.FromRgb(232, 178, 140), dusk);
        Color skyBottom = LerpColor(Color.FromRgb(210, 190, 165), Color.FromRgb(250, 200, 150), dusk);

        if (IsRestPhase)
        {
            skyTop = LerpColor(skyTop, Color.FromRgb(154, 120, 118), 0.38);
            skyBottom = LerpColor(skyBottom, Color.FromRgb(220, 178, 150), 0.28);
        }
        else if (isGoal)
        {
            skyTop = LerpColor(skyTop, Color.FromRgb(250, 186, 132), 0.18);
            skyBottom = LerpColor(skyBottom, Color.FromRgb(255, 220, 170), 0.24);
        }

        var skyBrush = new LinearGradientBrush(skyTop, skyBottom, new Point(0, 0), new Point(0, 1));
        var viewRect = new Rect(card.X, card.Y, card.Width, viewH);
        dc.DrawRectangle(skyBrush, null, viewRect);

        var sunGlow = new RadialGradientBrush(Color.FromArgb((byte)(isGoal ? 175 : 140), 255, 235, 190), Color.FromArgb(0, 255, 235, 190));
        Point sunPoint = new Point(card.X + (card.Width * 0.66), card.Y + (viewH * 0.22));
        dc.DrawEllipse(sunGlow, null, sunPoint, card.Width * 0.55, viewH * 0.4);
        if (!isMini)
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 255, 225, 170)), null, sunPoint, 28, 18);
        }

        double horizonY = card.Y + (viewH * 0.70);
        var treeRand = new Random(11);
        Color[] foliage =
        {
            Color.FromRgb(196, 122, 58), Color.FromRgb(214, 156, 66), Color.FromRgb(150, 108, 58),
            Color.FromRgb(120, 96, 56), Color.FromRgb(168, 130, 70)
        };

        for (double tx = card.X - 6; tx < card.X + card.Width + 6; tx += (isMini ? 10 : 7))
        {
            double th = 8 + (treeRand.NextDouble() * (isMini ? 8 : 16));
            var tBrush = new SolidColorBrush(foliage[treeRand.Next(foliage.Length)]);
            dc.DrawEllipse(tBrush, null, new Point(tx, horizonY - (th * 0.4)), isMini ? 6 : 9, th);
        }

        for (int i = 0; i < (isMini ? 3 : 6); i++)
        {
            double hutX = card.X + (card.Width * 0.08) + (i * (card.Width * 0.13));
            double hutY = horizonY - 4 - ((i % 2) * 2);
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 70, 48, 34)), null, new Rect(hutX, hutY, isMini ? 4 : 6, isMini ? 3 : 4));
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(140, 255, 214, 120)), null, new Rect(hutX + 1, hutY + 0.8, isMini ? 2 : 3, 1.2));
        }

        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(60, 50, 42)), null, new Rect(card.X, horizonY - 1, card.Width, 2));

        var waterBrush = new LinearGradientBrush(LerpColor(skyBottom, Color.FromRgb(150, 120, 95), 0.3), Color.FromRgb(70, 55, 48), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(waterBrush, null, new Rect(card.X, horizonY, card.Width, viewH - (horizonY - card.Y)));

        DrawCoffeeJazzLakeRipples(dc, card, viewH, horizonY, isMini);
        DrawCoffeeJazzBoat(dc, card, horizonY, isMini);

        if (!isMini)
        {
            DrawCoffeeJazzBirds(dc, card, viewH);
            DrawCoffeeJazzFallingLeaves(dc, card, viewH, false);
        }
        else if ((IsTracking || isGoal) && _frameTick % 30 < 15)
        {
            DrawCoffeeJazzFallingLeaves(dc, card, viewH, true);
        }
    }

        private void DrawCoffeeJazzWindowFrame(DrawingContext dc, Rect card, double viewH, bool isMini)
    {
        var woodBrush = new LinearGradientBrush(Color.FromRgb(120, 78, 48), Color.FromRgb(70, 44, 26), new Point(0, 0), new Point(1, 1));
        double frameT = isMini ? 5 : 10;

        dc.DrawRectangle(woodBrush, null, new Rect(card.X, card.Y, card.Width, frameT));
        dc.DrawRectangle(woodBrush, null, new Rect(card.X + (card.Width * 0.78), card.Y, frameT * 0.8, viewH));
        dc.DrawRectangle(woodBrush, null, new Rect(card.X, viewH - (frameT * 0.45), card.Width, frameT * 0.45));

        if (!isMini)
        {
            var grainPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 255, 210, 170)), 0.8);
            dc.DrawLine(grainPen, new Point(card.X, card.Y + (frameT * 0.5)), new Point(card.X + card.Width, card.Y + (frameT * 0.5)));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(55, 45, 24, 12)), 1.0), new Point(card.X + (card.Width * 0.78) + frameT * 0.8, card.Y), new Point(card.X + (card.Width * 0.78) + frameT * 0.8, viewH));
        }
    }

        private void DrawCoffeeJazzTable(DrawingContext dc, Rect card, double viewH, bool isMini)
    {
        GetCoffeeJazzCupLayout(card, viewH, isMini, out double tableY, out double tableH, out double cupX, out double cupY, out double cupW, out double cupH);
        var tableRect = new Rect(card.X, tableY, card.Width, tableH);

        var tableBrush = new LinearGradientBrush(Color.FromRgb(150, 100, 62), Color.FromRgb(96, 60, 36), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(tableBrush, null, tableRect);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(150, 235, 190, 140)), 1.4), new Point(card.X, tableY), new Point(card.X + card.Width, tableY));

        var grainRand = new Random(3);
        for (int i = 0; i < (isMini ? 3 : 7); i++)
        {
            double gy = tableY + 4 + (grainRand.NextDouble() * Math.Max(2, tableH - 8));
            var gBrush = new SolidColorBrush(Color.FromArgb(40, 60, 35, 20));
            dc.DrawLine(new Pen(gBrush, 1.0), new Point(card.X, gy), new Point(card.X + card.Width, gy + (grainRand.NextDouble() * 3 - 1.5)));
        }

        if (IsRestPhase && !isMini)
        {
            DrawCoffeeJazzRestScarf(dc, card, tableY, tableH);
        }

        DrawCoffeeJazzCup(dc, cupX, cupY, cupW, cupH, isMini);
        DrawMugSteamWisps(dc, cupX + (cupW * 0.5), cupY, cupW, cupH, isMini);
    }

        private void DrawCoffeeJazzCup(DrawingContext dc, double x, double y, double cupW, double cupH, bool isMini)
    {
        bool isGoal = IsGoalReached || ProgressFraction >= 0.999;
        double saucerW = cupW * 1.45;
        double saucerH = cupH * 0.28;
        double saucerX = x + ((cupW - saucerW) * 0.5);
        double saucerY = y + cupH - (saucerH * 0.55);

        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(110, 20, 10, 5)), null, new Point(saucerX + (saucerW * 0.5), saucerY + saucerH - 1), saucerW * 0.52, saucerH * 0.5);

        var ceramicBrush = new LinearGradientBrush(Color.FromRgb(238, 226, 208), Color.FromRgb(205, 188, 165), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(ceramicBrush, new Pen(new SolidColorBrush(Color.FromRgb(160, 140, 115)), 1), new Rect(saucerX, saucerY, saucerW, saucerH), saucerH * 0.5, saucerH * 0.5);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(45, 95, 72, 52)), null, new Point(saucerX + (saucerW * 0.5), saucerY + (saucerH * 0.45)), saucerW * 0.32, saucerH * 0.22);

        var cupBrush = new LinearGradientBrush(Color.FromRgb(232, 218, 198), Color.FromRgb(196, 178, 152), new Point(0, 0), new Point(0, 1));
        var cupPen = new Pen(new SolidColorBrush(Color.FromRgb(150, 130, 105)), 1.2);
        dc.DrawRoundedRectangle(cupBrush, cupPen, new Rect(x, y, cupW, cupH), cupH * 0.28, cupH * 0.28);

        if (!isMini)
        {
            for (int i = 0; i < 7; i++)
            {
                double speckX = x + (cupW * 0.14) + ((i * 13.0) % (cupW * 0.62));
                double speckY = y + (cupH * 0.18) + ((i * 9.0) % (cupH * 0.42));
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(55, 145, 126, 100)), null, new Point(speckX, speckY), 0.9, 0.9);
            }
        }

        var handleGeom = new PathGeometry();
        var hf = new PathFigure { StartPoint = new Point(x + cupW, y + (cupH * 0.28)) };
        hf.Segments.Add(new BezierSegment(
            new Point(x + cupW + (cupW * 0.32), y + (cupH * 0.1)),
            new Point(x + cupW + (cupW * 0.32), y + (cupH * 0.85)),
            new Point(x + cupW, y + (cupH * 0.66)),
            true));
        handleGeom.Figures.Add(hf);
        dc.DrawGeometry(null, new Pen(cupBrush, cupH * 0.16) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, handleGeom);

        double foamW = cupW * 0.82;
        double foamH = cupH * 0.42;
        double foamX = x + ((cupW - foamW) * 0.5);
        double foamY = y + (cupH * 0.06);
        Point foamCenter = new Point(foamX + (foamW * 0.5), foamY + (foamH * 0.5));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(214, 170, 120)), null, foamCenter, foamW * 0.5, foamH * 0.5);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(55, 255, 244, 226)), null, new Point(foamCenter.X - (foamW * 0.15), foamCenter.Y - (foamH * 0.15)), foamW * 0.20, foamH * 0.14);

        var foamPen = new Pen(new SolidColorBrush(Color.FromRgb(248, 236, 216)), isMini ? 1.4 : 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        double rx = foamCenter.X;
        double ry = foamCenter.Y + (foamH * 0.08);
        for (int i = 0; i < 4; i++)
        {
            double s = 1.0 - (i * 0.2);
            dc.DrawLine(foamPen, new Point(rx, ry - (foamH * 0.42 * s)), new Point(rx, ry + (foamH * 0.05)));
            var arcGeom = new PathGeometry();
            var af = new PathFigure { StartPoint = new Point(rx - (foamW * 0.28 * s), ry - (foamH * 0.05 * s)) };
            af.Segments.Add(new QuadraticBezierSegment(new Point(rx, ry - (foamH * 0.34 * s)), new Point(rx + (foamW * 0.28 * s), ry - (foamH * 0.05 * s)), true));
            arcGeom.Figures.Add(af);
            dc.DrawGeometry(null, foamPen, arcGeom);
        }

        if (!isMini)
        {
            double spoonY = saucerY + (saucerH * 0.15);
            var spoonPen = new Pen(new SolidColorBrush(Color.FromRgb(210, 205, 198)), 1.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            dc.DrawLine(spoonPen, new Point(saucerX + (saucerW * 0.68), spoonY), new Point(saucerX + saucerW - 6, spoonY + 5));
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(225, 220, 212)), null, new Point(saucerX + (saucerW * 0.66), spoonY), 3.3, 1.8);
        }

        if (isGoal)
        {
            DrawSparkle(dc, x + (cupW * 0.82), y + (cupH * 0.18), isMini ? 2.0 : 3.0, new SolidColorBrush(Color.FromRgb(255, 228, 145)));
        }
    }

        private void DrawCoffeeJazzSparkles(DrawingContext dc, Rect card, bool isMini)
    {
        bool isGoal = IsGoalReached || ProgressFraction >= 0.999;
        int count = isMini ? 6 : (isGoal ? 20 : 14);
        Color baseColor = IsRestPhase ? Color.FromRgb(255, 214, 176) : (isGoal ? Color.FromRgb(255, 238, 186) : Color.FromRgb(255, 236, 196));

        for (int i = 0; i < count; i++)
        {
            double seedX = (i * 53.7) % card.Width;
            double speed = 0.35 + ((i % 3) * 0.18) + (isGoal ? 0.04 : 0.0);
            double travel = ((_frameTick * speed) + (i * 40)) % (card.Height + 20);
            double sx = card.X + seedX + (Math.Sin((_frameTick * 0.04) + i) * (isGoal ? 11 : 8));
            double sy = card.Y + card.Height - travel;
            double alpha = Math.Clamp(Math.Sin((travel / (card.Height + 20)) * Math.PI), 0.05, 1.0);
            var dustBrush = new SolidColorBrush(Color.FromArgb((byte)(185 * alpha), baseColor.R, baseColor.G, baseColor.B));
            dc.DrawEllipse(dustBrush, null, new Point(sx, sy), 1.4 + (isGoal && i % 4 == 0 ? 0.7 : 0), 1.4 + (isGoal && i % 4 == 0 ? 0.7 : 0));

            if (!isMini && i % 5 == 0)
            {
                DrawSparkle(dc, sx, sy, isGoal ? 3.4 : 2.2, new SolidColorBrush(Color.FromArgb((byte)(145 * alpha), baseColor.R, baseColor.G, baseColor.B)));
            }
        }
    }


    private void GetCoffeeJazzCupLayout(Rect card, double viewH, bool isMini, out double tableY, out double tableH, out double cupX, out double cupY, out double cupW, out double cupH)
    {
        tableY = card.Y + viewH;
        tableH = card.Height - viewH;
        cupH = Math.Min(tableH * 0.82, card.Height * 0.42);
        cupW = cupH / 0.62;
        double maxCupW = card.Width * (isMini ? 0.30 : 0.26);
        if (cupW > maxCupW)
        {
            cupW = maxCupW;
            cupH = cupW * 0.62;
        }

        cupX = card.X + (card.Width * 0.33) - (cupW * 0.5);
        double saucerH = cupH * 0.28;
        double saucerBottomOffset = cupH + (saucerH * 0.45);
        cupY = tableY + (tableH * 0.94) - saucerBottomOffset;
    }

    private void DrawCoffeeJazzLakeRipples(DrawingContext dc, Rect card, double viewH, double horizonY, bool isMini)
    {
        for (int i = 0; i < (isMini ? 4 : 8); i++)
        {
            double sy = horizonY + 3 + (i * (isMini ? 4 : 6));
            if (sy > card.Y + viewH) break;
            double shimmer = Math.Sin((_frameTick * 0.05) + i) * 0.5 + 0.5;
            var shimmerBrush = new SolidColorBrush(Color.FromArgb((byte)(65 * shimmer), 255, 235, 210));
            dc.DrawLine(new Pen(shimmerBrush, 1.2), new Point(card.X + (card.Width * 0.1), sy), new Point(card.X + (card.Width * (0.55 + (0.08 * Math.Sin(i)))), sy));

            if (!isMini)
            {
                double rippleX = card.X + (card.Width * (0.18 + ((i * 0.11) % 0.55))) + (Math.Sin((_frameTick * 0.03) + i) * 8);
                double rippleW = 16 + (i * 4);
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(58, 255, 229, 198)), 0.8), new Point(rippleX, sy + 4), rippleW * 0.5, 2.5);
            }
        }
    }

    private void DrawCoffeeJazzBoat(DrawingContext dc, Rect card, double horizonY, bool isMini)
    {
        double travel = ((_frameTick * 0.20) % (card.Width + 50)) - 20;
        double boatX = card.X + (card.Width * 0.15) + travel;
        if (boatX > card.Right + 30)
        {
            boatX -= card.Width + 80;
        }

        double boatY = horizonY + (card.Height * 0.065);
        double scale = isMini ? 0.65 : 1.0;
        var hullBrush = new SolidColorBrush(Color.FromArgb(150, 72, 48, 42));
        var hullGeom = new PathGeometry();
        var hf = new PathFigure { StartPoint = new Point(boatX, boatY) };
        hf.Segments.Add(new LineSegment(new Point(boatX + (16 * scale), boatY), true));
        hf.Segments.Add(new LineSegment(new Point(boatX + (13 * scale), boatY + (4 * scale)), true));
        hf.Segments.Add(new LineSegment(new Point(boatX + (3 * scale), boatY + (4 * scale)), true));
        hf.IsClosed = true;
        hullGeom.Figures.Add(hf);
        dc.DrawGeometry(hullBrush, null, hullGeom);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(145, 232, 220, 190)), 0.9), new Point(boatX + (8 * scale), boatY), new Point(boatX + (8 * scale), boatY - (10 * scale)));
        dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(125, 250, 236, 202)), null, new PathGeometry(new[]
        {
            new PathFigure
            {
                StartPoint = new Point(boatX + (8 * scale), boatY - (10 * scale)),
                Segments = new PathSegmentCollection
                {
                    new LineSegment(new Point(boatX + (15 * scale), boatY - (5 * scale)), true),
                    new LineSegment(new Point(boatX + (8 * scale), boatY - (3 * scale)), true)
                },
                IsClosed = true
            }
        }));
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(40, 255, 240, 220)), null, new Point(boatX + (8 * scale), boatY + 4), 15 * scale, 2.4 * scale);
    }

    private void DrawCoffeeJazzBirds(DrawingContext dc, Rect card, double viewH)
    {
        var birdPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 96, 82, 88)), 1.1) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        for (int i = 0; i < 3; i++)
        {
            double bx = card.X + (((_frameTick * 0.55) + (i * 90)) % (card.Width + 60)) - 30;
            double by = card.Y + (viewH * (0.20 + (i * 0.08))) + (Math.Sin((_frameTick * 0.12) + i) * 5);
            double flap = Math.Sin((_frameTick * 0.45) + i) * 3.0;
            dc.DrawLine(birdPen, new Point(bx - 5, by + flap), new Point(bx, by));
            dc.DrawLine(birdPen, new Point(bx, by), new Point(bx + 5, by + flap));
        }
    }

    private void DrawCoffeeJazzFallingLeaves(DrawingContext dc, Rect card, double viewH, bool miniOnly)
    {
        int count = miniOnly ? 2 : 6;
        Color[] colors = { Color.FromRgb(214, 144, 76), Color.FromRgb(188, 108, 56), Color.FromRgb(228, 186, 92) };
        for (int i = 0; i < count; i++)
        {
            double lx = card.X + (((_frameTick * (0.40 + (i * 0.04))) + (i * 58)) % (card.Width + 30)) - 15;
            double ly = card.Y + (((_frameTick * (0.55 + (i * 0.05))) + (i * 26)) % Math.Max(24, viewH - 10));
            double tilt = Math.Sin((_frameTick * 0.16) + i) * 4;
            var leafBrush = new SolidColorBrush(Color.FromArgb(175, colors[i % colors.Length].R, colors[i % colors.Length].G, colors[i % colors.Length].B));
            dc.DrawEllipse(leafBrush, null, new Point(lx, ly), miniOnly ? 2.3 : 3.6, miniOnly ? 1.6 : 2.2);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(135, 120, 72, 42)), 0.8), new Point(lx - tilt * 0.2, ly - 1), new Point(lx + tilt * 0.2, ly + 2));
        }
    }

    private void DrawCoffeeJazzGlassReflections(DrawingContext dc, Rect card, double viewH, bool isMini)
    {
        var gloss = new LinearGradientBrush(Color.FromArgb(55, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), new Point(0, 0), new Point(1, 1));
        dc.DrawRoundedRectangle(gloss, null, new Rect(card.X + (card.Width * 0.04), card.Y + 14, card.Width * 0.12, viewH * 0.48), 10, 10);
        if (!isMini)
        {
            dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromArgb(35, 255, 240, 225), Color.FromArgb(0, 255, 240, 225), new Point(0, 0), new Point(1, 1)), null, new Rect(card.X + (card.Width * 0.52), card.Y + 26, card.Width * 0.08, viewH * 0.34), 8, 8);
        }
    }

    private void DrawCoffeeJazzRestScarf(DrawingContext dc, Rect card, double tableY, double tableH)
    {
        double scarfX = card.Right - 64;
        double scarfY = tableY + (tableH * 0.08);
        var scarfBrush = new LinearGradientBrush(Color.FromRgb(198, 154, 126), Color.FromRgb(148, 104, 86), new Point(0, 0), new Point(0, 1));
        var scarfGeom = new PathGeometry();
        var sf = new PathFigure { StartPoint = new Point(scarfX, scarfY) };
        sf.Segments.Add(new LineSegment(new Point(scarfX + 34, scarfY + 4), true));
        sf.Segments.Add(new LineSegment(new Point(scarfX + 42, scarfY + 24), true));
        sf.Segments.Add(new LineSegment(new Point(scarfX + 18, scarfY + 30), true));
        sf.Segments.Add(new LineSegment(new Point(scarfX + 6, scarfY + 22), true));
        sf.IsClosed = true;
        scarfGeom.Figures.Add(sf);
        dc.DrawGeometry(scarfBrush, new Pen(new SolidColorBrush(Color.FromArgb(80, 120, 82, 70)), 0.8), scarfGeom);
        for (int i = 0; i < 5; i++)
        {
            double tx = scarfX + 12 + (i * 4);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(110, 230, 205, 186)), 0.7), new Point(tx, scarfY + 26), new Point(tx, scarfY + 31));
        }
    }

    private void DrawCoffeeJazzRestAccent(DrawingContext dc, Rect card, double viewH, bool isMini)
    {
        if (!isMini)
        {
            Point catCenter = new Point(card.X + (card.Width * 0.84), viewH - 10);
            var catBrush = new SolidColorBrush(Color.FromArgb(190, 52, 44, 44));
            dc.DrawEllipse(catBrush, null, catCenter, 12, 6.5);
            dc.DrawEllipse(catBrush, null, new Point(catCenter.X + 10, catCenter.Y - 2), 5.2, 4.5);

            var ear1 = new PathGeometry();
            var e1 = new PathFigure { StartPoint = new Point(catCenter.X + 6, catCenter.Y - 4) };
            e1.Segments.Add(new LineSegment(new Point(catCenter.X + 8, catCenter.Y - 9), true));
            e1.Segments.Add(new LineSegment(new Point(catCenter.X + 10, catCenter.Y - 4), true));
            e1.IsClosed = true;
            ear1.Figures.Add(e1);
            dc.DrawGeometry(catBrush, null, ear1);

            var ear2 = new PathGeometry();
            var e2 = new PathFigure { StartPoint = new Point(catCenter.X + 10, catCenter.Y - 4) };
            e2.Segments.Add(new LineSegment(new Point(catCenter.X + 12, catCenter.Y - 9), true));
            e2.Segments.Add(new LineSegment(new Point(catCenter.X + 14, catCenter.Y - 4), true));
            e2.IsClosed = true;
            ear2.Figures.Add(e2);
            dc.DrawGeometry(catBrush, null, ear2);

            var tailPen = new Pen(catBrush, 2.0) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            var tail = new PathGeometry();
            var tf = new PathFigure { StartPoint = new Point(catCenter.X - 11, catCenter.Y) };
            tf.Segments.Add(new QuadraticBezierSegment(new Point(catCenter.X - 20, catCenter.Y - 8), new Point(catCenter.X - 15, catCenter.Y + 3), true));
            tail.Figures.Add(tf);
            dc.DrawGeometry(null, tailPen, tail);
        }

        var glow = new RadialGradientBrush(Color.FromArgb(80, 255, 210, 165), Color.FromArgb(0, 255, 210, 165));
        dc.DrawEllipse(glow, null, new Point(card.X + (card.Width * 0.78), viewH * 0.72), isMini ? 28 : 48, isMini ? 20 : 32);
    }

    private void DrawCoffeeJazzGoalAccent(DrawingContext dc, Rect card, double viewH, bool isMini)
    {
        GetCoffeeJazzCupLayout(card, viewH, isMini, out _, out _, out double cupX, out double cupY, out double cupW, out double cupH);
        double cupCenterX = cupX + (cupW * 0.5);
        double steamHeartY = cupY - (isMini ? 10 : 16);

        var burst = new RadialGradientBrush(Color.FromArgb(110, 255, 224, 152), Color.FromArgb(0, 255, 224, 152));
        dc.DrawEllipse(burst, null, new Point(card.X + (card.Width * 0.72), viewH * 0.28), card.Width * 0.28, viewH * 0.22);
        DrawHeart(dc, cupCenterX, steamHeartY, isMini ? 6.0 : 10.0, new SolidColorBrush(Color.FromArgb(200, 255, 214, 224)));

        for (int i = 0; i < (isMini ? 3 : 7); i++)
        {
            double sx = card.X + (card.Width * (0.50 + (i * 0.06)));
            double sy = viewH * (0.18 + ((i % 3) * 0.10)) + (Math.Sin((_frameTick * 0.10) + i) * 6);
            DrawSparkle(dc, sx, sy, isMini ? 2.6 : 4.0, new SolidColorBrush(Color.FromArgb(180, 255, 230, 160)));
        }
    }

    #endregion

    #region 🍦 Scene: Cute Pastel Ice Cream Truck & Children

    private void RenderIceCreamScene(DrawingContext dc, double w, double h)
    {
        double groundY = h * 0.70;

        // 1. Warm Sunny Afternoon Sky & Sunlight
        var skyBrush = new LinearGradientBrush(
            Color.FromRgb(140, 205, 255),
            Color.FromRgb(255, 245, 220),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(skyBrush, null, new Rect(0, 0, w, groundY));

        // Soft drifting summer clouds
        DrawSummerClouds(dc, w, groundY);

        // 2. Big Shady Oak Tree Canopy on Top-Left
        DrawParkTree(dc, w, groundY);

        // Pastel Bunting Flags strung between tree and top-right
        DrawPastelBuntingFlags(dc, w);

        // Cheerful sun in the top-right corner, glowing over the truck's queue area
        DrawSceneSun(dc, w, groundY);

        // Drifting butterflies and birds for a livelier sky
        DrawFlyingBirds(dc, w, groundY);
        DrawButterflies(dc, w, groundY);

        // 3. Lush Green Park Lawn & Sidewalk Path
        var grassBrush = new LinearGradientBrush(
            Color.FromRgb(115, 205, 120),
            Color.FromRgb(75, 165, 85),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(grassBrush, null, new Rect(0, groundY, w, h - groundY));

        // Paved sidewalk path
        double pathY = groundY + (IsMiniMode ? 6 : 10);
        var pathBrush = new LinearGradientBrush(
            Color.FromRgb(245, 238, 225),
            Color.FromRgb(220, 208, 190),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(pathBrush, null, new Rect(0, pathY, w, h - pathY));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(185, 168, 145)), 1.2), new Point(0, pathY), new Point(w, pathY));

        // Little flower patches dotted along the grass for extra scenery
        DrawFlowerPatch(dc, w, groundY);

        // Park bench off to the right where served kids rest with their treats
        if (!IsMiniMode)
        {
            DrawParkBench(dc, w - 46, groundY - 2, 1.0);
        }

        // 4. Draw the Cute Pastel Ice Cream Truck
        double scale = IsMiniMode ? 0.62 : 1.0;
        double truckW = 145 * scale;
        double truckH = 84 * scale;
        double baseTruckX = IsMiniMode ? 6 : 24;
        double truckY = groundY - truckH + (14 * scale);

        bool isGoalReached = IsGoalReached || ProgressFraction >= 0.999;

        // End-of-shift sequence timing (in animation frames @ ~75ms/frame):
        // 1. Kids party with their treats  2. Shutter rolls down painted "GOOD JOB"
        // 3. Shutter holds so it's readable  4. Truck drives off for the day  5. Quiet closed street
        const int partyFrames = 130;
        const int shutterCloseFrames = 45;
        const int shutterHoldFrames = 70;
        const int driveFrames = 110;
        int shutterStart = partyFrames;
        int holdStart = shutterStart + shutterCloseFrames;
        int driveStart = holdStart + shutterHoldFrames;

        int elapsed = (isGoalReached && _goalReachedStartTick >= 0) ? Math.Max(0, _frameTick - _goalReachedStartTick) : 0;

        double truckX = baseTruckX;
        double shutterProgress = 0.0;

        if (isGoalReached && elapsed >= shutterStart)
        {
            shutterProgress = elapsed < holdStart
                ? Math.Clamp((elapsed - shutterStart) / (double)shutterCloseFrames, 0.0, 1.0)
                : 1.0;
        }

        if (isGoalReached && elapsed >= driveStart)
        {
            double driveT = Math.Clamp((elapsed - driveStart) / (double)driveFrames, 0.0, 1.0);
            double eased = driveT * driveT * (3 - (2 * driveT)); // smoothstep acceleration
            truckX = baseTruckX + (eased * (w + truckW));
        }

        bool isServingNow = !isGoalReached && !IsRestPhase && IsTracking && IsIceCreamVendorServingNow();

        bool truckVisible = truckX < w + 4;
        if (truckVisible)
        {
            DrawPastelIceCreamTruck(dc, truckX, truckY, truckW, truckH, scale, shutterProgress, isServingNow);
        }

        // 5. Draw Queueing Kids, Celebration / Goodbye, Rest, or Closed-Street mode
        if (isGoalReached)
        {
            if (elapsed < shutterStart)
            {
                DrawIceCreamPartyCelebration(dc, w, groundY, truckX + truckW + (8 * scale), scale);
            }
            else if (elapsed < driveStart)
            {
                DrawIceCreamGoodbyeKids(dc, w, groundY, truckX + truckW + (8 * scale), scale);
            }
            else if (truckVisible)
            {
                DrawIceCreamGoodbyeKids(dc, w, groundY, Math.Min(truckX + truckW + (8 * scale), w - (10 * scale)), scale);
            }
            else
            {
                DrawIceCreamClosedStreet(dc, w, groundY, scale);
            }
        }
        else if (IsRestPhase)
        {
            DrawIceCreamRestPicnic(dc, w, groundY, truckX + truckW + (8 * scale), scale);
        }
        else
        {
            DrawIceCreamKidsFlow(dc, w, groundY, truckX + truckW + (8 * scale), scale, ProgressFraction, IsTracking);
        }
    }

    // Timing (in ~75ms animation frames) for a single kid's visit to the truck: walking up the
    // path, standing at the counter to get served, then walking off-screen with their cone.
    private const int KidEnterFrames = 55;
    private const int KidServeFrames = 35;
    private const int KidExitFrames = 70;
    private const int KidCycleFrames = KidEnterFrames + KidServeFrames + KidExitFrames;
    private const int KidConeRevealFrame = KidEnterFrames + (int)(KidServeFrames * 0.4);

    /// <summary>
    /// Computes where a given "kid slot" is along its walk-up / get-served / walk-off cycle at the
    /// current animation frame. Two slots run staggered so a second kid is already approaching
    /// while the first is walking away, keeping the queue feeling alive without ever fully overlapping.
    /// </summary>
    private void GetIceCreamKidSlotState(int slot, double w, double startX, double scale, out double x, out bool hasCone, out int kidDesign, out bool isBeingServedNow, out bool facingLeft)
    {
        int slotOffset = slot * (KidCycleFrames / 2);
        int globalFrame = _frameTick + slotOffset;
        int localFrame = ((globalFrame % KidCycleFrames) + KidCycleFrames) % KidCycleFrames;
        int cycleGen = (globalFrame - localFrame) / KidCycleFrames;
        kidDesign = ((cycleGen * 2) + slot) & 3;

        double xEnterStart = w + (26 * scale);
        double xCounter = startX + (6 * scale);
        double xExit = w + (30 * scale);

        isBeingServedNow = false;

        if (localFrame < KidEnterFrames)
        {
            // Walking in from the right toward the counter on the left: facing left.
            double t = localFrame / (double)KidEnterFrames;
            double eased = t * t * (3 - (2 * t));
            x = xEnterStart + (eased * (xCounter - xEnterStart));
            hasCone = false;
            facingLeft = true;
        }
        else if (localFrame < KidEnterFrames + KidServeFrames)
        {
            // Standing at the counter, facing the vendor (to the left).
            x = xCounter;
            hasCone = localFrame >= KidConeRevealFrame;
            isBeingServedNow = !hasCone;
            facingLeft = true;
        }
        else
        {
            // Walking off to the right with the treat in hand: facing right.
            double t = (localFrame - KidEnterFrames - KidServeFrames) / (double)KidExitFrames;
            double eased = t * t * (3 - (2 * t));
            x = xCounter + (eased * (xExit - xCounter));
            hasCone = true;
            facingLeft = false;
        }
    }

    /// <summary>
    /// True while at least one kid slot is standing at the counter waiting for their cone,
    /// used to trigger the vendor's hand-off arm animation on the truck itself.
    /// </summary>
    private bool IsIceCreamVendorServingNow()
    {
        for (int slot = 0; slot < 2; slot++)
        {
            GetIceCreamKidSlotState(slot, 400, 0, 1.0, out _, out _, out _, out bool isBeingServedNow, out _);
            if (isBeingServedNow) return true;
        }

        return false;
    }

    private static readonly string[] IceCreamFlavors = { "strawberry", "mint_rainbow", "chocolate", "pop" };

    private void DrawIceCreamKidsFlow(DrawingContext dc, double w, double groundY, double startX, double scale, double progress, bool isTracking)
    {
        // Idle (not tracking yet): a single kid stands happily at the counter already holding
        // a cone, facing the vendor, waiting for the timer to start.
        if (!isTracking)
        {
            DrawChibiKid(dc, startX + (6 * scale), groundY, scale, 0, true, "strawberry", facingLeft: true);
        }
        else
        {
            // How many kids are actively cycling through the truck scales with focus progress,
            // so the queue feels busier the further along the session gets.
            int activeSlots = progress < 0.4 ? 1 : 2;

            for (int slot = 0; slot < activeSlots; slot++)
            {
                GetIceCreamKidSlotState(slot, w, startX, scale, out double kx, out bool hasCone, out int kidDesign, out _, out bool facingLeft);
                if (kx > w + (40 * scale)) continue; // fully off-screen, skip drawing

                string flavor = IceCreamFlavors[kidDesign % IceCreamFlavors.Length];
                DrawChibiKid(dc, kx, groundY, scale, kidDesign, hasCone, flavor, facingLeft);

                // A wagging puppy tags along behind the second slot for extra charm
                if (slot == 1 && hasCone)
                {
                    DrawCutePuppy(dc, kx + (facingLeft ? 14 : -14) * scale, groundY, scale);
                }
            }
        }

        // Status text / hint on top right — kept generic since the queue is a continuous,
        // looping flow of kids rather than a specific progress-tied milestone.
        if (isTracking)
        {
            int pct = (int)(progress * 100);
            string hint = IsMiniMode ? $"🍦 {pct}%" : $"🍦 {pct}% Focus • Kids visiting the truck!";
            var ft = CreateText(hint, IsMiniMode ? 9.5 : 11, new SolidColorBrush(Color.FromRgb(60, 45, 30)), FontWeights.SemiBold);
            dc.DrawText(ft, new Point(w - ft.Width - (IsMiniMode ? 8 : 16), IsMiniMode ? 4 : 8));
        }
    }

    private void DrawSceneSun(DrawingContext dc, double w, double groundY)
    {
        double sunX = w - (IsMiniMode ? 16 : 34);
        double sunY = IsMiniMode ? 14 : 24;
        double pulse = Math.Sin(_frameTick * 0.08) * 0.15 + 0.85;

        var glowBrush = new RadialGradientBrush(Color.FromArgb((byte)(90 * pulse), 255, 235, 150), Color.FromArgb(0, 255, 235, 150));
        dc.DrawEllipse(glowBrush, null, new Point(sunX, sunY), 30 * pulse, 30 * pulse);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 220, 110)), new Pen(new SolidColorBrush(Color.FromRgb(255, 195, 70)), 1.2), new Point(sunX, sunY), 11, 11);

        if (!IsMiniMode)
        {
            var rayPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 220, 130)), 1.6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            for (int i = 0; i < 8; i++)
            {
                double angle = (i * Math.PI / 4) + (_frameTick * 0.01);
                double innerR = 14, outerR = 19;
                dc.DrawLine(rayPen,
                    new Point(sunX + (Math.Cos(angle) * innerR), sunY + (Math.Sin(angle) * innerR)),
                    new Point(sunX + (Math.Cos(angle) * outerR), sunY + (Math.Sin(angle) * outerR)));
            }
        }
    }

    private void DrawFlyingBirds(DrawingContext dc, double w, double groundY)
    {
        if (IsMiniMode) return;

        var birdPen = new Pen(new SolidColorBrush(Color.FromRgb(90, 75, 95)), 1.3) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        for (int i = 0; i < 3; i++)
        {
            double bx = ((_frameTick * 0.6) + (i * 90)) % (w + 60) - 30;
            double by = (groundY * 0.15) + (i * 12) + (Math.Sin((_frameTick * 0.12) + i) * 4);
            double flap = Math.Sin((_frameTick * 0.5) + i) * 3.5;

            dc.DrawLine(birdPen, new Point(bx - 6, by + flap), new Point(bx, by));
            dc.DrawLine(birdPen, new Point(bx, by), new Point(bx + 6, by + flap));
        }
    }

    private void DrawButterflies(DrawingContext dc, double w, double groundY)
    {
        if (IsMiniMode) return;

        var colors = new[] { Color.FromRgb(255, 170, 200), Color.FromRgb(160, 220, 255) };

        for (int i = 0; i < 2; i++)
        {
            double t = (_frameTick * 0.05) + (i * 3.1);
            double bx = (w * (0.3 + (i * 0.3))) + (Math.Sin(t) * 26);
            double by = (groundY * 0.55) + (Math.Cos(t * 1.4) * 14);
            double wingFlap = Math.Abs(Math.Sin(_frameTick * 0.4)) * 3 + 2;

            var wingBrush = new SolidColorBrush(colors[i % colors.Length]);
            dc.DrawEllipse(wingBrush, null, new Point(bx - 2, by), wingFlap, wingFlap * 0.7);
            dc.DrawEllipse(wingBrush, null, new Point(bx + 2, by), wingFlap, wingFlap * 0.7);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(70, 55, 60)), 0.8), new Point(bx, by - 2), new Point(bx, by + 2));
        }
    }

    private void DrawFlowerPatch(DrawingContext dc, double w, double groundY)
    {
        var petalColors = new[]
        {
            Color.FromRgb(255, 235, 130), // Yellow daisy
            Color.FromRgb(255, 170, 200), // Pink
            Color.FromRgb(255, 255, 255)  // White
        };

        double spacing = IsMiniMode ? 26 : 34;
        int count = (int)(w / spacing);

        for (int i = 0; i < count; i++)
        {
            double fx = (i * spacing) + 12 + (((i * 37) % 9));
            double fy = groundY + (IsMiniMode ? 3 : 5) + ((i % 2) * 3);

            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(90, 175, 100)), null, new Point(fx, fy + 2), 0.8, 2.2);

            var petalBrush = new SolidColorBrush(petalColors[i % petalColors.Length]);
            for (int p = 0; p < 4; p++)
            {
                double ang = p * Math.PI / 2;
                dc.DrawEllipse(petalBrush, null, new Point(fx + (Math.Cos(ang) * 1.6), fy + (Math.Sin(ang) * 1.6)), 1.4, 1.4);
            }

            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 200, 90)), null, new Point(fx, fy), 1, 1);
        }
    }

    private void DrawParkBench(DrawingContext dc, double x, double groundY, double scale)
    {
        var woodBrush = new SolidColorBrush(Color.FromRgb(165, 115, 75));
        var woodPen = new Pen(new SolidColorBrush(Color.FromRgb(120, 80, 50)), 1);

        double benchW = 34 * scale;
        double benchY = groundY - (16 * scale);

        // Backrest slats
        dc.DrawRoundedRectangle(woodBrush, woodPen, new Rect(x, benchY - (10 * scale), benchW, 4 * scale), 1, 1);
        dc.DrawRoundedRectangle(woodBrush, woodPen, new Rect(x, benchY - (5 * scale), benchW, 4 * scale), 1, 1);
        // Seat slats
        dc.DrawRoundedRectangle(woodBrush, woodPen, new Rect(x - (2 * scale), benchY, benchW + (4 * scale), 3.5 * scale), 1, 1);
        // Legs
        dc.DrawRectangle(woodBrush, null, new Rect(x, benchY + (3 * scale), 2.5 * scale, 8 * scale));
        dc.DrawRectangle(woodBrush, null, new Rect(x + benchW - (2.5 * scale), benchY + (3 * scale), 2.5 * scale, 8 * scale));
    }

    private void DrawSummerClouds(DrawingContext dc, double w, double groundY)
    {
        var cloudBrush = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255));
        double cloud1X = ((_frameTick * 0.3) + 40) % (w + 100) - 50;
        double cloud2X = ((_frameTick * 0.2) + 260) % (w + 120) - 60;

        dc.DrawEllipse(cloudBrush, null, new Point(cloud1X, groundY * 0.28), 24, 12);
        dc.DrawEllipse(cloudBrush, null, new Point(cloud1X + 14, groundY * 0.24), 18, 14);

        if (!IsMiniMode)
        {
            dc.DrawEllipse(cloudBrush, null, new Point(cloud2X, groundY * 0.40), 30, 14);
            dc.DrawEllipse(cloudBrush, null, new Point(cloud2X + 18, groundY * 0.35), 22, 16);
        }
    }

    private void DrawParkTree(DrawingContext dc, double w, double groundY)
    {
        if (IsMiniMode)
        {
            // Mini leaf canopy in corner
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(230, 85, 175, 95)), null, new Point(0, 0), 45, 30);
            return;
        }

        // Shady oak tree trunk
        var trunkBrush = new LinearGradientBrush(Color.FromRgb(105, 65, 45), Color.FromRgb(75, 42, 28), new Point(0, 0), new Point(1, 0));
        var trunkGeom = new PathGeometry();
        var tf = new PathFigure { StartPoint = new Point(0, groundY) };
        tf.Segments.Add(new LineSegment(new Point(18, groundY), true));
        tf.Segments.Add(new QuadraticBezierSegment(new Point(12, groundY * 0.4), new Point(0, 0), true));
        tf.IsClosed = true;
        trunkGeom.Figures.Add(tf);
        dc.DrawGeometry(trunkBrush, null, trunkGeom);

        // Lush green layered leaf clusters
        var leafDark = new SolidColorBrush(Color.FromRgb(55, 140, 70));
        var leafMid = new SolidColorBrush(Color.FromRgb(80, 175, 95));
        var leafLight = new SolidColorBrush(Color.FromRgb(115, 205, 120));

        dc.DrawEllipse(leafDark, null, new Point(20, 10), 55, 42);
        dc.DrawEllipse(leafMid, null, new Point(45, 18), 45, 35);
        dc.DrawEllipse(leafLight, null, new Point(15, 30), 40, 30);
        dc.DrawEllipse(leafMid, null, new Point(75, 8), 35, 26);
    }

    private void DrawPastelBuntingFlags(DrawingContext dc, double w)
    {
        if (IsMiniMode) return;

        var colors = new[]
        {
            Color.FromRgb(255, 150, 170), // Pastel Pink
            Color.FromRgb(130, 220, 200), // Pastel Mint
            Color.FromRgb(255, 225, 120), // Pastel Yellow
            Color.FromRgb(185, 170, 245), // Pastel Lavender
            Color.FromRgb(255, 180, 130)  // Pastel Peach
        };

        var stringPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 90, 70, 50)), 0.9);
        double startX = 65;
        double endX = w - 20;
        if (endX <= startX) return;

        double spacing = 28;
        int idx = 0;
        double prevX = startX;
        double prevY = 12;

        for (double fx = startX + spacing; fx <= endX; fx += spacing)
        {
            double dipY = 18 + (Math.Sin((_frameTick * 0.1) + idx) * 1.5);
            var geom = new PathGeometry();
            var fig = new PathFigure { StartPoint = new Point(prevX, prevY) };
            fig.Segments.Add(new QuadraticBezierSegment(new Point((prevX + fx) * 0.5, dipY), new Point(fx, 12), true));
            geom.Figures.Add(fig);
            dc.DrawGeometry(null, stringPen, geom);

            // Flag triangle
            var flagGeom = new PathGeometry();
            var ff = new PathFigure { StartPoint = new Point((prevX + fx) * 0.5 - 7, dipY - 1) };
            ff.Segments.Add(new LineSegment(new Point((prevX + fx) * 0.5 + 7, dipY - 1), true));
            ff.Segments.Add(new LineSegment(new Point((prevX + fx) * 0.5, dipY + 12), true));
            ff.IsClosed = true;
            flagGeom.Figures.Add(ff);
            dc.DrawGeometry(new SolidColorBrush(colors[idx % colors.Length]), null, flagGeom);

            prevX = fx;
            prevY = 12;
            idx++;
        }
    }

    private void DrawPastelIceCreamTruck(DrawingContext dc, double x, double y, double w, double h, double scale, double shutterProgress = 0.0, bool isServing = false)
    {
        // 1. Truck Shadow on Ground
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(70, 20, 40, 20)), null, new Point(x + (w * 0.5), y + h + (3 * scale)), w * 0.52, 6 * scale);

        // 2. Main Truck Body: Two-tone Pastel (Mint Green bottom, Cream/Pink top)
        double bodyH = h * 0.78;
        double bodyY = y + (h * 0.15);

        // Bottom Half (Pastel Mint)
        var mintBrush = new LinearGradientBrush(Color.FromRgb(128, 226, 184), Color.FromRgb(72, 197, 149), new Point(0, 0), new Point(0, 1));
        var mintPen = new Pen(new SolidColorBrush(Color.FromRgb(55, 160, 120)), 1.2 * scale);
        dc.DrawRoundedRectangle(mintBrush, mintPen, new Rect(x, bodyY + (bodyH * 0.45), w, bodyH * 0.55), 8 * scale, 8 * scale);

        // Top Half (Pastel Strawberry Cream)
        var creamBrush = new LinearGradientBrush(Color.FromRgb(255, 245, 238), Color.FromRgb(255, 218, 228), new Point(0, 0), new Point(0, 1));
        var creamPen = new Pen(new SolidColorBrush(Color.FromRgb(230, 180, 195)), 1.2 * scale);
        dc.DrawRoundedRectangle(creamBrush, creamPen, new Rect(x, bodyY, w, bodyH * 0.52), 8 * scale, 8 * scale);

        // Chrome Divider Stripe across body
        var chromeBrush = new LinearGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(215, 220, 230), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(chromeBrush, null, new Rect(x + (2 * scale), bodyY + (bodyH * 0.44), w - (4 * scale), 3.5 * scale));

        // 3. Front Cab Windshield (Right side of truck facing right)
        double cabW = w * 0.28;
        double cabX = x + w - cabW - (4 * scale);
        double cabY = bodyY + (4 * scale);
        double cabH = bodyH * 0.42;

        var glassBrush = new LinearGradientBrush(Color.FromArgb(200, 190, 235, 255), Color.FromArgb(130, 140, 200, 240), new Point(0, 0), new Point(1, 1));
        dc.DrawRoundedRectangle(glassBrush, new Pen(new SolidColorBrush(Color.FromRgb(160, 200, 220)), 1 * scale), new Rect(cabX, cabY, cabW, cabH), 5 * scale, 5 * scale);

        // Windshield specular glint
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1.5 * scale), new Point(cabX + (4 * scale), cabY + (4 * scale)), new Point(cabX + (cabW * 0.6), cabY + cabH - (4 * scale)));

        // Front Headlight (Warm Amber Glow)
        double hlX = x + w - (2 * scale);
        double hlY = bodyY + (bodyH * 0.55);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 230, 120)), new Pen(new SolidColorBrush(Color.FromRgb(210, 180, 70)), 1), new Point(hlX, hlY), 3.5 * scale, 4 * scale);
        var hlGlow = new RadialGradientBrush(Color.FromArgb(80, 255, 230, 100), Color.FromArgb(0, 255, 230, 100));
        dc.DrawEllipse(hlGlow, null, new Point(hlX + (4 * scale), hlY), 12 * scale, 10 * scale);

        // Chrome Front Bumper
        dc.DrawRoundedRectangle(chromeBrush, new Pen(new SolidColorBrush(Color.FromRgb(170, 175, 185)), 1), new Rect(x + w - (3 * scale), bodyY + (bodyH * 0.78), 7 * scale, 8 * scale), 2 * scale, 2 * scale);

        // 4. Wide Open Service Counter Window (Left/Center of truck)
        double servX = x + (12 * scale);
        double servY = bodyY + (6 * scale);
        double servW = w * 0.56;
        double servH = bodyH * 0.42;

        // Interior warm light
        var interiorBrush = new LinearGradientBrush(Color.FromRgb(255, 242, 195), Color.FromRgb(255, 215, 140), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(interiorBrush, new Pen(new SolidColorBrush(Color.FromRgb(215, 170, 120)), 1.2 * scale), new Rect(servX, servY, servW, servH), 3 * scale, 3 * scale);

        // Ice Cream Seller with Chef Hat inside counter
        double vendorX = servX + (servW * 0.5);
        double vendorY = servY + servH - (2 * scale);

        // Vendor head & blush
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 218, 195)), null, new Point(vendorX, vendorY - (11 * scale)), 7 * scale, 7 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(160, 255, 130, 150)), null, new Point(vendorX - (4 * scale), vendorY - (9 * scale)), 2 * scale, 1.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(160, 255, 130, 150)), null, new Point(vendorX + (4 * scale), vendorY - (9 * scale)), 2 * scale, 1.2 * scale);

        // Vendor happy eyes (^_^)
        var eyePen = new Pen(new SolidColorBrush(Color.FromRgb(70, 45, 30)), 1.2 * scale);
        dc.DrawLine(eyePen, new Point(vendorX - (5 * scale), vendorY - (11 * scale)), new Point(vendorX - (2 * scale), vendorY - (11 * scale)));
        dc.DrawLine(eyePen, new Point(vendorX + (2 * scale), vendorY - (11 * scale)), new Point(vendorX + (5 * scale), vendorY - (11 * scale)));

        // Chef Hat on vendor
        var hatBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        dc.DrawRoundedRectangle(hatBrush, new Pen(new SolidColorBrush(Color.FromRgb(210, 215, 225)), 1), new Rect(vendorX - (6 * scale), vendorY - (22 * scale), 12 * scale, 6 * scale), 2 * scale, 2 * scale);
        dc.DrawEllipse(hatBrush, null, new Point(vendorX, vendorY - (21 * scale)), 7 * scale, 5 * scale);

        // Vendor waving hand, or reaching out to hand a fresh cone to the kid at the counter
        double waveOffset = Math.Sin(_frameTick * 0.2) * (3 * scale);
        if (isServing)
        {
            double reach = Math.Min(1.0, (_frameTick % 8) / 6.0) * (5 * scale);
            double armX = vendorX + (10 * scale) + reach;
            double armY = vendorY - (10 * scale);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 218, 195)), 3 * scale), new Point(vendorX + (6 * scale), vendorY - (10 * scale)), new Point(armX, armY));
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 218, 195)), null, new Point(armX, armY), 2.6 * scale, 2.6 * scale);
            // Cone being handed over
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(200, 130, 65)), 1.8 * scale), new Point(armX + (2 * scale), armY + (4 * scale)), new Point(armX + (2 * scale), armY - (1 * scale)));
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 130, 160)), null, new Point(armX + (2 * scale), armY - (3 * scale)), 3 * scale, 3 * scale);
        }
        else
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 218, 195)), null, new Point(vendorX + (10 * scale), vendorY - (12 * scale) + waveOffset), 2.8 * scale, 2.8 * scale);
        }

        // Service Counter Shelf with colorful ice cream tubs
        dc.DrawRoundedRectangle(chromeBrush, null, new Rect(servX - (2 * scale), servY + servH - (3 * scale), servW + (4 * scale), 4 * scale), 1.5 * scale, 1.5 * scale);

        // 3 mini tubs: Strawberry, Mint, Chocolate
        if (!IsMiniMode)
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 130, 160)), null, new Rect(servX + (4 * scale), servY + servH - (7 * scale), 7 * scale, 4 * scale), 1, 1);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(120, 220, 180)), null, new Rect(servX + (13 * scale), servY + servH - (7 * scale), 7 * scale, 4 * scale), 1, 1);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(140, 80, 50)), null, new Rect(servX + (22 * scale), servY + servH - (7 * scale), 7 * scale, 4 * scale), 1, 1);
        }

        // 4b. End-of-Shift Roll-Down Shutter (slides down over the service counter, painted "GOOD JOB")
        if (shutterProgress > 0.001)
        {
            double shutterH = servH * shutterProgress;

            var shutterBrush = new LinearGradientBrush(Color.FromRgb(240, 232, 218), Color.FromRgb(205, 190, 170), new Point(0, 0), new Point(0, 1));
            var shutterPen = new Pen(new SolidColorBrush(Color.FromRgb(150, 130, 105)), 1.2 * scale);
            dc.DrawRoundedRectangle(shutterBrush, shutterPen, new Rect(servX, servY, servW, shutterH), 2 * scale, 2 * scale);

            // Corrugated metal ridge lines
            var ridgePen = new Pen(new SolidColorBrush(Color.FromArgb(90, 140, 120, 100)), 1);
            for (double ry = servY + (3 * scale); ry < servY + shutterH - 2; ry += 4 * scale)
            {
                dc.DrawLine(ridgePen, new Point(servX + (1 * scale), ry), new Point(servX + servW - (1 * scale), ry));
            }

            // Bottom pull-handle bar once mostly closed
            if (shutterProgress > 0.85)
            {
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(120, 100, 80)), null, new Rect(servX + (servW * 0.5) - (6 * scale), servY + shutterH - (2 * scale), 12 * scale, 3 * scale), 1.5, 1.5);
            }

            // Hand-painted "GOOD JOB" once the shutter is far enough down to read clearly
            if (shutterProgress > 0.5)
            {
                double textAlpha = Math.Clamp((shutterProgress - 0.5) / 0.4, 0.0, 1.0);
                string title = IsMiniMode ? "GOOD JOB!" : "✨ GOOD JOB! ✨";
                var titleFt = CreateText(title, (IsMiniMode ? 7.5 : 11) * Math.Min(scale + 0.3, 1.0), new SolidColorBrush(Color.FromArgb((byte)(255 * textAlpha), 235, 90, 110)), FontWeights.Bold);
                dc.DrawText(titleFt, new Point(servX + ((servW - titleFt.Width) * 0.5), servY + (shutterH * 0.5) - (titleFt.Height * 0.5)));

                if (!IsMiniMode && shutterProgress > 0.9)
                {
                    var subFt = CreateText("🍦 Closed for today", 8, new SolidColorBrush(Color.FromArgb((byte)(210 * textAlpha), 110, 85, 65)), FontWeights.SemiBold);
                    dc.DrawText(subFt, new Point(servX + ((servW - subFt.Width) * 0.5), servY + shutterH - (subFt.Height * 1.1)));
                }
            }
        }

        // 5. Striped Scalloped Awning over Service Window
        double awningX = servX - (6 * scale);
        double awningY = bodyY - (4 * scale);
        double awningW = servW + (12 * scale);
        double awningH = 14 * scale;

        double flutter = Math.Sin(_frameTick * 0.15) * (0.8 * scale);

        var awningPink = new SolidColorBrush(Color.FromRgb(255, 120, 155));
        var awningWhite = new SolidColorBrush(Color.FromRgb(255, 250, 252));
        var awningPen = new Pen(new SolidColorBrush(Color.FromRgb(220, 95, 130)), 1 * scale);

        int stripes = 6;
        double stripeW = awningW / stripes;
        for (int st = 0; st < stripes; st++)
        {
            var sBrush = (st % 2 == 0) ? awningPink : awningWhite;
            double sx = awningX + (st * stripeW);

            var sGeom = new PathGeometry();
            var sf = new PathFigure { StartPoint = new Point(sx, awningY) };
            sf.Segments.Add(new LineSegment(new Point(sx + stripeW, awningY), true));
            sf.Segments.Add(new LineSegment(new Point(sx + stripeW, awningY + awningH + flutter), true));
            sf.Segments.Add(new QuadraticBezierSegment(new Point(sx + (stripeW * 0.5), awningY + awningH + (3 * scale) + flutter), new Point(sx, awningY + awningH + flutter), true));
            sf.IsClosed = true;
            sGeom.Figures.Add(sf);
            dc.DrawGeometry(sBrush, awningPen, sGeom);
        }

        // 6. Giant 3D Soft-Serve Ice Cream Cone on Truck Roof
        DrawIceCreamRoofCone(dc, x + (w * 0.42), bodyY - (2 * scale), scale);

        // 7. Truck Wheels with White-Wall Rims
        double wheelRadius = 13 * scale;
        double frontWheelX = x + w - (24 * scale);
        double rearWheelX = x + (24 * scale);
        double wheelY = bodyY + bodyH + (2 * scale);

        DrawTruckWheel(dc, frontWheelX, wheelY, wheelRadius, scale);
        DrawTruckWheel(dc, rearWheelX, wheelY, wheelRadius, scale);

        // 8. Cute Chalkboard Menu Sign standing beside truck
        if (!IsMiniMode)
        {
            double menuX = x + w + 4;
            double menuY = bodyY + (bodyH * 0.35);
            var menuWood = new SolidColorBrush(Color.FromRgb(120, 75, 45));
            var boardBrush = new SolidColorBrush(Color.FromRgb(45, 55, 50));
            dc.DrawRoundedRectangle(menuWood, null, new Rect(menuX, menuY, 20, 28), 2, 2);
            dc.DrawRectangle(boardBrush, null, new Rect(menuX + 2, menuY + 2, 16, 20));
            var menuFt = CreateText("🍦\n🍓\n🍫", 6, new SolidColorBrush(Color.FromRgb(255, 235, 170)));
            dc.DrawText(menuFt, new Point(menuX + 4, menuY + 3));
            dc.DrawLine(new Pen(menuWood, 2), new Point(menuX + 3, menuY + 28), new Point(menuX + 1, menuY + 34));
            dc.DrawLine(new Pen(menuWood, 2), new Point(menuX + 17, menuY + 28), new Point(menuX + 19, menuY + 34));
        }
    }

    private void DrawTruckWheel(DrawingContext dc, double x, double y, double r, double scale)
    {
        // Black Tire
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(40, 44, 48)), new Pen(new SolidColorBrush(Color.FromRgb(20, 22, 24)), 1), new Point(x, y), r, r);
        // White-Wall Rim
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(250, 248, 242)), null, new Point(x, y), r * 0.72, r * 0.72);
        // Pastel Mint Hubcap
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(110, 215, 170)), new Pen(new SolidColorBrush(Color.FromRgb(70, 165, 125)), 1), new Point(x, y), r * 0.48, r * 0.48);
        // Center Chrome Nut
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new Point(x, y), r * 0.18, r * 0.18);
    }

    private void DrawIceCreamRoofCone(DrawingContext dc, double x, double y, double scale)
    {
        // Waffle Cone (Bottom)
        double coneW = 18 * scale;
        double coneH = 16 * scale;
        double coneTopY = y - (10 * scale);

        var waffleBrush = new LinearGradientBrush(Color.FromRgb(240, 175, 105), Color.FromRgb(200, 130, 65), new Point(0, 0), new Point(1, 1));
        var wafflePen = new Pen(new SolidColorBrush(Color.FromRgb(165, 95, 45)), 1 * scale);

        var cGeom = new PathGeometry();
        var cf = new PathFigure { StartPoint = new Point(x - (coneW * 0.5), coneTopY) };
        cf.Segments.Add(new LineSegment(new Point(x + (coneW * 0.5), coneTopY), true));
        cf.Segments.Add(new LineSegment(new Point(x, coneTopY + coneH), true));
        cf.IsClosed = true;
        cGeom.Figures.Add(cf);
        dc.DrawGeometry(waffleBrush, wafflePen, cGeom);

        // Swirled Vanilla & Strawberry Soft-Serve Ice Cream Swirls
        double swirlY = coneTopY;
        var vanBrush = new SolidColorBrush(Color.FromRgb(255, 252, 240));
        var pinkBrush = new SolidColorBrush(Color.FromRgb(255, 145, 180));

        // Bottom swirl ring
        dc.DrawEllipse(vanBrush, new Pen(new SolidColorBrush(Color.FromRgb(230, 220, 195)), 1 * scale), new Point(x, swirlY - (2 * scale)), 12 * scale, 6 * scale);
        // Middle strawberry swirl
        dc.DrawEllipse(pinkBrush, null, new Point(x, swirlY - (8 * scale)), 9 * scale, 5 * scale);
        // Top vanilla tip swirl
        dc.DrawEllipse(vanBrush, null, new Point(x, swirlY - (13 * scale)), 6 * scale, 4 * scale);

        // Shiny Red Cherry on top!
        double cherryY = swirlY - (17 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(235, 40, 60)), new Pen(new SolidColorBrush(Color.FromRgb(170, 20, 35)), 1), new Point(x, cherryY), 3.5 * scale, 3.5 * scale);
        // Cherry specular shine glint
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new Point(x - (1 * scale), cherryY - (1 * scale)), 1.2 * scale, 1.2 * scale);

        // Sparkle glint on ice cream roof cone
        double pulse = Math.Sin(_frameTick * 0.2) * 0.3 + 0.7;
        DrawSparkle(dc, x + (12 * scale), cherryY - (4 * scale), 4 * scale * pulse, new SolidColorBrush(Color.FromRgb(255, 235, 140)));
    }

    private void DrawChibiKid(DrawingContext dc, double x, double groundY, double scale, int kidIndex, bool hasIceCream, string flavor, bool facingLeft = false)
    {
        double bounce = Math.Sin((_frameTick * 0.2) + kidIndex) * (1.5 * scale);
        double kidH = 34 * scale;
        double kidY = groundY - kidH + bounce;

        // Mirror multiplier for asymmetric features (cap bill, balloon, held treat) so the kid
        // visibly faces the direction they're walking instead of always facing one fixed way.
        double dir = facingLeft ? -1.0 : 1.0;

        var skinBrush = new SolidColorBrush(Color.FromRgb(255, 224, 205));
        var blushBrush = new SolidColorBrush(Color.FromArgb(170, 255, 125, 150));
        var eyeBrush = new SolidColorBrush(Color.FromRgb(60, 40, 35));
        var mouthPen = new Pen(new SolidColorBrush(Color.FromRgb(210, 100, 120)), 1.1 * scale);

        // Big chibi-anime head radius (matches the lo-fi metro girl's proportions) with a
        // smaller body underneath, plus a gentle smiling mouth on every kid.
        double headR = 9 * scale;
        double headCy = kidY + (9 * scale);

        switch (kidIndex)
        {
            case 0: // Boy with Blue Cap
                // Body (Blue Shirt & Shorts)
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(70, 145, 235)), null, new Rect(x - (6 * scale), kidY + (17 * scale), 12 * scale, 11 * scale), 3, 3);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(190, 130, 85)), null, new Rect(x - (5 * scale), kidY + (26 * scale), 10 * scale, 7 * scale), 1, 1);
                // Head
                dc.DrawEllipse(skinBrush, null, new Point(x, headCy), headR, headR);
                dc.DrawEllipse(blushBrush, null, new Point(x - (5 * scale), headCy + (2 * scale)), 2 * scale, 1.3 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x + (5 * scale), headCy + (2 * scale)), 2 * scale, 1.3 * scale);
                // Eyes (^_^)
                dc.DrawLine(new Pen(eyeBrush, 1.3 * scale), new Point(x - (5 * scale), headCy), new Point(x - (2.5 * scale), headCy));
                dc.DrawLine(new Pen(eyeBrush, 1.3 * scale), new Point(x + (2.5 * scale), headCy), new Point(x + (5 * scale), headCy));
                dc.DrawLine(mouthPen, new Point(x - (2 * scale), headCy + (4 * scale)), new Point(x + (2 * scale), headCy + (4 * scale)));
                // Blue Cap with bill pointing in the direction the kid is walking/facing
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(50, 115, 215)), null, new Rect(x - (9 * scale), headCy - (9 * scale), 18 * scale, 7 * scale), 3, 3);
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(40, 95, 185)), null, new Rect(x + (dir * (5.5 * scale)) - (6.5 * scale), headCy - (5 * scale), 13 * scale, 3 * scale));
                break;

            case 1: // Girl with Yellow Dress & Pigtails
                // Yellow Dress
                var dressGeom = new PathGeometry();
                var df = new PathFigure { StartPoint = new Point(x - (4 * scale), kidY + (16 * scale)) };
                df.Segments.Add(new LineSegment(new Point(x + (4 * scale), kidY + (16 * scale)), true));
                df.Segments.Add(new LineSegment(new Point(x + (8 * scale), kidY + (28 * scale)), true));
                df.Segments.Add(new LineSegment(new Point(x - (8 * scale), kidY + (28 * scale)), true));
                df.IsClosed = true;
                dressGeom.Figures.Add(df);
                dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(255, 220, 75)), null, dressGeom);

                // Head
                dc.DrawEllipse(skinBrush, null, new Point(x, headCy), headR, headR);
                dc.DrawEllipse(blushBrush, null, new Point(x - (5 * scale), headCy + (2 * scale)), 2 * scale, 1.3 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x + (5 * scale), headCy + (2 * scale)), 2 * scale, 1.3 * scale);
                dc.DrawLine(new Pen(eyeBrush, 1.3 * scale), new Point(x - (5 * scale), headCy), new Point(x - (2.5 * scale), headCy));
                dc.DrawLine(new Pen(eyeBrush, 1.3 * scale), new Point(x + (2.5 * scale), headCy), new Point(x + (5 * scale), headCy));
                dc.DrawLine(mouthPen, new Point(x - (2 * scale), headCy + (4 * scale)), new Point(x + (2 * scale), headCy + (4 * scale)));
                // Brown Pigtails & Bangs (symmetric, no mirroring needed)
                var hairBrush = new SolidColorBrush(Color.FromRgb(115, 65, 35));
                dc.DrawEllipse(hairBrush, null, new Point(x - (10 * scale), headCy - (1 * scale)), 4 * scale, 6 * scale);
                dc.DrawEllipse(hairBrush, null, new Point(x + (10 * scale), headCy - (1 * scale)), 4 * scale, 6 * scale);
                dc.DrawRoundedRectangle(hairBrush, null, new Rect(x - (8 * scale), headCy - (9 * scale), 16 * scale, 6 * scale), 3, 3);
                break;

            case 2: // Boy in Denim Overalls
                // Overalls
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 140, 80)), null, new Rect(x - (6 * scale), kidY + (16 * scale), 12 * scale, 7 * scale), 2, 2);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(65, 115, 175)), null, new Rect(x - (6 * scale), kidY + (20 * scale), 12 * scale, 15 * scale), 2, 2);
                // Head
                dc.DrawEllipse(skinBrush, null, new Point(x, headCy), headR, headR);
                dc.DrawEllipse(blushBrush, null, new Point(x - (5 * scale), headCy + (2 * scale)), 2 * scale, 1.3 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x + (5 * scale), headCy + (2 * scale)), 2 * scale, 1.3 * scale);
                dc.DrawLine(new Pen(eyeBrush, 1.3 * scale), new Point(x - (5 * scale), headCy), new Point(x - (2.5 * scale), headCy));
                dc.DrawLine(new Pen(eyeBrush, 1.3 * scale), new Point(x + (2.5 * scale), headCy), new Point(x + (5 * scale), headCy));
                dc.DrawLine(mouthPen, new Point(x - (2 * scale), headCy + (4 * scale)), new Point(x + (2 * scale), headCy + (4 * scale)));
                // Tousled Hair with Bangs (symmetric, no mirroring needed)
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(70, 45, 30)), null, new Point(x, headCy - (2 * scale)), 9.5 * scale, 8 * scale);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(70, 45, 30)), null, new Rect(x - (8 * scale), headCy - (9 * scale), 16 * scale, 6 * scale), 3, 3);
                break;

            default: // Toddler in Pink Romper with Balloon
                // Pink Romper
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 160, 195)), null, new Rect(x - (5.5 * scale), kidY + (15 * scale), 11 * scale, 14 * scale), 3, 3);
                // Head
                dc.DrawEllipse(skinBrush, null, new Point(x, headCy - (1 * scale)), headR * 0.9, headR * 0.9);
                dc.DrawEllipse(blushBrush, null, new Point(x - (4.5 * scale), headCy + (1 * scale)), 1.8 * scale, 1.1 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x + (4.5 * scale), headCy + (1 * scale)), 1.8 * scale, 1.1 * scale);
                dc.DrawLine(new Pen(eyeBrush, 1.3 * scale), new Point(x - (4 * scale), headCy - (1 * scale)), new Point(x - (2 * scale), headCy - (1 * scale)));
                dc.DrawLine(new Pen(eyeBrush, 1.3 * scale), new Point(x + (2 * scale), headCy - (1 * scale)), new Point(x + (4 * scale), headCy - (1 * scale)));
                dc.DrawLine(mouthPen, new Point(x - (1.6 * scale), headCy + (3 * scale)), new Point(x + (1.6 * scale), headCy + (3 * scale)));
                // Little Tuft of Hair
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(150, 100, 60)), null, new Point(x, headCy - (2 * scale)), 1.6 * scale, 3 * scale);
                // Floating Star Balloon, held on the trailing side (opposite the facing direction)
                if (!IsMiniMode)
                {
                    double bx = x - (dir * (10 * scale));
                    double by = kidY - (12 * scale) + (Math.Sin(_frameTick * 0.15) * 3);
                    dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(140, 100, 100, 100)), 0.8), new Point(x - (dir * (3 * scale)), kidY + (14 * scale)), new Point(bx, by + 6));
                    DrawSparkle(dc, bx, by, 7 * scale, new SolidColorBrush(Color.FromRgb(255, 215, 75)));
                }
                break;
        }

        // Draw Ice Cream Held in Hand
        if (hasIceCream)
        {
            double icX = x - (dir * (7 * scale));
            double icY = kidY + (18 * scale);

            if (flavor == "pop")
            {
                // Rocket Pop
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(210, 170, 120)), 1.5 * scale), new Point(icX, icY), new Point(icX, icY + (6 * scale)));
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 75, 95)), null, new Rect(icX - (2.5 * scale), icY - (6 * scale), 5 * scale, 6 * scale), 1.5, 1.5);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(80, 195, 255)), null, new Rect(icX - (2.5 * scale), icY - (3 * scale), 5 * scale, 3 * scale), 1, 1);
            }
            else
            {
                // Cone with Scoops
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(200, 130, 65)), 2 * scale), new Point(icX, icY + (6 * scale)), new Point(icX, icY));
                var scoopCol = flavor == "strawberry"
                    ? Color.FromRgb(255, 120, 160)
                    : flavor == "chocolate"
                        ? Color.FromRgb(130, 75, 45)
                        : Color.FromRgb(110, 225, 185);
                dc.DrawEllipse(new SolidColorBrush(scoopCol), null, new Point(icX, icY - (2 * scale)), 4 * scale, 4 * scale);

                if (flavor == "mint_rainbow")
                {
                    dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 90)), null, new Point(icX, icY - (7 * scale)), 3.2 * scale, 3.2 * scale);
                }
            }
        }
    }

    private void DrawCutePuppy(DrawingContext dc, double x, double groundY, double scale)
    {
        double pupY = groundY - (16 * scale);
        var pupBrush = new SolidColorBrush(Color.FromRgb(240, 195, 140));

        // Body
        dc.DrawEllipse(pupBrush, null, new Point(x, pupY + (6 * scale)), 6 * scale, 5 * scale);
        // Head
        dc.DrawEllipse(pupBrush, null, new Point(x - (3 * scale), pupY), 5 * scale, 4.5 * scale);
        // Floppy Ear
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(185, 130, 75)), null, new Point(x - (5 * scale), pupY - (1 * scale)), 2.5 * scale, 4 * scale);
        // Nose dot & eye
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(40, 25, 15)), null, new Point(x - (6 * scale), pupY + (1 * scale)), 1 * scale, 1 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(40, 25, 15)), null, new Point(x - (3 * scale), pupY - (1 * scale)), 1 * scale, 1 * scale);
        // Wagging Tail!
        double wag = Math.Sin(_frameTick * 0.4) * (4 * scale);
        dc.DrawLine(new Pen(pupBrush, 2 * scale), new Point(x + (5 * scale), pupY + (5 * scale)), new Point(x + (9 * scale), pupY + (2 * scale) + wag));
    }

    private void DrawIceCreamPartyCelebration(DrawingContext dc, double w, double groundY, double startX, double scale)
    {
        // 1. Festive Golden Banner across top
        double bannerW = IsMiniMode ? 170 : 310;
        double bannerH = IsMiniMode ? 18 : 26;
        double bannerX = (w - bannerW) * 0.5;
        double bannerY = IsMiniMode ? 3 : 6;

        var bannerBrush = new LinearGradientBrush(Color.FromRgb(255, 235, 140), Color.FromRgb(255, 190, 60), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(bannerBrush, new Pen(new SolidColorBrush(Color.FromRgb(215, 150, 30)), 1.2), new Rect(bannerX, bannerY, bannerW, bannerH), 5, 5);

        string title = IsMiniMode ? "🍨 ICE CREAM PARTY! 🎉" : "✨ 🍨 YAY! ICE CREAM PARTY! 🍦 ✨";
        var ft = CreateText(title, IsMiniMode ? 10 : 13, new SolidColorBrush(Color.FromRgb(70, 40, 10)), FontWeights.Bold);
        dc.DrawText(ft, new Point(bannerX + ((bannerW - ft.Width) * 0.5), bannerY + ((bannerH - ft.Height) * 0.5)));

        // 2. All 4 Kids Celebrating side-by-side
        double kidSpacing = IsMiniMode ? (28 * scale) : (40 * scale);
        double kStartX = Math.Min(startX, w - (kidSpacing * 4) - (10 * scale));

        DrawChibiKid(dc, kStartX, groundY, scale, 0, true, "strawberry");
        DrawChibiKid(dc, kStartX + kidSpacing, groundY, scale, 1, true, "mint_rainbow");
        DrawChibiKid(dc, kStartX + (kidSpacing * 2), groundY, scale, 2, true, "chocolate");
        DrawCutePuppy(dc, kStartX + (kidSpacing * 2) + (14 * scale), groundY, scale);
        DrawChibiKid(dc, kStartX + (kidSpacing * 3), groundY, scale, 3, true, "pop");

        // 3. Falling Confetti
        var rand = new Random(777 + (_frameTick / 2));
        var confettiColors = new[]
        {
            Color.FromRgb(255, 105, 145), Color.FromRgb(255, 215, 65), Color.FromRgb(95, 220, 190),
            Color.FromRgb(125, 180, 255), Color.FromRgb(200, 160, 255)
        };

        for (int c = 0; c < (IsMiniMode ? 8 : 18); c++)
        {
            double cx = rand.NextDouble() * w;
            double cy = ((_frameTick * 1.5) + (c * 20)) % groundY;
            double cr = 2.5 + rand.Next(2);
            dc.DrawRoundedRectangle(new SolidColorBrush(confettiColors[c % confettiColors.Length]), null, new Rect(cx, cy, cr, cr * 1.6), 1, 1);
        }
    }

    private void DrawIceCreamRestPicnic(DrawingContext dc, double w, double groundY, double startX, double scale)
    {
        // Shady Picnic Table with Parasol Umbrella & Big Sundaes
        double tblX = startX + (20 * scale);
        double tblY = groundY - (12 * scale);
        double tblW = 50 * scale;

        // Parasol Umbrella
        if (!IsMiniMode)
        {
            double umbX = tblX + (tblW * 0.5);
            double umbY = groundY - (55 * scale);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(160, 160, 170)), 2), new Point(umbX, umbY), new Point(umbX, groundY));
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 130, 160)), new Pen(new SolidColorBrush(Color.FromRgb(220, 90, 120)), 1), new Point(umbX, umbY), 28 * scale, 12 * scale);
        }

        // Picnic Table
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(240, 225, 205)), new Pen(new SolidColorBrush(Color.FromRgb(180, 155, 130)), 1.2), new Rect(tblX, tblY, tblW, 6 * scale), 2, 2);

        // Giant Ice Cream Sundae Glass on Table
        double sunX = tblX + (tblW * 0.5);
        double sunY = tblY - (14 * scale);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 140, 175)), null, new Rect(sunX - (7 * scale), sunY, 14 * scale, 10 * scale), 3, 3);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new Point(sunX, sunY - (2 * scale)), 6 * scale, 5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(235, 40, 60)), null, new Point(sunX, sunY - (6 * scale)), 2.5 * scale, 2.5 * scale);

        // Rest Banner
        string text = IsMiniMode ? "🏖️ 🍨 Sundae Rest Break" : "🏖️ 🍨 Sweet Ice Cream Break • Relax & Refresh ✨";
        var ft = CreateText(text, IsMiniMode ? 10 : 12, new SolidColorBrush(Color.FromRgb(60, 40, 20)), FontWeights.Bold);
        dc.DrawText(ft, new Point(w - ft.Width - (IsMiniMode ? 8 : 16), IsMiniMode ? 4 : 8));
    }

    /// <summary>
    /// Happy kids waving goodbye with their treats while the truck's shutter rolls down / drives off.
    /// A lighter version of the party celebration without the banner or confetti.
    /// </summary>
    private void DrawIceCreamGoodbyeKids(DrawingContext dc, double w, double groundY, double startX, double scale)
    {
        double kidSpacing = IsMiniMode ? (28 * scale) : (40 * scale);
        double kStartX = Math.Min(startX, w - (kidSpacing * 4) - (10 * scale));

        DrawChibiKid(dc, kStartX, groundY, scale, 0, true, "strawberry");
        DrawChibiKid(dc, kStartX + kidSpacing, groundY, scale, 1, true, "mint_rainbow");
        DrawChibiKid(dc, kStartX + (kidSpacing * 2), groundY, scale, 2, true, "chocolate");
        DrawCutePuppy(dc, kStartX + (kidSpacing * 2) + (14 * scale), groundY, scale);
        DrawChibiKid(dc, kStartX + (kidSpacing * 3), groundY, scale, 3, true, "pop");

        // Waving hands & floating little hearts
        double waveArc = Math.Sin(_frameTick * 0.3) * 4;
        var handBrush = new SolidColorBrush(Color.FromRgb(255, 222, 198));
        dc.DrawEllipse(handBrush, null, new Point(kStartX - (7 * scale), groundY - (26 * scale) + waveArc), 2.4 * scale, 2.4 * scale);
        dc.DrawEllipse(handBrush, null, new Point(kStartX + (kidSpacing * 3) + (9 * scale), groundY - (26 * scale) - waveArc), 2.4 * scale, 2.4 * scale);

        if (!IsMiniMode)
        {
            DrawHeart(dc, kStartX + (kidSpacing * 1.5), groundY - (34 * scale) + waveArc, 3.5, new SolidColorBrush(Color.FromRgb(255, 140, 175)));
        }

        string text = IsMiniMode ? "👋 Bye bye!" : "👋 Bye bye! See you tomorrow! 🍦";
        var ft2 = CreateText(text, IsMiniMode ? 9.5 : 11.5, new SolidColorBrush(Color.FromRgb(60, 40, 20)), FontWeights.Bold);
        dc.DrawText(ft2, new Point(w - ft2.Width - (IsMiniMode ? 8 : 16), IsMiniMode ? 4 : 8));
    }

    /// <summary>
    /// Peaceful empty street after the ice cream truck has driven off for the day.
    /// A leaning chalkboard sign left behind gives a final quiet note of closure.
    /// </summary>
    private void DrawIceCreamClosedStreet(DrawingContext dc, double w, double groundY, double scale)
    {
        // Soft golden-hour tint settling over the park now that the shift is done
        var duskGlow = new LinearGradientBrush(
            Color.FromArgb(0, 255, 200, 140),
            Color.FromArgb(60, 255, 170, 110),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(duskGlow, null, new Rect(0, 0, w, groundY));

        double signX = w * (IsMiniMode ? 0.42 : 0.38);
        double signY = groundY - (30 * scale);

        // Leaning wooden chalkboard sign
        var woodBrush = new SolidColorBrush(Color.FromRgb(120, 75, 45));
        var boardBrush = new SolidColorBrush(Color.FromRgb(45, 55, 50));
        dc.DrawRoundedRectangle(woodBrush, null, new Rect(signX, signY, 34 * scale, 30 * scale), 2, 2);
        dc.DrawRoundedRectangle(boardBrush, null, new Rect(signX + (2 * scale), signY + (2 * scale), 30 * scale, 26 * scale), 1.5, 1.5);
        dc.DrawLine(new Pen(woodBrush, 2.5 * scale), new Point(signX + (4 * scale), signY + (30 * scale)), new Point(signX, signY + (40 * scale)));
        dc.DrawLine(new Pen(woodBrush, 2.5 * scale), new Point(signX + (30 * scale), signY + (30 * scale)), new Point(signX + (34 * scale), signY + (40 * scale)));

        string chalkText = IsMiniMode ? "🍦 Closed" : "🍦 Closed\nSee you\ntomorrow!";
        var chalkFt = CreateText(chalkText, IsMiniMode ? 7.5 : 9, new SolidColorBrush(Color.FromRgb(255, 240, 210)), FontWeights.SemiBold);
        dc.DrawText(chalkFt, new Point(signX + (17 * scale) - (chalkFt.Width * 0.5), signY + (14 * scale) - (chalkFt.Height * 0.5)));

        // A couple of gentle drifting dandelion seeds for a calm end-of-day feel
        if (!IsMiniMode)
        {
            for (int i = 0; i < 3; i++)
            {
                double dx = ((_frameTick * (0.6 + i * 0.2)) + (i * 90)) % (w + 40) - 20;
                double dy = (groundY * 0.35) + (Math.Sin((_frameTick * 0.08) + i) * 10) + (i * 14);
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), null, new Point(dx, dy), 2, 2);
            }
        }

        string text = IsMiniMode ? "🌇 Shift complete!" : "🌇 A wonderful day's work • Shift complete! ✨";
        var ft = CreateText(text, IsMiniMode ? 9.5 : 11.5, new SolidColorBrush(Color.FromRgb(90, 60, 35)), FontWeights.Bold);
        dc.DrawText(ft, new Point(w - ft.Width - (IsMiniMode ? 8 : 16), IsMiniMode ? 4 : 8));
    }

    #endregion

    #region 🎧 Scene: Tokyo Metro Lo-Fi Girl

    private void RenderMetroScene(DrawingContext dc, double w, double h)
    {
        double windowTopY = IsMiniMode ? 12 : 20;
        double windowH = h - windowTopY - (IsMiniMode ? 12 : 24);
        double seatY = h - (IsMiniMode ? 15 : 30);

        // Gentle carriage sway/rumble - rocks dynamically when train travels at high speed
        double swayAngle = IsTracking ? Math.Sin(_frameTick * 0.08) * 0.75 : (IsRestPhase ? Math.Sin(_frameTick * 0.04) * 0.45 : 0.0);
        dc.PushTransform(new RotateTransform(swayAngle, w * 0.5, h * 0.9));

        // 1. Carriage Wall & Interior Frame
        var carriageBrush = new LinearGradientBrush(
            Color.FromRgb(36, 40, 56),
            Color.FromRgb(24, 26, 38),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(carriageBrush, null, new Rect(0, 0, w, h));

        // 2. Large Panoramic Train Window
        double windowMarginX = IsMiniMode ? 6 : 16;
        double windowW = w - (windowMarginX * 2);
        var windowRect = new Rect(windowMarginX, windowTopY, windowW, windowH);

        // Clip outside view to window area
        var windowClip = new RectangleGeometry(windowRect, 6, 6);
        dc.PushClip(windowClip);

        // A. Night / Twilight Sky outside the window
        var skyBrush = new LinearGradientBrush(
            Color.FromRgb(14, 10, 32),
            Color.FromRgb(38, 20, 62),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(skyBrush, null, windowRect);

        // B. Distant Tokyo Skyline Silhouettes & Lit Windows (Reduced density)
        DrawTokyoSkyline(dc, windowRect);

        // C. Glowing Tokyo Tower in the distance (Warm Orange/Red)
        DrawTokyoTower(dc, windowRect);

        // D. Parallax Moving Japanese Neon Billboard Signs
        DrawPassingTokyoNeonSigns(dc, windowRect);

        // E. Moving Streetlight Bokeh Orbs outside
        DrawMetroPassingBokeh(dc, windowRect);

        // F. High-speed transit light streaks outside the window when task is active
        DrawMetroSpeedStreaks(dc, windowRect);

        // G. Soft Window Glass Reflection & condensation glints
        DrawMetroWindowReflection(dc, windowRect);

        dc.Pop(); // Pop window clip

        // 3. Window Frame Bevels (Sleek brushed aluminum / warm trim)
        var framePen = new Pen(new LinearGradientBrush(
            Color.FromRgb(160, 170, 190),
            Color.FromRgb(90, 100, 120),
            new Point(0, 0),
            new Point(0, 1)), IsMiniMode ? 1.8 : 3.0);
        dc.DrawRoundedRectangle(null, framePen, windowRect, 6, 6);

        // 4. Overhead Stainless Steel Handrail with Hanging Straps
        DrawMetroHandrailsAndStraps(dc, w, windowTopY);

        // 5. LED Electronic Train Route Indicator / Ticker above window
        DrawMetroRouteTicker(dc, w, windowTopY);

        // 6. Metro Green Velvet Bench Seat along bottom
        DrawMetroSeatBench(dc, w, seatY, h - seatY);

        // 7. Lo-Fi Anime Girl Character by the Window
        if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawMetroGoalGirl(dc, w, seatY, IsMiniMode);
        }
        else if (IsRestPhase)
        {
            DrawMetroRestGirl(dc, w, seatY, IsMiniMode);
        }
        else
        {
            DrawMetroLoFiGirl(dc, w, seatY, ProgressFraction, IsTracking, IsMiniMode);
        }

        dc.Pop(); // Pop carriage sway transform
    }

    private void DrawTokyoSkyline(DrawingContext dc, Rect rect)
    {
        double trainSpeed = IsTracking ? 5.2 : (IsRestPhase ? 0.6 : (IsGoalReached ? 0.4 : 0.25));

        // 1. Far background skyline silhouettes & communication towers (Slow Parallax Layer)
        DrawFarTokyoSkyline(dc, rect, trainSpeed);

        // 2. Mid-ground detailed Tokyo high-rises & skyscrapers with lit windows (Smooth Parallax Layer)
        DrawMidTokyoCityscape(dc, rect, trainSpeed);
    }

    private void DrawFarTokyoSkyline(DrawingContext dc, Rect rect, double trainSpeed)
    {
        double bBaseY = rect.Bottom;
        var farSilhouetteBrush = new SolidColorBrush(Color.FromRgb(16, 12, 34));
        var farWinBrush = new SolidColorBrush(Color.FromArgb(90, 255, 235, 140));

        // Well-proportioned skyline heights for a clean, visible distant background in both mini and full modes
        var farBuildings = new (double w, double hPct, bool beacon)[]
        {
            (36, 0.54, false),
            (26, 0.44, true),
            (42, 0.62, false),
            (28, 0.48, true),
            (44, 0.56, false)
        };

        double totalFarSpan = 0;
        const double farGap = 28;
        for (int i = 0; i < farBuildings.Length; i++) totalFarSpan += farBuildings[i].w + farGap;

        double farScroll = (_frameTick * trainSpeed * 0.75) % totalFarSpan;
        double startX = rect.Left - farScroll;
        while (startX > rect.Left - totalFarSpan) startX -= totalFarSpan;

        double currentX = startX;
        while (currentX < rect.Right + 30)
        {
            for (int i = 0; i < farBuildings.Length; i++)
            {
                var (bw, hPct, beacon) = farBuildings[i];
                double bh = rect.Height * hPct;
                double bx = currentX;
                double by = bBaseY - bh;

                if (bx + bw >= rect.Left - 10 && bx <= rect.Right + 10)
                {
                    dc.DrawRectangle(farSilhouetteBrush, null, new Rect(bx, by, bw, bh));

                    // Small distant window pinpricks anchored to building-relative coordinates
                    if (hPct > 0.40)
                    {
                        double winW = IsMiniMode ? 1.6 : 1.8;
                        double winH = IsMiniMode ? 1.0 : 1.3;
                        double winGapX = IsMiniMode ? 4.5 : 6.0;
                        double winGapY = IsMiniMode ? 4.5 : 6.5;

                        int row = 0;
                        for (double wy = by + (IsMiniMode ? 3.0 : 4.0); wy < bBaseY - 4; wy += winGapY, row++)
                        {
                            int col = 0;
                            for (double wx = bx + (IsMiniMode ? 2.5 : 3.5); wx < bx + bw - 2.5; wx += winGapX, col++)
                            {
                                int hash = (col * 19 + row * 13 + i * 37);
                                if ((hash % 3) == 0)
                                {
                                    dc.DrawRectangle(farWinBrush, null, new Rect(wx, wy, winW, winH));
                                }
                            }
                        }
                    }

                    // Distant red aviation beacon light on rooftop antenna
                    if (beacon)
                    {
                        double antH = IsMiniMode ? 4 : 6;
                        dc.DrawLine(new Pen(farSilhouetteBrush, 1.0), new Point(bx + (bw * 0.5), by), new Point(bx + (bw * 0.5), by - antH));
                        if ((_frameTick + (i * 12)) % 32 < 16)
                        {
                            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 60, 60)), null, new Point(bx + (bw * 0.5), by - antH), IsMiniMode ? 1.0 : 1.2, IsMiniMode ? 1.0 : 1.2);
                        }
                    }
                }

                currentX += bw + farGap;
            }
        }
    }

    private void DrawMidTokyoCityscape(DrawingContext dc, Rect rect, double trainSpeed)
    {
        var buildBrush1 = new SolidColorBrush(Color.FromRgb(22, 17, 44));
        var buildBrush2 = new SolidColorBrush(Color.FromRgb(18, 14, 38));
        var rimPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 140, 130, 210)), IsMiniMode ? 0.7 : 0.8);

        var winGold = new SolidColorBrush(Color.FromArgb(180, 255, 235, 140));
        var winCyan = new SolidColorBrush(Color.FromArgb(170, 130, 225, 255));
        var winPink = new SolidColorBrush(Color.FromArgb(160, 255, 150, 210));

        // Proportional building heights visible and distinct in both mini and full views
        var midBuildings = new (double w, double hPct, int style, string? signText, Color signCol)[]
        {
            (46, 0.78, 1, "TOKYO", Color.FromRgb(255, 80, 180)),            // Shinjuku Stepped Tower with Neon
            (34, 0.58, 4, null, default),                                   // High-Rise with Rooftop Water Tank
            (50, 0.86, 5, null, default),                                   // Financial Skyscraper with Spire & Beacon
            (40, 0.68, 7, "METRO", Color.FromRgb(60, 240, 255))             // Cyber Glass Tower
        };

        double totalMidSpan = 0;
        const double midGap = 42;
        for (int i = 0; i < midBuildings.Length; i++) totalMidSpan += midBuildings[i].w + midGap;

        // Rapid continuous parallax movement from right to left as train travels
        double midScroll = (_frameTick * trainSpeed * 2.0) % totalMidSpan;
        double startX = rect.Left - midScroll;
        while (startX > rect.Left - totalMidSpan) startX -= totalMidSpan;

        double currentX = startX;
        double bBaseY = rect.Bottom;

        while (currentX < rect.Right + 50)
        {
            for (int i = 0; i < midBuildings.Length; i++)
            {
                var (bw, hPct, style, signText, signCol) = midBuildings[i];
                double bh = rect.Height * hPct;
                double bx = currentX;
                double by = bBaseY - bh;

                if (bx + bw >= rect.Left - 20 && bx <= rect.Right + 20)
                {
                    var bBrush = (i % 2 == 0) ? buildBrush1 : buildBrush2;

                    // Building main silhouette
                    dc.DrawRectangle(bBrush, rimPen, new Rect(bx, by, bw, bh));

                    // Architectural variations (Stepped crowns, penthouses, water tanks)
                    if (style == 1 || style == 5 || style == 8) // Stepped crown
                    {
                        double stepW = bw * 0.6;
                        double stepH = bh * (IsMiniMode ? 0.10 : 0.12);
                        double stepX = bx + (bw - stepW) * 0.5;
                        double stepY = by - stepH;
                        dc.DrawRectangle(bBrush, rimPen, new Rect(stepX, stepY, stepW, stepH));

                        // Rooftop communication spire with blinking collision light
                        double antH = IsMiniMode ? 4.5 : 9.0;
                        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(140, 145, 170)), 1.0), new Point(bx + (bw * 0.5), stepY), new Point(bx + (bw * 0.5), stepY - antH));
                        if ((_frameTick + (i * 8)) % 28 < 14)
                        {
                            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 50, 50)), null, new Point(bx + (bw * 0.5), stepY - antH), IsMiniMode ? 1.2 : 1.5, IsMiniMode ? 1.2 : 1.5);
                            dc.DrawEllipse(new RadialGradientBrush(Color.FromArgb(120, 255, 50, 50), Color.FromArgb(0, 255, 50, 50)), null, new Point(bx + (bw * 0.5), stepY - antH), IsMiniMode ? 2.5 : 3.5, IsMiniMode ? 2.5 : 3.5);
                        }
                    }
                    else if (style == 4) // Rooftop water tank
                    {
                        double tankW = bw * 0.35;
                        double tankH = IsMiniMode ? 3.5 : 5.0;
                        double tankX = bx + 4;
                        double tankY = by - tankH;
                        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(40, 35, 60)), null, new Rect(tankX, tankY, tankW, tankH));
                        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(30, 26, 48)), 1), new Point(tankX + 2, by), new Point(tankX + 2, tankY));
                        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(30, 26, 48)), 1), new Point(tankX + tankW - 2, by), new Point(tankX + tankW - 2, tankY));
                    }
                    else if (style == 6) // Rooftop HVAC unit
                    {
                        double hvacW = bw * 0.45;
                        double hvacH = IsMiniMode ? 3.0 : 4.0;
                        double hvacX = bx + (bw - hvacW) * 0.5;
                        double hvacY = by - hvacH;
                        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(35, 30, 55)), null, new Rect(hvacX, hvacY, hvacW, hvacH));
                    }

                    // Rooftop Mini Neon Sign / LED Accent
                    if (signText != null)
                    {
                        double signW = bw * (IsMiniMode ? 0.82 : 0.75);
                        double signH = IsMiniMode ? 6.5 : 8.0;
                        double signX = bx + (bw - signW) * 0.5;
                        double signY = by - signH - (IsMiniMode ? 1.0 : 2.0);
                        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(170, 20, 16, 36)), new Pen(new SolidColorBrush(signCol), IsMiniMode ? 0.7 : 1.0), new Rect(signX, signY, signW, signH), 2, 2);
                        var sFt = CreateText(signText, IsMiniMode ? 5.5 : 7.0, new SolidColorBrush(signCol), FontWeights.Bold);
                        dc.DrawText(sFt, new Point(signX + ((signW - sFt.Width) * 0.5), signY + ((signH - sFt.Height) * 0.5)));
                    }

                    // Illuminated Window Grid Matrix - compact horizontal windows anchored to building grid (no flickering)
                    double winW = IsMiniMode ? 2.4 : 3.2;
                    double winH = IsMiniMode ? 1.4 : 2.0;
                    double winGapX = IsMiniMode ? 4.8 : 6.2;
                    double winGapY = IsMiniMode ? 3.6 : 4.6;

                    int row = 0;
                    for (double wy = by + (IsMiniMode ? 3.5 : 5.0); wy < bBaseY - (IsMiniMode ? 3.0 : 5.0); wy += winGapY, row++)
                    {
                        int col = 0;
                        for (double wx = bx + (IsMiniMode ? 3.0 : 4.0); wx < bx + bw - (IsMiniMode ? 3.0 : 4.0); wx += winGapX, col++)
                        {
                            int hash = (col * 31 + row * 17 + i * 53);
                            if (hash % 4 != 0) // realistic night occupancy pattern
                            {
                                var wb = (hash % 7 == 0) ? winPink : ((hash % 3 == 0) ? winGold : winCyan);
                                dc.DrawRectangle(wb, null, new Rect(wx, wy, winW, winH));
                            }
                        }
                    }
                }

                currentX += bw + midGap;
            }
        }
    }

    private void DrawTokyoTower(DrawingContext dc, Rect rect)
    {
        double trainSpeed = IsTracking ? 5.2 : (IsRestPhase ? 0.6 : (IsGoalReached ? 0.4 : 0.25));

        // Tokyo Tower glides gracefully in the far background parallax layer
        double towerSpan = rect.Width + 380;
        double towerScroll = (_frameTick * trainSpeed * 0.6) % towerSpan;
        double towerX = rect.Right + 160 - towerScroll;
        if (towerX < rect.Left - 70) towerX += towerSpan;

        if (towerX < rect.Left - 50 || towerX > rect.Right + 50) return;

        double towerBaseY = rect.Bottom;
        double towerH = rect.Height * 0.88;
        double towerTopY = towerBaseY - towerH;

        var towerBrush = new SolidColorBrush(Color.FromRgb(255, 90, 60));
        var towerGlow = new RadialGradientBrush(Color.FromArgb(90, 255, 110, 70), Color.FromArgb(0, 255, 110, 70));
        dc.DrawEllipse(towerGlow, null, new Point(towerX, towerBaseY - (towerH * 0.5)), IsMiniMode ? 16 : 28, towerH * 0.55);

        // Lower main observation deck & top deck
        double mainDeckY = towerBaseY - (towerH * 0.42);
        double topDeckY = towerBaseY - (towerH * 0.68);
        double towerScale = IsMiniMode ? 0.75 : 1.0;

        // Tower Lattice Geometry
        var tGeom = new PathGeometry();
        var tf = new PathFigure { StartPoint = new Point(towerX - (13 * towerScale), towerBaseY) };
        tf.Segments.Add(new LineSegment(new Point(towerX - (5.5 * towerScale), mainDeckY), true));
        tf.Segments.Add(new LineSegment(new Point(towerX - (3.5 * towerScale), topDeckY), true));
        tf.Segments.Add(new LineSegment(new Point(towerX - (1.5 * towerScale), towerTopY + (12 * towerScale)), true));
        tf.Segments.Add(new LineSegment(new Point(towerX, towerTopY), true));
        tf.Segments.Add(new LineSegment(new Point(towerX + (1.5 * towerScale), towerTopY + (12 * towerScale)), true));
        tf.Segments.Add(new LineSegment(new Point(towerX + (3.5 * towerScale), topDeckY), true));
        tf.Segments.Add(new LineSegment(new Point(towerX + (5.5 * towerScale), mainDeckY), true));
        tf.Segments.Add(new LineSegment(new Point(towerX + (13 * towerScale), towerBaseY), true));
        tf.IsClosed = true;
        tGeom.Figures.Add(tf);
        dc.DrawGeometry(towerBrush, null, tGeom);

        // Observation Deck illuminated galleries
        var deckBrush = new SolidColorBrush(Color.FromRgb(255, 235, 160));
        dc.DrawRoundedRectangle(deckBrush, null, new Rect(towerX - (6.5 * towerScale), mainDeckY - (2 * towerScale), 13 * towerScale, 4.5 * towerScale), 1, 1);
        dc.DrawRoundedRectangle(deckBrush, null, new Rect(towerX - (4.5 * towerScale), topDeckY - (1.5 * towerScale), 9 * towerScale, 3.5 * towerScale), 1, 1);

        // Blinking aviation strobe beacon light at tip
        if (_frameTick % 20 < 10)
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 255, 240)), null, new Point(towerX, towerTopY), IsMiniMode ? 1.8 : 2.5, IsMiniMode ? 1.8 : 2.5);
            var tipGlow = new RadialGradientBrush(Color.FromArgb(180, 255, 80, 80), Color.FromArgb(0, 255, 80, 80));
            dc.DrawEllipse(tipGlow, null, new Point(towerX, towerTopY), IsMiniMode ? 6 : 9, IsMiniMode ? 6 : 9);
        }
    }

    private void DrawPassingTokyoNeonSigns(DrawingContext dc, Rect rect)
    {
        double trainSpeed = IsTracking ? 5.2 : (IsRestPhase ? 0.6 : (IsGoalReached ? 0.4 : 0.25));

        var signs = new (string text, Color color, double yPct, double speed)[]
        {
            ("新宿", Color.FromRgb(70, 240, 255), 0.22, 0.85),    // Radiant Cyan Neon (Shinjuku)
            ("渋谷", Color.FromRgb(255, 80, 185), 0.38, 1.15),    // Vibrant Magenta Neon (Shibuya)
            ("秋葉原", Color.FromRgb(185, 105, 255), 0.28, 0.95), // Electric Violet Neon (Akihabara)
            ("音楽", Color.FromRgb(255, 225, 70), 0.16, 0.65),    // Warm Amber Gold (Music)
            ("カフェ", Color.FromRgb(80, 250, 170), 0.44, 1.05),  // Emerald Mint (Cafe)
            ("地下鉄", Color.FromRgb(255, 150, 50), 0.32, 0.75)   // Sunset Orange (Metro)
        };

        for (int i = 0; i < signs.Length; i++)
        {
            var (text, col, yPct, speed) = signs[i];
            double travelWidth = rect.Width + 160;
            double rawX = (rect.Right + 80) - (((_frameTick * speed * trainSpeed * 2.4) + (i * 110)) % travelWidth);
            double sy = rect.Top + (rect.Height * yPct);

            if (rawX >= rect.Left - 50 && rawX <= rect.Right + 50)
            {
                // Neon glow background box
                var glowBrush = new RadialGradientBrush(Color.FromArgb(75, col.R, col.G, col.B), Color.FromArgb(0, col.R, col.G, col.B));
                dc.DrawEllipse(glowBrush, null, new Point(rawX + (IsMiniMode ? 10 : 16), sy + (IsMiniMode ? 5 : 8)), IsMiniMode ? 18 : 28, IsMiniMode ? 12 : 18);

                // Dark backing plate with thin neon border
                var signBrush = new SolidColorBrush(col);
                var ft = CreateText(text, IsMiniMode ? 8.0 : 12.0, signBrush, FontWeights.Bold);
                double boxW = ft.Width + (IsMiniMode ? 6 : 8);
                double boxH = ft.Height + (IsMiniMode ? 3 : 4);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(170, 15, 12, 28)), new Pen(signBrush, IsMiniMode ? 0.8 : 1.0), new Rect(rawX - (IsMiniMode ? 3 : 4), sy - (IsMiniMode ? 1.5 : 2), boxW, boxH), 2.5, 2.5);
                dc.DrawText(ft, new Point(rawX, sy));
            }
        }
    }

    private void DrawMetroPassingBokeh(DrawingContext dc, Rect rect)
    {
        double trainSpeed = IsTracking ? 5.2 : (IsRestPhase ? 0.6 : (IsGoalReached ? 0.4 : 0.25));
        var rand = new Random(555);

        for (int i = 0; i < (IsMiniMode ? 7 : 16); i++)
        {
            double speed = 1.0 + (rand.NextDouble() * 1.5);
            double bx = (rect.Right + 30) - (((_frameTick * speed * trainSpeed * 3.0) + (i * 36)) % (rect.Width + 60));
            double by = rect.Top + (rand.NextDouble() * (rect.Height * 0.78));
            double br = 4 + (rand.NextDouble() * 8.5);

            Color bColor = (i % 3 == 0)
                ? Color.FromArgb(48, 255, 190, 85)
                : (i % 3 == 1)
                    ? Color.FromArgb(48, 255, 105, 170)
                    : Color.FromArgb(48, 105, 230, 255);

            var bGlow = new RadialGradientBrush(bColor, Color.FromArgb(0, bColor.R, bColor.G, bColor.B));
            dc.DrawEllipse(bGlow, null, new Point(bx, by), br, br);
        }
    }

    /// <summary>
    /// Renders high-speed transit light streaks outside the window when a task is running,
    /// giving an energetic sensation of the train flying past the Tokyo skyline.
    /// </summary>
    private void DrawMetroSpeedStreaks(DrawingContext dc, Rect rect)
    {
        if (!IsTracking) return;

        int streakCount = IsMiniMode ? 6 : 14;
        for (int i = 0; i < streakCount; i++)
        {
            double speed = 16.0 + ((i * 37) % 18);
            double travelW = rect.Width + 180;
            double x = (rect.Right + 90) - (((_frameTick * speed) + (i * 68)) % travelW);
            double y = rect.Top + 6 + (((i * 41) % (int)Math.Max(10, rect.Height - 14)));
            double len = IsMiniMode ? (20 + (i % 4) * 12) : (45 + (i % 5) * 25);

            Color col = (i % 3 == 0)
                ? Color.FromArgb(110, 180, 235, 255)
                : (i % 3 == 1)
                    ? Color.FromArgb(95, 255, 130, 215)
                    : Color.FromArgb(85, 255, 240, 150);

            var streakGrad = new LinearGradientBrush(
                Color.FromArgb(0, col.R, col.G, col.B),
                col,
                new Point(0, 0),
                new Point(1, 0));

            dc.DrawLine(new Pen(streakGrad, IsMiniMode ? 1.0 : 1.6), new Point(x, y), new Point(x + len, y));
        }
    }

    private void DrawMetroWindowReflection(DrawingContext dc, Rect rect)
    {
        // Diagonal gloss reflection streak
        var glossBrush = new LinearGradientBrush(
            Color.FromArgb(30, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255),
            new Point(0, 0),
            new Point(1, 1));
        dc.DrawRectangle(glossBrush, null, new Rect(rect.Left + (rect.Width * 0.45), rect.Top, rect.Width * 0.35, rect.Height));
    }

    private void DrawMetroHandrailsAndStraps(DrawingContext dc, double w, double topY)
    {
        // Matte black modern subway rail bar
        double railY = topY + (IsMiniMode ? 2.5 : 8);
        var railBrush = new LinearGradientBrush(
            Color.FromRgb(30, 32, 40),
            Color.FromRgb(10, 12, 16),
            new Point(0, 0),
            new Point(0, 1));
        var railPen = new Pen(new SolidColorBrush(Color.FromRgb(50, 54, 65)), 0.6);
        dc.DrawRectangle(railBrush, railPen, new Rect(0, railY, w, IsMiniMode ? 2.0 : 4.0));

        // Hanging leather straps with triangular handholds
        int strapCount = IsMiniMode ? 3 : 5;
        double strapSpacing = w / (strapCount + 1);

        var strapPen = new Pen(new SolidColorBrush(Color.FromRgb(40, 42, 50)), IsMiniMode ? 1.2 : 2.2);
        var ringBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var ringPen = new Pen(new SolidColorBrush(Color.FromRgb(180, 185, 195)), IsMiniMode ? 0.9 : 1.2);

        for (int i = 1; i <= strapCount; i++)
        {
            double sx = i * strapSpacing;
            double swayRate = IsTracking ? 0.20 : 0.12;
            double swayAmp = IsTracking ? (IsMiniMode ? 3.0 : 8.0) : (IsMiniMode ? 2.0 : 5.5);
            double sway = Math.Sin((_frameTick * swayRate) + (i * 0.9)) * swayAmp;
            double strapLen = IsMiniMode ? 8.5 : 24;

            Point startPt = new Point(sx, railY + 1.5);
            Point endPt = new Point(sx + sway, railY + strapLen);

            dc.DrawLine(strapPen, startPt, endPt);

            // Handhold Ring
            double ringR = IsMiniMode ? 3.0 : 6.5;
            dc.DrawEllipse(null, ringPen, new Point(endPt.X, endPt.Y + ringR), ringR, ringR);
        }
    }

    private void DrawMetroRouteTicker(DrawingContext dc, double w, double topY)
    {
        double tickerW = IsMiniMode ? Math.Min(w - 20, 165) : 280;
        double tickerH = IsMiniMode ? 10.5 : 16;
        double tickerX = (w - tickerW) * 0.5;
        double tickerY = topY - tickerH - 1.5;

        if (tickerY < 1) tickerY = 1;

        // LED Display Body
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(15, 18, 22)), new Pen(new SolidColorBrush(Color.FromRgb(60, 65, 75)), 1), new Rect(tickerX, tickerY, tickerW, tickerH), 2.5, 2.5);

        string tickerText = (IsGoalReached || ProgressFraction >= 0.999)
            ? (IsMiniMode ? "🌟 TARGET REACHED! 🌟" : "🌟 ARRIVED: TARGET STATION • 100% FOCUS! 🌟")
            : IsRestPhase
                ? (IsMiniMode ? "🍵 METRO TEA BREAK" : "🍵 TOKYO METRO: LO-FI TEA & REST BREAK")
                : IsTracking
                    ? (IsMiniMode ? $"🟢 EXPRESS • {(int)(ProgressFraction * 100)}%" : $"🟢 EXPRESS YAMANOTE • NEXT: FOCUS STATION ──► {(int)(ProgressFraction * 100)}%")
                    : (IsMiniMode ? "🟢 READY TO DEPART" : "🟢 YAMANOTE LINE • READY TO DEPART");

        var ledBrush = (IsGoalReached || ProgressFraction >= 0.999)
            ? new SolidColorBrush(Color.FromRgb(255, 225, 110))
            : IsRestPhase
                ? new SolidColorBrush(Color.FromRgb(130, 240, 180))
                : new SolidColorBrush(Color.FromRgb(110, 245, 140));

        var ft = CreateText(tickerText, IsMiniMode ? 7.0 : 9.5, ledBrush, FontWeights.Bold);
        dc.DrawText(ft, new Point(tickerX + ((tickerW - ft.Width) * 0.5), tickerY + ((tickerH - ft.Height) * 0.5)));
    }

    private void DrawMetroSeatBench(DrawingContext dc, double w, double seatY, double seatH)
    {
        bool isMini = IsMiniMode;
        double scale = isMini ? 0.60 : 1.0;
        double floorY = isMini ? seatY + (8.5 * scale) : seatY + (26 * scale);

        // 1. Upper Backrest Cushions (背もたれ - Ergonomic Segmented Tokyo Metro Plush Moquette)
        double backrestTop = seatY - (isMini ? 8.5 : 20);
        double backrestH = seatY - backrestTop;
        double seatSecW = isMini ? 32 : 48;
        int numSections = (int)Math.Ceiling(w / seatSecW) + 1;

        for (int i = 0; i < numSections; i++)
        {
            double secX = i * seatSecW;
            var secRect = new Rect(secX, backrestTop, seatSecW, backrestH);

            // A. Backrest Main Plush Velvet Body (Deep Tokyo Metro Emerald Green Moquette)
            var backrestBrush = new LinearGradientBrush(
                Color.FromRgb(36, 126, 88),
                Color.FromRgb(22, 80, 56),
                new Point(0, 0),
                new Point(0, 1));
            dc.DrawRoundedRectangle(backrestBrush, null, secRect, isMini ? 2 : 4, isMini ? 2 : 4);

            // B. Upper Headrest/Shoulder Bolster Roll Accent (Sage/Olive moquette)
            double bolsterH = isMini ? 3.0 : 6.5;
            var bolsterBrush = new LinearGradientBrush(
                Color.FromRgb(60, 155, 105),
                Color.FromRgb(42, 122, 82),
                new Point(0, 0),
                new Point(0, 1));
            dc.DrawRoundedRectangle(bolsterBrush, null, new Rect(secX + 1, backrestTop, seatSecW - 2, bolsterH), isMini ? 1.5 : 3, isMini ? 1.5 : 3);

            // C. Top Bolster Velvet Highlight Sheen Line
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(160, 150, 245, 195)), 1.0), new Point(secX + 2, backrestTop + 1), new Point(secX + seatSecW - 2, backrestTop + 1));

            // D. Center Ergonomic Vertical Accent Stitch / Piping
            double cx = secX + (seatSecW * 0.5);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(110, 130, 235, 180)), 1.2), new Point(cx, backrestTop + bolsterH + 1), new Point(cx, seatY - 1));

            // E. Vertical Seam Divider Groove between passenger spots
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(14, 52, 36)), 1.4), new Point(secX, backrestTop), new Point(secX, seatY));
        }

        // 2. Stainless Steel Under-Seat Heater Plinth & Kickplate (蹴込板 / ヒーター)
        double cushionBottom = seatY + (isMini ? 6.5 : 12.0);
        double plinthH = Math.Max(0, floorY - cushionBottom);
        var plinthBrush = new LinearGradientBrush(
            Color.FromRgb(44, 48, 60),
            Color.FromRgb(26, 30, 38),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(plinthBrush, null, new Rect(0, cushionBottom, w, plinthH));

        // Horizontal Heater Ventilation Louver Slits under each seat
        for (int i = 0; i < numSections; i++)
        {
            double secX = i * seatSecW;
            double louverW = seatSecW - (isMini ? 8 : 12);
            double louverX = secX + ((seatSecW - louverW) * 0.5);
            for (double ly = cushionBottom + (isMini ? 2 : 4); ly < floorY - 2; ly += (isMini ? 3.5 : 5.0))
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(12, 14, 18)), null, new Rect(louverX, ly, louverW, isMini ? 1.2 : 1.8));
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(70, 78, 95)), 0.6), new Point(louverX, ly + (isMini ? 1.2 : 1.8)), new Point(louverX + louverW, ly + (isMini ? 1.2 : 1.8)));
            }
        }

        // 3. Thick Front Seat-Base Cushion (座面 - Sculpted 3D Velvet Cushion with Highlight)
        double cushionH = cushionBottom - (seatY - (isMini ? 1.5 : 2.5));
        var cushionBrush = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(Color.FromRgb(50, 160, 112), 0.0),
                new GradientStop(Color.FromRgb(34, 118, 82), 0.4),
                new GradientStop(Color.FromRgb(20, 78, 54), 0.85),
                new GradientStop(Color.FromRgb(14, 56, 38), 1.0)
            },
            new Point(0, 0),
            new Point(0, 1));
        var cushionPen = new Pen(new SolidColorBrush(Color.FromRgb(14, 50, 34)), 1.2);
        dc.DrawRoundedRectangle(cushionBrush, cushionPen, new Rect(0, seatY - (isMini ? 1.5 : 2.5), w, cushionH), isMini ? 2.5 : 4.5, isMini ? 2.5 : 4.5);

        // Plush velvet top highlight line catching interior lighting
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(170, 150, 245, 195)), 1.5), new Point(0, seatY - (isMini ? 0.5 : 1.0)), new Point(w, seatY - (isMini ? 0.5 : 1.0)));

        // Cushion division indentations aligning with backrest seats
        for (int i = 0; i < numSections; i++)
        {
            double secX = i * seatSecW;
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(120, 12, 46, 32)), 1.2), new Point(secX, seatY - (isMini ? 1.5 : 2.5)), new Point(secX, cushionBottom));
        }

        // Under-cushion drop shadow cast onto heater plinth
        var shadowBrush = new LinearGradientBrush(
            Color.FromArgb(160, 0, 0, 0),
            Color.FromArgb(0, 0, 0, 0),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(shadowBrush, null, new Rect(0, cushionBottom, w, isMini ? 2.5 : 4.5));

        // 4. Carriage Transit Floor along bottom
        var floorBrush = new LinearGradientBrush(
            Color.FromRgb(28, 30, 42),
            Color.FromRgb(18, 20, 28),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(floorBrush, null, new Rect(0, floorY, w, seatH - (floorY - seatY)));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(55, 60, 78)), 1.0), new Point(0, floorY), new Point(w, floorY));
    }

    /// <summary>
    /// Modern Tokyo Metro partition screen (袖仕切) with frosted glass and stainless steel grab stanchion
    /// with iconic safety-orange grip sleeve.
    /// </summary>
    private void DrawMetroArmrestDivider(DrawingContext dc, double x, double seatY, double floorY, double scale)
    {
        double topY = seatY - (28 * scale);
        double partW = 10 * scale;
        double partH = floorY - topY;

        // 1. Sleek Partition Screen Frame (Composite / Brushed Aluminum)
        var frameBrush = new LinearGradientBrush(
            Color.FromRgb(215, 222, 235),
            Color.FromRgb(140, 150, 165),
            new Point(0, 0),
            new Point(1, 0));
        var framePen = new Pen(new SolidColorBrush(Color.FromRgb(100, 110, 125)), 0.8 * scale);
        dc.DrawRoundedRectangle(frameBrush, framePen, new Rect(x - (partW * 0.5), topY, partW, partH), 3 * scale, 3 * scale);

        // 2. Frosted Safety Glass Panel Insert
        double glassMargin = 2 * scale;
        double glassH = (seatY - topY) - (6 * scale);
        var glassBrush = new LinearGradientBrush(
            Color.FromArgb(100, 190, 230, 255),
            Color.FromArgb(60, 150, 200, 230),
            new Point(0, 0),
            new Point(1, 1));
        dc.DrawRoundedRectangle(glassBrush, null, new Rect(x - (partW * 0.5) + glassMargin, topY + (3 * scale), partW - (glassMargin * 2), glassH), 1.5 * scale, 1.5 * scale);

        // Glass reflection sheen
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 1.0 * scale), new Point(x - 2 * scale, topY + 5 * scale), new Point(x + 2 * scale, topY + (glassH - 2 * scale)));

        // 3. Vertical Stainless Steel Grab Stanchion Pole
        double poleX = x + (partW * 0.5) - (1.5 * scale);
        double poleTopY = topY - (16 * scale);
        var poleBrush = new LinearGradientBrush(
            Color.FromRgb(235, 240, 250),
            Color.FromRgb(165, 175, 190),
            new Point(0, 0),
            new Point(1, 0));
        dc.DrawRoundedRectangle(poleBrush, new Pen(new SolidColorBrush(Color.FromRgb(120, 130, 145)), 0.8 * scale), new Rect(poleX - (1.6 * scale), poleTopY, 3.2 * scale, floorY - poleTopY), 1.5 * scale, 1.5 * scale);

        // 4. Iconic Tokyo Metro High-Visibility Safety Orange Non-Slip Grip Sleeve
        double gripTopY = topY - (8 * scale);
        double gripH = 22 * scale;
        var gripBrush = new LinearGradientBrush(
            Color.FromRgb(255, 175, 55),
            Color.FromRgb(220, 130, 30),
            new Point(0, 0),
            new Point(1, 0));
        dc.DrawRoundedRectangle(gripBrush, new Pen(new SolidColorBrush(Color.FromRgb(190, 105, 20)), 0.8 * scale), new Rect(poleX - (2.2 * scale), gripTopY, 4.4 * scale, gripH), 2 * scale, 2 * scale);

        // Subtle grip rings
        for (double gy = gripTopY + (3 * scale); gy < gripTopY + gripH - (2 * scale); gy += (4 * scale))
        {
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 220, 150)), 0.8 * scale), new Point(poleX - (2.0 * scale), gy), new Point(poleX + (2.0 * scale), gy));
        }

        // 5. Padded Armrest Cap matching Emerald Velvet Moquette
        dc.DrawRoundedRectangle(
            new SolidColorBrush(Color.FromRgb(32, 112, 78)),
            new Pen(new SolidColorBrush(Color.FromRgb(18, 70, 48)), 0.8 * scale),
            new Rect(x - (partW * 0.5) - (1.5 * scale), seatY - (4 * scale), partW + (3 * scale), 6 * scale),
            2 * scale,
            2 * scale);
    }

    /// <summary>
    /// Draws seated legs (thighs bent at the knee, shins down to the floor) with cute shoes.
    /// Anchoring the lower body to the floor is what makes the character read as sitting on
    /// the train seat instead of floating above it.
    /// </summary>
    private void DrawSeatedGirlLegs(DrawingContext dc, double girlX, double hipY, double floorY, double scale, Color legColor, Color shoeColor, double spreadScale = 1.0)
    {
        var legPen = new Pen(new SolidColorBrush(legColor), 6.5 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        double kneeY = hipY + (9 * scale);
        double kneeSpread = 6.5 * scale * spreadScale;
        double footSpread = 5.5 * scale * spreadScale;

        Point leftHip = new Point(girlX - (4.5 * scale), hipY);
        Point leftKnee = new Point(girlX - kneeSpread, kneeY);
        Point leftFoot = new Point(girlX - footSpread, floorY);

        Point rightHip = new Point(girlX + (4.5 * scale), hipY);
        Point rightKnee = new Point(girlX + kneeSpread, kneeY);
        Point rightFoot = new Point(girlX + footSpread, floorY);

        dc.DrawLine(legPen, leftHip, leftKnee);
        dc.DrawLine(legPen, leftKnee, leftFoot);
        dc.DrawLine(legPen, rightHip, rightKnee);
        dc.DrawLine(legPen, rightKnee, rightFoot);

        // Cute little shoes resting on the train floor
        var shoeBrush = new SolidColorBrush(shoeColor);
        dc.DrawRoundedRectangle(shoeBrush, null, new Rect(leftFoot.X - (4 * scale), floorY - (2 * scale), 8 * scale, 4 * scale), 2, 2);
        dc.DrawRoundedRectangle(shoeBrush, null, new Rect(rightFoot.X - (4 * scale), floorY - (2 * scale), 8 * scale, 4 * scale), 2, 2);
    }

    /// <summary>
    /// A small pastel backpack resting on the seat beside her - reinforces that she's a
    /// commuter sitting with her things, not a disconnected floating sprite.
    /// </summary>
    private void DrawSeatBackpack(DrawingContext dc, double x, double seatTopY, double scale)
    {
        double bpW = 13 * scale;
        double bpH = 15 * scale;
        double bpY = seatTopY - bpH + (3 * scale);

        var bpBrush = new LinearGradientBrush(Color.FromRgb(255, 195, 210), Color.FromRgb(235, 150, 175), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(bpBrush, new Pen(new SolidColorBrush(Color.FromRgb(205, 115, 145)), 1), new Rect(x, bpY, bpW, bpH), 4, 4);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 220, 228)), null, new Rect(x + (2 * scale), bpY + (2 * scale), bpW - (4 * scale), bpH * 0.38), 3, 3);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(225, 140, 165)), null, new Point(x + (bpW * 0.5), bpY + (2 * scale)), 1.6 * scale, 1.6 * scale);
    }

    private void DrawMetroLoFiGirl(DrawingContext dc, double w, double seatY, double progress, bool isTracking, bool isMini)
    {
        double scale = isMini ? 0.60 : 1.0;
        double girlX = isMini ? (w * 0.68) : (w * 0.72);
        double headBob = Math.Sin(_frameTick * 0.22) * (2.0 * scale);

        // Anchor the whole pose to the seat: hip sinks slightly into the cushion, knees bend
        // forward, and feet rest on the carriage floor at the very bottom of the scene.
        double hipY = seatY - (2 * scale) + headBob;
        double floorY = (isMini ? seatY + (8.5 * scale) : seatY + (26 * scale));
        double girlY = hipY - (48 * scale);

        // Chrome armrest divider grounding her seat within the carriage
        if (!isMini)
        {
            DrawMetroArmrestDivider(dc, girlX - (22 * scale), seatY, floorY, scale);
        }

        // Backpack resting on the seat beside her
        if (!isMini)
        {
            DrawSeatBackpack(dc, girlX + (16 * scale), seatY, scale);
        }

        // 0. Seated Legs & Shoes (drawn first so the torso/skirt overlaps the hip naturally)
        DrawSeatedGirlLegs(dc, girlX, hipY, floorY, scale, Color.FromRgb(70, 60, 90), Color.FromRgb(255, 250, 245));

        // 1. Cozy Pastel Lavender Hoodie Body
        var hoodieBrush = new LinearGradientBrush(
            Color.FromRgb(200, 180, 235),
            Color.FromRgb(165, 140, 205),
            new Point(0, 0),
            new Point(0, 1));
        var hoodiePen = new Pen(new SolidColorBrush(Color.FromRgb(140, 115, 180)), 1.2 * scale);

        // Body torso
        dc.DrawRoundedRectangle(hoodieBrush, hoodiePen, new Rect(girlX - (14 * scale), girlY + (22 * scale), 28 * scale, 26 * scale), 6 * scale, 6 * scale);

        // Cozy scarf / collar
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 245, 230)), null, new Rect(girlX - (11 * scale), girlY + (18 * scale), 22 * scale, 8 * scale), 4 * scale, 4 * scale);

        // 2. Chibi Anime Head & Face
        var skinBrush = new SolidColorBrush(Color.FromRgb(255, 224, 205));
        var blushBrush = new SolidColorBrush(Color.FromArgb(170, 255, 125, 150));
        var eyeBrush = new SolidColorBrush(Color.FromRgb(60, 40, 35));

        dc.DrawEllipse(skinBrush, null, new Point(girlX, girlY + (10 * scale)), 11 * scale, 10 * scale);
        // Blushing cheeks
        dc.DrawEllipse(blushBrush, null, new Point(girlX - (6 * scale), girlY + (12 * scale)), 2.8 * scale, 1.8 * scale);
        dc.DrawEllipse(blushBrush, null, new Point(girlX + (6 * scale), girlY + (12 * scale)), 2.8 * scale, 1.8 * scale);

        // Eyes: Relaxed peaceful gaze or gentle smile (^_^)
        dc.DrawLine(new Pen(eyeBrush, 1.6 * scale), new Point(girlX - (7 * scale), girlY + (9 * scale)), new Point(girlX - (3 * scale), girlY + (10 * scale)));
        dc.DrawLine(new Pen(eyeBrush, 1.6 * scale), new Point(girlX + (3 * scale), girlY + (10 * scale)), new Point(girlX + (7 * scale), girlY + (9 * scale)));

        // Cute smiling mouth curve
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(210, 100, 120)), 1.2 * scale), new Point(girlX - (2 * scale), girlY + (15 * scale)), new Point(girlX + (2 * scale), girlY + (15 * scale)));

        // 3. Cute Anime Hair with Bangs & Side Strands
        var hairBrush = new SolidColorBrush(Color.FromRgb(85, 55, 45));
        // Back hair
        dc.DrawEllipse(hairBrush, null, new Point(girlX, girlY + (8 * scale)), 13 * scale, 12 * scale);
        // Bangs
        dc.DrawRoundedRectangle(hairBrush, null, new Rect(girlX - (11 * scale), girlY, 22 * scale, 8 * scale), 4 * scale, 4 * scale);
        // Side strands
        dc.DrawRoundedRectangle(hairBrush, null, new Rect(girlX - (12 * scale), girlY + (4 * scale), 4 * scale, 16 * scale), 2 * scale, 2 * scale);
        dc.DrawRoundedRectangle(hairBrush, null, new Rect(girlX + (8 * scale), girlY + (4 * scale), 4 * scale, 16 * scale), 2 * scale, 2 * scale);

        // 4. Stylish Over-Ear Headphones with Pulsing LED Ring
        var hpHeadband = new Pen(new SolidColorBrush(Color.FromRgb(240, 240, 245)), 2.5 * scale);
        var hpGeom = new PathGeometry();
        var hpf = new PathFigure { StartPoint = new Point(girlX - (12 * scale), girlY + (8 * scale)) };
        hpf.Segments.Add(new QuadraticBezierSegment(new Point(girlX, girlY - (4 * scale)), new Point(girlX + (12 * scale), girlY + (8 * scale)), true));
        hpGeom.Figures.Add(hpf);
        dc.DrawGeometry(null, hpHeadband, hpGeom);

        // Earcups
        var earcupBrush = new SolidColorBrush(Color.FromRgb(45, 50, 65));
        dc.DrawRoundedRectangle(earcupBrush, new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1), new Rect(girlX - (15 * scale), girlY + (4 * scale), 5 * scale, 12 * scale), 2.5 * scale, 2.5 * scale);
        dc.DrawRoundedRectangle(earcupBrush, new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1), new Rect(girlX + (10 * scale), girlY + (4 * scale), 5 * scale, 12 * scale), 2.5 * scale, 2.5 * scale);

        // Pulsing Neon LED Ring on Earcup
        double pulse = Math.Sin(_frameTick * 0.25) * 0.35 + 0.65;
        var ledColor = Color.FromArgb((byte)(220 * pulse), 90, 220, 255);
        dc.DrawEllipse(new SolidColorBrush(ledColor), null, new Point(girlX - (12.5 * scale), girlY + (10 * scale)), 2 * scale, 3 * scale);
        dc.DrawEllipse(new SolidColorBrush(ledColor), null, new Point(girlX + (12.5 * scale), girlY + (10 * scale)), 2 * scale, 3 * scale);

        // 4b. Hands resting on her lap holding a phone, with the headphone cord running up to her ear.
        // This ties the headphones to a visible source and gives her lap something to hold,
        // instead of leaving her arms invisible inside the hoodie block.
        double lapY = hipY - (6 * scale);
        var handBrush = new SolidColorBrush(Color.FromRgb(255, 224, 205));
        var sleevePen = new Pen(new SolidColorBrush(Color.FromRgb(180, 160, 215)), 4.5 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(sleevePen, new Point(girlX - (11 * scale), girlY + (26 * scale)), new Point(girlX - (7 * scale), lapY));
        dc.DrawLine(sleevePen, new Point(girlX + (11 * scale), girlY + (26 * scale)), new Point(girlX + (7 * scale), lapY));
        dc.DrawEllipse(handBrush, null, new Point(girlX - (7 * scale), lapY), 2.6 * scale, 2.6 * scale);
        dc.DrawEllipse(handBrush, null, new Point(girlX + (7 * scale), lapY), 2.6 * scale, 2.6 * scale);

        if (!isMini)
        {
            // Phone cradled in her hands
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(55, 52, 70)), new Pen(new SolidColorBrush(Color.FromRgb(30, 28, 42)), 1), new Rect(girlX - (4 * scale), lapY - (2 * scale), 8 * scale, 12 * scale), 2, 2);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(130, 220, 255)), null, new Rect(girlX - (3 * scale), lapY - (1 * scale), 6 * scale, 10 * scale), 1, 1);

            // Headphone cord looping from the phone up to her ear
            var cordPen = new Pen(new SolidColorBrush(Color.FromRgb(235, 235, 240)), 1.1 * scale);
            var cordGeom = new PathGeometry();
            var cordFig = new PathFigure { StartPoint = new Point(girlX, lapY - (2 * scale)) };
            cordFig.Segments.Add(new QuadraticBezierSegment(new Point(girlX + (10 * scale), girlY + (4 * scale)), new Point(girlX + (12.5 * scale), girlY + (9 * scale)), true));
            cordGeom.Figures.Add(cordFig);
            dc.DrawGeometry(null, cordPen, cordGeom);
        }

        // 5. Floating Lo-Fi Music Notes Rising
        if (isTracking || isMini)
        {
            string[] notes = { "♪", "♫", "♩", "♬" };
            var noteColors = new[] { Color.FromRgb(255, 140, 185), Color.FromRgb(120, 225, 255), Color.FromRgb(255, 225, 110), Color.FromRgb(190, 160, 255) };

            for (int n = 0; n < (isMini ? 2 : 4); n++)
            {
                double noteOffset = ((_frameTick * 1.2) + (n * 25)) % 65;
                double nx = girlX - (18 * scale) + (Math.Sin((_frameTick * 0.15) + n) * (14 * scale));
                double ny = girlY - noteOffset;

                if (ny > 8)
                {
                    double alpha = Math.Clamp(1.0 - (noteOffset / 65.0), 0.1, 0.9);
                    var nBrush = new SolidColorBrush(Color.FromArgb((byte)(230 * alpha), noteColors[n % noteColors.Length].R, noteColors[n % noteColors.Length].G, noteColors[n % noteColors.Length].B));
                    var ft = CreateText(notes[n % notes.Length], (isMini ? 10 : 13) * scale, nBrush, FontWeights.Bold);
                    dc.DrawText(ft, new Point(nx, ny));
                }
            }
        }

        // 6. Canned Royal Milk Tea on Window Sill
        if (!isMini)
        {
            double canX = girlX - (28 * scale);
            double canY = seatY - (14 * scale);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(240, 205, 120)), new Pen(new SolidColorBrush(Color.FromRgb(180, 140, 70)), 1), new Rect(canX, canY, 9 * scale, 14 * scale), 2, 2);
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new Rect(canX + 1, canY + 3, 7 * scale, 4 * scale));
            // Steam from hot can
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), null, new Point(canX + (4.5 * scale), canY - 3), 2, 3);
        }
    }

    private void DrawMetroGoalGirl(DrawingContext dc, double w, double seatY, bool isMini)
    {
        double scale = isMini ? 0.60 : 1.0;
        double girlX = isMini ? (w * 0.68) : (w * 0.72);
        double hipY = seatY - (2 * scale);
        double floorY = (isMini ? seatY + (8.5 * scale) : seatY + (26 * scale));
        double girlY = hipY - (48 * scale);
        double kick = Math.Sin(_frameTick * 0.35) * (4 * scale);

        if (!isMini)
        {
            DrawMetroArmrestDivider(dc, girlX - (22 * scale), seatY, floorY, scale);
            DrawSeatBackpack(dc, girlX + (16 * scale), seatY, scale);
        }

        // Cheerfully kicking legs (celebrating in her seat!)
        DrawSeatedGirlLegs(dc, girlX, hipY - kick * 0.3, floorY - kick, scale, Color.FromRgb(70, 60, 90), Color.FromRgb(255, 250, 245), 1.15);

        // Body
        var hoodieBrush = new SolidColorBrush(Color.FromRgb(215, 190, 245));
        dc.DrawRoundedRectangle(hoodieBrush, null, new Rect(girlX - (14 * scale), girlY + (22 * scale), 28 * scale, 26 * scale), 6 * scale, 6 * scale);

        // Head
        var skinBrush = new SolidColorBrush(Color.FromRgb(255, 224, 205));
        dc.DrawEllipse(skinBrush, null, new Point(girlX, girlY + (10 * scale)), 11 * scale, 10 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(190, 255, 125, 150)), null, new Point(girlX - (6 * scale), girlY + (12 * scale)), 3 * scale, 2 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(190, 255, 125, 150)), null, new Point(girlX + (6 * scale), girlY + (12 * scale)), 3 * scale, 2 * scale);

        // Sparkling Joyful Eyes (^o^)
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(50, 30, 25)), null, new Point(girlX - (5 * scale), girlY + (9 * scale)), 2.8 * scale, 2.8 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new Point(girlX - (5.5 * scale), girlY + (8.2 * scale)), 1.2 * scale, 1.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(50, 30, 25)), null, new Point(girlX + (5 * scale), girlY + (9 * scale)), 2.8 * scale, 2.8 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new Point(girlX + (4.5 * scale), girlY + (8.2 * scale)), 1.2 * scale, 1.2 * scale);

        // Happy open smile
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(230, 80, 105)), null, new Point(girlX, girlY + (15 * scale)), 3 * scale, 2.5 * scale);

        // Hair
        var hairBrush = new SolidColorBrush(Color.FromRgb(85, 55, 45));
        dc.DrawRoundedRectangle(hairBrush, null, new Rect(girlX - (11 * scale), girlY, 22 * scale, 8 * scale), 4 * scale, 4 * scale);

        // Headphones around neck in celebration
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(45, 50, 65)), null, new Rect(girlX - (12 * scale), girlY + (18 * scale), 24 * scale, 6 * scale), 3 * scale, 3 * scale);

        // Victory Peace Sign v(^_^)v
        double armY = girlY + (14 * scale);
        var armPen = new Pen(skinBrush, 2.4 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(armPen, new Point(girlX - (11 * scale), girlY + (26 * scale)), new Point(girlX - (16 * scale), armY));
        dc.DrawEllipse(skinBrush, null, new Point(girlX - (16 * scale), armY), 3.5 * scale, 3.5 * scale);
        dc.DrawLine(new Pen(skinBrush, 2.2 * scale), new Point(girlX - (16 * scale), armY), new Point(girlX - (19 * scale), armY - (8 * scale)));
        dc.DrawLine(new Pen(skinBrush, 2.2 * scale), new Point(girlX - (16 * scale), armY), new Point(girlX - (13 * scale), armY - (8 * scale)));

        // Other arm resting cheerfully on her lap
        dc.DrawLine(armPen, new Point(girlX + (11 * scale), girlY + (26 * scale)), new Point(girlX + (8 * scale), hipY - (6 * scale)));
        dc.DrawEllipse(skinBrush, null, new Point(girlX + (8 * scale), hipY - (6 * scale)), 2.6 * scale, 2.6 * scale);

        // Celebration Banner on top left
        double bannerW = isMini ? 150 : 270;
        double bannerH = isMini ? 18 : 24;
        var bBrush = new LinearGradientBrush(Color.FromRgb(255, 225, 110), Color.FromRgb(255, 185, 60), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(bBrush, new Pen(new SolidColorBrush(Color.FromRgb(215, 140, 30)), 1), new Rect(isMini ? 10 : 24, isMini ? 4 : 10, bannerW, bannerH), 4, 4);

        string text = isMini ? "✨ ARRIVED! ✨" : "✨ 🚉 TOKYO METRO: DESTINATION REACHED! ✨";
        var ft = CreateText(text, isMini ? 9.5 : 11.5, new SolidColorBrush(Color.FromRgb(60, 35, 10)), FontWeights.Bold);
        dc.DrawText(ft, new Point((isMini ? 10 : 24) + ((bannerW - ft.Width) * 0.5), (isMini ? 4 : 10) + ((bannerH - ft.Height) * 0.5)));

        // Floating sparkles
        DrawSparkle(dc, girlX + (18 * scale), girlY, 6 * scale, new SolidColorBrush(Color.FromRgb(255, 230, 110)));
        DrawSparkle(dc, girlX - (22 * scale), girlY - 6, 5 * scale, new SolidColorBrush(Color.FromRgb(255, 140, 190)));
    }

    private void DrawMetroRestGirl(DrawingContext dc, double w, double seatY, bool isMini)
    {
        double scale = isMini ? 0.60 : 1.0;
        double girlX = isMini ? (w * 0.68) : (w * 0.72);
        double hipY = seatY - (2 * scale);
        double floorY = (isMini ? seatY + (8.5 * scale) : seatY + (26 * scale));
        double girlY = hipY - (44 * scale);

        if (!isMini)
        {
            DrawMetroArmrestDivider(dc, girlX - (22 * scale), seatY, floorY, scale);
            DrawSeatBackpack(dc, girlX + (16 * scale), seatY, scale);
        }

        // Legs relaxed, slightly apart, resting on the floor while she naps
        DrawSeatedGirlLegs(dc, girlX, hipY, floorY, scale, Color.FromRgb(70, 60, 90), Color.FromRgb(255, 250, 245), 1.3);

        // Body leaning on arm
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(190, 175, 225)), null, new Rect(girlX - (14 * scale), girlY + (20 * scale), 28 * scale, 24 * scale), 6 * scale, 6 * scale);

        // Head resting peacefully
        var skinBrush = new SolidColorBrush(Color.FromRgb(255, 224, 205));
        dc.DrawEllipse(skinBrush, null, new Point(girlX, girlY + (10 * scale)), 11 * scale, 10 * scale);
        // Sleepy eyes ( ᴗ ᴗ)
        var eyePen = new Pen(new SolidColorBrush(Color.FromRgb(70, 45, 40)), 1.5 * scale);
        dc.DrawLine(eyePen, new Point(girlX - (6 * scale), girlY + (11 * scale)), new Point(girlX - (2 * scale), girlY + (11 * scale)));
        dc.DrawLine(eyePen, new Point(girlX + (2 * scale), girlY + (11 * scale)), new Point(girlX + (6 * scale), girlY + (11 * scale)));

        // Hair
        var hairBrush = new SolidColorBrush(Color.FromRgb(85, 55, 45));
        dc.DrawRoundedRectangle(hairBrush, null, new Rect(girlX - (11 * scale), girlY, 22 * scale, 8 * scale), 4 * scale, 4 * scale);

        // Headphones on head
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(45, 50, 65)), null, new Rect(girlX - (15 * scale), girlY + (4 * scale), 5 * scale, 12 * scale), 2.5 * scale, 2.5 * scale);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(45, 50, 65)), null, new Rect(girlX + (10 * scale), girlY + (4 * scale), 5 * scale, 12 * scale), 2.5 * scale, 2.5 * scale);

        // Arms resting loosely in her lap
        var armPen = new Pen(skinBrush, 2.4 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(armPen, new Point(girlX - (11 * scale), girlY + (24 * scale)), new Point(girlX - (6 * scale), hipY - (7 * scale)));
        dc.DrawLine(armPen, new Point(girlX + (11 * scale), girlY + (24 * scale)), new Point(girlX + (6 * scale), hipY - (7 * scale)));
        dc.DrawEllipse(skinBrush, null, new Point(girlX - (6 * scale), hipY - (7 * scale)), 2.4 * scale, 2.4 * scale);
        dc.DrawEllipse(skinBrush, null, new Point(girlX + (6 * scale), hipY - (7 * scale)), 2.4 * scale, 2.4 * scale);

        // Floating Dream / Snooze "💤 z Z Z"
        string zText = (_frameTick % 20 < 10) ? "💤 z Z Z" : "💤 Z z z";
        var zFt = CreateText(zText, isMini ? 10 : 13, new SolidColorBrush(Color.FromRgb(190, 150, 255)), FontWeights.Bold);
        dc.DrawText(zFt, new Point(girlX - (26 * scale), girlY - (16 * scale)));

        // Rest Banner
        string text = isMini ? "🎧 🍵 Tokyo Rest Break" : "🎧 🍵 Tokyo Rest Break • Lo-Fi Chill & Breathe ✨";
        var ft = CreateText(text, isMini ? 9.5 : 12, new SolidColorBrush(Color.FromRgb(150, 235, 185)), FontWeights.Bold);
        dc.DrawText(ft, new Point(isMini ? 10 : 20, isMini ? 4 : 8));
    }

    #endregion

    #region 🏃 Scene 5: Chibi Marathon Runner & Shiba Inu

    private void RenderRunnerScene(DrawingContext dc, double w, double h)
    {
        double trackY = h * 0.72;

        // 1. Vibrant Sunset Athletic Park Sky
        var skyBrush = new LinearGradientBrush(Color.FromRgb(70, 130, 210), Color.FromRgb(255, 175, 130), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(skyBrush, null, new Rect(0, 0, w, trackY));

        // Celebratory Bunting Flags across top
        if (!IsMiniMode)
        {
            var flagColors = new[] { Color.FromRgb(255, 95, 95), Color.FromRgb(255, 215, 60), Color.FromRgb(80, 200, 120), Color.FromRgb(70, 160, 245) };
            for (double fx = 20; fx < w; fx += 35)
            {
                var fgeom = new PathGeometry();
                var ffig = new PathFigure { StartPoint = new Point(fx, 10) };
                ffig.Segments.Add(new LineSegment(new Point(fx + 15, 10), true));
                ffig.Segments.Add(new LineSegment(new Point(fx + 7.5, 24), true));
                fgeom.Figures.Add(ffig);
                dc.DrawGeometry(new SolidColorBrush(flagColors[(int)(fx / 35) % flagColors.Length]), null, fgeom);
            }
        }

        // Red Running Track
        var trackBrush = new LinearGradientBrush(Color.FromRgb(215, 80, 65), Color.FromRgb(175, 55, 45), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(trackBrush, null, new Rect(0, trackY, w, h - trackY));
        dc.DrawLine(new Pen(new SolidColorBrush(Colors.White), 1.8), new Point(0, trackY + 8), new Point(w, trackY + 8));

        // Finish Line on Right (100%)
        double finishX = w - (IsMiniMode ? 28 : 60);
        DrawCuteFinishLine(dc, finishX, trackY, IsMiniMode);

        // Runner & Shiba Movement
        double startX = IsMiniMode ? 16 : 28;
        double endX = finishX - (IsMiniMode ? 14 : 26);
        double runnerX = startX + (ProgressFraction * (endX - startX));

        if (IsRestPhase)
        {
            DrawCuteRestRunner(dc, w * 0.5, trackY, IsMiniMode);
        }
        else if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawCuteVictoryRunner(dc, finishX, trackY, IsMiniMode);
        }
        else
        {
            DrawCuteActiveRunnerAndShiba(dc, runnerX, trackY, IsMiniMode, IsTracking);
        }
    }

    private void DrawCuteFinishLine(DrawingContext dc, double x, double trackY, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;
        double postH = 38 * scale;

        // Checkered Posts
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(45, 45, 50)), null, new Rect(x, trackY - postH, 4 * scale, postH));

        // Bright Red Breakable Ribbon
        if (!IsGoalReached && ProgressFraction < 0.999)
        {
            var ribbonPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 60, 60)), 3.5 * scale);
            dc.DrawLine(ribbonPen, new Point(x, trackY - (postH * 0.55)), new Point(x - (14 * scale), trackY - (postH * 0.55)));
        }
    }

    private void DrawCuteActiveRunnerAndShiba(DrawingContext dc, double x, double trackY, bool isMini, bool isRunning)
    {
        double scale = isMini ? 0.72 : 1.0;
        double runCycle = isRunning ? (_frameTick * 0.55) : 0;
        double legSwing = Math.Sin(runCycle) * 8 * scale;
        double bounce = isRunning ? Math.Abs(Math.Sin(runCycle)) * 2 * scale : 0;

        Point hip = new Point(x, trackY - (17 * scale) - bounce);
        Point head = new Point(x + (6 * scale), trackY - (32 * scale) - bounce);

        // Runner Legs
        var legPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 215, 180)), 3.2 * scale);
        dc.DrawLine(legPen, hip, new Point(hip.X + legSwing, trackY));
        dc.DrawLine(legPen, hip, new Point(hip.X - legSwing, trackY));

        // Cute Cyan Jersey
        var jerseyPen = new Pen(new SolidColorBrush(Color.FromRgb(50, 160, 245)), 7 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(jerseyPen, hip, new Point(head.X - (2 * scale), head.Y + (6 * scale)));

        // Pumping Chibi Arms
        var armPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 215, 180)), 3 * scale);
        dc.DrawLine(armPen, new Point(head.X - (2 * scale), head.Y + (8 * scale)), new Point(head.X + (9 * scale) - legSwing, head.Y + (14 * scale)));

        // Cute Chibi Head
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 180)), null, head, 6.5 * scale, 6.5 * scale);
        // Blushing cheeks & eyes
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 120, 140)), null, new Point(head.X + (2 * scale), head.Y + (2 * scale)), 2.5 * scale, 1.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(40, 30, 25)), null, new Point(head.X + (3 * scale), head.Y - (0.5 * scale)), 1.4 * scale, 1.8 * scale);

        // Red Fluttering Headband
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 65, 65)), 2.5 * scale), new Point(head.X - (5 * scale), head.Y - (2 * scale)), new Point(head.X + (6 * scale), head.Y - (2 * scale)));
        // Headband tails in wind
        double tailWave = Math.Sin(_frameTick * 0.7) * 3 * scale;
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 65, 65)), 2 * scale), new Point(head.X - (5 * scale), head.Y - (2 * scale)), new Point(head.X - (13 * scale), head.Y - (4 * scale) + tailWave));

        // RUNNING SHIBA INU COMPANION 🐕
        if (!isMini)
        {
            Point shiba = new Point(x - 22, trackY - 12 - (isRunning ? Math.Abs(Math.Sin(runCycle)) * 3 : 0));
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(235, 160, 80)), null, shiba, 8, 6);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(235, 160, 80)), null, new Point(shiba.X + 7, shiba.Y - 4), 5, 5);
            // Shiba cute face & tongue
            dc.DrawEllipse(new SolidColorBrush(Colors.Black), null, new Point(shiba.X + 9, shiba.Y - 5), 1, 1);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 120, 160)), null, new Point(shiba.X + 11, shiba.Y - 2), 2, 1.5);
        }
    }

    private void DrawCuteVictoryRunner(DrawingContext dc, double x, double trackY, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;
        Point head = new Point(x, trackY - (34 * scale));

        // Arms raised in victory \o/
        var armPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 215, 180)), 3 * scale);
        dc.DrawLine(armPen, new Point(head.X, head.Y + (8 * scale)), new Point(head.X - (11 * scale), head.Y - (8 * scale)));
        dc.DrawLine(armPen, new Point(head.X, head.Y + (8 * scale)), new Point(head.X + (11 * scale), head.Y - (8 * scale)));

        // Head
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 180)), null, head, 6.5 * scale, 6.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 120, 140)), null, new Point(head.X, head.Y + (2 * scale)), 3 * scale, 1.5 * scale);

        // Golden Star Trophy 🏆
        DrawSparkle(dc, head.X, head.Y - (18 * scale), 7 * scale, new SolidColorBrush(Color.FromRgb(255, 220, 60)));

        string text = isMini ? "🏆 FINISH! 🏆" : "🏆 CHAMPION! FINISH LINE CROSSED! 🥇✨";
        var ft = CreateText(text, isMini ? 10 : 13, new SolidColorBrush(Color.FromRgb(255, 220, 60)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.5), trackY - (48 * scale)));
    }

    private void DrawCuteRestRunner(DrawingContext dc, double x, double trackY, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;

        // Grassy bench
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(150, 90, 50)), null, new Rect(x - (20 * scale), trackY - (10 * scale), 40 * scale, 5 * scale));

        // Runner resting sipping juice box 🧃
        Point head = new Point(x, trackY - (24 * scale));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 180)), null, head, 6 * scale, 6 * scale);

        string text = isMini ? "💧 Hydrate" : "💧 Hydrate & Cool Down! 🧃✨";
        var ft = CreateText(text, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(110, 215, 255)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.5), trackY - (36 * scale)));
    }

    #endregion

    #region 👾 Scene 6: Ultra-Kawaii Focus Pet Tamagotchi

    private enum TamagotchiPetState
    {
        Active,
        Rest,
        Goal
    }

    private void RenderTamagotchiScene(DrawingContext dc, double w, double h)
    {
        Rect shellRect = new Rect(4, 4, w - 8, h - 8);
        var shellBrush = new LinearGradientBrush(
            Color.FromRgb(242, 230, 255),
            Color.FromRgb(206, 243, 232),
            new Point(0, 0),
            new Point(1, 1));
        var shellPen = new Pen(new SolidColorBrush(Color.FromRgb(178, 156, 214)), IsMiniMode ? 1.6 : 2.2);
        dc.DrawRoundedRectangle(shellBrush, shellPen, shellRect, IsMiniMode ? 12 : 16, IsMiniMode ? 12 : 16);

        dc.DrawRoundedRectangle(
            new LinearGradientBrush(Color.FromArgb(90, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), new Point(0, 0), new Point(0, 1)),
            null,
            new Rect(shellRect.Left + 4, shellRect.Top + 4, shellRect.Width - 8, shellRect.Height * 0.28),
            IsMiniMode ? 10 : 14,
            IsMiniMode ? 10 : 14);

        double screenMargin = IsMiniMode ? 8 : 14;
        Rect screenRect = new Rect(screenMargin, screenMargin, w - (screenMargin * 2), h - (screenMargin * 2));
        var bezelPen = new Pen(new SolidColorBrush(Color.FromRgb(88, 76, 126)), IsMiniMode ? 1.2 : 1.8);
        var screenBrush = new LinearGradientBrush(Color.FromRgb(24, 28, 48), Color.FromRgb(18, 22, 40), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(screenBrush, bezelPen, screenRect, 8, 8);

        if (!IsMiniMode)
        {
            DrawTamagotchiConsoleControls(dc, shellRect, screenRect);
        }

        if (IsMiniMode)
        {
            DrawTamagotchiMiniScene(dc, screenRect);
        }
        else
        {
            dc.PushClip(new RectangleGeometry(screenRect, 7, 7));
            if (IsGoalReached || ProgressFraction >= 0.999)
            {
                DrawTamagotchiGoalScene(dc, screenRect);
            }
            else if (IsRestPhase)
            {
                DrawTamagotchiRestScene(dc, screenRect);
            }
            else
            {
                DrawTamagotchiActiveScene(dc, screenRect);
            }
            dc.Pop();

            DrawTamagotchiScreenGlass(dc, screenRect);
        }

        DrawTamagotchiHud(dc, screenRect);
    }

    private void DrawTamagotchiConsoleControls(DrawingContext dc, Rect shellRect, Rect screenRect)
    {
        double dpadCenterX = shellRect.Left + (shellRect.Width * 0.22);
        double dpadCenterY = shellRect.Bottom - 28;
        var dpadBrush = new LinearGradientBrush(Color.FromRgb(138, 126, 176), Color.FromRgb(96, 86, 136), new Point(0, 0), new Point(0, 1));
        var dpadPen = new Pen(new SolidColorBrush(Color.FromRgb(78, 68, 108)), 1.1);
        dc.DrawRoundedRectangle(dpadBrush, dpadPen, new Rect(dpadCenterX - 16, dpadCenterY - 5, 32, 10), 3, 3);
        dc.DrawRoundedRectangle(dpadBrush, dpadPen, new Rect(dpadCenterX - 5, dpadCenterY - 16, 10, 32), 3, 3);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), null, new Point(dpadCenterX - 3, dpadCenterY - 9), 3.5, 2.0);

        double buttonY = shellRect.Bottom - 30;
        for (int i = 0; i < 2; i++)
        {
            double bx = shellRect.Right - 40 + (i * 16);
            var buttonBrush = new RadialGradientBrush(Color.FromRgb(255, 170, 215), Color.FromRgb(214, 110, 176));
            dc.DrawEllipse(buttonBrush, new Pen(new SolidColorBrush(Color.FromRgb(165, 80, 132)), 1.0), new Point(bx, buttonY + (i == 0 ? 4 : -2)), 6.5, 6.5);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), null, new Point(bx - 1.5, buttonY + (i == 0 ? 2 : -4)), 2.0, 1.2);
        }

        for (int s = 0; s < 4; s++)
        {
            double sy = shellRect.Bottom - 18 + (s * 4.5);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(120, 110, 96, 150)), 1.0), new Point(screenRect.Right - 34, sy), new Point(screenRect.Right - 10, sy));
        }

        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(126, 232, 145)), null, new Point(screenRect.Left + 6, shellRect.Top + 6), 2.5, 2.5);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), null, new Point(screenRect.Left + 5.2, shellRect.Top + 5.2), 1.1, 1.1);
    }

    private void DrawTamagotchiMiniScene(DrawingContext dc, Rect screenRect)
    {
        double floorY = screenRect.Bottom - 18;
        var bgBrush = new LinearGradientBrush(
            IsRestPhase ? Color.FromRgb(32, 34, 74) : (IsGoalReached ? Color.FromRgb(82, 44, 112) : Color.FromRgb(72, 92, 132)),
            IsRestPhase ? Color.FromRgb(16, 18, 36) : (IsGoalReached ? Color.FromRgb(42, 24, 72) : Color.FromRgb(24, 34, 66)),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRoundedRectangle(bgBrush, null, screenRect, 7, 7);

        Rect windowRect = new Rect(screenRect.Left + 6, screenRect.Top + 9, 22, 12);
        DrawTamagotchiWindowView(
            dc,
            windowRect,
            IsRestPhase ? Color.FromRgb(30, 38, 90) : Color.FromRgb(130, 206, 255),
            IsRestPhase ? Color.FromRgb(70, 92, 168) : Color.FromRgb(250, 214, 172),
            IsRestPhase,
            IsGoalReached);

        dc.DrawRectangle(new LinearGradientBrush(Color.FromRgb(122, 94, 88), Color.FromRgb(82, 60, 58), new Point(0, 0), new Point(0, 1)), null, new Rect(screenRect.Left, floorY, screenRect.Width, screenRect.Bottom - floorY));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(196, 174, 148)), 0.8), new Point(screenRect.Left, floorY), new Point(screenRect.Right, floorY));

        Point petCenter = new Point(screenRect.Left + (screenRect.Width * 0.63), floorY - 10.5);
        TamagotchiPetState petState = IsGoalReached ? TamagotchiPetState.Goal : (IsRestPhase ? TamagotchiPetState.Rest : TamagotchiPetState.Active);
        DrawUltraKawaiiPet(dc, petCenter, 0.56, petState, true);

        if (IsTracking && !IsRestPhase && !IsGoalReached)
        {
            DrawTamagotchiOrbitingTreats(dc, petCenter, 0.58, 2, false);
        }
        else if (IsGoalReached)
        {
            DrawTamagotchiConfetti(dc, screenRect, 8, 0.72);
        }
        else if (IsRestPhase)
        {
            DrawTamagotchiSleepBubbles(dc, petCenter, 0.52);
        }
    }

    private void DrawTamagotchiActiveScene(DrawingContext dc, Rect screenRect)
    {
        Color skyTop = LerpColor(Color.FromRgb(138, 214, 255), Color.FromRgb(255, 168, 188), ProgressFraction * 0.65);
        Color skyBottom = LerpColor(Color.FromRgb(218, 243, 255), Color.FromRgb(255, 225, 184), ProgressFraction * 0.65);
        double floorY = DrawTamagotchiRoomShell(
            dc,
            screenRect,
            Color.FromRgb(86, 86, 132),
            Color.FromRgb(56, 60, 102),
            Color.FromRgb(138, 102, 90),
            Color.FromRgb(88, 66, 64),
            Color.FromArgb(110, 160, 255, 225));

        Rect windowRect = new Rect(screenRect.Left + 12, screenRect.Top + 14, screenRect.Width * 0.34, screenRect.Height * 0.28);
        DrawTamagotchiWindowView(dc, windowRect, skyTop, skyBottom, false, false);

        DrawTamagotchiWallShelf(dc, screenRect.Left + 14, screenRect.Top + 42, 0.95, false);
        DrawTamagotchiBed(dc, screenRect.Left + 14, floorY, 0.88, false, false);
        DrawTamagotchiToyChest(dc, screenRect.Right - 52, floorY, 0.92, true, false);
        DrawTamagotchiFoodBowl(dc, screenRect.Left + (screenRect.Width * 0.47), floorY, 0.82, false);
        DrawTamagotchiAmbientPixels(dc, screenRect, 14, Color.FromArgb(110, 130, 255, 220), true);

        Point petCenter = new Point(screenRect.Left + (screenRect.Width * 0.63), floorY - 19);
        DrawUltraKawaiiPet(dc, petCenter, 0.98, TamagotchiPetState.Active, false);
        DrawTamagotchiOrbitingTreats(dc, petCenter, 0.95, 3, true);
        DrawTamagotchiFocusMeter(dc, new Rect(screenRect.Right - 58, screenRect.Top + 13, 44, 12));
    }

    private void DrawTamagotchiRestScene(DrawingContext dc, Rect screenRect)
    {
        double floorY = DrawTamagotchiRoomShell(
            dc,
            screenRect,
            Color.FromRgb(46, 46, 88),
            Color.FromRgb(22, 24, 48),
            Color.FromRgb(78, 68, 96),
            Color.FromRgb(50, 42, 68),
            Color.FromArgb(90, 126, 146, 255));

        Rect windowRect = new Rect(screenRect.Left + 13, screenRect.Top + 13, screenRect.Width * 0.32, screenRect.Height * 0.27);
        DrawTamagotchiWindowView(dc, windowRect, Color.FromRgb(26, 34, 82), Color.FromRgb(76, 90, 164), true, false);

        DrawTamagotchiBed(dc, screenRect.Left + 12, floorY, 1.08, true, false);
        DrawTamagotchiToyChest(dc, screenRect.Right - 50, floorY, 0.90, false, true);
        DrawTamagotchiFoodBowl(dc, screenRect.Left + (screenRect.Width * 0.44), floorY, 0.80, true);
        DrawTamagotchiWallShelf(dc, screenRect.Left + 18, screenRect.Top + 42, 0.92, true);
        DrawTamagotchiAmbientPixels(dc, screenRect, 9, Color.FromArgb(85, 180, 205, 255), false);

        Point petCenter = new Point(screenRect.Left + (screenRect.Width * 0.31), floorY - 20);
        DrawUltraKawaiiPet(dc, petCenter, 0.94, TamagotchiPetState.Rest, false);
        DrawTamagotchiSleepBubbles(dc, petCenter, 0.96);

        var sleepyText = CreateText("💤 Snoozing & Recharging...", 12, new SolidColorBrush(Color.FromRgb(204, 226, 255)), FontWeights.Bold);
        dc.DrawText(sleepyText, new Point(screenRect.Right - sleepyText.Width - 16, floorY - 24));
    }

    private void DrawTamagotchiGoalScene(DrawingContext dc, Rect screenRect)
    {
        double floorY = DrawTamagotchiRoomShell(
            dc,
            screenRect,
            Color.FromRgb(104, 60, 134),
            Color.FromRgb(48, 28, 78),
            Color.FromRgb(155, 92, 86),
            Color.FromRgb(92, 54, 68),
            Color.FromArgb(130, 255, 220, 140));

        Rect windowRect = new Rect(screenRect.Left + 12, screenRect.Top + 12, screenRect.Width * 0.34, screenRect.Height * 0.27);
        DrawTamagotchiWindowView(dc, windowRect, Color.FromRgb(255, 174, 152), Color.FromRgb(255, 224, 176), false, true);

        Point rayCenter = new Point(screenRect.Left + (screenRect.Width * 0.63), screenRect.Top + (screenRect.Height * 0.42));
        DrawTamagotchiCelebrationRays(dc, screenRect, rayCenter);
        DrawTamagotchiBed(dc, screenRect.Left + 12, floorY, 0.88, false, true);
        DrawTamagotchiToyChest(dc, screenRect.Right - 54, floorY, 0.92, true, false);
        DrawTamagotchiTrophyStand(dc, screenRect.Left + (screenRect.Width * 0.19), floorY, 0.95);
        DrawTamagotchiAmbientPixels(dc, screenRect, 14, Color.FromArgb(110, 255, 206, 120), true);
        DrawTamagotchiConfetti(dc, screenRect, 18, 1.0);

        Point petCenter = new Point(screenRect.Left + (screenRect.Width * 0.62), floorY - 20);
        DrawUltraKawaiiPet(dc, petCenter, 1.02, TamagotchiPetState.Goal, false);
        DrawTamagotchiOrbitingTreats(dc, petCenter, 1.02, 4, true);

        var goalText = CreateText("👑 FOCUS MASTER! ✨", 12, new SolidColorBrush(Color.FromRgb(255, 239, 150)), FontWeights.Bold);
        dc.DrawText(goalText, new Point(screenRect.Right - goalText.Width - 16, floorY - 24));
    }

    private double DrawTamagotchiRoomShell(DrawingContext dc, Rect screenRect, Color wallTop, Color wallBottom, Color floorTop, Color floorBottom, Color glowColor)
    {
        double floorY = screenRect.Bottom - (screenRect.Height * 0.27);
        Rect wallRect = new Rect(screenRect.Left, screenRect.Top, screenRect.Width, floorY - screenRect.Top);
        Rect floorRect = new Rect(screenRect.Left, floorY, screenRect.Width, screenRect.Bottom - floorY);

        dc.DrawRectangle(new LinearGradientBrush(wallTop, wallBottom, new Point(0, 0), new Point(0, 1)), null, wallRect);
        dc.DrawEllipse(new RadialGradientBrush(glowColor, Color.FromArgb(0, glowColor.R, glowColor.G, glowColor.B)), null, new Point(screenRect.Left + (screenRect.Width * 0.48), screenRect.Top + (screenRect.Height * 0.24)), screenRect.Width * 0.56, screenRect.Height * 0.34);

        for (int i = 0; i < 6; i++)
        {
            double stripeX = screenRect.Left + (i * (screenRect.Width / 5.5));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)), 1.0), new Point(stripeX, wallRect.Top), new Point(stripeX, wallRect.Bottom));
        }

        dc.DrawRectangle(new LinearGradientBrush(floorTop, floorBottom, new Point(0, 0), new Point(0, 1)), null, floorRect);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(216, 194, 164)), 1.3), new Point(screenRect.Left, floorY), new Point(screenRect.Right, floorY));

        for (int i = 0; i < 5; i++)
        {
            double plankX = screenRect.Left + (i * (screenRect.Width / 4.5));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(80, 82, 56, 54)), 0.9), new Point(plankX, floorY + 2), new Point(plankX, screenRect.Bottom));
        }

        return floorY;
    }

    private void DrawTamagotchiWindowView(DrawingContext dc, Rect windowRect, Color skyTop, Color skyBottom, bool isNight, bool isParty)
    {
        var frameBrush = new LinearGradientBrush(Color.FromRgb(232, 228, 255), Color.FromRgb(174, 170, 214), new Point(0, 0), new Point(0, 1));
        var framePen = new Pen(new SolidColorBrush(Color.FromRgb(122, 118, 166)), 1.1);
        dc.DrawRoundedRectangle(frameBrush, framePen, windowRect, 5, 5);

        Rect glassRect = new Rect(windowRect.Left + 3, windowRect.Top + 3, windowRect.Width - 6, windowRect.Height - 6);
        dc.PushClip(new RectangleGeometry(glassRect, 4, 4));
        dc.DrawRectangle(new LinearGradientBrush(skyTop, skyBottom, new Point(0, 0), new Point(0, 1)), null, glassRect);

        if (isNight)
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(240, 250, 243, 178)), null, new Point(glassRect.Right - 12, glassRect.Top + 8), 5.2, 5.2);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), null, new Point(glassRect.Right - 10.4, glassRect.Top + 6.8), 1.2, 1.2);
        }
        else
        {
            var sunGlow = new RadialGradientBrush(Color.FromArgb(170, 255, 242, 185), Color.FromArgb(0, 255, 242, 185));
            dc.DrawEllipse(sunGlow, null, new Point(glassRect.Right - 10, glassRect.Top + 8), 12, 8);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 236, 166)), null, new Point(glassRect.Right - 12, glassRect.Top + 8), 4.4, 4.4);
        }

        for (int i = 0; i < (isNight ? 8 : 4); i++)
        {
            double x = glassRect.Left + 4 + ((i * 11.0 + (isNight ? 0 : _frameTick * 0.25)) % Math.Max(12.0, glassRect.Width - 8));
            double y = glassRect.Top + 4 + ((i * 7.0) % Math.Max(10.0, glassRect.Height - 8));
            if (isNight || isParty)
            {
                DrawSparkle(dc, x, y, isNight ? 1.5 : 1.8, new SolidColorBrush(Color.FromArgb(150, 255, 250, 200)));
            }
        }

        if (!isNight)
        {
            for (int i = 0; i < 3; i++)
            {
                double cloudX = glassRect.Left - 8 + (((_frameTick * 0.24) + (i * 22.0)) % (glassRect.Width + 16));
                double cloudY = glassRect.Top + 6 + (i * 4.5);
                var cloudBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255));
                dc.DrawEllipse(cloudBrush, null, new Point(cloudX, cloudY), 7, 3.8);
                dc.DrawEllipse(cloudBrush, null, new Point(cloudX - 5, cloudY + 1), 5, 2.8);
                dc.DrawEllipse(cloudBrush, null, new Point(cloudX + 5, cloudY + 1), 4.5, 2.6);
            }
        }

        double horizonY = glassRect.Bottom - (glassRect.Height * 0.28);
        dc.DrawRectangle(new SolidColorBrush(isNight ? Color.FromRgb(42, 54, 92) : Color.FromRgb(100, 142, 186)), null, new Rect(glassRect.Left, horizonY, glassRect.Width, glassRect.Bottom - horizonY));

        var hillBrush = new SolidColorBrush(isNight ? Color.FromRgb(28, 34, 64) : Color.FromRgb(76, 112, 146));
        var hillGeom = new PathGeometry();
        var hillFig = new PathFigure { StartPoint = new Point(glassRect.Left, glassRect.Bottom) };
        hillFig.Segments.Add(new BezierSegment(new Point(glassRect.Left + (glassRect.Width * 0.18), horizonY - 8), new Point(glassRect.Left + (glassRect.Width * 0.30), horizonY + 2), new Point(glassRect.Left + (glassRect.Width * 0.44), horizonY - 5), true));
        hillFig.Segments.Add(new BezierSegment(new Point(glassRect.Left + (glassRect.Width * 0.58), horizonY - 10), new Point(glassRect.Left + (glassRect.Width * 0.72), horizonY + 4), new Point(glassRect.Right, horizonY - 6), true));
        hillFig.Segments.Add(new LineSegment(new Point(glassRect.Right, glassRect.Bottom), true));
        hillFig.IsClosed = true;
        hillGeom.Figures.Add(hillFig);
        dc.DrawGeometry(hillBrush, null, hillGeom);

        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), 1.0), new Point(glassRect.Left + 5, glassRect.Top + 4), new Point(glassRect.Right - 4, glassRect.Bottom - 6));
        dc.Pop();
    }

    private void DrawTamagotchiWallShelf(DrawingContext dc, double x, double y, double scale, bool sleepy)
    {
        double shelfW = 40 * scale;
        double shelfH = 3 * scale;
        var woodBrush = new LinearGradientBrush(Color.FromRgb(188, 146, 122), Color.FromRgb(128, 94, 82), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(woodBrush, new Pen(new SolidColorBrush(Color.FromRgb(102, 76, 68)), 0.8), new Rect(x, y, shelfW, shelfH), 2, 2);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(80, 70, 45, 40)), 1.0), new Point(x + (8 * scale), y + shelfH), new Point(x + (12 * scale), y + (10 * scale)));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(80, 70, 45, 40)), 1.0), new Point(x + (shelfW - (8 * scale)), y + shelfH), new Point(x + (shelfW - (12 * scale)), y + (10 * scale)));

        dc.DrawRoundedRectangle(new SolidColorBrush(sleepy ? Color.FromRgb(120, 132, 198) : Color.FromRgb(118, 228, 164)), null, new Rect(x + (4 * scale), y - (8 * scale), 7 * scale, 8 * scale), 2, 2);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 194, 122)), null, new Rect(x + (14 * scale), y - (7 * scale), 5 * scale, 7 * scale), 1.8, 1.8);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 236, 166)), null, new Point(x + (29 * scale), y - (4 * scale)), 4.2 * scale, 4.2 * scale);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(118, 168, 102)), null, new Rect(x + (27 * scale), y - (2 * scale), 4 * scale, 4 * scale));
    }

    private void DrawTamagotchiBed(DrawingContext dc, double x, double floorY, double scale, bool withPillow, bool partyAccent)
    {
        Rect baseRect = new Rect(x, floorY - (16 * scale), 40 * scale, 12 * scale);
        var baseBrush = new LinearGradientBrush(Color.FromRgb(255, 224, 234), partyAccent ? Color.FromRgb(255, 174, 168) : Color.FromRgb(230, 176, 208), new Point(0, 0), new Point(1, 1));
        dc.DrawRoundedRectangle(baseBrush, new Pen(new SolidColorBrush(Color.FromRgb(188, 128, 156)), 1.0), baseRect, 5 * scale, 5 * scale);
        dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(255, 248, 252), Color.FromRgb(255, 222, 232), new Point(0, 0), new Point(0, 1)), null, new Rect(baseRect.Left + (3 * scale), baseRect.Top + (2 * scale), baseRect.Width - (6 * scale), baseRect.Height - (4 * scale)), 4 * scale, 4 * scale);

        if (withPillow)
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(230, 236, 255)), null, new Rect(baseRect.Left + (4 * scale), baseRect.Top - (3 * scale), 12 * scale, 7 * scale), 3 * scale, 3 * scale);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(180, 168, 194, 255)), null, new Rect(baseRect.Left + (3 * scale), baseRect.Top + (1 * scale), baseRect.Width - (6 * scale), 8 * scale), 4 * scale, 4 * scale);
        }
        else
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), null, new Rect(baseRect.Left + (3 * scale), baseRect.Top + (2 * scale), baseRect.Width * 0.45, 3 * scale), 2 * scale, 2 * scale);
        }
    }

    private void DrawTamagotchiToyChest(DrawingContext dc, double x, double floorY, double scale, bool open, bool sleepy)
    {
        Rect chestRect = new Rect(x, floorY - (18 * scale), 30 * scale, 16 * scale);
        var chestBrush = new LinearGradientBrush(
            sleepy ? Color.FromRgb(102, 116, 168) : Color.FromRgb(138, 120, 214),
            sleepy ? Color.FromRgb(70, 82, 126) : Color.FromRgb(94, 82, 168),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRoundedRectangle(chestBrush, new Pen(new SolidColorBrush(Color.FromRgb(66, 58, 112)), 1.0), chestRect, 4 * scale, 4 * scale);

        if (open)
        {
            var lidGeom = new PathGeometry();
            var lidFig = new PathFigure { StartPoint = new Point(chestRect.Left + (2 * scale), chestRect.Top + (3 * scale)) };
            lidFig.Segments.Add(new LineSegment(new Point(chestRect.Left + (6 * scale), chestRect.Top - (4 * scale)), true));
            lidFig.Segments.Add(new LineSegment(new Point(chestRect.Right - (1 * scale), chestRect.Top), true));
            lidFig.Segments.Add(new LineSegment(new Point(chestRect.Right - (1 * scale), chestRect.Top + (4 * scale)), true));
            lidFig.Segments.Add(new LineSegment(new Point(chestRect.Left + (2 * scale), chestRect.Top + (6 * scale)), true));
            lidFig.IsClosed = true;
            lidGeom.Figures.Add(lidFig);
            dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(220, 176, 162, 255)), new Pen(new SolidColorBrush(Color.FromRgb(88, 78, 132)), 0.8), lidGeom);

            DrawStar5(dc, chestRect.Left + (9 * scale), chestRect.Top + (8 * scale), 3.8 * scale, 1.8 * scale, new SolidColorBrush(Color.FromRgb(255, 225, 118)));
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 164, 196)), null, new Point(chestRect.Right - (7 * scale), chestRect.Top + (7 * scale)), 3.8 * scale, 2.8 * scale);
        }
        else
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), null, new Rect(chestRect.Left + (3 * scale), chestRect.Top + (2 * scale), chestRect.Width - (6 * scale), 4 * scale), 2, 2);
        }

        DrawStar5(dc, chestRect.Left + (chestRect.Width * 0.5), chestRect.Top + (chestRect.Height * 0.58), 4.2 * scale, 1.9 * scale, new SolidColorBrush(Color.FromArgb(190, 255, 240, 170)));
    }

    private void DrawTamagotchiFoodBowl(DrawingContext dc, double x, double floorY, double scale, bool sleepy)
    {
        Rect bowlRect = new Rect(x, floorY - (7 * scale), 18 * scale, 8 * scale);
        var bowlBrush = new LinearGradientBrush(Color.FromRgb(255, 184, 198), Color.FromRgb(226, 120, 156), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(bowlBrush, new Pen(new SolidColorBrush(Color.FromRgb(162, 74, 116)), 0.8), bowlRect, 4 * scale, 4 * scale);
        dc.DrawEllipse(new SolidColorBrush(sleepy ? Color.FromRgb(168, 196, 255) : Color.FromRgb(255, 236, 142)), null, new Point(bowlRect.Left + (bowlRect.Width * 0.5), bowlRect.Top + (2.2 * scale)), 6.0 * scale, 1.8 * scale);
        if (!sleepy)
        {
            DrawSparkle(dc, bowlRect.Left + (bowlRect.Width * 0.5), bowlRect.Top - (2.0 * scale), 2.0 * scale, new SolidColorBrush(Color.FromArgb(160, 255, 226, 120)));
        }
    }

    private void DrawTamagotchiAmbientPixels(DrawingContext dc, Rect rect, int count, Color color, bool upwardDrift)
    {
        for (int i = 0; i < count; i++)
        {
            double x = rect.Left + 6 + (((i * 27.0) + (_frameTick * (upwardDrift ? 0.7 : 0.3))) % Math.Max(18.0, rect.Width - 12));
            double travel = ((_frameTick * (upwardDrift ? 1.15 : 0.48)) + (i * 13.0)) % Math.Max(14.0, rect.Height - 12);
            double y = upwardDrift ? (rect.Bottom - 6 - travel) : (rect.Top + 6 + travel);
            double size = (i % 3 == 0) ? 3.0 : 2.0;
            var pixelBrush = new SolidColorBrush(Color.FromArgb((byte)(70 + ((i % 4) * 30)), color.R, color.G, color.B));
            dc.DrawRectangle(pixelBrush, null, new Rect(x, y, size, size));
            if ((i % 4) == 0)
            {
                DrawSparkle(dc, x + size, y + size, size * 0.85, new SolidColorBrush(Color.FromArgb((byte)(90 + ((i % 3) * 30)), color.R, color.G, color.B)));
            }
        }
    }

    private void DrawTamagotchiFocusMeter(DrawingContext dc, Rect meterRect)
    {
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(176, 20, 22, 38)), new Pen(new SolidColorBrush(Color.FromArgb(160, 146, 224, 255)), 0.9), meterRect, 4, 4);
        var fillRect = new Rect(meterRect.Left + 2, meterRect.Top + 2, Math.Max(4, (meterRect.Width - 4) * ProgressFraction), meterRect.Height - 4);
        var fillBrush = new LinearGradientBrush(Color.FromRgb(122, 255, 198), Color.FromRgb(255, 228, 108), new Point(0, 0), new Point(1, 0));
        dc.DrawRoundedRectangle(fillBrush, null, fillRect, 3, 3);
        for (int i = 1; i < 4; i++)
        {
            double x = meterRect.Left + (i * (meterRect.Width / 4.0));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), 0.7), new Point(x, meterRect.Top + 1), new Point(x, meterRect.Bottom - 1));
        }

        var label = CreateText("FOCUS", 7.5, new SolidColorBrush(Color.FromRgb(222, 244, 255)), FontWeights.Bold);
        dc.DrawText(label, new Point(meterRect.Left + ((meterRect.Width - label.Width) * 0.5), meterRect.Top - 10));
    }

    private void DrawTamagotchiOrbitingTreats(DrawingContext dc, Point center, double scale, int count, bool includeCandy)
    {
        for (int i = 0; i < count; i++)
        {
            double angle = (_frameTick * 0.10) + (i * (Math.PI * 2.0 / count));
            double orbitX = center.X + (Math.Cos(angle) * (28 * scale));
            double orbitY = center.Y - (6 * scale) + (Math.Sin(angle * 1.4) * (12 * scale));
            DrawStar5(dc, orbitX, orbitY, 4.8 * scale, 2.2 * scale, new SolidColorBrush(Color.FromRgb(255, 224, 94)));
            if (includeCandy)
            {
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 168, 206)), null, new Point(orbitX + (4 * scale), orbitY + (4 * scale)), 2.6 * scale, 2.6 * scale);
            }
        }
    }

    private void DrawTamagotchiSleepBubbles(DrawingContext dc, Point center, double scale)
    {
        for (int i = 0; i < 3; i++)
        {
            double x = center.X + (10 * scale) + (i * 10 * scale);
            double y = center.Y - (24 * scale) - (i * 10 * scale) + (Math.Sin((_frameTick * 0.12) + i) * (2 * scale));
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)(120 - (i * 18)), 194, 220, 255)), null, new Point(x, y), (4.0 - (i * 0.6)) * scale, (3.0 - (i * 0.4)) * scale);
            var zText = CreateText("Z", 10 + (i * 2), new SolidColorBrush(Color.FromArgb((byte)(200 - (i * 25)), 220, 236, 255)), FontWeights.Bold);
            dc.DrawText(zText, new Point(x + (2 * scale), y - (8 * scale)));
        }
    }

    private void DrawTamagotchiCelebrationRays(DrawingContext dc, Rect rect, Point center)
    {
        for (int i = 0; i < 10; i++)
        {
            double angle = (-55 + (i * 12)) * (Math.PI / 180.0);
            double len = Math.Max(rect.Width, rect.Height) * 0.65;
            double ex = center.X + (Math.Cos(angle) * len);
            double ey = center.Y + (Math.Sin(angle) * len);
            var rayPen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(60 + ((i % 3) * 20)), 255, 236, 138)), (i % 2 == 0) ? 5.0 : 3.5)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            dc.DrawLine(rayPen, center, new Point(ex, ey));
        }
    }

    private void DrawTamagotchiConfetti(DrawingContext dc, Rect rect, int count, double scale)
    {
        Color[] confettiColors =
        {
            Color.FromRgb(255, 224, 94),
            Color.FromRgb(255, 144, 196),
            Color.FromRgb(122, 238, 255),
            Color.FromRgb(180, 255, 162)
        };

        for (int i = 0; i < count; i++)
        {
            double x = rect.Left + 8 + (((i * 22.0) + (_frameTick * 1.8)) % Math.Max(20.0, rect.Width - 16));
            double y = rect.Top + 8 + (((i * 15.0) + (_frameTick * 1.1)) % Math.Max(16.0, rect.Height - 16));
            Color c = confettiColors[i % confettiColors.Length];

            dc.PushTransform(new RotateTransform((i * 31) + (_frameTick * 4.0), x, y));
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(220, c.R, c.G, c.B)), null, new Rect(x - (1.5 * scale), y - (4.0 * scale), 3.0 * scale, 8.0 * scale));
            dc.Pop();

            if ((i % 5) == 0)
            {
                DrawSparkle(dc, x + (2 * scale), y - (3 * scale), 2.4 * scale, new SolidColorBrush(Color.FromArgb(160, 255, 240, 180)));
            }
        }
    }

    private void DrawTamagotchiTrophyStand(DrawingContext dc, double x, double floorY, double scale)
    {
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(102, 72, 68)), null, new Rect(x - (10 * scale), floorY - (8 * scale), 20 * scale, 6 * scale), 2, 2);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(140, 96, 88)), null, new Rect(x - (3 * scale), floorY - (18 * scale), 6 * scale, 10 * scale), 2, 2);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 224, 94)), null, new Point(x, floorY - (24 * scale)), 8 * scale, 5.5 * scale);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(255, 224, 94)), null, new Rect(x - (6 * scale), floorY - (24 * scale), 12 * scale, 5 * scale));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 224, 94)), null, new Point(x - (7 * scale), floorY - (22 * scale)), 2.4 * scale, 4.0 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 224, 94)), null, new Point(x + (7 * scale), floorY - (22 * scale)), 2.4 * scale, 4.0 * scale);
        DrawStar5(dc, x, floorY - (24 * scale), 3.2 * scale, 1.4 * scale, new SolidColorBrush(Color.FromRgb(255, 246, 198)));
    }

    private void DrawTamagotchiHud(DrawingContext dc, Rect screenRect)
    {
        int heartCount = PetHappiness >= 67 ? 3 : (PetHappiness >= 34 ? 2 : (PetHappiness > 0 ? 1 : 0));
        Rect leftHud = new Rect(screenRect.Left + (IsMiniMode ? 6 : 8), screenRect.Top + (IsMiniMode ? 5 : 7), IsMiniMode ? 44 : 62, IsMiniMode ? 14 : 18);
        Rect rightHud = new Rect(screenRect.Right - (IsMiniMode ? 48 : 82), screenRect.Top + (IsMiniMode ? 5 : 7), IsMiniMode ? 42 : 74, IsMiniMode ? 14 : 18);

        var hudBg = new LinearGradientBrush(Color.FromArgb(196, 22, 24, 42), Color.FromArgb(196, 12, 14, 24), new Point(0, 0), new Point(0, 1));
        var hudPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 128, 226, 255)), IsMiniMode ? 0.9 : 1.1);
        dc.DrawRoundedRectangle(hudBg, hudPen, leftHud, 4, 4);
        dc.DrawRoundedRectangle(hudBg, hudPen, rightHud, 4, 4);

        for (int i = 0; i < 3; i++)
        {
            var heartBrush = new SolidColorBrush(i < heartCount ? Color.FromRgb(255, 102, 158) : Color.FromArgb(90, 110, 120, 146));
            DrawHeart(dc, leftHud.Left + 9 + (i * (IsMiniMode ? 10 : 14)), leftHud.Top + (IsMiniMode ? 8 : 10), IsMiniMode ? 3.0 : 4.0, heartBrush);
            if (i < heartCount)
            {
                DrawSparkle(dc, leftHud.Left + 9 + (i * (IsMiniMode ? 10 : 14)), leftHud.Top + (IsMiniMode ? 3.0 : 4.0), IsMiniMode ? 1.2 : 1.6, new SolidColorBrush(Color.FromArgb(120, 255, 232, 240)));
            }
        }

        if (!IsMiniMode)
        {
            var hpText = CreateText($"HP {PetHappiness}%", 8.5, new SolidColorBrush(Color.FromRgb(240, 248, 255)), FontWeights.Bold);
            dc.DrawText(hpText, new Point(leftHud.Right - hpText.Width - 6, leftHud.Top + 4));
        }

        int level = Math.Max(1, (FocusXp / 100) + 1);
        string xpLabel = IsMiniMode ? $"LV {level}" : $"LV {level} • XP {FocusXp}";
        var xpText = CreateText(xpLabel, IsMiniMode ? 8.0 : 9.0, new SolidColorBrush(Color.FromRgb(255, 236, 140)), FontWeights.Bold);
        dc.DrawText(xpText, new Point(rightHud.Left + ((rightHud.Width - xpText.Width) * 0.5), rightHud.Top + ((rightHud.Height - xpText.Height) * 0.5)));
    }

    private void DrawTamagotchiScreenGlass(DrawingContext dc, Rect screenRect)
    {
        dc.DrawRoundedRectangle(
            new LinearGradientBrush(Color.FromArgb(46, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), new Point(0, 0), new Point(1, 1)),
            null,
            new Rect(screenRect.Left + 3, screenRect.Top + 3, screenRect.Width - 6, screenRect.Height * 0.42),
            7,
            7);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 1.1), new Point(screenRect.Left + 10, screenRect.Top + 12), new Point(screenRect.Right - 18, screenRect.Bottom - 18));
    }

    private void DrawUltraKawaiiPet(DrawingContext dc, Point center, double scale, TamagotchiPetState state, bool isMini)
    {
        double idleSpeed = state == TamagotchiPetState.Goal ? 0.42 : (state == TamagotchiPetState.Active ? 0.26 : 0.12);
        double bounceAmp = state == TamagotchiPetState.Goal ? 4.8 : (state == TamagotchiPetState.Active ? 2.6 : 1.1);
        double bounce = Math.Sin(_frameTick * idleSpeed) * bounceAmp * scale;
        double sway = Math.Sin((_frameTick * 0.16) + center.X * 0.03) * 0.9 * scale;
        Point p = new Point(center.X + sway, center.Y + bounce);

        double breathX = 1.0 + (Math.Sin(_frameTick * 0.14) * 0.028);
        double breathY = 1.0 + (Math.Sin((_frameTick * 0.14) + 0.8) * (state == TamagotchiPetState.Rest ? 0.050 : 0.040));

        dc.DrawEllipse(new RadialGradientBrush(Color.FromArgb(state == TamagotchiPetState.Rest ? (byte)90 : (byte)120, 14, 22, 42), Color.FromArgb(0, 14, 22, 42)), null, new Point(p.X, p.Y + (17 * scale)), 18 * scale, 5.4 * scale);

        dc.PushTransform(new ScaleTransform(breathX, breathY, p.X, p.Y));

        var outlinePen = new Pen(new SolidColorBrush(Color.FromRgb(34, 146, 104)), 1.5 * scale);
        var bodyBrush = new RadialGradientBrush();
        bodyBrush.GradientStops.Add(new GradientStop(Color.FromRgb(222, 255, 236), 0.0));
        bodyBrush.GradientStops.Add(new GradientStop(Color.FromRgb(138, 252, 198), 0.45));
        bodyBrush.GradientStops.Add(new GradientStop(Color.FromRgb(64, 208, 152), 1.0));
        bodyBrush.Center = new Point(0.35, 0.25);
        bodyBrush.GradientOrigin = new Point(0.35, 0.25);
        bodyBrush.RadiusX = 0.85;
        bodyBrush.RadiusY = 0.85;

        double pawWiggle = Math.Sin((_frameTick * 0.22) + center.Y * 0.02) * 1.3 * scale;
        var limbBrush = new LinearGradientBrush(Color.FromRgb(116, 232, 176), Color.FromRgb(70, 190, 134), new Point(0, 0), new Point(0, 1));
        dc.DrawEllipse(limbBrush, null, new Point(p.X - (14 * scale), p.Y + (7 * scale) + (pawWiggle * 0.2)), 5.0 * scale, 4.3 * scale);
        dc.DrawEllipse(limbBrush, null, new Point(p.X + (14 * scale), p.Y + (7 * scale) - (pawWiggle * 0.2)), 5.0 * scale, 4.3 * scale);

        dc.DrawEllipse(bodyBrush, outlinePen, p, 22 * scale, 19 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(110, 255, 255, 255)), null, new Point(p.X - (6 * scale), p.Y - (8 * scale)), 10 * scale, 6 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(54, 24, 108, 82)), null, new Point(p.X + (7 * scale), p.Y + (6 * scale)), 10 * scale, 8 * scale);

        dc.DrawEllipse(limbBrush, null, new Point(p.X - (8 * scale), p.Y + (14 * scale) + pawWiggle), 5.2 * scale, 4.5 * scale);
        dc.DrawEllipse(limbBrush, null, new Point(p.X + (8 * scale), p.Y + (14 * scale) - pawWiggle), 5.2 * scale, 4.5 * scale);

        double antennaTipX = p.X + (Math.Sin(_frameTick * 0.18) * 5.5 * scale);
        double antennaTipY = p.Y - (28 * scale) + (Math.Cos(_frameTick * 0.16) * 1.8 * scale);
        var stemPen = new Pen(new SolidColorBrush(Color.FromRgb(56, 176, 112)), 2.0 * scale)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        var antennaGeom = new PathGeometry();
        var antennaFig = new PathFigure { StartPoint = new Point(p.X - (1.2 * scale), p.Y - (16 * scale)) };
        antennaFig.Segments.Add(new BezierSegment(new Point(p.X - (4 * scale), p.Y - (22 * scale)), new Point(antennaTipX - (2 * scale), p.Y - (24 * scale)), new Point(antennaTipX, antennaTipY), true));
        antennaGeom.Figures.Add(antennaFig);
        dc.DrawGeometry(null, stemPen, antennaGeom);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 228, 96)), new Pen(new SolidColorBrush(Color.FromRgb(218, 176, 58)), 0.8 * scale), new Point(antennaTipX, antennaTipY), 3.8 * scale, 3.8 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), null, new Point(antennaTipX - (1.2 * scale), antennaTipY - (1.1 * scale)), 1.1 * scale, 1.1 * scale);

        bool blink = state != TamagotchiPetState.Rest && ((_frameTick % 96) >= 72 && (_frameTick % 96) <= 78);
        Point leftEye = new Point(p.X - (7 * scale), p.Y - (2.5 * scale));
        Point rightEye = new Point(p.X + (7 * scale), p.Y - (2.5 * scale));
        var cheekBrush = new SolidColorBrush(Color.FromArgb(180, 255, 146, 176));
        dc.DrawEllipse(cheekBrush, null, new Point(p.X - (12 * scale), p.Y + (3 * scale)), 3.2 * scale, 2.0 * scale);
        dc.DrawEllipse(cheekBrush, null, new Point(p.X + (12 * scale), p.Y + (3 * scale)), 3.2 * scale, 2.0 * scale);

        if (state == TamagotchiPetState.Rest)
        {
            var eyePen = new Pen(new SolidColorBrush(Color.FromRgb(44, 62, 82)), 1.4 * scale)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            var leftSleep = new PathGeometry();
            var leftSleepFig = new PathFigure { StartPoint = new Point(leftEye.X - (3 * scale), leftEye.Y + (1.2 * scale)) };
            leftSleepFig.Segments.Add(new QuadraticBezierSegment(new Point(leftEye.X, leftEye.Y - (1.4 * scale)), new Point(leftEye.X + (3 * scale), leftEye.Y + (1.2 * scale)), true));
            leftSleep.Figures.Add(leftSleepFig);
            dc.DrawGeometry(null, eyePen, leftSleep);

            var rightSleep = new PathGeometry();
            var rightSleepFig = new PathFigure { StartPoint = new Point(rightEye.X - (3 * scale), rightEye.Y + (1.2 * scale)) };
            rightSleepFig.Segments.Add(new QuadraticBezierSegment(new Point(rightEye.X, rightEye.Y - (1.4 * scale)), new Point(rightEye.X + (3 * scale), rightEye.Y + (1.2 * scale)), true));
            rightSleep.Figures.Add(rightSleepFig);
            dc.DrawGeometry(null, eyePen, rightSleep);
        }
        else if (blink)
        {
            var blinkPen = new Pen(new SolidColorBrush(Color.FromRgb(36, 48, 72)), 1.6 * scale)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            dc.DrawLine(blinkPen, new Point(leftEye.X - (3 * scale), leftEye.Y), new Point(leftEye.X + (3 * scale), leftEye.Y + (0.4 * scale)));
            dc.DrawLine(blinkPen, new Point(rightEye.X - (3 * scale), rightEye.Y + (0.4 * scale)), new Point(rightEye.X + (3 * scale), rightEye.Y));
        }
        else
        {
            Color irisTop = state == TamagotchiPetState.Goal ? Color.FromRgb(255, 195, 92) : Color.FromRgb(100, 232, 255);
            Color irisBottom = state == TamagotchiPetState.Goal ? Color.FromRgb(255, 126, 114) : Color.FromRgb(84, 126, 255);
            var irisBrush = new LinearGradientBrush(irisTop, irisBottom, new Point(0, 0), new Point(0, 1));
            var pupilBrush = new SolidColorBrush(Color.FromRgb(26, 28, 44));

            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(34, 38, 58)), null, leftEye, 4.0 * scale, 5.2 * scale);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(34, 38, 58)), null, rightEye, 4.0 * scale, 5.2 * scale);
            dc.DrawEllipse(irisBrush, null, new Point(leftEye.X, leftEye.Y + (0.5 * scale)), 2.8 * scale, 3.8 * scale);
            dc.DrawEllipse(irisBrush, null, new Point(rightEye.X, rightEye.Y + (0.5 * scale)), 2.8 * scale, 3.8 * scale);
            dc.DrawEllipse(pupilBrush, null, new Point(leftEye.X, leftEye.Y + (1.0 * scale)), 1.4 * scale, 1.9 * scale);
            dc.DrawEllipse(pupilBrush, null, new Point(rightEye.X, rightEye.Y + (1.0 * scale)), 1.4 * scale, 1.9 * scale);
            dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(leftEye.X - (1.0 * scale), leftEye.Y - (1.4 * scale)), 1.0 * scale, 1.0 * scale);
            dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(rightEye.X - (1.0 * scale), rightEye.Y - (1.4 * scale)), 1.0 * scale, 1.0 * scale);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), null, new Point(leftEye.X + (0.9 * scale), leftEye.Y + (0.6 * scale)), 0.8 * scale, 0.8 * scale);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), null, new Point(rightEye.X + (0.9 * scale), rightEye.Y + (0.6 * scale)), 0.8 * scale, 0.8 * scale);
        }

        var mouthPen = new Pen(new SolidColorBrush(Color.FromRgb(224, 92, 132)), 1.3 * scale)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        if (state == TamagotchiPetState.Rest)
        {
            var mouth = new PathGeometry();
            var mouthFig = new PathFigure { StartPoint = new Point(p.X - (2.5 * scale), p.Y + (6 * scale)) };
            mouthFig.Segments.Add(new QuadraticBezierSegment(new Point(p.X, p.Y + (8.2 * scale)), new Point(p.X + (2.5 * scale), p.Y + (6 * scale)), true));
            mouth.Figures.Add(mouthFig);
            dc.DrawGeometry(null, mouthPen, mouth);
        }
        else if (state == TamagotchiPetState.Goal)
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(244, 108, 136)), new Pen(new SolidColorBrush(Color.FromRgb(206, 72, 108)), 0.8 * scale), new Point(p.X, p.Y + (6 * scale)), 3.6 * scale, 2.6 * scale);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 255, 182, 210)), null, new Point(p.X, p.Y + (7.2 * scale)), 2.2 * scale, 1.2 * scale);
        }
        else
        {
            var smile = new PathGeometry();
            var smileFig = new PathFigure { StartPoint = new Point(p.X - (4 * scale), p.Y + (5 * scale)) };
            smileFig.Segments.Add(new QuadraticBezierSegment(new Point(p.X, p.Y + (9 * scale)), new Point(p.X + (4 * scale), p.Y + (5 * scale)), true));
            smile.Figures.Add(smileFig);
            dc.DrawGeometry(null, mouthPen, smile);
        }

        if (state == TamagotchiPetState.Rest)
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(210, 176, 196, 255)), new Pen(new SolidColorBrush(Color.FromRgb(124, 136, 214)), 0.9 * scale), new Rect(p.X - (16 * scale), p.Y + (1 * scale), 32 * scale, 14 * scale), 6 * scale, 6 * scale);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1.0 * scale), new Point(p.X - (12 * scale), p.Y + (6 * scale)), new Point(p.X + (10 * scale), p.Y + (6 * scale)));

            var capGeom = new PathGeometry();
            var capFig = new PathFigure { StartPoint = new Point(p.X - (10 * scale), p.Y - (13 * scale)) };
            capFig.Segments.Add(new LineSegment(new Point(p.X - (4 * scale), p.Y - (24 * scale)), true));
            capFig.Segments.Add(new LineSegment(new Point(p.X + (5 * scale), p.Y - (14 * scale)), true));
            capFig.IsClosed = true;
            capGeom.Figures.Add(capFig);
            dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(150, 128, 228)), new Pen(new SolidColorBrush(Color.FromRgb(98, 86, 168)), 0.9 * scale), capGeom);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 244, 214)), null, new Point(p.X + (5.5 * scale), p.Y - (14 * scale)), 2.0 * scale, 2.0 * scale);
        }
        else if (state == TamagotchiPetState.Goal)
        {
            var crownBrush = new LinearGradientBrush(Color.FromRgb(255, 234, 110), Color.FromRgb(238, 178, 56), new Point(0, 0), new Point(0, 1));
            var crownGeom = new PathGeometry();
            var crownFig = new PathFigure { StartPoint = new Point(p.X - (10 * scale), p.Y - (18 * scale)) };
            crownFig.Segments.Add(new LineSegment(new Point(p.X - (6 * scale), p.Y - (26 * scale)), true));
            crownFig.Segments.Add(new LineSegment(new Point(p.X - (1 * scale), p.Y - (18 * scale)), true));
            crownFig.Segments.Add(new LineSegment(new Point(p.X + (3 * scale), p.Y - (27 * scale)), true));
            crownFig.Segments.Add(new LineSegment(new Point(p.X + (7 * scale), p.Y - (18 * scale)), true));
            crownFig.Segments.Add(new LineSegment(new Point(p.X + (10 * scale), p.Y - (24 * scale)), true));
            crownFig.Segments.Add(new LineSegment(new Point(p.X + (10 * scale), p.Y - (14 * scale)), true));
            crownFig.Segments.Add(new LineSegment(new Point(p.X - (10 * scale), p.Y - (14 * scale)), true));
            crownFig.IsClosed = true;
            crownGeom.Figures.Add(crownFig);
            dc.DrawGeometry(crownBrush, new Pen(new SolidColorBrush(Color.FromRgb(184, 132, 28)), 0.9 * scale), crownGeom);
            DrawSparkle(dc, p.X, p.Y - (24 * scale), 3.0 * scale, new SolidColorBrush(Color.FromArgb(180, 255, 246, 180)));
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 106, 148)), null, new Point(p.X, p.Y + (12 * scale)), 4.0 * scale, 3.0 * scale);
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(255, 216, 102)), null, new Rect(p.X - (0.8 * scale), p.Y + (8.5 * scale), 1.6 * scale, 6.0 * scale));
        }
        else
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(185, 255, 242, 166)), null, new Rect(p.X - (6 * scale), p.Y + (10 * scale), 12 * scale, 3.5 * scale), 2, 2);
            DrawStar5(dc, p.X, p.Y + (11.8 * scale), 2.5 * scale, 1.1 * scale, new SolidColorBrush(Color.FromRgb(255, 210, 78)));
        }

        dc.Pop();
    }

    #endregion
}
