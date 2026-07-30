using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NLog;
using Z2Randomizer.RandomizerCore.Flags;
using Z2Randomizer.RandomizerCore.Sidescroll;
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantDefaultMemberInitializer

namespace Z2Randomizer.RandomizerCore;

[AttributeUsage(AttributeTargets.Class)]
public class FlagSerializeAttribute : Attribute
{
}

/**
 * We don't need to bring in ReactiveUI to the base RandomizerCore if we just make our own source generator.
 * To keep the usage similar to the original ReactiveUI SourceGenerator, I kept the name `Reactive` for the attribute
 * in case we bail on this idea later.
 */
public class ReactiveAttribute : Attribute
{

}


[FlagSerialize]
public sealed partial class RandomizerConfiguration() : INotifyPropertyChanged
{
    [IgnoreInFlags]
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [IgnoreInFlags]
    private static readonly Collectable[] POSSIBLE_STARTING_ITEMS = [
        Collectable.CANDLE,
        Collectable.GLOVE,
        Collectable.RAFT,
        Collectable.BOOTS,
        Collectable.FLUTE,
        Collectable.CROSS,
        Collectable.HAMMER,
        Collectable.MAGIC_KEY
    ];

    [IgnoreInFlags]
    private static readonly Collectable[] POSSIBLE_STARTING_SPELLS = [
        Collectable.SHIELD_SPELL,
        Collectable.JUMP_SPELL,
        Collectable.LIFE_SPELL,
        Collectable.FAIRY_SPELL,
        Collectable.FIRE_SPELL,
        Collectable.REFLECT_SPELL,
        Collectable.SPELL_SPELL,
        Collectable.THUNDER_SPELL
    ];

    [IgnoreInFlags]
    private readonly static Collectable[] POSSIBLE_LINKED_FIRE_SPELLS = [
        Collectable.SHIELD_SPELL,
        Collectable.JUMP_SPELL,
        Collectable.LIFE_SPELL,
        Collectable.FAIRY_SPELL,
        Collectable.REFLECT_SPELL,
        Collectable.SPELL_SPELL,
        Collectable.THUNDER_SPELL
    ];

    //Start Configuration
    [Reactive]
    private bool shuffleStartingItems = false;

    [Reactive]
    private bool startWithCandle = false;

    [Reactive]
    private bool startWithGlove = false;

    [Reactive]
    private bool startWithRaft = false;

    [Reactive]
    private bool startWithBoots = false;

    [Reactive]
    private bool startWithFlute = false;

    [Reactive]
    private bool startWithCross = false;

    [Reactive]
    private bool startWithHammer = false;

    [Reactive]
    private bool startWithMagicKey = false;

    [Reactive]
    private bool shuffleStartingSpells = false;

    [Reactive]
    private StartingResourceLimit startItemsLimit = StartingResourceLimit.NO_LIMIT;

    [Reactive]
    private bool startWithShield = false;

    [Reactive]
    private bool startWithJump = false;

    [Reactive]
    private bool startWithLife = false;

    [Reactive]
    private bool startWithFairy = false;

    [Reactive]
    private bool startWithFire = false;

    [Reactive]
    private bool startWithReflect = false;

    [Reactive]
    private bool startWithSpellSpell = false;

    [Reactive]
    private bool startWithThunder = false;

    [Reactive]
    private StartingResourceLimit startSpellsLimit = StartingResourceLimit.NO_LIMIT;

    [Reactive]
    [Minimum(1)]
    [Maximum(8)]
    private int? startingHeartContainersMin = 4;

    [Reactive]
    [Minimum(1)]
    [Maximum(8)]
    private int? startingHeartContainersMax = 4;

    [Reactive]
    [Minimum(1)]
    [Maximum(8)]
    private int? startingMagicContainersMin = 4;

    [Reactive]
    [Minimum(1)]
    [Maximum(8)]
    private int? startingMagicContainersMax = 4;

    [Reactive]
    private MaxHeartsOption maxHeartContainers = MaxHeartsOption.EIGHT;

    [Reactive]
    private StartingTechs startingTechniques = StartingTechs.NONE;

    [Reactive]
    private StartingLives startingLives = StartingLives.Lives3;

    [Reactive]
    [Minimum(1)]
    [Maximum(8)]
    private int startingAttackLevel = 1;

    [Reactive]
    [Minimum(1)]
    [Maximum(8)]
    private int startingMagicLevel = 1;

    [Reactive]
    [Minimum(1)]
    [Maximum(8)]
    private int startingLifeLevel = 1;

    [Reactive]
    private StartingLocation startingLocation = StartingLocation.NORTH_PALACE;

    [Reactive]
    private IndeterminateOptionRate indeterminateOptionRate = IndeterminateOptionRate.HALF;

    //Overworld
    [Reactive]
    private bool? palacesCanSwapContinents = false;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool? shuffleGP = false;
    public bool shuffleGPIncluded() => palacesCanSwapContinents != false;

    [Reactive]
    private EncounterRate encounterRate = EncounterRate.NORMAL;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool? shuffleEncounters = false;
    public bool shuffleEncountersIncluded() => encounterRate != EncounterRate.NONE;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool allowUnsafePathEncounters = false;
    public bool allowUnsafePathEncountersIncluded() => shuffleEncountersIncluded() && shuffleEncounters != false;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool includeLavaInEncounterShuffle = false;
    public bool includeLavaInEncounterShuffleIncluded() => shuffleEncountersIncluded() && shuffleEncounters != false;

    [Reactive]
    private bool? hidePalace = true;

    [Reactive]
    private bool? hideKasuto = true;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool? shuffleWhichLocationIsHidden = false;
    public bool shuffleWhichLocationIsHiddenIncluded() => hidePalace != false || hideKasuto != false;

    [Reactive]
    private LessImportantLocationsOption lessImportantLocationsOption = LessImportantLocationsOption.HIDE;

    [Reactive]
    private bool? restrictConnectionCaveShuffle = true;

    [Reactive]
    private bool allowConnectionCavesToBeBlocked = false;

    [Reactive]
    private bool? goodBoots = false;

    [Reactive]
    private bool? generateBaguWoods = true;

    [Reactive]
    private ContinentConnectionType continentConnectionType = ContinentConnectionType.NORMAL;

    [Reactive]
    private OverworldSizeOption westSize = OverworldSizeOption.LARGE;

    [Reactive]
    private OverworldSizeOption eastSize = OverworldSizeOption.LARGE;

    [Reactive]
    private DmSizeOption dmSize = DmSizeOption.LARGE;

    [Reactive]
    private MazeSizeOption mazeSize = MazeSizeOption.LARGE;

    [Reactive]
    private Biome westBiome = Biome.VANILLA;

    [Reactive]
    private Biome eastBiome = Biome.VANILLA;

    [Reactive]
    private Biome dmBiome = Biome.VANILLA;

    [Reactive]
    private Biome mazeBiome = Biome.VANILLA;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private ImmutableDictionary<Biome, int> biomeWeights = biomeWeightsDefault();
    public bool biomeWeightsIncluded()
    {
        foreach (var biome in (List<Biome>)[westBiome, eastBiome, dmBiome, mazeBiome])
        {
            switch (biome)
            {
                case Biome.RANDOM_CUSTOM:
                    return true;
            }
        }
        return false;
    }
    public static ImmutableDictionary<Biome, int> biomeWeightsDefault()
    {
        var builder = ImmutableDictionary.CreateBuilder<Biome, int>();

        foreach (var enumValue in Enum.GetValues<Biome>().Where(b => b.CanHaveWeight()))
        {
            builder.Add(enumValue, enumValue switch
            {
                Biome.VANILLA => 0,
                Biome.VANILLA_SHUFFLE => 0,
                _ => 1,
            });
        }
        return builder.ToImmutableDictionary();
    }

    [Reactive]
    private ClimateEnum westClimate = ClimateEnum.VANILLA_WEIGHTED;

    [Reactive]
    private ClimateEnum eastClimate = ClimateEnum.VANILLA_WEIGHTED;

    [Reactive]
    private ClimateEnum dmClimate = ClimateEnum.CLASSIC;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private ImmutableDictionary<ClimateEnum, int> climateWeights = climateWeightsDefault();
    public bool climateWeightsIncluded()
    {
        foreach (var biome in (List<ClimateEnum>)[westClimate, eastClimate, dmClimate])
        {
            switch (biome)
            {
                case ClimateEnum.RANDOM_CUSTOM:
                    return true;
            }
        }
        return false;
    }
    public static ImmutableDictionary<ClimateEnum, int> climateWeightsDefault()
    {
        var builder = ImmutableDictionary.CreateBuilder<ClimateEnum, int>();

        foreach (var enumValue in Enum.GetValues<ClimateEnum>().Where(b => b.CanHaveWeight()))
        {
            builder.Add(enumValue, enumValue switch
            {
                ClimateEnum.CHAOS => 0,
                ClimateEnum.WETLANDS => 0,
                _ => 1,
            });
        }
        return builder.ToImmutableDictionary();
    }

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool legacyVanillaShuffledLocations = false;
    public bool legacyVanillaShuffledLocationsIncluded() {
        foreach (var biome in (List<Biome>)[westBiome, eastBiome, dmBiome, mazeBiome])
        {
            switch (biome)
            {
                case Biome.VANILLA_SHUFFLE:
                case Biome.RANDOM:
                case Biome.RANDOM_NO_VANILLA:
                    return true;
                case Biome.RANDOM_CUSTOM:
                    if (biomeWeights.GetValueOrDefault(Biome.VANILLA_SHUFFLE) > 0)
                    {
                        return true;
            }
                    continue;
        }
        }
        return false;
    }

    [Reactive]
    public bool mazeRevealLocations;

    //Palaces
    [Reactive]
    private PalaceStyle normalPalaceStyle = PalaceStyle.VANILLA;

    [Reactive]
    private PalaceStyle gpStyle = PalaceStyle.VANILLA;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private ImmutableDictionary<PalaceStyle, int> palaceStyleWeights = palaceStyleWeightsDefault();
    public bool palaceStyleWeightsIncluded() => palaceStylesAnyMetastyleSelected();
    public static ImmutableDictionary<PalaceStyle, int> palaceStyleWeightsDefault()
    {
        var builder = ImmutableDictionary.CreateBuilder<PalaceStyle, int>();
        foreach (var enumValue in Enum.GetValues<PalaceStyle>().Where(b => b.CanHaveWeight()))
        {
            builder.Add(enumValue, enumValue switch
            {
                PalaceStyle.CHAOS => 0,
                _ => 1,
            });
        }
        return builder.ToImmutableDictionary();
    }

