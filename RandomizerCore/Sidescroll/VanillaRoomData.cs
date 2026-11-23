using System;
using System.Linq;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.RandomizerCore.Sidescroll;

/// <summary>Reads vanilla room data at a palace group and map index.</summary>
public sealed class VanillaRoomData
{
    public const byte LastMap = 62;

    private readonly ROM rom;

    public VanillaRoomData(byte[] baseRom)
    {
        var bytes = baseRom.ToArray();
        ROM.ValidateVanillaRom(ref bytes);
        rom = new ROM(bytes);
    }

    internal VanillaRoomData(ROM rom) => this.rom = rom;

    public byte[] ReadSideview(PalaceGrouping group, byte map)
    {
        int table = group switch
        {
            PalaceGrouping.Palace125 => RomMap.SIDEVIEW_PTR_TABLE_P125,
            PalaceGrouping.Palace346 => RomMap.SIDEVIEW_PTR_TABLE_P346,
            PalaceGrouping.PalaceGp => RomMap.SIDEVIEW_PTR_TABLE_GP,
            _ => throw new ArgumentOutOfRangeException(nameof(group)),
        };
        int offset = group == PalaceGrouping.PalaceGp ? RomMap.VANILLA_GP_SIDEVIEW_POINTER_BASE : RomMap.VANILLA_PALACE_SIDEVIEW_POINTER_BASE;
        return ReadRecord(table, offset, map);
    }

    public byte[] ReadEnemies(PalaceGrouping group, byte map)
    {
        int table = group switch
        {
            PalaceGrouping.Palace125 => Enemy.Enemies.Palace125EnemyPtr,
            PalaceGrouping.Palace346 => Enemy.Enemies.Palace346EnemyPtr,
            PalaceGrouping.PalaceGp => Enemy.Enemies.GPEnemyPtr,
            _ => throw new ArgumentOutOfRangeException(nameof(group)),
        };
        int offset = group == PalaceGrouping.PalaceGp ? RomMap.VANILLA_GP_ENEMY_POINTER_BASE : RomMap.VANILLA_PALACE_ENEMY_POINTER_BASE;
        return ReadRecord(table, offset, map);
    }

    public byte[] ReadConnections(PalaceGrouping group, byte map)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(map, LastMap);
        return rom.GetBytes(Room.GetConnectionStartAddress(group, map), 4);
    }

    public byte ReadItemBits(PalaceGrouping group, byte map)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(map, LastMap);
        int table = group switch
        {
            PalaceGrouping.Palace125 => Room.Group1ItemPresenceBitsStartAddress,
            PalaceGrouping.Palace346 => Room.Group2ItemPresenceBitsStartAddress,
            PalaceGrouping.PalaceGp => Room.Group3ItemPresenceBitsStartAddress,
            _ => throw new ArgumentOutOfRangeException(nameof(group)),
        };
        byte bothMaps = rom.GetByte(table + map / 2);
        return (byte)(map % 2 == 0 ? bothMaps >> 4 : bothMaps & 0xF);
    }

    private byte[] ReadRecord(int table, int offset, byte map)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(map, LastMap);
        int pointer = table + map * 2;
        int address = offset + rom.GetShort(pointer + 1, pointer);
        if (address >= ROM.VanillaChrRomOffs)
        {
            throw new FormatException($"Vanilla room map {map} has a pointer outside PRG ROM.");
        }
        int length = rom.GetByte(address);
        if (length == 0 || address + length > ROM.VanillaChrRomOffs)
        {
            throw new FormatException($"Vanilla room map {map} has an invalid data length.");
        }
        return rom.GetBytes(address, length);
    }
}
