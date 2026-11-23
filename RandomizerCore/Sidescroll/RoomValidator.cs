using System;
using System.Collections.Generic;
using System.Linq;
using Z2Randomizer.RandomizerCore.Enemy;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.RandomizerCore.Sidescroll;

public enum RoomDiagnosticSeverity { Warning, Error }

/// <summary>A room to validate and its source location. Room numbers within arrays start at one.</summary>
public sealed record RoomValidationInput(Room Room, string Source, int? RoomIndex = null);

public sealed record RoomDiagnostic(
    string Id, RoomDiagnosticSeverity Severity, string Source, int? RoomIndex,
    string? RoomName, bool IsMirrored, string Message)
{
    public override string ToString()
    {
        string location = RoomIndex is int index ? $"{Source}, room {index}" : Source;
        string name = string.IsNullOrWhiteSpace(RoomName) ? "" : $" \"{RoomName}\"";
        string variant = IsMirrored ? " (mirrored)" : "";
        return $"{Severity} [{Id}] {location}{name}{variant}: {Message}";
    }
}

public static class RoomValidator
{
    /// <summary>
    /// Validate the supplied rooms and return diagnostics in input order within each check pass.
    /// Additional catalogue rooms provide name, link, and duplicate-layout context only.
    /// Pass any linked companions that also need diagnostics as validation inputs.
    /// </summary>
    public static IReadOnlyList<RoomDiagnostic> Validate(
        IEnumerable<RoomValidationInput> rooms, IEnumerable<Room>? catalogueContext = null)
    {
        return new ValidationRun(rooms.ToArray(), catalogueContext?.ToArray() ?? []).Validate();
    }

    private sealed class ValidationRun(RoomValidationInput[] inputs, Room[] catalogueContext)
    {
        private const int DROP_MIN_X = 17; // inclusive
        private const int DROP_MAX_X = 45; // inclusive
        private readonly List<RoomDiagnostic> diagnostics = [];
        private readonly ILookup<string, Room> roomsByName = inputs.Select(input => input.Room)
            .Concat(catalogueContext).Where(room => !string.IsNullOrWhiteSpace(room.Name))
            .ToLookup(room => room.Name, StringComparer.Ordinal);
        private RoomValidationInput currentInput = null!;
        private bool isMirrored;

        internal IReadOnlyList<RoomDiagnostic> Validate()
        {
            var validInputs = new List<RoomValidationInput>();
            foreach (var input in inputs)
            {
                currentInput = input;
                if (CheckStructure(input.Room)) { validInputs.Add(input); }
            }

            var validRooms = validInputs.Select(input => input.Room).ToArray();
            var duplicateLayouts = validRooms.Concat(catalogueContext)
                .Where(room => room.SideView is { Length: >= 4 })
                .ToLookup(room => (room.PalaceNumber == 7, Convert.ToHexString(room.SideView)));
            var activeInputs = validInputs.Where(input => IsActuallyEnabled(input.Room)).ToArray();
            var regularRoomsWithValidLayouts = new List<RoomValidationInput>();
            // Keep the legacy report order: regular rooms, their mirrors, then Great Palace.
            foreach (var input in activeInputs.Where(input => input.Room.PalaceNumber != 7))
            {
                if (!CheckOriginal(input)) { continue; }
                regularRoomsWithValidLayouts.Add(input);
                CheckDuplicates(input.Room, duplicateLayouts);
            }
            foreach (var input in regularRoomsWithValidLayouts)
            {
                currentInput = input;
                CheckMirror(input.Room);
            }
            foreach (var input in activeInputs.Where(input => input.Room.PalaceNumber == 7))
            {
                if (CheckOriginal(input)) { CheckDuplicates(input.Room, duplicateLayouts); }
            }
            return diagnostics.AsReadOnly();
        }