    private bool palaceStylesAreNotAllVanilla()
    {
        foreach (var style in (List<PalaceStyle>)[normalPalaceStyle, gpStyle])
        {
            switch (style)
            {
                case PalaceStyle.VANILLA:
                    break;
                default:
                    return true;
            }
        }
        return false;
    }

    private bool palaceStylesAreNotAllVanillaOrShuffled()
    {
        foreach (var style in (List<PalaceStyle>)[normalPalaceStyle, gpStyle])
        {
            switch (style)
            {
                case PalaceStyle.VANILLA:
                case PalaceStyle.SHUFFLED:
                    break;
                default:
                    return true;
            }
        }
        return false;
    }

    private bool roomSelectionEnabled()
    {
        if (customRoomPool)
        {
            return false;
        }
        return palaceStylesAreNotAllVanillaOrShuffled();
    }

    [Reactive]
    private bool customRoomPool = true;

    private bool palaceStylesAnyMetastyleSelected()
    {
        foreach (var style in (List<PalaceStyle>)[normalPalaceStyle, gpStyle])
        {
            if (style.IsMetastyle())
            {
                return true;
            }
        }
        return false;
    }

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool? includeVanillaRooms = true;
    public bool includeVanillaRoomsIncluded() => roomSelectionEnabled();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool? includev4_0Rooms = false;
    public bool includev4_0RoomsIncluded() => roomSelectionEnabled();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool? includev5_0Rooms = false;
    public bool includev5_0RoomsIncluded() => roomSelectionEnabled();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool blockingRoomsInAnyPalace = false;
    public bool blockingRoomsInAnyPalaceIncluded() => palaceStylesAreNotAllVanilla();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private PalaceDropStyle palaceDropStyle = PalaceDropStyle.ANY_EXIT;
    public bool palaceDropStyleIncluded() => palaceStylesAreNotAllVanilla();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool removeLongDeadEnds = false;
    public bool removeLongDeadEndsIncluded() => includev5_0RoomsIncluded() && includev5_0Rooms is not false;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool includeExpertRooms = false;
    public bool includeExpertRoomsIncluded() => roomSelectionEnabled();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private BossRoomsExitType bossRoomsExitType = BossRoomsExitType.OVERWORLD;
    public bool bossRoomsExitTypeIncluded() => palaceStylesAreNotAllVanillaOrShuffled();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool? tBirdRequired = true;
    public bool tBirdRequiredIncluded() => palaceStylesAreNotAllVanilla();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool removeTBird = false;
    public bool removeTBirdIncluded() => tBirdRequiredIncluded() && tBirdRequired != true;

    [Reactive]
    private bool restartAtPalacesOnGameOver = false;

    [Reactive]
    private bool? global5050JarDrop = false;

    [Reactive]
    private bool reduceDripperVariance = false;

    [Reactive]
    private bool changePalacePallettes = false;

    [Reactive]
    private bool randomizeBossItemDrop = false;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private BossRoomMinDistance darkLinkMinDistance = BossRoomMinDistance.NONE;
    public bool darkLinkMinDistanceIncluded() => palaceStylesAreNotAllVanilla();

    [Reactive]
    private PalaceItemRoomCount palaceItemRoomCount = PalaceItemRoomCount.ONE;

    [Reactive]
    [Minimum(0)]
    [Maximum(6)]
    private int palacesToCompleteMin = 6;

    [Reactive]
    [Minimum(0)]
    [Maximum(6)]
    [ConditionallyIncludeInFlags]
    private int palacesToCompleteMax = 6;
    public bool palacesToCompleteMaxIncluded() => palacesToCompleteMin != 6;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool noDuplicateRoomsByLayout = false;
    public bool noDuplicateRoomsByLayoutIncluded() => palaceStylesAreNotAllVanillaOrShuffled();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool noDuplicateRoomsByEnemies = false;
    public bool noDuplicateRoomsByEnemiesIncluded() => palaceStylesAreNotAllVanillaOrShuffled();

    [Reactive]
    private bool hardBosses = false;

    [Reactive]
    private bool aggressiveTbird = false;

    //Levels
    [Reactive]
    private bool shuffleAttackExperience = false;

    [Reactive]
    private bool shuffleMagicExperience = false;

    [Reactive]
    private bool shuffleLifeExperience = false;

    [Reactive]
    [Minimum(1)]
    [Maximum(8)]
    private int attackLevelCap = 8;

    [Reactive]
    [Minimum(1)]
    [Maximum(8)]
    private int magicLevelCap = 8;

    [Reactive]
    [Minimum(1)]
    [Maximum(8)]
    private int lifeLevelCap = 8;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool scaleLevelRequirementsToCap = false;
    public bool scaleLevelRequirementsToCapIncluded() => attackLevelCap < 8 || magicLevelCap < 8 || lifeLevelCap < 8;

    [Reactive]
    private AttackEffectiveness attackEffectiveness = AttackEffectiveness.VANILLA;

    [Reactive]
    private MagicEffectiveness magicEffectiveness = MagicEffectiveness.VANILLA;

    [Reactive]
    private LifeEffectiveness lifeEffectiveness = LifeEffectiveness.VANILLA;

    //Spells
    [Reactive]
    private bool shuffleLifeRefillAmount = false;

    [Reactive]
    private bool? shuffleSpellLocations = false;

    [Reactive]
    private bool? disableMagicContainerRequirements = false;

    [Reactive]
    private bool? randomizeSpellSpellEnemy = false;

    [Reactive]
    private bool? swapUpAndDownStab = false;

    [Reactive]
    private FireOption fireOption = FireOption.NORMAL;

    //Enemies
    [Reactive]
    private bool? shuffleOverworldEnemies = false;

    [Reactive]
    private bool? shufflePalaceEnemies = false;

    private bool anyEnemiesAreShuffled() => shuffleOverworldEnemies != false || shufflePalaceEnemies != false;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private DripperEnemyOption dripperEnemyOption = DripperEnemyOption.ONLY_BOTS;
    public bool dripperEnemyOptionIncluded() => anyEnemiesAreShuffled();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool? mixLargeAndSmallEnemies = false;
    public bool mixLargeAndSmallEnemiesIncluded() => anyEnemiesAreShuffled();

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool generatorsAlwaysMatch = true;
    public bool generatorsAlwaysMatchIncluded() => anyEnemiesAreShuffled();

    [Reactive]
    private EnemyLifeOption shuffleEnemyHP = EnemyLifeOption.VANILLA;

    [Reactive]
    private EnemyLifeOption shuffleBossHP = EnemyLifeOption.VANILLA;

    [Reactive]
    private bool shuffleXPStealers = false;

    [Reactive]
    private bool shuffleXPStolenAmount = false;

    [Reactive]
    private SwordImmunityOption swordImmunityOption = SwordImmunityOption.VANILLA;

    [Reactive]
    private XPEffectiveness enemyXPDrops = XPEffectiveness.VANILLA;

    //Items
    [Reactive]
    private bool? shufflePalaceItems = false;

    [Reactive]
    private bool? shuffleOverworldItems = false;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool? mixOverworldAndPalaceItems = false;
    public bool mixOverworldAndPalaceItemsIncluded() => shufflePalaceItems != false && shuffleOverworldItems != false;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool? includePBagCavesInItemShuffle = false;
    public bool includePBagCavesInItemShuffleIncluded() => shuffleOverworldItems != false;

    [Reactive]
    private bool shuffleSmallItems = false;

    [Reactive]
    private bool? palacesContainExtraKeys = false;

    [Reactive]
    private bool randomizeNewKasutoJarRequirements = false;

    [Reactive]
    private bool fastItemPickup = false;

    [Reactive]
    private bool allowImportantItemDuplicates = false;

    [Reactive]
    private bool? removeSpellItems = false;

    [Reactive]
    private bool? shufflePBagAmounts = false;

    [Reactive]
    private bool? includeSpellsInShuffle = false;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool questItemChainsAllowed = false;
    public bool questItemChainsAllowedIncluded() => includeSpellsInShuffle != false;

    [Reactive]
    private bool? includeSwordTechsInShuffle = false;

    [Reactive]
    private bool? includeQuestItemsInShuffle = false;

    [Reactive]
    private bool? includeBagusNoteInShuffle = false;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool townQuestLocationsAreMinorItems = false;
    public bool townQuestLocationsAreMinorItemsIncluded() => includeSwordTechsInShuffle != false || includeQuestItemsInShuffle != false;

    [Reactive]
    private bool removeFairy = false;

    //Drops
    [Reactive]
    private bool shuffleItemDropFrequency = false;

    [Reactive]
    private bool randomizeDrops = false;

    [Reactive]
    private bool standardizeDrops = false;

    [Reactive]
    private bool smallEnemiesCanDropBlueJar = false;

    [Reactive]
    private bool smallEnemiesCanDropRedJar = false;

    [Reactive]
    private bool smallEnemiesCanDropSmallBag = false;

    [Reactive]
    private bool smallEnemiesCanDropMediumBag = false;

    [Reactive]
    private bool smallEnemiesCanDropLargeBag = false;

    [Reactive]
    private bool smallEnemiesCanDropXLBag = false;

    [Reactive]
    private bool smallEnemiesCanDrop1up = false;

    [Reactive]
    private bool smallEnemiesCanDropKey = false;

    [Reactive]
    private bool largeEnemiesCanDropBlueJar = false;

    [Reactive]
    private bool largeEnemiesCanDropRedJar = false;

    [Reactive]
    private bool largeEnemiesCanDropSmallBag = false;

    [Reactive]
    private bool largeEnemiesCanDropMediumBag = false;

    [Reactive]
    private bool largeEnemiesCanDropLargeBag = false;

    [Reactive]
    private bool largeEnemiesCanDropXLBag = false;

    [Reactive]
    private bool largeEnemiesCanDrop1up = false;

    [Reactive]
    private bool largeEnemiesCanDropKey = false;

    //Misc
    [Reactive]
    private bool? enableHelpfulHints = false;

    [Reactive]
    private bool? enableSpellItemHints = false;

    [Reactive]
    private bool? enableTownNameHints = false;

    [Reactive]
    private bool jumpAlwaysOn = false;

    [Reactive]
    private bool dashAlwaysOn = false;

    [Reactive]
    [ConditionallyIncludeInFlags]
    private bool fasterDashFairy = false;
    public bool fasterDashFairyIncluded() => fireOption.CanBeDash() || dashAlwaysOn != false;

