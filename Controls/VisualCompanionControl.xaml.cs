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
        IsGoalReached = isGoalReached;
        IsRestPhase = isRestPhase;
        ContextDetails = contextDetails;
        FocusXp = focusXp;
        PetHappiness = petHappiness;

        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        TriggerCheer();
        Clicked?.Invoke();
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
            case "rocket":
                RenderRocketScene(dc, w, h);
                break;
            case "cat":
                RenderCatScene(dc, w, h);
                break;
            case "cafe":
                RenderCafeScene(dc, w, h);
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

        // Cheer overlay sparkles
        if (_cheerAnimationTicks > 0)
        {
            RenderCheerSparkles(dc, w, h);
        }

        dc.Pop(); // Pop clip
    }

    #region Helper Drawing Methods

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

    private void RenderCheerSparkles(DrawingContext dc, double w, double h)
    {
        var rand = new Random(42 + (_frameTick / 3));
        for (int i = 0; i < 10; i++)
        {
            double sx = rand.NextDouble() * w;
            double sy = rand.NextDouble() * h;
            double size = 3 + rand.Next(4);
            var color = Color.FromRgb((byte)rand.Next(200, 255), (byte)rand.Next(180, 255), (byte)rand.Next(50, 255));
            var brush = new SolidColorBrush(color);
            dc.DrawEllipse(brush, null, new Point(sx, sy), size, size);
        }
    }

    #endregion

    #region 🚴 Scene 1: Boy Cycling Home

    private void RenderCyclingScene(DrawingContext dc, double w, double h)
    {
        double groundY = h * 0.72;
        double groundHeight = h - groundY;

        // 1. Sky Gradient
        var skyBrush = new LinearGradientBrush(
            Color.FromRgb(32, 68, 120),
            Color.FromRgb(110, 180, 235),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(skyBrush, null, new Rect(0, 0, w, groundY));

        // 2. Sun / Moon
        double sunX = w * 0.85;
        double sunY = h * 0.22;
        var sunGlow = new RadialGradientBrush(Color.FromArgb(220, 255, 235, 120), Color.FromArgb(0, 255, 200, 0));
        dc.DrawEllipse(sunGlow, null, new Point(sunX, sunY), h * 0.35, h * 0.35);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 230, 90)), null, new Point(sunX, sunY), h * 0.12, h * 0.12);

        // 3. Drifting Clouds
        double cloudOffset1 = ((_frameTick * 0.5) % (w + 100)) - 50;
        double cloudOffset2 = (((_frameTick * 0.3) + 200) % (w + 100)) - 50;
        DrawCloud(dc, cloudOffset1, h * 0.18, h * 0.1);
        DrawCloud(dc, cloudOffset2, h * 0.28, h * 0.08);

        // 4. Distant Mountains / Rolling Hills
        var hillPath = new PathGeometry();
        var hillFig = new PathFigure { StartPoint = new Point(0, groundY) };
        hillFig.Segments.Add(new QuadraticBezierSegment(new Point(w * 0.25, groundY - (h * 0.3)), new Point(w * 0.55, groundY), true));
        hillFig.Segments.Add(new QuadraticBezierSegment(new Point(w * 0.75, groundY - (h * 0.22)), new Point(w, groundY), true));
        hillFig.Segments.Add(new LineSegment(new Point(w, groundY), true));
        hillFig.Segments.Add(new LineSegment(new Point(0, groundY), true));
        hillPath.Figures.Add(hillFig);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(58, 120, 75)), null, hillPath);

        // 5. Road / Ground
        var roadBrush = new LinearGradientBrush(
            Color.FromRgb(70, 75, 85),
            Color.FromRgb(45, 50, 60),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(roadBrush, null, new Rect(0, groundY, w, groundHeight));

        // Grass verge
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(95, 175, 75)), null, new Rect(0, groundY, w, 4));

        // Road dashed line
        double dashOffset = IsTracking ? (_frameTick * 4) % 24 : 0;
        var dashPen = new Pen(new SolidColorBrush(Color.FromRgb(240, 210, 60)), 2)
        {
            DashStyle = new DashStyle(new double[] { 4, 4 }, dashOffset / 3.0)
        };
        dc.DrawLine(dashPen, new Point(0, groundY + (groundHeight * 0.5)), new Point(w, groundY + (groundHeight * 0.5)));

        // 6. Destination: Cozy House on Right
        double houseW = IsMiniMode ? 45 : 75;
        double houseH = IsMiniMode ? 36 : 60;
        double houseX = w - houseW - (IsMiniMode ? 6 : 14);
        double houseY = groundY - houseH + 4;
        DrawCozyHouse(dc, houseX, houseY, houseW, houseH);

        // 7. Scenery Tree beside House
        if (!IsMiniMode)
        {
            DrawTree(dc, houseX - 25, groundY, 40);
        }

        // 8. Boy & Bicycle or Rest State
        double startX = IsMiniMode ? 12 : 24;
        double endX = houseX - (IsMiniMode ? 18 : 36);
        double currentX = startX + (ProgressFraction * (endX - startX));

        if (IsRestPhase)
        {
            // Rest Bench & Relaxing Boy
            DrawRestBench(dc, w * 0.45, groundY, IsMiniMode);
        }
        else if (IsGoalReached || ProgressFraction >= 0.999)
        {
            // Boy arrived at house celebrating
            DrawCelebrationBoy(dc, endX + (IsMiniMode ? 6 : 12), groundY, IsMiniMode);
        }
        else
        {
            // Boy cycling
            double bounce = IsTracking ? Math.Sin(_frameTick * 0.8) * (IsMiniMode ? 1 : 2) : 0;
            DrawCyclingBoy(dc, currentX, groundY + bounce, IsMiniMode, IsTracking);
        }
    }

    private void DrawCloud(DrawingContext dc, double x, double y, double size)
    {
        var cloudBrush = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255));
        dc.DrawEllipse(cloudBrush, null, new Point(x, y), size * 1.3, size * 0.7);
        dc.DrawEllipse(cloudBrush, null, new Point(x + (size * 0.6), y - (size * 0.25)), size * 0.9, size * 0.7);
        dc.DrawEllipse(cloudBrush, null, new Point(x - (size * 0.5), y), size * 0.8, size * 0.6);
    }

    private void DrawTree(DrawingContext dc, double x, double groundY, double height)
    {
        // Trunk
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(110, 65, 35)), null, new Rect(x - 3, groundY - height, 6, height));
        // Foliage
        var leafBrush = new SolidColorBrush(Color.FromRgb(40, 140, 60));
        dc.DrawEllipse(leafBrush, null, new Point(x, groundY - height - 10), height * 0.5, height * 0.45);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(60, 165, 80)), null, new Point(x - 4, groundY - height - 14), height * 0.35, height * 0.3);
    }

    private void DrawCozyHouse(DrawingContext dc, double x, double y, double w, double h)
    {
        double wallH = h * 0.6;
        double wallY = y + (h - wallH);

        // Chimney smoke
        if (IsTracking || IsGoalReached)
        {
            double smokeY = y - ((_frameTick * 1.5) % 30);
            double smokeR = 3 + (((_frameTick * 1.5) % 30) * 0.25);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 230, 230, 230)), null, new Point(x + (w * 0.2), smokeY), smokeR, smokeR);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 240, 240, 240)), null, new Point(x + (w * 0.2) + 4, smokeY - 12), smokeR * 1.3, smokeR * 1.3);
        }

        // Chimney
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(160, 50, 45)), null, new Rect(x + (w * 0.15), y + (h * 0.1), w * 0.15, h * 0.35));

        // House walls
        var wallBrush = new LinearGradientBrush(Color.FromRgb(245, 235, 210), Color.FromRgb(220, 205, 175), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(wallBrush, new Pen(new SolidColorBrush(Color.FromRgb(130, 110, 85)), 1), new Rect(x, wallY, w, wallH));

        // Roof triangle
        var roofPath = new PathGeometry();
        var roofFig = new PathFigure { StartPoint = new Point(x - (w * 0.08), wallY) };
        roofFig.Segments.Add(new LineSegment(new Point(x + (w * 0.5), y), true));
        roofFig.Segments.Add(new LineSegment(new Point(x + w + (w * 0.08), wallY), true));
        roofPath.Figures.Add(roofFig);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(190, 55, 45)), new Pen(new SolidColorBrush(Color.FromRgb(140, 35, 25)), 1), roofPath);

        // Door
        double doorW = w * 0.28;
        double doorH = wallH * 0.65;
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(140, 75, 40)), null, new Rect(x + (w * 0.55), wallY + (wallH - doorH), doorW, doorH));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 0)), null, new Point(x + (w * 0.55) + (doorW * 0.8), wallY + (wallH - (doorH * 0.5))), 1.5, 1.5);

        // Glowing Window
        double winW = w * 0.28;
        double winH = wallH * 0.45;
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(255, 240, 130)), new Pen(new SolidColorBrush(Color.FromRgb(120, 95, 65)), 1), new Rect(x + (w * 0.12), wallY + (wallH * 0.18), winW, winH));
    }

    private void DrawCyclingBoy(DrawingContext dc, double x, double groundY, bool isMini, bool isPedaling)
    {
        double scale = isMini ? 0.65 : 1.0;
        double wheelRadius = 11 * scale;
        double wheelDist = 32 * scale;
        double bottomBracketY = groundY - wheelRadius;

        Point rearHub = new Point(x, groundY - wheelRadius);
        Point frontHub = new Point(x + wheelDist, groundY - wheelRadius);
        Point crank = new Point(x + (wheelDist * 0.48), groundY - wheelRadius + (2 * scale));
        Point seatPost = new Point(x + (wheelDist * 0.35), groundY - (wheelRadius * 2.1));
        Point headTube = new Point(x + (wheelDist * 0.85), groundY - (wheelRadius * 2.3));
        Point handleBar = new Point(x + (wheelDist * 0.88), groundY - (wheelRadius * 2.8));

        // 1. Wheels (Rear & Front)
        DrawBicycleWheel(dc, rearHub, wheelRadius, isPedaling);
        DrawBicycleWheel(dc, frontHub, wheelRadius, isPedaling);

        // 2. Bike Frame
        var framePen = new Pen(new SolidColorBrush(Color.FromRgb(40, 150, 235)), 2.2 * scale);
        dc.DrawLine(framePen, rearHub, crank);
        dc.DrawLine(framePen, crank, seatPost);
        dc.DrawLine(framePen, seatPost, rearHub);
        dc.DrawLine(framePen, crank, headTube);
        dc.DrawLine(framePen, seatPost, headTube);
        dc.DrawLine(framePen, headTube, frontHub);
        dc.DrawLine(framePen, headTube, handleBar);

        // Handlebar grip
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(30, 30, 30)), 3 * scale), handleBar, new Point(handleBar.X - (4 * scale), handleBar.Y - (2 * scale)));

        // Saddle
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(25, 25, 25)), 3.5 * scale), new Point(seatPost.X - (7 * scale), seatPost.Y - (2 * scale)), new Point(seatPost.X + (5 * scale), seatPost.Y - (2 * scale)));

        // 3. Boy Cyclist
        double pedalAngle = isPedaling ? (_frameTick * 0.45) : 0;
        double crankLen = 5 * scale;
        Point pedalPoint = new Point(crank.X + (Math.Cos(pedalAngle) * crankLen), crank.Y + (Math.Sin(pedalAngle) * crankLen));

        Point hip = new Point(seatPost.X + (1 * scale), seatPost.Y - (6 * scale));
        Point shoulder = new Point(seatPost.X + (12 * scale), seatPost.Y - (22 * scale));
        Point head = new Point(shoulder.X + (3 * scale), shoulder.Y - (8 * scale));

        // Legs
        var legPen = new Pen(new SolidColorBrush(Color.FromRgb(35, 60, 120)), 2.5 * scale);
        Point knee = new Point(hip.X + (10 * scale) + (Math.Sin(pedalAngle) * 3 * scale), hip.Y + (8 * scale));
        dc.DrawLine(legPen, hip, knee);
        dc.DrawLine(legPen, knee, pedalPoint);

        // Torso / Shirt
        var shirtPen = new Pen(new SolidColorBrush(Color.FromRgb(235, 75, 60)), 5 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(shirtPen, hip, shoulder);

        // Arm reaching to handlebar
        var armPen = new Pen(new SolidColorBrush(Color.FromRgb(240, 185, 145)), 2.2 * scale);
        dc.DrawLine(armPen, shoulder, handleBar);

        // Head & Helmet
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(245, 195, 150)), null, head, 5.5 * scale, 5.5 * scale);
        // Helmet cap
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(250, 190, 30)), null, new Point(head.X, head.Y - (2 * scale)), 6 * scale, 4.5 * scale);
        // Visor
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(250, 190, 30)), 2 * scale), new Point(head.X + (2 * scale), head.Y - (1 * scale)), new Point(head.X + (8 * scale), head.Y - (1 * scale)));

        // Wind speed lines behind boy
        if (isPedaling && !isMini)
        {
            var windPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)), 1.2);
            double wy = hip.Y - 5;
            dc.DrawLine(windPen, new Point(x - 15, wy), new Point(x - 3, wy));
            dc.DrawLine(windPen, new Point(x - 22, wy + 8), new Point(x - 6, wy + 8));
        }
    }

    private void DrawBicycleWheel(DrawingContext dc, Point hub, double radius, bool isSpinning)
    {
        // Tire
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(30, 30, 35)), 2.2), hub, radius, radius);
        // Rim
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(190, 195, 205)), 1.0), hub, radius - 1.5, radius - 1.5);

        // Spokes
        double spokeOffset = isSpinning ? (_frameTick * 0.45) : 0;
        var spokePen = new Pen(new SolidColorBrush(Color.FromRgb(180, 180, 190)), 0.8);
        for (int i = 0; i < 4; i++)
        {
            double angle = spokeOffset + (i * Math.PI / 2.0);
            Point p1 = new Point(hub.X + (Math.Cos(angle) * (radius - 2)), hub.Y + (Math.Sin(angle) * (radius - 2)));
            Point p2 = new Point(hub.X - (Math.Cos(angle) * (radius - 2)), hub.Y - (Math.Sin(angle) * (radius - 2)));
            dc.DrawLine(spokePen, p1, p2);
        }

        // Hub center
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(40, 40, 40)), null, hub, 2, 2);
    }

    private void DrawCelebrationBoy(DrawingContext dc, double x, double groundY, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;
        Point boyPos = new Point(x, groundY - (28 * scale));

        // Boy celebrating with arms raised
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(245, 195, 150)), null, new Point(boyPos.X, boyPos.Y - (8 * scale)), 6 * scale, 6 * scale);
        // Cap
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(250, 190, 30)), null, new Point(boyPos.X, boyPos.Y - (10 * scale)), 6.5 * scale, 4 * scale);

        // Torso
        var shirtPen = new Pen(new SolidColorBrush(Color.FromRgb(235, 75, 60)), 6 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(shirtPen, new Point(boyPos.X, boyPos.Y - (2 * scale)), new Point(boyPos.X, boyPos.Y + (12 * scale)));

        // Raised arms \o/
        var armPen = new Pen(new SolidColorBrush(Color.FromRgb(240, 185, 145)), 2.5 * scale);
        dc.DrawLine(armPen, new Point(boyPos.X, boyPos.Y + (2 * scale)), new Point(boyPos.X - (9 * scale), boyPos.Y - (10 * scale)));
        dc.DrawLine(armPen, new Point(boyPos.X, boyPos.Y + (2 * scale)), new Point(boyPos.X + (9 * scale), boyPos.Y - (10 * scale)));

        // Legs
        var legPen = new Pen(new SolidColorBrush(Color.FromRgb(35, 60, 120)), 3 * scale);
        dc.DrawLine(legPen, new Point(boyPos.X - (2 * scale), boyPos.Y + (12 * scale)), new Point(boyPos.X - (4 * scale), groundY));
        dc.DrawLine(legPen, new Point(boyPos.X + (2 * scale), boyPos.Y + (12 * scale)), new Point(boyPos.X + (4 * scale), groundY));

        // Banner
        string text = isMini ? "★ HOME! ★" : "🎉 WELCOME HOME! 🎉";
        var ft = CreateText(text, isMini ? 10 : 13, new SolidColorBrush(Color.FromRgb(255, 230, 80)), FontWeights.Bold);
        dc.DrawText(ft, new Point(boyPos.X - (ft.Width / 2), boyPos.Y - (28 * scale)));
    }

    private void DrawRestBench(DrawingContext dc, double x, double groundY, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;

        // Wooden Bench
        var benchBrush = new SolidColorBrush(Color.FromRgb(140, 80, 45));
        dc.DrawRectangle(benchBrush, null, new Rect(x - (18 * scale), groundY - (12 * scale), 36 * scale, 4 * scale));
        dc.DrawRectangle(benchBrush, null, new Rect(x - (16 * scale), groundY - (22 * scale), 32 * scale, 3 * scale));
        // Bench legs
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(50, 50, 50)), 2 * scale), new Point(x - (14 * scale), groundY - (12 * scale)), new Point(x - (14 * scale), groundY));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(50, 50, 50)), 2 * scale), new Point(x + (14 * scale), groundY - (12 * scale)), new Point(x + (14 * scale), groundY));

        // Relaxed resting boy sitting
        Point head = new Point(x - (4 * scale), groundY - (28 * scale));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(245, 195, 150)), null, head, 5.5 * scale, 5.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(70, 160, 240)), null, new Point(head.X, head.Y - (2 * scale)), 6 * scale, 4 * scale);

        // Torso
        var shirtPen = new Pen(new SolidColorBrush(Color.FromRgb(235, 75, 60)), 5.5 * scale);
        dc.DrawLine(shirtPen, new Point(head.X, head.Y + (5 * scale)), new Point(head.X, groundY - (12 * scale)));

        // Coffee mug in hand
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(250, 250, 250)), null, new Rect(x + (6 * scale), groundY - (20 * scale), 6 * scale, 8 * scale));
        // Steam
        double steamY = groundY - (22 * scale) - ((_frameTick * 0.8) % 12);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)), null, new Point(x + (9 * scale), steamY), 2 * scale, 2 * scale);

        // Floating "z Z Z"
        string zText = (_frameTick % 20 < 10) ? "☕ z Z" : "☕ Z z";
        var ft = CreateText(zText, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(255, 215, 90)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x + (12 * scale), groundY - (34 * scale)));
    }

    #endregion

    #region 🚀 Scene 2: Space Rocket Odyssey

    private void RenderRocketScene(DrawingContext dc, double w, double h)
    {
        // 1. Deep Space Gradient
        var spaceBrush = new LinearGradientBrush(
            Color.FromRgb(8, 12, 28),
            Color.FromRgb(20, 25, 55),
            new Point(0, 0),
            new Point(1, 1));
        dc.DrawRectangle(spaceBrush, null, new Rect(0, 0, w, h));

        // 2. Stars
        var starRand = new Random(101);
        int starCount = IsMiniMode ? 25 : 55;
        for (int i = 0; i < starCount; i++)
        {
            double sx = starRand.NextDouble() * w;
            double sy = starRand.NextDouble() * h;
            double baseRadius = (i % 5 == 0) ? 2.0 : 1.0;
            double pulse = Math.Sin((_frameTick * 0.2) + i) * 0.5 + 0.5;
            byte alpha = (byte)(100 + (pulse * 155));
            var starColor = (i % 7 == 0) ? Color.FromArgb(alpha, 160, 220, 255) : Color.FromArgb(alpha, 255, 255, 255);
            dc.DrawEllipse(new SolidColorBrush(starColor), null, new Point(sx, sy), baseRadius, baseRadius);
        }

        // 3. Launch Planet Earth on Left (0%)
        double earthRadius = IsMiniMode ? 28 : 55;
        Point earthCenter = new Point(0, h * 0.5);
        var earthBrush = new RadialGradientBrush(Color.FromRgb(70, 160, 245), Color.FromRgb(20, 60, 150));
        dc.DrawEllipse(earthBrush, new Pen(new SolidColorBrush(Color.FromArgb(100, 140, 200, 255)), 2), earthCenter, earthRadius, earthRadius);
        // Earth green continent spot
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(60, 175, 95)), null, new Point(earthRadius * 0.45, (h * 0.5) - (earthRadius * 0.2)), earthRadius * 0.35, earthRadius * 0.25);

        // 4. Moon Destination on Right (100%)
        double moonRadius = IsMiniMode ? 26 : 50;
        Point moonCenter = new Point(w, h * 0.5);
        var moonBrush = new RadialGradientBrush(Color.FromRgb(235, 235, 245), Color.FromRgb(140, 145, 160));
        dc.DrawEllipse(moonBrush, new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 2), moonCenter, moonRadius, moonRadius);
        // Moon Craters
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(120, 125, 140)), null, new Point(w - (moonRadius * 0.5), (h * 0.5) - (moonRadius * 0.3)), moonRadius * 0.2, moonRadius * 0.2);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(125, 130, 145)), null, new Point(w - (moonRadius * 0.7), (h * 0.5) + (moonRadius * 0.25)), moonRadius * 0.25, moonRadius * 0.25);

        // Trajectory flight line
        var trajPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 120, 180, 255)), 1.5)
        {
            DashStyle = DashStyles.Dash
        };
        dc.DrawLine(trajPen, new Point(earthRadius + 5, h * 0.5), new Point(w - moonRadius - 5, h * 0.5));

        // 5. Rocket Position Calculation
        double startX = earthRadius + (IsMiniMode ? 10 : 20);
        double endX = w - moonRadius - (IsMiniMode ? 12 : 24);
        double rocketX = startX + (ProgressFraction * (endX - startX));
        double rocketY = (h * 0.5) + (Math.Sin(_frameTick * 0.15) * (IsMiniMode ? 2 : 4));

        if (IsRestPhase)
        {
            // Zero-G Orbital Lounge
            DrawZeroGLounge(dc, w * 0.5, h * 0.5, IsMiniMode);
        }
        else if (IsGoalReached || ProgressFraction >= 0.999)
        {
            // Rocket landed on Moon with victory flag
            DrawMoonLanding(dc, w - moonRadius - (IsMiniMode ? 8 : 16), h * 0.5, IsMiniMode);
        }
        else
        {
            // Rocket flying
            DrawSpaceRocket(dc, rocketX, rocketY, IsMiniMode, IsTracking);
        }
    }

    private void DrawSpaceRocket(DrawingContext dc, double x, double y, bool isMini, bool isThrusting)
    {
        double scale = isMini ? 0.65 : 1.0;
        double rocketLen = 38 * scale;
        double rocketH = 16 * scale;

        // Thrust Flame
        if (isThrusting)
        {
            double flameLen = (20 + (Math.Sin(_frameTick * 1.8) * 8)) * scale;
            var flameGeom = new PathGeometry();
            var flameFig = new PathFigure { StartPoint = new Point(x - (rocketLen * 0.5), y - (rocketH * 0.35)) };
            flameFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.5) - flameLen, y), true));
            flameFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.5), y + (rocketH * 0.35)), true));
            flameGeom.Figures.Add(flameFig);

            var flameBrush = new LinearGradientBrush(Color.FromRgb(255, 120, 20), Color.FromRgb(255, 230, 60), new Point(0, 0), new Point(1, 0));
            dc.DrawGeometry(flameBrush, null, flameGeom);

            // Core inner flame
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(100, 230, 255)), null, new Point(x - (rocketLen * 0.5) - (flameLen * 0.3), y), 3 * scale, 3 * scale);
        }

        // Rocket Body (Capsule)
        var bodyBrush = new LinearGradientBrush(Color.FromRgb(245, 248, 255), Color.FromRgb(190, 205, 225), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(bodyBrush, new Pen(new SolidColorBrush(Color.FromRgb(140, 160, 190)), 1), new Rect(x - (rocketLen * 0.45), y - (rocketH * 0.5), rocketLen * 0.7, rocketH), 4 * scale, 4 * scale);

        // Nose Cone (Red)
        var noseGeom = new PathGeometry();
        var noseFig = new PathFigure { StartPoint = new Point(x + (rocketLen * 0.25), y - (rocketH * 0.5)) };
        noseFig.Segments.Add(new LineSegment(new Point(x + (rocketLen * 0.55), y), true));
        noseFig.Segments.Add(new LineSegment(new Point(x + (rocketLen * 0.25), y + (rocketH * 0.5)), true));
        noseGeom.Figures.Add(noseFig);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(235, 55, 55)), null, noseGeom);

        // Fins (Top & Bottom)
        var finBrush = new SolidColorBrush(Color.FromRgb(235, 55, 55));
        var topFin = new PathGeometry();
        var tfFig = new PathFigure { StartPoint = new Point(x - (rocketLen * 0.45), y - (rocketH * 0.5)) };
        tfFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.6), y - (rocketH * 0.9)), true));
        tfFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.15), y - (rocketH * 0.5)), true));
        topFin.Figures.Add(tfFig);
        dc.DrawGeometry(finBrush, null, topFin);

        var btmFin = new PathGeometry();
        var bfFig = new PathFigure { StartPoint = new Point(x - (rocketLen * 0.45), y + (rocketH * 0.5)) };
        bfFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.6), y + (rocketH * 0.9)), true));
        bfFig.Segments.Add(new LineSegment(new Point(x - (rocketLen * 0.15), y + (rocketH * 0.5)), true));
        btmFin.Figures.Add(bfFig);
        dc.DrawGeometry(finBrush, null, btmFin);

        // Porthole Window
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(70, 180, 245)), new Pen(new SolidColorBrush(Color.FromRgb(30, 40, 60)), 1.5 * scale), new Point(x, y), 5 * scale, 5 * scale);
        // Glass shine
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), null, new Point(x - (1.5 * scale), y - (1.5 * scale)), 1.8 * scale, 1.8 * scale);
    }

    private void DrawMoonLanding(DrawingContext dc, double x, double y, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;

        // Landed Rocket
        DrawSpaceRocket(dc, x - (10 * scale), y, isMini, false);

        // Planted Flag
        Point flagPole = new Point(x + (15 * scale), y - (18 * scale));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(220, 220, 220)), 2 * scale), flagPole, new Point(flagPole.X, flagPole.Y + (24 * scale)));

        // Flag banner
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(240, 60, 50)), null, new Rect(flagPole.X, flagPole.Y, 14 * scale, 9 * scale));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 230, 80)), null, new Point(flagPole.X + (7 * scale), flagPole.Y + (4.5 * scale)), 2.5 * scale, 2.5 * scale);

        // Text
        string text = isMini ? "★ MOON BASE! ★" : "🚀 MISSION ACCOMPLISHED! 🌕";
        var ft = CreateText(text, isMini ? 10 : 13, new SolidColorBrush(Color.FromRgb(255, 230, 80)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.5), y - (32 * scale)));
    }

    private void DrawZeroGLounge(DrawingContext dc, double x, double y, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;
        double floatBob = Math.Sin(_frameTick * 0.15) * 4 * scale;

        // Astronaut Floating
        Point astroCenter = new Point(x, y + floatBob);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(240, 245, 255)), new Pen(new SolidColorBrush(Color.FromRgb(100, 140, 180)), 1.5), astroCenter, 14 * scale, 14 * scale);
        // Visor
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(250, 190, 40)), null, new Point(astroCenter.X + (2 * scale), astroCenter.Y), 7 * scale, 6 * scale);

        // Boba Tea Cup
        Point cup = new Point(astroCenter.X + (16 * scale), astroCenter.Y - (4 * scale));
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 255, 215, 175)), new Pen(new SolidColorBrush(Colors.White), 1), new Rect(cup.X, cup.Y, 8 * scale, 12 * scale));
        // Straw
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 100, 150)), 1.5 * scale), new Point(cup.X + (4 * scale), cup.Y), new Point(cup.X + (7 * scale), cup.Y - (6 * scale)));

        // Text
        string text = isMini ? "☕ Zero-G Rest" : "☕ Zero-G Relaxation Lounge • Hydrate!";
        var ft = CreateText(text, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(140, 220, 255)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.5), y - (24 * scale)));
    }

    #endregion

    #region 🐱 Scene 3: Playful Focus Kitty

    private void RenderCatScene(DrawingContext dc, double w, double h)
    {
        double floorY = h * 0.72;

        // 1. Room Wallpaper & Cozy Wooden Floor
        var wallBrush = new LinearGradientBrush(Color.FromRgb(255, 245, 235), Color.FromRgb(245, 225, 210), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(wallBrush, null, new Rect(0, 0, w, floorY));

        var floorBrush = new LinearGradientBrush(Color.FromRgb(215, 160, 110), Color.FromRgb(180, 125, 80), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(floorBrush, null, new Rect(0, floorY, w, h - floorY));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(140, 95, 55)), 2), new Point(0, floorY), new Point(w, floorY));

        // 2. Sunny Window on Wall
        if (!IsMiniMode)
        {
            double winX = w * 0.15;
            double winY = h * 0.12;
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(180, 225, 255)), new Pen(new SolidColorBrush(Color.FromRgb(160, 120, 85)), 2), new Rect(winX, winY, 40, 45));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(160, 120, 85)), 1.5), new Point(winX + 20, winY), new Point(winX + 20, winY + 45));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(160, 120, 85)), 1.5), new Point(winX, winY + 22), new Point(winX + 40, winY + 22));
        }

        // 3. Golden Fish Bowl on Right (100%)
        double bowlX = w - (IsMiniMode ? 35 : 65);
        double bowlY = floorY - (IsMiniMode ? 14 : 22);
        DrawFishBowl(dc, bowlX, bowlY, IsMiniMode);

        // 4. Cat Movement / Rest
        double startX = IsMiniMode ? 14 : 30;
        double endX = bowlX - (IsMiniMode ? 16 : 30);
        double catX = startX + (ProgressFraction * (endX - startX));

        if (IsRestPhase)
        {
            DrawSleepingKitty(dc, w * 0.5, floorY, IsMiniMode);
        }
        else if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawHappyFeastingKitty(dc, bowlX - (IsMiniMode ? 12 : 22), floorY, IsMiniMode);
        }
        else
        {
            DrawWalkingKitty(dc, catX, floorY, IsMiniMode, IsTracking);
        }
    }

    private void DrawFishBowl(DrawingContext dc, double x, double y, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;
        double bowlW = 26 * scale;
        double bowlH = 16 * scale;

        // Golden Bowl
        var bowlBrush = new LinearGradientBrush(Color.FromRgb(255, 215, 60), Color.FromRgb(220, 160, 20), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(bowlBrush, new Pen(new SolidColorBrush(Color.FromRgb(180, 120, 10)), 1), new Rect(x, y, bowlW, bowlH), 4 * scale, 4 * scale);

        // Fish sticking out
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(90, 180, 240)), null, new Point(x + (bowlW * 0.5), y + (2 * scale)), 8 * scale, 4 * scale);
        dc.DrawEllipse(new SolidColorBrush(Colors.Black), null, new Point(x + (bowlW * 0.7), y + (1.5 * scale)), 1 * scale, 1 * scale);
    }

    private void DrawWalkingKitty(DrawingContext dc, double x, double floorY, bool isMini, bool isWalking)
    {
        double scale = isMini ? 0.7 : 1.0;
        double bob = isWalking ? Math.Sin(_frameTick * 0.6) * 2 * scale : 0;
        double catY = floorY - (14 * scale) + bob;

        var catColor = new SolidColorBrush(Color.FromRgb(245, 150, 65)); // Ginger kitty

        // Tail swishing
        double tailAngle = Math.Sin(_frameTick * 0.4) * 0.3;
        Point tailStart = new Point(x - (10 * scale), catY + (2 * scale));
        Point tailEnd = new Point(tailStart.X - (8 * scale) + (Math.Sin(tailAngle) * 6), tailStart.Y - (12 * scale));
        dc.DrawLine(new Pen(catColor, 3.5 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, tailStart, tailEnd);

        // Legs (Walking animation)
        double legSwing = isWalking ? Math.Sin(_frameTick * 0.6) * 4 * scale : 0;
        var legPen = new Pen(catColor, 3 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(legPen, new Point(x - (6 * scale), catY + (6 * scale)), new Point(x - (6 * scale) + legSwing, floorY));
        dc.DrawLine(legPen, new Point(x - (2 * scale), catY + (6 * scale)), new Point(x - (2 * scale) - legSwing, floorY));
        dc.DrawLine(legPen, new Point(x + (4 * scale), catY + (6 * scale)), new Point(x + (4 * scale) - legSwing, floorY));
        dc.DrawLine(legPen, new Point(x + (8 * scale), catY + (6 * scale)), new Point(x + (8 * scale) + legSwing, floorY));

        // Body
        dc.DrawEllipse(catColor, null, new Point(x, catY), 12 * scale, 8 * scale);

        // Head
        Point head = new Point(x + (12 * scale), catY - (4 * scale));
        dc.DrawEllipse(catColor, null, head, 7 * scale, 7 * scale);

        // Ears
        var earBrush = new SolidColorBrush(Color.FromRgb(255, 185, 175));
        var ear1 = new PathGeometry();
        var e1Fig = new PathFigure { StartPoint = new Point(head.X - (4 * scale), head.Y - (5 * scale)) };
        e1Fig.Segments.Add(new LineSegment(new Point(head.X - (2 * scale), head.Y - (12 * scale)), true));
        e1Fig.Segments.Add(new LineSegment(new Point(head.X + (1 * scale), head.Y - (6 * scale)), true));
        ear1.Figures.Add(e1Fig);
        dc.DrawGeometry(catColor, null, ear1);
        dc.DrawGeometry(earBrush, null, ear1);

        var ear2 = new PathGeometry();
        var e2Fig = new PathFigure { StartPoint = new Point(head.X + (2 * scale), head.Y - (6 * scale)) };
        e2Fig.Segments.Add(new LineSegment(new Point(head.X + (5 * scale), head.Y - (12 * scale)), true));
        e2Fig.Segments.Add(new LineSegment(new Point(head.X + (7 * scale), head.Y - (4 * scale)), true));
        ear2.Figures.Add(e2Fig);
        dc.DrawGeometry(catColor, null, ear2);

        // Eyes & Nose
        dc.DrawEllipse(new SolidColorBrush(Colors.Black), null, new Point(head.X + (3 * scale), head.Y - (1 * scale)), 1.2 * scale, 1.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 130, 150)), null, new Point(head.X + (6 * scale), head.Y + (1 * scale)), 1 * scale, 0.8 * scale);
    }

    private void DrawHappyFeastingKitty(DrawingContext dc, double x, double floorY, bool isMini)
    {
        DrawWalkingKitty(dc, x, floorY, isMini, false);

        // Floating Hearts & Purrs
        double scale = isMini ? 0.7 : 1.0;
        string text = isMini ? "💖 YUM! 💖" : "💖 PURRR! DELICIOUS FISH! 💖";
        var ft = CreateText(text, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(245, 80, 140)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.4), floorY - (36 * scale)));
    }

    private void DrawSleepingKitty(DrawingContext dc, double x, double floorY, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;
        double breathe = Math.Sin(_frameTick * 0.2) * 1.5 * scale;

        // Cushion
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(180, 95, 160)), null, new Rect(x - (22 * scale), floorY - (8 * scale), 44 * scale, 8 * scale), 4 * scale, 4 * scale);

        // Curled Kitty
        Point catPos = new Point(x, floorY - (12 * scale) + breathe);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(245, 150, 65)), null, catPos, 14 * scale, (10 * scale) + breathe);
        // Head
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(245, 150, 65)), null, new Point(catPos.X + (8 * scale), catPos.Y), 6 * scale, 6 * scale);

        // "z Z Z"
        string text = (_frameTick % 20 < 10) ? "💤 z Z Z" : "💤 Z z z";
        var ft = CreateText(text, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(160, 100, 220)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x + (14 * scale), floorY - (28 * scale)));
    }

    #endregion

    #region ☕ Scene 4: Cozy Lo-Fi Cafe

    private void RenderCafeScene(DrawingContext dc, double w, double h)
    {
        double deskY = h * 0.75;

        // 1. Warm Cafe Wall Background
        var cafeWall = new LinearGradientBrush(Color.FromRgb(45, 30, 25), Color.FromRgb(75, 45, 35), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(cafeWall, null, new Rect(0, 0, w, deskY));

        // Desk
        var deskBrush = new LinearGradientBrush(Color.FromRgb(130, 75, 40), Color.FromRgb(95, 50, 25), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(deskBrush, null, new Rect(0, deskY, w, h - deskY));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(65, 35, 15)), 2), new Point(0, deskY), new Point(w, deskY));

        // 2. Vinyl Turntable on Left
        double recordX = IsMiniMode ? 24 : 50;
        double recordY = deskY - (IsMiniMode ? 10 : 16);
        DrawVinylPlayer(dc, recordX, recordY, IsMiniMode, IsTracking);

        // 3. Books Stacking with Progress
        double booksX = w * 0.45;
        DrawBookStack(dc, booksX, deskY, IsMiniMode);

        // 4. Steaming Coffee Mug (Fills with Progress)
        double mugX = w - (IsMiniMode ? 35 : 75);
        double mugY = deskY - (IsMiniMode ? 22 : 36);
        DrawCoffeeMug(dc, mugX, mugY, IsMiniMode);

        // Ambient Lamp Glow
        var lampGlow = new RadialGradientBrush(Color.FromArgb(80, 255, 230, 150), Color.FromArgb(0, 255, 200, 50));
        dc.DrawEllipse(lampGlow, null, new Point(w * 0.8, h * 0.2), h * 0.6, h * 0.6);
    }

    private void DrawVinylPlayer(DrawingContext dc, double x, double y, bool isMini, bool isSpinning)
    {
        double scale = isMini ? 0.7 : 1.0;
        double r = 16 * scale;

        // Player Base
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(190, 130, 80)), new Pen(new SolidColorBrush(Color.FromRgb(70, 40, 20)), 1), new Rect(x - r - 4, y - r - 2, (r * 2) + 8, (r * 2) + 4), 3, 3);

        // Vinyl Record
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(25, 25, 25)), null, new Point(x, y), r, r);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 60)), 0.8), new Point(x, y), r * 0.7, r * 0.7);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(235, 75, 60)), null, new Point(x, y), r * 0.35, r * 0.35);

        // Floating Music Notes
        if (isSpinning)
        {
            double noteY = y - r - ((_frameTick * 0.8) % 18);
            string note = (_frameTick % 16 < 8) ? "🎵" : "🎶";
            var ft = CreateText(note, 10 * scale, new SolidColorBrush(Color.FromRgb(255, 215, 90)));
            dc.DrawText(ft, new Point(x + (Math.Sin(_frameTick * 0.2) * 6), noteY));
        }
    }

    private void DrawBookStack(DrawingContext dc, double x, double deskY, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;
        int maxBooks = isMini ? 3 : 5;
        int currentBooks = Math.Max(1, (int)Math.Ceiling(ProgressFraction * maxBooks));

        var bookColors = new[] { Color.FromRgb(195, 60, 50), Color.FromRgb(55, 120, 190), Color.FromRgb(60, 150, 80), Color.FromRgb(225, 160, 40), Color.FromRgb(140, 75, 160) };

        double bookH = 6 * scale;
        double bookW = 28 * scale;

        for (int i = 0; i < currentBooks; i++)
        {
            double by = deskY - ((i + 1) * (bookH + 1));
            var brush = new SolidColorBrush(bookColors[i % bookColors.Length]);
            dc.DrawRoundedRectangle(brush, new Pen(new SolidColorBrush(Color.FromRgb(30, 30, 30)), 0.8), new Rect(x - (bookW * 0.5) + (i % 2 == 0 ? 0 : 2), by, bookW, bookH), 2, 2);
        }
    }

    private void DrawCoffeeMug(DrawingContext dc, double x, double y, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;
        double mugW = 22 * scale;
        double mugH = 26 * scale;

        // Mug Handle
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(230, 230, 235)), 3 * scale), new Point(x + mugW + (2 * scale), y + (mugH * 0.5)), 5 * scale, 6 * scale);

        // Mug Body
        var mugBrush = new SolidColorBrush(Color.FromRgb(245, 245, 250));
        dc.DrawRoundedRectangle(mugBrush, new Pen(new SolidColorBrush(Color.FromRgb(190, 190, 200)), 1), new Rect(x, y, mugW, mugH), 3 * scale, 3 * scale);

        // Coffee Fill Level (rises with progress)
        double fillH = (mugH - (6 * scale)) * Math.Max(0.1, ProgressFraction);
        double fillY = y + mugH - (3 * scale) - fillH;
        var coffeeBrush = new SolidColorBrush(Color.FromRgb(95, 50, 30));
        dc.DrawRectangle(coffeeBrush, null, new Rect(x + (2 * scale), fillY, mugW - (4 * scale), fillH));

        // Latte Foam on Top
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(240, 220, 195)), null, new Point(x + (mugW * 0.5), fillY), (mugW * 0.45), 2.5 * scale);

        // Swirling Steam
        if (IsTracking || IsGoalReached || IsRestPhase)
        {
            double steamY = y - ((_frameTick * 1.2) % 24);
            double steamWiggle = Math.Sin(_frameTick * 0.25) * 4 * scale;
            var steamBrush = new SolidColorBrush(Color.FromArgb(140, 240, 240, 240));
            dc.DrawEllipse(steamBrush, null, new Point(x + (mugW * 0.5) + steamWiggle, steamY), 3 * scale, 2 * scale);
        }

        if (IsGoalReached || ProgressFraction >= 0.999)
        {
            string text = isMini ? "☕ BREWED!" : "☕ PERFECT BREW! ✨";
            var ft = CreateText(text, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(255, 215, 90)), FontWeights.Bold);
            dc.DrawText(ft, new Point(x - (ft.Width * 0.4), y - (18 * scale)));
        }
    }

    #endregion

    #region 🏃 Scene 5: Marathon Runner

    private void RenderRunnerScene(DrawingContext dc, double w, double h)
    {
        double trackY = h * 0.72;

        // 1. Stadium Sky
        var skyBrush = new LinearGradientBrush(Color.FromRgb(50, 95, 160), Color.FromRgb(130, 185, 240), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(skyBrush, null, new Rect(0, 0, w, trackY));

        // Track (Red clay)
        var trackBrush = new LinearGradientBrush(Color.FromRgb(205, 75, 60), Color.FromRgb(165, 50, 40), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(trackBrush, null, new Rect(0, trackY, w, h - trackY));
        dc.DrawLine(new Pen(new SolidColorBrush(Colors.White), 1.5), new Point(0, trackY + 8), new Point(w, trackY + 8));

        // Finish Line Ribbon on Right (100%)
        double finishX = w - (IsMiniMode ? 25 : 55);
        DrawFinishLine(dc, finishX, trackY, IsMiniMode);

        // Runner Movement
        double startX = IsMiniMode ? 14 : 26;
        double endX = finishX - (IsMiniMode ? 12 : 22);
        double runnerX = startX + (ProgressFraction * (endX - startX));

        if (IsRestPhase)
        {
            DrawRestRunner(dc, w * 0.5, trackY, IsMiniMode);
        }
        else if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawVictoryRunner(dc, finishX, trackY, IsMiniMode);
        }
        else
        {
            DrawActiveRunner(dc, runnerX, trackY, IsMiniMode, IsTracking);
        }
    }

    private void DrawFinishLine(DrawingContext dc, double x, double trackY, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;
        double postH = 34 * scale;

        // Posts
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(40, 40, 40)), null, new Rect(x, trackY - postH, 3 * scale, postH));

        // Red Ribbon
        if (!IsGoalReached && ProgressFraction < 0.999)
        {
            var ribbonPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 50, 50)), 3 * scale);
            dc.DrawLine(ribbonPen, new Point(x, trackY - (postH * 0.6)), new Point(x - (12 * scale), trackY - (postH * 0.6)));
        }
    }

    private void DrawActiveRunner(DrawingContext dc, double x, double trackY, bool isMini, bool isRunning)
    {
        double scale = isMini ? 0.7 : 1.0;
        double runCycle = isRunning ? (_frameTick * 0.5) : 0;
        double legSwing = Math.Sin(runCycle) * 8 * scale;

        Point hip = new Point(x, trackY - (16 * scale));
        Point head = new Point(x + (6 * scale), trackY - (30 * scale));

        // Legs
        var legPen = new Pen(new SolidColorBrush(Color.FromRgb(240, 185, 145)), 3 * scale);
        dc.DrawLine(legPen, hip, new Point(hip.X + legSwing, trackY));
        dc.DrawLine(legPen, hip, new Point(hip.X - legSwing, trackY));

        // Torso & Jersey
        var jerseyPen = new Pen(new SolidColorBrush(Color.FromRgb(40, 130, 235)), 6 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(jerseyPen, hip, new Point(head.X - (2 * scale), head.Y + (6 * scale)));

        // Arms (Pumping)
        var armPen = new Pen(new SolidColorBrush(Color.FromRgb(240, 185, 145)), 2.5 * scale);
        dc.DrawLine(armPen, new Point(head.X - (2 * scale), head.Y + (8 * scale)), new Point(head.X + (8 * scale) - legSwing, head.Y + (14 * scale)));

        // Head & Headband
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(245, 195, 150)), null, head, 5.5 * scale, 5.5 * scale);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(245, 60, 60)), 2 * scale), new Point(head.X - (4 * scale), head.Y - (2 * scale)), new Point(head.X + (5 * scale), head.Y - (2 * scale)));
    }

    private void DrawVictoryRunner(DrawingContext dc, double x, double trackY, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;
        Point head = new Point(x, trackY - (32 * scale));

        // Arms raised in victory
        var armPen = new Pen(new SolidColorBrush(Color.FromRgb(240, 185, 145)), 2.5 * scale);
        dc.DrawLine(armPen, new Point(head.X, head.Y + (8 * scale)), new Point(head.X - (10 * scale), head.Y - (6 * scale)));
        dc.DrawLine(armPen, new Point(head.X, head.Y + (8 * scale)), new Point(head.X + (10 * scale), head.Y - (6 * scale)));

        // Head
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(245, 195, 150)), null, head, 5.5 * scale, 5.5 * scale);

        // Torso
        var jerseyPen = new Pen(new SolidColorBrush(Color.FromRgb(40, 130, 235)), 6 * scale);
        dc.DrawLine(jerseyPen, new Point(head.X, head.Y + (5 * scale)), new Point(head.X, trackY - (14 * scale)));

        // Banner / Trophy
        string text = isMini ? "🏆 FINISH! 🏆" : "🏆 CHAMPION! FINISH LINE CROSSED! 🥇";
        var ft = CreateText(text, isMini ? 10 : 13, new SolidColorBrush(Color.FromRgb(255, 215, 60)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.5), trackY - (46 * scale)));
    }

    private void DrawRestRunner(DrawingContext dc, double x, double trackY, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;
        // Bench
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(140, 80, 45)), null, new Rect(x - (16 * scale), trackY - (10 * scale), 32 * scale, 4 * scale));

        // Runner resting with towel
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(245, 195, 150)), null, new Point(x, trackY - (22 * scale)), 5 * scale, 5 * scale);

        string text = isMini ? "💧 Hydrate" : "💧 Hydrate & Stretch!";
        var ft = CreateText(text, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(100, 200, 255)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.5), trackY - (34 * scale)));
    }

    #endregion

    #region 👾 Scene 6: Focus Pet Tamagotchi

    private void RenderTamagotchiScene(DrawingContext dc, double w, double h)
    {
        // 1. Handheld Device Screen
        var shellBrush = new LinearGradientBrush(Color.FromRgb(25, 30, 45), Color.FromRgb(15, 18, 30), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(shellBrush, null, new Rect(0, 0, w, h));

        // Neon Grid Lines in Background
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 80, 200, 240)), 1);
        for (double gx = 0; gx < w; gx += 30) dc.DrawLine(gridPen, new Point(gx, 0), new Point(gx, h));
        for (double gy = 0; gy < h; gy += 20) dc.DrawLine(gridPen, new Point(0, gy), new Point(w, gy));

        // Pet Center
        Point petCenter = new Point(w * 0.5, h * 0.5);
        DrawFocusPet(dc, petCenter, IsMiniMode);
    }

    private void DrawFocusPet(DrawingContext dc, Point center, bool isMini)
    {
        double scale = isMini ? 0.7 : 1.0;
        double bounce = Math.Sin(_frameTick * 0.3) * (IsTracking ? 4 : 2) * scale;
        Point p = new Point(center.X, center.Y + bounce);

        // Pet Body (Chubby Neon Blob)
        var petBrush = new RadialGradientBrush(Color.FromRgb(120, 240, 180), Color.FromRgb(35, 180, 120));
        dc.DrawEllipse(petBrush, new Pen(new SolidColorBrush(Color.FromRgb(20, 120, 75)), 2 * scale), p, 20 * scale, 18 * scale);

        // Cute Triangular Ears
        var earBrush = new SolidColorBrush(Color.FromRgb(50, 200, 140));
        dc.DrawEllipse(earBrush, null, new Point(p.X - (12 * scale), p.Y - (14 * scale)), 5 * scale, 7 * scale);
        dc.DrawEllipse(earBrush, null, new Point(p.X + (12 * scale), p.Y - (14 * scale)), 5 * scale, 7 * scale);

        // Sparkling Eyes
        bool isBlinking = (_frameTick % 40) > 36;
        if (isBlinking || IsRestPhase)
        {
            // Happy Closed Eyes ^ ^
            var eyePen = new Pen(new SolidColorBrush(Color.FromRgb(20, 40, 30)), 2 * scale);
            dc.DrawLine(eyePen, new Point(p.X - (9 * scale), p.Y - (2 * scale)), new Point(p.X - (3 * scale), p.Y - (2 * scale)));
            dc.DrawLine(eyePen, new Point(p.X + (3 * scale), p.Y - (2 * scale)), new Point(p.X + (9 * scale), p.Y - (2 * scale)));
        }
        else
        {
            // Big Sparkling Eyes
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(20, 40, 30)), null, new Point(p.X - (6 * scale), p.Y - (2 * scale)), 3.5 * scale, 4 * scale);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(20, 40, 30)), null, new Point(p.X + (6 * scale), p.Y - (2 * scale)), 3.5 * scale, 4 * scale);
            // Highlights
            dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(p.X - (7 * scale), p.Y - (3.5 * scale)), 1.2 * scale, 1.2 * scale);
            dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(p.X + (5 * scale), p.Y - (3.5 * scale)), 1.2 * scale, 1.2 * scale);
        }

        // Rosy Cheeks
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(160, 255, 120, 160)), null, new Point(p.X - (11 * scale), p.Y + (3 * scale)), 3 * scale, 2 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(160, 255, 120, 160)), null, new Point(p.X + (11 * scale), p.Y + (3 * scale)), 3 * scale, 2 * scale);

        // Mouth
        var mouthPen = new Pen(new SolidColorBrush(Color.FromRgb(20, 40, 30)), 1.5 * scale);
        dc.DrawLine(mouthPen, new Point(p.X - (2 * scale), p.Y + (3 * scale)), new Point(p.X + (2 * scale), p.Y + (3 * scale)));

        // Stats Text
        int level = Math.Max(1, (FocusXp / 100) + 1);
        string text = IsRestPhase ? "💤 Resting Pet" : $"⭐ Lv.{level} Pet • {(int)(ProgressFraction * 100)}%";
        var ft = CreateText(text, isMini ? 9 : 11, new SolidColorBrush(Color.FromRgb(120, 240, 180)), FontWeights.Bold);
        dc.DrawText(ft, new Point(p.X - (ft.Width * 0.5), p.Y - (26 * scale)));
    }

    #endregion
}
