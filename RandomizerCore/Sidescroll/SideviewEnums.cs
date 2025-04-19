using System;

namespace RandomizerCore.Sidescroll;

/// <summary>
/// IDs for overworld forest sideviews. Overworld sideviews with ObjectSet bit set to 0 in the header, to be exact.
/// </summary>
public enum ForestObject
{
    Headstone = 0x00,
    Cross = 0x01,
    AngledCross = 0x02,
    TreeStump = 0x03,
    Stonehenge = 0x04,
    LockedDoor = 0x05,
    SleepingZelda1 = 0x06,
    SleepingZelda2 = 0x07,
    VerticalPit = 0x08,
    LargeCloud = 0x09,
    SmallCloud0A = 0x0A,
    SmallCloud0B = 0x0B,
    SmallCloud0C = 0x0C,
    SmallCloud0D = 0x0D,
    SmallCloud0E = 0x0E,
    ForestCeiling = 0x20,
    ForestCeiling2 = 0x30,
    Curtains2High = 0x40,
    Curtains1High = 0x50,
    BreakableBlock = 0x60,
    HorizontalPit = 0x70,
    SingleWeed = 0x80,
    DoubleWeed = 0x90,
    NorthCastleSteps = 0xA0,
    NorthCastleBricks = 0xB0,
    VolcanoBackground = 0xC0,
    BreakableBlockVertical = 0xD0,
    TreeTrunk = 0xE0,
    Pillar = 0xF0
}

/// <summary>
/// IDs for overworld cave sideviews. Overworld sideviews with ObjectSet bit set to 1 in the header, to be exact.
/// </summary>
public enum CaveObject
{
    Headstone = 0x00,
    Cross = 0x01,
    AngledCross = 0x02,
    TreeStump = 0x03,
    Stonehenge = 0x04,
    LockedDoor = 0x05,
    SleepingZelda1 = 0x06,
    SleepingZelda2 = 0x07,
    VerticalPit = 0x08,
    LargeCloud = 0x09,
    SmallCloud0A = 0x0A,
    SmallCloud0B = 0x0B,
    SmallCloud0C = 0x0C,
    SmallCloud0D = 0x0D,
    SmallCloud0E = 0x0E,
    RockFloor = 0x20,
    RockCeiling = 0x30,
    Bridge = 0x40,
    CavePlatform = 0x50,
    BreakableBlock = 0x60,
    CrumbleBridge = 0x70,
    SingleWeed = 0x80,
    DoubleWeed = 0x90,
    HorizontalPit = 0xA0,
    NorthCastleBricks = 0xB0,
    VolcanoBackground = 0xC0,
    BreakableBlockVertical = 0xD0,
    RockFloorVertical = 0xE0,
    StoneSpire = 0xF0
}

/// <summary>
/// IDs that are shared in all palaces.
/// 
/// It's a class of constants and not an enum, so that
/// PalaceObjectShared.Window == PalaceObject.Window is true.
/// </summary>
public static class PalaceObjectShared
{
    public const int Window = 0x00;
    public const int DragonHead = 0x01;
    public const int WolfHead = 0x02;
    public const int CrystalReturnStatue1 = 0x03;
    public const int CrystalReturnStatue2 = 0x04;
    public const int LockedDoor = 0x05;
    public const int LargeCloud = 0x07;
    public const int SmallCloud08 = 0x08;
    public const int SmallCloud0B = 0x0B;
    public const int SmallCloud0C = 0x0C;
    public const int SmallCloud0D = 0x0D;
    public const int SmallCloud0E = 0x0E;
    public const int Collectable = 0x0F;
    public const int HorizontalBrick = 0x20;
    public const int BreakableBlock1 = 0x30;
    public const int SteelBrick = 0x40;
}

