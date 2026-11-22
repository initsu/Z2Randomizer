using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using Z2Randomizer.RandomizerCore.Diagnostics;
using Z2Randomizer.RandomizerCore.Overworld;

namespace Z2Randomizer.RandomizerCore;

/// <summary>
/// Originally this class corresponded to the flags and controlled the logic, with all the actual configuration randomization
/// logic scattershot all over the rando. Now this class should (as much as possible) represent what the actual net configuration
/// was, and RandomizerConfiguration should represent the flags/interface, with the configuration randomization done entirly
/// in the interface between them.
/// </summary>

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(RandomizerProperties))]
[JsonSerializable(typeof(Climate))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
[CheckedInitialization]
public partial class RandomizerProperties
{

    //ROM Info
    public partial string? Seed { get; set; }
    public partial string? Flags { get; set; }

    //Items
    public partial bool StartCandle { get; set; }
    public partial bool StartGlove { get; set; }
    public partial bool StartRaft { get; set; }
    public partial bool StartBoots { get; set; }
    public partial bool StartFlute { get; set; }
    public partial bool StartCross { get; set; }
    public partial bool StartHammer { get; set; }
    public partial bool StartKey { get; set; }

    //Spells
    public partial bool StartShield { get; set; }
    public partial bool StartJump { get; set; }
    public partial bool StartLife { get; set; }
    public partial bool StartFairy { get; set; }
    public partial bool StartFire { get; set; }
    public partial bool StartReflect { get; set; }
    public partial bool StartSpell { get; set; }
    public partial bool StartThunder { get; set; }
    public partial Collectable? LinkedFireSpell { get; set; }
    public partial bool ReplaceFireWithDash { get; set; }

    //Other starting attributes
    public partial int StartHearts { get; set; }
    public partial int MaxHearts { get; set; }
    public partial int StartMagicContainers { get; set; }
    public partial int MaxMagicContainers { get; set; }
    public partial bool StartWithUpstab { get; set; }
    public partial bool StartWithDownstab { get; set; }
    public partial int StartLives { get; set; }
    public partial bool PermanentBeam { get; set; }
    public partial bool UseCommunityText { get; set; }
    public partial int StartAtk { get; set; }
    public partial int StartingMagicLevel { get; set; }
    public partial int StartLifeLvl { get; set; }
    public partial bool SwapUpAndDownStab { get; set; }
    public partial StartingLocation StartingLocation { get; set; }

    //Overworld
    public partial bool ShuffleEncounters { get; set; }
    public partial bool AllowPathEnemies { get; set; }
    public partial bool IncludeLavaInEncounterShuffle { get; set; }
    public partial bool PalacesCanSwapContinent { get; set; }
    public partial bool P7shuffle { get; set; }
    public partial bool HiddenPalace { get; set; }
    public partial bool HiddenKasuto { get; set; }
    public partial EncounterRate EncounterRates { get; set; }
    public partial ContinentConnectionType ContinentConnections { get; set; }
    public partial OverworldSizeOption WestSize { get; set; }
    public partial OverworldSizeOption EastSize { get; set; }
    public partial DmSizeOption DmSize { get; set; }
    public partial MazeSizeOption MazeSize { get; set; }
    public partial bool BoulderBlockConnections { get; set; }
    public partial Biome WestBiome { get; set; }
    public partial Biome EastBiome { get; set; }
    public partial Biome DmBiome { get; set; }
    public partial Biome MazeBiome { get; set; }
    public partial bool DmIsHorizontal { get; set; }
    public partial bool WestIsHorizontal { get; set; }
    public partial bool EastIsHorizontal { get; set; }
    public partial bool EastRockIsPath { get; set; }
#pragma warning disable CS8618
    public partial ClimateEnum WestClimate { get; set; }
    public partial ClimateEnum EastClimate { get; set; }
    public partial ClimateEnum DmClimate { get; set; }
#pragma warning restore CS8618 
    public partial bool LegacyVanillaShuffledLocations { get; set; }
    public partial bool ShuffleHidden { get; set; }
    public partial bool CanWalkOnWaterWithBoots { get; set; }
    public partial bool BagusWoods { get; set; }
    public partial LessImportantLocationsOption LessImportantLocationsOption { get; set; }
    public partial bool SaneCaves { get; set; }
    public partial RiverDevilBlockerOption RiverDevilBlockerOption { get; set; }
    public partial bool EastRocks { get; set; }

    //Palaces
    [NotMapped]
    public partial PalaceStyle[] PalaceStyles { get; set; }
    [NotMapped]
    public partial int[] PalaceLengths { get; set; }
    public partial int StartGems { get; set; }
    public partial bool RequireTbird { get; set; }
    public partial int DarkLinkMinDistance { get; set; }
    public partial bool ShufflePalacePalettes { get; set; }
    public partial bool UpARestartsAtPalaces { get; set; }
    public partial bool Global5050JarDrop { get; set; }
    public partial bool ReduceDripperVariance { get; set; }
    public partial bool RemoveTbird { get; set; }
    public partial bool BossItem { get; set; }
    public partial KeyLogicOption KeyLogic { get; set; }
    public partial bool BlockersAnywhere { get; set; }

    /// <summary>
    /// P1-P5 need self-contained small keys. P6 does too with BlockersAnywhere on
    /// or Magic Key is removed. GP is not covered by key logic.
    /// </summary>
    public bool RequiresSmallKeyValidation(int palaceNumber) =>
        KeyLogic == KeyLogicOption.KEYS_PER_PALACE_SOLVABLE
        && palaceNumber < 7
        && (palaceNumber < 6 || BlockersAnywhere || RemoveItems.Contains(Collectable.MAGIC_KEY));
    public bool LockedDoorsBlockFairy { get; set; } = false;
    public partial bool RemoveLongDeadEnds { get; set; }
    public partial bool IncludeExpertRooms { get; set; }
    [NotMapped]
    public partial bool[] BossRoomsExitToPalace { get; set; }
    public partial bool NoDuplicateRooms { get; set; }
    public partial PalaceDropStyle PalaceDropStyle { get; set; }
    public partial bool DropsMayBypassBosses { get; set; }
    public partial bool NoDuplicateRoomsBySideview { get; set; }
    public partial bool GeneratorsAlwaysMatch { get; set; }
    public partial RoomPoolOption RoomPoolOption { get; set; }
    public partial bool AllowVanillaRooms { get; set; }
    public partial bool AllowV4Rooms { get; set; }
    public partial bool AllowV5_0Rooms { get; set; }
    public partial bool HardBosses { get; set; }
    public partial bool AggressiveTbird { get; set; }
    [NotMapped]
    public partial int[] PalaceItemRoomCounts { get; set; }
    public partial bool UsePalaceItemRoomCountIndicator { get; set; }
    public partial bool RevealWalkthroughWalls { get; set; }
    public partial bool RevealHiddenJars { get; set; }

    //Enemies
    public partial EnemyLifeOption ShuffleEnemyHP { get; set; }
    public partial EnemyLifeOption ShuffleBossHP { get; set; }
    public partial bool ShuffleEnemyStealExp { get; set; }
    public partial bool ShuffleStealExpAmt { get; set; }
    public SwordImmunityOption SwordImmunityOption;
    public partial bool ShuffleOverworldEnemies { get; set; }
    public partial bool ShufflePalaceEnemies { get; set; }
    public partial bool MixLargeAndSmallEnemies { get; set; }
    public partial DripperEnemyOption DripperEnemyOption { get; set; }
    public partial bool ShuffleEnemyPalettes { get; set; }
    public partial XPEffectiveness EnemyXPDrops { get; set; }

    //Levels
    public partial bool ShuffleAtkExp { get; set; }
    public partial bool ShuffleMagicExp { get; set; }
    public partial bool ShuffleLifeExp { get; set; }
    public partial bool ShuffleLifeRefill { get; set; }
    public partial bool ShuffleSpellLocations { get; set; }
    public partial bool DisableMagicRecs { get; set; }
    public partial AttackEffectiveness AttackEffectiveness { get; set; }
    public partial MagicEffectiveness MagicEffectiveness { get; set; }
    public partial LifeEffectiveness LifeEffectiveness { get; set; }
    public partial int AttackCap { get; set; }
    public partial int MagicCap { get; set; }
    public partial int LifeCap { get; set; }
    public partial bool ScaleLevels { get; set; }
    public partial bool SpellEnemy { get; set; }

    //Items
    public partial bool ShuffleOverworldItems { get; set; }
    public partial bool ShufflePalaceItems { get; set; }
    public partial bool MixOverworldPalaceItems { get; set; }
    public partial bool IncludeSpellsInShuffle { get; set; }
    public partial bool IncludeSwordTechsInShuffle { get; set; }
    //Fountain water / Saria mirror
    public partial bool IncludeQuestItemsInShuffle { get; set; }
    public partial bool IncludeBagusNoteInShuffle { get; set; }
    //Saria mirror table / Nabooru fountain / Downstab guy / Upstab guy hold minor items if they are shuffled
    public partial bool TownQuestLocationsAreMinorItems { get; set; }
    //Spell items may not be required to obtain another spell item
    public partial bool PreventSpellItemChains { get; set; }
    public partial bool RandomizeSmallItems { get; set; }
    public partial bool ExtraKeys { get; set; }
    public partial bool FastItemPickup { get; set; }
    public partial bool AllowImportantItemDuplicates { get; set; }
    public partial int NewKasutoBasementRequirement { get; set; }
    //Include PBag caves in item shuffle
    public partial bool PbagItemShuffle { get; set; }
    public partial bool StartWithSpellItems { get; set; }
    public partial bool ShufflePbagXp { get; set; }

    public HashSet<Collectable> RemoveItems { get; set; } = [];

    //Drops
    public partial bool ShuffleItemDropFrequency { get; set; }
    //public bool shuffleEnemyDrops { get; set; }
    public partial bool Smallbluejar { get; set; }
    public partial bool Smallredjar { get; set; }
    public partial bool Small50 { get; set; }
    public partial bool Small100 { get; set; }
    public partial bool Small200 { get; set; }
    public partial bool Small500 { get; set; }
    public partial bool Small1up { get; set; }
    public partial bool Smallkey { get; set; }

    public partial bool Largebluejar { get; set; }
    public partial bool Largeredjar { get; set; }
    public partial bool Large50 { get; set; }
    public partial bool Large100 { get; set; }
    public partial bool Large200 { get; set; }
    public partial bool Large500 { get; set; }
    public partial bool Large1up { get; set; }
    public partial bool Largekey { get; set; }
    public partial bool StandardizeDrops { get; set; }
    public partial bool RandomizeDrops { get; set; }

    //Hints
    public partial bool SpellItemHints { get; set; }
    public partial HelpfulHintOption HelpfulHints { get; set; }
    public partial bool TownNameHints { get; set; }

    //Misc.
    public partial bool MarioMode { get; set; }
    public partial byte BeepFrequency { get; set; }
    public partial byte BeepThreshold { get; set; }
    public partial bool JumpAlwaysOn { get; set; }
    public partial bool DashAlwaysOn { get; set; }
    public partial bool FasterDashFairy { get; set; }
    public partial FluteWarpMode FluteWarpMode { get; set; }
    public partial bool FastCast { get; set; }

    public partial BeamSprites BeamSprite { get; set; }
    public partial bool DisableMusic { get; set; }
    public partial bool RandomizeMusic { get; set; }
    public partial bool MixCustomAndOriginalMusic { get; set; }
    public partial bool IncludeDiverseMusic { get; set; }
    public partial bool DisableUnsafeMusic { get; set; }
    [NotMapped]
#pragma warning disable CS8618 
    public partial CharacterSprite CharSprite { get; set; }
#pragma warning restore CS8618
    public partial bool ChangeItemSprites { get; set; }
    public partial NesColor TunicColor { get; set; }
    public partial NesColor SkinTone { get; set; }
    public partial NesColor OutlineColor { get; set; }
    public partial NesColor ShieldColor { get; set; }
    public partial bool UpAC1 { get; set; }
    public partial bool RemoveFlashing { get; set; }
    public partial bool DarkenThunderbird { get; set; }
    public partial bool HorizontalBossBarsGP { get; set; }
    public partial bool UseCustomRooms { get; set; }
    public partial bool UpdatedHud { get; set; }
    public partial bool DisableHUDLag { get; set; }
    public partial bool RandomizeKnockback { get; set; }

    //For Statistics
    [Key]
    public int Id { get; set; } = 0;

    public bool StartsWithCollectable(Collectable? collectable)
    {
        return collectable switch
        {
            Collectable.SHIELD_SPELL => StartShield,
            Collectable.JUMP_SPELL => StartJump,
            Collectable.TANOOKI => StartJump,
            Collectable.LIFE_SPELL => StartLife,
            Collectable.FAIRY_SPELL => StartFairy,
            Collectable.FIRE_SPELL => StartFire,
            Collectable.DASH_SPELL => StartFire,
            Collectable.REFLECT_SPELL => StartReflect,
            Collectable.SPELL_SPELL => StartSpell,
            Collectable.THUNDER_SPELL => StartThunder,

            Collectable.UPSTAB => StartWithUpstab,
            Collectable.DOWNSTAB => StartWithDownstab,

            Collectable.CANDLE => StartCandle,
            Collectable.GLOVE => StartGlove,
            Collectable.RAFT => StartRaft,
            Collectable.BOOTS => StartBoots,
            Collectable.FLUTE => StartFlute,
            Collectable.CROSS => StartCross,
            Collectable.HAMMER => StartHammer,
            Collectable.MAGIC_KEY => StartKey,

            Collectable.TROPHY => StartWithSpellItems,
            Collectable.MEDICINE => StartWithSpellItems,
            Collectable.CHILD => StartWithSpellItems,
            Collectable.MIRROR => StartWithSpellItems,
            Collectable.WATER => StartWithSpellItems,

            _ => false
        };
    }

    public void SetStartingCollectable(Collectable collectable, bool starts = true)
    {
        if (collectable == Collectable.SHIELD_SPELL)
        {
            StartShield = starts;
        }
        else if (collectable == Collectable.JUMP_SPELL)
        {
            StartJump = starts;
        }
        else if (collectable == Collectable.LIFE_SPELL)
        {
            StartLife = starts;
        }
        else if (collectable == Collectable.FAIRY_SPELL)
        {
            StartFairy = starts;
        }
        else if (collectable == Collectable.FIRE_SPELL)
        {
            StartFire = starts;
        }
        else if (collectable == Collectable.REFLECT_SPELL)
        {
            StartReflect = starts;
        }
        else if (collectable == Collectable.SPELL_SPELL)
        {
            StartSpell = starts;
        }
        else if (collectable == Collectable.THUNDER_SPELL)
        {
            StartThunder = starts;
        }
        else if (collectable == Collectable.CANDLE)
        {
            StartCandle = starts;
        }
        else if (collectable == Collectable.GLOVE)
        {
            StartGlove = starts;
        }
        else if (collectable == Collectable.RAFT)
        {
            StartRaft = starts;
        }
        else if (collectable == Collectable.BOOTS)
        {
            StartBoots = starts;
        }
        else if (collectable == Collectable.FLUTE)
        {
            StartFlute = starts;
        }
        else if (collectable == Collectable.CROSS)
        {
            StartCross = starts;
        }
        else if (collectable == Collectable.HAMMER)
        {
            StartHammer = starts;
        }
        else if (collectable == Collectable.MAGIC_KEY)
        {
            StartKey = starts;
        }
    }

    public bool HasEnoughSpaceToAllocateItems()
    {
        int overworldMinorItemCount = 0;
        int palaceMinorItemCount = 0;

        // The pbag caves are available only if they are included in the item shuffle.
        // When excluded, items may no longer overflow into them like in the old versions.
        if (ShuffleOverworldItems && PbagItemShuffle && WestBiome.InItemShuffle())
        {
            overworldMinorItemCount += 1;
        }
        if (ShuffleOverworldItems && PbagItemShuffle && EastBiome.InItemShuffle())
        {
            overworldMinorItemCount += 2;
        }

        //more or less than 4 containers in the seed adds/removes overworld minor items
        overworldMinorItemCount -= MaxHearts - StartHearts - 4;
        overworldMinorItemCount -= MaxMagicContainers - StartMagicContainers - 4;

        //palace items other than 1 adjusts the count
        palaceMinorItemCount += PalaceItemRoomCounts.Take(6).Sum(c => c - 1);

        //Start items add 1 to the count, bucketed by whether they sit in a palace or the overworld
        if (ShufflePalaceItems && WestBiome.InItemShuffle())
        {
            palaceMinorItemCount += StartCandle ? 1 : 0;
            palaceMinorItemCount += StartGlove ? 1 : 0;
            palaceMinorItemCount += StartRaft ? 1 : 0;
        }
        if (ShuffleOverworldItems && DmBiome.InItemShuffle())
        {
            overworldMinorItemCount += StartHammer ? 1 : 0;
        }
        if (ShufflePalaceItems && MazeBiome.InItemShuffle())
        {
            palaceMinorItemCount += StartBoots ? 1 : 0;
        }
        if (ShufflePalaceItems && EastBiome.InItemShuffle())
        {
            palaceMinorItemCount += StartCross ? 1 : 0;
            palaceMinorItemCount += StartFlute ? 1 : 0;
        }
        if (ShuffleOverworldItems && EastBiome.InItemShuffle())
        {
            overworldMinorItemCount += StartKey || RemoveItems.Contains(Collectable.MAGIC_KEY) ? 1 : 0;
        }

        if (IncludeSpellsInShuffle)
        {
            if (WestBiome.InItemShuffle())
            {
                overworldMinorItemCount += StartShield ? 1 : 0;
                overworldMinorItemCount += StartJump ? 1 : 0;
                overworldMinorItemCount += StartLife ? 1 : 0;
                overworldMinorItemCount += StartFairy ? 1 : 0;
            }
            if (EastBiome.InItemShuffle())
            {
                overworldMinorItemCount += StartFire ? 1 : 0;
                overworldMinorItemCount += StartReflect ? 1 : 0;
                overworldMinorItemCount += StartSpell ? 1 : 0;
                overworldMinorItemCount += StartThunder ? 1 : 0;
            }
        }

        if (IncludeSwordTechsInShuffle)
        {
            if (WestBiome.InItemShuffle())
            {
                overworldMinorItemCount += StartWithDownstab ? 1 : 0;
                if (TownQuestLocationsAreMinorItems)
                {
                    overworldMinorItemCount -= 1;
                }
            }
            if (EastBiome.InItemShuffle())
            {
                overworldMinorItemCount += StartWithUpstab ? 1 : 0;
                if (TownQuestLocationsAreMinorItems)
                {
                    overworldMinorItemCount -= 1;
                }
            }
        }

        if (IncludeQuestItemsInShuffle && TownQuestLocationsAreMinorItems)
        {
            if (WestBiome.InItemShuffle())
            {
                overworldMinorItemCount -= 1;
            }
            if (EastBiome.InItemShuffle())
            {
                overworldMinorItemCount -= 1;
            }
        }

        if (MixOverworldPalaceItems)
        {
            var minorItemCount = overworldMinorItemCount + palaceMinorItemCount;
            return minorItemCount >= 0;
        }
        else
        {
            return overworldMinorItemCount >= 0 && palaceMinorItemCount >= 0;
        }
    }
}
