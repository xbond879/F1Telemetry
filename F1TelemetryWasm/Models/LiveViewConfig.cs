using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace F1TelemetryWasm.Models;

public class LiveViewConfig
{
    public bool RunningUpdate { get; set; } = true;

    private const double AxisXOverflowLimit = 500;
    private const double AxisXZoomLimit = 200;
    private const double AxisXRunningFactor = 1.5;

    private readonly ObservableCollection<ObservablePoint> _fullPercentRange = new([
        new ObservablePoint(0, 0), new ObservablePoint(10000, 100)
    ]);

    private readonly LapData _lapData;

    public LiveViewConfig(LapData lapData, DataUpdateSyncRoot syncObject)
    {
        _syncObject = syncObject;
        
        _lapData = lapData;
        _lapData.PropertyChanged += OnLapDataPropertyChanged;
        
        ThrottleSeries =
        [
            MakeSeries(lapData.ThrottleValues, SKColors.Blue, 3),
            MakeSeries(lapData.ThrottleValuesBest, SKColors.DarkViolet),
            MakeSeries(_fullPercentRange, SKColors.Transparent)
        ];

        BrakeSeries =
        [
            MakeSeries(lapData.BrakeValues, SKColors.Red, 3),
            MakeSeries(lapData.BrakeValuesBest, SKColors.DarkViolet),
            MakeSeries(_fullPercentRange, SKColors.Transparent)
        ];

        SpeedSeries =
        [
            MakeSeries(lapData.SpeedValues, SKColors.LightGoldenrodYellow, 3),
            MakeSeries(lapData.SpeedValuesBest, SKColors.DarkViolet),
            MakeSeries(_fullPercentRange, SKColors.Transparent)
        ];

        _throttleAxe = MakeXAxis("Throttle");
        _brakeAxe = MakeXAxis("Brake");
        _speedAxe = MakeXAxis("Speed");
        ThrottleXAxes = [ _throttleAxe ];
        BrakeXAxes = [ _brakeAxe ];
        SpeedXAxes = [ _speedAxe ];
    }

    public void JumpToLastData(bool force = false)
    {
        lock (_syncObject)
        {
            if ((!RunningUpdate && !force) ||
                !_throttleAxe.MaxLimit.HasValue || !_throttleAxe.MinLimit.HasValue ||
                _throttleAxe.MaxLimit.Value < _throttleAxe.MinLimit.Value)
                return;

            var zoomWindowSize = _throttleAxe.MaxLimit.Value - _throttleAxe.MinLimit.Value;
            if (!(zoomWindowSize < _lapData.RaceDistance / 2))
                return;

            var lastValue = _lapData.ThrottleValues.Any() ? _lapData.ThrottleValues.Last().X : 0;
            if (!force && (lastValue < _throttleAxe.MinLimit.Value + zoomWindowSize / AxisXRunningFactor ||
                lastValue > _throttleAxe.MaxLimit.Value))
                return;

            var min = (double)(lastValue > zoomWindowSize / AxisXRunningFactor
                ? lastValue - (zoomWindowSize / AxisXRunningFactor)
                : 0)!;
            var max = min + zoomWindowSize;

            if (max - min < AxisXZoomLimit)
                max = min + AxisXZoomLimit;
            
            if (Equals(_speedAxe.MinLimit, min) && Equals(_speedAxe.MaxLimit, max))
                return;

            _throttleAxe.SetLimits(min, max);
            _brakeAxe.SetLimits(min, max);
            _speedAxe.SetLimits(min, max);
        }
    }


    void OnLapDataPropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(_lapData.CurrentLap):
            {
                var zoomWindowSize = _throttleAxe.MaxLimit.Value - _throttleAxe.MinLimit.Value;
                lock (_syncObject)
                {
                    _throttleAxe.SetLimits(0, zoomWindowSize);
                    _brakeAxe.SetLimits(0, zoomWindowSize);
                    _speedAxe.SetLimits(0, zoomWindowSize);
                }

                break;
            }

            case nameof(_lapData.CurrentDistance):
            {
                JumpToLastData();
                break;
            }

            case nameof(_lapData.FlashbacksCount):
            {
                JumpToLastData(true);
                break;
            }

        }
    }

    #region Series
    private static LineSeries<ObservablePoint> MakeSeries(IReadOnlyCollection<ObservablePoint> values, SKColor color, float width = 1)
    {
        return new LineSeries<ObservablePoint>()
        {
            Values = values,
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(color, width)
        };
    }

    public ISeries[] ThrottleSeries { get; }
    public ISeries[] BrakeSeries { get; }
    public ISeries[] SpeedSeries { get; }
    #endregion Series

    #region XAxes
    private Axis MakeXAxis(string name)
    {
        var result = new Axis()
        {
            Name = name,
            InLineNamePlacement = true,
            NamePaint = new SolidColorPaint(SKColors.White),
            LabelsPaint = new SolidColorPaint(SKColors.WhiteSmoke),
            TextSize = 10,
            MinLimit = 0,
            MaxLimit = 1000,
            Tag = _syncObject
        };
        result.PropertyChanged += OnAxisPropertyChanged;
        return result;
    }

    private readonly Axis _throttleAxe; 
    public Axis[] ThrottleXAxes { get; } 
    private readonly Axis _brakeAxe; 
    public Axis[] BrakeXAxes { get; } 
    private readonly Axis _speedAxe;
    private readonly DataUpdateSyncRoot _syncObject;
    public Axis[] SpeedXAxes { get; }

    private void OnAxisPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Axis axis)
            return;
        if (e.PropertyName is not ((nameof(axis.MaxLimit)) or (nameof(axis.MinLimit))))
            return;
        if (!axis.MinLimit.HasValue || !axis.MaxLimit.HasValue)
            return;

        var minXVisible = axis.MinLimit.Value;
        var maxXVisible = axis.MaxLimit.Value;
        if (minXVisible < 0)
            minXVisible = 0;
        if (maxXVisible - minXVisible < AxisXZoomLimit)
            maxXVisible = minXVisible + AxisXZoomLimit;
        if (maxXVisible > _lapData.RaceDistance + AxisXOverflowLimit)
            maxXVisible = _lapData.RaceDistance + AxisXOverflowLimit;

        lock (axis.Tag)
        {
            _throttleAxe.SetLimits(minXVisible, maxXVisible);
            _brakeAxe.SetLimits(minXVisible, maxXVisible);
            _speedAxe.SetLimits(minXVisible, maxXVisible);
        }
    }
    #endregion XAxes

    #region YAxes

    private static readonly SolidColorPaint SeparatorPaint = new(new SKColor(100, 100, 100, 70)) { StrokeThickness = 1 };
    public Axis[] PercentYAxes { get; } =
    [
        new()
        {
            MinLimit = 0,
            MaxLimit = 100,
            SeparatorsPaint = SeparatorPaint, 
        }
    ];

    public Axis[] SpeedYAxes { get; } =
    [
        new()
        {
            MinLimit = 0,
            MaxLimit = 370,
            SeparatorsPaint = SeparatorPaint, 
        }
    ];
    
    #endregion YAxes
}
