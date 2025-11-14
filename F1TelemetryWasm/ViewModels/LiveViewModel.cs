using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using F1UdpParser;
using F1UdpParser.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace F1TelemetryWasm.ViewModels;

public class LiveViewModel : ObservableObject, IF1TelemetryConsumer
{
    public LiveViewModel()
    {
        ThrottleXAxes.First().PropertyChanged += OnAxisPropertyChanged;
        BrakeXAxes.First().PropertyChanged += OnAxisPropertyChanged;
        SpeedXAxes.First().PropertyChanged += OnAxisPropertyChanged;
    }

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
        if (maxXVisible > RaceDistance)
            maxXVisible = RaceDistance;
        lock (Sync)
        {
            ThrottleXAxes.First().MinLimit = minXVisible;
            ThrottleXAxes.First().MaxLimit = maxXVisible;
            BrakeXAxes.First().MinLimit = minXVisible;
            BrakeXAxes.First().MaxLimit = maxXVisible;
            SpeedXAxes.First().MinLimit = minXVisible;
            SpeedXAxes.First().MaxLimit = maxXVisible;
        }
    }

    public static ObservableCollection<ObservablePoint> ThrottleValues { get; set; } = [];
    public static ObservableCollection<ObservablePoint> ThrottleValuesBest { get; set; } = [];

    public static ObservableCollection<ObservablePoint> BrakeValues { get; set; } = [];
    public static ObservableCollection<ObservablePoint> BrakeValuesBest { get; set; } = [];

    public static ObservableCollection<ObservablePoint> SpeedValues { get; set; } = [];
    public static ObservableCollection<ObservablePoint> SpeedValuesBest { get; set; } = [];

    public object Sync { get; } = new();

    public double RaceDistance { get; set; } = 7000;

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
            Stroke = new SolidColorPaint(SKColors.Purple)
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
            Stroke = new SolidColorPaint(SKColors.MediumPurple)
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
            LabelsPaint = new SolidColorPaint(SKColors.Blue),
            TextSize = 8,
            MinLimit = 0,
            MaxLimit = 1000,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { StrokeThickness = 1 }
        }
    ];

    public Axis[] BrakeXAxes { get; } =
    [
        new()
        {
            Name = "Brake",
            InLineNamePlacement = true,
            NamePaint = new SolidColorPaint(SKColors.White),
            LabelsPaint = new SolidColorPaint(SKColors.Blue),
            TextSize = 8,
            MinLimit = 0,
            MaxLimit = 1000,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { StrokeThickness = 1 }
        }
    ];

    public Axis[] SpeedXAxes { get; } =
    [
        new()
        {
            Name = "Speed",
            InLineNamePlacement = true,
            NamePaint = new SolidColorPaint(SKColors.White),
            LabelsPaint = new SolidColorPaint(SKColors.Blue),
            TextSize = 8,
            MinLimit = 0,
            MaxLimit = 1000,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { StrokeThickness = 1 }
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
    public void ReceivePacket(BasePacketData packet)
    {
        switch (packet)
        {
            case PacketCarTelemetryData carTelemetryData:
                if (_currentDistance < 0
                    || (_currentDistance > _lastDistance && _currentDistance - _lastDistance < 3))
                    return;

                lock (Sync)
                {
                    ThrottleValues.Add(new ObservablePoint(_currentDistance, carTelemetryData.Throttle * 100));
                    BrakeValues.Add(new ObservablePoint(_currentDistance, carTelemetryData.Brake * 100));
                    SpeedValues.Add(new ObservablePoint(_currentDistance, carTelemetryData.Speed));
                }

                _lastDistance = _currentDistance; 

                break;
            case PacketLapData lapData:
                if (lapData.DriverStatus == 0 || lapData.LapDistance < 0)
                    return;
                if (_currentDistance >= lapData.LapDistance)
                {
                    lock (Sync)
                    {
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

                        Console.WriteLine(ThrottleValues.Count);
                        ThrottleValues.Clear();
                        BrakeValues.Clear();
                        SpeedValues.Clear();
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