    [Reactive]
    private bool permanentBeamSword = false;

    //Custom
    [Reactive]
    [IgnoreInFlags]
    private bool useCommunityText = false;

    [Reactive]
    [IgnoreInFlags]
    private BeepFrequency beepFrequency = BeepFrequency.Normal;

    [Reactive]
    [IgnoreInFlags]
    private BeepThreshold beepThreshold = BeepThreshold.Normal;

    [Reactive]
    [IgnoreInFlags]
    private bool disableMusic = false;

    [Reactive]
    [IgnoreInFlags]
    private bool randomizeMusic = false;

    [Reactive]
    [IgnoreInFlags]
    private bool mixCustomAndOriginalMusic = false;
    
    [Reactive]
    [IgnoreInFlags]
    private bool includeDiverseMusic = false;

    [Reactive]
    [IgnoreInFlags]
    private bool disableUnsafeMusic = false;

    [Reactive]
    [IgnoreInFlags]
    private bool fastSpellCasting = false;

    [Reactive]
    [IgnoreInFlags]
    private bool upAOnController1 = false;

    [Reactive]
    [IgnoreInFlags]
    private bool removeFlashing = true;

    [Reactive]
    [IgnoreInFlags]
    private CharacterSprite sprite = CharacterSprite.LINK;

    [Reactive]
    [IgnoreInFlags]
    private string spriteName = CharacterSprite.LINK.DisplayName;


    [Reactive]
    [IgnoreInFlags]
    private bool changeItemSprites = false;

    [Reactive]
    [IgnoreInFlags]
    private NesColor tunic = NesColor.Default;

    [Reactive]
    [IgnoreInFlags]
    private NesColor skinTone = NesColor.Default;

    [Reactive]
    [IgnoreInFlags]
    private NesColor tunicOutline = NesColor.Default;

    [Reactive]
    [IgnoreInFlags]
    private NesColor shieldTunic = NesColor.Default;

    [Reactive]
    [IgnoreInFlags]
    private bool shuffleSpritePalettes = false;

    [Reactive]
    [IgnoreInFlags]
    private BeamSprites beamSprite = BeamSprites.DEFAULT;

    [Reactive]
    [IgnoreInFlags]
    private bool useCustomRooms = false;

    [Reactive]
    [IgnoreInFlags]
    private bool disableHUDLag = false;

    [Reactive]
    private bool randomizeKnockback = false;

    [Reactive]
    private PalaceLengthOption gpLength = PalaceLengthOption.FULL;

    [Reactive]
    private PalaceLengthOption normalPalaceLength = PalaceLengthOption.FULL;


    [Reactive]
    private RiverDevilBlockerOption riverDevilBlockerOption = RiverDevilBlockerOption.PATH;

    [Reactive]
    private bool? eastRocks = true;

    [Reactive]
    private bool generateSpoiler = false;

    [Reactive]
    private bool revealWalkthroughWalls = false;

    //Meta
    [Reactive]
    [Required]
    [IgnoreInFlags]
    private string? seed;
    // public string Seed { get => seed ?? ""; set => SetField(ref seed, value); }

    [IgnoreInFlags]
    private bool _inDeserializeFlags = false;

    public void DeserializeFlags(string flags)
    {
        // avoid emitting property changed for Flags during deserialization
        _inDeserializeFlags = true;
        Deserialize(flags?.Trim() ?? "");
        _inDeserializeFlags = false;
        OnPropertyChanged("Flags");
    }
    public String SerializeFlags()
    {
        return Serialize();
    }

    public RandomizerConfiguration(string flagstring) : this()
    {
        Deserialize(flagstring);
    }

    public static bool DeserializeBool(FlagReader flags, string name)
    {
        return flags.ReadBool();
    }

    public static bool? DeserializeNullableBool(FlagReader flags, string name)
    {
        return flags.ReadNullableBool();
    }

    public static int DeserializeInt(FlagReader flags, string name, int minimum, int maximum)
    {
        var extent = maximum - minimum + 1;
        return flags.ReadInt(extent) + minimum;
    }

    public static T DeserializeEnum<T>(FlagReader flags, string name) where T: Enum
    {
        var extent = GetEnumCount<T>();
        var index = flags.ReadInt(extent);
        return GetEnumFromIndex<T>(index)!;
    }

    private ImmutableDictionary<T, int> DeserializeWeightedEnum<T>(FlagReader flags, string name, Predicate<T> includeOptionPredicate) where T : Enum
    {
        Dictionary<T, int> val = new();
        int enumCount = GetEnumCount<T>();
        for (int enumIndex = 0; enumIndex < enumCount; enumIndex++)
        {
            T enumOption = GetEnumFromIndex<T>(enumIndex)!;
            if (includeOptionPredicate(enumOption))
            {
                int enumWeight = flags.ReadInt(4);
                Debug.Assert(0 <= enumWeight && enumWeight <= 3);
                val[enumOption] = enumWeight;
            }
        }
        return val.ToImmutableDictionary();
    }

    public static void SerializeBool(FlagBuilder flags, string name, bool val)
    {
        flags.Append(val);
    }

    public static void SerializeNullableBool(FlagBuilder flags, string name, bool? val)
    {
        flags.Append(val);
    }
    public static void SerializeInt(FlagBuilder flags, string name, int? val, int minimum, int maximum)
    {
        // null values will be coerced to the minimum value
        // For nullable ints, Enums are our preferred option.
        int extent = maximum - minimum + 1;
        int value = val - minimum ?? minimum;
        if (value < 0 || value >= extent)
        {
            logger.Warn($"Property ({name}={val}) is out of range.");
        }
        flags.Append(value, extent);
    }

    public static void SerializeEnum<T>(FlagBuilder flags, string name, T? val) where T: Enum
    {
        var index = GetEnumIndex<T>(val);
        var extent = GetEnumCount<T>();
        flags.Append(index, extent);
    }

    private void SerializeWeightedEnum<T>(FlagBuilder flags, string name, ImmutableDictionary<T, int>? val, Predicate<T> includeOptionPredicate) where T : Enum
    {
        int enumCount = GetEnumCount<T>();
        for (int enumIndex = 0; enumIndex < enumCount; enumIndex++)
        {
            T enumOption = GetEnumFromIndex<T>(enumIndex)!;
            if (includeOptionPredicate(enumOption))
            {
                int enumWeight = val?.GetValueOrDefault(enumOption) ?? 0;
                Debug.Assert(0 <= enumWeight && enumWeight <= 3);
                flags.Append(enumWeight, 4);
            }
        }
    }

    public RandomizerProperties Export(Random r)
    {
        RandomizerProperties properties = new()
        {
            Flags = SerializeFlags(),

            WestIsHorizontal = r.Next(2) == 1,
            EastIsHorizontal = r.Next(2) == 1,
            DmIsHorizontal = r.Next(2) == 1,
            EastRockIsPath = r.Next(2) == 1,

            //ROM Info
            Seed = seed
        };

        properties.RemoveItems = [];
        if (removeFairy) { properties.RemoveItems.Add(Collectable.FAIRY_SPELL); }

        //Set biomes first (so Vanilla Everything is known)
        AssignBiome(properties, r);

        //Properties that can affect available minor item replacements
        do // while (!properties.HasEnoughSpaceToAllocateItems())
        {
            //Start Configuration
            ShuffleStartingCollectables(POSSIBLE_STARTING_ITEMS, startItemsLimit, shuffleStartingItems, properties, r);
            properties.StartingSpells = ShuffleStartingCollectables(POSSIBLE_STARTING_SPELLS, startSpellsLimit, shuffleStartingSpells, properties, r).ToHashSet();

            // Give North Palace its chance to roll now. Other Random cases are rolled later.
            if (startingLocation.IsMetastyle() && r.Next(9) == 0)
            {
                properties.StartingLocation = StartingLocation.NORTH_PALACE;
            }
            else
            {
                properties.StartingLocation = startingLocation;
            }

            List<PalaceStyle> allowedPalaceStyles;
            if(GpStyle.IsMetastyle())
            {
                Debug.Assert(GpStyle == PalaceStyle.RANDOM);
                allowedPalaceStyles = [.. Enums.GetShufflableList<PalaceStyle>().Where(i => i.IsGpStyle())];
                var weightedList = allowedPalaceStyles.Select(k => (k, palaceStyleWeights.GetValueOrDefault(k, 0))).ToList();
                var weightedRnd = new LinearWeightedRandom<PalaceStyle>(weightedList);
                if (!weightedRnd.HasPositiveWeight()) { throw new UserFacingException("Impossible Palace Style Weights", "At least one style must be included at above zero weight."); }
                properties.PalaceStyles[6] = weightedRnd.Next(r);
                }
            else
            {
                properties.PalaceStyles[6] = GpStyle;
            }
            Debug.Assert(!properties.PalaceStyles[6].IsMetastyle());

            if (NormalPalaceStyle.IsMetastyle())
            {
                Debug.Assert(NormalPalaceStyle == PalaceStyle.RANDOM_PER_PALACE || NormalPalaceStyle == PalaceStyle.RANDOM_ALL);
                allowedPalaceStyles = [.. Enums.GetShufflableList<PalaceStyle>().Where(i => i.NormalPalaceStyle())];
                var weightedList = allowedPalaceStyles.Select(k => (k, palaceStyleWeights.GetValueOrDefault(k, 0))).ToList();
                var weightedRnd = new LinearWeightedRandom<PalaceStyle>(weightedList);
                if (!weightedRnd.HasPositiveWeight()) { throw new UserFacingException("Impossible Palace Style Weights", "At least one style must be included at above zero weight."); }
                if (NormalPalaceStyle == PalaceStyle.RANDOM_PER_PALACE)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        properties.PalaceStyles[i] = weightedRnd.Next(r);
                }
            }
                else if (NormalPalaceStyle == PalaceStyle.RANDOM_ALL)
            {
                    PalaceStyle style = weightedRnd.Next(r);
            for (int i = 0; i < 6; i++)
            {
                        properties.PalaceStyles[i] = style;
                }
                }
            }
                else
                {
                Debug.Assert(!NormalPalaceStyle.IsMetastyle());
                for (int i = 0; i < 6; i++)
                {
                    properties.PalaceStyles[i] = NormalPalaceStyle;
                }
            }

            for (int i = 0; i < 7; i++)
            {
                if (IsVanillaEverythingPalace(i))
                {
                    properties.PalaceStyles[i] = PalaceStyle.VANILLA;
                }
            }

            properties.PalaceLengths = Palaces.RollPalaceLengths(this, properties, r);

