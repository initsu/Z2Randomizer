using System.Text.Json;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Enemy;
using Z2Randomizer.RandomizerCore.Sidescroll;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.Tests;

internal static class TestRoomFiles
{
    internal static string ReadBundledJson() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "PalaceRooms.json"));

    internal static PalaceRooms LoadNonVanillaRooms() => LoadBundledSubset(vanilla: false);

    internal static PalaceRooms LoadVanillaRoomsWithBaseRom() => LoadBundledSubset(vanilla: true, ReadBaseRom());

    private static PalaceRooms LoadBundledSubset(bool vanilla, VanillaRoomData? vanillaRoomData = null)
    {
        using var json = JsonDocument.Parse(ReadBundledJson());
        var definitions = json.RootElement.EnumerateArray()
            .Where(definition => IsVanilla(definition) == vanilla)
            .Select(definition => definition.GetRawText());
        return new PalaceRooms($"[{string.Join(',', definitions)}]", false, vanillaRoomData);

        static bool IsVanilla(JsonElement definition) =>
            RoomDefinitionFields.IsVanilla(RoomDefinitionFields.Read(definition));
    }

    // For tests that use the bundled room names and metadata but do not exercise vanilla geometry.
    internal static PalaceRooms LoadBundledWithSyntheticRom()
    {
        var rom = new ROM(new byte[ROM.VanillaRomSize]);
        AddRoomTables(RomMap.SIDEVIEW_PTR_TABLE_P125, Enemies.Palace125EnemyPtr, 0x8010, 0x98B0);
        AddRoomTables(RomMap.SIDEVIEW_PTR_TABLE_P346, Enemies.Palace346EnemyPtr, 0x8010, 0x98B0);
        AddRoomTables(RomMap.SIDEVIEW_PTR_TABLE_GP, Enemies.GPEnemyPtr, 0xC010, 0xD8B0);
        return new PalaceRooms(ReadBundledJson(), false, new VanillaRoomData(rom));

        void AddRoomTables(int sideviewTable, int enemyTable, int sideviewBase, int enemyBase)
        {
            const int sideviewPointer = 0x9000;
            const int enemyPointer = 0x8000;
            rom.Put(sideviewBase + sideviewPointer, [4, 0x60, 0, 0]);
            rom.Put(enemyBase + enemyPointer, 1);
            for (int map = 0; map <= VanillaRoomData.LastMap; map++)
            {
                int sideview = sideviewTable + 2 * map;
                int enemies = enemyTable + 2 * map;
                rom.PutShort(sideview + 1, sideview, sideviewPointer);
                rom.PutShort(enemies + 1, enemies, enemyPointer);
            }
        }
    }

    internal static PalaceRooms LoadBundledWithBaseRom() => new(ReadBundledJson(), false, ReadBaseRom());

    private static VanillaRoomData ReadBaseRom()
    {
        var path = Environment.GetEnvironmentVariable("Z2R_TEST_ROM");
        if (string.IsNullOrWhiteSpace(path))
        {
            Assert.Inconclusive("Set Z2R_TEST_ROM to run tests that inspect the vanilla ROM room data.");
        }
        return new VanillaRoomData(File.ReadAllBytes(path!));
    }
}