/// <summary>
/// IDs for palace 1-6
/// </summary>
public enum PalaceObject
{
    Window = 0x00,
    DragonHead = 0x01,
    WolfHead = 0x02,
    CrystalReturnStatue1 = 0x03,
    CrystalReturnStatue2 = 0x04,
    LockedDoor = 0x05,
    LargeCloud = 0x07,
    SmallCloud08 = 0x08,
    IronknuckleStatue = 0x09,
    SmallCloud0A = 0x0A,
    SmallCloud0B = 0x0B,
    SmallCloud0C = 0x0C,
    SmallCloud0D = 0x0D,
    SmallCloud0E = 0x0E,
    Collectable = 0x0F,
    HorizontalPitOrLava = 0x10,
    HorizontalBrick = 0x20,
    BreakableBlock1 = 0x30,
    SteelBrick = 0x40,
    CrumbleBridgeOrElevator = 0x50,
    Bridge = 0x60,
    PalaceBricks = 0x70,
    Curtains = 0x80,
    BreakableBlock2 = 0x90,
    WalkThruBricks = 0xA0,
    BreakableBlockVertical = 0xB0,
    Pillar = 0xC0,
    VerticalPit1 = 0xD0,
    VerticalPit2 = 0xE0,
    HorizontalPit = 0xF0,
}

/// <summary>
/// IDs for Great Palace
/// </summary>
public enum GreatPalaceObject
{
    Window = 0x00,
    DragonHead = 0x01,
    WolfHead = 0x02,
    CrystalReturnStatue1 = 0x03,
    CrystalReturnStatue2 = 0x04,
    LockedDoor = 0x05,
    ElevatorShaft = 0x06,
    LargeCloud = 0x07,
    SmallCloud08 = 0x08,
    SleepingZelda = 0x09,
    BirdKnight = 0x0A,
    SmallCloud0B = 0x0B,
    SmallCloud0C = 0x0C,
    SmallCloud0D = 0x0D,
    SmallCloud0E = 0x0E,
    Collectable = 0x0F,
    FinalBossCanopyOrLava = 0x10,
    HorizontalBrick = 0x20,
    BreakableBlock1 = 0x30,
    SteelBrick = 0x40,
    NorthCastleBricksOrElevator = 0x50,
    NorthCastleSteps = 0x60,
    CrumbleBridge = 0x70,
    Bridge = 0x80,
    Bricks = 0x90,
    Curtains = 0xA0,
    WalkThruBricks = 0xB0,
    BreakableBlock2 = 0xC0,
    BreakableBlockVertical = 0xD0,
    ElectricBarrier = 0xE0,
    Pillar = 0xF0
}

public static class ForestObjectExtensions
{
    public static int Width(SideviewMapCommand<ForestObject> command)
    {
        switch (command.Id)
        {
            case ForestObject.ForestCeiling:
            case ForestObject.ForestCeiling2:
            case ForestObject.Curtains2High:
            case ForestObject.Curtains1High:
            case ForestObject.BreakableBlock:
            case ForestObject.HorizontalPit:
            case ForestObject.SingleWeed:
            case ForestObject.DoubleWeed:
            case ForestObject.NorthCastleSteps:
            case ForestObject.NorthCastleBricks:
            case ForestObject.VolcanoBackground:
                return 1 + command.Param;
            case ForestObject.SleepingZelda1:
            case ForestObject.SleepingZelda2:
            case ForestObject.LargeCloud:
            case ForestObject.SmallCloud0A:
            case ForestObject.SmallCloud0B:
            case ForestObject.SmallCloud0C:
            case ForestObject.SmallCloud0D:
            case ForestObject.SmallCloud0E:
                return 2;
            case ForestObject.Stonehenge:
                return 4;
            default:
                return 1;
        }
    }

    public static int Height(SideviewMapCommand<ForestObject> command)
    {
        switch (command.Id)
        {
            case ForestObject.BreakableBlockVertical:
            case ForestObject.TreeTrunk:
            case ForestObject.Pillar:
                return 1 + command.Param;
            case ForestObject.VerticalPit:
                return 13 - command.Y;
            case ForestObject.TreeStump:
            case ForestObject.ForestCeiling:
            case ForestObject.ForestCeiling2:
            case ForestObject.Curtains2High:
                return 2;
            case ForestObject.Stonehenge:
            case ForestObject.LockedDoor:
                return 3;
            default:
                return 1;
        }
    }

    public static bool IsSolid(SideviewMapCommand<ForestObject> command)
    {
        switch (command.Id)
        {
            case ForestObject.TreeStump:
            case ForestObject.Stonehenge:
            case ForestObject.ForestCeiling:
            case ForestObject.ForestCeiling2:
            case ForestObject.BreakableBlock:
            case ForestObject.BreakableBlockVertical:
                return true;
            default:
                return false;
        }
    }

