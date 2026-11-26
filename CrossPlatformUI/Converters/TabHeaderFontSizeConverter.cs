using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CrossPlatformUI.Converters;

/// <summary>Keeps the dense second row readable as the tab strip narrows.</summary>
public sealed class TabHeaderFontSizeConverter : IValueConverter
{
    public double ExpandedFontSize { get; set; }
    public double CompactFontSize { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double width && width >= ResponsiveLayout.CompactTabStripWidth ? ExpandedFontSize : CompactFontSize;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
