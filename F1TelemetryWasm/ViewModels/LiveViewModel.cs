using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using F1UdpParser;
using F1UdpParser.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace F1TelemetryWasm.ViewModels;

public partial class LiveViewModel : ObservableObject, IF1TelemetryConsumer
{
    private readonly ILogger<LiveViewModel> _logger;

    public LiveViewModel(ILogger<LiveViewModel> logger)
    {
        _logger = logger;
        ThrottleXAxes.First().PropertyChanged += OnAxisPropertyChanged;
        BrakeXAxes.First().PropertyChanged += OnAxisPropertyChanged;
        SpeedXAxes.First().PropertyChanged += OnAxisPropertyChanged;
    }

    private const double AxisXOverflowLimit = 500;
    private const double AxisXRunningAheadSize = 50;
    private const double AxisXRunningAheadThreshold = 30;
    private const double ValuesDistanceThreshold = 0;
    private const double LapCompleteDistanceTrashold = 100;
    private void OnAxisPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Axis axis)
            return;
        if (e.PropertyName is not ((nameof(axis.MaxLimit)) or (nameof(axis.MinLimit))))
            return;

        var minXVisible = axis.MinLimit; 
        var maxXVisible = axis.MaxLimit;
        if (minXVisible < 0)
            minXVisible = 0;
        if (maxXVisible > RaceDistance + AxisXOverflowLimit)
            maxXVisible = RaceDistance + AxisXOverflowLimit;

        if (_activeXZoom.HasValue &&
            (Math.Abs(_activeXZoom.Value - (maxXVisible.Value - minXVisible.Value)) < AxisXRunningAheadThreshold)
            || maxXVisible - minXVisible < 100)
        {
            minXVisible = maxXVisible - _activeXZoom;
        }

        lock (Sync)
        {
            ThrottleXAxes.First().SetLimits(minXVisible.Value, maxXVisible.Value);
            BrakeXAxes.First().SetLimits(minXVisible.Value, maxXVisible.Value);
            SpeedXAxes.First().SetLimits(minXVisible.Value, maxXVisible.Value);
            
            if (_activeXZoom.HasValue && Math.Abs(_activeXZoom.Value - maxXVisible.Value + minXVisible.Value) > AxisXRunningAheadThreshold)
            {
                _activeXZoom = maxXVisible - minXVisible;
            }
            else
            {
                if (!_activeXZoom.HasValue)
                {
                    _activeXZoom = maxXVisible - minXVisible;
                }
            }
        }
    }

    public object Sync { get; } = new();
    public double RaceDistance { get; set; } = 7000;
    private double? _activeXZoom = null;

    public static ObservableCollection<ObservablePoint> ThrottleValues { get; set; } = [];
    public static ObservableCollection<ObservablePoint> ThrottleValuesBest { get; set; } = [];

    public static ObservableCollection<ObservablePoint> BrakeValues { get; set; } = [];
    public static ObservableCollection<ObservablePoint> BrakeValuesBest { get; set; } = [];

    public static ObservableCollection<ObservablePoint> SpeedValues { get; set; } = [];
    public static ObservableCollection<ObservablePoint> SpeedValuesBest { get; set; } = [];

    public ISeries[] ThrottleSeries { get; } =
    [
        new LineSeries<ObservablePoint>()
        {
            Values = ThrottleValues,
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(SKColors.Blue, 3) { StrokeThickness = 3},
        },
        new LineSeries<ObservablePoint>()
        {
            Values = ThrottleValuesBest,
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(SKColors.DarkViolet)
        },
        new LineSeries<ObservablePoint>()
        {
            Values = new ObservableCollection<ObservablePoint>([
                new ObservablePoint(0, 0), new ObservablePoint(10000, 100)
            ]),
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(SKColors.Transparent)
        }
    ];

    public ISeries[] BrakeSeries { get; } =
    [
        new LineSeries<ObservablePoint>()
        {
            Values = BrakeValues,
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(SKColors.Red, 2)
        },
        new LineSeries<ObservablePoint>
        {
            Values = BrakeValuesBest,
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(SKColors.DarkViolet)
        },
        new LineSeries<ObservablePoint>()
        {
            Values = new ObservableCollection<ObservablePoint>([
                new ObservablePoint(0, 0), new ObservablePoint(10000, 100)
            ]),
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(SKColors.Transparent)
        }

    ];

    public ISeries[] SpeedSeries { get; } =
    [
        new LineSeries<ObservablePoint>()
        {
            Values = SpeedValues,
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(SKColors.LightGoldenrodYellow, 2)
        },
        new LineSeries<ObservablePoint>
        {
            Values = SpeedValuesBest,
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(SKColors.DarkViolet)
        },
        new LineSeries<ObservablePoint>()
        {
            Values = new ObservableCollection<ObservablePoint>([
                new ObservablePoint(0, 0), new ObservablePoint(10000, 350)
            ]),
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            Stroke = new SolidColorPaint(SKColors.Transparent)
        }

    ];

    public Axis[] ThrottleXAxes { get; } =
    [
        new()
        {
            Name = "Throttle",
            InLineNamePlacement = true,
            NamePaint = new SolidColorPaint(SKColors.White),
            LabelsPaint = new SolidColorPaint(SKColors.WhiteSmoke),
            TextSize = 10,
            MinLimit = 0,
            MaxLimit = 500
        }
    ];

    public Axis[] BrakeXAxes { get; } =
    [
        new()
        {
            Name = "Brake",
            InLineNamePlacement = true,
            NamePaint = new SolidColorPaint(SKColors.White),
            LabelsPaint = new SolidColorPaint(SKColors.WhiteSmoke),
            TextSize = 10,
            MinLimit = 0,
            MaxLimit = 500
        }
    ];

    public Axis[] SpeedXAxes { get; } =
    [
        new()
        {
            Name = "Speed",
            InLineNamePlacement = true,
            NamePaint = new SolidColorPaint(SKColors.White),
            LabelsPaint = new SolidColorPaint(SKColors.WhiteSmoke),
            TextSize = 8,
            MinLimit = 0,
            MaxLimit = 500
        }
    ];

    public Axis[] PercentYAxes { get; } =
    [
        new()
        {
            MinLimit = 0,
            MaxLimit = 101,
        }
    ];

    public Axis[] SpeedYAxes { get; } =
    [
        new()
        {
            MinLimit = 0,
            MaxLimit = 370,
        }
    ];

    private double _currentDistance = -1;
    private double _lastDistance = -1;
    [ObservableProperty]
    private uint _currentLap = 1;

    [ObservableProperty]
    private TimeSpan _bestLapTime;

    private uint _bestLapTimeInMsValue = int.MaxValue;
    private uint _bestLapTimeInMs
    {
        get => _bestLapTimeInMsValue;
        set
        {
            BestLapTime = new TimeSpan(0, 0, 0, 0, (int)value);
            _bestLapTimeInMsValue = value;
        }
    }

    public void ReceivePacket(BasePacketData packet)
    {
        switch (packet)
        {
            case PacketSessionData sessionData:
                RaceDistance = sessionData.TrackLength;
                break;
            
            case PacketCarTelemetryData carTelemetryData:
                if (_currentDistance < 0
                    || (_currentDistance > _lastDistance && _currentDistance - _lastDistance < ValuesDistanceThreshold))
                    return;

                lock (Sync)
                {
                    ThrottleValues.Add(new ObservablePoint(_currentDistance, carTelemetryData.Throttle * 100));
                    BrakeValues.Add(new ObservablePoint(_currentDistance, carTelemetryData.Brake * 100));
                    SpeedValues.Add(new ObservablePoint(_currentDistance, carTelemetryData.Speed));
                    if (ThrottleXAxes.First().MaxLimit + AxisXRunningAheadThreshold < _currentDistance + AxisXRunningAheadSize)
                    {
                        var diff = _currentDistance + AxisXRunningAheadSize - ThrottleXAxes.First().MaxLimit;
                        ThrottleXAxes.First().MaxLimit = _currentDistance + AxisXRunningAheadSize;
                        ThrottleXAxes.First().MinLimit += diff;
                    }
                }

                _lastDistance = _currentDistance; 
                break;
            case PacketLapData lapData:
                if (lapData.DriverStatus == 0 || lapData.LapDistance < 0)
                    return;
                if (CurrentLap != lapData.CurrentLapNum)
                {
                    lock (Sync)
                    {
                        CurrentLap = lapData.CurrentLapNum;
                        _logger.LogInformation($"Lap {lapData.CurrentLapNum} started.");

                        if (_activeXZoom.HasValue && _activeXZoom < RaceDistance / 2)
                        {
                            ThrottleXAxes.First().SetLimits(0, _activeXZoom.Value);
                            BrakeXAxes.First().SetLimits(0, _activeXZoom.Value);
                            SpeedXAxes.First().SetLimits(0, _activeXZoom.Value);
                        }
                        
                        if (lapData.LastLapTimeInMs < _bestLapTimeInMs
                            && ThrottleValues.Any()
                            && ThrottleValues.First().X < LapCompleteDistanceTrashold
                            && ThrottleValues.Last().X + LapCompleteDistanceTrashold > RaceDistance)
                        {
                            _logger.LogInformation("Last lap was the best! Saving...");
                            _bestLapTimeInMs = lapData.LastLapTimeInMs;

                            ThrottleValuesBest.Clear();
                            BrakeValuesBest.Clear();
                            SpeedValuesBest.Clear();

                            foreach (var point in ThrottleValues)
                            {
                                ThrottleValuesBest.Add(new ObservablePoint(point.X, point.Y));
                            }

                            foreach (var point in BrakeValues)
                            {
                                BrakeValuesBest.Add(new ObservablePoint(point.X, point.Y));
                            }

                            foreach (var point in SpeedValues)
                            {
                                SpeedValuesBest.Add(new ObservablePoint(point.X, point.Y));
                            }
                        }

                        ThrottleValues.Clear();
                        BrakeValues.Clear();
                        SpeedValues.Clear();
                    }
                }
                else if (_currentDistance > lapData.LapDistance)
                {
                    _logger.LogInformation($"Flashback from {_currentDistance} to {lapData.LapDistance}.");
                    lock (Sync)
                    {
                        var i = ThrottleValues.Count - 1;
                        while (i >= 0 && ThrottleValues[i].X > lapData.LapDistance)
                        {
                            ThrottleValues.RemoveAt(i);
                            BrakeValues.RemoveAt(i);
                            SpeedValues.RemoveAt(i);
                            i--;
                        }
                    }
                    var x = ThrottleXAxes.First();
                    var zoom = x.MaxLimit - x.MinLimit;
                    if (zoom < 0)
                        zoom = x.MinLimit - x.MaxLimit;
                    if (zoom < RaceDistance / 2)
                    {
                        var firstX = ThrottleValues.Any() ? ThrottleValues.Last().X : 0;
                        var min = (double)(firstX > zoom ? firstX + AxisXRunningAheadSize - zoom : 0);
                        var max = (double)(min + zoom)!;
                        ThrottleXAxes.First().SetLimits(min, max);
                        BrakeXAxes.First().SetLimits(min, max);
                        SpeedXAxes.First().SetLimits(min, max);
                    }
                }

                _currentDistance = lapData.LapDistance;
                if (RaceDistance < _currentDistance)
                {
                    RaceDistance = _currentDistance;
                }

                break;
        }

    }
}