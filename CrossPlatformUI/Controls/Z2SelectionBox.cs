using Avalonia;
using Avalonia.Controls;

namespace CrossPlatformUI.Controls;

/// <summary>An app-styled ComboBox for arbitrary items and item templates.</summary>
public class Z2SelectionBox : ComboBox
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<Z2SelectionBox, string?>(nameof(Label));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
}
