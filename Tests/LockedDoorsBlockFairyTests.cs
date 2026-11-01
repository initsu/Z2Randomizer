using CrossPlatformUI.Presets;
using Z2Randomizer.RandomizerCore;
using Random = Z2Randomizer.RandomizerCore.Random;

namespace Z2Randomizer.Tests;

[TestClass]
public class LockedDoorsBlockFairyTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void OptionSurvivesFlagsAndExport(bool enabled)
    {
        var config = new RandomizerConfiguration(StandardPreset.Preset.SerializeFlags())
        {
            LockedDoorsBlockFairy = enabled,
            ShareSeedAcrossDifficulty = true,
        };
        var roundTrip = new RandomizerConfiguration(config.SerializeFlags());
        Assert.AreEqual(enabled, roundTrip.LockedDoorsBlockFairy);
        Assert.AreEqual(enabled, roundTrip.Export(new Random(42)).LockedDoorsBlockFairy);
    }

    [TestMethod]
    [DataRow(PalaceStyle.VANILLA)]
    [DataRow(PalaceStyle.RANDOM_WALK)]
    public void NoMagicKeyAndNoFairyBypassRejectsLegacyKeyLogic(PalaceStyle style)
    {
        var config = new RandomizerConfiguration(StandardPreset.Preset.SerializeFlags())
        {
            LockedDoorsBlockFairy = true,
            RemoveMagicKey = true,
            NormalPalaceStyle = style,
            GpStyle = style,
            KeyLogic = KeyLogicOption.FAIRY_OR_MAGICKEY,
        };
        Assert.Throws<UserFacingException>(config.CheckForFlagConflicts);
        Assert.Throws<UserFacingException>(() => config.Export(new Random(42)));
    }

    [TestMethod]
    public void SmallKeyLogicSupportsNoMagicKeyWithoutRemovingFairy()
    {
        var config = new RandomizerConfiguration(StandardPreset.Preset.SerializeFlags())
        {
            LockedDoorsBlockFairy = true,
            RemoveMagicKey = true,
            RemoveFairy = false,
            KeyLogic = KeyLogicOption.KEYS_PER_PALACE_SOLVABLE,
            BlockingRoomsInAnyPalace = true,
        };
        var roundTrip = new RandomizerConfiguration(config.SerializeFlags());
        Assert.IsFalse(roundTrip.blockingRoomsInAnyPalaceIncluded());
        var props = roundTrip.Export(new Random(42));
        Assert.IsFalse(props.BlockersAnywhere);
        Assert.IsFalse(props.RemoveItems.Contains(Collectable.FAIRY_SPELL));
        Assert.IsTrue(props.RequiresSmallKeyValidation(6));
    }
}
