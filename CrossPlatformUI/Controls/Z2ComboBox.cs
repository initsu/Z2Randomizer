using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Flags;

namespace CrossPlatformUI.Controls;

/// <summary>
/// A real, templatable <see cref="ComboBox"/> bound to one <see cref="ConfigFlag"/>:
/// <c>&lt;z:Z2ComboBox Flag="GpLength" /&gt;</c>. The options, label, tooltip,
/// difficulty-only and gating behaviour all come from the flag's
/// <see cref="FlagEntry"/>, so a view no longer needs a class per flag.
/// </summary>
public sealed class Z2ComboBox : Z2SelectionBox
{
    public static readonly StyledProperty<ConfigFlag?> FlagProperty =
        AvaloniaProperty.Register<Z2ComboBox, ConfigFlag?>(nameof(Flag));

    private FlagBinding? _binding;
    private List<EnumDescription>? _items;

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FlagProperty)
        {
            ApplyFlag();
            return;
        }

        if (change.Property == SelectedItemProperty)
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
    /// Takes the flag's options and presentation from the registry. The flag is
    /// normally set while the XAML is parsed, i.e. before the control is attached,
    /// so the initial bind is left to <c>OnAttachedToVisualTree</c>.
    /// </summary>
    private void ApplyFlag()
    {
        Binding.SetEntry(Flag);
        if (Binding.Entry is not { } entry)
            return;

        // The option list is read once per control: some Enums properties compute a
        // fresh list on every access, and SelectedItem is matched against these
        // exact instances.
        _items = entry.Items?.Invoke().ToList();
        ItemsSource = _items;

        // Options may carry a right-aligned detail column; without one the default
        // template already renders EnumDescription.ToString(), i.e. the description.
        ItemTemplate = entry.HasInfoColumn ? BuildItemTemplate() : null;

        var toolTip = entry.ToolTip();
        if (!string.IsNullOrEmpty(toolTip))
            ToolTip.SetTip(this, toolTip);

        Label = entry.Label();

        if (VisualRoot is not null)
            Binding.Rebind(DataContext);
    }

    private static FuncDataTemplate<EnumDescription> BuildItemTemplate() =>
        new((data, _) =>
        {
            if (data is null)
            {
                // The closed ComboBox runs the template for a null selection while
                // PullFromConfig clears it.
                return null;
            }

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto, *"),
            };
            var description = new TextBlock { Text = data.Description };
            Grid.SetColumn(description, 0);
            grid.Children.Add(description);

            if (!string.IsNullOrEmpty(data.Info))
            {
                var info = new TextBlock
                {
                    Text = data.Info,
                    HorizontalAlignment = HorizontalAlignment.Right,
                };
                Grid.SetColumn(info, 1);
                grid.Children.Add(info);
            }

            return grid;
        });

    private void PullFromConfig()
    {
        if (Binding is not { Config: { } config, Entry: { } entry } || _items is null)
            return;

        var value = entry.ReadValue?.Invoke(config);
        SelectedItem = null;
        foreach (var item in _items)
        {
            if (Equals(item.Value, value))
            {
                SelectedItem = item;
                break;
            }
        }
    }

    private void PushToConfig()
    {
        if (Binding is not { Config: { } config, Entry: { } entry })
            return;

        if (SelectedItem is EnumDescription item)
            entry.WriteValue?.Invoke(config, item.Value);
    }
}
