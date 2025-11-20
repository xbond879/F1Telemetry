using System;
using Avalonia.Controls;
using F1TelemetryWasm.ViewModels;

namespace F1TelemetryWasm.Views;

public partial class LiveView : UserControl
{
    public LiveView()
    {
        InitializeComponent();
    }

    private void StyledElement_OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is LiveViewModel viewModel)
            viewModel.OnInitialized(this);
    }
}