using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using RandomizerCore;
using RandomizerCore.Sidescroll;
using SkiaSharp;

namespace PrintRooms;

public class Program
{
    private static Dictionary<int, SKBitmap> palaceTiles = new();
    private static ROM? rom;
    private static byte[]? paletteClouds;
    private static byte[]? paletteBricks;
    private static byte[]? paletteWindows;
    private static byte[]? paletteCurtains;
    private static byte[]? paletteLink;
    private static byte[]? paletteOrange;
    private static byte[]? paletteRed;
    private static byte[]? paletteBlue;
    private static byte[]? paletteGpBricks;

    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Pass a vanilla ROM file as argument so we can read tile data.");
            return 0;
        }
        string romPath = args[0];
        if (!File.Exists(romPath))
        {
            Console.WriteLine($"The specified ROM file does not exist: {romPath}");
            return 0;
        }
        var vanillaRomData = File.ReadAllBytes(romPath);
        rom = new ROM(vanillaRomData, true);
        rom.Put(ROM.ChrRomOffset + 0x1a000, Util.ReadBinaryResource("RandomizerCore.Asm.Graphics.item_sprites.chr"));

        paletteLink = rom.GetBytes(ROM.RomHdrSize + 0x2849, 4);
        paletteClouds = rom.GetBytes(ROM.RomHdrSize + 0x1001e, 4);
        paletteBricks = rom.GetBytes(ROM.RomHdrSize + 0x1001e + 4, 4);
        paletteWindows = rom.GetBytes(ROM.RomHdrSize + 0x1001e + 8, 4);
        paletteCurtains = rom.GetBytes(ROM.RomHdrSize + 0x1001e + 12, 4);
        paletteCurtains[0] = 0;
        paletteOrange = rom.GetBytes(ROM.RomHdrSize + 0x100a2, 4);
        paletteOrange[0] = 0;
        paletteRed = rom.GetBytes(ROM.RomHdrSize + 0x10056, 4);
        paletteBlue = rom.GetBytes(ROM.RomHdrSize + 0x100aa, 4);
        paletteBlue[0] = 0;
        paletteGpBricks = rom.GetBytes(ROM.RomHdrSize + 0x10062, 4);

        PrintRoomsForFileAndGroup("PalaceRooms.json", PrintRooms.IncludeGroups);
        PrintRequirements();

        return 0;
    }

    public static void PrintRoomsForFileAndGroup(string jsonFilename, RoomGroup[]? roomGroup)
    {
        Console.WriteLine("Scanning \"" + jsonFilename + "\"...");
        var json = RandomizerCore.Util.ReadAllTextFromFile(jsonFilename);
        var palaceRooms = JsonSerializer.Deserialize(json, RoomSerializationContext.Default.ListRoom);
        var html = new RoomsHtml();

        foreach (Room room in palaceRooms!.Where(r => r.Enabled))
        {
            if (roomGroup != null && !roomGroup.Contains(room.Group)) { continue; }
            if (PrintRoom(room))
            {
                Room? linkedRoom = room.LinkedRoomName is { } name ? palaceRooms!.Find(r => r.Name == name) : null;
                html.Add(room, linkedRoom, PrintRooms.GetFilename(room));
            }
        }

        File.WriteAllText("index.html", html.Finalize());
        Console.WriteLine($"index.html generated!");
    }

    public static bool PrintRoom(Room room)
    {
        if (room.PalaceNumber == 7)
        {
            var sv = new SideviewEditable<GreatPalaceObject>(room.SideView);
            var ee = new EnemiesEditable<EnemiesGreatPalace>(room.Enemies);
            return PrintRoom(room, sv, ee);
        }
        else if (room.PalaceGroup == PalaceGrouping.Palace346)
        {
            var sv = new SideviewEditable<PalaceObject>(room.SideView);
            var ee = new EnemiesEditable<EnemiesPalace346>(room.Enemies);
            return PrintRoom(room, sv, ee);
        }
        else
        {
            var sv = new SideviewEditable<PalaceObject>(room.SideView);
            var ee = new EnemiesEditable<EnemiesPalace125>(room.Enemies);
            return PrintRoom(room, sv, ee);
        }
    }

    public static bool PrintRoom<T,U>(Room room, SideviewEditable<T> sv, EnemiesEditable<U> ee) where T : Enum where U : Enum
    {
        if (sv.BackgroundMap != 0) { return false; /* Not supporting built-in "background" maps */ }

        int width = sv.PageCount * 16 * 16;
        const int height = 13 * 16;
        using SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(0, 0, 0));
        DrawFloors(canvas, sv);
        foreach (var cmd in sv.Commands)
        {
            if (cmd.IsXSkip() || cmd.IsNewFloor() || cmd.IsElevator()) { continue; }
            DrawCommand(canvas, cmd);
        }
        // 2nd phase - draw objects that must appear on top
        foreach (var cmd in sv.Commands)
        {
            if (cmd.IsElevator()) {
                DrawElevator(canvas, cmd.AbsX, 0x98 - cmd.Param * 8);
            }
        }
        foreach (var e in ee.Enemies)
        {
            DrawEnemy(canvas, e);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var filePath = PrintRooms.GetFilename(room);
        var fileInfo = new FileInfo(filePath);
        fileInfo.Directory!.Create();
        File.WriteAllBytes(fileInfo.FullName, data.ToArray());
        image.Dispose();
        canvas.Dispose();
        bitmap.Dispose();
        Console.WriteLine($"Room image generated: {filePath}");
        return true;
    }

    static void DrawFloors<T>(SKCanvas canvas, SideviewEditable<T> sv) where T : Enum
    {
        var floor = SideviewMapCommand<T>.CreateNewFloor(0, sv.FloorHeader);
        List<SideviewMapCommand<T>> floors = sv.FindAll(o => o.IsNewFloor());
        var tile = LoadPalaceBrickTile<T>();
        for (var x = 0; x < 64; x++)
        {
            while (floors.Count > 0 && floors[0].AbsX == x)
            {
                floor = floors[0];
                floors.RemoveAt(0);
            }
            for (var y = 0; y < 13; y++)
            {
                if (floor.IsFloorSolidAt(y))
                {
                    canvas.DrawBitmap(tile, x * 16, y * 16);
                }
            }
        }
    }

    static void DrawCommand<T>(SKCanvas canvas, SideviewMapCommand<T> cmd) where T : Enum
    {
        int startX = cmd.AbsX;
        int startY = cmd.Y;
        int endX = cmd.AbsX + cmd.Width;
        int endY = cmd.Y + cmd.Height;
        SKBitmap? tile = null;
        if (cmd.IsLava())
        {
            // lava goes 1 tile higher in GP
            startY = typeof(T) == typeof(GreatPalaceObject) ? 10 : 11;
            tile = LoadChrFillPattern(palaceTiles, 0x9980, 1, 2, 2, 2, paletteWindows!, false);
            for (int i = startX; i < endX; i++)
            {
                canvas.DrawBitmap(tile, i * 16, startY * 16);
            }
            startY++;
            tile = LoadChrFillPattern(palaceTiles, 0x9fe0, 1, 1, 2, 2, paletteWindows!);
        }
        else if (cmd.IsElevator())
        {
            return;
        }
        else
        {
            switch (cmd.Id)
            {
                case PalaceObject.Window:
                case GreatPalaceObject.Window:
                    tile = LoadChr(palaceTiles, 0x96a0, 2, 1, paletteWindows!, false);
                    canvas.DrawBitmap(tile, startX * 16, startY * 16 + 0 * 8);
                    tile = LoadChr(palaceTiles, 0x96c0, 2, 1, paletteWindows!, false);
                    canvas.DrawBitmap(tile, startX * 16, startY * 16 + 1 * 8);
                    canvas.DrawBitmap(tile, startX * 16, startY * 16 + 2 * 8);
                    tile = LoadChr(palaceTiles, 0x96e0, 2, 1, paletteWindows!, false);
                    canvas.DrawBitmap(tile, startX * 16, startY * 16 + 3 * 8);
                    return;
                case PalaceObject.DragonHead:
                    tile = LoadChr(palaceTiles, 0x9340, 2, 2, paletteBricks!, false);
                    break;
                case GreatPalaceObject.DragonHead:
                    tile = LoadChr(palaceTiles, 0xd340, 2, 2, paletteGpBricks!, false);
                    break;
                case PalaceObject.WolfHead:
                    tile = LoadChr(palaceTiles, 0x9380, 2, 2, paletteBricks!, false);
                    break;
                case GreatPalaceObject.WolfHead:
                    tile = LoadChr(palaceTiles, 0xd380, 2, 2, paletteGpBricks!, false);
                    break;
                case PalaceObject.CrystalReturnStatue1:
                case PalaceObject.CrystalReturnStatue2:
                case GreatPalaceObject.CrystalReturnStatue1:
                case GreatPalaceObject.CrystalReturnStatue2:
                    tile = LoadChr(palaceTiles, 0x9700, 2, 1, paletteCurtains!);
                    canvas.DrawBitmap(tile, startX * 16 + 3 * 8, startY * 16 + 1 * 8);
                    tile = LoadChr(palaceTiles, 0x9720, 4, 1, paletteCurtains!);
                    canvas.DrawBitmap(tile, startX * 16 + 2 * 8, startY * 16 + 2 * 8);
                    tile = LoadChr(palaceTiles, 0x9760, 1, 1, paletteCurtains!);
                    canvas.DrawBitmap(tile, startX * 16 + 2 * 8, startY * 16 + 5 * 8);
                    tile = LoadChr(palaceTiles, 0x9770, 1, 1, paletteCurtains!);
                    RepeatDrawTile(canvas, tile, startX, startY,
                        [3, 3, 3, 3, 3, 3, 4, 4],
                        [3, 4, 5, 6, 8, 9, 8, 9]);
                    tile = LoadChr(palaceTiles, 0x9780, 1, 1, paletteCurtains!);
                    canvas.DrawBitmap(tile, startX * 16 + 4 * 8, startY * 16 + 3 * 8);
                    tile = LoadChr(palaceTiles, 0x9790, 1, 1, paletteCurtains!);
                    RepeatDrawTile(canvas, tile, startX, startY,
                        [4, 4, 4],
                        [4, 5, 6]);
                    tile = LoadChr(palaceTiles, 0x97a0, 2, 1, paletteCurtains!);
                    canvas.DrawBitmap(tile, startX * 16 + 3 * 8, startY * 16 + 7 * 8);
                    tile = LoadChr(palaceTiles, 0x9880, 1, 1, paletteCurtains!);
                    RepeatDrawTile(canvas, tile, startX, startY,
                        [2, 0, 6],
                        [0, 6, 6]);
                    tile = LoadChr(palaceTiles, 0x98a0, 1, 1, paletteCurtains!);
                    RepeatDrawTile(canvas, tile, startX, startY,
                        [3, 4],
                        [0, 0]);
                    tile = LoadChr(palaceTiles, 0x98c0, 1, 1, paletteCurtains!);
                    RepeatDrawTile(canvas, tile, startX, startY,
                        [5, 1, 7],
                        [0, 6, 6]);
                    tile = LoadChr(palaceTiles, 0x9a80, 1, 1, paletteCurtains!);
                    RepeatDrawTile(canvas, tile, startX, startY,
                        [0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 6, 6, 6],
                        [7, 8, 9, 1, 3, 4, 6, 7, 8, 9, 7, 8, 9]);
                    tile = LoadChr(palaceTiles, 0x9aa0, 1, 1, paletteCurtains!);
                    RepeatDrawTile(canvas, tile, startX, startY,
                        [1, 1, 1, 5, 5, 5, 5, 5, 5, 5, 5, 7, 7, 7],
                        [7, 8, 9, 1, 3, 4, 5, 6, 7, 8, 9, 7, 8, 9]);
                    return;
                case PalaceObject.LockedDoor:
                case GreatPalaceObject.LockedDoor:
                    DrawLockedDoor(canvas, startX, startY);
                    return;
                case PalaceObject.LargeCloud:
                case GreatPalaceObject.LargeCloud:
                    tile = LoadChr(palaceTiles, 0x9b30, 1, 2, paletteClouds!, false);
                    canvas.DrawBitmap(tile, startX * 16 + 0 * 8, startY * 16);
                    tile = LoadChr(palaceTiles, 0x9b50, 1, 2, paletteClouds!, false);
                    canvas.DrawBitmap(tile, startX * 16 + 1 * 8, startY * 16);
                    canvas.DrawBitmap(tile, startX * 16 + 2 * 8, startY * 16);
                    tile = LoadChr(palaceTiles, 0x9b70, 1, 2, paletteClouds!, false);
                    canvas.DrawBitmap(tile, startX * 16 + 3 * 8, startY * 16);
                    return;
                case PalaceObject.SmallCloud08:
                case PalaceObject.SmallCloud0A:
                case PalaceObject.SmallCloud0B:
                case PalaceObject.SmallCloud0C:
                case PalaceObject.SmallCloud0D:
                case PalaceObject.SmallCloud0E:
                case GreatPalaceObject.SmallCloud08:
                case GreatPalaceObject.SmallCloud0B:
                case GreatPalaceObject.SmallCloud0C:
                case GreatPalaceObject.SmallCloud0D:
                case GreatPalaceObject.SmallCloud0E:
                    tile = LoadChr(palaceTiles, 0x9b30, 1, 2, paletteClouds!, false);
                    canvas.DrawBitmap(tile, startX * 16 + 0 * 8, startY * 16);
                    tile = LoadChr(palaceTiles, 0x9b50, 1, 2, paletteClouds!, false);
                    canvas.DrawBitmap(tile, startX * 16 + 1 * 8, startY * 16);
                    tile = LoadChr(palaceTiles, 0x9b70, 1, 2, paletteClouds!, false);
                    canvas.DrawBitmap(tile, startX * 16 + 2 * 8, startY * 16);
                    return;
                case PalaceObject.IronknuckleStatue:
                    tile = LoadChr(palaceTiles, 0x9400, 2, 2, paletteBricks!, false);
                    canvas.DrawBitmap(tile, startX * 16, startY * 16);
                    tile = LoadChr(palaceTiles, 0x9480, 2, 2, paletteBricks!, false);
                    canvas.DrawBitmap(tile, startX * 16, (startY + 1) * 16);
                    return;
                case GreatPalaceObject.BirdKnight:
                    tile = LoadChr(palaceTiles, 0xd400, 2, 2, paletteGpBricks!, false);
                    canvas.DrawBitmap(tile, startX * 16, startY * 16);
                    tile = LoadChr(palaceTiles, 0xd480, 2, 2, paletteGpBricks!, false);
                    canvas.DrawBitmap(tile, startX * 16, (startY + 1) * 16);
                    return;
                case PalaceObject.Collectable:
                case GreatPalaceObject.Collectable:
                    switch (cmd.Extra)
                    {
                        case Collectable.KEY:
                            tile = LoadChr(palaceTiles, 0x8660, 1, 2, paletteOrange!);
                            break;
                        case Collectable.SMALL_BAG:
                        case Collectable.MEDIUM_BAG:
                        case Collectable.LARGE_BAG:
                        case Collectable.XL_BAG:
                            tile = LoadChr(palaceTiles, 0x8720, 1, 2, paletteOrange!);
                            break;
                        case Collectable.BLUE_JAR:
                            tile = LoadChr(palaceTiles, 0x88a0, 1, 2, paletteBlue!);
                            break;
                        case Collectable.RED_JAR:
                            DrawRedJar(canvas, startX, startY);
                            return;
                        case Collectable.ONEUP:
                            tile = LoadChr(palaceTiles, 0x8a80, 1, 2, paletteLink!);
                            break;
                        default:
                            Debug.Assert(!cmd.Extra.IsMinorItem());
                            tile = LoadChr(palaceTiles, 0x88e0, 1, 2, paletteOrange!);
                            break;
                    }
                    break;
                case PalaceObject.HorizontalPitOrLava:
                case PalaceObject.VerticalPit1:
                case PalaceObject.VerticalPit2:
                case PalaceObject.HorizontalPit:
                case GreatPalaceObject.ElevatorShaft:
                    tile = LoadChrFillPattern(palaceTiles, 0x9f40, 1, 1, 2, 2, paletteBricks!, false);
                    break;
                case PalaceObject.HorizontalBrick:
                case PalaceObject.PalaceBricks:
                    tile = LoadPalaceBrickTile<PalaceObject>();
                    break;
                case GreatPalaceObject.HorizontalBrick:
                case GreatPalaceObject.Bricks:
                    tile = LoadPalaceBrickTile<GreatPalaceObject>();
                    break;
                case PalaceObject.BreakableBlock1:
                case PalaceObject.BreakableBlock2:
                case PalaceObject.BreakableBlockVertical:
                    tile = LoadChr(palaceTiles, 0x9ba0, 2, 2, paletteBricks!);
                    break;
                case GreatPalaceObject.BreakableBlock1:
                case GreatPalaceObject.BreakableBlock2:
                case GreatPalaceObject.BreakableBlockVertical:
                    tile = LoadChr(palaceTiles, 0xdba0, 2, 2, paletteGpBricks!);
                    break;
                case PalaceObject.SteelBrick:
                case GreatPalaceObject.SteelBrick:
                    tile = LoadChr(palaceTiles, 0x95c0, 2, 2, paletteCurtains!);
                    break;
                case GreatPalaceObject.NorthCastleBricksOrElevator:
                    tile = LoadChrBrickPattern(palaceTiles, 0xd9a0, 1, 1, paletteGpBricks!);
                    break;
                case PalaceObject.CrumbleBridgeOrElevator:
                    tile = LoadChrFillPattern(palaceTiles, 0x9920, 1, 2, 2, 2, paletteBricks!);
                    break;
                case GreatPalaceObject.CrumbleBridge:
                    tile = LoadChrFillPattern(palaceTiles, 0xd920, 1, 2, 2, 2, paletteGpBricks!);
                    break;
                case PalaceObject.Bridge:
                    tile = LoadChrFillPattern(palaceTiles, 0x97c0, 1, 2, 2, 2, paletteBricks!, false);
                    startY++;
                    break;
                case GreatPalaceObject.Bridge:
                    tile = LoadChrFillPattern(palaceTiles, 0xd7c0, 1, 2, 2, 2, paletteGpBricks!, false);
                    startY++;
                    break;
                case PalaceObject.Curtains:
                case GreatPalaceObject.Curtains:
                    tile = LoadChr(palaceTiles, 0x9840, 2, 2, paletteCurtains!, true);
                    for (int i = startX; i < endX; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            canvas.DrawBitmap(tile, i * 16, startY * 16 + j * 8);
                        }
                    }
                    return;
                case PalaceObject.WalkThruBricks:
                    tile = LoadPalaceBrickTile<PalaceObject>();
                    // we can't really use the alpha channel because
                    // walkthrough bricks are likely put on top of regular
                    // bricks, so we just make them darker
                    tile = AdjustTileBrightness(tile, 0.50f);
                    break;
                case GreatPalaceObject.WalkThruBricks:
                    tile = LoadPalaceBrickTile<GreatPalaceObject>();
                    tile = AdjustTileBrightness(tile, 0.50f);
                    break;
                case PalaceObject.Pillar:
                    tile = LoadChr(palaceTiles, 0x98e0, 2, 2, paletteBricks!);
                    canvas.DrawBitmap(tile, startX * 16, startY * 16);
                    startY++;
                    tile = LoadChrFillPattern(palaceTiles, 0x9a50, 2, 1, 2, 2, paletteBricks!);
                    break;
                case GreatPalaceObject.Pillar:
                    tile = LoadChr(palaceTiles, 0xd8e0, 2, 2, paletteGpBricks!);
                    canvas.DrawBitmap(tile, startX * 16, startY * 16);
                    startY++;
                    tile = LoadChrFillPattern(palaceTiles, 0xda50, 2, 1, 2, 2, paletteGpBricks!);
                    break;
                case GreatPalaceObject.FinalBossCanopyOrLava:
                    tile = LoadChrFillPattern(palaceTiles, 0xd780, 1, 2, 2, 2, paletteGpBricks!);
                    break;
                case GreatPalaceObject.SleepingZelda:
                    break;
                case GreatPalaceObject.NorthCastleSteps:
                    break;
                case GreatPalaceObject.ElectricBarrier:
                    return;
            }
        }
        if (tile != null)
        {
            for (int i = startX; i < endX; i++)
            {
                for (int j = startY; j < endY; j++)
                {
                    canvas.DrawBitmap(tile, i * 16, j * 16);
                    if (tile.Height > 16)
                    {
                        j += (int)Math.Ceiling(tile.Height / 16.0) - 1;
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"No tile info for: {cmd.DebugString()}");
        }
    }

    static void DrawEnemy<T>(SKCanvas canvas, Enemy<T> enemy) where T : Enum
    {
        int startX = enemy.X;
        int startY = enemy.Y;
        SKBitmap? tile = null;
        switch (enemy.Id)
        {
            case EnemiesPalace125.FAIRY:
            case EnemiesPalace346.FAIRY:
            case EnemiesGreatPalace.FAIRY:
                tile = DrawFairy(canvas, startX, startY);
                return;
            case EnemiesPalace125.STRIKE_FOR_RED_JAR:
            case EnemiesPalace346.STRIKE_FOR_RED_JAR_OR_IRON_KNUCKLE:
            case EnemiesGreatPalace.STRIKE_FOR_RED_JAR_OR_FOKKA:
                DrawRedJar(canvas, startX, startY);
                return;
            case EnemiesPalace125.DRIPPER:
            case EnemiesPalace346.DRIPPER:
                tile = LoadChr(palaceTiles, 0xb8e0, 2, 2, paletteRed!, false);
                canvas.DrawBitmap(tile, startX * 16, startY * 16);
                return;
            case EnemiesPalace125.BAGO_BAGO: // Probably meant to be a flame
            case EnemiesPalace346.FLAME:
                tile = LoadChr(palaceTiles, 0x8520, 1, 2, paletteRed!);
                canvas.DrawBitmap(tile, startX * 16, startY * 16);
                return;
            case EnemiesPalace125.ELEVATOR:
            case EnemiesPalace346.ELEVATOR:
            case EnemiesGreatPalace.ELEVATOR:
                DrawElevator(canvas, startX, startY * 16 + 40);
                return;
        }
        if (enemy.IsShufflableSmall())
        {
            tile = LoadChr(palaceTiles, 0x8b40, 1, 2, paletteBlue!);
            canvas.DrawBitmap(tile, startX * 16, startY * 16);
            tile = FlipTileHorizontally(tile);
            canvas.DrawBitmap(tile, startX * 16 + 8, startY * 16);
            return;
        }
        if (enemy.IsShufflableLarge())
        {
            if (enemy is Enemy<EnemiesGreatPalace>)
            {
                tile = LoadChr(palaceTiles, 0xce00, 2, 4, paletteRed!);
                canvas.DrawBitmap(tile, startX * 16, startY * 16);
            }
            else
            {
                tile = LoadChr(palaceTiles, 0xb400, 2, 4, paletteOrange!);
                canvas.DrawBitmap(tile, startX * 16, startY * 16);
            }
            return;
        }
        if (enemy.IsShufflableFlying())
        {
            if (enemy is not Enemy<EnemiesGreatPalace> && enemy.IdByte != EnemiesRegularPalaceShared.ORANGE_MOA)
            {
                tile = LoadChr(palaceTiles, 0x9660, 1, 2, paletteOrange!);
                canvas.DrawBitmap(tile, startX * 16, startY * 16);
                tile = FlipTileHorizontally(tile);
                canvas.DrawBitmap(tile, startX * 16 + 8, startY * 16);
                return;
            }
            else
            {
                tile = LoadChr(palaceTiles, 0x8b60, 2, 2, paletteOrange!);
                canvas.DrawBitmap(tile, startX * 16, startY * 16);
            }
            return;
        }
        if (enemy.IsShufflableGenerator())
        {
            tile = LoadChr(palaceTiles, 0x8c00, 2, 2, paletteOrange!);
            canvas.DrawBitmap(tile, startX * 16, startY * 16);
            return;
        }
    }

    private static void DrawElevator(SKCanvas canvas, int startX, int startYPixel)
    {
        SKBitmap? tile = LoadChr(palaceTiles, 0x8ac0, 1, 1, paletteOrange!);
        for (int i = startX * 16 + 4; i < (startX + 2) * 16 - 8; i += 8)
        {
            canvas.DrawBitmap(tile, i, startYPixel - 32);
            canvas.DrawBitmap(tile, i, startYPixel + 8);
        }
    }

    private static SKBitmap DrawFairy(SKCanvas canvas, int startX, int startY)
    {
        SKBitmap tile = LoadChr(palaceTiles, 0x86a0, 1, 2, paletteOrange!);
        canvas.DrawBitmap(tile, startX * 16, startY * 16);
        return tile;
    }

    private static void DrawLockedDoor(SKCanvas canvas, int startX, int startY)
    {
        SKBitmap? tile = LoadChr(palaceTiles, 0x8740, 1, 2, paletteOrange!, false);
        canvas.DrawBitmap(tile, startX * 16 + 0, startY * 16 + 0 * 16);
        canvas.DrawBitmap(tile, startX * 16 + 0, startY * 16 + 2 * 16);
        tile = LoadChr(palaceTiles, 0x8760, 1, 2, paletteOrange!);
        canvas.DrawBitmap(tile, startX * 16 + 0, startY * 16 + 1 * 16);
    }

    private static void DrawRedJar(SKCanvas canvas, int startX, int startY)
    {
        SKBitmap? tile = LoadChr(palaceTiles, 0xa8a0, 1, 2, paletteRed!);
        canvas.DrawBitmap(tile, startX * 16, startY * 16);
    }

    static void PrintRequirement(string name, int addr1, int w1, int h1, int addr2=0, int w2=0, int h2=0)
    {
        int width = (w1 + w2) * 8;
        int height = 16;
        using SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using SKCanvas canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(0, 0, 0, 0));
        SKBitmap tile = LoadChr(palaceTiles, addr1, w1, h1, paletteOrange!);
        canvas.DrawBitmap(tile, 0, 0);
        if (addr2 != 0)
        {
            tile = LoadChr(palaceTiles, addr2, w2, h2, paletteOrange!);
            tile = FlipTileHorizontally(tile);
            canvas.DrawBitmap(tile, w1 * 8, 0);
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var filePath = $"requirements/{name}.png";
        var fileInfo = new FileInfo(filePath);
        fileInfo.Directory!.Create();
        File.WriteAllBytes(fileInfo.FullName, data.ToArray());
        image.Dispose();
        canvas.Dispose();
        bitmap.Dispose();
        Console.WriteLine($"Requirement image generated: {filePath}");
    }

    static void PrintRequirements()
    {
        PrintRequirement("jump", 0x1a120, 1, 2, 0x1a140, 1, 2);
        PrintRequirement("fairy", 0x86a0, 1, 2);
        PrintRequirement("upstab", 0x1a200, 1, 2);
        PrintRequirement("downstab", 0x1a220, 1, 2);
        PrintRequirement("key", 0x8660, 1, 2);
        PrintRequirement("dash", 0x1a060, 1, 2, 0x1a140, 1, 2);
        PrintRequirement("glove", 0x88e0, 1, 2);
    }

    private static void RepeatDrawTile(SKCanvas canvas, SKBitmap tile, int startX, int startY, int[] xOffsets, int[] yOffsets)
    {
        for (int i = 0; i < xOffsets.Length; i++)
        {
            canvas.DrawBitmap(tile, startX * 16 + xOffsets[i] * 8, startY * 16 + yOffsets[i] * 8);
        }
    }

    private static SKBitmap LoadPalaceBrickTile<T>()
    {
        if (typeof(T) == typeof(GreatPalaceObject))
        {
            return LoadChrBrickPattern(palaceTiles, 0xd640, 2, 1, paletteGpBricks!);
        }
        else
        {
            return LoadChrBrickPattern(palaceTiles, 0x9640, 2, 1, paletteBricks!);
        }
    }

    private static SKBitmap AdjustTileBrightness(SKBitmap tile, float v)
    {
        SKBitmap res = new SKBitmap(tile.Width, tile.Height);
        for (int x = 0; x < tile.Width; x++)
        {
            for (int y = 0; y < tile.Height; y++)
            {
                SKColor color = tile.GetPixel(x, y);
                byte r = (byte)(color.Red * v);
                byte g = (byte)(color.Green * v);
                byte b = (byte)(color.Blue * v);
                res.SetPixel(x, y, new SKColor(r, g, b, color.Alpha));
            }
        }
        return res;
    }

    private static SKBitmap FlipTileHorizontally(SKBitmap tile)
    {
        SKBitmap res = new SKBitmap(tile.Width, tile.Height);
        for (int x = 0; x < tile.Width; x++)
        {
            var mirrorX = tile.Width - x - 1;
            for (int y = 0; y < tile.Height; y++)
            {
                res.SetPixel(mirrorX, y, tile.GetPixel(x, y));
            }
        }
        return res;
    }

    private static SKBitmap LoadChr(Dictionary<int, SKBitmap> d, int chrAddr, int w, int h, byte[] palette, bool alpha = true)
    {
        SKBitmap? tile;
        d.TryGetValue(chrAddr, out tile);
        if (tile == null)
        {
            int stride = w * 8 * 4;
            byte[] tileData = rom!.ReadSprite(ROM.ChrRomOffset + chrAddr, w, h, palette);
            if (!alpha)
            {
                for (var i = 3; i < tileData.Length; i += 4)
                {
                    tileData[i] = 0xff;
                }
            }
            d[chrAddr] = tile = MakeSpriteBitmap(tileData, w * 8, h * 8);
        }
        return tile;
    }

    private static SKBitmap LoadChrFillPattern(Dictionary<int, SKBitmap> d, int chrAddr, int w, int h, int fullW, int fullH, byte[] palette, bool alpha=true)
    {
        SKBitmap? tile;
        d.TryGetValue(chrAddr, out tile);
        if (tile == null)
        {
            int stride = w * 8 * 4;
            byte[] originalTileData = rom!.ReadSprite(ROM.ChrRomOffset + chrAddr, w, h, palette);
            int fullStride = fullW * 8 * 4;
            byte[] fullTileData = new byte[fullW * 8 * fullH * 8 * 4];
            for (var j = 0; j < fullH * 8; j++)
            {
                int originalRowStart = (j % (h * 8)) * stride;
                int fullRowStart = j * fullStride;
                byte[] row = originalTileData.Skip(originalRowStart).Take(stride).ToArray();
                for (var i = 0; i < fullW; i += w)
                {
                    Array.Copy(row, 0, fullTileData, fullRowStart + i * stride, stride);
                }
            }
            if (!alpha)
            {
                for (var i = 3; i < fullTileData.Length; i += 4)
                {
                    fullTileData[i] = 0xff;
                }
            }
            d[chrAddr] = tile = MakeSpriteBitmap(fullTileData, fullW * 8, fullH * 8);
        }
        return tile;
    }

    private static SKBitmap LoadChrBrickPattern(Dictionary<int, SKBitmap> d, int chrAddr, int w, int h, byte[] palette)
    {
        SKBitmap? tile;
        d.TryGetValue(chrAddr, out tile);
        if (tile == null)
        {
            int stride = w * 8 * 4;
            byte[] firstRowTileData = rom!.ReadSprite(ROM.ChrRomOffset + chrAddr, w, h, palette);
            byte[] fullTileData = new byte[2 * firstRowTileData.Length];
            Array.Copy(firstRowTileData, 0, fullTileData, 0, firstRowTileData.Length);
            int shiftX = (w * 8 / 2) * 4; // amount to shift 2nd row of bricks (in bytes)
            for (var i = 0; i < 8; i++)
            {
                int rowStart = i * stride;
                byte[] shiftedRow = firstRowTileData.Skip(rowStart + stride - shiftX).Take(shiftX)
                    .Concat(firstRowTileData.Skip(rowStart).Take(stride - shiftX)).ToArray();
                Array.Copy(shiftedRow, 0, fullTileData, firstRowTileData.Length + rowStart, stride);
            }
            d[chrAddr] = tile = MakeSpriteBitmap(fullTileData, w * 8, h * 2 * 8);
        }
        return tile;
    }

    private static SKBitmap MakeSpriteBitmap(byte[] tileData, int w, int h)
    {
        SKBitmap tile = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        GCHandle handle = GCHandle.Alloc(tileData, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            using (var pixmap = new SKPixmap(tile.Info, ptr, tile.Info.RowBytes))
            {
                tile.InstallPixels(pixmap);
            }
            // Copy to avoid problems with original memory being freed
            return tile.Copy();
        }
        finally { handle.Free(); }
    }
}

internal class PrintRooms
{
    internal readonly static RoomGroup[] IncludeGroups = [RoomGroup.VANILLA, RoomGroup.STUBS, RoomGroup.V4_0, RoomGroup.V4_4];

    internal static string GetName(Room room)
    {
        return room.Name.Length > 0 ? room.Name : Convert.ToHexString(room.SideView);
    }

    internal static string GetFilename(Room room)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        var safe = new string(GetName(room).Where(c => !invalidChars.Contains(c)).ToArray());
        safe = Regex.Replace(safe, "[']", "");
        safe = Regex.Replace(safe, "[ ,]", "_");
        safe = Regex.Replace(safe, "_+", "_");
        safe = safe.Trim(['.', '_']);
        return $"rooms/{safe}.png";
    }
}
