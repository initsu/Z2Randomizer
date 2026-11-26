using Avalonia;
using Avalonia.Controls;

namespace CrossPlatformUI.Controls;

/// <summary>A text editor with an app-owned theme and optional field label.</summary>
public sealed class Z2TextBox : TextBox
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<Z2TextBox, string?>(nameof(Label));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DataValidationErrors.HasErrorsProperty)
            PseudoClasses.Set(":invalid", DataValidationErrors.GetHasErrors(this));
    }
}
