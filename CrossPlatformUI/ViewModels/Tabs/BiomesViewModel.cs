using System;
using System.Diagnostics.CodeAnalysis;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace CrossPlatformUI.ViewModels.Tabs;

[RequiresUnreferencedCode("ReactiveUI uses reflection")]
public class BiomesViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; }
    public MainViewModel Main { get; }

    public IObservable<bool> LegacyVanillaShuffledLocationsIncludedObservable { get; }
    public IObservable<bool> BiomeWeightsIsIncludedObservable { get; }
    public IObservable<bool> ClimateWeightsIsIncludedObservable { get; }

    public BiomesViewModel(MainViewModel main)
    {
        Main = main;
        Activator = new();

        LegacyVanillaShuffledLocationsIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.legacyVanillaShuffledLocationsIncluded())
            .DistinctUntilChanged();

        BiomeWeightsIsIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.biomeWeightsIncluded())
            .DistinctUntilChanged();

        ClimateWeightsIsIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.climateWeightsIncluded())
            .DistinctUntilChanged();

        this.WhenActivated(OnActivate);
    }

    internal void OnActivate(MultipleDisposable disposables)
    {
    }
}
