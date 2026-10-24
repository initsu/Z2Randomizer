using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace CrossPlatformUI.Views;

public class FlexColumnsPanel : Panel
{
    public static readonly StyledProperty<double> MaxColumnWidthProperty =
        AvaloniaProperty.Register<FlexColumnsPanel, double>(nameof(MaxColumnWidth), 300);

    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<FlexColumnsPanel, double>(nameof(ColumnSpacing), 16);

    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<FlexColumnsPanel, double>(nameof(ItemSpacing), 0);

    static FlexColumnsPanel()
    {
        AffectsMeasure<FlexColumnsPanel>(
            MaxColumnWidthProperty, ColumnSpacingProperty, ItemSpacingProperty);
    }

    public double MaxColumnWidth { get => GetValue(MaxColumnWidthProperty); set => SetValue(MaxColumnWidthProperty, value); }
    public double ColumnSpacing { get => GetValue(ColumnSpacingProperty); set => SetValue(ColumnSpacingProperty, value); }
    public double ItemSpacing { get => GetValue(ItemSpacingProperty); set => SetValue(ItemSpacingProperty, value); }

    // --- Viewport tracking -------------------------------------------------

    private ScrollViewer? _scrollViewer;
    private double _viewportHeight = double.PositiveInfinity; // unknown => single column

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _scrollViewer = this.FindAncestorOfType<ScrollViewer>();
        if (_scrollViewer != null)
        {
            _scrollViewer.PropertyChanged += OnScrollViewerPropertyChanged;
            UpdateViewportHeight(_scrollViewer.Viewport.Height);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_scrollViewer != null)
            _scrollViewer.PropertyChanged -= OnScrollViewerPropertyChanged;
        _scrollViewer = null;
        _viewportHeight = double.PositiveInfinity;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.ViewportProperty)
            UpdateViewportHeight(((Size)e.NewValue!).Height);
    }

    private void UpdateViewportHeight(double h)
    {
        if (h <= 0) h = double.PositiveInfinity; // not laid out yet
        if (double.IsInfinity(h) != double.IsInfinity(_viewportHeight) ||
            Math.Abs(h - _viewportHeight) > 0.5)
        {
            _viewportHeight = h;
            InvalidateMeasure();
        }
    }

    // --- Layout ------------------------------------------------------------

    private readonly List<Control> _items = new();
    private readonly List<double> _heights = new();
    private readonly List<Rect> _rects = new();

    protected override Size MeasureOverride(Size available)
    {
        double cs = ColumnSpacing, rs = ItemSpacing, maxW = MaxColumnWidth;

        // How many columns could fit by width?
        int widthCols;
        double colW;
        if (double.IsInfinity(available.Width))
        {
            widthCols = 1;
            colW = maxW;
        }
        else
        {
            widthCols = Math.Max((int)Math.Floor((available.Width + cs) / (maxW + cs)), 1);
            colW = Math.Min(maxW, available.Width);
        }

        // 1. Measure each visible child once. Each child is one unbreakable unit.
        _items.Clear();
        _heights.Clear();
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            child.Measure(new Size(colW, double.PositiveInfinity));
            _items.Add(child);
            _heights.Add(child.DesiredSize.Height);
        }

        // 2. Pick the smallest column count that fits the viewport height.
        //    If none fits, use the maximum and let the ScrollViewer scroll.
        double limit = _viewportHeight - Margin.Top - Margin.Bottom;
        int cols = widthCols;
        if (widthCols > 1 && !double.IsInfinity(limit))
        {
            int c = 1;
            while (c < widthCols && !Fits(_heights, c, rs, limit)) c++;
            cols = c;
        }
        else if (double.IsInfinity(limit))
        {
            cols = 1;
        }

        // 3. Balance: smallest column height that fits everything in <= cols columns.
        double lo = 0, hi = 0;
        for (int i = 0; i < _heights.Count; i++)
        {
            lo = Math.Max(lo, _heights[i]);
            hi += _heights[i] + (i > 0 ? rs : 0);
        }
        while (hi - lo > 0.5)
        {
            double mid = (lo + hi) / 2;
            if (Fits(_heights, cols, rs, mid)) hi = mid; else lo = mid;
        }

        // 4. Place items into columns.
        _rects.Clear();
        double x = 0, y = 0, tallest = 0;
        int usedCols = 1;
        bool columnEmpty = true;

        for (int i = 0; i < _items.Count; i++)
        {
            double h = _heights[i];
            double add = (columnEmpty ? 0 : rs) + h;

            if (!columnEmpty && y + add > hi + 0.01 && usedCols < cols)
            {
                x += colW + cs;
                y = 0;
                usedCols++;
                columnEmpty = true;
            }
            if (!columnEmpty) y += rs;

            _rects.Add(new Rect(x, y, colW, h));
            y += h;
            columnEmpty = false;
            tallest = Math.Max(tallest, y);
        }

        return new Size(usedCols * colW + (usedCols - 1) * cs, tallest);
    }

    private static bool Fits(List<double> heights, int cols, double rs, double limit)
    {
        int used = 1;
        double cur = 0;
        bool empty = true;
        foreach (var h in heights)
        {
            double add = (empty ? 0 : rs) + h;
            if (!empty && cur + add > limit)
            {
                if (++used > cols) return false;
                cur = h;
            }
            else cur += add;
            empty = false;
        }
        return true;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        for (int i = 0; i < _items.Count; i++)
            _items[i].Arrange(_rects[i]);
        return finalSize;
    }
}
