using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Z2Randomizer.RandomizerCore;

internal class AddressRange
{
    public int Start { get; set; }
    public int Length { get; set; }

    public int End => Start + Length; // exclusive

    public AddressRange(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public static HashSet<int> CreateSet(AddressRange[] ranges) => [.. ranges.SelectMany(r => Enumerable.Range(r.Start, r.Length))];

    public static AddressRange[] ChrRanges(int start, int end, int[] pages)
    {
        var length = end - start;
        return pages.Select(page =>
        {
            int romAddrStart = ROM.VanillaChrRomOffs + page * 0x1000 + (start % 0x2000);
            return new AddressRange(romAddrStart, length);
        }).ToArray();
    }

    public bool Contains(int addr) => Start <= addr && addr < End;

    public bool Overlaps(AddressRange other) => Start < other.End && other.Start < End;
}

/// <summary>
/// Applies sanitized IPS sprite patches
/// </summary>
public class SpritePatcher
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    static readonly IReadOnlyList<byte> PatchSig = Encoding.ASCII.GetBytes("PATCH");
    static readonly IReadOnlyList<byte> EofSig = Encoding.ASCII.GetBytes("EOF");

    public static readonly HashSet<int> ChrItemAddresses = AddressRange.CreateSet([
        .. AddressRange.ChrRanges(0x0660, 0x0680, [0,0x2,0x4,0x6,0x8,0xA,0xC,0xE,0x10,0x12,0x14,0x16,0x18]), // Key sprite
        .. AddressRange.ChrRanges(0x0720, 0x0740, [0,0x2,0x4,0x6,0x8,0xA,0xC,0xE,0x10,0x12,0x14,0x16,0x18]), // P-bag sprite
        .. AddressRange.ChrRanges(0x08a0, 0x09c0, [0,0x2,0x4,0x6,0x8,0xA,0xC,0xE,0x10,0x12,0x14,0x16,0x18]), // Most pickup items
        .. AddressRange.ChrRanges(0x1800, 0x1840, [0,0x2,0x4,0x6,0x8,0xA,0xC,0xE,0x10,0x12,0x14,0x16,0x18]), // Heart, Magic container
    ]);

    public static void PatchSpriteSanitized(string filename, byte[] romData, byte[] ipsData, bool expandRom, bool changeItems, bool changeGameOver = true, bool changeDialog = true, bool changeEmptySpace = true)
    {
        int ipsOffs = PatchSig.Count;
        while (!EofSig.SequenceEqual(new ArraySegment<byte>(ipsData, ipsOffs, EofSig.Count)))
        {
            int tgtOffs = ((int)ipsData[ipsOffs] << 16)
                | ((int)ipsData[ipsOffs + 1] << 8)
                | ipsData[ipsOffs + 2];
            ipsOffs += 3;

            int size = ((int)ipsData[ipsOffs] << 8) | ipsData[ipsOffs + 1];
            ipsOffs += 2;

            byte? fillValue = null;
            if (size == 0)
            {
                size = ((int)ipsData[ipsOffs] << 8) | ipsData[ipsOffs + 1];
                ipsOffs += 2;

                fillValue = ipsData[ipsOffs++];
            }

            // destination ROM base addr before rom expansion
            int srcOffs = tgtOffs;
            if (expandRom && tgtOffs + size > ROM.VanillaChrRomOffs)
            {
                if (tgtOffs < ROM.VanillaChrRomOffs)
                {
                    int segSize = ROM.VanillaChrRomOffs - tgtOffs;
                    PatchIfSanitized(ipsOffs, srcOffs, tgtOffs, segSize, fillValue);
                    if (fillValue is null)
                    {
                        ipsOffs += segSize;
                    }

                    size -= segSize;

                    // just continue unless the patch crossed PRG-CHR boundry
                    if (size == 0) { continue; }

                    tgtOffs += segSize;
                    srcOffs += segSize;
                }

                tgtOffs += ROM.ChrRomOffset - ROM.VanillaChrRomOffs;
            }

            PatchIfSanitized(ipsOffs, srcOffs, tgtOffs, size, fillValue);
            if (fillValue is null)
            {
                ipsOffs += size;
            }
        }

        void PatchIfSanitized(int ipsOffs, int srcOffs, int tgtOffs, int size, byte? fillValue)
        {
            if (IsSanitizedSpriteAddress(srcOffs, size, changeItems, changeGameOver, changeDialog, changeEmptySpace))
            {
                if (fillValue != null)
                {
                    Array.Fill(romData, (byte)fillValue, tgtOffs, size);
                }
                else
                {
                    Array.Copy(ipsData, ipsOffs, romData, tgtOffs, size);
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    int addr = srcOffs + i;
                    byte newValue = fillValue != null ? (byte)fillValue : ipsData[ipsOffs + i];
                    if (IsSanitizedSpriteAddress(addr, 1, changeItems, changeGameOver, changeDialog, changeEmptySpace))
                    {
                        romData[tgtOffs + i] = newValue;
                    }
                    else
                    {
                        if (addr < ROM.VanillaChrRomOffs) // don't warn about changeItems=false moderation
                        {
                            byte oldValue = romData[tgtOffs + i];
                            logger.Warn($"Moderating IPS patch \"{filename}\" write at address: 0x{addr:x5} from 0x{oldValue:x2} to 0x{newValue:x2}");
                        }
                    }
                }
            }
        }
    }

