# Working on the CrossPlatformUI

The Desktop and Browser apps share an Avalonia interface. This guide covers the
usual UI work: adding settings, choosing controls, changing the appearance, and
adding icons.

## Contents

1. [Project layout](#1-project-layout)
2. [How a flag becomes a control](#2-how-a-flag-becomes-a-control)
3. [ReactiveUI conventions](#3-reactiveui-conventions)
4. [Flag controls in detail](#4-flag-controls-in-detail)
5. [Enabling and disabling controls](#5-enabling-and-disabling-controls)
6. [Adding a flag, end to end](#6-adding-a-flag-end-to-end)
7. [The settings search box](#7-the-settings-search-box)
8. [Menus](#8-menus)
9. [Dialogs](#9-dialogs)
10. [Preset presentation](#10-preset-presentation)
11. [General controls](#11-general-controls)
12. [App shell](#12-app-shell)
13. [Text-editing menus and touch selection](#13-text-editing-menus-and-touch-selection)
14. [Managed file-picker fallback](#14-managed-file-picker-fallback)
15. [Adding a Material Design icon](#15-adding-a-material-design-icon)

## 1. Project layout

Most UI changes belong in `CrossPlatformUI/`. The two host projects provide
platform services; they use the same views and controls.

| Project | Purpose |
| --- | --- |
| `CrossPlatformUI/` | Shared views, controls, view models, styles, and resources. |
| `CrossPlatformUI.Desktop/` | Desktop startup and platform integration. |
| `CrossPlatformUI.Browser/` | WebAssembly startup and browser services. |
| `Desktop.Common/` | Shared desktop file and JavaScript services. |
| `CrossPlatformUI.SourceGenerator/` | Generates flag-control metadata and factories. |
| `CoreSourceGenerator/` | Generates configuration properties, flag serialization, and `ConfigFlag`. |

Within the shared project, `Views/` contains screens and `Views/Tabs/` contains
settings pages. Their view models are under `ViewModels/`. Reusable controls and
themes live in `Controls/`; labels and tooltips live in `Lang/Resources.resx`.
`Presets/`, `Services/`, `Converters/`, and `Behaviors/` hold their corresponding
UI features.

## 2. How a flag becomes a control

A flag control connects to a configuration property by name. Its label, tooltip,
options, and numeric bounds come from shared metadata, so a view usually needs
only the flag and its layout position:

```xml
<z:Z2CheckBox Flag="StartWithCandle" />
<z:Z2ComboBox Flag="GpLength" />
<z:Z2NumericUpDown Flag="StartingAttackLevel" />
```

These examples use `xmlns:z="clr-namespace:CrossPlatformUI.Controls"` on the
view's root element. The control's DataContext supplies the configuration through
`IFlagHost`, normally `MainViewModel`. A tab with its own view model can bind its
flag controls to `Main`.

Building the project generates a `FlagRegistry` entry for each supported property.
The controls read that metadata and synchronize with the live configuration.

| Property type | Control | Label and other metadata |
| --- | --- | --- |
| `bool` / `bool?` | `Z2CheckBox` | `Resources.<Name>` and `<Name>ToolTip`. |
| Enum | `Z2ComboBox` | The same resource keys, plus `Enums.<Name>List`. |
| `int` / `int?` with bounds | `Z2NumericUpDown` | `[Minimum]`, `[Maximum]`, and a label resource; pair captions are described below. |

The configuration attributes also describe how a setting appears:

| Attribute | Purpose |
| --- | --- |
| `[ConditionallyIncludeInFlags]` | Serializes the setting only when its `xIncluded()` predicate allows it. |
| `[IgnoreInFlags]` | Omits a setting from the flag string while still allowing a UI control. |
| `[IgnoreInFlagControls]` | Omits the standard control, for settings with a custom editor such as NES colors. |
| `[Minimum(n)]` / `[Maximum(n)]` | Defines the numeric editor's bounds. |
| `[UiNumericPair(nameof(partner))]` | Links the two ends of a Min/Max pair. |
| `[DifficultyOnly]` | Applies the difficulty-only appearance while the seed is shared. |
| `[TooltipResource("Key")]` | Uses a shared tooltip resource. |

These attributes go on the backing field in `RandomizerConfiguration`; attributes
forwarded to the generated property must support fields as an attribute target.

## 3. ReactiveUI conventions

View models use ReactiveUI with source-generated properties. A typical observable
property looks like this:

```csharp
using RUISG = ReactiveUI.SourceGenerators;

[RUISG.Reactive]
public partial string SearchText { get; set; }
```

The containing view-model class is `partial`. A property that needs side effects
can instead use a backing field and a setter that calls `RaisePropertyChanged()`.

Observable helpers come from `ReactiveUI.Primitives` and the project's
`SubscribeExtensions`. Importing `System.Reactive.Linq` introduces conflicting
extensions. The core's separate `ReactiveAttribute` is for configuration code;
UI view models use the `RUISG` alias above.

## 4. Flag controls in detail

### 4.1 The shared contract

The flag controls share a small API: `Flag` identifies the setting, while the
DataContext provides an `IFlagHost`. `MainViewModel` implements that interface,
and `SpriteCustomizationViewModel` forwards it to `Main`. A control without a
compatible DataContext has no configuration to edit.

The registry supplies labels and tooltips. `Classes` is useful for visual variants
of a shared control; `[DifficultyOnly]` adds its class automatically. Custom
controls such as the sprite picker and the experience-shuffle grouping have their
own bindings rather than a `Flag` property.

For a dynamically built editor, `FlagControlFactory.Create(ConfigFlag)` returns
the appropriate control, or `null` for a setting without a standard editor. This
is how search results and preset differences display the same editors as the tabs.

### 4.2 Checkboxes (`Z2CheckBox`)

A nullable boolean has three states. Checked and unchecked mean true and false;
the question mark means the randomizer will choose. Labels wrap to fit the page.

`Controls/Z2CheckBox.axaml` defines the checkbox appearance, including focus,
disabled, and difficulty-only states. Its colors come from `AppPalette.axaml`.
The ordinary CheckBox used for "Shuffle All Experience" shares the theme.

### 4.3 Combo boxes (`Z2ComboBox`)

`Z2ComboBox` displays the registry label across the top outline. Disabled flag
combos retain the label but hide the selected value and arrow.

For a list that is not a flag, such as character sprites, use `Z2SelectionBox`:

```xml
<z:Z2SelectionBox Label="Character Sprite"
                  ItemsSource="{Binding SpriteCustomizationViewModel.Options}"
                  SelectedItem="{Binding SpriteCustomizationViewModel.Sprite}" />
```

It shares the ComboBox styling and supports a custom `ItemTemplate`. Its selected
value remains visible when disabled. The templates are in `Controls/Z2ComboBox.axaml`.

#### 4.3.1 Required symbols

Each enum control uses `Resources.<Name>`, `Resources.<Name>ToolTip`, and
`Enums.<Name>List`. Missing symbols produce a build error naming the resource
or list to add.

#### 4.3.2 Shared tooltips (`[TooltipResource]`)

A tooltip normally uses `<Name>ToolTip`. Several related settings can share one
resource by putting `[TooltipResource("ClimateToolTip")]`, for example, on their
configuration fields.

#### 4.3.3 Two-column ComboBoxes

An enum description can include supplementary text after a semicolon:
`[Description("Long name;Extra info")]`. The dropdown displays that text in a
second column. Descriptions without it use the ordinary one-line layout.

#### 4.3.4 Why not derive the list from the enum type?

Some settings allow only part of an enum. The named lists in `EnumTypes.cs` are
where that filtering belongs:

| List | Filtering |
| --- | --- |
| `BossLifeOptionList` | Excludes `EnemyLifeOption.WIDE`. |
| `WestBiomeList`, `EastBiomeList`, `DmBiomeList`, `MazeBiomeList` | Offers biomes appropriate to the region. |
| The corresponding climate lists | Applies the regional climate choices. |

### 4.4 Numeric controls (`Z2NumericUpDown`)

Numeric editors use the configuration's `[Minimum]` and `[Maximum]` attributes.
The registry label appears underneath the field as its `Hint`. Clearing a nullable
value selects the random state. Numerics remain editable rather than being
automatically disabled by flag enablement rules.

For rows containing both numeric fields and a ComboBox, `Classes="AlignedFields"`
on their parent Grid aligns the outlines:

```xml
<Grid Classes="AlignedFields" ColumnDefinitions="72,72,122" ColumnSpacing="8">
    <z:Z2NumericUpDown Flag="StartingHeartContainersMin" Grid.Column="0" />
    <z:Z2NumericUpDown Flag="StartingHeartContainersMax" Grid.Column="1" />
    <z:Z2ComboBox Flag="MaxHeartContainers" Grid.Column="2" />
</Grid>
```

#### 4.4.1 Min/Max pairs

Both fields in a pair carry `[UiNumericPair(nameof(partner))]`. Changing either
end updates the other's allowed range; clearing an end restores the partner's
configured bound.

A pair has labels for each editor, a `<Name>Caption` for the group, and a shared
`<MinName>ToolTip`. `Z2NumericPanel` adds the group caption when an editor appears
on its own in search results or the preset diff.

### 4.5 Text entry

`Z2TextBox` is used for flags, seed, search, preset names, and output filenames.
`Label` sits in the outline; `PlaceholderText` appears while the field is empty.
`InnerLeftContent` and `InnerRightContent` accept icons or buttons, as in the search
field. It otherwise uses the standard TextBox bindings and editing behavior.

The theme in `Controls/Z2TextBox.axaml` provides focus and validation feedback.
The flag-string field receives its validity border from `ThemeHelper`, so an
invalid string stays visibly marked during editing.

### 4.6 Buttons

Ordinary Avalonia buttons use the theme in `Controls/Z2Button.axaml`. The default
is a primary-colored button. The existing variants are:

| Variant | Example |
| --- | --- |
| Flat | `<Button Classes="Flat" Content="Cancel" />` |
| Secondary surface | `<Button Classes="accent" Content="Other action" />` |
| Outline | `<Button Theme="{StaticResource Z2OutlineButton}" Content="Browse" />` |

Commands and default/cancel actions use Avalonia's Button behavior. Local size
and padding settings are available for toolbar actions and color swatches.

### 4.7 Semantic palette

Colors are configured in [AppPalette.axaml](../CrossPlatformUI/AppPalette.axaml).
The `Default` and `Dark` dictionaries hold theme-specific values; resources above
them apply to both themes. There are separate roles for control states,
validation, difficulty highlights, flag alerts, borders, and shadows.

For example, changing this resource changes the wiki-link color throughout the app:

```xml
<SolidColorBrush x:Key="Z2WikiLinkBrush" Color="#4557c4" />
```

Views and themes refer to the roles with `DynamicResource`, so a new UI color
usually means adding a brush here and referencing it from the control. Intrinsic
NES colors and multicolor icon artwork remain with their data; they are not part
of the interface theme.

## 5. Enabling and disabling controls

Most enablement comes from a configuration method named after the property:
`fooIncluded()` supplies the rule for `Foo`. The same predicate can describe
whether a setting belongs in the flag string and whether its editor is available.
The presence of the predicate enables this behavior; it also works for settings
that are not serialized flags.

### 5.1 The tri-state rule: `!= false`, never `== true`

For nullable settings, `null` means "choose randomly," so dependent options remain
available in that state. A predicate based on a nullable boolean therefore uses
`Config.X != false`. An existing `xIncluded()` method is usually the best way to
reuse the configuration's rule.

### 5.2 Rules the generator derives for you

The generator recognizes the PascalCase property and its camelCase predicate
without an extra UI attribute. Numerics are the exception: their editors are not
automatically disabled.

A rule based on view-model state can use a `<Name>EnabledObservable` instead. For
simple non-nullable UI properties, an ordinary `IsEnabled` binding also works.
Binding a nullable configuration boolean directly to `IsEnabled` would turn its
random state into false; use the predicate or observable for that case.

## 6. Adding a flag, end to end

1. Add the configuration field and its attributes in `RandomizerConfiguration.cs`.
   An integer needs bounds; a Min/Max pair needs partner attributes on both fields.
   A conditional flag also needs its `xIncluded()` predicate.
2. Add the label and tooltip in `CrossPlatformUI/Lang/Resources.resx`. Enum and
   boolean labels use `<Name>` and `<Name>ToolTip`; a numeric pair also has its
   group caption. [Section 4](#4-flag-controls-in-detail) describes the differences.
3. For an enum, add `<Name>List` to `RandomizerCore/EnumTypes.cs`, including any
   filtering specific to that setting.
4. Add the matching `z:Z2CheckBox`, `z:Z2ComboBox`, or `z:Z2NumericUpDown` to the
   relevant tab. The examples in [section 2](#2-how-a-flag-becomes-a-control) show
   the usual markup; normal Grid and margin attributes handle its placement.

## 7. The settings search box

Search opens from the box beside the top tab row. It matches the beginning of
words in labels, tooltips, and ComboBox options, with synonyms for terms such as
"gems" and "crystals." Multiple words narrow the results. Each result is the same
editor used on the settings page; a newly registered flag becomes searchable
automatically. Clearing the query returns to the previous tab.

The relevant files are
`Controls/FlagSearchIndex.cs`,
`ViewModels/Tabs/SearchViewModel.cs`, and
`Views/Tabs/SearchView.axaml`.

The tab themes are in `Controls/Z2Tabs.axaml`. Their `TabIdleOutline`,
`TabHoverOutline`, and `TabActiveOutline` resources control corner cuts and which
edges are drawn. `MultiRowTabsPanel` lays out the rows and search box.

At the breakpoint in `ResponsiveLayout.CompactTabStripWidth`, the tab font,
inputs, seed icon, and selected-tab decoration switch to a compact presentation.
Font sizes are configured in `Typography.axaml`; layout styles are in
`Views/RandomizerView.axaml`.

## 8. Menus

The gear menu in `RandomizerView.axaml` uses ordinary `MenuItem` elements with
command bindings. New actions can be added alongside the existing items, with an
icon from [section 15](#15-adding-a-material-design-icon) if useful.

`Controls/Z2Menu.axaml` supplies the menu, item, and separator themes.
`Z2MenuRootItem` is the gear-button presentation; its popup opens below the button,
while submenus open to the side. Avalonia handles keyboard navigation and dismissal.

## 9. Dialogs

The main view hosts generation progress and preset dialogs through
DialogHost.Avalonia. Each dialog has a view and view model; the corresponding
open flag on `MainViewModel` controls visibility. `CloseOnClickAway=False` means
clicking outside a dialog does not dismiss it.

For appearance changes, `Controls/Z2Dialogs.axaml` selects the dialog surface,
text, overlay, and border resources. DialogHost supplies the modal behavior and
focus handling.

## 10. Preset presentation

`Views/Tabs/PresetsView.axaml` contains the preset list and the expandable
"Compare To Current Flags" panel. The diff displays the normal flag editors next
to the preset's values, so changes apply to the live configuration.

`Controls/Z2Presets.axaml` provides the list selection, diff header, and expander
themes.

## 11. General controls

The shared themes also cover ordinary Avalonia controls:

| Controls | Theme file |
| --- | --- |
| `ScrollViewer`, `ScrollBar` | `Controls/Z2Scrolling.axaml` |
| `ContentControl`, `ItemsControl` | `Controls/Z2Content.axaml` |
| `HyperlinkButton` | `Controls/Z2Hyperlink.axaml` |

Scroll viewers support wheel and touch input, thumb dragging, and track paging.
For a link, `HyperlinkButton.NavigateUri` provides the destination and Avalonia
handles opening it. The `WikiLink` class applies the app's wiki-link appearance.

## 12. App shell

### Fonts

[Typography.axaml](../CrossPlatformUI/Typography.axaml) is the place to change
font families, weights, styles, and sizes. Settings are grouped by their visual
role rather than by the view where they happen to appear:

| Role | Resource prefix |
| --- | --- |
| App default | `Z2` (`Z2FontFamily`, `Z2FontSize`, etc.) |
| Section header | `Z2Section` |
| Section caption | `Z2SectionCaption` |
| ComboBox heading | `Z2ComboBoxHeading` |
| Hyperlink | `Z2Hyperlink` |
| Label below a numeric field | `Z2NumericLabel` |
| Button | `Z2Button` |
| Tab header | `Z2Tab` |

For example, a button's family, style, weight, and size are configured together:

```xml
<FontFamily x:Key="Z2ButtonFontFamily">$Default</FontFamily>
<FontStyle x:Key="Z2ButtonFontStyle">Normal</FontStyle>
<FontWeight x:Key="Z2ButtonFontWeight">Normal</FontWeight>
<x:Double x:Key="Z2ButtonFontSize">14</x:Double>
```

`$Default` selects the host's default font. A bundled font uses an Avalonia
resource URI, such as `avares://CrossPlatformUI/Assets/Fonts/Roboto#Roboto`.
The file also contains heading sizes, compact input sizes, and the wide/compact
tab sizes. This keeps font experiments in one file.
Tab headers share their family, style, and weight across both sizes;
`Z2TabFontSize` and `Z2CompactTabFontSize` set the wide and compact sizes.

### Theme and window appearance

`ThemeHelper` selects Light, Dark, or the system theme. Most appearance changes
belong in [the palette](#47-semantic-palette), `Typography.axaml`, or a control's
theme under `Controls/`.

`Z2Application.axaml` provides the window and popup surfaces, tooltips, and routed
content presentation. `Z2WindowDecorations.axaml` provides app-drawn title bars
and caption buttons where the platform uses them. Native OS-drawn decorations
remain platform-owned.

## 13. Text-editing menus and touch selection

Text fields and numeric editors offer Cut, Copy, Paste, and Select All through
the shared themes in `Controls/Z2TextEditing.axaml`. Desktop menus show shortcut
hints; touch input uses a compact horizontal menu and draggable selection handles.
The operations use Avalonia's TextBox behavior, including read-only restrictions
and undo.

The global TextBox style in `App.axaml` applies the menu to both `Z2TextBox` and
the editor inside a numeric control. A local `ContextFlyout` can provide a custom
menu for an individual field.

## 14. Managed file-picker fallback

File and folder selection use Avalonia's `StorageProvider`, which chooses the
platform dialog or a managed fallback. The fallback's appearance is in
`Controls/Z2FileChooser.axaml`, including its overwrite-confirmation prompt.
Its path and filename fields use `Z2TextBox`, and its filter uses `Z2SelectionBox`.
Navigation, filtering, selection, and cancellation remain Avalonia features.

## 15. Adding a Material Design icon

Icons are small vector paths in
[ToolbarIcons.axaml](../CrossPlatformUI/Views/ToolbarIcons.axaml). They come from
**Pictogrammers Material Design Icons (MDI)**; the app includes only the paths it
uses, rather than an icon catalog package.

### Choose the icon and add its path

Find an icon in the [MDI library](https://pictogrammers.com/library/mdi/) and download
its SVG. Ordinary MDI icons use a 24×24 view box, matching `ToolbarIconTheme`.
Copy the `<path>` element's `d` value into a named `StreamGeometry` in
`ToolbarIcons.axaml`. For example, the plus icon is:

```xml
<StreamGeometry x:Key="IconPlus">M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z</StreamGeometry>
```

An SVG with transforms or several differently styled paths needs its artwork
adapted to the canvas first; copying a single path may change its appearance.

### Use it in a view

```xml
<PathIcon Theme="{StaticResource ToolbarIconTheme}"
          Data="{StaticResource IconPlus}" Width="24" Height="24" />
```

The theme scales the icon and uses its `Foreground` as the fill, so it normally
picks up the surrounding button or menu color. `RandomizerView` already includes
the dictionary. In another view, add it to the merged resources:

```xml
<UserControl.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://CrossPlatformUI/Views/ToolbarIcons.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</UserControl.Resources>
```

The action containing the icon should have readable text or an automation name.
After building the Desktop app, check the icon at its intended size in both themes.