    public static bool IsBreakable(SideviewMapCommand<ForestObject> command)
    {
        switch (command.Id)
        {
            case ForestObject.BreakableBlock:
            case ForestObject.BreakableBlockVertical:
                return true;
            default:
                return false;
        }
    }

    public static bool IsPit(SideviewMapCommand<ForestObject> command)
    {
        switch (command.Id)
        {
            case ForestObject.HorizontalPit:
            case ForestObject.VerticalPit:
                return true;
            default:
                return false;
        }
    }
}

public static class CaveObjectExtensions
{
    public static int Width(SideviewMapCommand<CaveObject> command)
    {
        switch (command.Id)
        {
            case CaveObject.RockFloor:
            case CaveObject.RockCeiling:
            case CaveObject.Bridge:
            case CaveObject.CavePlatform:
            case CaveObject.BreakableBlock:
            case CaveObject.CrumbleBridge:
            case CaveObject.SingleWeed:
            case CaveObject.DoubleWeed:
            case CaveObject.HorizontalPit:
            case CaveObject.NorthCastleBricks:
            case CaveObject.VolcanoBackground:
                return 1 + command.Param;
            case CaveObject.SleepingZelda1:
            case CaveObject.SleepingZelda2:
            case CaveObject.LargeCloud:
            case CaveObject.SmallCloud0A:
            case CaveObject.SmallCloud0B:
            case CaveObject.SmallCloud0C:
            case CaveObject.SmallCloud0D:
            case CaveObject.SmallCloud0E:
                return 2;
            case CaveObject.Stonehenge:
                return 4;
            default:
                return 1;
        }
    }

    public static int Height(SideviewMapCommand<CaveObject> command)
    {
        switch (command.Id)
        {
            case CaveObject.BreakableBlockVertical:
            case CaveObject.RockFloorVertical:
            case CaveObject.StoneSpire:
                return 1 + command.Param;
            case CaveObject.VerticalPit:
                return 13 - command.Y;
            case CaveObject.TreeStump:
            case CaveObject.RockFloor:
            case CaveObject.RockCeiling:
            case CaveObject.Bridge:
                return 2;
            case CaveObject.Stonehenge:
            case CaveObject.LockedDoor:
                return 3;
            default:
                return 1;
        }
    }

    public static bool IsSolid(SideviewMapCommand<CaveObject> command)
    {
        switch (command.Id)
        {
            case CaveObject.TreeStump:
            case CaveObject.Stonehenge:
            case CaveObject.RockFloor:
            case CaveObject.RockCeiling:
            case CaveObject.Bridge: // actually top half of the bridge is not solid :D
            case CaveObject.CavePlatform:
            case CaveObject.CrumbleBridge:
            case CaveObject.BreakableBlock:
            case CaveObject.BreakableBlockVertical:
            case CaveObject.RockFloorVertical:
            case CaveObject.StoneSpire:
                return true;
            default:
                return false;
        }
    }

    public static bool IsBreakable(SideviewMapCommand<CaveObject> command)
    {
        switch (command.Id)
        {
            case CaveObject.BreakableBlock:
            case CaveObject.BreakableBlockVertical:
                return true;
            default:
                return false;
        }
    }

    public static bool IsPit(SideviewMapCommand<CaveObject> command)
    {
        switch (command.Id)
        {
            case CaveObject.HorizontalPit:
            case CaveObject.VerticalPit:
                return true;
            default:
                return false;
        }
    }
}

public static class PalaceObjectExtensions
{
    public static int Width(SideviewMapCommand<PalaceObject> command)
    {
        switch (command.Id)
        {
            case PalaceObject.HorizontalPitOrLava:
            case PalaceObject.HorizontalBrick:
            case PalaceObject.BreakableBlock1:
            case PalaceObject.SteelBrick:
            case PalaceObject.CrumbleBridgeOrElevator:
            case PalaceObject.Bridge:
            case PalaceObject.PalaceBricks:
            case PalaceObject.Curtains:
            case PalaceObject.BreakableBlock2:
            case PalaceObject.WalkThruBricks:
            case PalaceObject.HorizontalPit:
                return 1 + command.Param;
            case PalaceObject.LargeCloud:
            case PalaceObject.SmallCloud08:
            case PalaceObject.SmallCloud0A:
            case PalaceObject.SmallCloud0B:
            case PalaceObject.SmallCloud0C:
            case PalaceObject.SmallCloud0D:
            case PalaceObject.SmallCloud0E:
                return 2;
            case PalaceObject.CrystalReturnStatue1:
            case PalaceObject.CrystalReturnStatue2:
                return 4;
            default:
                return 1;
        }
    }

