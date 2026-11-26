using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CrossPlatformUI.Converters;

/// <summary>Builds a tab's clipped shape and configurable outline.</summary>
public sealed class TabOutlineClipConverter : IValueConverter
{
    public double CornerCut { get; set; }
    // The top edge includes both corner diagonals.
    public bool DrawTopEdge { get; set; } = true;
    public bool DrawLeftEdge { get; set; }
    public bool DrawRightEdge { get; set; }
    public bool DrawBottomEdge { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Rect { Width: > 0, Height: > 0 } bounds)
            return null;

        var cut = Math.Clamp(CornerCut, 0, Math.Min(bounds.Width / 2, bounds.Height));
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            if (parameter is "Outline")
            {
                if (DrawTopEdge)
                {
                    context.BeginFigure(new Point(0.5, cut + 0.5), false);
                    context.LineTo(new Point(cut + 0.5, 0.5));
                    context.LineTo(new Point(bounds.Width - cut - 0.5, 0.5));
                    context.LineTo(new Point(bounds.Width - 0.5, cut + 0.5));
                    context.EndFigure(false);
                }
                if (DrawLeftEdge)
                {
                    context.BeginFigure(new Point(0.0, bounds.Height - 0.5), false);
                    context.LineTo(new Point(0.0, cut + 0.5));
                    context.EndFigure(false);
                }
                if (DrawRightEdge)
                {
                    context.BeginFigure(new Point(bounds.Width - 1.0, cut + 0.5), false);
                    context.LineTo(new Point(bounds.Width - 1.0, bounds.Height - 0.5));
                    context.EndFigure(false);
                }
                if (DrawBottomEdge)
                {
                    context.BeginFigure(new Point(bounds.Width - 0.5, bounds.Height - 0.5), false);
                    context.LineTo(new Point(0, bounds.Height - 0.5));
                    context.EndFigure(false);
                }
                return geometry;
            }

            context.BeginFigure(new Point(0, cut), true);
            context.LineTo(new Point(cut, 0));
            context.LineTo(new Point(bounds.Width - cut, 0));
            context.LineTo(new Point(bounds.Width, cut));
            context.LineTo(new Point(bounds.Width, bounds.Height));
            context.LineTo(new Point(0, bounds.Height));
            context.EndFigure(true);
        }
        return geometry;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
