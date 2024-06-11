﻿using System.Diagnostics;
using NLog;

namespace RandomizerCore.Overworld;

[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public class Location
{
    Logger logger = LogManager.GetCurrentClassLogger();
    private int appear2loweruponexit;
    public Collectable Collectable { get; set; }
    public Collectable VanillaCollectable { get; set; }
    public bool ItemGet { get; set; }

    public Terrain TerrainType { get; set; }
    public int Ypos { get; set; }
    public int Xpos { get; set; }
    public byte[] LocationBytes { get; set; }

    public int MemAddress { get; set; }

    public int PassThrough { get; set; }

    public int Map { get; set; }

    public int World { get; set; }

    public (int, int) Coords
    {
        get
        {
            return (Ypos, Xpos);
        }

        set
        {
            Ypos = value.Item1;
            Xpos = value.Item2;
        }
    }

    //TODO: Remove all of these location requirement properties, and refactor the whole thing to just use the requirements system.
    //Probably this happens in conjunction with custom overworld.
    public bool NeedJump { get; set; }
    public bool NeedHammer { get; set; }
    public bool NeedBoots { get; set; }
    public bool NeedFairy { get; set; }
    public bool NeedFlute { get; set; }
    public bool NeedBagu { get; set; }

    //This does 2 things and should only do 1. It both tracks whether the location is a location that should be possible to shuffle,
    //and tracks whether this location has actually been shuffled already. This persistence doesn't always get reset properly,
    //which causes a bunch of bugs. That said, tracking 100 references and reworking it everywhere is beyond what I want to mess with
    //so for now i'm going to keep this crap design but I am mad about it.
    public bool CanShuffle { get; set; }
    /// <summary>
    /// Page you enter this location from, 0 means left. 1/2/3 will enter from the right on areas that have
    /// that many pages total or midscren on other areas.
    /// </summary>
    public int MapPage { get; set; }

    public int ExternalWorld { get; set; }

    public bool Reachable { get; set; }

    public int PalaceNumber { get; set; }

    public Town ActualTown { get; set; }
    public Continent Continent { get; set; }
    public int FallInHole { get; set; }
    public int ForceEnterRight { get; set; }
    public int Secondpartofcave { get; set; }

    public string Name { get; set; }

    public bool AppearsOnMap { get; set; }

    /*
    Byte 0

    .xxx xxxx - Y position
    x... .... - External to this world

    Byte 1 (offset 3F bytes from Byte 0)

    ..xx xxxx - X position
    .x.. .... - Second part of a cave
    x... .... - Appear at the position of the area in ROM offset 2 lower than this one upon exit

    Byte 2 (offset 7E bytes from Byte 0)

    ..xx xxxx - Map number
    xx.. .... - Horizontal position to enter within map
        0 = enter from the left
        1 = enter at x=256 or from the right for 2 screens maps
        2 = enter at x=512 or from the right for 3 screens maps
        3 = enter from the right for 4 screens maps

    Byte 3 (offset BD bytes from Byte 0)

    ...x xxxx - World number
    ..x. .... - Forced enter from the right edge of screen
    .x.. .... - Pass through
    x... .... - Fall in hole
    */
    public Location(byte[] bytes, Terrain t, int mem, Continent c)
    {
        LocationBytes = bytes;
        ExternalWorld = bytes[0] & 128;
        Ypos = bytes[0] & 127;
        appear2loweruponexit = bytes[1] & 128;
        Secondpartofcave = bytes[1] & 64;
        Xpos = bytes[1] & 63;
        MapPage = bytes[2] & 192;
        Map = bytes[2] & 63;
        FallInHole = bytes[3] & 128;
        PassThrough = bytes[3] & 64;
        ForceEnterRight = bytes[3] & 32;
        World = bytes[3] & 31;
        TerrainType = t;
        MemAddress = mem;
        CanShuffle = true;
        VanillaCollectable = Collectable = Collectable.DO_NOT_USE;
        ItemGet = false;
        Reachable = false;
        PalaceNumber = 0;
        ActualTown = 0;
        Continent = c;

        //Shoutouts to thetruekingofspace for datamining this
        Name = (Continent, Ypos, Xpos, Map) switch
        {
            (Continent.WEST, 52, 23, 0) => "NORTH_CASTLE",
            (Continent.WEST, 32, 29, 33) => "TROPHY_CAVE",
            (Continent.WEST, 42, 37, 43) => "FOREST_50P",
            (Continent.WEST, 60, 16, 45) => "MAGIC_CAVE",
            (Continent.WEST, 86, 20, 46) => "FOREST_100P",
            (Continent.WEST, 64, 62, 38) => "GRASS_TILE",
            (Continent.WEST, 77, 21, 44) => "LOST_WOODS_1",
            (Continent.WEST, 57, 61, 6) => "BUBBLE_CLIFF",
            (Continent.WEST, 71, 8, 51) => "EX_LIFE_SWAMP_1",
            (Continent.WEST, 92, 48, 56) => "RED_JAR_CEM",
            (Continent.WEST, 41, 48, 7) => "PARAPA_CAVE_N",
            (Continent.WEST, 46, 55, 7) => "PARAPA_CAVE_S",
            (Continent.WEST, 58, 1, 9) => "JUMP_CAVE_N",
            (Continent.WEST, 62, 3, 11) => "JUMP_CAVE_S",
            (Continent.WEST, 62, 38, 12) => "PILLAR_PBAG_CAVE",
            (Continent.WEST, 69, 9, 14) => "MEDICINE_CAVE",
            (Continent.WEST, 62, 54, 16) => "HEART_CONTAINER_CAVE",
            (Continent.WEST, 96, 50, 18) => "FAIRY_CAVE_HOLE",
            (Continent.WEST, 102, 59, 19) => "FAIRY_CAVE",
            (Continent.WEST, 82, 16, 1) => "LIFE_TOWN_BRIDGE_NS",
            (Continent.WEST, 87, 26, 2) => "LIFE_TOWN_BRIDGE_EW",
            (Continent.WEST, 97, 26, 4) => "DM_BRIDGE_EXIT_W",
            (Continent.WEST, 97, 34, 5) => "DM_BRIDGE_EXIT_E",
            (Continent.WEST, 64, 7, 20) => "MEDICINE_CAVE_FAIRY",
            (Continent.WEST, 67, 17, 51) => "RED_JAR_SWAMP",
            (Continent.WEST, 87, 33, 20) => "SARIA_FAIRY",
            (Continent.WEST, 76, 20, 21) => "LOST_WOODS_2",
            (Continent.WEST, 77, 17, 22) => "LOST_WOODS_3",
            (Continent.WEST, 78, 19, 23) => "LOST_WOODS_4",
            (Continent.WEST, 77, 23, 23) => "LOST_WOODS_5",
            (Continent.WEST, 68, 37, 57) => "P2_RED_JAR",
            (Continent.WEST, 0, 0, 57) => "RED_JAR_BEACH",
            (Continent.WEST, 102, 38, 57) => "EX_LIFE_BEACH",
            (Continent.WEST, 77, 61, 41) => "RAFT_DOCK",
            (Continent.WEST, 95, 10, 42) => "DM_ENTRANCE",
            (Continent.WEST, 96, 21, 43) => "DM_EXIT",
            (Continent.WEST, 88, 50, 60) => "KINGS_TOMB",
            (Continent.WEST, 54, 46, 2) => "RAURU",
            (Continent.WEST, 36, 2, 5) => "JUMP_TOWN",
            (Continent.WEST, 91, 8, 6) => "SARIA_S",
            (Continent.WEST, 89, 8, 8) => "SARIA_N",
            (Continent.WEST, 76, 21, 24) => "BAGUS_CABIN",
            (Continent.WEST, 75, 60, 11) => "FAIRY_TOWN",
            (Continent.WEST, 32, 62, 0) => "P1",
            (Continent.WEST, 64, 11, 14) => "P2",
            (Continent.WEST, 98, 57, 0) => "P3",
            (Continent.EAST, 58, 10, 38) => "SUNKEN_PBAG_CAVE",
            (Continent.EAST, 91, 54, 44) => "RISEN_PBAG_CAVE",
            (Continent.EAST, 76, 21, 2) => "WILSON_FENCE_1",
            (Continent.EAST, 81, 17, 3) => "WILSON_FENCE_2",
            (Continent.EAST, 84, 19, 4) => "WILSON_FENCE_3",
            (Continent.EAST, 96, 24, 5) => "WILSON_FENCE_4",
            (Continent.EAST, 93, 35, 0) => "THUNDER_TOWN_N_BRIDGE",
            (Continent.EAST, 100, 37, 1) => "THUNDER_TOWN_E_BRIDGE",
            (Continent.EAST, 36, 9, 6) => "REFLECT_TOWN_CLIFF_1",
            (Continent.EAST, 38, 10, 23) => "REFLECT_TOWN_CLIFF_2",
            (Continent.EAST, 56, 63, 7) => "WATER_TILE",
            (Continent.EAST, 52, 24, 8) => "FIRE_TOWN_CAVE_EXIT",
            (Continent.EAST, 48, 27, 8) => "FIRE_TOWN_CAVE_ENTRACE",
            (Continent.EAST, 71, 25, 9) => "SUNKEN_PBAG_CAVE",
            (Continent.EAST, 78, 31, 11) => "RISEN_PBAG_CAVE",
            (Continent.EAST, 78, 49, 13) => "NEW_KASUTO_CAVE_ENTRANCE",
            (Continent.EAST, 78, 57, 14) => "NEW_KASUTO_CAVE_EXIT",
            (Continent.EAST, 75, 2, 16) => "DEATH_VALLEY_CAVE_1_EXIT",
            (Continent.EAST, 75, 4, 16) => "DEATH_VALLEY_CAVE_1_ENTRANCE",
            (Continent.EAST, 77, 6, 19) => "DEATH_VALLEY_CAVE_2_EXIT",
            (Continent.EAST, 77, 10, 20) => "DEATH_VALLEY_CAVE_2_ENTRANCE",
            (Continent.EAST, 81, 26, 51) => "OLD_KASUTO_SWAMP_LIFE",
            (Continent.EAST, 91, 4, 61) => "DEATH_VALLEY_BATTLE_EX",
            (Continent.EAST, 64, 53, 33) => "P5_500P_BAG",
            (Continent.EAST, 56, 34, 57) => "FIRE_TOWN_RED_JAR",
            (Continent.EAST, 44, 48, 57) => "DAZZLE_LIFE",
            (Continent.EAST, 99, 57, 46) => "DESERT_TILE",
            (Continent.EAST, 68, 13, 45) => "FIRE_TOWN_FAIRY",
            (Continent.EAST, 91, 4, 62) => "DEATH_VALLEY_500P_BAG",
            (Continent.EAST, 99, 27, 62) => "DEATH_VALLEY_RED_JAR",
            (Continent.EAST, 83, 3, 25) => "DEATH_VALLEY_BATTLE_3",
            (Continent.EAST, 86, 8, 24) => "DEATH_VALLEY_BATTLE_2",
            (Continent.EAST, 99, 8, 26) => "DEATH_VALLEY_BATTLE_1",
            (Continent.EAST, 40, 52, 40) => "MAZE_ISLAND_BRIDGE",
            (Continent.EAST, 52, 7, 41) => "RAFT_DOCK",
            (Continent.EAST, 60, 23, 14) => "NABOORU",
            (Continent.EAST, 33, 3, 17) => "DARUNIA",
            (Continent.EAST, 81, 61, 18) => "NEW_KASUTO",
            (Continent.EAST, 99, 34, 23) => "OLD_KASUTO",
            (Continent.EAST, 60, 62, 35) => "P5",
            (Continent.EAST, 102, 45, 36) => "P6",
            (Continent.EAST, 73, 4, 0) => "GP",
            (Continent.DM, 42, 0, 1) => "CAVE_B_W",
            (Continent.DM, 41, 4, 1) => "CAVE_B_E",
            (Continent.DM, 40, 9, 2) => "CAVE_C_E",
            (Continent.DM, 42, 11, 2) => "CAVE_C_W",
            (Continent.DM, 43, 16, 3) => "CAVE_E_N",
            (Continent.DM, 39, 14, 3) => "CAVE_E_S",
            (Continent.DM, 45, 3, 4) => "CAVE_D_E",
            (Continent.DM, 45, 7, 4) => "CAVE_D_W",
            (Continent.DM, 48, 3, 5) => "CAVE_F_E",
            (Continent.DM, 47, 5, 5) => "CAVE_F_W",
            (Continent.DM, 47, 14, 6) => "CAVE_J_E",
            (Continent.DM, 48, 16, 6) => "CAVE_J_W",
            (Continent.DM, 51, 4, 7) => "CAVE_I_S",
            (Continent.DM, 54, 5, 7) => "CAVE_I_N",
            (Continent.DM, 48, 20, 8) => "CAVE_L_S",
            (Continent.DM, 50, 20, 8) => "CAVE_L_N",
            (Continent.DM, 52, 22, 9) => "CAVE_O_S",
            (Continent.DM, 54, 20, 9) => "CAVE_O_N",
            (Continent.DM, 59, 3, 10) => "CAVE_M_E",
            (Continent.DM, 57, 7, 10) => "CAVE_M_W",
            (Continent.DM, 58, 15, 11) => "CAVE_P_E",
            (Continent.DM, 57, 18, 11) => "CAVE_P_W",
            (Continent.DM, 60, 13, 12) => "CAVE_Q_E",
            (Continent.DM, 60, 17, 12) => "CAVE_Q_W",
            (Continent.DM, 65, 16, 13) => "CAVE_R_S",
            (Continent.DM, 63, 18, 13) => "CAVE_R_N",
            (Continent.DM, 63, 22, 14) => "CAVE_N_S",
            (Continent.DM, 60, 24, 14) => "CAVE_N_N",
            (Continent.DM, 64, 10, 15) => "HAMMER_CAVE",
            (Continent.DM, 54, 11, 22) => "ELEVATOR_CAVE_G_E_BL",
            (Continent.DM, 54, 14, 22) => "ELEVATOR_CAVE_G_W_BR",
            (Continent.DM, 47, 9, 23) => "ELEVATOR_CAVE_G_E_TL",
            (Continent.DM, 48, 11, 23) => "ELEVATOR_CAVE_G_W_TR",
            (Continent.DM, 52, 8, 24) => "ELEVATOR_CAVE_H_E_TL",
            (Continent.DM, 51, 10, 24) => "ELEVATOR_CAVE_H_W_TR",
            (Continent.DM, 40, 18, 25) => "ELEVATOR_CAVE_H_D_BL",
            (Continent.DM, 44, 18, 25) => "ELEVATOR_CAVE_H_N_BR",
            (Continent.MAZE, 62, 45, 34) => "MAZE_ISLAND_FORCED_BATTLE_2",
            (Continent.MAZE, 68, 48, 35) => "MAZE_ISLAND_FORCED_BATTLE_1",
            (Continent.MAZE, 58, 41, 36) => "MAZE_ISLAND_MAGIC",
            (Continent.DM, 67, 40, 40) => "EAST_HYRULE_BRIDGE",
            (Continent.DM, 37, 7, 42) => "CAVE_A",
            (Continent.DM, 37, 23, 43) => "CAVE_K",
            (Continent.MAZE, 58, 60, 15) => "P4",
            (Continent.MAZE, 60, 57, 37) => "MAZE_ISLAND_CHILD",
            (Continent.DM, 64, 8, 26) => "DM_MAGIC",
            (Continent.MAZE, 58, 48, 47) => "MAZE_ISLAND_FORCED_BATTLE_3",
            (Continent.MAZE, 49, 51, 48) => "MAZE_ISLAND_FORCED_BATTLE_7",
            (Continent.MAZE, 50, 46, 49) => "MAZE_ISLAND_FORCED_BATTLE_4",
            (Continent.MAZE, 46, 48, 50) => "MAZE_ISLAND_FORCED_BATTLE_5",
            (Continent.MAZE, 42, 50, 51) => "MAZE_ISLAND_FORCED_BATTLE_6",

            //Junk locations
            (Continent.DM, 127, 0, 41) => "UNKNOWN",
            (Continent.EAST, 77, 61, 41) => "FAKE_RAFT",
            (Continent.DM, 77, 61, 41) => "FAKE_RAFT",
            (Continent.MAZE, 127, 0, 41) => "UNKNOWN",
            (Continent.WEST, 127, 0, 41) => "UNKNOWN",
            (Continent.WEST, 52, 7, 41) => "FAKE_RAFT",
            (Continent.MAZE, 52, 7, 41) => "FAKE_RAFT",
            (Continent.EAST, 67, 40, 40) => "FAKE_MAZE_BRIDGE",
            (Continent.MAZE, 77, 61, 40) => "FAKE_RAFT",
            (Continent.MAZE, 40, 52, 40) => "FAKE_MAZE_BRIDGE",
            (Continent.MAZE, 77, 61, 41) => "FAKE_RAFT",
            (Continent.WEST, 37, 7, 42) => "FAKE_DM_EXIT",
            (Continent.WEST, 67, 40, 40) => "FAKE_MAZE_BRIDGE",
            (Continent.EAST, 127, 0, 41) => "UNKNOWN",
            (Continent.MAZE, 127, 0, 40) => "UNKNOWN",
            (Continent.DM, 52, 7, 40) => "FAKE_RAFT",
            (Continent.MAZE, 67, 40, 40) => "FAKE_MAZE_BRIDGE",
            (Continent.DM, 82, 10, 40) => "FAKE_MAZE_BRIDGE",
            (Continent.MAZE, 95, 10, 42) => "FAKE_DM_ENTRANCE",
            (Continent.DM, 127, 0, 40) => "UNKNOWN",
            (Continent.DM, 52, 7, 41) => "FAKE_RAFT",
            (Continent.DM, 95, 10, 42) => "FAKE_DM_ENTRANCE",
            (Continent.DM, 40, 52, 42) => "FAKE_MAZE_BRIDGE",
            (Continent.WEST, 0, 0, 43) => "UNKNOWN",
            (Continent.WEST, 40, 52, 43) => "FAKE_MAZE_BRIDGE",
            (Continent.EAST, 96, 21, 43) => "FAKE_DM_ENTRANCE",
            (Continent.WEST, 40, 52, 40) => "FAKE_MAZE_BRIDGE",
            (Continent.EAST, 95, 10, 42) => "FAKE_DM_ENTRANCE",
            (Continent.MAZE, 0, 0, 43) => "UNKNOWN",
            (Continent.EAST, 37, 7, 42) => "FAKE_DM_EXIT",
            (Continent.MAZE, 37, 23, 43) => "FAKE_DM_EXIT",
            (Continent.DM, 40, 52, 43) => "FAKE_MAZE_BRIDGE",
            (Continent.MAZE, 37, 7, 42) => "FAKE_DM_EXIT",
            (Continent.DM, 0, 7, 42) => "UNKNOWN",
            (Continent.WEST, 0, 0, 42) => "UNKNOWN",
            (Continent.DM, 0, 0, 42) => "UNKNOWN",
            (Continent.EAST, 37, 23, 42) => "FAKE_DM_EXIT",
            (Continent.DM, 40, 52, 40) => "FAKE_MAZE_BRIDGE",
            (Continent.MAZE, 0, 0, 42) => "UNKNOWN",
            (Continent.EAST, 37, 23, 43) => "FAKE_DM_EXIT",
            (Continent.MAZE, 96, 21, 43) => "FAKE_DM_ENTRANCE",
            (Continent.WEST, 37, 23, 43) => "FAKE_DM_EXIT",
            (Continent.DM, 96, 21, 43) => "FAKE_DM_ENTRANCE",

            (_, _, _, _) => "Unknown (" + Continent.GetName(Continent) + ")"
        };
        if(Name.StartsWith("Unknown") && Xpos != 0 && Ypos != 0)
        {
            logger.Info("Missing location name on " + Continent.GetName(Continent) + " (" + Xpos + ", " + Ypos + ") Map: " + Map);
        }
    }

    //Why does this empty constructor exist. Don't want to delete it if it's needed for serialization magic of some kind.
    public Location()
    {

    }
    //These bytes should not be kept synchromized on the object. They should just be written when the state pushes to the ROM
    //at the very end.
    public void UpdateBytes()
    {
        if (NeedHammer || NeedFlute)
        {
            LocationBytes[0] = 0;
        }
        else
        {
            LocationBytes[0] = (byte)(ExternalWorld + Ypos);
        }
        LocationBytes[1] = (byte)(appear2loweruponexit + Secondpartofcave + Xpos);
        LocationBytes[2] = (byte)(MapPage + Map);
        LocationBytes[3] = (byte)(FallInHole + PassThrough + ForceEnterRight + World);
    }

    public string GetDebuggerDisplay()
    {
        return Continent.ToString()
            + " " + TerrainType.ToString()
            + " " + Name
            + " (" + (Ypos - 30) + "," + (Xpos) + ") _"
            + (Reachable ? "Reachable " : "Unreachable ")
            + (Collectable == Collectable.DO_NOT_USE ? "" : Collectable.ToString());
    }

    public bool HasVanillaItem()
    {
        return Collectable == VanillaCollectable;
    }
}