        private bool CheckStructure(Room room)
        {
            int before = diagnostics.Count;
            if (string.IsNullOrWhiteSpace(room.Name)) { Error("MissingRoomName", "Room is missing a name."); }
            else if (roomsByName[room.Name].Skip(1).Any()) { Error("DuplicateRoomName", "Room name is used more than once."); }
            if (room.Connections is not { Length: 4 }) { Error("InvalidConnections", "Room must have four connection bytes."); }
            if (room.ItemPresenceBits is not { Length: 1 } || room.ItemPresenceBits[0] > 0xF) { Error("InvalidBitmask", "Room item bits must fit in four bits."); }
            if (!Enum.IsDefined(room.Group)) { Error("InvalidRoomGroup", "Room group is not recognized."); }
            if (room.PalaceNumber is < 1 or > 7) { Error("InvalidPalaceNumber", "Palace number must be between 1 and 7 when specified."); }
            if (room.LinkRequirements == null) { Error("InvalidRequirements", "Room requirements must be an array."); }
            if (room.LinkedRoomName is { } name)
            {
                var linkedRooms = roomsByName[name];
                var linked = linkedRooms.FirstOrDefault();
                if (linked == null || linkedRooms.Skip(1).Any() || ReferenceEquals(room, linked) ||
                    linked.LinkedRoomName != room.Name || linked.PalaceNumber != room.PalaceNumber ||
                    (room.Enabled && linked.Enabled))
                {
                    Error("InvalidLinkedRoom", $"Linked room \"{name}\" must resolve to a distinct, reciprocal segment for the same palace. At most one segment may be enabled.");
                }
            }
            if (room.PalaceNumber == 7)
            {
                CheckSideviewData<GreatPalaceObject>(room.SideView, "InvalidSideview");
                if (room.MirrorSideView != null) { CheckSideviewData<GreatPalaceObject>(room.MirrorSideView, "InvalidMirrorSideview"); }
            }
            else
            {
                CheckSideviewData<PalaceObject>(room.SideView, "InvalidSideview");
                if (room.MirrorSideView != null) { CheckSideviewData<PalaceObject>(room.MirrorSideView, "InvalidMirrorSideview"); }
            }
            try
            {
                // All palace enemy lists use the same length header and record layout.
                _ = new EnemiesEditable<EnemiesPalace125>(room.Enemies ?? []);
            }
            catch (ArgumentException exception) { Error("InvalidEnemies", exception.Message); }
            return diagnostics.Count == before;
        }

        private void CheckSideviewData<T>(byte[]? bytes, string id) where T : struct, Enum
        {
            try { _ = new SideviewEditable<T>(bytes ?? []); }
            catch (Exception exception) when (exception is ArgumentException or NotImplementedException)
            {
                Error(id, exception.Message);
            }
        }

        private bool IsActuallyEnabled(Room room) => room.Enabled ||
            (room.LinkedRoomName is { } name && roomsByName[name].Any(linked => linked.Enabled));

        private bool CheckOriginal(RoomValidationInput input)
        {
            currentInput = input;
            var room = input.Room;
            try
            {
                if (room.PalaceNumber == 7) { return CheckLayout<GreatPalaceObject, EnemiesGreatPalace>(room); }
                if (room.PalaceGroup == PalaceGrouping.Palace346) { return CheckLayout<PalaceObject, EnemiesPalace346>(room); }
                return CheckLayout<PalaceObject, EnemiesPalace125>(room);
            }
            catch (Exception exception) when (IsLayoutDataException(exception))
            {
                Error("InvalidRoomLayout", exception.Message);
                return false;
            }
        }

        private void CheckMirror(Room room)
        {
            if (room.HasTag("NoMirror") || room.HasBoss || room.IsBossRoom || room.HasItem) { return; }
            isMirrored = true;
            try
            {
                if (room.HasDrop && room.HasUpExit)
                {
                    Warning("CannotMirror", "A room with both a drop and an up elevator cannot be mirrored. Consider the NoMirror tag.");
                }
                else if (room.PalaceGroup == PalaceGrouping.Palace346)
                {
                    CheckLayout<PalaceObject, EnemiesPalace346>(room.Mirror<PalaceObject, EnemiesPalace346>());
                }
                else
                {
                    CheckLayout<PalaceObject, EnemiesPalace125>(room.Mirror<PalaceObject, EnemiesPalace125>());
                }
            }
            catch (Exception exception) when (IsLayoutDataException(exception))
            {
                Warning("CannotMirror", $"Room could not be validated after mirroring: {exception.Message}");
            }
            finally { isMirrored = false; }
        }

        private static bool IsLayoutDataException(Exception exception) => exception is
            ArgumentException or InvalidOperationException or IndexOutOfRangeException or NotImplementedException or OverflowException;