            AssignItemPoolProps(properties, r);
            AssignPalaceItemCounts(properties, r);

            //Other starting attributes
            int startHeartsMin, startHeartsMax;
            if (startingHeartContainersMin == null)
            {
                startHeartsMin = r.Next(1, 9);
            }
            else
            {
                startHeartsMin = (int)startingHeartContainersMin;
            }
            if (startingHeartContainersMax == null)
            {
                startHeartsMax = r.Next(startHeartsMin, 9);
            }
            else
            {
                startHeartsMax = (int)startingHeartContainersMax;
            }
            properties.StartHearts = r.Next(startHeartsMin, startHeartsMax + 1);

            //+1/+2/+3
            if (maxHeartContainers == MaxHeartsOption.RANDOM)
            {
                properties.MaxHearts = r.Next(properties.StartHearts, 9);
            }
            else if ((int)maxHeartContainers <= 8)
            {
                properties.MaxHearts = (int)maxHeartContainers;
            }
            else
            {
                int additionalHearts = maxHeartContainers switch
                {
                    MaxHeartsOption.PLUS_ONE => 1,
                    MaxHeartsOption.PLUS_TWO => 2,
                    MaxHeartsOption.PLUS_THREE => 3,
                    MaxHeartsOption.PLUS_FOUR => 4,
                    _ => throw new ImpossibleException("Invalid heart container max configuration")
                };
                properties.MaxHearts = Math.Min(properties.StartHearts + additionalHearts, 8);
            }
            properties.MaxHearts = Math.Max(properties.MaxHearts, properties.StartHearts);

            int startMagicsMin, startMagicsMax;
            if (startingMagicContainersMin == null)
            {
                startMagicsMin = r.Next(1, 9);
            }
            else
            {
                startMagicsMin = (int)startingMagicContainersMin;
            }
            if (startingMagicContainersMax == null)
            {
                startMagicsMax = r.Next(startMagicsMin, 9);
            }
            else
            {
                startMagicsMax = (int)startingMagicContainersMax;
            }
            properties.StartMagicContainers = r.Next(startMagicsMin, startMagicsMax + 1);

