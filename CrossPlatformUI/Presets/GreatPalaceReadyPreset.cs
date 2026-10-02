using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Overworld;

namespace CrossPlatformUI.Presets;

public static class GreatPalaceReadyPreset
{
    public static string Name => "Great Palace-Ready";

    public static string Description => """
Start in Great Palace with 0 gems, all items and spells and 5-6-7 stats.
""";

    public static readonly RandomizerConfiguration Preset = new()
    {
        //Start
        StartWithCandle = true,
        StartWithGlove = true,
        StartWithRaft = true,
        StartWithBoots = true,
        StartWithFlute = true,
        StartWithCross = true,
        StartWithHammer = true,
        StartWithMagicKey = true,
        StartWithShield = true,
        StartWithJump = true,
        StartWithLife = true,
        StartWithFairy = true,
        StartWithFire = true,
        StartWithReflect = true,
        StartWithSpellSpell = true,
        StartWithThunder = true,
        ShuffleStartingItems = false,
        ShuffleStartingSpells = false,
        StartingHeartContainersMin = 3,
        StartingHeartContainersMax = 8,
        MaxHeartContainers = MaxHeartsOption.PLUS_ONE,
        StartingMagicContainersMin = 8,
        StartingMagicContainersMax = 8,
        StartingTechniques = StartingTechs.BOTH,
        StartingLives = StartingLives.Lives3,
        StartingAttackLevel = 5,
        StartingMagicLevel = 6,
        StartingLifeLevel = 7,
        StartingLocation = StartingLocation.GREAT_PALACE,
        IndeterminateOptionRate = IndeterminateOptionRate.HALF,

        //Overworld
        PalacesCanSwapContinents = true,
        ShuffleGP = true,
        ShuffleEncounters = false,
        AllowUnsafePathEncounters = false,
        IncludeLavaInEncounterShuffle = false,
        EncounterRate = EncounterRate.HALF,
        RiverDevilBlockerOption = RiverDevilBlockerOption.RANDOM,
        EastRocks = true,
        GenerateBaguWoods = false,
        LessImportantLocationsOption = LessImportantLocationsOption.REMOVE,
        RestrictConnectionCaveShuffle = true,
        AllowConnectionCavesToBeBlocked = true,
        GoodBoots = true,
        HidePalace = null,
        HideKasuto = null,
        ShuffleWhichLocationIsHidden = false,
        WestSize = OverworldSizeOption.MEDIUM,
        EastSize = OverworldSizeOption.MEDIUM,
        DmSize = DmSizeOption.SMALL,
        MazeSize = MazeSizeOption.MEDIUM,
        WestBiome = Biome.VANILLALIKE,
        EastBiome = Biome.VANILLALIKE,
        MazeBiome = Biome.VANILLALIKE,
        DmBiome = Biome.VANILLALIKE,
        WestClimate = ClimateEnum.VANILLA_WEIGHTED,
        EastClimate = ClimateEnum.VANILLA_WEIGHTED,
        DmClimate = ClimateEnum.CLASSIC,
        ContinentConnectionType = ContinentConnectionType.NORMAL,

        //Palaces
        NormalPalaceStyle = PalaceStyle.VANILLA_WEIGHTED,
        GpStyle = PalaceStyle.RANDOM,
        NormalPalaceLength = PalaceLengthOption.MEDIUM,
        GpLength = PalaceLengthOption.MEDIUM,
        RoomPoolOption = RoomPoolOption.V5_0,
        PalacesToCompleteMin = 0,
        PalacesToCompleteMax = 0,
        RestartAtPalacesOnGameOver = true,
        Global5050JarDrop = true,
        ReduceDripperVariance = true,
        ChangePalacePallettes = true,
        RandomizeBossItemDrop = true,
        PalaceDropStyle = PalaceDropStyle.RANDOM,
        BossRoomsExitType = BossRoomsExitType.OVERWORLD,
        NoDuplicateRoomsByLayout = true,
        BlockingRoomsInAnyPalace = true,
        HardBosses = true,
        AggressiveTbird = true,
        PalaceItemRoomCount = PalaceItemRoomCount.ONE,
        DarkLinkMinDistance = BossRoomMinDistance.SHORT,

        //Levels
        ShuffleAttackExperience = true,
        ShuffleMagicExperience = true,
        ShuffleLifeExperience = true,

        AttackLevelCap = 8,
        MagicLevelCap = 8,
        LifeLevelCap = 8,
        AttackEffectiveness = AttackEffectiveness.AVERAGE,
        MagicEffectiveness = MagicEffectiveness.AVERAGE,
        LifeEffectiveness = LifeEffectiveness.AVERAGE,

        //Spells
        ShuffleLifeRefillAmount = false,
        ShuffleSpellLocations = true,
        DisableMagicContainerRequirements = true,
        RandomizeSpellSpellEnemy = false,
        SwapUpAndDownStab = false,
        FireOption = FireOption.REPLACE_WITH_DASH,

        //Enemies
        ShuffleOverworldEnemies = true,
        ShufflePalaceEnemies = true,
        DripperEnemyOption = DripperEnemyOption.EASIER_GROUND_ENEMIES_FULL_HP,
        MixLargeAndSmallEnemies = true,
        GeneratorsAlwaysMatch = true,
        ShuffleEnemyHP = EnemyLifeOption.MEDIUM,
        ShuffleBossHP = EnemyLifeOption.MEDIUM,
        ShuffleXPStealers = true,
        ShuffleXPStolenAmount = true,
        EnemyXPDrops = XPEffectiveness.RANDOM,

        //Items
        ShufflePalaceItems = true,
        ShuffleOverworldItems = true,
        MixOverworldAndPalaceItems = true,
        IncludePBagCavesInItemShuffle = true,
        IncludeSwordTechsInShuffle = false,
        IncludeQuestItemsInShuffle = false,
        IncludeSpellsInShuffle = false,
        ShuffleSmallItems = true,
        RemoveSpellItems = false,
        ShufflePBagAmounts = false,
        PalacesContainExtraKeys = false,
        RandomizeNewKasutoJarRequirements = true,
        AllowImportantItemDuplicates = false,

        //Drops
        ShuffleItemDropFrequency = false,
        RandomizeDrops = false,
        StandardizeDrops = true,
        SmallEnemiesCanDropBlueJar = true,
        LargeEnemiesCanDropRedJar = true,

        //Hints
        HelpfulHints = HelpfulHintOption.TOWNS_SEPARATE,
        EnableSpellItemHints = true,
        EnableTownNameHints = true,
        FasterDashFairy = true,
        RevealWalkthroughWalls = false,
    };
}
