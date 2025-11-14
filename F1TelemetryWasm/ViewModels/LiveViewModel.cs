using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using F1UdpParser;
using F1UdpParser.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace F1TelemetryWasm.ViewModels;

public class LiveViewModel : ObservableObject, IF1TelemetryConsumer
{
    ConcurrentQueue<BasePacketData> queue = new ConcurrentQueue<BasePacketData>();

    public LiveViewModel()
    {
        Task.Run(ProcessQueue);
    }

    public static ObservableCollection<ObservablePoint> ThrottleValues { get; set; } = [];
    public static ObservableCollection<ObservablePoint> ThrottleValuesBest { get; set; } = [];

    public static ObservableCollection<ObservablePoint> BrakeValues { get; set; } = [];
    public static ObservableCollection<ObservablePoint> BrakeValuesBest { get; set; } = [];

    public object Sync { get; } = new();

    public double RaceDistance { get; set; } = 7000;

    public ISeries[] ThrottleSeries { get; } =
    [
        new XamlLineSeries()
        {
            Values = ThrottleValues,
            SeriesName = "SeriesName",
            Name = "Name",
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            ShowDataLabels = false,
            Stroke = new SolidColorPaint(SKColors.Green, 2),
        },
        new XamlLineSeries()
        {
            Values = ThrottleValuesBest,
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            ShowDataLabels = false,
            Stroke = new SolidColorPaint(SKColors.Purple)
        },
        new XamlLineSeries()
        {
            Values = new ObservableCollection<ObservablePoint>([
                new ObservablePoint(0, 0), new ObservablePoint(10000, 0)
            ]),
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            ShowDataLabels = false,
            Stroke = new SolidColorPaint(SKColors.Transparent)
        }
    ];

    public ISeries[] BrakeSeries { get; } =
    [
        new XamlLineSeries()
        {
            Values = BrakeValues,
            Margin = new Thickness(10),
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            ShowDataLabels = false,
            Stroke = new SolidColorPaint(SKColors.Red, 2)
        },
        new XamlLineSeries()
        {
            Values = BrakeValuesBest,
            Margin = new Thickness(10),
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            ShowDataLabels = false,
            Stroke = new SolidColorPaint(SKColors.MediumPurple)
        },
        new XamlLineSeries()
        {
            Values = new ObservableCollection<ObservablePoint>([
                new ObservablePoint(0, 0), new ObservablePoint(10000, 0)
            ]),
            Fill = null,
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0,
            ShowDataLabels = false,
            Stroke = new SolidColorPaint(SKColors.Transparent)
        }
    ];

    private double _currentDistance = -1;
    private double _lastDistance = -1;

    public void ReceivePacket(BasePacketData packet)
    {
        queue.Enqueue(packet);
    }

    private async Task ProcessQueue()
    {
        while (true)
        {
            while (queue.TryDequeue(out var packet))
            {
                ProcessPacket(packet);
            }
        }
    }

    private void ProcessPacket(BasePacketData packet)
    {
        switch (packet)
        {
            case PacketCarTelemetryData carTelemetryData:
                if (_currentDistance < 0
                    /*|| (_currentDistance > _lastDistance && _currentDistance - _lastDistance < 5)*/)
                    return;

                lock (Sync)
                {
                    ThrottleValues.Add(new ObservablePoint(_currentDistance, carTelemetryData.Throttle * 100));
                    BrakeValues.Add(new ObservablePoint(_currentDistance, carTelemetryData.Brake * 100));
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

                        foreach (var point in ThrottleValues)
                        {
                            ThrottleValuesBest.Add(new ObservablePoint(point.X, point.Y));
                        }

                        foreach (var point in BrakeValues)
                        {
                            BrakeValuesBest.Add(new ObservablePoint(point.X, point.Y));
                        }

                        ThrottleValues.Clear();
                        BrakeValues.Clear();
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