using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using CrossPlatformUI.Controls.Generated;
using Z2Randomizer.RandomizerCore.Flags;

namespace CrossPlatformUI.Controls;

/// <summary>
/// Builds the control for a StackPanel holding a Z2NumericUpDown plus its
/// caption, when it has one.
/// </summary>
internal static class Z2NumericPanel
{
    public static Control Build(ConfigFlag flag)
    {
        var box = new Z2NumericUpDown { Flag = flag };

        // Every numeric row is a StackPanel, so it picks up the same margins as
        // the rest of the app (App.axaml styles StackPanel and its children).
        // A bare box would sit flush against the row edge and be wider than the
        // other rows.
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        panel.Children.Add(box);

        // Show the optional group caption above the box, including for a pair
        // end displayed on its own in search results or the preset diff.
        var caption = FlagRegistry.Get(flag)?.Caption?.Invoke();
        if (!string.IsNullOrEmpty(caption))
        {
            panel.Children.Insert(0, new TextBlock
            {
                Text = caption,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2, 0, 0, 4),
                [!TextBlock.ForegroundProperty] = new DynamicResourceExtension("Z2NumericForeground"),
            });
        }

        return panel;
    }
}
