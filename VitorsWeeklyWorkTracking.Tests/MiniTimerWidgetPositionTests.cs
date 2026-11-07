using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using Xunit;

namespace VitorsWeeklyWorkTracking.Tests;

public class MiniTimerWidgetPositionTests
{
    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

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
            throw new Exception("STA test execution failed", exception);
        }
    }

    [Fact]
    public void MiniTimerWidget_WhenManuallyMoved_DoesNotSnapOnApplyCornerPosition()
    {
        RunInSta(() =>
        {
            var widget = new MiniTimerWidget();
            widget.Show(); 

            // Simulate manual move
            widget.Left = 500;
            widget.Top = 500;
            
            var field = typeof(MiniTimerWidget).GetField("_isManuallyMoved", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            field.SetValue(widget, true);

            // Call ApplyCornerPosition (e.g. from a resize or start/stop)
            widget.ApplyCornerPosition();

            // Should remain at 500, 500
            Assert.Equal(500, widget.Left);
            Assert.Equal(500, widget.Top);

            // Change corner explicitly
            widget.SetCorner(ScreenCorner.BottomLeft);
            
            // Should have moved from 500, 500 and reset the flag
            Assert.NotEqual(500, widget.Left);
            Assert.False((bool)field.GetValue(widget)!);

            widget.Close();
        });
    }

    [Fact]
    public void MiniTimerWidget_WhenResetManualMovementCalled_ResetsManualMoveFlag()
    {
        RunInSta(() =>
        {
            var widget = new MiniTimerWidget();
            widget.Show();

            var field = typeof(MiniTimerWidget).GetField("_isManuallyMoved", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            field.SetValue(widget, true);

            widget.ResetManualMovement();

            // Flag should be reset to false
            Assert.False((bool)field.GetValue(widget)!);

            widget.Close();
        });
    }

    [Fact]
    public void MiniTimerWidget_WhenSetMonitorCalled_ResetsManualMoveFlag()
    {
        RunInSta(() =>
        {
            var widget = new MiniTimerWidget();
            widget.Show();

            var field = typeof(MiniTimerWidget).GetField("_isManuallyMoved", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            field.SetValue(widget, true);

            widget.SetMonitor("Primary");

            // Flag should be reset to false
            Assert.False((bool)field.GetValue(widget)!);

            widget.Close();
        });
    }
}
