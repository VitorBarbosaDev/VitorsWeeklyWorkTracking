using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VitorsWeeklyWorkTracking.Controls;

public class PieSliceItem
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string FormattedValue { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public Brush? Color { get; set; }
}

public partial class PieChartControl : UserControl
{
    private static readonly List<string> DefaultColors = new()
    {
        "#1E88E5", // Blue
        "#43A047", // Green
        "#FB8C00", // Orange
        "#8E24AA", // Purple
        "#E53935", // Red
        "#00ACC1", // Cyan
        "#FDD835", // Yellow
        "#D81B60", // Pink
        "#3949AB", // Indigo
        "#7CB342", // Light Green
        "#00897B", // Teal
        "#6D4C41", // Brown
        "#F4511E", // Deep Orange
        "#546E7A"  // Blue Grey
    };

    private List<PieSliceItem> _slices = new();

    public PieChartControl()
    {
        InitializeComponent();
    }

    public void SetData(IEnumerable<PieSliceItem> items, string? title = null)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            ChartTitleTextBlock.Text = title;
            ChartTitleTextBlock.Visibility = Visibility.Visible;
        }
        else
        {
            ChartTitleTextBlock.Visibility = Visibility.Collapsed;
        }

        var rawList = items?.Where(x => x.Value > 0).ToList() ?? new List<PieSliceItem>();
        var total = rawList.Sum(x => x.Value);

        _slices.Clear();
        int colorIdx = 0;

        foreach (var item in rawList)
        {
            var percentage = total > 0 ? (item.Value / total) * 100.0 : 0.0;
            var brush = item.Color ?? (Brush)new BrushConverter().ConvertFromString(DefaultColors[colorIdx % DefaultColors.Count])!;
            colorIdx++;

            _slices.Add(new PieSliceItem
            {
                Label = string.IsNullOrWhiteSpace(item.Label) ? "(Unassigned)" : item.Label,
                Value = item.Value,
                FormattedValue = item.FormattedValue,
                Percentage = percentage,
                Color = brush
            });
        }

        RenderChart();
        RenderLegend();
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderChart();
    }

    private void RenderChart()
    {
        ChartCanvas.Children.Clear();

        if (_slices.Count == 0)
        {
            EmptyMessageTextBlock.Visibility = Visibility.Visible;
            return;
        }

        EmptyMessageTextBlock.Visibility = Visibility.Collapsed;

        double width = ChartCanvas.ActualWidth;
        double height = ChartCanvas.ActualHeight;

        if (width <= 10 || height <= 10)
        {
            // Canvas not measured yet; defer until layout is ready
            return;
        }

        double centerX = width / 2.0;
        double centerY = height / 2.0;
        double radius = Math.Max(10, Math.Min(centerX, centerY) - 10);

        if (_slices.Count == 1)
        {
            // Single full slice (100%)
            var singleSlice = _slices[0];
            var circle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = singleSlice.Color,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                ToolTip = $"{singleSlice.Label}\n{singleSlice.FormattedValue} (100.0%)",
                Cursor = Cursors.Hand
            };

            Canvas.SetLeft(circle, centerX - radius);
            Canvas.SetTop(circle, centerY - radius);
            ChartCanvas.Children.Add(circle);
            return;
        }

        double startAngle = -90.0; // Start from top 12 o'clock

        foreach (var slice in _slices)
        {
            double sweepAngle = (slice.Percentage / 100.0) * 360.0;
            if (sweepAngle <= 0.001)
                continue;

            double endAngle = startAngle + sweepAngle;

            var path = CreatePieSlicePath(centerX, centerY, radius, startAngle, endAngle, slice);
            ChartCanvas.Children.Add(path);

            startAngle = endAngle;
        }
    }

    private static Path CreatePieSlicePath(double cx, double cy, double radius, double startAngleDeg, double endAngleDeg, PieSliceItem slice)
    {
        double startRad = startAngleDeg * Math.PI / 180.0;
        double endRad = endAngleDeg * Math.PI / 180.0;

        Point pStart = new Point(cx + radius * Math.Cos(startRad), cy + radius * Math.Sin(startRad));
        Point pEnd = new Point(cx + radius * Math.Cos(endRad), cy + radius * Math.Sin(endRad));

        bool isLargeArc = (endAngleDeg - startAngleDeg) > 180.0;

        var figure = new PathFigure
        {
            StartPoint = new Point(cx, cy),
            IsClosed = true
        };

        figure.Segments.Add(new LineSegment(pStart, true));
        figure.Segments.Add(new ArcSegment(pEnd, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise, true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        var path = new Path
        {
            Fill = slice.Color,
            Stroke = Brushes.White,
            StrokeThickness = 1.5,
            Data = geometry,
            ToolTip = $"{slice.Label}\n{slice.FormattedValue} ({slice.Percentage:F1}%)",
            Cursor = Cursors.Hand
        };

        // Subtle highlight on mouse hover
        path.MouseEnter += (s, e) =>
        {
            path.Stroke = Brushes.DimGray;
            path.StrokeThickness = 2.5;
        };

        path.MouseLeave += (s, e) =>
        {
            path.Stroke = Brushes.White;
            path.StrokeThickness = 1.5;
        };

        return path;
    }

    private void RenderLegend()
    {
        LegendPanel.Children.Clear();

        if (_slices.Count == 0)
            return;

        foreach (var slice in _slices)
        {
            var itemPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(6, 3, 6, 3),
                ToolTip = $"{slice.Label}: {slice.FormattedValue} ({slice.Percentage:F1}%)",
                Cursor = Cursors.Hand
            };

            var colorIndicator = new Border
            {
                Width = 12,
                Height = 12,
                Background = slice.Color,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var labelText = new TextBlock
            {
                Text = $"{slice.Label} ({slice.Percentage:F1}%)",
                FontSize = 11,
                Foreground = (Brush)new BrushConverter().ConvertFromString("#37474F")!,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 130,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            itemPanel.Children.Add(colorIndicator);
            itemPanel.Children.Add(labelText);

            LegendPanel.Children.Add(itemPanel);
        }
    }
}