        private bool CheckLayout<T, U>(Room room) where T : struct, Enum where U : struct, Enum
        {
            var sv = new SideviewEditable<T>(room.SideView);
            var ee = new EnemiesEditable<U>(room.Enemies);
            bool greatPalace = room.PalaceNumber == 7;
            if (!isMirrored && ee.Enemies.Count >= 12)
            {
                Error("UnsafeEnemyCount", $"Room has {ee.Enemies.Count} enemies. Rooms with 12 enemies can crash the game.");
                return false;
            }
            if (!isMirrored && ee.Enemies.Count > 9)
            {
                // Nine is the maximum in vanilla. Testing found that twelve can crash the game.
                Warning("TooManyEnemies", $"Room has {ee.Enemies.Count} enemies. Vanilla rooms have at most 9.");
            }
            if (!greatPalace && room.Enabled && sv.HasItem() != room.HasItem)
            {
                Warning("HasItemMismatch", "Room HasItem does not match its sideview data.");
            }
            CheckPageOverflow(sv, ee);
            CheckHeaders(room, sv);
            CheckDoorsAndItems(room, sv, ee);
            if (greatPalace) { CheckDoorsKeyRequirement(room, sv); }
            CheckEnemyPlacements(room, sv, ee);
            CheckForFalseWalls(room, sv);

            bool[,] solidGrid = sv.CreateSolidGrid();
            CheckLeftExit(room, solidGrid);
            CheckRightExit(room, sv.PageCount, solidGrid);
            if (!greatPalace && room.IsBossRoom)
            {
                var statue = sv.Find(o => o.Id is PalaceObject.IRON_KNUCKLE_STATUE && o.AbsX == 62 && o.Y == 9);
                if (statue == null) { Warning("BossRoomMissingStatue", "Boss room is missing exit statue."); }
                if (!FindTileOpeningAtX(solidGrid, (sv.PageCount * 16) - 1))
                {
                    Warning("BossRoomLowCeiling", "Boss room has too low ceiling at exit, preventing re-entry.");
                }
            }

            SortedSet<int> openCeilingTiles = [];
            SortedSet<int> dropTiles = [];
            AddOpenCeilingTiles(sv, solidGrid, openCeilingTiles);
            AddDropTiles(sv, solidGrid, dropTiles);
            var lavaPits = sv.FindAll(o => o.IsLava());
            CheckDropsOverLava(room, sv, lavaPits, openCeilingTiles);
            if (!greatPalace) { CheckFallingBlockRooms(ee, lavaPits, dropTiles); }
            RemoveTilesThatAreLavaPits(lavaPits, dropTiles);
            var elevators = sv.FindAll(o => o.IsElevator());
            CheckElevatorsAndDrops(room, sv, solidGrid, elevators, dropTiles);
            CheckDropZones(room, solidGrid, openCeilingTiles);
            return true;
        }

        private void CheckDuplicates(Room room, ILookup<(bool, string), Room> duplicateLayouts)
        {
            if (!room.Enabled || room.IsEntrance || room.IsBossRoom) { return; }
            var copies = duplicateLayouts[(room.PalaceNumber == 7, Convert.ToHexString(room.SideView))].ToArray();
            if (copies.Length == 1)
            {
                if (!string.IsNullOrEmpty(room.DuplicateGroup))
                {
                    Warning("DuplicateGroupMismatch", "Room has DuplicateGroup set but has no duplicates.");
                }
                return;
            }
            if (copies.Count(copy => copy.Enabled) < 2) { return; }

            var groups = copies.Select(copy => copy.DuplicateGroup).Where(group => !string.IsNullOrEmpty(group))
                .Distinct(StringComparer.Ordinal).ToArray();
            if (groups.Length > 1)
            {
                Warning("DuplicateGroupOverlap", $"Duplicate groups overlap: {string.Join(", ", groups)}.");
                return;
            }
            string expectedGroup = groups.FirstOrDefault() ?? copies.First(copy => copy.Enabled).Name;
            if (room.DuplicateGroup != expectedGroup)
            {
                Warning("DuplicateGroupMismatch", $"Room duplicate group is not synchronized. Set duplicateGroup to \"{expectedGroup}\".");
            }
        }

        private void CheckHeaders<T>(Room room, SideviewEditable<T> sv) where T : struct, Enum
        {
            if (sv.TilesHeader == 0 /* palace room/cave */)
            {
                //No problems found with this (not thoroughly tested)
                if (room.PalaceNumber == 7 && sv.BackgroundPalette != 1) { Warning("BackgroundPalette", "Using an odd background palette"); }

            }
            else if (sv.TilesHeader == 1 /* palace entrance (blue sky)/forest */)
            {
                if (room.PalaceNumber != 7 && sv.BackgroundPalette != 0) { Warning("BackgroundPalette", "Using a background palette > 0 with Palace forest tiles"); }
                else if (room.PalaceNumber == 7 && sv.BackgroundPalette != 1 && !room.IsEntrance) { Warning("BackgroundPalette", "Using a background palette other than 1 with GP forest tiles"); }
            }
            else if (sv.TilesHeader == 3 && room.PalaceNumber == 7 && room.IsBossRoom)
            {
            }
            else
            {
                Warning("TilesHeader", $"Using odd tiles header byte={sv.TilesHeader}");
            }

            if (room.Group == RoomGroup.VANILLA) { return; }

            // We have enough space, it's not worth maintaining rooms with background maps to save a few bytes.
            if (sv.BackgroundMap != 0) { Warning("BackgroundMapInCustomRoom", "Using a background map in custom rooms"); }
        }

