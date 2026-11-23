using System;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace CrossPlatformUI.Views;

public partial class RoomDiagnosticsView : UserControl
{
    private const double VerticalClearance = 64;

    public static IValueConverter DialogHeightLimit { get; } = new FuncValueConverter<double, double>(
        availableHeight => Math.Max(0, availableHeight - 2 * VerticalClearance));

    public RoomDiagnosticsView() => InitializeComponent();
}
