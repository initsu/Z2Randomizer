using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.Binding;
using RUISG = ReactiveUI.SourceGenerators;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using RxVoid = ReactiveUI.Primitives.RxVoid;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Sidescroll;
using CrossPlatformUI.Presets;
using CrossPlatformUI.Services;
using CrossPlatformUI.ViewModels.Tabs;

namespace CrossPlatformUI.ViewModels;

public partial class RandomizerViewModel : ReactiveObject, IRoutableViewModel, IActivatableViewModel
{
    [JsonIgnore]
    public IObservable<bool> CanGenerateObservable { get; private set; }

    [JsonIgnore]
    public IObservable<IBrush> FlagInputBorderObservable { get; }

    [JsonIgnore]
    public IObservable<IBrush> HeaderBackgroundObservable { get; }

    [RUISG.Reactive]
    public partial bool FlagsValid { get; set; } = true;

    [JsonIgnore]
    [RUISG.Reactive]
    public partial string CustomRoomPoolText { get; private set; } = "";

    [JsonIgnore]
    [RUISG.Reactive]
    public partial bool IsLoadingCustomRooms { get; private set; }

    [JsonIgnore]
    [RUISG.Reactive]
    public partial bool CustomRoomsReady { get; private set; }

    private CancellationTokenSource? customRoomRefresh;
    private (string Rooms, string Spec, RoomDiagnostic[] Warnings)? acknowledgedWarnings;

    private static bool IsFlagStringValid(string flags) => FlagPasteParser.IsValidFlagString(flags);

    [JsonIgnore]
    public string FlagInput { get; set { field = value.Trim(); this.RaisePropertyChanged(); } } = "";

    [JsonIgnore]
    public string Seed
    {
        get => Main.Config.Seed ?? "";
        set
        {
            Main.Config.Seed = value.Trim();
            this.RaisePropertyChanged();
        }
    }

    private string themeVariantName = "";
    public string ThemeVariantName
    {
        get => themeVariantName;
        set
        {
            if (themeVariantName != value)
            {
                themeVariantName = value;
                this.RaisePropertyChanged();
                ThemeHelper.SetTheme(value);
            }
        }
    }
    [JsonIgnore]
    public string AppVersion
    {
        get => $"Z2R {App.Version}";
    }

    // Remembers the selected tab between app restarts.
    [RUISG.Reactive]
    public partial int CurrentTabIndex { get; set; }

    // This remembers the preset you had selected in the list between app restarts (not which preset you *loaded*)
    public string? SelectedPresetName { get; set; }

