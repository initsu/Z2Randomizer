using System;
using System.Diagnostics.CodeAnalysis;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace CrossPlatformUI.ViewModels.Tabs;

[RequiresUnreferencedCode("ReactiveUI uses reflection")]
public class SpellsViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; }
    public MainViewModel Main { get; }

    public IObservable<bool> FasterDashFairyIncludedObservable { get; }

    public SpellsViewModel(MainViewModel main)
    {
        Main = main;
        Activator = new();

        FasterDashFairyIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.fasterDashFairyIncluded())
            .DistinctUntilChanged();

        this.WhenActivated(OnActivate);
    }

    internal void OnActivate(MultipleDisposable disposables)
    {
    }
}
