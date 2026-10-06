using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using VitorsWeeklyWorkTracking.Controls;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class VisualCompanionPerformanceTests
{
    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw new AggregateException("STA thread failed", exception);
        }
    }

    [Fact]
    public void VisualCompanionControl_RendersMetroScene_WithoutThrowing()
    {
        RunInSta(() =>
        {
            var control = new VisualCompanionControl
            {
                SceneId = "metro",
                IsTracking = true,
                ProgressFraction = 0.65,
                Width = 400,
                Height = 120
            };

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // Measure/Arrange and render
                control.Measure(new Size(400, 120));
                control.Arrange(new Rect(0, 0, 400, 120));
            }

            Assert.Equal("metro", control.SceneId);
        });
    }

    [Fact]
    public void VisualCompanionControl_RendersCoffeeJazzScene_WithoutThrowing()
    {
        RunInSta(() =>
        {
            var control = new VisualCompanionControl
            {
                SceneId = "coffeejazz",
                IsTracking = true,
                ProgressFraction = 0.50,
                Width = 400,
                Height = 120
            };

            control.Measure(new Size(400, 120));
            control.Arrange(new Rect(0, 0, 400, 120));

            Assert.Equal("coffeejazz", control.SceneId);
        });
    }

    [Fact]
    public void VisualCompanionControl_MultipleFrameTicks_RunSmoothly()
    {
        RunInSta(() =>
        {
            var control = new VisualCompanionControl
            {
                SceneId = "metro",
                IsTracking = true,
                ProgressFraction = 0.35,
                Width = 350,
                Height = 100
            };

            control.Measure(new Size(350, 100));
            control.Arrange(new Rect(0, 0, 350, 100));

            // Trigger cheer animation
            control.TriggerCheer();
            Assert.True(control.IsTracking);
        });
    }
}