    [JsonConstructor]
#pragma warning disable CS8618 
    public RandomizerViewModel() {}
#pragma warning restore CS8618
    public RandomizerViewModel(MainViewModel main)
    {
        Main = main;
        HostScreen = Main;
        PresetViewModel = new(Main);
        SearchViewModel = new(Main);
        SearchViewModel.SearchStarted += (_, _) => OpenSearch();
        SearchViewModel.SearchEnded += OnSearchEnded;
        Activator = new ViewModelActivator();

        RerollSeed = ReactiveCommand.Create(() =>
        {
            Main.Config.Seed = new System.Random().Next(0, 999999999).ToString();
        });

        LoadRom = ReactiveCommand.CreateFromObservable(
            () => Main.Router.Navigate.Execute(Main.RomFileViewModel)
        );

        SaveFolder = ReactiveCommand.CreateFromTask(async () =>
        {
            Main.OutputFilePath = await SelectSaveFolder() ?? "";
        });

        CheckForUpdates = ReactiveCommand.CreateFromTask(async () =>
        {
            var checkUpdateService = App.Current?.Services?.GetService<ICheckUpdateService>();
            if (checkUpdateService == null)
            {
                throw new Exception("Unable to load update service");
            }
            await checkUpdateService.CheckUpdate();
        });

        ToggleTheme = ReactiveCommand.Create(() =>
        {
            if (ThemeHelper.IsDark(ThemeVariantName))
            {
                ThemeVariantName = "Light";
            }
            else
            {
                ThemeVariantName = "Dark";
            }
        });

        var seedValidObservable = Main.WhenAnyValue(x => x.Config.Seed)
            .Select(seed => !string.IsNullOrWhiteSpace(seed))
            .DistinctUntilChanged();

        var customMode = Main.WhenAnyValue(x => x.Config.RoomPoolOption)
            .Select(option => option == RoomPoolOption.CUSTOM)
            .DistinctUntilChanged();
        var roomsReady = customMode.CombineLatest(this.WhenAnyValue(x => x.CustomRoomsReady),
            (isCustom, ready) => !isCustom || ready);

        CanGenerateObservable = this.WhenAnyValue(vm => vm.FlagsValid).CombineLatest(
            seedValidObservable,
            Main.RomFileViewModel.HasRomDataObservable,
            Main.GenerateRomViewModel.WhenAnyValue(vm => vm.IsRunning),
            (flagsValid, seedValid, hasRom, isRunning) =>
                flagsValid && seedValid && hasRom && !isRunning)
            .CombineLatest(roomsReady, (canGenerate, ready) => canGenerate && ready);

        FlagInputBorderObservable = this.WhenAnyValue(x => x.ThemeVariantName)
            .CombineLatest(this.WhenAnyValue(x => x.FlagsValid), (theme, valid) =>
                ThemeHelper.GetFlagBorderBrush(theme, valid));

        HeaderBackgroundObservable = this.WhenAnyValue(x => x.ThemeVariantName)
            .CombineLatest(
                Main.FlagsChanged.Select(_ => Main.Config.GenerateSpoiler).DistinctUntilChanged(),
                (theme, alert) => ThemeHelper.GetFlagAlertBackgroundBrush(theme, alert));

        Generate = ReactiveCommand.CreateFromTask(async () =>
        {
            if (Main.Config.RoomPoolOption != RoomPoolOption.CUSTOM)
            {
                Main.GenerateRomViewModel.Open(null);
                return;
            }

            var snapshot = await RefreshCustomRooms();
            if (snapshot != null && Main.Config.RoomPoolOption == RoomPoolOption.CUSTOM)
            {
                Main.GenerateRomViewModel.Open(snapshot);
            }
        }, CanGenerateObservable);

        var canReload = Main.RomFileViewModel.HasRomDataObservable
            .CombineLatest(this.WhenAnyValue(x => x.IsLoadingCustomRooms), (hasRom, loading) => hasRom && !loading);
        ReloadRooms = ReactiveCommand.CreateFromTask(async () => { await RefreshCustomRooms(reviewAgain: true); }, canReload);

        VisitDiscord = ReactiveCommand.CreateFromTask<Control>(async control =>
        {
            var topLevel = TopLevel.GetTopLevel(control);
            if (topLevel is not null)
            {
                await topLevel.Launcher.LaunchUriAsync(new Uri("https://discord.com/invite/BsK47Nsrde"));
            }
        });

        VisitWiki = ReactiveCommand.CreateFromTask<Control>(async control =>
        {
            var topLevel = TopLevel.GetTopLevel(control);
            if (topLevel is not null)
            {
                await topLevel.Launcher.LaunchUriAsync(new Uri("https://github.com/Ellendar/Z2Randomizer/wiki"));
            }
        });

        VisitThirdPartyNotices = ReactiveCommand.CreateFromTask<Control>(async control =>
        {
            var topLevel = TopLevel.GetTopLevel(control);
            if (topLevel is not null)
            {
                await topLevel.Launcher.LaunchUriAsync(new Uri("https://github.com/Ellendar/Z2Randomizer/THIRD-PARTY-NOTICES.md"));
            }
        });

        this.WhenActivated(OnActivate);
    }

