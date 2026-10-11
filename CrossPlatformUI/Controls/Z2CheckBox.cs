using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using CrossPlatformUI.Controls.Generated;
using Z2Randomizer.RandomizerCore.Flags;

namespace CrossPlatformUI.Controls;

/// <summary>
/// A real, templatable <see cref="CheckBox"/> bound to one <see cref="ConfigFlag"/>:
/// <c>&lt;z:Z2CheckBox Flag="ShuffleSpellLocations" /&gt;</c>. The label, tooltip,
/// tri-state, difficulty-only and gating behaviour all come from the flag's
/// <see cref="FlagEntry"/>, so a view no longer needs a class per flag.
/// </summary>
public sealed class Z2CheckBox : CheckBox
{
    public static readonly StyledProperty<ConfigFlag?> FlagProperty =
        AvaloniaProperty.Register<Z2CheckBox, ConfigFlag?>(nameof(Flag));

    private FlagBinding? _binding;

    /// <summary>
    /// Created on first use rather than in the constructor: base constructors can
    /// raise property changes before this type's initializers have run.
    /// </summary>
    private FlagBinding Binding =>
        _binding ??= new FlagBinding(this, PullFromConfig, PushToConfig);

    /// <summary>The config flag this control is bound to.</summary>
    public ConfigFlag? Flag
    {
        get => GetValue(FlagProperty);
        set => SetValue(FlagProperty, value);
    }

    // Avalonia's type selectors (Selector="CheckBox", including the one
    // Material.Avalonia uses to supply the template) match on StyleKey, which
    // defaults to the *concrete* type. Without this override every CheckBox style
    // and the theme are skipped: the control still handles clicks and still shows
    // its Content, but has no template, so the box itself never draws.
    protected override Type StyleKeyOverride => typeof(CheckBox);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FlagProperty)
        {
            ApplyFlag();
            return;
        }

        if (change.Property == IsCheckedProperty)
            Binding.Push();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (VisualRoot is not null)
            Binding.Rebind(DataContext);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        Binding.Rebind(DataContext);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Binding.Unbind();

        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// Takes the flag's presentation from the registry. The flag is normally set
    /// while the XAML is parsed, i.e. before the control is attached, so the initial
    /// bind is left to <c>OnAttachedToVisualTree</c>.
    /// </summary>
    private void ApplyFlag()
    {
        Binding.SetEntry(Flag);
        if (Binding.Entry is not { } entry)
            return;

        // A wrapping TextBlock, not a bare string: Avalonia renders string content
        // with TextWrapping=NoWrap, so long labels get clipped at the panel edge
        // instead of wrapping. This is what the hand-written <LineBreak/>s in the
        // views were working around.
        Content = new TextBlock
        {
            Text = entry.Label(),
            TextWrapping = TextWrapping.Wrap,
        };

        // An empty resource means "no tooltip": Avalonia would otherwise show an
        // empty popup for every property that has no description yet.
        var toolTip = entry.ToolTip();
        if (!string.IsNullOrEmpty(toolTip))
            ToolTip.SetTip(this, toolTip);

        IsThreeState = entry.IsThreeState;

        if (VisualRoot is not null)
            Binding.Rebind(DataContext);
    }

    private void PullFromConfig()
    {
        if (Binding is { Config: { } config, Entry: { } entry })
            IsChecked = entry.Read?.Invoke(config);
    }

    private void PushToConfig()
    {
        if (Binding is { Config: { } config, Entry: { } entry })
            entry.Write?.Invoke(config, IsChecked);
    }
}
