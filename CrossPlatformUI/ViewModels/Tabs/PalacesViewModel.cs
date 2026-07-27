using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using CrossPlatformUI.Services;

namespace CrossPlatformUI.ViewModels.Tabs;

[RequiresUnreferencedCode("ReactiveUI uses reflection")]
public class PalacesViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; }
    public MainViewModel Main { get; }
    public string CustomRoomPoolText { get; private set; } = "Use predefined room pool";
    public string PalaceRoomsText { get; private set; } = "";
    public IObservable<bool> PalaceStyleWeightsIncludedObservable { get; }
    public IObservable<bool> BossRoomsExitTypeIncludedObservable { get; }
    public IObservable<bool> NoDuplicateRoomsByLayoutIncludedObservable { get; }
    public IObservable<bool> NoDuplicateRoomsByEnemiesIncludedObservable { get; }
    public IObservable<bool> RemoveLongDeadEndsIncludedObservable { get; }
    public IObservable<bool> IncludeVanillaRoomsIncludedObservable { get; }
    public IObservable<bool> Includev4_0RoomsIncludedObservable { get; }
    public IObservable<bool> Includev5_0RoomsIncludedObservable { get; }
    public IObservable<bool> IncludeExpertRoomsIncludedObservable { get; }
    public IObservable<bool> BlockingRoomsInAnyPalaceIncludedObservable { get; }
    public IObservable<bool> RemoveTBirdIncludedObservable { get; }
    public IObservable<bool> TBirdRequiredIncludedObservable { get; }

    public PalacesViewModel(MainViewModel main)
    {
        Main = main;
        Activator = new();

        PalaceStyleWeightsIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.palaceStyleWeightsIncluded())
            .DistinctUntilChanged();

        BossRoomsExitTypeIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.bossRoomsExitTypeIncluded())
            .DistinctUntilChanged();

        NoDuplicateRoomsByLayoutIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.noDuplicateRoomsByLayoutIncluded())
            .DistinctUntilChanged();

        NoDuplicateRoomsByEnemiesIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.noDuplicateRoomsByEnemiesIncluded())
            .DistinctUntilChanged();

        RemoveLongDeadEndsIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.removeLongDeadEndsIncluded())
            .DistinctUntilChanged();

        IncludeVanillaRoomsIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.includeVanillaRoomsIncluded())
            .DistinctUntilChanged();

        Includev4_0RoomsIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.includev4_0RoomsIncluded())
            .DistinctUntilChanged();

        Includev5_0RoomsIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.includev5_0RoomsIncluded())
            .DistinctUntilChanged();

        IncludeExpertRoomsIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.includeExpertRoomsIncluded())
            .DistinctUntilChanged();

        BlockingRoomsInAnyPalaceIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.blockingRoomsInAnyPalaceIncluded())
            .DistinctUntilChanged();

        RemoveTBirdIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.removeTBirdIncluded())
            .DistinctUntilChanged();

        TBirdRequiredIncludedObservable = Main.FlagsChanged
            .Select(_ => Main.Config.tBirdRequiredIncluded())
            .DistinctUntilChanged();

        _ = InitializeAsync();

        this.WhenActivated(OnActivate);
    }

    internal void OnActivate(MultipleDisposable disposables)
    {
    }

    public async Task InitializeAsync()
    {
        var roomLoaderService = App.Current?.Services?.GetService<RoomLoaderService>();

        var palaceRooms = await roomLoaderService!.GetPalaceRooms();
        PalaceRoomsText = $"Palace rooms hash: {palaceRooms.Hash}";

        var spec = await roomLoaderService!.GetRoomPoolSpec();
        CustomRoomPoolText = spec != null ? $"Use {spec.Name}" : "Custom Room Pool Not Found";
    }
}
