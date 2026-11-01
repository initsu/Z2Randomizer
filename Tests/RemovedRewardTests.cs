using CrossPlatformUI.Presets;
using Z2Randomizer.RandomizerCore;
using Random = Z2Randomizer.RandomizerCore.Random;

namespace Z2Randomizer.Tests;

[TestClass]
public class RemovedRewardTests
{
    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void RemovedRewardsStayRemovedAcrossExportAndStartingRolls(bool fairy, bool key)
    {
        var config = new RandomizerConfiguration(StandardPreset.Preset.SerializeFlags())
        {
            RemoveFairy = fairy,
            RemoveMagicKey = key,
            KeyLogic = KeyLogicOption.KEYS_PER_PALACE_SOLVABLE,
            StartWithFairy = true,
            StartWithMagicKey = true,
            ShuffleStartingSpells = true,
            ShuffleStartingItems = true,
            FireOption = FireOption.PAIR_WITH_RANDOM,
        };
        var roundTrip = new RandomizerConfiguration(config.SerializeFlags());
        Assert.AreEqual(fairy, roundTrip.RemoveFairy);
        Assert.AreEqual(key, roundTrip.RemoveMagicKey);
        for (int seed = 0; seed < 100; seed++)
        {
            var props = roundTrip.Export(new Random(seed));
            Assert.AreEqual(fairy, props.RemoveItems.Contains(Collectable.FAIRY_SPELL));
            Assert.AreEqual(key, props.RemoveItems.Contains(Collectable.MAGIC_KEY));
            if (fairy)
            {
                Assert.IsFalse(props.StartFairy);
                Assert.AreNotEqual(Collectable.FAIRY_SPELL, props.LinkedFireSpell);
            }
            if (key) { Assert.IsFalse(props.StartKey); }
            Assert.IsTrue(props.HasEnoughSpaceToAllocateItems());
        }
    }

    [TestMethod]
    [DataRow(PalaceStyle.VANILLA, PalaceStyle.RANDOM_WALK)]
    [DataRow(PalaceStyle.SHUFFLED, PalaceStyle.RANDOM_WALK)]
    [DataRow(PalaceStyle.RANDOM_WALK, PalaceStyle.VANILLA)]
    [DataRow(PalaceStyle.RANDOM_WALK, PalaceStyle.SHUFFLED)]
    public void FairyRemovalRejectsVanillaLayouts(PalaceStyle normal, PalaceStyle gp)
    {
        var config = new RandomizerConfiguration(StandardPreset.Preset.SerializeFlags())
        {
            RemoveFairy = true,
            NormalPalaceStyle = normal,
            GpStyle = gp,
        };
        Assert.Throws<UserFacingException>(config.CheckForFlagConflicts);
        Assert.Throws<UserFacingException>(() => config.Export(new Random(42)));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    [DataRow(true, true)]
    public void RemovingBothDisablesBlockersAnywhere(bool fairy, bool key)
    {
        var config = new RandomizerConfiguration(StandardPreset.Preset.SerializeFlags())
        {
            RemoveFairy = fairy,
            RemoveMagicKey = key,
            KeyLogic = KeyLogicOption.KEYS_PER_PALACE_SOLVABLE,
            BlockingRoomsInAnyPalace = true,
        };
        bool enabled = !(fairy && key);
        Assert.AreEqual(enabled, config.blockingRoomsInAnyPalaceIncluded());
        Assert.AreEqual(enabled, config.Export(new Random(42)).BlockersAnywhere);
        var roundTrip = new RandomizerConfiguration(config.SerializeFlags());
        Assert.AreEqual(fairy, roundTrip.RemoveFairy);
        Assert.AreEqual(key, roundTrip.RemoveMagicKey);
        Assert.AreEqual(enabled, roundTrip.blockingRoomsInAnyPalaceIncluded());
        Assert.AreEqual(enabled, roundTrip.Export(new Random(42)).BlockersAnywhere);
    }

    [TestMethod]
    public void RemovingBothRequiresSmallKeyLogic()
    {
        var config = new RandomizerConfiguration(StandardPreset.Preset.SerializeFlags())
        {
            RemoveFairy = true,
            RemoveMagicKey = true,
            KeyLogic = KeyLogicOption.FAIRY_OR_MAGICKEY,
        };
        Assert.Throws<UserFacingException>(config.CheckForFlagConflicts);
        Assert.Throws<UserFacingException>(() => config.Export(new Random(42)));
    }
}