    private async Task<CustomRoomSnapshot?> RefreshCustomRooms(bool reviewAgain = false)
    {
        customRoomRefresh?.Cancel();
        Main.RoomDiagnostics.Dismiss();
        CustomRoomsReady = false;
        var rom = Main.RomFileViewModel.RomData;
        if (Main.Config.RoomPoolOption != RoomPoolOption.CUSTOM || rom.Length == 0)
        {
            customRoomRefresh = null;
            IsLoadingCustomRooms = false;
            CustomRoomPoolText = "Select a base ROM to load custom rooms.";
            return null;
        }

        using var refresh = new CancellationTokenSource();
        customRoomRefresh = refresh;
        IsLoadingCustomRooms = true;
        CustomRoomPoolText = "Loading custom rooms…";
        try
        {
            var roomLoader = App.Current!.Services!.GetRequiredService<RoomLoaderService>();
            var snapshot = await roomLoader.LoadCustomRooms(new VanillaRoomData(rom), refresh.Token);
            refresh.Token.ThrowIfCancellationRequested();

            var diagnostics = snapshot.Result.Diagnostics;
            if (snapshot.Result.LoadedRoomsHash is not { } hash)
            {
                CustomRoomPoolText = "Custom rooms could not be loaded.";
                await Main.RoomDiagnostics.ShowAsync(diagnostics, allowContinue: false);
                return null;
            }

            CustomRoomPoolText = $"{snapshot.Spec.Name}\n(Loaded rooms hash: {hash})";
            var warnings = diagnostics.ToArray();
            bool accepted = true;
            if (warnings.Length != 0 && (reviewAgain || acknowledgedWarnings is not { } previous ||
                previous.Rooms != hash || previous.Spec != snapshot.Spec.Hash || !previous.Warnings.SequenceEqual(warnings)))
            {
                accepted = await Main.RoomDiagnostics.ShowAsync(warnings, allowContinue: true);
                refresh.Token.ThrowIfCancellationRequested();
                if (accepted) { acknowledgedWarnings = (hash, snapshot.Spec.Hash, warnings); }
                else { acknowledgedWarnings = null; }
            }
            CustomRoomsReady = true;
            return accepted ? snapshot : null;
        }
        catch (OperationCanceledException) when (refresh.IsCancellationRequested) { return null; }
        catch (Exception exception)
        {
            if (refresh.IsCancellationRequested) { return null; }
            NLog.LogManager.GetCurrentClassLogger().Error(exception, "Unable to load custom rooms.");
            CustomRoomPoolText = "Custom rooms could not be loaded.";
            string message = exception is UserFacingException ? exception.Message : "Unable to read the room files. See the application log for details.";
            await Main.RoomDiagnostics.ShowAsync([new("CustomRoomLoadFailed", RoomDiagnosticSeverity.Error, "", null, null, false, message)], allowContinue: false);
            return null;
        }
        finally
        {
            if (ReferenceEquals(customRoomRefresh, refresh))
            {
                customRoomRefresh = null;
                IsLoadingCustomRooms = false;
            }
        }
    }

    public static async Task<string?> SelectSaveFolder()
    {
        var fileDialog = App.Current?.Services?.GetService<IFileDialogService>()!;
        var folder = await fileDialog.OpenFolderAsync();
        Uri? path = folder?.Path;
        // both LocalPath and TryGetLocalPath() throw for non-absoloute URIs
        string? localPath = path?.IsAbsoluteUri == true
            ? path.LocalPath
            : path?.OriginalString;
        return localPath;
    }

    private void OnActivate(MultipleDisposable disposables)
    {
        var loadedFlags = Main.Config.SerializeFlags(); // this serializes the configuration
        var defaultFlags = new RandomizerConfiguration().SerializeFlags();
        // If the flags are entirely default, use the vanilla preset
        if (loadedFlags == defaultFlags)
        {
            Main.Config.DeserializeFlags(VanillaPreset.Preset.SerializeFlags());
        }

        var roomInputs = Main.RomFileViewModel.RomDataObservable.CombineLatest(
            Main.WhenAnyValue(x => x.Config.RoomPoolOption), (rom, option) => (rom, option)).DistinctUntilChanged();
        SubscribeExtensions.Subscribe(roomInputs, inputs => { _ = RefreshCustomRooms(); })
            .DisposeWith(disposables);

        // flag updates from RandomizerConfiguration always overwrites our flag input
        SubscribeExtensions.Subscribe(Main.FlagsObservable, flags =>
        {
            FlagsValid = true;
            FlagInput = flags;
        })
            .DisposeWith(disposables);

        SubscribeExtensions.Subscribe(
            this.WhenAnyValue(viewModel => viewModel.FlagInput)
                .WithLatestFrom(Main.FlagsObservable, (Input, Current) => (Input, Current)),
            tuple =>
            {
                var isNew = tuple.Input != tuple.Current;
                if (isNew)
                {
                    bool isValid = IsFlagStringValid(tuple.Input);
                    FlagsValid = isValid;
                    if (isValid)
                    {
                        Main.Config.DeserializeFlags(tuple.Input);
                    }
                }
                else if (!FlagsValid)
                {
                    // The input matches the currently loaded flags (always valid), but the flag is
                    // stale after a prior invalid paste left it false.
                    FlagsValid = true;
                }
            })
            .DisposeWith(disposables);

        SubscribeExtensions.Subscribe(
            Main.Config.WhenAnyValue(x => x.Seed).Select(_ => RxVoid.Default),
            _ => this.RaisePropertyChanged(nameof(Seed)))
            .DisposeWith(disposables);

        // ReactiveUI.Validation (latest 7.1.0) is not updated to ReactiveUI 24 and can't be used
        // this.ValidationRule(viewModel => viewModel.FlagsValid, this.WhenAnyValue(x => x.FlagsValid), "Invalid Flags");

        AddValidationRules(disposables);
    }

