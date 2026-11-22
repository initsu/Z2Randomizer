using System.Reflection;
using System.Text.Json;
using CrossPlatformUI.Presets;
using Z2Randomizer.RandomizerCore;
using Random = Z2Randomizer.RandomizerCore.Primitives.Random;

namespace Z2Randomizer.Tests;

[TestClass]
public class PropertyInitializationTests
{
    [TestMethod]
    [DataRow(nameof(RandomizerProperties.ShuffleOverworldItems), false)]
    [DataRow(nameof(RandomizerProperties.ShufflePalaceItems), false)]
    [DataRow(nameof(RandomizerProperties.IncludeSpellsInShuffle), false)]
    [DataRow(nameof(RandomizerProperties.IncludeSwordTechsInShuffle), false)]
    [DataRow(nameof(RandomizerProperties.IncludeQuestItemsInShuffle), false)]
    [DataRow(nameof(RandomizerProperties.StartHearts), 0)]
    [DataRow(nameof(RandomizerProperties.WestBiome), Biome.VANILLA)]
    [DataRow(nameof(RandomizerProperties.Seed), null)]
    [DataRow(nameof(RandomizerProperties.PalaceStyles), null)]
    [DataRow(nameof(RandomizerProperties.PalaceLengths), null)]
    [DataRow(nameof(RandomizerProperties.BossRoomsExitToPalace), null)]
    [DataRow(nameof(RandomizerProperties.PalaceItemRoomCounts), null)]
    [DataRow(nameof(RandomizerProperties.CharSprite), null)]
    public void GettersDistinguishUnassignedFromExplicitDefaultValues(string name, object? value)
    {
        var props = new RandomizerProperties();
        var property = typeof(RandomizerProperties).GetProperty(name)!;
#if DEBUG
        var exception = Assert.Throws<TargetInvocationException>(() => property.GetValue(props));
        Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        StringAssert.Contains(exception.InnerException.Message, $"RandomizerProperties.{name}");
#else
        Assert.AreEqual(value, property.GetValue(props));
#endif
        property.SetValue(props, value);
        Assert.AreEqual(value, property.GetValue(props));
    }

    [TestMethod]
    public void ReassigningAnInitializedPropertyKeepsTheLatestValue()
    {
        var props = new RandomizerProperties { IncludeSpellsInShuffle = true };
        Assert.IsTrue(props.IncludeSpellsInShuffle);
        props.IncludeSpellsInShuffle = false;
        Assert.IsFalse(props.IncludeSpellsInShuffle);
    }

