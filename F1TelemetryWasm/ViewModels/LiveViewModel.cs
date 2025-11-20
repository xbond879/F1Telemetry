using System;
using System.Collections.Generic;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using F1TelemetryWasm.Models;
using F1TelemetryWasm.Views;
using F1UdpParser;
using F1UdpParser.Models;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.Avalonia;
using Microsoft.Extensions.Logging;

namespace F1TelemetryWasm.ViewModels;

public partial class LiveViewModel(ILogger<LiveViewModel> logger, LiveViewConfig liveViewConfig, LapData lapData, DataUpdateSyncRoot syncObject)
    : ObservableObject, IF1TelemetryConsumer
{
    [ObservableProperty]
    private LiveViewConfig _liveViewConfig = liveViewConfig;
    
    [ObservableProperty]
    private LapData _lapData = lapData;

    [ObservableProperty]
    private IBrush _indicatorBrush = Brushes.Blue;

    private CartesianChart NewChart(double height, ISeries[] series, IEnumerable<ICartesianAxis> x,
        IEnumerable<ICartesianAxis> y, object sync)
    {
        var result = new CartesianChart()
        {
            Height = height,
            ZoomMode = ZoomAndPanMode.X,
            SyncContext = sync,
            TooltipPosition = TooltipPosition.Hidden,
            AnimationsSpeed = new TimeSpan(0, 0, 0, 0, 1),
            Series = series,
            XAxes = x,
            YAxes = y,
        };
        result.PointerPressed += (s, e) =>
        {
            LiveViewConfig.RunningUpdate = false;
        };
        result.PointerReleased += (s, e) =>
        {
            LiveViewConfig.RunningUpdate = true;
        };
        result.DoubleTapped += (s, e) =>
        {
            LiveViewConfig.JumpToLastData(true);
        };
        return result;
    }

    public void OnInitialized(LiveView liveView)
    {
        liveView.MainStack.Children.AddRange([
            NewChart(250, LiveViewConfig.ThrottleSeries, LiveViewConfig.ThrottleXAxes, LiveViewConfig.PercentYAxes, syncObject),
            NewChart(350, LiveViewConfig.SpeedSeries, LiveViewConfig.SpeedXAxes, LiveViewConfig.SpeedYAxes, syncObject),
            NewChart(250, LiveViewConfig.BrakeSeries, LiveViewConfig.BrakeXAxes, LiveViewConfig.PercentYAxes, syncObject)
        ]);
    }

    public void ReceivePacket(BasePacketData packet) => LapData.ApplyUpdate(packet, syncObject);
}