    /// <summary>
    /// Opens the search results, remembering the tab the user came from.
    /// </summary>
    internal void OpenSearch()
    {
        if (SearchTabIndex < 0)
        {
            return;
        }

        if (CurrentTabIndex == SearchTabIndex)
        {
            return;
        }

        tabIndexBeforeSearch = CurrentTabIndex;
        CurrentTabIndex = SearchTabIndex;
    }

    /// <summary>
    /// The search box was emptied, so the tabs take over again.
    /// </summary>
    private void OnSearchEnded(object? sender, EventArgs e)
    {
        if (SearchTabIndex < 0)
        {
            return;
        }

        // The user moved on to another tab while searching; leave them there.
        if (CurrentTabIndex != SearchTabIndex)
        {
            tabIndexBeforeSearch = -1;
            return;
        }

        if (tabIndexBeforeSearch >= 0)
        {
            CurrentTabIndex = tabIndexBeforeSearch;
        }

        tabIndexBeforeSearch = -1;
    }

    private void AddValidationRules(MultipleDisposable disposables)
    {
        SubscribeExtensions.Subscribe(
            Main.Config.WhenAnyValue(
                x => x.ShuffleOverworldEnemies,
                x => x.ShufflePalaceEnemies,
                (a,b) => (a ?? true) || (b ?? true)
            ),
            _ =>
        {
            var overworldEnemyShuffle = Main.Config.ShuffleOverworldEnemies ?? true;
            var palaceEnemyShuffle = Main.Config.ShufflePalaceEnemies ?? true;
            if (!overworldEnemyShuffle && !palaceEnemyShuffle)
            {
                Main.Config.MixLargeAndSmallEnemies = false;
            }
            if (!palaceEnemyShuffle)
            {
                Main.Config.DripperEnemyOption = DripperEnemyOption.ONLY_BOTS;
                Main.Config.GeneratorsAlwaysMatch = false;
            }
        })
            .DisposeWith(disposables);

        // When PalaceItems and OverworldItems are off, then don't allow MixingOverworldAndPalaceItems
        SubscribeExtensions.Subscribe(
            Main.Config.WhenAnyValue(
                x => x.ShufflePalaceItems,
                x => x.ShuffleOverworldItems,
                (palaceItems, overworldItems) =>
                    !(palaceItems ?? true) || !(overworldItems ?? true)
            ),
            _ =>
        {
            if (!(Main.Config.ShufflePalaceItems ?? true) || !(Main.Config.ShufflePalaceItems ?? true))
            {
                Main.Config.MixOverworldAndPalaceItems = false;
            }
        })
            .DisposeWith(disposables);

        // If shuffle overworld items is off, turn off pbag cave item shuffle too
        SubscribeExtensions.Subscribe(
            Main.Config.WhenAnyValue(x => x.ShuffleOverworldItems).Select(value => value == false),
            disabled =>
        {
            if (!disabled) return;
            Main.Config.IncludePBagCavesInItemShuffle = false;
        })
            .DisposeWith(disposables);

        SubscribeExtensions.Subscribe(
            Main.Config.WhenAnyValue(x => x.ShuffleEncounters).Select(value => value == false),
            disabled =>
        {
            if (!disabled) return;
            Main.Config.IncludeLavaInEncounterShuffle = false;
            Main.Config.AllowUnsafePathEncounters = false;
        })
            .DisposeWith(disposables);

        // If shuffle palaces is off, then don't allow shuffling GP
        SubscribeExtensions.Subscribe(
            Main.Config.WhenAnyValue(x => x.PalacesCanSwapContinents).Select(value => value == false),
            disabled =>
            {
                if (!disabled)
                    return;
                Main.Config.ShuffleGP = false;
            })
            .DisposeWith(disposables);

        SubscribeExtensions.Subscribe(Main.WhenAnyValue(x => x.ShuffleAllExpState), x =>
        {
            if (!x)
                return;
            Main.Config.ShuffleAttackExperience = true;
            Main.Config.ShuffleMagicExperience = true;
            Main.Config.ShuffleLifeExperience = true;
        })
            .DisposeWith(disposables);

        // At most one of the "no duplicate rooms" settings may be on:
        // checking either clears the other (was view code-behind before).
        SubscribeExtensions.Subscribe(Main.Config.WhenAnyValue(x => x.NoDuplicateRoomsByLayout), byLayout =>
        {
            if (byLayout)
                Main.Config.NoDuplicateRoomsByEnemies = false;
        })
            .DisposeWith(disposables);

        SubscribeExtensions.Subscribe(Main.Config.WhenAnyValue(x => x.NoDuplicateRoomsByEnemies), byEnemies =>
        {
            if (byEnemies)
                Main.Config.NoDuplicateRoomsByLayout = false;
        })
            .DisposeWith(disposables);
    }