        private void CheckPageOverflow<T, U>(SideviewEditable<T> sv, EnemiesEditable<U> ee) where T : struct, Enum where U : struct, Enum
        {
            foreach (var cmd in sv.Commands)
            {
                if (cmd.AbsX + cmd.Width - 1 > 63)
                {
                    Warning("PageOverflow", $" has command that overflows into page 4.\n{cmd.DebugString()}\n{FixedCommandBytes(sv, cmd, fixedCommand =>
                    {
                        fixedCommand.AbsX = Math.Min(63, fixedCommand.AbsX);
                        if (fixedCommand.HasParam()) { fixedCommand.Param = 63 - fixedCommand.AbsX; }
                    })}");
                    break;
                }
                if (cmd.Y < 13 && (cmd.Y + cmd.Height - 1 > 12))
                {
                    Warning("PageOverflow", $" has command that overflows below the last row.\n{cmd.DebugString()}");
                    break;
                }
            }
            foreach (var e in ee.Enemies)
            {
                if (e.X > 63)
                {
                    Warning("PageOverflow", $" has enemy that overflows into page 4.\n{e.DebugString()}\n");
                    break;
                }
                if (e.Y > 12)
                {
                    Warning("PageOverflow", $" has enemy that overflows below the last row.\n{e.DebugString()}\n");
                    break;
                }
            }
        }

        private void CheckDoorsAndItems<T, U>(Room room, SideviewEditable<T> sv, EnemiesEditable<U> ee) where T : struct, Enum where U : struct, Enum
        {
            var lockedDoorCmds = sv.FindAll(o => o.Y < 12 && (int)(object)o.Id == (int)PalaceObjectShared.LOCKED_DOOR);
            var itemCmds = sv.Commands.Where(o => o.HasExtra());

            var lockedDoorEnemies = ee.Enemies.Where(o => (int)(object)o.Id == (int)EnemiesShared.LOCKED_DOOR);
            var elevatorEnemies = ee.Enemies.Where(o => (int)(object)o.Id == (int)EnemiesPalaceShared.ELEVATOR);
            var strikeForJarEnemies = ee.Enemies.Where(o => o.IsStrikeForJarLike());

            if (lockedDoorEnemies.Count() > 0) { Warning("LockedDoorEnemy", "Room uses LockedDoor enemy."); }
            if (elevatorEnemies.Count() > 0) { Warning("ElevatorEnemy", "Room uses Elevator enemy."); }

            if (room.Group == RoomGroup.VANILLA) { return; }

            for (int page = 0; page < 4; page++)
            {
                List<string> itemsOnPage = new();

                foreach (var o in lockedDoorCmds.Where(o => o.Page == page)) { itemsOnPage.Add($"Cmd:{o.Id} ({Convert.ToHexString(o.Bytes)})"); }
                foreach (var o in itemCmds.Where(o => o.Page == page)) { itemsOnPage.Add($"Cmd:{o.Extra} ({Convert.ToHexString(o.Bytes)})"); }
                if (itemsOnPage.Count == 0)
                {
                    continue; // if no locked doors or items are involved, we don't mind.
                }
                foreach (var o in strikeForJarEnemies.Where(o => o.Page == page)) { itemsOnPage.Add($"Enemy:{o.Id} ({Convert.ToHexString(o.Bytes)})"); }

                if (itemsOnPage.Count > 1)
                {
                    Warning("MultipleItemsOnPage", $"Has multiple items on page {page + 1}: {string.Join(", ", itemsOnPage)}");
                }
            }
        }

        private void CheckDoorsKeyRequirement<T>(Room room, SideviewEditable<T> sv) where T : struct, Enum
        {
            var lockedDoorCmds = sv.FindAll(o => o.Y < 12 && (int)(object)o.Id == (int)PalaceObjectShared.LOCKED_DOOR);

            if (lockedDoorCmds.Count > 0 && !room.LinkRequirements.Contains(RequirementType.KEY)) { Warning("LockedDoorNotInRequirements", $"Locked doors in GP are supposed to be in requirements."); }
        }