    private static bool IsSanitizedSpriteAddress(int addr, int length, bool changeItems, bool changeGameOver, bool changeDialog, bool changeEmptySpace)
    {
        // NOTE: this isn't written in stone, if anything
        // more should be allowed, let us know.

        var requestedRange = new AddressRange(addr, length);

        // item sprite logic
        if (addr >= ROM.VanillaChrRomOffs)
        {
            if (!changeItems)
            {
                if (ChrItemAddresses.Any(requestedRange.Contains))
                { 
                    return false;
                }
            }
            return true;
        }

        // Link's palettes
        if (ROM.LinkOutlinePaletteAddr.Contains(addr) && requestedRange.Length == 1) { return true; }
        if (ROM.LinkFacePaletteAddr.Contains(addr) && requestedRange.Length == 1) { return true; }
        if (ROM.LinkTunicPaletteAddr.Contains(addr) && requestedRange.Length == 1) { return true; }
        if (ROM.LinkShieldPaletteAddr == addr && requestedRange.Length == 1) { return true; }
        // Zelda's palettes
        if (ROM.ZeldaOutlinePaletteAddr.Contains(addr) && requestedRange.Length == 1) { return true; }
        if (ROM.ZeldaFacePaletteAddr.Contains(addr) && requestedRange.Length == 1) { return true; }
        if (ROM.ZeldaDressPaletteAddr.Contains(addr) && requestedRange.Length == 1) { return true; }
        // iNES header
        if (requestedRange.Start < 0x10) { return false; }
        // Allow custom game over screens. Since this contains PPU pointers,
        // it can probably crash if the patch is bad. We allow everything here
        // until the next section. It's unlikely it will lead to *hard-to-trace* crashes.
        // (We're keeping the final FF ending byte at 0xE4.)
        if (requestedRange.Start >= 0x10 && requestedRange.End <= 0xE4) { return changeGameOver; }

        // Beam sword projectile
        // LDA      #$32                      ; 0x18fa  A9 32
        // STA      ,y                        ; 0x18fc  99 01 02  --  writes to RAM 0x239
        if (addr == 0x18FB && requestedRange.Length == 1) { return true; }
        // Level up pane
        if (requestedRange.Start >= 0x1bba && requestedRange.End <= 0x1c2a) { return true; }

        // Table_for_Links_Palettes_Probably
        if (requestedRange.Start >= 0x2a00 && requestedRange.End <= 0x2a18) { return true; }

        // bank1_Pointer_table_for_Background_Areas_Data
        // This is the sideview map pointer table for the background maps in the west
        // We deny all map command changes and pointers to room changes
        if (requestedRange.Start >= 0x4010 && requestedRange.End <= 0x401e) { return false; }
        // Palettes_for_Overworld
        if (requestedRange.Start >= 0x401e && requestedRange.End <= 0x40fe) { return true; }

        // bank1_Area_Pointers_West_Hyrule
        if (requestedRange.Start >= 0x4533 && requestedRange.End <= 0x45b1) { return false; }
        // bank1_Area_Data__West_Hyrule_Random_Battle___Desert__South_West_Hyrule_
        if (requestedRange.Start >= 0x478f && requestedRange.End <= 0x479f) { return false; }
        // bank1_Background_Areas_Data
        if (requestedRange.Start >= 0x4c4c && requestedRange.End <= 0x506c) { return false; }

        // bank1_Area_Pointers_Death_Mountain
        if (requestedRange.Start >= 0x6010 && requestedRange.End <= 0x610c) { return false; }
        // Area_Data_Death_Mountain_And_Maze
        if (requestedRange.Start >= 0x627c && requestedRange.End <= 0x665c) { return false; }
        // Blank data in bank 1
        if (requestedRange.Start >= 0x6943 && requestedRange.End <= 0x7f80) { return changeEmptySpace; }

        // bank2_Pointer_table_for_background_level_data
        if (requestedRange.Start >= 0x8010 && requestedRange.End <= 0x801e) { return false; }
        // Palettes_for_Overworld
        if (requestedRange.Start >= 0x801e && requestedRange.End <= 0x80fe) { return true; }
        // bank2_Background_Areas_Data
        if (requestedRange.Start >= 0x8c72 && requestedRange.End <= 0x8ce8) { return false; }

        // bank2_Area_Pointers_Maze_Island
        if (requestedRange.Start >= 0xA010 && requestedRange.End <= 0xA10c) { return false; }
        // bank2: Area_Data_Death_Mountain_And_Maze
        if (requestedRange.Start >= 0xA27c && requestedRange.End <= 0xA65c) { return false; }
        // Blank data in bank 2
        if (requestedRange.Start >= 0xA943 && requestedRange.End <= 0xBf80) { return changeEmptySpace; }

        // Palettes for towns
        if (requestedRange.Start >= 0xC01e && requestedRange.End <= 0xC0fe) { return true; }
        // Area objects tile mappings
        if (requestedRange.Start >= 0xC3da && requestedRange.End <= 0xC51c) { return false; }
        // bank3_Area_Pointers__Towns
        if (requestedRange.Start >= 0xC533 && requestedRange.End <= 0xC5b1) { return false; }
        // bank3_Area_Data_Towns1
        if (requestedRange.Start >= 0xC9d0 && requestedRange.End <= 0xCb9e) { return false; }
        // bank3_SmallObjectsConstructionRoutines
        if (requestedRange.Start >= 0xCb9e && requestedRange.End <= 0xCba5) { return false; }
        // bank3_Object_Construction_Routine
        if (requestedRange.Start >= 0xCba5 && requestedRange.End <= 0xCbc1) { return false; }
        // Blank data in bank 3
        if (requestedRange.Start >= 0xCbc1 && requestedRange.End <= 0xD100) { return changeEmptySpace; }

        // bank3_Pointer_table_for_Objects_Construction_Routines
        if (requestedRange.Start >= 0xDbaf && requestedRange.End <= 0xDbdb) { return false; }
        // bank3_Table_for_Small_Objects_Construction_Routines
        if (requestedRange.Start >= 0xDbed && requestedRange.End <= 0xDc21) { return false; }
        // bank3_Area_Data_Towns3
        if (requestedRange.Start >= 0xDcd2 && requestedRange.End <= 0xEfce) { return false; }

        // bank3_Dialogs_Pointer_Table_Towns_in_West_Hyrule
        if (requestedRange.Start >= 0xEfce && requestedRange.End <= 0xF092) { return changeDialog; }

        // Blank data in bank 3
        if (requestedRange.Start >= 0xF813 && requestedRange.End <= 0xFf80) { return changeEmptySpace; }

        // bank4_Default_Palettes_for_Palaces_Type_A_B_
        if (requestedRange.Start >= 0x1001e && requestedRange.End <= 0x100fe) { return true; }
        // bank4_Area_Pointers_Palaces_Type_A
        if (requestedRange.Start >= 0x10533 && requestedRange.End <= 0x1072b) { return false; }
        // bank4_Area_Data
        if (requestedRange.Start >= 0x10c83 && requestedRange.End <= 0x10f26) { return false; }

        // Blank data in bank 4
        if (requestedRange.Start >= 0x12775 && requestedRange.End <= 0x12910) { return changeEmptySpace; }

        // Palettes for Great Palace
        if (requestedRange.Start >= 0x1401e && requestedRange.End <= 0x140fe) { return true; }
        // bank5_Area_Data_Great_Palace2
        if (requestedRange.Start >= 0x14533 && requestedRange.End <= 0x145b1) { return false; }
        // bank5_Area_Pointers_Great_Palace
        if (requestedRange.Start >= 0x14827 && requestedRange.End <= 0x148b0) { return false; }
        // bank5_Area_Data_Great_Palace3
        if (requestedRange.Start >= 0x149e8 && requestedRange.End <= 0x14b41) { return false; }
        // bank5_Ending_Text_Zelda_
        if (requestedRange.Start >= 0x14df1 && requestedRange.End <= 0x14e1a) { return true; }
        // bank5_End_Credits
        if (requestedRange.Start >= 0x1528d && requestedRange.End <= 0x153bd) { return changeEmptySpace; }

        // bank5_table_intro_screen_text (Sprite credits stored at 0x16abb)
        if (requestedRange.Start >= 0x16942 && requestedRange.End <= 0x16af5) { return true; }

        // Blank data in bank 5
        if (requestedRange.Start >= 0x17db1 && requestedRange.End <= 0x17f70) { return changeEmptySpace; }

        // bank7_Table_for_Overworld_Palettes
        if (requestedRange.Start >= 0x1c468 && requestedRange.End <= 0x1c48c) { return true; }

        // bank7_Table_for_some_Palettes
        if (requestedRange.Start >= 0x1d0cd && requestedRange.End <= 0x1d0e1) { return true; }
        // bank7_Tables_for_some_PPU_Command_Data
        // Addr before Magic? icon?
        if (requestedRange.Start >= 0x1d0e1 && requestedRange.End <= 0x1d0e3) { return true; }
        // Not allowing change of length of the MAGIC- text
        if (requestedRange.Start == 0x1d0e3) { return false; }
        // text: MAGIC-
        if (requestedRange.Start >= 0x1d0e4 && requestedRange.End <= 0x1d0ea) { return true; }
        // Addr before Life? icon?
        if (requestedRange.Start >= 0x1d0ea && requestedRange.End <= 0x1d0ec) { return true; }
        // Not allowing change of length of the text
        if (requestedRange.Start == 0x1d0ec) { return false; }
        // text: LIFE-
        if (requestedRange.Start >= 0x1d0ed && requestedRange.End <= 0x1d0f2) { return true; }

        // bank7_Continue_Save_Screen_Tile_Mappings
        if (requestedRange.Start >= 0x1fddb && requestedRange.End <= 0x1fe86) { return true; }

        return false;
    }
}
