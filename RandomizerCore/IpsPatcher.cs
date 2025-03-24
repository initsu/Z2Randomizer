using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RandomizerCore;

/// <summary>
/// Applies IPS Patches
/// </summary>
internal class IpsPatcher
{
    static readonly IReadOnlyList<byte> PatchSig = Encoding.ASCII.GetBytes("PATCH");
    static readonly IReadOnlyList<byte> EofSig = Encoding.ASCII.GetBytes("EOF");

    public static void Patch(byte[] romData, byte[] ipsData, bool expandRom = false)
    {
        Debug.Assert(PatchSig.SequenceEqual(new ArraySegment<byte>(ipsData, 0, PatchSig.Count)));

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

            if (expandRom && tgtOffs + size > ROM.VanillaChrRomOffs)
            {
                if (tgtOffs < ROM.VanillaChrRomOffs)
                {
                    int segSize = ROM.VanillaChrRomOffs - tgtOffs;
                    if (fillValue is not null)
                        Array.Fill<byte>(romData, (byte)fillValue, tgtOffs, segSize);
                    else
                    {
                        Array.Copy(ipsData, ipsOffs, romData, tgtOffs, segSize);
                        ipsOffs += segSize;
                    }

                    tgtOffs += segSize;
                    size -= segSize;
                }

                tgtOffs += ROM.ChrRomOffset - ROM.VanillaChrRomOffs;
            }

            if (fillValue is not null)
                Array.Fill<byte>(romData, (byte)fillValue, tgtOffs, size);
            else
            {
                Array.Copy(ipsData, ipsOffs, romData, tgtOffs, size);
                ipsOffs += size;
            }
        }
    }

    public static void PatchSpriteSanitized(byte[] romData, byte[] ipsData, bool expandRom)
    {
        Debug.Assert(PatchSig.SequenceEqual(new ArraySegment<byte>(ipsData, 0, PatchSig.Count)));

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

            int srcOffs = tgtOffs;
            if (expandRom && tgtOffs + size > ROM.VanillaChrRomOffs)
            {
                if (tgtOffs < ROM.VanillaChrRomOffs)
                {
                    int segSize = ROM.VanillaChrRomOffs - tgtOffs;

                    for (int i = 0; i < segSize; i++)
                    {
                        if (IsSanitizedSpriteAddress(srcOffs + i))
                        {
                            if (fillValue is not null)
                            {
                                romData[tgtOffs + i] = (byte)fillValue;
                            }
                            else
                            {
                                romData[tgtOffs + i] = ipsData[ipsOffs + i];
                            }
                        }
                    }
                    if (fillValue is null)
                    {
                        ipsOffs += segSize;
                    }

                    tgtOffs += segSize;
                    srcOffs += segSize;
                    size -= segSize;
                }

                tgtOffs += ROM.ChrRomOffset - ROM.VanillaChrRomOffs;
            }

            for (int i = 0; i < size; i++)
            {
                if (IsSanitizedSpriteAddress(srcOffs + i))
                {
                    if (fillValue is not null)
                    {
                        romData[tgtOffs + i] = (byte)fillValue;
                    }
                    else
                    {
                        romData[tgtOffs + i] = ipsData[ipsOffs + i];
                    }
                }
            }
            if (fillValue is null)
            {
                ipsOffs += size;
            }
        }
    }

    private static bool IsSanitizedSpriteAddress(int addr)
    {
        // NOTE: this isn't written in stone, if anything
        // more should be allowed, let us know.

        if (ROM.VanillaChrRomOffs <= addr) {
            return true;
        }

        // Link's palettes
        if (ROM.LinkOutlinePaletteAddr.Contains(addr)) { return true; }
        if (ROM.LinkFacePaletteAddr.Contains(addr)) { return true; }
        if (ROM.LinkTunicPaletteAddr.Contains(addr)) { return true; }
        if (ROM.LinkShieldPaletteAddr == addr) { return true; }
        // Zelda's palettes
        if (ROM.ZeldaOutlinePaletteAddr.Contains(addr)) { return true; }
        if (ROM.ZeldaFacePaletteAddr.Contains(addr)) { return true; }
        if (ROM.ZeldaDressPaletteAddr.Contains(addr)) { return true; }
        // iNES header
        if (addr < 0x10) { return false; }
        // PPU addr before Game over
        if (0x10 <= addr && addr < 0x12) { return true; }
        // Not allowing change of length of the text
        if (0x12 == addr) { return false; }
        // text: Game over
        if (0x13 <= addr && addr < 0x13 + 0x0a) { return true; }
        // PPU addr before Return of Ganon
        if (0x1d <= addr && addr < 0x1f) { return true; }
        // Not allowing change of length of the text
        if (0x1f == addr) { return false; }
        // text: Return of Ganon
        if (0x20 <= addr && addr < 0x20 + 0x0f) { return true; }
        // Palette Mappings
        if (0x2f <= addr && addr < 0xE5) { return true; }

        // Beam sword projectile
        // LDA      #$32                      ; 0x18fa  A9 32
        // STA      ,y                        ; 0x18fc  99 01 02  --  writes to RAM 0x239
        if (addr == 0x18FB) { return true; }
        // Level up pane
        if (0x1bba <= addr && addr < 0x1c2a) { return true; }

        // Table_for_Links_Palettes_Probably
        if (0x2a00 <= addr && addr < 0x2a18) { return true; }

        // bank1_Pointer_table_for_Background_Areas_Data
        if (0x4010 <= addr && addr < 0x401e) { return false; }
        // Palettes_for_Overworld
        if (0x401e <= addr && addr < 0x40fe) { return true; }

        // bank1_Area_Pointers_West_Hyrule
        if (0x4533 <= addr && addr < 0x45b1) { return false; }
        // bank1_Area_Data__West_Hyrule_Random_Battle___Desert__South_West_Hyrule_
        if (0x478f <= addr && addr < 0x479f) { return false; }
        // bank1_Background_Areas_Data
        if (0x4c4c <= addr && addr < 0x506c) { return false; }

        // bank1_Area_Pointers_Death_Mountain
        if (0x6010 <= addr && addr < 0x610c) { return false; }
        // Area_Data_Death_Mountain_And_Maze
        if (0x627c <= addr && addr < 0x665c) { return false; }
        // Blank data
        if (0x6943 <= addr && addr < 0x7f80) { return true; }

        // bank2_Pointer_table_for_background_level_data
        if (0x8010 <= addr && addr < 0x801e) { return false; }
        // Palettes_for_Overworld
        if (0x801e <= addr && addr < 0x80fe) { return true; }
        // bank2_Background_Areas_Data
        if (0x8c72 <= addr && addr < 0x8ce8) { return false; }

        // bank2_Area_Pointers_Maze_Island
        if (0xA010 <= addr && addr < 0xA10c) { return false; }
        // bank2: Area_Data_Death_Mountain_And_Maze
        if (0xA27c <= addr && addr < 0xA65c) { return false; }
        // Blank data
        if (0xA943 <= addr && addr < 0xBf80) { return true; }

        // Palettes for towns
        if (0xC01e <= addr && addr < 0xC0fe) { return true; }
        // Area objects tile mappings
        if (0xC3da <= addr && addr < 0xC51c) { return true; }
        // bank3_Area_Pointers__Towns
        if (0xC533 <= addr && addr < 0xC5b1) { return false; }
        // bank3_Area_Data_Towns1
        if (0xC9d0 <= addr && addr < 0xCb9e) { return false; }
        // bank3_SmallObjectsConstructionRoutines_Locked_Door_glitched_tiles_0E
        if (0xCb9e <= addr && addr < 0xCba5) { return false; }
        // bank3_Objects_Construction_Routines_Bushes_1_high_X_wide_Y_Position_A__1x
        if (0xCb9e <= addr && addr < 0xCba5) { return false; }
        // bank3_Object_Construction_Routine
        if (0xCba5 <= addr && addr < 0xCbc1) { return false; }
        // Blank data
        if (0xCbc1 <= addr && addr < 0xD100) { return true; }

        // bank3_Pointer_table_for_Objects_Construction_Routines
        if (0xDbaf <= addr && addr < 0xDbdb) { return false; }
        // bank3_Table_for_Small_Objects_Construction_Routines
        if (0xDbed <= addr && addr < 0xDc21) { return false; }
        // bank3_Area_Data_Towns3
        if (0xDcd2 <= addr && addr < 0xEfce) { return false; }

        // bank3_Dialogs_Pointer_Table_Towns_in_West_Hyrule
        if (0xEfce <= addr && addr < 0xF092) { return true; }

        // Blank data
        if (0xF813 <= addr && addr < 0xFf80) { return true; }

        // bank4_Default_Palettes_for_Palaces_Type_A_B_
        if (0x1001e <= addr && addr < 0x100fe) { return true; }
        // bank4_Area_Pointers_Palaces_Type_A
        if (0x10533 <= addr && addr < 0x1072b) { return false; }
        // bank4_Area_Data
        if (0x10c83 <= addr && addr < 0x10f26) { return false; }

        // Blank data
        if (0x12775 <= addr && addr < 0x12910) { return true; }

        // Palettes for Great Palace
        if (0x1401e <= addr && addr < 0x140fe) { return true; }
        // bank5_Area_Data_Great_Palace2
        if (0x14533 <= addr && addr < 0x145b1) { return false; }
        // bank5_Area_Pointers_Great_Palace
        if (0x14827 <= addr && addr < 0x148b0) { return false; }
        // bank5_Area_Data_Great_Palace3
        if (0x149e8 <= addr && addr < 0x14b41) { return false; }
        // bank5_Ending_Text_Zelda_
        if (0x14df1 <= addr && addr < 0x14e1a) { return true; }

        // bank5_End_Credits
        if (0x1528d <= addr && addr < 0x153bd) { return true; }

        // bank5_table_intro_screen_text (Sprite credits stored at 0x16abb)
        if (0x16942 <= addr && addr < 0x16af5) { return true; }
        
        // Blank data
        if (0x17db1 <= addr && addr < 0x17f70) { return true; }

        // bank7_Table_for_Overworld_Palettes
        if (0x1c468 <= addr && addr < 0x1c48c) { return true; }

        // bank7_Table_for_some_Palettes
        if (0x1d0cd <= addr && addr < 0x1d0e1) { return true; }
        // bank7_Tables_for_some_PPU_Command_Data
        // Addr before Magic? icon?
        if (0x1d0e1 <= addr && addr < 0x1d0e3) { return true; }
        // Not allowing change of length of the MAGIC- text
        if (0x1d0e3 == addr) { return false; }
        // text: MAGIC-
        if (0x1d0e4 <= addr && addr < 0x1d0ea) { return true; }
        // Addr before Life? icon?
        if (0x1d0ea <= addr && addr < 0x1d0ec) { return true; }
        // Not allowing change of length of the text
        if (0x1d0ec == addr) { return false; }
        // text: LIFE-
        if (0x1d0ed <= addr && addr < 0x1d0f2) { return true; }

        return false;
    }

    public static void Patch(byte[] romData, string patchName, bool expandRom = false)
    {
        byte[]? ipsData = null;
        using (var ipsStream = new FileStream(patchName, FileMode.Open, FileAccess.Read))
        {
            ipsData = new byte[checked((int)ipsStream.Length)];
            ipsStream.ReadAtLeast(new(ipsData), ipsData.Length);
        }

        Patch(romData, ipsData, expandRom);
    }
}