        private void CheckEnemyPlacements<T, U>(Room room, SideviewEditable<T> sv, EnemiesEditable<U> ee) where T : struct, Enum where U : struct, Enum
        {
            var xEnd = sv.PageCount * 16 - 6;
            foreach (var enemy in ee.Enemies)
            {
                if (!(enemy.IsShufflableSmallOrLarge())) { continue; }
                if (room.HasLeftExit && enemy.X < 5) { Warning("EnemyTooCloseToEntrance", $"Room has an enemy too close to the left entrance: {enemy.DebugString()}"); }
                if (room.HasRightExit && enemy.X > xEnd) { Warning("EnemyTooCloseToEntrance", $"Room has an enemy too close to the right entrance: {enemy.DebugString()}"); }
            }
        }

        private void CheckForFalseWalls<T>(Room room, SideviewEditable<T> sv) where T : struct, Enum
        {
            foreach (var cmd in sv.Commands)
            {
                if (cmd.Id is PalaceObject.WALKTHROUGH_WALL || cmd.Id is GreatPalaceObject.WALKTHROUGH_WALL)
                {
                    if (room.Tags == null || !room.Tags.Contains("WalkthroughWall"))
                    {
                        Warning("WalkthroughWall", $"Room uses walkthrough walls: {cmd.DebugString()}");
                    }
                    break;
                }
            }
        }

        private static void AddOpenCeilingTiles<T>(SideviewEditable<T> sv, bool[,] solidGrid, SortedSet<int> openCeilingTiles) where T : struct, Enum
        {
            int w = sv.PageCount * 16;
            for (var x = 0; x < w; x++)
            {
                if (!solidGrid[x, 0])
                {
                    openCeilingTiles.Add(x);
                }
            }
        }

        private static void AddDropTiles<T>(SideviewEditable<T> sv, bool[,] solidGrid, SortedSet<int> dropTiles) where T : struct, Enum
        {
            int w = sv.PageCount * 16;
            for (var x = 0; x < w; x++)
            {
                if (!solidGrid[x, 12] && !solidGrid[x, 11])
                {
                    dropTiles.Add(x);
                }
            }
        }

        private static void RemoveTilesThatAreLavaPits<T>(List<SideviewMapCommand<T>> pits, SortedSet<int> dropTiles) where T : struct, Enum
        {
            foreach (var pit in pits)
            {
                for (var i = 0; i < pit.Param + 1; i++)
                {
                    dropTiles.Remove(pit.AbsX + i);
                }
            }
        }

        private void CheckDropsOverLava<T>(Room room, SideviewEditable<T> sv, List<SideviewMapCommand<T>> lavaPits, SortedSet<int> openCeilingTiles) where T : struct, Enum
        {
            if (!room.IsDropZone) { return; }

            SortedSet<int> dropTilesOverLava = [];

            foreach (var lavaPit in lavaPits)
            {
                var endX = lavaPit.AbsX + lavaPit.Width;
                for (int x = lavaPit.AbsX; x < endX; x++)
                {
                    if (x < 16 || x > 47) { continue; }
                    if (openCeilingTiles.Contains(x))
                    {
                        if (!sv.Commands.Any(o => o.IsSolid && o.Intersects(x, x, 8, 11)))
                        {
                            dropTilesOverLava.Add(x);
                        }
                    }
                }
            }
            if (dropTilesOverLava.Count > 0)
            {
                Warning("DropOverLava", $"Room is a drop zone over lava at x={ConvertToRangeString(dropTilesOverLava)}");
            }
        }

