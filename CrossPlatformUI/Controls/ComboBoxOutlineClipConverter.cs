using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CrossPlatformUI.Controls;

internal sealed class ComboBoxOutlineClipConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2 || values[0] is not Rect field || values[1] is not Rect label ||
            field.Width <= 0 || field.Height <= 0)
            return null;

        return new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new RectangleGeometry(new Rect(field.Size)),
            new RectangleGeometry(new Rect(14, 0, label.Width, 2)));
    }
}