    [TestMethod]
    public void DeclarationDefaultsAreImmediatelyReadable()
    {
        var props = new RandomizerProperties();
        Assert.IsEmpty(props.RemoveItems);
        Assert.IsFalse(props.LockedDoorsBlockFairy);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ExportInitializesEveryProperty(bool includeDifficulty)
    {
        RandomizerConfiguration[] configs = [new(), new(StandardPreset.Preset.SerializeFlags())];
        foreach (var config in configs)
        {
            var props = config.Export(new Random(42), includeDifficulty);
            var failures = new List<string>();
            foreach (var property in typeof(RandomizerProperties).GetProperties())
            {
                try
                {
                    property.GetValue(props);
                }
                catch (TargetInvocationException exception)
                {
                    failures.Add(exception.InnerException!.Message);
                }
            }
            Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
        }
    }

    [TestMethod]
    public void JsonRoundTripInitializesTheDeserializedProperties()
    {
        var props = new RandomizerConfiguration().Export(new Random(42));
        var contract = SourceGenerationContext.Default.RandomizerProperties;
        string json = JsonSerializer.Serialize(props, contract);

        var restored = JsonSerializer.Deserialize(json, contract)!;

        Assert.AreEqual(json, JsonSerializer.Serialize(restored, contract));
    }

    [TestMethod]
    public void NotMappedPropertiesAreIncludedInJson()
    {
        var props = new RandomizerConfiguration().Export(new Random(42));
        props.PalaceStyles = Enumerable.Repeat(PalaceStyle.SEQUENTIAL, 7).ToArray();
        props.PalaceLengths = [1, 2, 3, 4, 5, 6, 7];
        props.PalaceItemRoomCounts = [0, 1, 2, 3, 4, 5, 0];
        props.BossRoomsExitToPalace = [true, false, true, false, true, false, false];
        var contract = SourceGenerationContext.Default.RandomizerProperties;

        string json = JsonSerializer.Serialize(props, contract);
        using var document = JsonDocument.Parse(json);
        foreach (string name in new[] { nameof(props.PalaceStyles), nameof(props.PalaceLengths),
            nameof(props.PalaceItemRoomCounts), nameof(props.BossRoomsExitToPalace), nameof(props.CharSprite) })
        {
            Assert.IsTrue(document.RootElement.TryGetProperty(name, out _), $"{name} must be serialized despite [NotMapped]");
        }

        var restored = JsonSerializer.Deserialize(json, contract)!;
        CollectionAssert.AreEqual(props.PalaceStyles, restored.PalaceStyles);
        CollectionAssert.AreEqual(props.PalaceLengths, restored.PalaceLengths);
        CollectionAssert.AreEqual(props.PalaceItemRoomCounts, restored.PalaceItemRoomCounts);
        CollectionAssert.AreEqual(props.BossRoomsExitToPalace, restored.BossRoomsExitToPalace);
    }

    [TestMethod]
    [DataRow(PalaceItemRoomCount.ZERO, BossRoomsExitType.OVERWORLD)]
    [DataRow(PalaceItemRoomCount.ONE, BossRoomsExitType.PALACE)]
    [DataRow(PalaceItemRoomCount.TWO, BossRoomsExitType.RANDOM_ALL)]
    [DataRow(PalaceItemRoomCount.RANDOM_NOT_ZERO, BossRoomsExitType.RANDOM_PER_PALACE)]
    [DataRow(PalaceItemRoomCount.RANDOM_INCLUDE_ZERO, BossRoomsExitType.RANDOM_ALL)]
    public void ExportPublishesCompletePalaceArrays(PalaceItemRoomCount itemRoomCount, BossRoomsExitType bossExitType)
    {
        var config = new RandomizerConfiguration
        {
            NormalPalaceStyle = PalaceStyle.SEQUENTIAL,
            PalaceItemRoomCount = itemRoomCount,
            BossRoomsExitType = bossExitType,
        };

        for (int seed = 0; seed < 8; seed++)
        {
            var props = config.Export(new Random(seed));
            Assert.HasCount(7, props.PalaceStyles);
            Assert.HasCount(7, props.PalaceLengths);
            Assert.HasCount(7, props.PalaceItemRoomCounts);
            Assert.HasCount(7, props.BossRoomsExitToPalace);
            Assert.IsTrue(props.PalaceStyles.Take(6).All(style => style == PalaceStyle.SEQUENTIAL));
            Assert.IsTrue(props.PalaceLengths.All(length => length > 0));
            Assert.IsTrue(props.PalaceItemRoomCounts.Take(6).All(count => count >= 1),
                "Unshuffled palace items each need an item room");
            Assert.AreEqual(0, props.PalaceItemRoomCounts[6]);
            Assert.IsFalse(props.BossRoomsExitToPalace[6]);
            if (bossExitType == BossRoomsExitType.PALACE)
                Assert.IsTrue(props.BossRoomsExitToPalace.Take(6).All(exitToPalace => exitToPalace));
        }
    }

    [TestMethod]
    public void StartingTechniquesAreResolvedBeforeCheckingItemCapacity()
    {
        // Three P-bag slots and a starting downstab cover the four displaced town items.
        var config = new RandomizerConfiguration
        {
            ShuffleOverworldItems = true,
            IncludeSwordTechsInShuffle = true,
            IncludeQuestItemsInShuffle = true,
            TownQuestLocationsAreMinorItems = true,
            IncludePBagCavesInItemShuffle = true,
            StartingTechniques = StartingTechs.DOWNSTAB,
            WestBiome = Biome.VANILLA_SHUFFLE,
            EastBiome = Biome.VANILLA_SHUFFLE,
        };

        var props = config.Export(new Random(42));

        Assert.IsTrue(props.StartWithDownstab);
        Assert.IsTrue(props.HasEnoughSpaceToAllocateItems());
    }

    [TestMethod]
    public void DisablingEveryRoomPoolExplicitlyInitializesTheVanillaFallback()
    {
        var config = new RandomizerConfiguration
        {
            IncludeVanillaRooms = false,
            Includev4_0Rooms = false,
            Includev5_0Rooms = false,
        };

        var props = config.Export(new Random(42));

        Assert.IsTrue(props.AllowVanillaRooms);
        Assert.IsFalse(props.AllowV4Rooms);
        Assert.IsFalse(props.AllowV5_0Rooms);
    }
}
