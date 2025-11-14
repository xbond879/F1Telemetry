using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using F1TelemetryWasm.ViewModels;
using LiveChartsCore.SkiaSharpView;

namespace F1TelemetryWasm.Views;

public partial class LiveView : UserControl
{
    public LiveView()
    {
        InitializeComponent();
    }

    private void BaseXamlAxis_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Axis axis)
            return;
        if (e.PropertyName is not ((nameof(axis.MaxLimit)) or (nameof(axis.MinLimit))))
            return;

        // at this point the axis limits changed 
        // the user is using the zooming or panning features 
        // or the range was set explicitly in code 
        var minXVisible = axis.MinLimit; 
        var maxXVisible = axis.MaxLimit;
        if (minXVisible < 0)
            minXVisible = 0;
        if (maxXVisible > (DataContext as LiveViewModel).RaceDistance)
            maxXVisible = (DataContext as LiveViewModel).RaceDistance;
        lock ((DataContext as LiveViewModel).Sync)
        {
            ThrottleChart.XAxes.First().MinLimit = minXVisible;
            ThrottleChart.XAxes.First().MaxLimit = maxXVisible;
            BrakeChart.XAxes.First().MinLimit = minXVisible;
            BrakeChart.XAxes.First().MaxLimit = maxXVisible;
        }
    }
}