    [JsonIgnore]
    public bool IsDesktop { get; } = !OperatingSystem.IsBrowser();

    [JsonIgnore]
    public MainViewModel Main { get; }
    public PresetsViewModel PresetViewModel { get; }

    /// <summary>
    /// Searches the flag controls by caption and shows the matches.
    /// </summary>
    [JsonIgnore]
    public SearchViewModel SearchViewModel { get; }

    private int searchTabIndex = -1;

    /// <summary>
    /// Where the "Search" tab sits in the view's tab strip. The view reports it
    /// (the tabs are declared in XAML), so reordering them cannot desynchronize
    /// this.
    /// </summary>
    [JsonIgnore]
    public int SearchTabIndex
    {
        get => searchTabIndex;
        set
        {
            if (searchTabIndex == value)
            {
                return;
            }

            searchTabIndex = value;

            // A session that ended on the Search tab has no search to show, so
            // the app does not come back to it.
            if (value >= 0 &&
                !SearchViewModel.HasQuery &&
                CurrentTabIndex == value)
            {
                CurrentTabIndex = 0;
            }
        }
    }

    /// <summary>The tab the user was on before the search took over.</summary>
    private int tabIndexBeforeSearch = -1;

    [JsonIgnore]
    public ReactiveCommand<RxVoid, RxVoid> RerollSeed { get; }
    [JsonIgnore]
    public ReactiveCommand<RxVoid, RxVoid> Generate { get; }
    [JsonIgnore]
    public ReactiveCommand<RxVoid, RxVoid> ReloadRooms { get; }
    [JsonIgnore]
    public ReactiveCommand<RxVoid, RxVoid> SaveFolder { get; }
    [JsonIgnore]
    public ReactiveCommand<RxVoid, RxVoid> CheckForUpdates { get; }
    [JsonIgnore]
    public ReactiveCommand<RxVoid, RxVoid> ToggleTheme { get; }
    [JsonIgnore]
    public ReactiveCommand<Control, RxVoid> VisitDiscord { get; }
    [JsonIgnore]
    public ReactiveCommand<Control, RxVoid> VisitWiki { get; }

    [JsonIgnore]
    public ReactiveCommand<Control, RxVoid> VisitThirdPartyNotices { get; }
    [JsonIgnore]
    public ReactiveCommand<RxVoid, IRoutableViewModel> LoadRom { get; }

    // Unique identifier for the routable view model.
    [JsonIgnore]
    public string UrlPathSegment { get; } = Guid.NewGuid().ToString()[..5];
    [JsonIgnore]
    public IScreen HostScreen { get; }
    [JsonIgnore]
    public ViewModelActivator Activator { get; }
}