        private void CheckFallingBlockRooms<T, U>(EnemiesEditable<U> ee, List<SideviewMapCommand<T>> lavaPits, SortedSet<int> dropTiles) where T : struct, Enum where U : struct, Enum
        {
            var singles = ee.Enemies.Where(e => e.Id is EnemiesPalace125.SINGLE_FALLING_BLOCK || e.Id is EnemiesPalace346.SINGLE_FALLING_BLOCK).ToList();
            // Generators create blocks from positions X-5 to X+4 (inclusive)
            var gens = ee.Enemies.Where(e => e.Id is EnemiesPalace125.FALLING_BLOCK_GENERATOR || e.Id is EnemiesPalace346.FALLING_BLOCK_GENERATOR).ToList();

            foreach (var lavaPit in lavaPits)
            {
                var endX = lavaPit.AbsX + lavaPit.Width;
                for (int x = lavaPit.AbsX; x < endX; x++)
                {
                    foreach (var gen in gens)
                    {
                        if (x <= gen.X + 4 && gen.X - 5 <= endX)
                        {
                            Warning("FallingBlockGenerator", $"Room has a falling block generator over lava");
                            return;
                        }
                    }
                    foreach (var single in singles)
                    {
                        if (x <= single.X && single.X <= endX)
                        {
                            Warning("SingleFallingBlock", $"Room has a falling block generator over lava");
                            return;
                        }
                    }
                }
            }

            foreach (var x in dropTiles)
            {
                foreach (var gen in gens)
                {
                    if (x <= gen.X + 4 && gen.X - 5 <= x)
                    {
                        Warning("FallingBlockGenerator", $"Room has a falling block generator over drop");
                        return;
                    }
                }
                foreach (var single in singles)
                {
                    if (x == single.X)
                    {
                        Warning("SingleFallingBlock", $"Room has a falling block generator over drop");
                        return;
                    }
                }
            }
        }

        private void CheckLeftExit(Room room, bool[,] solidGrid)
        {
            bool hasLeftOpeningWall = FindTileOpeningAtX(solidGrid, 0);
            if (room.HasLeftExit && !hasLeftOpeningWall) { Warning("LeftExitNotOpen", "Room is marked as having a left exit, but there is no left wall opening"); }
        }

        private void CheckRightExit(Room room, int pageCount, bool[,] solidGrid)
        {
            if (pageCount == 1 && room.HasRightExit) { Warning("SinglePageRightExit", "Single page rooms can not have a right exit"); }
            if (pageCount == 3 && room.HasRightExit) { Warning("ThreePageRightExit", "3-page rooms can't be entered from the right. (Engine limitation.)"); }
            bool hasRightOpeningWall = FindTileOpeningAtX(solidGrid, (pageCount * 16) - 1);
            if (room.HasRightExit && !hasRightOpeningWall) { Warning("RightExitNotOpen", "Room is marked as having a right exit, but there is no right wall opening"); }
        }

        private static bool FindTileOpeningAtX(bool[,] solidGrid, int x, int n = 3)
        {
            int count = 0;
            for (int j = 0; j < solidGrid.GetLength(1); j++)
            {
                count = !solidGrid[x, j] ? count + 1 : 0;
                if (count == n) return true;
            }
            return false;
        }

