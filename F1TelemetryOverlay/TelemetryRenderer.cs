using GameOverlay.Drawing;
using GameOverlay.Windows;
using System.Management;
using F1TelemetryOverlay.Models;

namespace F1TelemetryOverlay;

public class TelemetryRenderer
{
    private readonly ActiveTelemetryData _data;
    private readonly GraphicsWindow _window;
    private SolidBrush _currentSpeedBrush;
    private SolidBrush _bestSpeedBrush;
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
            FPS = 60,
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
        _currentSpeedBrush = gfx.CreateSolidBrush(255, 0, 0);
        _bestSpeedBrush = gfx.CreateSolidBrush(128, 0, 255);
    }

    private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
    {
        _currentSpeedBrush.Dispose();
    }
    
    private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
    {
        var gfx = e.Graphics;

        var currentSpeedGeometry = gfx.CreateGeometry();
        var bestSpeedGeometry = gfx.CreateGeometry();

        lock (_data)
        {
            var shift = (int)_data.CurrentSpeedValues.Keys.LastOrDefault() - OverlayCurrentWidth;
            
            KeyValuePair<uint, uint>? pStart = null;
            foreach (var point in _data.CurrentSpeedValues.SkipWhile(p => p.Key < shift))
            {
                if (pStart.HasValue)
                {
                    currentSpeedGeometry.BeginFigure(new Line(
                        pStart.Value.Key - shift, OverlayHeight - pStart.Value.Value,
                        point.Key - shift, OverlayHeight - point.Value));
                    currentSpeedGeometry.EndFigure(false);
                }

                pStart = point;
            }


            if (_data.BestLapSpeedValues != null)
            {
                pStart = null;
                foreach (var point in _data.BestLapSpeedValues.SkipWhile(p => p.Key < shift && p.Key > OverlayWidth))
                {
                    if (pStart.HasValue)
                    {
                        bestSpeedGeometry.BeginFigure(new Line(
                            pStart.Value.Key - shift, OverlayHeight - pStart.Value.Value,
                            point.Key - shift, OverlayHeight - point.Value));
                        bestSpeedGeometry.EndFigure(false);
                    }

                    pStart = point;
                }
            }
        }

        currentSpeedGeometry.Close();
        bestSpeedGeometry.Close();

        gfx.ClearScene();
        gfx.DrawGeometry(currentSpeedGeometry, _currentSpeedBrush, 3.0f);
        gfx.DrawGeometry(bestSpeedGeometry, _bestSpeedBrush, 1.0f);
    }
}