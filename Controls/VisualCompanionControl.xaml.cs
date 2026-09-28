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
            case "cafe":
                RenderCafeScene(dc, w, h);
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
        var rand = new Random(42 + (_frameTick / 2));
        for (int i = 0; i < (IsMiniMode ? 8 : 16); i++)
        {
            double sx = rand.NextDouble() * w;
            double sy = rand.NextDouble() * h;
            double size = 3 + rand.Next(4);
            var color = Color.FromRgb((byte)rand.Next(220, 255), (byte)rand.Next(180, 255), (byte)rand.Next(100, 255));
            DrawSparkle(dc, sx, sy, size, new SolidColorBrush(color));
        }
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

    #endregion

    #region 🚴 Scene 1: Cute Boy & Puppy Cycling Home

    private void RenderCyclingScene(DrawingContext dc, double w, double h)
    {
        double trailY = h * 0.70;
        double trailHeight = h - trailY;

        // 1. Warm Sunny Morning Sky Gradient
        var skyBrush = new LinearGradientBrush(
            Color.FromRgb(92, 172, 248),
            Color.FromRgb(248, 238, 222),
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

        // 3. Launch Planet Earth on Left (0%)
        double earthRadius = IsMiniMode ? 28 : 55;
        Point earthCenter = new Point(0, h * 0.5);
        var earthBrush = new RadialGradientBrush(Color.FromRgb(85, 190, 255), Color.FromRgb(25, 80, 180));
        dc.DrawEllipse(earthBrush, new Pen(new SolidColorBrush(Color.FromArgb(120, 150, 220, 255)), 2), earthCenter, earthRadius, earthRadius);
        // Cute green continent & cloud swirls
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(80, 210, 125)), null, new Point(earthRadius * 0.42, (h * 0.5) - (earthRadius * 0.2)), earthRadius * 0.35, earthRadius * 0.25);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), null, new Point(earthRadius * 0.35, (h * 0.5) + (earthRadius * 0.15)), earthRadius * 0.28, earthRadius * 0.15);

        // 4. Moon Destination on Right (100%)
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

        // 5. Chunky Cute Rocket
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
        double floorY = h * 0.72;

        // 1. Cozy Pastel Peach Wallpaper & Polished Wooden Floor
        var wallBrush = new LinearGradientBrush(Color.FromRgb(255, 242, 235), Color.FromRgb(255, 228, 218), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(wallBrush, null, new Rect(0, 0, w, floorY));

        // Decorative wall garland / fairy bunting
        if (!IsMiniMode)
        {
            var garlandPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 180, 140, 120)), 1);
            dc.DrawLine(garlandPen, new Point(0, 14), new Point(w * 0.5, 22));
            dc.DrawLine(garlandPen, new Point(w * 0.5, 22), new Point(w, 14));
            for (double bx = 30; bx < w; bx += 50)
            {
                DrawHeart(dc, bx, 18, 4, new SolidColorBrush(Color.FromRgb(255, 160, 180)));
            }
        }

        // Wooden Floor
        var floorBrush = new LinearGradientBrush(Color.FromRgb(225, 175, 125), Color.FromRgb(190, 135, 90), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(floorBrush, null, new Rect(0, floorY, w, h - floorY));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(150, 105, 65)), 2), new Point(0, floorY), new Point(w, floorY));

        // Cozy Circular Rug in middle
        double rugW = w * 0.65;
        double rugH = (h - floorY) * 0.8;
        var rugBrush = new LinearGradientBrush(Color.FromRgb(255, 225, 235), Color.FromRgb(240, 195, 215), new Point(0, 0), new Point(0, 1));
        dc.DrawEllipse(rugBrush, new Pen(new SolidColorBrush(Color.FromRgb(220, 160, 185)), 1.2), new Point(w * 0.5, floorY + (rugH * 0.5)), rugW * 0.5, rugH * 0.5);

        // 2. Sparkling Golden Fish Treat Bowl on Right (100%)
        double bowlX = w - (IsMiniMode ? 36 : 68);
        double bowlY = floorY - (IsMiniMode ? 14 : 24);
        DrawCuteFishBowl(dc, bowlX, bowlY, IsMiniMode);

        // 3. Cute Kitty Movement & Yarn
        double startX = IsMiniMode ? 16 : 32;
        double endX = bowlX - (IsMiniMode ? 18 : 34);
        double catX = startX + (ProgressFraction * (endX - startX));

        if (IsRestPhase)
        {
            DrawCuteSleepingKitty(dc, w * 0.5, floorY, IsMiniMode);
        }
        else if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawCuteFeastingKitty(dc, bowlX - (IsMiniMode ? 14 : 26), floorY, IsMiniMode);
        }
        else
        {
            DrawCuteWalkingKittyWithYarn(dc, catX, floorY, IsMiniMode, IsTracking);
        }
    }

    private void DrawCuteFishBowl(DrawingContext dc, double x, double y, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;
        double bowlW = 28 * scale;
        double bowlH = 18 * scale;

        // Golden Treat Bowl
        var bowlBrush = new LinearGradientBrush(Color.FromRgb(255, 225, 75), Color.FromRgb(235, 175, 30), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(bowlBrush, new Pen(new SolidColorBrush(Color.FromRgb(185, 130, 15)), 1.2), new Rect(x, y, bowlW, bowlH), 5 * scale, 5 * scale);

        // Tasty Fish Treat popping out
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(105, 195, 255)), null, new Point(x + (bowlW * 0.5), y + (3 * scale)), 9 * scale, 5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Colors.Black), null, new Point(x + (bowlW * 0.75), y + (2 * scale)), 1.2 * scale, 1.2 * scale);

        // Sparkle above bowl
        DrawSparkle(dc, x + (bowlW * 0.5), y - (6 * scale), 3 * scale, new SolidColorBrush(Color.FromRgb(255, 230, 90)));
    }

    private void DrawCuteWalkingKittyWithYarn(DrawingContext dc, double x, double floorY, bool isMini, bool isWalking)
    {
        double scale = isMini ? 0.72 : 1.0;
        double bob = isWalking ? Math.Sin(_frameTick * 0.7) * 2.2 * scale : 0;
        double catY = floorY - (16 * scale) + bob;

        var gingerBrush = new SolidColorBrush(Color.FromRgb(255, 160, 75));

        // Yarn Ball rolling ahead of kitty 🧶
        double yarnX = x + (24 * scale);
        double yarnY = floorY - (7 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 110, 150)), null, new Point(yarnX, yarnY), 6 * scale, 6 * scale);
        // Yarn line
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 140, 175)), 1.2), new Point(x + (12 * scale), floorY - (4 * scale)), new Point(yarnX, yarnY));

        // Swishing Fluffy Tail
        double tailWave = Math.Sin(_frameTick * 0.45) * 0.35;
        Point tailStart = new Point(x - (12 * scale), catY + (2 * scale));
        Point tailEnd = new Point(tailStart.X - (10 * scale) + (Math.Sin(tailWave) * 8), tailStart.Y - (14 * scale));
        dc.DrawLine(new Pen(gingerBrush, 4.5 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, tailStart, tailEnd);

        // Cute Walking Paws
        double step = isWalking ? Math.Sin(_frameTick * 0.7) * 4.5 * scale : 0;
        var pawPen = new Pen(gingerBrush, 3.5 * scale) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(pawPen, new Point(x - (7 * scale), catY + (7 * scale)), new Point(x - (7 * scale) + step, floorY));
        dc.DrawLine(pawPen, new Point(x - (2 * scale), catY + (7 * scale)), new Point(x - (2 * scale) - step, floorY));
        dc.DrawLine(pawPen, new Point(x + (4 * scale), catY + (7 * scale)), new Point(x + (4 * scale) - step, floorY));
        dc.DrawLine(pawPen, new Point(x + (9 * scale), catY + (7 * scale)), new Point(x + (9 * scale) + step, floorY));

        // Chubby Fluffy Body
        dc.DrawEllipse(gingerBrush, null, new Point(x, catY), 13 * scale, 9 * scale);
        // White belly patch
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 248, 240)), null, new Point(x + (1 * scale), catY + (2 * scale)), 8 * scale, 5 * scale);

        // KAWAII KITTY HEAD
        Point head = new Point(x + (14 * scale), catY - (5 * scale));
        dc.DrawEllipse(gingerBrush, null, head, 8.5 * scale, 8 * scale);

        // Fluffy Cute Ears with Pink Inside
        var ear1 = new PathGeometry();
        var e1Fig = new PathFigure { StartPoint = new Point(head.X - (5 * scale), head.Y - (6 * scale)) };
        e1Fig.Segments.Add(new LineSegment(new Point(head.X - (3 * scale), head.Y - (15 * scale)), true));
        e1Fig.Segments.Add(new LineSegment(new Point(head.X + (1 * scale), head.Y - (7 * scale)), true));
        ear1.Figures.Add(e1Fig);
        dc.DrawGeometry(gingerBrush, null, ear1);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(255, 195, 205)), null, ear1);

        var ear2 = new PathGeometry();
        var e2Fig = new PathFigure { StartPoint = new Point(head.X + (2 * scale), head.Y - (7 * scale)) };
        e2Fig.Segments.Add(new LineSegment(new Point(head.X + (6 * scale), head.Y - (15 * scale)), true));
        e2Fig.Segments.Add(new LineSegment(new Point(head.X + (8 * scale), head.Y - (5 * scale)), true));
        ear2.Figures.Add(e2Fig);
        dc.DrawGeometry(gingerBrush, null, ear2);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(255, 195, 205)), null, ear2);

        // Big Anime Sparkle Eyes
        Point eye = new Point(head.X + (3.5 * scale), head.Y - (1 * scale));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(35, 25, 20)), null, eye, 2.2 * scale, 2.5 * scale);
        dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(eye.X - (0.6 * scale), eye.Y - (0.8 * scale)), 0.8 * scale, 0.8 * scale);
        dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(eye.X + (0.6 * scale), eye.Y + (0.6 * scale)), 0.4 * scale, 0.4 * scale);

        // Cute Pink Nose & Rosy Blush
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 130, 160)), null, new Point(head.X + (7 * scale), head.Y + (1.5 * scale)), 1.2 * scale, 0.9 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 130, 160)), null, new Point(head.X + (2 * scale), head.Y + (3 * scale)), 2.8 * scale, 1.6 * scale);
    }

    private void DrawCuteFeastingKitty(DrawingContext dc, double x, double floorY, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;
        DrawCuteWalkingKittyWithYarn(dc, x, floorY, isMini, false);

        // Floating Hearts & Purrs 💖
        for (int h = 0; h < 3; h++)
        {
            double hx = x + (12 * scale) + (h * 10 * scale);
            double hy = floorY - (28 * scale) - ((_frameTick * 0.8 + (h * 12)) % 22);
            DrawHeart(dc, hx, hy, 4 * scale, new SolidColorBrush(Color.FromRgb(255, 95, 150)));
        }

        string text = isMini ? "💖 YUM! 💖" : "💖 PURRR! YUMMY TREATS! 🐱✨";
        var ft = CreateText(text, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(255, 90, 150)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x - (ft.Width * 0.35), floorY - (38 * scale)));
    }

    private void DrawCuteSleepingKitty(DrawingContext dc, double x, double floorY, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;
        double breathe = Math.Sin(_frameTick * 0.22) * 1.5 * scale;

        // Plush Donut Cushion
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(220, 145, 195)), null, new Rect(x - (26 * scale), floorY - (10 * scale), 52 * scale, 10 * scale), 5 * scale, 5 * scale);

        // Sleeping Curled Kitty
        Point catPos = new Point(x, floorY - (14 * scale) + breathe);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 160, 75)), null, catPos, 16 * scale, (11 * scale) + breathe);
        // Head
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 160, 75)), null, new Point(catPos.X + (9 * scale), catPos.Y), 7 * scale, 7 * scale);
        // Sleepy closed eye curve (^_^)
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(80, 45, 30)), 1.5), new Point(catPos.X + (7 * scale), catPos.Y), new Point(catPos.X + (10 * scale), catPos.Y));

        // "💤 z Z Z"
        string text = (_frameTick % 20 < 10) ? "💤 z Z Z" : "💤 Z z z";
        var ft = CreateText(text, isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(180, 120, 240)), FontWeights.Bold);
        dc.DrawText(ft, new Point(x + (16 * scale), floorY - (32 * scale)));
    }

    #endregion

    #region ☕ Scene 4: 🌧️ Zoomed-in Rainy Window Cafe (Foggy Glass & Prominent Coffee Mug)

    private void RenderCafeScene(DrawingContext dc, double w, double h)
    {
        // 1. Zoomed-in Rainy Twilight Window Backdrop (NO dividing line!)
        var outdoorBrush = new LinearGradientBrush(
            Color.FromRgb(14, 18, 34),
            Color.FromRgb(26, 32, 52),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(outdoorBrush, null, new Rect(0, 0, w, h));

        // 2. Soft Blurred Bokeh Orbs (Distant Streetlights & Cafe lanterns glowing warmly through glass)
        DrawRainyBokeh(dc, w, h);

        // 3. Falling Rain Streaks Outside in the Rain (\ \ \)
        DrawFallingRainStreaks(dc, w, h);

        // 4. Cozy Wooden Window Sill along the bottom
        double sillH = IsMiniMode ? 12 : 22;
        double sillY = h - sillH;

        var sillBrush = new LinearGradientBrush(
            Color.FromRgb(62, 38, 24),
            Color.FromRgb(40, 22, 14),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(sillBrush, null, new Rect(0, sillY, w, sillH));

        // Bevel / ambient light reflection along the top edge of the wooden sill
        var bevelPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 145, 100, 70)), IsMiniMode ? 1.0 : 1.5);
        dc.DrawLine(bevelPen, new Point(0, sillY), new Point(w, sillY));

        // Mini decorative details on the wooden windowsill
        if (!IsMiniMode)
        {
            // Tiny green succulent pot on the left sill
            DrawMiniSucculent(dc, 28, sillY);

            // Warm fairy lights string glowing along the window sill
            DrawFairyLights(dc, w, sillY);
        }

        // 5. Large Prominent Artisan Coffee Mug on Saucer (Right side)
        double mugW = IsMiniMode ? 46 : 84;
        double mugH = IsMiniMode ? 42 : 78;
        double mugX = w - mugW - (IsMiniMode ? 18 : 60);
        double mugY = sillY - mugH + (IsMiniMode ? 3 : 5);

        // Ceramic Saucer / Coaster under the mug
        double saucerW = mugW * 1.35;
        double saucerH = IsMiniMode ? 7 : 12;
        double saucerX = mugX - ((saucerW - mugW) * 0.5);
        double saucerY = sillY - (saucerH * 0.4);

        // Saucer drop shadow
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(90, 15, 8, 4)), null, new Point(saucerX + (saucerW * 0.5), saucerY + saucerH - 1), saucerW * 0.52, saucerH * 0.45);

        // Saucer body
        var saucerBrush = new LinearGradientBrush(
            Color.FromRgb(252, 248, 240),
            Color.FromRgb(224, 212, 196),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRoundedRectangle(saucerBrush, new Pen(new SolidColorBrush(Color.FromRgb(175, 160, 142)), 1.2), new Rect(saucerX, saucerY, saucerW, saucerH), saucerH * 0.5, saucerH * 0.5);

        // Inner saucer rim
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(50, 120, 100, 80)), null, new Point(saucerX + (saucerW * 0.5), saucerY + (saucerH * 0.45)), saucerW * 0.35, saucerH * 0.25);

        // 6. Draw the Large Artisan Coffee Mug with Latte Art and rising level
        DrawAestheticRainyCoffeeMug(dc, mugX, mugY, mugW, mugH, IsMiniMode);

        // 7. Rising Steam Wisps from Hot Coffee Mug
        DrawMugSteamWisps(dc, mugX + (mugW * 0.5), mugY, mugW, mugH, IsMiniMode);

        // 8. 4-Pane Window Lattice Grid (Warm mahogany window mullions & frame)
        DrawFourPaneWindowGrid(dc, w, h - sillH);

        // 9. Dynamic Glass Fog Layer: Glass gets progressively foggier across all 4 panes!
        double fogLevel = Math.Clamp(0.12 + (ProgressFraction * 0.78), 0.12, 0.90);
        DrawWindowFogLayer(dc, w, h - sillH, fogLevel, mugX, mugY);

        // 10. Water Droplets & Trickling Trails running down the foggy glass
        DrawWindowGlassDroplets(dc, w, h - sillH, fogLevel);

        // 11. Soft Warm Indoor Ambient Light Reflection on the Glass
        var warmAmbient = new RadialGradientBrush(
            Color.FromArgb(35, 255, 205, 120),
            Color.FromArgb(0, 255, 180, 80));
        dc.DrawEllipse(warmAmbient, null, new Point(w * 0.3, h * 0.6), w * 0.5, h * 0.45);

        // 12. Status Overlays: Finger-drawn in condensation on foggy glass!
        if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawTaskCompleteFingerWriting(dc, w, h - sillH, mugX, IsMiniMode);
        }
        else if (IsRestPhase)
        {
            DrawRestFingerWriting(dc, w, h - sillH, mugX, IsMiniMode);
        }
        else if (IsTracking)
        {
            DrawFogProgressHint(dc, w, h - sillH, mugX, ProgressFraction, IsMiniMode);
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

    private void DrawFairyLights(DrawingContext dc, double w, double sillY)
    {
        var wirePen = new Pen(new SolidColorBrush(Color.FromArgb(120, 80, 60, 40)), 0.8);
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
            double dipY = sillY + 6;
            // Drooping wire segment
            var geom = new PathGeometry();
            var fig = new PathFigure { StartPoint = new Point(prevX, prevY) };
            fig.Segments.Add(new QuadraticBezierSegment(new Point((prevX + fx) * 0.5, dipY), new Point(fx, sillY + 3), true));
            geom.Figures.Add(fig);
            dc.DrawGeometry(null, wirePen, geom);

            // Glowing light bulb
            Color bulbCol = bulbColors[idx % bulbColors.Length];
            double pulse = Math.Sin((_frameTick * 0.12) + idx) * 0.2 + 0.8;
            var glowBrush = new RadialGradientBrush(
                Color.FromArgb((byte)(160 * pulse), bulbCol.R, bulbCol.G, bulbCol.B),
                Color.FromArgb(0, bulbCol.R, bulbCol.G, bulbCol.B));
            dc.DrawEllipse(glowBrush, null, new Point(fx, sillY + 3), 9, 9);
            dc.DrawEllipse(new SolidColorBrush(bulbCol), null, new Point(fx, sillY + 3), 2.2, 2.8);

            prevX = fx;
            prevY = sillY + 3;
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
        if (IsTracking || IsGoalReached || IsRestPhase)
        {
            double scale = isMini ? 0.6 : 1.0;
            int wispCount = isMini ? 3 : 5;

            for (int s = 0; s < wispCount; s++)
            {
                double maxRise = isMini ? 35 : 75;
                double speed = 1.0 + (s * 0.22);
                double steamOffset = (_frameTick * speed + (s * 16)) % maxRise;
                double steamY = mugTopY - steamOffset;

                double wave = Math.Sin((_frameTick * 0.18) + (s * 1.2)) * (6.0 * scale);
                double alphaFrac = Math.Clamp(1.0 - (steamOffset / maxRise), 0.05, 0.85);

                var steamBrush = new SolidColorBrush(Color.FromArgb((byte)(160 * alphaFrac), 255, 246, 235));
                double wispX = centerX + wave + ((s - (wispCount * 0.5)) * (9 * scale));
                double wispRadiusX = (4.5 + (steamOffset * 0.08)) * scale;
                double wispRadiusY = (3.0 + (steamOffset * 0.05)) * scale;

                dc.DrawEllipse(steamBrush, null, new Point(wispX, steamY), wispRadiusX, wispRadiusY);
            }
        }
    }

    private void DrawAestheticRainyCoffeeMug(DrawingContext dc, double x, double y, double w, double h, bool isMini)
    {
        double scale = isMini ? 0.55 : 1.0;

        // Mug Drop Shadow onto saucer
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 20, 10, 5)), null, new Point(x + (w * 0.5), y + h), w * 0.48, 5 * scale);

        // 1. Ceramic Handle on the right of the mug
        double handleW = 16 * scale;
        double handleH = 38 * scale;
        double handleX = x + w - (4 * scale);
        double handleY = y + (h * 0.22);

        var handlePen = new Pen(new LinearGradientBrush(
            Color.FromRgb(254, 250, 244),
            Color.FromRgb(215, 202, 186),
            new Point(0, 0),
            new Point(1, 1)), 6.0 * scale);

        var handleGeom = new PathGeometry();
        var hf = new PathFigure { StartPoint = new Point(handleX, handleY) };
        hf.Segments.Add(new BezierSegment(
            new Point(handleX + handleW + (12 * scale), handleY + (handleH * 0.1)),
            new Point(handleX + handleW + (12 * scale), handleY + (handleH * 0.9)),
            new Point(handleX, handleY + handleH),
            true));
        handleGeom.Figures.Add(hf);
        dc.DrawGeometry(null, handlePen, handleGeom);

        // 2. Ceramic Mug Body (Warm Cream / Latte Glaze)
        var mugBrush = new LinearGradientBrush(
            Color.FromRgb(255, 252, 246),
            Color.FromRgb(230, 218, 202),
            new Point(0, 0),
            new Point(1, 0));
        var mugPen = new Pen(new SolidColorBrush(Color.FromRgb(180, 168, 150)), 1.4 * scale);

        // Elegant tapered mug body with rounded bottom corners
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

        // Specular vertical gloss highlight on left curve of ceramic
        var glossBrush = new LinearGradientBrush(
            Color.FromArgb(130, 255, 255, 255),
            Color.FromArgb(0, 255, 255, 255),
            new Point(0, 0),
            new Point(1, 0));
        dc.DrawRoundedRectangle(glossBrush, null, new Rect(x + (9 * scale), y + (12 * scale), 8 * scale, h - (22 * scale)), 4 * scale, 4 * scale);

        // Cute Coffee Heart Emblem Stamp on Mug Front ♡
        double heartY = y + (h * 0.55);
        DrawHeart(dc, x + (w * 0.48), heartY, 7 * scale, new SolidColorBrush(Color.FromRgb(215, 115, 95)));
        if (!isMini)
        {
            // Golden bronze aroma sparkle next to heart
            DrawSparkle(dc, x + (w * 0.48) + 14, heartY - 6, 2.5, new SolidColorBrush(Color.FromRgb(235, 180, 90)));
        }

        // 3. Mug Top Opening & Coffee Fill Level
        double rimH = 16 * scale;
        double rimY = y + (2 * scale);

        // Dark inner ceramic rim depth
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(65, 42, 28)), new Pen(new SolidColorBrush(Color.FromRgb(165, 150, 135)), 1.2 * scale), new Point(x + (w * 0.5), rimY + (rimH * 0.5)), (w * 0.47), rimH * 0.5);

        // Dynamic coffee liquid fill level
        double fillFraction = Math.Max(0.18, ProgressFraction);
        double maxFillTravel = (h - (18 * scale));
        double liquidY = y + h - (6 * scale) - (maxFillTravel * fillFraction);
        double liquidH = rimH * (0.6 + (0.4 * fillFraction));

        // Liquid body
        var coffeeBrush = new LinearGradientBrush(
            Color.FromRgb(68, 32, 16),
            Color.FromRgb(46, 20, 10),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawEllipse(coffeeBrush, null, new Point(x + (w * 0.5), liquidY + (liquidH * 0.5)), (w * 0.44), liquidH * 0.5);

        // Golden crema surface gradient
        var cremaBrush = new RadialGradientBrush(
            Color.FromRgb(215, 160, 100),
            Color.FromRgb(105, 50, 24));
        dc.DrawEllipse(cremaBrush, null, new Point(x + (w * 0.5), liquidY + (liquidH * 0.5)), (w * 0.40), liquidH * 0.42);

        // Creamy Latte Art Heart on coffee surface
        double latteHeartSize = (w * 0.18) * Math.Min(1.0, fillFraction * 1.5);
        DrawHeart(dc, x + (w * 0.5), liquidY + (liquidH * 0.5), latteHeartSize, new SolidColorBrush(Color.FromRgb(255, 242, 225)));
        DrawHeart(dc, x + (w * 0.5), liquidY + (liquidH * 0.5), latteHeartSize * 0.55, new SolidColorBrush(Color.FromRgb(140, 75, 40)));
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

        // 4. Draw the Cute Pastel Ice Cream Truck
        double scale = IsMiniMode ? 0.62 : 1.0;
        double truckW = 145 * scale;
        double truckH = 84 * scale;
        double truckX = IsMiniMode ? 6 : 24;
        double truckY = groundY - truckH + (14 * scale);

        DrawPastelIceCreamTruck(dc, truckX, truckY, truckW, truckH, scale);

        // 5. Draw Queueing Kids or Celebration / Rest mode
        if (IsGoalReached || ProgressFraction >= 0.999)
        {
            DrawIceCreamPartyCelebration(dc, w, groundY, truckX + truckW + (8 * scale), scale);
        }
        else if (IsRestPhase)
        {
            DrawIceCreamRestPicnic(dc, w, groundY, truckX + truckW + (8 * scale), scale);
        }
        else
        {
            DrawIceCreamQueueingKids(dc, w, groundY, truckX + truckW + (8 * scale), scale, ProgressFraction, IsTracking);
        }
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

    private void DrawPastelIceCreamTruck(DrawingContext dc, double x, double y, double w, double h, double scale)
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

        // Vendor waving hand
        double waveOffset = Math.Sin(_frameTick * 0.2) * (3 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 218, 195)), null, new Point(vendorX + (10 * scale), vendorY - (12 * scale) + waveOffset), 2.8 * scale, 2.8 * scale);

        // Service Counter Shelf with colorful ice cream tubs
        dc.DrawRoundedRectangle(chromeBrush, null, new Rect(servX - (2 * scale), servY + servH - (3 * scale), servW + (4 * scale), 4 * scale), 1.5 * scale, 1.5 * scale);

        // 3 mini tubs: Strawberry, Mint, Chocolate
        if (!IsMiniMode)
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 130, 160)), null, new Rect(servX + (4 * scale), servY + servH - (7 * scale), 7 * scale, 4 * scale), 1, 1);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(120, 220, 180)), null, new Rect(servX + (13 * scale), servY + servH - (7 * scale), 7 * scale, 4 * scale), 1, 1);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(140, 80, 50)), null, new Rect(servX + (22 * scale), servY + servH - (7 * scale), 7 * scale, 4 * scale), 1, 1);
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

    private void DrawIceCreamQueueingKids(DrawingContext dc, double w, double groundY, double startX, double scale, double progress, bool isTracking)
    {
        double kidSpacing = IsMiniMode ? (30 * scale) : (44 * scale);

        // Kid 1
        if (progress >= 0.05 || !isTracking)
        {
            double targetX = startX + (8 * scale);
            double kx = isTracking ? Math.Min(targetX, w - (40 * scale) - ((1.0 - Math.Min(1.0, progress * 4.0)) * (80 * scale))) : targetX;
            bool hasCone = progress >= 0.15 || !isTracking;
            DrawChibiKid(dc, kx, groundY, scale, 0, hasCone, "strawberry");
        }

        // Kid 2
        if (progress >= 0.25)
        {
            double targetX = startX + (8 * scale) + kidSpacing;
            double kx = Math.Min(targetX, w - (30 * scale) - ((1.0 - Math.Min(1.0, (progress - 0.25) * 4.0)) * (80 * scale)));
            bool hasCone = progress >= 0.40;
            DrawChibiKid(dc, kx, groundY, scale, 1, hasCone, "mint_rainbow");
        }

        // Kid 3 (+ Puppy!)
        if (progress >= 0.50)
        {
            double targetX = startX + (8 * scale) + (kidSpacing * 2);
            double kx = Math.Min(targetX, w - (20 * scale) - ((1.0 - Math.Min(1.0, (progress - 0.50) * 4.0)) * (80 * scale)));
            bool hasCone = progress >= 0.65;
            DrawChibiKid(dc, kx, groundY, scale, 2, hasCone, "chocolate");
            // Puppy beside kid 3
            DrawCutePuppy(dc, kx + (14 * scale), groundY, scale);
        }

        // Kid 4 (+ Balloon!)
        if (progress >= 0.75)
        {
            double targetX = startX + (8 * scale) + (kidSpacing * 3);
            double kx = Math.Min(targetX, w - (10 * scale) - ((1.0 - Math.Min(1.0, (progress - 0.75) * 4.0)) * (80 * scale)));
            bool hasCone = progress >= 0.85;
            DrawChibiKid(dc, kx, groundY, scale, 3, hasCone, "pop");
        }

        // Status text / hint on top right
        if (isTracking)
        {
            int pct = (int)(progress * 100);
            string hint = IsMiniMode ? $"🍦 {pct}%" : $"🍦 {pct}% Focus • Serving treats to happy kids!";
            var ft = CreateText(hint, IsMiniMode ? 9.5 : 11, new SolidColorBrush(Color.FromRgb(60, 45, 30)), FontWeights.SemiBold);
            dc.DrawText(ft, new Point(w - ft.Width - (IsMiniMode ? 8 : 16), IsMiniMode ? 4 : 8));
        }
    }

    private void DrawChibiKid(DrawingContext dc, double x, double groundY, double scale, int kidIndex, bool hasIceCream, string flavor)
    {
        double bounce = Math.Sin((_frameTick * 0.2) + kidIndex) * (1.5 * scale);
        double kidH = 34 * scale;
        double kidY = groundY - kidH + bounce;

        var skinBrush = new SolidColorBrush(Color.FromRgb(255, 222, 198));
        var blushBrush = new SolidColorBrush(Color.FromArgb(170, 255, 120, 140));
        var eyeBrush = new SolidColorBrush(Color.FromRgb(60, 40, 25));

        switch (kidIndex)
        {
            case 0: // Boy with Blue Cap
                // Body (Blue Shirt & Shorts)
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(70, 145, 235)), null, new Rect(x - (5 * scale), kidY + (14 * scale), 10 * scale, 12 * scale), 2, 2);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(190, 130, 85)), null, new Rect(x - (4 * scale), kidY + (24 * scale), 8 * scale, 7 * scale), 1, 1);
                // Head
                dc.DrawEllipse(skinBrush, null, new Point(x, kidY + (8 * scale)), 7 * scale, 7 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x - (4 * scale), kidY + (10 * scale)), 1.8 * scale, 1.2 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x + (4 * scale), kidY + (10 * scale)), 1.8 * scale, 1.2 * scale);
                // Eyes (^_^)
                dc.DrawLine(new Pen(eyeBrush, 1.2 * scale), new Point(x - (4 * scale), kidY + (8 * scale)), new Point(x - (2 * scale), kidY + (8 * scale)));
                dc.DrawLine(new Pen(eyeBrush, 1.2 * scale), new Point(x + (2 * scale), kidY + (8 * scale)), new Point(x + (4 * scale), kidY + (8 * scale)));
                // Blue Cap
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(50, 115, 215)), null, new Rect(x - (7 * scale), kidY + (2 * scale), 14 * scale, 6 * scale), 3, 3);
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(40, 95, 185)), null, new Rect(x - (2 * scale), kidY + (5 * scale), 11 * scale, 2.5 * scale));
                break;

            case 1: // Girl with Yellow Dress & Pigtails
                // Yellow Dress
                var dressGeom = new PathGeometry();
                var df = new PathFigure { StartPoint = new Point(x - (3 * scale), kidY + (13 * scale)) };
                df.Segments.Add(new LineSegment(new Point(x + (3 * scale), kidY + (13 * scale)), true));
                df.Segments.Add(new LineSegment(new Point(x + (7 * scale), kidY + (27 * scale)), true));
                df.Segments.Add(new LineSegment(new Point(x - (7 * scale), kidY + (27 * scale)), true));
                df.IsClosed = true;
                dressGeom.Figures.Add(df);
                dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(255, 220, 75)), null, dressGeom);

                // Head
                dc.DrawEllipse(skinBrush, null, new Point(x, kidY + (8 * scale)), 6.5 * scale, 6.5 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x - (4 * scale), kidY + (10 * scale)), 1.8 * scale, 1.2 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x + (4 * scale), kidY + (10 * scale)), 1.8 * scale, 1.2 * scale);
                dc.DrawLine(new Pen(eyeBrush, 1.2 * scale), new Point(x - (4 * scale), kidY + (8 * scale)), new Point(x - (2 * scale), kidY + (8 * scale)));
                dc.DrawLine(new Pen(eyeBrush, 1.2 * scale), new Point(x + (2 * scale), kidY + (8 * scale)), new Point(x + (4 * scale), kidY + (8 * scale)));
                // Brown Pigtails
                var hairBrush = new SolidColorBrush(Color.FromRgb(115, 65, 35));
                dc.DrawEllipse(hairBrush, null, new Point(x - (8 * scale), kidY + (6 * scale)), 3.5 * scale, 5 * scale);
                dc.DrawEllipse(hairBrush, null, new Point(x + (8 * scale), kidY + (6 * scale)), 3.5 * scale, 5 * scale);
                dc.DrawRoundedRectangle(hairBrush, null, new Rect(x - (6.5 * scale), kidY + (2 * scale), 13 * scale, 5 * scale), 2, 2);
                break;

            case 2: // Boy in Denim Overalls
                // Overalls
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 140, 80)), null, new Rect(x - (5 * scale), kidY + (13 * scale), 10 * scale, 6 * scale), 2, 2);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(65, 115, 175)), null, new Rect(x - (5 * scale), kidY + (17 * scale), 10 * scale, 14 * scale), 2, 2);
                // Head
                dc.DrawEllipse(skinBrush, null, new Point(x, kidY + (8 * scale)), 6.5 * scale, 6.5 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x - (4 * scale), kidY + (10 * scale)), 1.8 * scale, 1.2 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x + (4 * scale), kidY + (10 * scale)), 1.8 * scale, 1.2 * scale);
                dc.DrawLine(new Pen(eyeBrush, 1.2 * scale), new Point(x - (4 * scale), kidY + (8 * scale)), new Point(x - (2 * scale), kidY + (8 * scale)));
                dc.DrawLine(new Pen(eyeBrush, 1.2 * scale), new Point(x + (2 * scale), kidY + (8 * scale)), new Point(x + (4 * scale), kidY + (8 * scale)));
                // Hair
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(70, 45, 30)), null, new Rect(x - (6.5 * scale), kidY + (2 * scale), 13 * scale, 5 * scale), 2, 2);
                break;

            default: // Toddler in Pink Romper with Balloon
                // Pink Romper
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 160, 195)), null, new Rect(x - (4.5 * scale), kidY + (12 * scale), 9 * scale, 13 * scale), 3, 3);
                // Head
                dc.DrawEllipse(skinBrush, null, new Point(x, kidY + (7 * scale)), 6 * scale, 6 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x - (3.5 * scale), kidY + (9 * scale)), 1.6 * scale, 1.0 * scale);
                dc.DrawEllipse(blushBrush, null, new Point(x + (3.5 * scale), kidY + (9 * scale)), 1.6 * scale, 1.0 * scale);
                dc.DrawLine(new Pen(eyeBrush, 1.2 * scale), new Point(x - (3 * scale), kidY + (7 * scale)), new Point(x - (1.5 * scale), kidY + (7 * scale)));
                dc.DrawLine(new Pen(eyeBrush, 1.2 * scale), new Point(x + (1.5 * scale), kidY + (7 * scale)), new Point(x + (3 * scale), kidY + (7 * scale)));
                // Floating Star Balloon
                if (!IsMiniMode)
                {
                    double bx = x + (10 * scale);
                    double by = kidY - (12 * scale) + (Math.Sin(_frameTick * 0.15) * 3);
                    dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(140, 100, 100, 100)), 0.8), new Point(x + (3 * scale), kidY + (14 * scale)), new Point(bx, by + 6));
                    DrawSparkle(dc, bx, by, 7 * scale, new SolidColorBrush(Color.FromRgb(255, 215, 75)));
                }
                break;
        }

        // Draw Ice Cream Held in Hand
        if (hasIceCream)
        {
            double icX = x + (6 * scale);
            double icY = kidY + (14 * scale);

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

    #endregion

    #region 🎧 Scene: Tokyo Metro Lo-Fi Girl

    private void RenderMetroScene(DrawingContext dc, double w, double h)
    {
        double windowTopY = IsMiniMode ? 14 : 20;
        double windowH = h - windowTopY - (IsMiniMode ? 14 : 24);
        double seatY = h - (IsMiniMode ? 16 : 30);

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

        // B. Distant Tokyo Skyline Silhouettes & Lit Windows
        DrawTokyoSkyline(dc, windowRect);

        // C. Glowing Tokyo Tower in the distance (Warm Orange/Red)
        DrawTokyoTower(dc, windowRect);

        // D. Parallax Moving Japanese Neon Billboard Signs
        DrawPassingTokyoNeonSigns(dc, windowRect);

        // E. Moving Streetlight Bokeh Orbs outside
        DrawMetroPassingBokeh(dc, windowRect);

        // F. Soft Window Glass Reflection & condensation glints
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
    }

    private void DrawTokyoSkyline(DrawingContext dc, Rect rect)
    {
        var buildBrush = new SolidColorBrush(Color.FromRgb(18, 14, 38));
        var winBrush1 = new SolidColorBrush(Color.FromArgb(160, 255, 235, 140));
        var winBrush2 = new SolidColorBrush(Color.FromArgb(160, 120, 220, 255));

        // Background buildings
        double bBaseY = rect.Bottom;
        var buildingDefs = new (double xPct, double w, double h)[]
        {
            (0.04, 34, rect.Height * 0.65),
            (0.18, 28, rect.Height * 0.48),
            (0.30, 42, rect.Height * 0.72),
            (0.48, 36, rect.Height * 0.55),
            (0.64, 40, rect.Height * 0.68),
            (0.78, 30, rect.Height * 0.50),
            (0.88, 38, rect.Height * 0.62)
        };

        foreach (var (xPct, bw, bh) in buildingDefs)
        {
            double bx = rect.Left + (rect.Width * xPct);
            double by = bBaseY - bh;
            dc.DrawRectangle(buildBrush, null, new Rect(bx, by, bw, bh));

            // Lit windows inside building
            if (!IsMiniMode)
            {
                for (double wy = by + 6; wy < bBaseY - 8; wy += 8)
                {
                    for (double wx = bx + 4; wx < bx + bw - 4; wx += 7)
                    {
                        var wb = ((int)(wx + wy) % 3 == 0) ? winBrush1 : winBrush2;
                        dc.DrawRectangle(wb, null, new Rect(wx, wy, 3, 4));
                    }
                }
            }
        }
    }

    private void DrawTokyoTower(DrawingContext dc, Rect rect)
    {
        double towerX = rect.Left + (rect.Width * 0.22);
        double towerBaseY = rect.Bottom;
        double towerH = rect.Height * 0.85;
        double towerTopY = towerBaseY - towerH;

        var towerBrush = new SolidColorBrush(Color.FromRgb(255, 95, 65));
        var towerGlow = new RadialGradientBrush(Color.FromArgb(80, 255, 110, 70), Color.FromArgb(0, 255, 110, 70));
        dc.DrawEllipse(towerGlow, null, new Point(towerX, towerBaseY - (towerH * 0.5)), 25, towerH * 0.55);

        // Tower Lattice Geometry
        var tGeom = new PathGeometry();
        var tf = new PathFigure { StartPoint = new Point(towerX - 12, towerBaseY) };
        tf.Segments.Add(new LineSegment(new Point(towerX - 4, towerBaseY - (towerH * 0.55)), true));
        tf.Segments.Add(new LineSegment(new Point(towerX - 1.5, towerTopY + 10), true));
        tf.Segments.Add(new LineSegment(new Point(towerX, towerTopY), true));
        tf.Segments.Add(new LineSegment(new Point(towerX + 1.5, towerTopY + 10), true));
        tf.Segments.Add(new LineSegment(new Point(towerX + 4, towerBaseY - (towerH * 0.55)), true));
        tf.Segments.Add(new LineSegment(new Point(towerX + 12, towerBaseY), true));
        tf.IsClosed = true;
        tGeom.Figures.Add(tf);
        dc.DrawGeometry(towerBrush, null, tGeom);

        // Blinking beacon light at tip
        if (_frameTick % 20 < 10)
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 240, 220)), null, new Point(towerX, towerTopY), 2.5, 2.5);
            var tipGlow = new RadialGradientBrush(Color.FromArgb(160, 255, 80, 80), Color.FromArgb(0, 255, 80, 80));
            dc.DrawEllipse(tipGlow, null, new Point(towerX, towerTopY), 8, 8);
        }
    }

    private void DrawPassingTokyoNeonSigns(DrawingContext dc, Rect rect)
    {
        var signs = new (string text, Color color, double yPct, double speed)[]
        {
            ("新宿", Color.FromRgb(80, 235, 255), 0.30, 0.8),   // Cyan Neon (Shinjuku)
            ("渋谷", Color.FromRgb(255, 90, 180), 0.45, 1.1),   // Magenta Neon (Shibuya)
            ("音楽", Color.FromRgb(255, 220, 80), 0.22, 0.6),   // Amber Gold (Music)
            ("カフェ", Color.FromRgb(100, 245, 160), 0.55, 0.9)  // Emerald Green (Cafe)
        };

        for (int i = 0; i < signs.Length; i++)
        {
            var (text, col, yPct, speed) = signs[i];
            double travelWidth = rect.Width + 120;
            double rawX = (rect.Right + 60) - (((_frameTick * speed * 1.8) + (i * 110)) % travelWidth);
            double sy = rect.Top + (rect.Height * yPct);

            if (rawX >= rect.Left - 40 && rawX <= rect.Right + 40)
            {
                // Neon glow background box
                var glowBrush = new RadialGradientBrush(Color.FromArgb(70, col.R, col.G, col.B), Color.FromArgb(0, col.R, col.G, col.B));
                dc.DrawEllipse(glowBrush, null, new Point(rawX + 14, sy + 8), 24, 16);

                var signBrush = new SolidColorBrush(col);
                var ft = CreateText(text, IsMiniMode ? 9 : 12, signBrush, FontWeights.Bold);
                dc.DrawText(ft, new Point(rawX, sy));
            }
        }
    }

    private void DrawMetroPassingBokeh(DrawingContext dc, Rect rect)
    {
        var rand = new Random(555);
        for (int i = 0; i < (IsMiniMode ? 6 : 14); i++)
        {
            double speed = 1.0 + (rand.NextDouble() * 1.5);
            double bx = (rect.Right + 30) - (((_frameTick * speed * 2.2) + (i * 38)) % (rect.Width + 60));
            double by = rect.Top + (rand.NextDouble() * (rect.Height * 0.75));
            double br = 4 + (rand.NextDouble() * 8);

            Color bColor = (i % 3 == 0)
                ? Color.FromArgb(45, 255, 180, 80)
                : (i % 3 == 1)
                    ? Color.FromArgb(45, 255, 100, 160)
                    : Color.FromArgb(45, 100, 220, 255);

            var bGlow = new RadialGradientBrush(bColor, Color.FromArgb(0, bColor.R, bColor.G, bColor.B));
            dc.DrawEllipse(bGlow, null, new Point(bx, by), br, br);
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
        // Stainless steel rail bar
        double railY = topY + (IsMiniMode ? 4 : 8);
        var railBrush = new LinearGradientBrush(Color.FromRgb(225, 230, 240), Color.FromRgb(150, 160, 175), new Point(0, 0), new Point(0, 1));
        dc.DrawRectangle(railBrush, null, new Rect(0, railY, w, IsMiniMode ? 2.5 : 4.0));

        // 4 Hanging leather straps with triangular handholds
        int strapCount = IsMiniMode ? 3 : 5;
        double strapSpacing = w / (strapCount + 1);

        var strapPen = new Pen(new SolidColorBrush(Color.FromRgb(215, 195, 165)), IsMiniMode ? 1.5 : 2.2);
        var ringBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var ringPen = new Pen(new SolidColorBrush(Color.FromRgb(180, 185, 195)), 1.2);

        for (int i = 1; i <= strapCount; i++)
        {
            double sx = i * strapSpacing;
            double sway = Math.Sin((_frameTick * 0.12) + (i * 0.9)) * (IsMiniMode ? 3.0 : 6.0);
            double strapLen = IsMiniMode ? 14 : 24;

            Point startPt = new Point(sx, railY + 2);
            Point endPt = new Point(sx + sway, railY + strapLen);

            dc.DrawLine(strapPen, startPt, endPt);

            // Triangular / Ring Handhold
            double ringR = IsMiniMode ? 4 : 6.5;
            dc.DrawEllipse(null, ringPen, new Point(endPt.X, endPt.Y + ringR), ringR, ringR);
        }
    }

    private void DrawMetroRouteTicker(DrawingContext dc, double w, double topY)
    {
        double tickerW = IsMiniMode ? 160 : 280;
        double tickerH = IsMiniMode ? 12 : 16;
        double tickerX = (w - tickerW) * 0.5;
        double tickerY = topY - tickerH - 2;

        if (tickerY < 1) tickerY = 1;

        // LED Display Body
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(15, 18, 22)), new Pen(new SolidColorBrush(Color.FromRgb(60, 65, 75)), 1), new Rect(tickerX, tickerY, tickerW, tickerH), 3, 3);

        string tickerText = (IsGoalReached || ProgressFraction >= 0.999)
            ? (IsMiniMode ? "🌟 TARGET REACHED! 🌟" : "🌟 ARRIVED: TARGET STATION • 100% FOCUS! 🌟")
            : IsRestPhase
                ? (IsMiniMode ? "🍵 METRO TEA BREAK" : "🍵 TOKYO METRO: LO-FI TEA & REST BREAK")
                : IsTracking
                    ? (IsMiniMode ? $"🟢 YAMANOTE • {(int)(ProgressFraction * 100)}%" : $"🟢 YAMANOTE LINE • NEXT: FOCUS STATION ──► {(int)(ProgressFraction * 100)}%")
                    : "🟢 YAMANOTE LINE • READY TO DEPART";

        var ledBrush = (IsGoalReached || ProgressFraction >= 0.999)
            ? new SolidColorBrush(Color.FromRgb(255, 225, 110))
            : IsRestPhase
                ? new SolidColorBrush(Color.FromRgb(130, 240, 180))
                : new SolidColorBrush(Color.FromRgb(110, 245, 140));

        var ft = CreateText(tickerText, IsMiniMode ? 7.5 : 9.5, ledBrush, FontWeights.Bold);
        dc.DrawText(ft, new Point(tickerX + ((tickerW - ft.Width) * 0.5), tickerY + ((tickerH - ft.Height) * 0.5)));
    }

    private void DrawMetroSeatBench(DrawingContext dc, double w, double seatY, double seatH)
    {
        // Emerald Green Velvet Japanese Train Seat Cushion
        var seatBrush = new LinearGradientBrush(
            Color.FromRgb(42, 138, 98),
            Color.FromRgb(24, 88, 62),
            new Point(0, 0),
            new Point(0, 1));
        var seatPen = new Pen(new SolidColorBrush(Color.FromRgb(20, 70, 48)), 1.2);
        dc.DrawRectangle(seatBrush, seatPen, new Rect(0, seatY, w, seatH));

        // Velvet texture bevel line
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(100, 100, 215, 160)), 1.5), new Point(0, seatY), new Point(w, seatY));
    }

    private void DrawMetroLoFiGirl(DrawingContext dc, double w, double seatY, double progress, bool isTracking, bool isMini)
    {
        double scale = isMini ? 0.65 : 1.0;
        double girlX = isMini ? (w * 0.68) : (w * 0.72);
        double headBob = Math.Sin(_frameTick * 0.22) * (2.0 * scale);
        double girlY = seatY - (52 * scale) + headBob;

        // 1. Cozy Pastel Lavender Hoodie Body
        var hoodieBrush = new LinearGradientBrush(
            Color.FromRgb(200, 180, 235),
            Color.FromRgb(165, 140, 205),
            new Point(0, 0),
            new Point(0, 1));
        var hoodiePen = new Pen(new SolidColorBrush(Color.FromRgb(140, 115, 180)), 1.2 * scale);

        // Body torso
        dc.DrawRoundedRectangle(hoodieBrush, hoodiePen, new Rect(girlX - (14 * scale), girlY + (22 * scale), 28 * scale, 32 * scale), 6 * scale, 6 * scale);

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
        double scale = isMini ? 0.65 : 1.0;
        double girlX = isMini ? (w * 0.68) : (w * 0.72);
        double girlY = seatY - (52 * scale);

        // Body
        var hoodieBrush = new SolidColorBrush(Color.FromRgb(215, 190, 245));
        dc.DrawRoundedRectangle(hoodieBrush, null, new Rect(girlX - (14 * scale), girlY + (22 * scale), 28 * scale, 32 * scale), 6 * scale, 6 * scale);

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
        dc.DrawEllipse(skinBrush, null, new Point(girlX - (16 * scale), armY), 3.5 * scale, 3.5 * scale);
        dc.DrawLine(new Pen(skinBrush, 2.2 * scale), new Point(girlX - (16 * scale), armY), new Point(girlX - (19 * scale), armY - (8 * scale)));
        dc.DrawLine(new Pen(skinBrush, 2.2 * scale), new Point(girlX - (16 * scale), armY), new Point(girlX - (13 * scale), armY - (8 * scale)));

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
        double scale = isMini ? 0.65 : 1.0;
        double girlX = isMini ? (w * 0.68) : (w * 0.72);
        double girlY = seatY - (48 * scale);

        // Body leaning on arm
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(190, 175, 225)), null, new Rect(girlX - (14 * scale), girlY + (20 * scale), 28 * scale, 30 * scale), 6 * scale, 6 * scale);

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

    private void RenderTamagotchiScene(DrawingContext dc, double w, double h)
    {
        // 1. Pastel Cyber Handheld Console (Soft Lilac -> Mint Frame)
        var shellBrush = new LinearGradientBrush(Color.FromRgb(235, 225, 255), Color.FromRgb(215, 240, 235), new Point(0, 0), new Point(1, 1));
        dc.DrawRoundedRectangle(shellBrush, new Pen(new SolidColorBrush(Color.FromRgb(180, 160, 220)), 2), new Rect(4, 4, w - 8, h - 8), 12, 12);

        // Console Screen Inset
        double screenMargin = IsMiniMode ? 8 : 14;
        var screenRect = new Rect(screenMargin, screenMargin, w - (screenMargin * 2), h - (screenMargin * 2));
        var screenBrush = new LinearGradientBrush(Color.FromRgb(20, 24, 42), Color.FromRgb(32, 38, 62), new Point(0, 0), new Point(0, 1));
        dc.DrawRoundedRectangle(screenBrush, new Pen(new SolidColorBrush(Color.FromRgb(80, 70, 110)), 1.5), screenRect, 6, 6);

        // Soft Pixel Grid / Starry Backdrop
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(30, 100, 220, 255)), 1);
        for (double gx = screenRect.Left + 15; gx < screenRect.Right; gx += 25) dc.DrawLine(gridPen, new Point(gx, screenRect.Top), new Point(gx, screenRect.Bottom));
        for (double gy = screenRect.Top + 15; gy < screenRect.Bottom; gy += 20) dc.DrawLine(gridPen, new Point(screenRect.Left, gy), new Point(screenRect.Right, gy));

        // Mini Heart Health Meter & XP
        if (!IsMiniMode)
        {
            for (int hr = 0; hr < 3; hr++)
            {
                DrawHeart(dc, screenRect.Left + 16 + (hr * 14), screenRect.Top + 14, 4.5, new SolidColorBrush(Color.FromRgb(255, 95, 150)));
            }
            var xpText = CreateText($"LV {Math.Max(1, (FocusXp / 100) + 1)} • XP: {FocusXp}", 10, new SolidColorBrush(Color.FromRgb(255, 225, 90)), FontWeights.Bold);
            dc.DrawText(xpText, new Point(screenRect.Right - xpText.Width - 12, screenRect.Top + 8));
        }

        // 2. Kawaii Bouncing Virtual Pet
        Point petCenter = new Point(w * 0.5, h * 0.52);
        DrawUltraKawaiiPet(dc, petCenter, IsMiniMode);
    }

    private void DrawUltraKawaiiPet(DrawingContext dc, Point center, bool isMini)
    {
        double scale = isMini ? 0.72 : 1.0;
        double bounce = Math.Sin(_frameTick * 0.35) * (IsTracking ? 5 : 2.5) * scale;
        double squash = Math.Cos(_frameTick * 0.35) * 1.5 * scale;
        Point p = new Point(center.X, center.Y + bounce);

        // Soft Glowing Blob Body (Mint / Pastel Emerald)
        var petBrush = new RadialGradientBrush(Color.FromRgb(140, 255, 205), Color.FromRgb(55, 205, 145));
        dc.DrawEllipse(petBrush, new Pen(new SolidColorBrush(Color.FromRgb(30, 150, 95)), 1.8 * scale), p, (22 * scale) + squash, (19 * scale) - squash);

        // Cute Swaying Head Sprout / Antenna 🌱
        double sproutSway = Math.Sin(_frameTick * 0.4) * 3 * scale;
        Point sproutBase = new Point(p.X, p.Y - (18 * scale));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(40, 160, 100)), 2 * scale), sproutBase, new Point(sproutBase.X + sproutSway, sproutBase.Y - (8 * scale)));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 225, 75)), null, new Point(sproutBase.X + sproutSway, sproutBase.Y - (8 * scale)), 3.5 * scale, 3.5 * scale);

        // Huge Glossy Anime Sparkle Eyes (◕‿◕)
        Point leftEye = new Point(p.X - (7 * scale), p.Y - (2 * scale));
        Point rightEye = new Point(p.X + (7 * scale), p.Y - (2 * scale));

        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(25, 30, 45)), null, leftEye, 3.2 * scale, 3.8 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(25, 30, 45)), null, rightEye, 3.2 * scale, 3.8 * scale);
        // Sparkle highlights inside eyes
        dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(leftEye.X - (1 * scale), leftEye.Y - (1.2 * scale)), 1.2 * scale, 1.2 * scale);
        dc.DrawEllipse(new SolidColorBrush(Colors.White), null, new Point(rightEye.X - (1 * scale), rightEye.Y - (1.2 * scale)), 1.2 * scale, 1.2 * scale);

        // Rosy Blushing Cheeks
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(190, 255, 120, 150)), null, new Point(p.X - (12 * scale), p.Y + (3 * scale)), 3.5 * scale, 2 * scale);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(190, 255, 120, 150)), null, new Point(p.X + (12 * scale), p.Y + (3 * scale)), 3.5 * scale, 2 * scale);

        // Smiling Mouth
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 110, 140)), null, new Point(p.X, p.Y + (3 * scale)), 2.5 * scale, 1.8 * scale);

        // Floating Star Candies & Snack Rewards ⭐
        if (IsTracking)
        {
            for (int sc = 0; sc < 2; sc++)
            {
                double starX = p.X + (sc == 0 ? -32 : 32) * scale;
                double starY = p.Y - (10 * scale) - (Math.Sin(_frameTick * 0.3 + sc) * 4 * scale);
                DrawSparkle(dc, starX, starY, 3.5 * scale, new SolidColorBrush(Color.FromRgb(255, 230, 80)));
            }
        }

        // Rest / Goal overlays
        if (IsRestPhase)
        {
            var ft = CreateText("💤 Snoozing & Recharging...", isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(180, 220, 255)), FontWeights.Bold);
            dc.DrawText(ft, new Point(p.X - (ft.Width * 0.5), p.Y + (24 * scale)));
        }
        else if (IsGoalReached)
        {
            // Golden Crown 👑
            DrawSparkle(dc, p.X, p.Y - (24 * scale), 6 * scale, new SolidColorBrush(Color.FromRgb(255, 215, 60)));
            var ft = CreateText("👑 FOCUS MASTER! ✨", isMini ? 10 : 12, new SolidColorBrush(Color.FromRgb(255, 225, 90)), FontWeights.Bold);
            dc.DrawText(ft, new Point(p.X - (ft.Width * 0.5), p.Y + (24 * scale)));
        }
    }

    #endregion
}