    public static int Height(SideviewMapCommand<PalaceObject> command)
    {
        switch (command.Id)
        {
            case PalaceObject.BreakableBlockVertical:
            case PalaceObject.Pillar:
            case PalaceObject.VerticalPit1:
            case PalaceObject.VerticalPit2:
                return 1 + command.Param;
            case PalaceObject.HorizontalPit:
                return 13 - command.Y;
            case PalaceObject.Window:
            case PalaceObject.IronknuckleStatue:
            case PalaceObject.Bridge:
            case PalaceObject.PalaceBricks:
            case PalaceObject.Curtains:
            case PalaceObject.BreakableBlock2:
            case PalaceObject.WalkThruBricks:
                return 2;
            case PalaceObject.LockedDoor:
                return 3;
            case PalaceObject.CrystalReturnStatue1:
            case PalaceObject.CrystalReturnStatue2:
                return 5;
            default:
                return 1;
        }
    }

    public static bool IsSolid(SideviewMapCommand<PalaceObject> command)
    {
        switch (command.Id)
        {
            case PalaceObject.Window:
            case PalaceObject.DragonHead:
            case PalaceObject.WolfHead:
            case PalaceObject.CrystalReturnStatue1:
            case PalaceObject.CrystalReturnStatue2:
            case PalaceObject.LockedDoor:
            case PalaceObject.LargeCloud:
            case PalaceObject.SmallCloud08:
            case PalaceObject.IronknuckleStatue:
            case PalaceObject.SmallCloud0A:
            case PalaceObject.SmallCloud0B:
            case PalaceObject.SmallCloud0C:
            case PalaceObject.SmallCloud0D:
            case PalaceObject.SmallCloud0E:
            case PalaceObject.Collectable:
            case PalaceObject.HorizontalPitOrLava:
            case PalaceObject.Curtains:
            case PalaceObject.WalkThruBricks:
            case PalaceObject.Pillar:
            case PalaceObject.VerticalPit1:
            case PalaceObject.VerticalPit2:
            case PalaceObject.HorizontalPit:
                return false;
            case PalaceObject.HorizontalBrick:
            case PalaceObject.BreakableBlock1:
            case PalaceObject.SteelBrick:
            case PalaceObject.CrumbleBridgeOrElevator:
            case PalaceObject.Bridge: // actually top half of the bridge is not solid :D
            case PalaceObject.PalaceBricks:
            case PalaceObject.BreakableBlock2:
            case PalaceObject.BreakableBlockVertical:
                return true;
            default:
                throw new NotImplementedException();
        }
    }

    public static bool IsBreakable(SideviewMapCommand<PalaceObject> command)
    {
        switch (command.Id)
        {
            case PalaceObject.BreakableBlock1:
            case PalaceObject.BreakableBlock2:
            case PalaceObject.BreakableBlockVertical:
                return true;
            default:
                return false;
        }
    }

    public static bool IsPit(SideviewMapCommand<PalaceObject> command)
    {
        switch (command.Id)
        {
            case PalaceObject.HorizontalPitOrLava:
            case PalaceObject.WalkThruBricks:
            case PalaceObject.VerticalPit1:
            case PalaceObject.VerticalPit2:
            case PalaceObject.HorizontalPit:
                return true;
            default:
                return false;
        }
    }
}

