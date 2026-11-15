using Avalonia.Markup.Xaml;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives.Disposables;
using CrossPlatformUI.ViewModels;
#if CUSTOM_MUSIC
using Z2Randomizer.RandomizerCore.Flags;
using CrossPlatformUI.Controls;
#endif

namespace CrossPlatformUI.Views.Tabs;

public partial class CustomizeView : ReactiveUserControl<MainViewModel>
{
    public CustomizeView()
    {
        this.WhenActivated((MultipleDisposable disposables) => { });
        AvaloniaXamlLoader.Load(this);
#if CUSTOM_MUSIC
        CustomMusicWiki.IsVisible = true;
        CustomMusicControls.Children.Add(new Z2CheckBox { Flag = ConfigFlag.RandomizeMusic });
        CustomMusicControls.Children.Add(new Z2CheckBox { Flag = ConfigFlag.MixCustomAndOriginalMusic });
        CustomMusicControls.Children.Add(new Z2CheckBox { Flag = ConfigFlag.IncludeDiverseMusic });
        CustomMusicControls.Children.Add(new Z2CheckBox { Flag = ConfigFlag.DisableUnsafeMusic });
        CustomMusicControls.IsVisible = true;
#endif
    }
}