        private void CheckElevatorsAndDrops<T>(Room room, SideviewEditable<T> sv, bool[,] solidGrid, List<SideviewMapCommand<T>> allElevators, SortedSet<int> dropTiles) where T : struct, Enum
        {
            bool IsDownElevator(SideviewMapCommand<T> o)
            {
                // check if there is at least a 1 tile wide open path down
                // from the default elevator y start position
                int x = o.AbsX;
                int y = 8;
                for (; y < 13; y++)
                {
                    if (solidGrid[x, y]) { break; }
                }
                if (y != 13)
                {
                    x = o.AbsX + 1;
                    y = 8;
                    for (; y < 13; y++)
                    {
                        if (solidGrid[x, y]) { break; }
                    }
                }
                return y == 13;
            }
            bool IsUpElevator(SideviewMapCommand<T> elev)
            {
                var elevatorY = 10 - elev.Param * 2;
                return SolidGridHelper.AreaIsOpen(solidGrid, elev.AbsX, 0, 1, elevatorY) || SolidGridHelper.AreaIsOpen(solidGrid, elev.AbsX + 1, 0, 1, elevatorY);
            }

            if (allElevators.Count > 1) { Warning("MultipleExitElevators", "Room has more than one elevator"); }
            int gridWidth = solidGrid.GetLength(0);
            if (allElevators.Any(elevator => elevator.AbsX + 1 >= gridWidth))
            {
                Warning("ElevatorOutsideRoom", "Room has an elevator that extends beyond the last column.");
                allElevators = allElevators.Where(elevator => elevator.AbsX + 1 < gridWidth).ToList();
            }


            foreach (var elevator in allElevators)
            {
                // calculate optimal elevator param (y position)
                if (IsDownElevator(elevator))
                {
                    // if you go above param 2 for down elevators you will respawn below the elevator
                    if (elevator.Param > 2)
                    {
                        Warning("ElevatorParam", $"Down elevator has param > 2.\n{FixedCommandBytes(sv, elevator, command => command.Param = 2)}");
                    }
                }
                else
                {
                    int y = 0;
                    for (; y < 11; y++)
                    {
                        if (solidGrid[elevator.AbsX, y] && solidGrid[elevator.AbsX + 1, y]) { break; }
                    }
                    if (y < 3) { continue; /* not up elevator */ }
                    int optimalParam = Math.Min((11 - y) * 2, 0xf);
                    if (Math.Abs(elevator.Param - optimalParam) > 2) // allow a little offset from the optimal position for style
                    {
                        Warning("ElevatorParam", $"Up elevator optimal param is {optimalParam}.\n{FixedCommandBytes(sv, elevator, command => command.Param = optimalParam)}");
                    }
                }
            }

            // skip non-exit elevators
            var exitElevators = allElevators.TakeWhile(o => IsUpElevator(o) || IsDownElevator(o)).ToList();

            foreach (var elevator in exitElevators)
            {
                var x = elevator.AbsX;
                if (x / 16 != room.ElevatorScreen) { Warning("ElevatorWrongScreen", $"ElevatorScreen={room.ElevatorScreen} but elevator.xpos={x}"); }
                var pageX = x % 16;
                if (pageX < 7) { Warning("ElevatorNotCentered", $"Elevator.xpos={x} is too far left on the page"); }
                if (pageX > 7) { Warning("ElevatorNotCentered", $"Elevator.xpos={x} is too far right on the page"); }
            }

            if (room.ElevatorScreen != -1 && room.IsUpDownReversed) { Warning("IsUpDownReversedElevator", $"IsUpDownReversed does not apply to elevators"); }
            if (room.ElevatorScreen != -1 && exitElevators.Count == 0) { Warning("ElevatorMissing", $"ElevatorScreen={room.ElevatorScreen} but room has no exit elevator"); }
            if (room.HasUpExit)
            {
                if (room.ElevatorScreen == -1) { Warning("ElevatorMissing", $"Room is marked as having an up exit but ElevatorScreen=-1"); }
                if (exitElevators.Count == 0) { Warning("ElevatorMissing", "Room has no exit elevator but is marked as having an up exit"); }
                else
                {
                    if (exitElevators.Find(o => IsUpElevator(o)) == null)
                    {
                        Warning("ElevatorCannotGoUp", "Elevator cannot go up but room is marked as having an up exit");
                    }
                }
                if (sv.PageCount == 3 && room.HasRightExit) { Warning("UpConnectorConflict", "Up and Right exits cannot both use the 3rd connection byte"); }
            }
            if (room.HasDownExit && !room.HasDrop)
            {
                if (room.ElevatorScreen == -1) { Warning("ElevatorMissing", "Room is marked as having a down exit but ElevatorScreen=-1"); }
                if (exitElevators.Count == 0) { Warning("ElevatorMissing", "Room has no exit elevator but is marked as having a down exit"); }
                else
                {
                    if (exitElevators.Find(o => IsDownElevator(o)) == null)
                    {
                        Warning("ElevatorCannotGoDown", "Elevator cannot go down but room is marked as having a down exit");
                    }
                }
                if (sv.PageCount == 2 && room.HasRightExit) { Warning("DownConnectorConflict", "Down and Right exits cannot both use the 2nd connection byte"); }
            }
            if (room.IsDropZone)
            {
                if (exitElevators.Find(o => IsUpElevator(o)) != null)
                {
                    Warning("ElevatorUpInDropZone", "Room cannot be a drop zone that has an up elevator");
                }
            }

            if (room.HasDownExit)
            {
                foreach (var elevator in exitElevators)
                {
                    dropTiles.Remove(elevator.AbsX);
                    dropTiles.Remove(elevator.AbsX + 1);
                }
            }

            if (!room.HasDrop)
            {
                // probably this is something like a bridge over a drop that you can't get to
                if (dropTiles.Count > 0) { Warning("DropTilesInNonDropRoom", $"Room is not a drop room, but has drop tiles at x={ConvertToRangeString(dropTiles)}"); }
            }
            else
            {
                if (dropTiles.Count == 0)
                {
                    Warning("NoDropTilesInDropRoom", "Room HasDrop but no drop tiles found.");
                }
                else
                {
                    var dropScreen = room.IsUpDownReversed ? 2 : 1;
                    var badDropTiles = dropTiles.Where(x => x < dropScreen * 16 + 1 || dropScreen * 16 + 15 < x || x < DROP_MIN_X || x > DROP_MAX_X);
                    if (badDropTiles.Count() > 0)
                    {
                        Warning("DropTilesOutsideRange", $"Drop tiles outside of valid range at x={ConvertToRangeString(badDropTiles)} (dropScreen={dropScreen})");
                    }

                    CheckDropConnector(room, sv, dropScreen);
                }
            }
            if (room.HasDroppableElevator)
            {
                if (room.ElevatorScreen < 0) { Warning("ElevatorMissing", $"HasDroppableElevator==true but room has no elevator"); }
                else { CheckDropConnector(room, sv, room.ElevatorScreen); }
            }
        }

