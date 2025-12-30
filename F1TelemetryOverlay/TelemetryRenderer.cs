using GameOverlay.Drawing;
using GameOverlay.Windows;
using System.Management;
using System.Numerics;
using F1TelemetryOverlay.Models;

namespace F1TelemetryOverlay;

public class TelemetryRenderer
{
    private readonly ActiveTelemetryData _data;
    private readonly GraphicsWindow _window;
    private SolidBrush _currentSpeedBrush;
    private SolidBrush _bestSpeedBrush;
    private SolidBrush _currentThrottleBrush;
    private SolidBrush _bestThrottleBrush;
    private SolidBrush _currentBrakeBrush;
    private SolidBrush _bestBrakeBrush;
    private const int OverlayWidth = 1200;
    private const int OverlayCurrentWidth = OverlayWidth / 2;
    private const int OverlayHeight = 600;

    public TelemetryRenderer(ActiveTelemetryData data)
    {
        _data = data;
        var gfx = new Graphics()
        {
            MeasureFPS = true,
            PerPrimitiveAntiAliasing = false,
            TextAntiAliasing = false
        };

        uint width = 0, height = 0;
        ManagementObjectSearcher mydisplayResolution = new ManagementObjectSearcher("SELECT CurrentHorizontalResolution, CurrentVerticalResolution FROM Win32_VideoController");
        foreach (var record in mydisplayResolution.Get())
        {
            Console.WriteLine($"Found display resolution:  {record["CurrentHorizontalResolution"]}x{record["CurrentVerticalResolution"]}");
            
            width = (uint)record["CurrentHorizontalResolution"];
            height = (uint)record["CurrentVerticalResolution"];
        }
        
        _window = new GraphicsWindow((int)(width - OverlayWidth) / 2, (int)(height - OverlayHeight) / 2, OverlayWidth, OverlayHeight, gfx)
        {
            FPS = 30,
            IsTopmost = true,
            IsVisible = true
        };

        _window.DrawGraphics += _window_DrawGraphics;
        _window.SetupGraphics += _window_SetupGraphics;
        _window.DestroyGraphics += _window_DestroyGraphics;
        
        _window.Create();
    }

    private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
    {
        var gfx = e.Graphics;
        _currentSpeedBrush = gfx.CreateSolidBrush(0, 0, 255);
        _bestSpeedBrush = gfx.CreateSolidBrush(128, 0, 255);
        _currentThrottleBrush = gfx.CreateSolidBrush(0, 255, 0);
        _bestThrottleBrush = gfx.CreateSolidBrush(128, 0, 255);
        _currentBrakeBrush = gfx.CreateSolidBrush(255, 0, 0);
        _bestBrakeBrush = gfx.CreateSolidBrush(128, 0, 255);
    }

    private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
    {
        _currentSpeedBrush.Dispose();
        _bestSpeedBrush.Dispose();
        _currentThrottleBrush.Dispose();
        _bestThrottleBrush.Dispose();
        _currentBrakeBrush.Dispose();
        _bestBrakeBrush.Dispose();
    }
    
    private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
    {
        var gfx = e.Graphics;

        using var currentSpeedGeometry = gfx.CreateGeometry();
        using var bestSpeedGeometry = gfx.CreateGeometry();
        using var currentThrottleGeometry = gfx.CreateGeometry();
        using var bestThrottleGeometry = gfx.CreateGeometry();
        using var currentBrakeGeometry = gfx.CreateGeometry();
        using var bestBrakeGeometry = gfx.CreateGeometry();

        void DrawChart<T>(int shift, IEnumerable<KeyValuePair<uint, T>> values, Geometry gfx, Func<T, int> valueConverter) where T : INumber<T>
        {
            KeyValuePair<uint, T>? pStart = null;
            foreach (var point in values.SkipWhile(p => p.Key < shift))
            {
                if (pStart.HasValue)
                {
                    gfx.BeginFigure(new Line(
                        pStart.Value.Key - shift, OverlayHeight - valueConverter(pStart.Value.Value),
                        point.Key - shift, OverlayHeight - valueConverter(point.Value)));
                    gfx.EndFigure(false);
                }

                pStart = point;
            }
        }

        lock (_data)
        {
            var shift = (int)_data.CurrentSpeedValues.Keys.LastOrDefault() - OverlayCurrentWidth;
            
            DrawChart(shift, _data.CurrentSpeedValues, currentSpeedGeometry, value => (int)value);
            DrawChart(shift, _data.CurrentThrottleValues, currentThrottleGeometry, value => (int)(value * 100));
            DrawChart(shift, _data.CurrentBrakeValues, currentBrakeGeometry, value => (int)(value * 100));
            
            if (_data.BestLapSpeedValues != null)
            {
                DrawChart(shift, _data.BestLapSpeedValues.SkipWhile(p => p.Key > OverlayWidth), bestSpeedGeometry, value => (int)value);
                DrawChart(shift, _data.BestLapThrottleValues.SkipWhile(p => p.Key > OverlayWidth), bestThrottleGeometry, value => (int)(value * 100));
                DrawChart(shift, _data.BestLapBrakeValues.SkipWhile(p => p.Key > OverlayWidth), bestBrakeGeometry, value => (int)(value * 100));
            }
        }

        currentSpeedGeometry.Close();
        bestSpeedGeometry.Close();
        currentThrottleGeometry.Close();
        bestThrottleGeometry.Close();
        currentBrakeGeometry.Close();
        bestBrakeGeometry.Close();

        gfx.ClearScene();
        gfx.DrawGeometry(currentSpeedGeometry, _currentSpeedBrush, 3.0f);
        gfx.DrawGeometry(bestSpeedGeometry, _bestSpeedBrush, 1.0f);
        gfx.DrawGeometry(currentThrottleGeometry, _currentThrottleBrush, 3.0f);
        gfx.DrawGeometry(bestThrottleGeometry, _bestThrottleBrush, 1.0f);
        gfx.DrawGeometry(currentBrakeGeometry, _currentBrakeBrush, 3.0f);
        gfx.DrawGeometry(bestBrakeGeometry, _bestBrakeBrush, 1.0f);
    }
}