public static class GreatPalaceObjectExtensions
{
    public static int Width(SideviewMapCommand<GreatPalaceObject> command)
    {
        switch (command.Id)
        {
            case GreatPalaceObject.FinalBossCanopyOrLava:
            case GreatPalaceObject.HorizontalBrick:
            case GreatPalaceObject.BreakableBlock1:
            case GreatPalaceObject.SteelBrick:
            case GreatPalaceObject.NorthCastleBricksOrElevator:
            case GreatPalaceObject.NorthCastleSteps:
            case GreatPalaceObject.CrumbleBridge:
            case GreatPalaceObject.Bridge:
            case GreatPalaceObject.Bricks:
            case GreatPalaceObject.Curtains:
            case GreatPalaceObject.WalkThruBricks:
            case GreatPalaceObject.BreakableBlock2:
                return 1 + command.Param;
            case GreatPalaceObject.LargeCloud:
            case GreatPalaceObject.SmallCloud08:
            case GreatPalaceObject.SmallCloud0B:
            case GreatPalaceObject.SmallCloud0C:
            case GreatPalaceObject.SmallCloud0D:
            case GreatPalaceObject.SmallCloud0E:
            case GreatPalaceObject.SleepingZelda:
                return 2;
            case GreatPalaceObject.CrystalReturnStatue1:
            case GreatPalaceObject.CrystalReturnStatue2:
                return 4;
            default:
                return 1;
        }
    }

    public static int Height(SideviewMapCommand<GreatPalaceObject> command)
    {
        switch (command.Id)
        {
            case GreatPalaceObject.BreakableBlockVertical:
            case GreatPalaceObject.ElectricBarrier:
            case GreatPalaceObject.Pillar:
                return 1 + command.Param;
            case GreatPalaceObject.ElevatorShaft:
                return 13 - command.Y;
            case GreatPalaceObject.Window:
            case GreatPalaceObject.BirdKnight:
            case GreatPalaceObject.Bridge:
            case GreatPalaceObject.Bricks:
            case GreatPalaceObject.Curtains:
            case GreatPalaceObject.WalkThruBricks:
            case GreatPalaceObject.BreakableBlock2:
                return 2;
            case GreatPalaceObject.LockedDoor:
                return 3;
            case GreatPalaceObject.CrystalReturnStatue1:
            case GreatPalaceObject.CrystalReturnStatue2:
                return 5;
            default:
                return 1;
        }
    }

    public static bool IsSolid(SideviewMapCommand<GreatPalaceObject> command)
    {
        switch (command.Id)
        {
            case GreatPalaceObject.Window:
            case GreatPalaceObject.DragonHead:
            case GreatPalaceObject.WolfHead:
            case GreatPalaceObject.CrystalReturnStatue1:
            case GreatPalaceObject.CrystalReturnStatue2:
            case GreatPalaceObject.LockedDoor:
            case GreatPalaceObject.ElevatorShaft:
            case GreatPalaceObject.LargeCloud:
            case GreatPalaceObject.SmallCloud08:
            case GreatPalaceObject.SleepingZelda:
            case GreatPalaceObject.BirdKnight:
            case GreatPalaceObject.SmallCloud0B:
            case GreatPalaceObject.SmallCloud0C:
            case GreatPalaceObject.SmallCloud0D:
            case GreatPalaceObject.SmallCloud0E:
            case GreatPalaceObject.Collectable:
            case GreatPalaceObject.FinalBossCanopyOrLava:
            case GreatPalaceObject.Curtains:
            case GreatPalaceObject.WalkThruBricks:
            case GreatPalaceObject.ElectricBarrier:
            case GreatPalaceObject.Pillar:
                return false;
            case GreatPalaceObject.HorizontalBrick:
            case GreatPalaceObject.BreakableBlock1:
            case GreatPalaceObject.SteelBrick:
            case GreatPalaceObject.NorthCastleBricksOrElevator:
            case GreatPalaceObject.NorthCastleSteps:
            case GreatPalaceObject.CrumbleBridge:
            case GreatPalaceObject.Bridge: // actually top half of the bridge is not solid :D
            case GreatPalaceObject.Bricks:
            case GreatPalaceObject.BreakableBlock2:
            case GreatPalaceObject.BreakableBlockVertical:
                return true;
            default:
                throw new NotImplementedException();
        }
    }

    public static bool IsBreakable(SideviewMapCommand<GreatPalaceObject> command)
    {
        switch (command.Id)
        {
            case GreatPalaceObject.BreakableBlock1:
            case GreatPalaceObject.BreakableBlock2:
            case GreatPalaceObject.BreakableBlockVertical:
                return true;
            default:
                return false;
        }
    }

    public static bool IsPit(SideviewMapCommand<GreatPalaceObject> command)
    {
        switch (command.Id)
        {
            case GreatPalaceObject.ElevatorShaft:
            case GreatPalaceObject.WalkThruBricks:
                return true;
            default:
                return false;
        }
    }
}