        private void CheckDropConnector<T>(Room room, SideviewEditable<T> sv, int dropScreen) where T : struct, Enum
        {
            switch (dropScreen)
            {
                case 0:
                    if (room.HasLeftExit) { Warning("DropConnectorConflict", "Drop on first page and Left exit cannot both use the 1st connection byte"); }
                    break;
                case 1:
                    if (sv.PageCount == 2 && room.HasRightExit) { Warning("DropConnectorConflict", "Drop on second page and Right exit cannot both use the 2nd connection byte"); }
                    break;
                case 2:
                    if (sv.PageCount == 3 && room.HasRightExit) { Warning("DropConnectorConflict", "Drop on third page and Right exit cannot both use the 3rd connection byte"); }
                    if (room.HasUpExit) { Warning("DropConnectorConflict", "Drop on third page and Up elevator exit cannot both use the 3rd connection byte"); }
                    break;
                case 3:
                    if (room.HasRightExit) { Warning("DropConnectorConflict", "Drop on fourth page and Right exit cannot both use the 4th connection byte"); }
                    break;
            }
        }

        private void CheckDropZones(Room room, bool[,] solidGrid, SortedSet<int> openCeilingTiles)
        {
            if (!room.IsDropZone) { return; }
            SortedSet<int> shouldBeOpen = new(Enumerable.Range(16, 32));
            shouldBeOpen.IntersectWith(openCeilingTiles);
            if (shouldBeOpen.Count < 6) // not sure what is the lowest allowed
            {
                Warning("DropZoneNotOpen", $"Is drop zone but is only open at x={ConvertToRangeString(shouldBeOpen)}");
            }
            int lastRespawnColumn = Math.Min(DROP_MAX_X, solidGrid.GetLength(0) - 1);
            for (int x = DROP_MIN_X; x < lastRespawnColumn; x++)
            {
                if (!FindTileOpeningAtX(solidGrid, x))
                {
                    // you'd respawn at y = 0 inside the wall in this case
                    // a few untested theories below
                    if (!solidGrid[x, 0]) // small 1 gap tile at top could be enough to break free?
                    {
                        continue;
                    }
                    if (!solidGrid[x, 2] && !solidGrid[x, 3]) // 2-high opening under where you would respawn in the wall - does that mean you fall down?
                    {
                        continue;
                    }
                    if (!solidGrid[x - 1, 0] && !solidGrid[x - 1, 1]) // 2-high opening to the left of where you'll spawn - can you just walk out?
                    {
                        continue;
                    }
                    if (!solidGrid[x + 1, 0] && !solidGrid[x + 1, 1]) // 2-high opening to the right
                    {
                        continue;
                    }
                    Warning("DropZoneNoRespawnPoint", $"Is drop zone but there is no respawn point at x={x}");
                    break;
                }
            }
        }

        private static string ConvertToRangeString(IEnumerable<int> numbers)
        {
            return string.Join(",", numbers
                .Select((num, index) => new { num, index })
                .GroupBy(x => x.num - x.index) // group by how many numbers skipped
                .Select(g => g.Count() > 1 ? $"{g.First().num}-{g.Last().num}" : $"{g.First().num}"));
        }

        private static string FixedCommandBytes<T>(SideviewEditable<T> sideview, SideviewMapCommand<T> command,
            Action<SideviewMapCommand<T>> edit) where T : struct, Enum
        {
            var commands = sideview.Commands;
            var copy = sideview.Copy();
            edit(copy.Commands[commands.IndexOf(command)]);
            return $"Fixed bytes:\n{Convert.ToHexString(copy.Finalize())}\n\n";
        }

        private void Warning(string id, string message)
        {
            if (currentInput.Room.SuppressWarning?.Contains(id) == true) { return; }
            Add(id, RoomDiagnosticSeverity.Warning, message);
        }

        private void Error(string id, string message) => Add(id, RoomDiagnosticSeverity.Error, message);

        private void Add(string id, RoomDiagnosticSeverity severity, string message) => diagnostics.Add(
            new RoomDiagnostic(id, severity, currentInput.Source, currentInput.RoomIndex,
                currentInput.Room.Name, isMirrored, message));
    }
}
