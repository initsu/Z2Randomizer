using Z2Randomizer.RandomizerCore;
using Random = Z2Randomizer.RandomizerCore.Primitives.Random;

namespace Z2Randomizer.Tests;

[TestClass]
public class OverworldItemShuffleDependencyTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    [DataRow(null)]
    public void OverworldItemPoolControlsAndFlagsFollowParentAvailability(bool? shuffleOverworld)
    {
        var config = new RandomizerConfiguration
        {
            ShuffleOverworldItems = shuffleOverworld,
            IncludeSpellsInShuffle = true,
            IncludeSwordTechsInShuffle = true,
            IncludeQuestItemsInShuffle = true,
            IncludeBagusNoteInShuffle = true,
        };
        bool available = shuffleOverworld != false;

        Assert.AreEqual(available, config.includeSpellsInShuffleIncluded());
        Assert.AreEqual(available, config.includeSwordTechsInShuffleIncluded());
        Assert.AreEqual(available, config.includeQuestItemsInShuffleIncluded());
        Assert.AreEqual(available, config.includeBagusNoteInShuffleIncluded());
        Assert.AreEqual(!available, config.shuffleSpellLocationsIncluded());
        Assert.AreEqual(!available, config.swapUpAndDownStabIncluded());

        var roundTrip = new RandomizerConfiguration(config.SerializeFlags());
        Assert.AreEqual(available, roundTrip.IncludeSpellsInShuffle.GetValueOrDefault());
        Assert.AreEqual(available, roundTrip.IncludeSwordTechsInShuffle.GetValueOrDefault());
        Assert.AreEqual(available, roundTrip.IncludeQuestItemsInShuffle.GetValueOrDefault());
        Assert.AreEqual(available, roundTrip.IncludeBagusNoteInShuffle.GetValueOrDefault());
        Assert.AreEqual(config.SerializeFlags(), roundTrip.SerializeFlags());
    }

    [TestMethod]
    public void SwordTechniquesCannotDisplaceTrainerItemsWithoutOverworldShuffle()
    {
        var config = new RandomizerConfiguration
        {
            ShuffleOverworldItems = false,
            IncludeSwordTechsInShuffle = true,
            TownQuestLocationsAreMinorItems = true,
        };

        foreach (var input in new[] { config, new RandomizerConfiguration(config.SerializeFlags()) })
        {
            var props = input.Export(new Random(42));
            Assert.IsFalse(props.IncludeSwordTechsInShuffle);
            Assert.IsFalse(props.TownQuestLocationsAreMinorItems);
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(false, null)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    [DataRow(true, null)]
    [DataRow(null, false)]
    [DataRow(null, true)]
    [DataRow(null, null)]
    public void ExportGatesItemPoolOptionsOnResolvedOverworldShuffle(bool? shuffleOverworld, bool? includeItems)
    {
        var config = new RandomizerConfiguration
        {
            ShuffleOverworldItems = shuffleOverworld,
            ShufflePalaceItems = true,
            IncludeSpellsInShuffle = includeItems,
            IncludeSwordTechsInShuffle = includeItems,
            IncludeQuestItemsInShuffle = includeItems,
            IncludeBagusNoteInShuffle = includeItems,
            IncludePBagCavesInItemShuffle = includeItems,
            MixOverworldAndPalaceItems = includeItems,
        };

        // Exercise both direct configuration (e.g. JSON) and generated flag strings.
        foreach (var input in new[] { config, new RandomizerConfiguration(config.SerializeFlags()) })
        {
            foreach (bool? shufflePalaces in new bool?[] { false, true, null })
            {
                input.ShufflePalaceItems = shufflePalaces;
                var parentOutcomes = new HashSet<bool>();
                var palaceOutcomes = new HashSet<bool>();
                var techniqueOutcomes = new HashSet<bool>();
                for (int seed = 0; seed < 64; seed++)
                {
                    var props = input.Export(new Random(seed));
                    parentOutcomes.Add(props.ShuffleOverworldItems);
                    palaceOutcomes.Add(props.ShufflePalaceItems);
                    bool[] itemPoolOptions = [props.IncludeSpellsInShuffle, props.IncludeSwordTechsInShuffle,
                        props.IncludeQuestItemsInShuffle, props.IncludeBagusNoteInShuffle, props.PbagItemShuffle];

                    if (!props.ShuffleOverworldItems || includeItems == false)
                    {
                        Assert.IsTrue(itemPoolOptions.All(enabled => !enabled),
                            $"Inactive overworld options must not enter the item pool (seed {seed})");
                    }
                    else if (includeItems == true)
                    {
                        Assert.IsTrue(itemPoolOptions.All(enabled => enabled));
                    }

                    if (props.ShuffleOverworldItems)
                        techniqueOutcomes.Add(props.IncludeSwordTechsInShuffle);
                    if (!props.ShuffleOverworldItems || !props.ShufflePalaceItems || includeItems == false)
                        Assert.IsFalse(props.MixOverworldPalaceItems);
                    else if (includeItems == true)
                        Assert.IsTrue(props.MixOverworldPalaceItems);
                }

                if (shuffleOverworld == null)
                    Assert.HasCount(2, parentOutcomes, "Exercise both resolutions of the random parent");
                if (shufflePalaces == null)
                    Assert.HasCount(2, palaceOutcomes, "Exercise both resolutions of the random palace parent");
                if (shuffleOverworld != false && includeItems == null)
                    Assert.HasCount(2, techniqueOutcomes, "Random children must still roll when their parent is on");
            }
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    [DataRow(null)]
    public void TownMinorItemLocationsRequireResolvedOverworldAndCategoryShuffles(bool? shuffleOverworld)
    {
        var config = new RandomizerConfiguration
        {
            ShuffleOverworldItems = shuffleOverworld,
            IncludeSwordTechsInShuffle = null,
            IncludeQuestItemsInShuffle = null,
            TownQuestLocationsAreMinorItems = true,
            IncludePBagCavesInItemShuffle = true,
            StartWithHammer = true,
            WestBiome = Biome.VANILLA_SHUFFLE,
            EastBiome = Biome.VANILLA_SHUFFLE,
            DmBiome = Biome.VANILLA_SHUFFLE,
        };

        var eligibilityOutcomes = new HashSet<bool>();
        for (int seed = 0; seed < 64; seed++)
        {
            var props = config.Export(new Random(seed));
            bool eligible = props.ShuffleOverworldItems && (props.IncludeSwordTechsInShuffle || props.IncludeQuestItemsInShuffle);
            Assert.AreEqual(eligible, props.TownQuestLocationsAreMinorItems, $"Seed {seed}");
            eligibilityOutcomes.Add(eligible);
        }
        if (shuffleOverworld != false)
            Assert.HasCount(2, eligibilityOutcomes, "Exercise both eligible and ineligible category resolutions");
    }

    [TestMethod]
    [DataRow(false, true)]
    [DataRow(true, true)]
    [DataRow(null, true)]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(null, false)]
    [DataRow(false, null)]
    [DataRow(true, null)]
    [DataRow(null, null)]
    public void StandaloneSpellShuffleAndStabSwapFollowResolvedPoolOptions(bool? shuffleOverworld, bool? includeItems)
    {
        var config = new RandomizerConfiguration
        {
            ShuffleOverworldItems = shuffleOverworld,
            IncludeSpellsInShuffle = includeItems,
            IncludeSwordTechsInShuffle = includeItems,
            ShuffleSpellLocations = true,
            SwapUpAndDownStab = true,
        };

        foreach (var input in new[] { config, new RandomizerConfiguration(config.SerializeFlags()) })
        {
            for (int seed = 0; seed < 64; seed++)
            {
                var props = input.Export(new Random(seed));
                Assert.AreEqual(input.shuffleSpellLocationsIncluded() && !props.IncludeSpellsInShuffle, props.ShuffleSpellLocations);
                Assert.AreEqual(input.swapUpAndDownStabIncluded() && !props.IncludeSwordTechsInShuffle, props.SwapUpAndDownStab);
            }
        }
    }

    [TestMethod]
    public void DisabledRandomItemPoolOptionsDoNotConsumeRandomness()
    {
        var config = new RandomizerConfiguration { ShuffleOverworldItems = false };
        var baselineRandom = new Random(42);
        config.Export(baselineRandom);

        config.IncludeSpellsInShuffle = null;
        config.IncludeSwordTechsInShuffle = null;
        config.IncludeQuestItemsInShuffle = null;
        config.IncludeBagusNoteInShuffle = null;
        config.IncludePBagCavesInItemShuffle = null;
        var actualRandom = new Random(42);
        config.Export(actualRandom);

        Assert.AreEqual(baselineRandom.GetState(), actualRandom.GetState());
    }

    [TestMethod]
    public void InactiveSpellPoolCannotAddStartingSpellSlotsToTheItemBudget()
    {
        var config = new RandomizerConfiguration
        {
            ShuffleOverworldItems = false,
            StartWithShield = true,
        };
        var baseline = config.CountPossibleMinorItems();

        config.IncludeSpellsInShuffle = true;

        Assert.AreEqual(baseline, config.CountPossibleMinorItems());
    }
}