            //Not settable yet
            properties.MaxMagicContainers = 8;
        } while (!properties.HasEnoughSpaceToAllocateItems());

        //Handle Fire
        Collectable RollLinkedFireSpell()
        {
            Collectable[] fireSpellOptions = POSSIBLE_LINKED_FIRE_SPELLS;
            if (removeFairy)
            {
                fireSpellOptions = fireSpellOptions.Except([Collectable.FAIRY_SPELL]).ToArray();
            }
            return fireSpellOptions[r.Next(fireSpellOptions.Length)];
        }
        switch (fireOption)
        {
            case FireOption.NORMAL:
                properties.LinkedFireSpell = null;
                properties.ReplaceFireWithDash = false;
                break;
            case FireOption.PAIR_WITH_RANDOM:
                properties.LinkedFireSpell = RollLinkedFireSpell();
                properties.ReplaceFireWithDash = false;
                break;
            case FireOption.REPLACE_WITH_DASH:
                properties.LinkedFireSpell = null;
                properties.ReplaceFireWithDash = true;
                break;
            case FireOption.RANDOM:
                switch (r.Next(3))
                {
                    case 0:
                        properties.LinkedFireSpell = null;
                        properties.ReplaceFireWithDash = false;
                        break;
                    case 1:
                        properties.LinkedFireSpell = RollLinkedFireSpell();
                        properties.ReplaceFireWithDash = false;
                        break;
                    case 2:
                        properties.LinkedFireSpell = null;
                        properties.ReplaceFireWithDash = true;
                        break;
                }
                break;
            default:
                throw new Exception("Illegal Fire option");
        }

        //If both stabs are random, use the classic weightings
        if (startingTechniques == StartingTechs.RANDOM)
        {
            switch (r.Next(7))
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    properties.StartWithDownstab = false;
                    properties.StartWithUpstab = false;
                    break;
                case 4:
                    properties.StartWithDownstab = true;
                    properties.StartWithUpstab = false;
                    break;
                case 5:
                    properties.StartWithDownstab = false;
                    properties.StartWithUpstab = true;
                    break;
                case 6:
                    properties.StartWithDownstab = true;
                    properties.StartWithUpstab = true;
                    break;
            }
        }
        else
        {
            properties.StartWithDownstab = startingTechniques.StartWithDownstab();
            properties.StartWithUpstab = startingTechniques.StartWithUpstab();
        }
        properties.SwapUpAndDownStab = swapUpAndDownStab ?? GetIndeterminateFlagValue(r);


        properties.StartLives = startingLives switch
        {
            StartingLives.Lives1 => 1,
            StartingLives.Lives2 => 2,
            StartingLives.Lives3 => 3,
            StartingLives.Lives4 => 4,
            StartingLives.Lives5 => 5,
            StartingLives.Lives8 => 8,
            StartingLives.Lives16 => 16,
            _ => r.Next(2, 6)
        };
        properties.PermanentBeam = permanentBeamSword;
        properties.UseCommunityText = useCommunityText;
        properties.StartAtk = startingAttackLevel;
        properties.StartingMagicLevel = startingMagicLevel;
        properties.StartLifeLvl = startingLifeLevel;
        properties.StartingLocation = startingLocation;

        //Overworld
        properties.ShuffleEncounters = shuffleEncounters ?? GetIndeterminateFlagValue(r);
        properties.AllowPathEnemies = allowUnsafePathEncounters;
        properties.IncludeLavaInEncounterShuffle = includeLavaInEncounterShuffle;
        properties.PalacesCanSwapContinent = palacesCanSwapContinents ?? GetIndeterminateFlagValue(r);
        properties.P7shuffle = shuffleGP ?? GetIndeterminateFlagValue(r);
        properties.HiddenPalace = hidePalace ?? GetIndeterminateFlagValue(r);
        properties.HiddenKasuto = hideKasuto ?? GetIndeterminateFlagValue(r);

        properties.EncounterRates = encounterRate;
        properties.ContinentConnections = continentConnectionType;
        properties.WestSize = westSize;
        properties.EastSize = eastSize;
        properties.DmSize = dmSize;
        properties.MazeSize = mazeSize;
        properties.BoulderBlockConnections = allowConnectionCavesToBeBlocked;

        //Climates
        if (westClimate == ClimateEnum.RANDOM)
        {
            List<ClimateEnum> westClimates = Enums.GetShufflableList<ClimateEnum>().Where(i => i.IsWestClimate() && !i.IsMetastyle()).ToList();
            properties.WestClimate = westClimates.Sample(r);
        }
        else if (westClimate == ClimateEnum.RANDOM_CUSTOM)
        {
            var keys = Enum.GetValues<ClimateEnum>().Where(c => c.IsWestClimate() && climateWeights.ContainsKey(c));
            var weightsList = keys.Select(k => (k, climateWeights[k])).ToList();
            var weightedRnd = new LinearWeightedRandom<ClimateEnum>(weightsList);
            if (!weightedRnd.HasPositiveWeight()) { throw new UserFacingException("Impossible Climate Weights", "At least one West climate must be included at above zero weight."); }
            properties.WestClimate = weightedRnd.Next(r);
        }
        else
        {
            properties.WestClimate = westClimate;
        }

        if (eastClimate == ClimateEnum.RANDOM)
        {
            List<ClimateEnum> eastClimates = Enums.GetShufflableList<ClimateEnum>().Where(i => i.IsEastClimate() && !i.IsMetastyle()).ToList();
            properties.EastClimate = eastClimates.Sample(r);
        }
        else if (eastClimate == ClimateEnum.RANDOM_CUSTOM)
        {
            var keys = Enum.GetValues<ClimateEnum>().Where(c => c.IsEastClimate() && climateWeights.ContainsKey(c));
            var weightsList = keys.Select(k => (k, climateWeights[k])).ToList();
            var weightedRnd = new LinearWeightedRandom<ClimateEnum>(weightsList);
            if (!weightedRnd.HasPositiveWeight()) { throw new UserFacingException("Impossible Climate Weights", "At least one East climate must be included at above zero weight."); }
            properties.EastClimate = weightedRnd.Next(r);
        }
        else
        {
            properties.EastClimate = eastClimate;
        }

        if (dmClimate == ClimateEnum.RANDOM)
        {
            List<ClimateEnum> dmClimates = Enums.GetShufflableList<ClimateEnum>().Where(i => i.IsDmClimate() && !i.IsMetastyle()).ToList();
            properties.DmClimate = dmClimates.Sample(r);
        }
        else if (dmClimate == ClimateEnum.RANDOM_CUSTOM)
        {
            var keys = Enum.GetValues<ClimateEnum>().Where(c => c.IsDmClimate() && climateWeights.ContainsKey(c));
            var weightsList = keys.Select(k => (k, climateWeights[k])).ToList();
            var weightedRnd = new LinearWeightedRandom<ClimateEnum>(weightsList);
            if (!weightedRnd.HasPositiveWeight()) { throw new UserFacingException("Impossible Climate Weights", "At least one Death Mountain climate must be included at above zero weight."); }
            properties.DmClimate = weightedRnd.Next(r);
        }
        else
        {
            properties.DmClimate = dmClimate;
        }

        properties.MazeRevealLocations = mazeRevealLocations;
        properties.LegacyVanillaShuffledLocations = legacyVanillaShuffledLocations;
        properties.ShuffleHidden = shuffleWhichLocationIsHidden ?? GetIndeterminateFlagValue(r);
        properties.CanWalkOnWaterWithBoots = goodBoots ?? GetIndeterminateFlagValue(r);
        properties.BagusWoods = generateBaguWoods ?? GetIndeterminateFlagValue(r);
        if (lessImportantLocationsOption == LessImportantLocationsOption.RANDOM)
        {
            properties.LessImportantLocationsOption = r.Next(3) switch
            {
                0 => LessImportantLocationsOption.HIDE,
                1 => LessImportantLocationsOption.ISOLATE,
                2 => LessImportantLocationsOption.REMOVE,
                _ => throw new ImpossibleException("Invalid LessImportantLocationsOption random option in Export")
            };
        }
        else
        {
            properties.LessImportantLocationsOption = LessImportantLocationsOption;
        }
        if(riverDevilBlockerOption == RiverDevilBlockerOption.RANDOM)
        {
            properties.RiverDevilBlockerOption = r.Next(3) switch
            {
                0 => RiverDevilBlockerOption.PATH,
                1 => RiverDevilBlockerOption.CAVE,
                2 => RiverDevilBlockerOption.SIEGE,
                _ => throw new ImpossibleException("Invalid RiverDevilBlockerOption random option in Export")
            };
        }
        else
        {
            properties.RiverDevilBlockerOption = riverDevilBlockerOption;
        }
        properties.EastRocks = eastRocks ?? GetIndeterminateFlagValue(r);
        properties.SaneCaves = RestrictConnectionCaveShuffle ?? GetIndeterminateFlagValue(r);

        //Palaces
        properties.StartGems = r.Next(palacesToCompleteMin, palacesToCompleteMax + 1);
        properties.RequireTbird = tBirdRequired ?? GetIndeterminateFlagValue(r);
        properties.DarkLinkMinDistance = GetDarkLinkMinDistance();
        properties.ShufflePalacePalettes = changePalacePallettes;
        properties.UpARestartsAtPalaces = restartAtPalacesOnGameOver;
        properties.Global5050JarDrop = global5050JarDrop ?? GetIndeterminateFlagValue(r);
        properties.ReduceDripperVariance = reduceDripperVariance;
        properties.RemoveTbird = removeTBird;
        properties.BossItem = randomizeBossItemDrop;

        //if all 3 room options are hard false, the seed can't generate. The UI tries to prevent this, but as a safety
        //if we get to this point, use vanilla rooms
        if(!((includeVanillaRooms ?? true) || (includev4_0Rooms ?? true) || (includev5_0Rooms ?? true)))
        {
            properties.AllowVanillaRooms = true;
        }
        while (!(properties.AllowVanillaRooms || properties.AllowV4Rooms || properties.AllowV5_0Rooms))
        {
            properties.AllowVanillaRooms = includeVanillaRooms ?? GetIndeterminateFlagValue(r);
            properties.AllowV4Rooms = includev4_0Rooms ?? GetIndeterminateFlagValue(r);
            properties.AllowV5_0Rooms = includev5_0Rooms ?? GetIndeterminateFlagValue(r);
        }
        properties.CustomRoomPool = customRoomPool;
        properties.BlockersAnywhere = blockingRoomsInAnyPalaceIncluded() && blockingRoomsInAnyPalace;
        properties.RemoveLongDeadEnds = removeLongDeadEndsIncluded() && removeLongDeadEnds;
        properties.IncludeExpertRooms = includeExpertRoomsIncluded() && includeExpertRooms;

        if (bossRoomsExitType == BossRoomsExitType.RANDOM_ALL)
        {
            BossRoomsExitType option = r.Next(2) switch
            {
                0 => BossRoomsExitType.OVERWORLD,
                1 => BossRoomsExitType.PALACE,
                _ => throw new Exception("Invalid BossRoomsExit")
            };
            for (int i = 0; i < 6; i++)
            {
                properties.BossRoomsExitToPalace[i] = option == BossRoomsExitType.PALACE;
            }
        }
        else if (bossRoomsExitType == BossRoomsExitType.RANDOM_PER_PALACE)
        {
            for (int i = 0; i < 6; i++)
            {
                BossRoomsExitType option = r.Next(2) switch
                {
                    0 => BossRoomsExitType.OVERWORLD,
                    1 => BossRoomsExitType.PALACE,
                    _ => throw new Exception("Invalid BossRoomsExit")
                };
                properties.BossRoomsExitToPalace[i] = option == BossRoomsExitType.PALACE;
            }
        }
        else
        {
            for (int i = 0; i < 6; i++)
            {
                properties.BossRoomsExitToPalace[i] = bossRoomsExitType == BossRoomsExitType.PALACE;
            }
        }
        properties.BossRoomsExitToPalace[6] = false;
        properties.PalaceDropStyle = palaceDropStyle;

        properties.NoDuplicateRooms = noDuplicateRoomsByEnemiesIncluded() && noDuplicateRoomsByEnemies;
        properties.NoDuplicateRoomsBySideview = noDuplicateRoomsByLayoutIncluded() && noDuplicateRoomsByLayout;
        properties.GeneratorsAlwaysMatch = generatorsAlwaysMatchIncluded() && generatorsAlwaysMatch;
        properties.HardBosses = hardBosses;
        properties.AggressiveTbird = aggressiveTbird;
        properties.RevealWalkthroughWalls = revealWalkthroughWalls;

        //Enemies
        properties.ShuffleEnemyHP = shuffleEnemyHP;
        properties.ShuffleBossHP = shuffleBossHP;
        properties.ShuffleEnemyStealExp = shuffleXPStealers;
        properties.ShuffleStealExpAmt = shuffleXPStolenAmount;
        properties.SwordImmunityOption = swordImmunityOption;
        properties.ShuffleOverworldEnemies = shuffleOverworldEnemies ?? GetIndeterminateFlagValue(r);
        properties.ShufflePalaceEnemies = shufflePalaceEnemies ?? GetIndeterminateFlagValue(r);
        properties.MixLargeAndSmallEnemies = mixLargeAndSmallEnemiesIncluded() && (mixLargeAndSmallEnemies ?? GetIndeterminateFlagValue(r));
        properties.DripperEnemyOption = dripperEnemyOption;
        properties.SpellEnemy = randomizeSpellSpellEnemy ?? GetIndeterminateFlagValue(r);
        properties.ShuffleEnemyPalettes = shuffleSpritePalettes;
        properties.EnemyXPDrops = enemyXPDrops;

        //Levels
        properties.ShuffleAtkExp = shuffleAttackExperience;
        properties.ShuffleMagicExp = shuffleMagicExperience;
        properties.ShuffleLifeExp = shuffleLifeExperience;
        properties.AttackEffectiveness = attackEffectiveness;
        properties.MagicEffectiveness = magicEffectiveness;
        properties.LifeEffectiveness = lifeEffectiveness;
        properties.ShuffleLifeRefill = shuffleLifeRefillAmount;
        properties.ShuffleSpellLocations = shuffleSpellLocations ?? GetIndeterminateFlagValue(r);
        properties.DisableMagicRecs = disableMagicContainerRequirements ?? GetIndeterminateFlagValue(r);
        properties.AttackCap = attackLevelCap;
        properties.MagicCap = magicLevelCap;
        properties.LifeCap = lifeLevelCap;
        properties.ScaleLevels = scaleLevelRequirementsToCapIncluded() && scaleLevelRequirementsToCap;

        //Items
        //properties affecting item pool/location count are set in AssignItemPoolProps()
        properties.RandomizeSmallItems = shuffleSmallItems;
        properties.ExtraKeys = palacesContainExtraKeys ?? GetIndeterminateFlagValue(r);
        properties.NewKasutoBasementRequirement = randomizeNewKasutoJarRequirements ? r.Next(5,8) : 6;
        properties.FastItemPickup = fastItemPickup;
        properties.AllowImportantItemDuplicates = allowImportantItemDuplicates;
        properties.PbagItemShuffle = includePBagCavesInItemShuffleIncluded() && (includePBagCavesInItemShuffle ?? GetIndeterminateFlagValue(r));
        properties.ShufflePbagXp = shufflePBagAmounts ?? GetIndeterminateFlagValue(r);

        if (westBiome is Biome.VANILLA_EVERYTHING ||
            dmBiome is Biome.VANILLA_EVERYTHING ||
            eastBiome is Biome.VANILLA_EVERYTHING ||
            mazeBiome is Biome.VANILLA_EVERYTHING)
        {
            // global
            properties.FastItemPickup = false;
        }

        if (westBiome is Biome.VANILLA_EVERYTHING ||
            eastBiome is Biome.VANILLA_EVERYTHING)
        {
            // town overrides
            properties.ShuffleSpellLocations = false;
        }
        if (westBiome is Biome.VANILLA_EVERYTHING ||
            eastBiome is Biome.VANILLA_EVERYTHING ||
            mazeBiome is Biome.VANILLA_EVERYTHING)
        {
            // palace overrides
            properties.BossItem = false;
        }
        if (eastBiome == Biome.VANILLA_EVERYTHING)
        {
            properties.HiddenPalace = true;
            properties.HiddenKasuto = true;
            properties.NewKasutoBasementRequirement = 7;
        }


        //Drops
        properties.ShuffleItemDropFrequency = shuffleItemDropFrequency;
        if (randomizeDrops)
        {
            do
            {
                properties.Smallbluejar = !smallEnemiesCanDropBlueJar && randomizeDrops ? r.Next(2) == 1 : smallEnemiesCanDropBlueJar;
                properties.Smallredjar = !smallEnemiesCanDropRedJar && randomizeDrops ? r.Next(2) == 1 : smallEnemiesCanDropRedJar;
                properties.Small50 = !smallEnemiesCanDropSmallBag && randomizeDrops ? r.Next(2) == 1 : smallEnemiesCanDropSmallBag;
                properties.Small100 = !smallEnemiesCanDropMediumBag && randomizeDrops ? r.Next(2) == 1 : smallEnemiesCanDropMediumBag;
                properties.Small200 = !smallEnemiesCanDropLargeBag && randomizeDrops ? r.Next(2) == 1 : smallEnemiesCanDropLargeBag;
                properties.Small500 = !smallEnemiesCanDropXLBag && randomizeDrops ? r.Next(2) == 1 : smallEnemiesCanDropXLBag;
                properties.Small1up = !smallEnemiesCanDrop1up && randomizeDrops ? r.Next(2) == 1 : smallEnemiesCanDrop1up;
                properties.Smallkey = !smallEnemiesCanDropKey && randomizeDrops ? r.Next(2) == 1 : smallEnemiesCanDropKey;
            } while (properties is { Smallbluejar: false, Smallredjar: false, Small50: false, Small100: false, Small200: false, Small500: false, Small1up: false, Smallkey: false });
        }
        else
        {
            properties.Smallbluejar = smallEnemiesCanDropBlueJar;
            properties.Smallredjar = smallEnemiesCanDropRedJar;
            properties.Small50 = smallEnemiesCanDropSmallBag;
            properties.Small100 = smallEnemiesCanDropMediumBag;
            properties.Small200 = smallEnemiesCanDropLargeBag;
            properties.Small500 = smallEnemiesCanDropXLBag;
            properties.Small1up = smallEnemiesCanDrop1up;
            properties.Smallkey = smallEnemiesCanDropKey;
        }
        if (randomizeDrops)
        {
            do
            {
                properties.Largebluejar = !largeEnemiesCanDropBlueJar && randomizeDrops ? r.Next(2) == 1 : largeEnemiesCanDropBlueJar;
                properties.Largeredjar = !largeEnemiesCanDropRedJar && randomizeDrops ? r.Next(2) == 1 : largeEnemiesCanDropRedJar;
                properties.Large50 = !largeEnemiesCanDropSmallBag && randomizeDrops ? r.Next(2) == 1 : largeEnemiesCanDropSmallBag;
                properties.Large100 = !largeEnemiesCanDropMediumBag && randomizeDrops ? r.Next(2) == 1 : largeEnemiesCanDropMediumBag;
                properties.Large200 = !largeEnemiesCanDropLargeBag && randomizeDrops ? r.Next(2) == 1 : largeEnemiesCanDropLargeBag;
                properties.Large500 = !largeEnemiesCanDropXLBag && randomizeDrops ? r.Next(2) == 1 : largeEnemiesCanDropXLBag;
                properties.Large1up = !largeEnemiesCanDrop1up && randomizeDrops ? r.Next(2) == 1 : largeEnemiesCanDrop1up;
                properties.Largekey = !largeEnemiesCanDropKey && randomizeDrops ? r.Next(2) == 1 : largeEnemiesCanDropKey;
            } while (properties is { Largebluejar: false, Largeredjar: false, Large50: false, Large100: false, Large200: false, Large500: false, Large1up: false, Largekey: false });
        }
        else
        {
            properties.Largebluejar = largeEnemiesCanDropBlueJar;
            properties.Largeredjar = largeEnemiesCanDropRedJar;
            properties.Large50 = largeEnemiesCanDropSmallBag;
            properties.Large100 = largeEnemiesCanDropMediumBag;
            properties.Large200 = largeEnemiesCanDropLargeBag;
            properties.Large500 = largeEnemiesCanDropXLBag;
            properties.Large1up = largeEnemiesCanDrop1up;
            properties.Largekey = largeEnemiesCanDropKey;
        }
        properties.StandardizeDrops = standardizeDrops;
        properties.RandomizeDrops = randomizeDrops;

        //Hints
        properties.SpellItemHints = enableSpellItemHints ?? GetIndeterminateFlagValue(r);
        properties.HelpfulHints = enableHelpfulHints ?? GetIndeterminateFlagValue(r);
        properties.TownNameHints = enableTownNameHints ?? GetIndeterminateFlagValue(r);

        //Misc.
        properties.BeepThreshold = beepThreshold switch
        {
            //Normal
            BeepThreshold.Normal => 0x20,
            //Half Speed
            BeepThreshold.HalfBar => 0x10,
            //Quarter Speed
            BeepThreshold.QuarterBar => 0x08,
            //Off
            BeepThreshold.TwoBars => 0x40,
            _ => 0x20
        };
        properties.BeepFrequency = beepFrequency switch
        {
            //Normal
            BeepFrequency.Normal => 0x30,
            //Half Speed
            BeepFrequency.HalfSpeed => 0x60,
            //Quarter Speed
            BeepFrequency.QuarterSpeed => 0xC0,
            //Off
            BeepFrequency.Off => 0,
            _ => 0x30
        };
        properties.JumpAlwaysOn = jumpAlwaysOn;
        properties.DashAlwaysOn = dashAlwaysOn;
        properties.FasterDashFairy = fasterDashFairyIncluded() && fasterDashFairy;
        properties.FastCast = fastSpellCasting;
        properties.BeamSprite = beamSprite;
        properties.DisableMusic = disableMusic;
        properties.RandomizeMusic = randomizeMusic;
        properties.MixCustomAndOriginalMusic = mixCustomAndOriginalMusic;
        properties.IncludeDiverseMusic = includeDiverseMusic;
        properties.DisableUnsafeMusic = disableUnsafeMusic;
        properties.CharSprite = sprite;
        properties.ChangeItemSprites = changeItemSprites;
        properties.TunicColor = tunic;
        properties.SkinTone = skinTone;
        properties.OutlineColor = tunicOutline;
        properties.ShieldColor = shieldTunic;
        properties.UpAC1 = upAOnController1;
        properties.RemoveFlashing = removeFlashing;
        //Removed the option to select this for now.
        properties.UseCustomRooms = false;
        properties.DisableHUDLag = disableHUDLag;
        properties.RandomizeKnockback = randomizeKnockback;

        //"Server" side validation
        //This is a replication of a bunch of logic from the UI so that configurations from sources other than the UI (YAML?)
        //or indeterminately generated properties still correspond to sanity. Without this you get randomly ungeneratable seeds.

        bool rerollPalaceItemRoomCounts = false;

        if (!properties.ShuffleEncounters)
        {
            properties.AllowPathEnemies = false;
            properties.IncludeLavaInEncounterShuffle = false;
        }

        if(properties.IncludeSwordTechsInShuffle)
        {
            properties.SwapUpAndDownStab = false;
        }

        if (properties is { ShuffleOverworldEnemies: false, ShufflePalaceEnemies: false })
        {
            properties.MixLargeAndSmallEnemies = false;
        }

        if (!properties.ShufflePalaceItems || !properties.ShuffleOverworldItems)
        {
            properties.MixOverworldPalaceItems = false;
            rerollPalaceItemRoomCounts = true;
        }

        if (!properties.ShuffleOverworldItems)
        {
            properties.PbagItemShuffle = false;
        }

        if (properties.RequireTbird)
        {
            properties.RemoveTbird = false;
        }

        //#180 Remove tbird doesn't currently work with vanilla, so make sure even if it comes up on random it works properly.
        if (properties.PalaceStyles[6] == PalaceStyle.VANILLA)
        {
            properties.RemoveTbird = false;
        }

        if (!properties.PalacesCanSwapContinent)
        {
            properties.P7shuffle = false;
        }

        if (properties.StartWithSpellItems)
        {
            properties.SpellItemHints = false;
        }

        //if (eastBiome.SelectedIndex == 0 || (hiddenPalaceList.SelectedIndex == 0 && hideKasutoList.SelectedIndex == 0))
        if ((properties.EastBiome is Biome.VANILLA or Biome.VANILLA_EVERYTHING) || properties is { HiddenPalace: false, HiddenKasuto: false })
        {
            properties.ShuffleHidden = false;
        }

        if (properties.WestBiome.UsesVanillaMap())
        {
            properties.BagusWoods = false;
        }

        if (properties.ReplaceFireWithDash)
        {
            Debug.Assert(properties.LinkedFireSpell == null);
        }

        //If spells are in the shuffle pool, shuffle spells means nothing, so diable it
        if(properties.IncludeSpellsInShuffle)
        {
            properties.ShuffleSpellLocations = false;
        }

        //Same principle with sword techs in the pool meaning swap stabs is meaningless.
        if (properties.IncludeSwordTechsInShuffle)
        {
            properties.SwapUpAndDownStab = false;
        }
    
        if(rerollPalaceItemRoomCounts)
        {
            do
            {
                AssignPalaceItemCounts(properties, r);
            }
            while (!properties.HasEnoughSpaceToAllocateItems());
        }

        return properties;
    }

    private void AssignBiome(RandomizerProperties properties, Random r)
    {
        if (westBiome == Biome.RANDOM || westBiome == Biome.RANDOM_NO_VANILLA || westBiome == Biome.RANDOM_NO_VANILLA_OR_SHUFFLE)
        {
            int shuffleLimit = westBiome switch
            {
                Biome.RANDOM => 7,
                Biome.RANDOM_NO_VANILLA => 6,
                Biome.RANDOM_NO_VANILLA_OR_SHUFFLE => 5,
                _ => throw new ImpossibleException()
            };
            properties.WestBiome = r.Next(shuffleLimit) switch
            {
                0 => Biome.VANILLALIKE,
                1 => Biome.ISLANDS,
                2 => r.Next(2) == 1 ? Biome.CANYON : Biome.DRY_CANYON,
                3 => Biome.CALDERA,
                4 => Biome.MOUNTAINOUS,
                5 => Biome.VANILLA_SHUFFLE,
                6 => Biome.VANILLA,
                _ => throw new Exception("Invalid Biome")
            };
        }
        else if (westBiome == Biome.RANDOM_CUSTOM)
        {
            var keys = Enum.GetValues<Biome>().Where(b => b.IsWestBiome() && biomeWeights.ContainsKey(b));
            var westWeights = keys.Select(k => (k, biomeWeights[k])).ToList();
            var weightedRnd = new LinearWeightedRandom<Biome>(westWeights);
            if (!weightedRnd.HasPositiveWeight()) { throw new UserFacingException("Impossible Biome Weights", "At least one West biome must be included at above zero weight."); }
            Biome b = weightedRnd.Next(r);
            if (b == Biome.CANYON) { b = r.Next(2) == 0 ? Biome.CANYON : Biome.DRY_CANYON; }
            properties.WestBiome = b;
        }
        else if (westBiome == Biome.CANYON)
        {
            properties.WestBiome = r.Next(2) == 0 ? Biome.CANYON : Biome.DRY_CANYON;
        }
        else
        {
            properties.WestBiome = westBiome;
        }

        if (eastBiome.IsMetastyle())
        {
            do
            {
                if (eastBiome == Biome.RANDOM || eastBiome == Biome.RANDOM_NO_VANILLA || eastBiome == Biome.RANDOM_NO_VANILLA_OR_SHUFFLE)
                {
                    int shuffleLimit = eastBiome switch
                    {
                        Biome.RANDOM => 7,
                        Biome.RANDOM_NO_VANILLA => 6,
                        Biome.RANDOM_NO_VANILLA_OR_SHUFFLE => 5,
                        _ => throw new ImpossibleException()
                    };
                    properties.EastBiome = r.Next(shuffleLimit) switch
                    {
                        0 => Biome.VANILLALIKE,
                        1 => Biome.ISLANDS,
                        2 => r.Next(2) == 1 ? Biome.CANYON : Biome.DRY_CANYON,
                        3 => Biome.VOLCANO,
                        4 => Biome.MOUNTAINOUS,
                        5 => Biome.VANILLA_SHUFFLE,
                        6 => Biome.VANILLA,
                        _ => throw new Exception("Invalid Biome")
                    };
                }
                else if (eastBiome == Biome.RANDOM_CUSTOM)
                {
                    var keys = Enum.GetValues<Biome>().Where(b => b.IsEastBiome() && biomeWeights.ContainsKey(b));
                    var eastWeights = keys.Select(k => (k, biomeWeights[k])).ToList();
                    var weightedRnd = new LinearWeightedRandom<Biome>(eastWeights);
                    if (!weightedRnd.HasPositiveWeight()) { throw new UserFacingException("Impossible Biome Weights", "At least one East biome must be included at above zero weight."); }
                    Biome b = weightedRnd.Next(r);
                    if (b == Biome.CANYON) { b = r.Next(2) == 0 ? Biome.CANYON : Biome.DRY_CANYON; }
                    properties.EastBiome = b;
                }
                // Don't allow random-West and random-East to roll the same biome
            } while (westBiome.IsMetastyle() && properties.WestBiome.IsSimilar(properties.EastBiome));
        }
        else if (eastBiome == Biome.CANYON)
        {
            properties.EastBiome = r.Next(2) == 0 ? Biome.CANYON : Biome.DRY_CANYON;
        }
        else
        {
            properties.EastBiome = eastBiome;
        }
        if (dmBiome == Biome.RANDOM || dmBiome == Biome.RANDOM_NO_VANILLA || dmBiome == Biome.RANDOM_NO_VANILLA_OR_SHUFFLE)
        {
            int shuffleLimit = dmBiome switch
            {
                Biome.RANDOM => 7,
                Biome.RANDOM_NO_VANILLA => 6,
                Biome.RANDOM_NO_VANILLA_OR_SHUFFLE => 5,
                _ => throw new ImpossibleException()
            };
            properties.DmBiome = r.Next(shuffleLimit) switch
            {
                0 => Biome.VANILLALIKE,
                1 => Biome.ISLANDS,
                2 => r.Next(2) == 1 ? Biome.CANYON : Biome.DRY_CANYON,
                3 => Biome.CALDERA,
                4 => Biome.MOUNTAINOUS,
                5 => Biome.VANILLA_SHUFFLE,
                6 => Biome.VANILLA,
                _ => throw new Exception("Invalid Biome")
            };
        }
        else if (dmBiome == Biome.RANDOM_CUSTOM)
        {
            var keys = Enum.GetValues<Biome>().Where(b => b.IsDmBiome() && biomeWeights.ContainsKey(b));
            var dmWeights = keys.Select(k => (k, biomeWeights[k])).ToList();
            var weightedRnd = new LinearWeightedRandom<Biome>(dmWeights);
            if (!weightedRnd.HasPositiveWeight()) { throw new UserFacingException("Impossible Biome Weights", "At least one Death Mountain biome must be included at above zero weight."); }
            Biome b = weightedRnd.Next(r);
            if (b == Biome.CANYON) { b = r.Next(2) == 0 ? Biome.CANYON : Biome.DRY_CANYON; }
            properties.DmBiome = b;
        }
        else if (dmBiome == Biome.CANYON)
        {
            properties.DmBiome = r.Next(2) == 0 ? Biome.CANYON : Biome.DRY_CANYON;
        }
        else
        {
            properties.DmBiome = dmBiome;
        }
        if (mazeBiome == Biome.RANDOM || mazeBiome == Biome.RANDOM_CUSTOM)
        {
            properties.MazeBiome = r.Next(3) switch
            {
                0 => Biome.VANILLA,
                1 => Biome.VANILLA_SHUFFLE,
                2 => Biome.VANILLALIKE,
                _ => throw new Exception("Invalid Biome")
            };
        }
        else
        {
            properties.MazeBiome = mazeBiome;
        }
    }

    //Set properties that may affect item pool/location count
    private void AssignItemPoolProps(RandomizerProperties properties, Random r)
    {
        properties.ShuffleOverworldItems = shuffleOverworldItems ?? GetIndeterminateFlagValue(r);
        properties.ShufflePalaceItems = shufflePalaceItems ?? GetIndeterminateFlagValue(r);
        properties.MixOverworldPalaceItems = mixOverworldAndPalaceItemsIncluded() && (mixOverworldAndPalaceItems ?? GetIndeterminateFlagValue(r));
        properties.IncludeSpellsInShuffle = includeSpellsInShuffle ?? GetIndeterminateFlagValue(r);
        properties.QuestItemChainsAllowed = questItemChainsAllowedIncluded() && questItemChainsAllowed;
        properties.IncludeSwordTechsInShuffle = includeSwordTechsInShuffle ?? GetIndeterminateFlagValue(r);
        properties.IncludeQuestItemsInShuffle = includeQuestItemsInShuffle ?? GetIndeterminateFlagValue(r);
        properties.IncludeBagusNoteInShuffle = includeBagusNoteInShuffle ?? GetIndeterminateFlagValue(r);
        properties.StartWithSpellItems = removeSpellItems ?? GetIndeterminateFlagValue(r);
        properties.TownQuestLocationsAreMinorItems = townQuestLocationsAreMinorItemsIncluded() && townQuestLocationsAreMinorItems;
    }

    public void AssignPalaceItemCounts(RandomizerProperties properties, Random r)
    {
        //I'm not sure whether I like the bias introduced in generating random values and then capping them
        //vs just determining min/max ranges and fair rolling between them. Keeping it for now.
        int[] palaceItemRoomsMin = palaceItemRoomCount == PalaceItemRoomCount.RANDOM_INCLUDE_ZERO ? [0, 0, 0, 0, 0, 0] : [1, 1, 1, 1, 1, 1];
        int[] palaceItemRoomsMax = GetPalaceItemRoomMaxCounts(palaceItemRoomCount, properties.PalaceLengths);
        int[] palaceItemRoomsLimit = GetPalaceItemRoomLimits(properties.PalaceStyles);

        switch (palaceItemRoomCount)
        {
            case PalaceItemRoomCount.RANDOM_INCLUDE_ZERO:
            case PalaceItemRoomCount.RANDOM_NOT_ZERO:
                for (int i = 0; i < 6; i++)
                {
                    var roll = r.Next(palaceItemRoomsMin[i], palaceItemRoomsMax[i] + 1);
                    var capped = Math.Min(roll, palaceItemRoomsLimit[i]);
                    properties.PalaceItemRoomCounts[i] = capped;
                }
                properties.UsePalaceItemRoomCountIndicator = true;
                break;
            default:
                properties.PalaceItemRoomCounts = Enumerable.Repeat((int)palaceItemRoomCount, 6).ToArray();
                properties.UsePalaceItemRoomCountIndicator = false;
                break;
        }

        //If shuffle palace items is off, the minimum number of palace rooms for a palace must be 1
        //otherwise it is impossible to place the palace items.
        if (!properties.ShufflePalaceItems)
        {
            for (int i = 0; i < 6; i++)
            {
                properties.PalaceItemRoomCounts[i] = int.Max(properties.PalaceItemRoomCounts[i], 1);
            }
        }

        for (int i = 0; i < 6; i++)
        {
            if (IsVanillaEverythingPalace(i))
            {
                properties.PalaceItemRoomCounts[i] = 1;
            }
        }

        //If mixed palace/overworld items is off, places must contain at least 6 items total so there is a place to put the items
        if (!properties.MixOverworldPalaceItems)
        {
            while (properties.PalaceItemRoomCounts.Sum() < 6)
            {
                int i = r.Next(6);
                if (properties.PalaceItemRoomCounts[i] < palaceItemRoomsLimit[i])
                {
                    properties.PalaceItemRoomCounts[i]++;
                }
            }
        }
    }

    /// Note: this value can be limited by other things, like the style being Vanilla
    public static int[] GetPalaceItemRoomMaxCounts(PalaceItemRoomCount palaceItemRoomCount, int[] palaceLengths)
    {
        // To keep weighting consistent between 0-item and 1-item rooms across
        // Standard, Max Rando, etc., we roll the room count the same way
        // regardless of palace type. This also reduces how much (imperfect)
        // information the item count reveals about the palace style.
        return palaceItemRoomCount switch
        {
            PalaceItemRoomCount.ZERO => [0, 0, 0, 0, 0, 0],
            PalaceItemRoomCount.ONE => [1, 1, 1, 1, 1, 1],
            PalaceItemRoomCount.TWO => [2, 2, 2, 2, 2, 2],
            PalaceItemRoomCount.RANDOM_NOT_ZERO or
            PalaceItemRoomCount.RANDOM_INCLUDE_ZERO =>
                palaceLengths.Select(len => len switch
                {
                    < 16 => 1,
                    < 26 => 2,
                    _ => 3,
                }).ToArray(),
            _ => throw new NotImplementedException(),
        };
    }

    public static int[] GetPalaceItemRoomLimits(PalaceStyle[] palaceStyles)
    {
        // We have to cap the number of item rooms for some styles.
        // Vanilla, because it doesn't make sense to change any rooms,
        // and Vanilla Shuffled, because it crashes with more than two.
        return palaceStyles.Select(style => style switch
        {
            PalaceStyle.VANILLA => 1,
            PalaceStyle.SHUFFLED => 2,
            _ => int.MaxValue,
        }).ToArray();
    }

    /// Let the user know when their combination of flags will not be
    /// possible to achieve.
    /// 
    /// For indeterminate options, we will only check that the best case
    /// scenario should work.
    public void CheckForFlagConflicts()
    {
        int requiredOverworldMinorItemReplacements = 0;
        int requiredPalaceMinorItemReplacements = 0;
        var heartsInPool = maxHeartContainers.HeartsInPool(startingHeartContainersMax ?? 8);
        requiredOverworldMinorItemReplacements += heartsInPool - 4;
        if (startingMagicContainersMax != null && startingMagicContainersMax < 4)
        {
            requiredOverworldMinorItemReplacements += 4 - startingMagicContainersMax.Value;
        }
        if (townQuestLocationsAreMinorItems)
        {
            if (includeQuestItemsInShuffle == true)
            {
                requiredOverworldMinorItemReplacements += 2;
            }
            if (includeSwordTechsInShuffle == true)
            {
                requiredOverworldMinorItemReplacements += 2;
            }
        }

        (int overworldMinorItemCount, int palaceMinorItemCount) = CountPossibleMinorItems();
        if (mixOverworldAndPalaceItems == true)
        {
            if (overworldMinorItemCount + palaceMinorItemCount < requiredOverworldMinorItemReplacements + requiredPalaceMinorItemReplacements)
            {
                throw new UserFacingException("Impossible Item Flags", "Not enough possible item locations for removed palace items.\n\nAdd more starting items or more palace items.");
            }
        }
        else
        {
            if (overworldMinorItemCount < requiredOverworldMinorItemReplacements)
            {
                throw new UserFacingException("Impossible Item Flags", "Not enough possible item locations for overworld items.\n\nAdd more starting Heart/Magic containers.");
            }
            if (palaceMinorItemCount < requiredPalaceMinorItemReplacements)
            {
                throw new UserFacingException("Impossible Item Flags", "Not enough possible item locations for removed palace items.\n\nAdd more starting items or more palace items.");
            }
        }

        if (!customRoomPool && (noDuplicateRoomsByLayout || noDuplicateRoomsByEnemies))
        {
            // if current palace generation logic changes, this should be updated
            int potentialRoomPools = 0;
            if (includeVanillaRooms != false) { potentialRoomPools++; }
            if (includev4_0Rooms != false) { potentialRoomPools++; }
            if (includev5_0Rooms != false) { potentialRoomPools++; }
            if (potentialRoomPools < 2)
            {
                throw new UserFacingException("Incompatible Palace Flags", "Not enough palace rooms in the pool.\n\nUnder the Palaces tab, include more room groups or disable No Duplicate Rooms.");
            }
        }
    }

    public static bool IsIntegerType(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte => true,
            TypeCode.Int16 => true,
            TypeCode.Int32 => true,
            TypeCode.Int64 => true,
            TypeCode.SByte => true,
            TypeCode.UInt16 => true,
            TypeCode.UInt32 => true,
            TypeCode.UInt64 => true,
            _ => false
        };
    }

    public string GetRoomsFile()
    {
        return useCustomRooms ? "CustomRooms.json" : "PalaceRooms.json";
    }

    private bool GetIndeterminateFlagValue(Random r)
    {
        return r.NextDouble() < indeterminateOptionRate switch
        {
            IndeterminateOptionRate.QUARTER => .25,
            IndeterminateOptionRate.HALF => .50,
            IndeterminateOptionRate.THREE_QUARTERS => .75,
            IndeterminateOptionRate.NINETY_PERCENT => .90,
            _ => throw new Exception("Unrecognized IndeterminateOptionRate")
        };
    }

    public bool StartsWithCollectable(Collectable collectable)
    {
        return collectable switch
        {
            Collectable.SHIELD_SPELL => startWithShield,
            Collectable.JUMP_SPELL => startWithJump,
            Collectable.LIFE_SPELL => startWithLife,
            Collectable.FAIRY_SPELL => startWithFairy,
            Collectable.FIRE_SPELL => startWithFire,
            Collectable.DASH_SPELL => startWithFire,
            Collectable.REFLECT_SPELL => startWithReflect,
            Collectable.SPELL_SPELL => startWithSpellSpell,
            Collectable.THUNDER_SPELL => startWithThunder,
            Collectable.CANDLE => startWithCandle,
            Collectable.GLOVE => startWithGlove,
            Collectable.RAFT => startWithRaft,
            Collectable.BOOTS => startWithBoots,
            Collectable.FLUTE => startWithFlute,
            Collectable.CROSS => startWithCross,
            Collectable.HAMMER => startWithHammer,
            Collectable.MAGIC_KEY => startWithMagicKey,
            _ => throw new ImpossibleException("Unrecognized collectable")
        };
    }

    private List<Collectable> ShuffleStartingCollectables(Collectable[] possibleCollectables, StartingResourceLimit limit, bool shuffleRandom, 
        RandomizerProperties properties, Random r)
    {
        int itemLimit = limit.AsInt();

        List<Collectable> startingItems = [];

        Collectable[] randomPossibleCollectables = new Collectable[possibleCollectables.Length];
        Array.Copy(possibleCollectables, randomPossibleCollectables, possibleCollectables.Length);
        r.Shuffle(randomPossibleCollectables);
        foreach (Collectable collectable in randomPossibleCollectables)
        {
            if (startingItems.Count >= itemLimit)
            {
                break;
            }
            if (StartsWithCollectable(collectable))
            {
                startingItems.Add(collectable);
            }
        }

        if (shuffleRandom)
        {
            foreach (Collectable collectable in randomPossibleCollectables)
            {
                if (startingItems.Count >= itemLimit)
                {
                    break;
                }
                if (!StartsWithCollectable(collectable) && !properties.RemoveItems.Contains(collectable))
                {
                    if (r.Next(4) == 0)
                    {
                        startingItems.Add(collectable);
                    }
                }
            }
        }

        foreach (Collectable collectable in randomPossibleCollectables)
        {
            properties.SetStartingCollectable(collectable, startingItems.Contains(collectable));
        }

        return startingItems;
    }

    private (int, int) CountPossibleMinorItems()
    {
        int overworldMinorItemCount = 0;
        int palaceMinorItemCount = 0;
        int mustExistContainers = 0;

        if (shuffleOverworldItems != false && westBiome is not Biome.VANILLA_EVERYTHING)
        {
            overworldMinorItemCount += 1;
        }
        else
        {
            mustExistContainers += 2;
        }
        if (shuffleOverworldItems != false && eastBiome is not Biome.VANILLA_EVERYTHING)
        {
            overworldMinorItemCount += 2;
        }
        else
        {
            mustExistContainers += 2;
        }

        int overworldStartMinorItemCount = 0;
        int palaceStartMinorItemCount = 0;
        if (shufflePalaceItems != false && westBiome is not Biome.VANILLA_EVERYTHING)
        {
            palaceStartMinorItemCount += shuffleStartingItems || startWithCandle ? 1 : 0;
            palaceStartMinorItemCount += shuffleStartingItems || startWithGlove ? 1 : 0;
            palaceStartMinorItemCount += shuffleStartingItems || startWithRaft ? 1 : 0;
        }
        if (shuffleOverworldItems != false && dmBiome is not Biome.VANILLA_EVERYTHING)
        {
            overworldStartMinorItemCount += shuffleStartingItems || startWithHammer ? 1 : 0;
        }
        if (shufflePalaceItems != false && mazeBiome is not Biome.VANILLA_EVERYTHING)
        {
            palaceStartMinorItemCount += shuffleStartingItems || startWithBoots ? 1 : 0;
        }
        if (shufflePalaceItems != false && eastBiome is not Biome.VANILLA_EVERYTHING)
        {
            palaceStartMinorItemCount += shuffleStartingItems || startWithFlute ? 1 : 0;
            palaceStartMinorItemCount += shuffleStartingItems || startWithCross ? 1 : 0;
        }
        if (shuffleOverworldItems != false && eastBiome is not Biome.VANILLA_EVERYTHING)
        {
            overworldStartMinorItemCount += shuffleStartingItems || startWithMagicKey ? 1 : 0;
        }
        int startItemsOverflow = Math.Min(0, palaceStartMinorItemCount + overworldStartMinorItemCount - (shuffleStartingItems ? startItemsLimit.AsInt() : 0));
        // overflow distribution between overworld items and palace items could be improved here (but needed for when the pools are not mixed)
        overworldMinorItemCount += Math.Max(0, overworldStartMinorItemCount - startItemsOverflow); 
        palaceMinorItemCount += Math.Max(0, palaceStartMinorItemCount - startItemsOverflow);

        int startSpellsOverflow = 0;
        if (includeSpellsInShuffle ?? true)
        {
            overworldStartMinorItemCount = 0;

            overworldStartMinorItemCount += shuffleStartingSpells || startWithShield ? 1 : 0;
            overworldStartMinorItemCount += shuffleStartingSpells || startWithJump ? 1 : 0;
            overworldStartMinorItemCount += shuffleStartingSpells || startWithLife ? 1 : 0;
            overworldStartMinorItemCount += shuffleStartingSpells || startWithFairy ? 1 : 0;
            overworldStartMinorItemCount += shuffleStartingSpells || startWithFire ? 1 : 0;
            overworldStartMinorItemCount += shuffleStartingSpells || startWithReflect ? 1 : 0;
            overworldStartMinorItemCount += shuffleStartingSpells || startWithSpellSpell ? 1 : 0;
            overworldStartMinorItemCount += shuffleStartingSpells || startWithThunder ? 1 : 0;

            startSpellsOverflow = Math.Min(overworldStartMinorItemCount - (shuffleStartingSpells ? startSpellsLimit.AsInt() : 0), 0);
            overworldMinorItemCount += Math.Max(0, startSpellsOverflow - startSpellsOverflow);
        }


        if (includeSwordTechsInShuffle ?? true)
        {
            overworldStartMinorItemCount += startingTechniques switch
            {
                StartingTechs.DOWNSTAB => 1,
                StartingTechs.UPSTAB => 1,
                StartingTechs.BOTH => 2,
                StartingTechs.RANDOM => 2,
                StartingTechs.NONE => 0,
                _ => throw new Exception("Unrecognized starting tech option")
            };
        }

        if (8 - (startingHeartContainersMax ?? 4) < mustExistContainers)
        {
            throw new UserFacingException("Heart Container Mismatch", "Unshuffled West and East must each contain their two vanilla Heart Containers. Your starting container configuration does not allow this.");
        }
        if (8 - (startingMagicContainersMax ?? 4) < mustExistContainers)
        {
            throw new UserFacingException("Magic Container Mismatch", "Unshuffled West and East must each contain their two vanilla Magic Containers. Your starting container configuration does not allow this.");
        }
        var heartsInPool = maxHeartContainers.HeartsInPool(startingHeartContainersMax ?? 8);
        int heartContainerReplacementSmallItemsCount = 4 - heartsInPool;
        overworldMinorItemCount += heartContainerReplacementSmallItemsCount;
        overworldMinorItemCount += 4 - (8 - (startingMagicContainersMax ?? 8));

        var palaceLengthsMax = Palaces.VANILLA_LENGTHS[..6].Select(n => Palaces.MaxLengthRoll(n, normalPalaceLength)).ToArray();
        var itemCountMaxRoll = GetPalaceItemRoomMaxCounts(palaceItemRoomCount, palaceLengthsMax);
        var itemCountLimit = GetPalaceItemRoomLimits(Enumerable.Repeat(normalPalaceStyle, 6).ToArray());
        var palaceItemsMaxDiff = itemCountMaxRoll.Zip(itemCountLimit, Math.Min).Sum(n => n - 1);
        palaceMinorItemCount += palaceItemsMaxDiff;

        return (overworldMinorItemCount, palaceMinorItemCount);
    }

    private int GetDarkLinkMinDistance()
    {
        if (darkLinkMinDistance == BossRoomMinDistance.MAX)
        {
            // limiting here based on how long it takes to generate the seeds
            if (gpStyle == PalaceStyle.RECONSTRUCTED) { return 16; }
            return 20;
        }
        else
        {
            return (int)darkLinkMinDistance;
        }
    }

    internal bool IsVanillaEverythingPalace(int palaceIndex)
    {
        return palaceIndex switch
        {
            0 or 1 or 2 => westBiome is Biome.VANILLA_EVERYTHING,
            3 => mazeBiome is Biome.VANILLA_EVERYTHING,
            4 or 5 or 6 => eastBiome is Biome.VANILLA_EVERYTHING,
            _ => throw new ArgumentException("Invalid palace number"),
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
