using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Z2Randomizer.RandomizerCore.Sidescroll.Palace;

/// <summary>
/// The set of palace rooms used to build room pools.
/// </summary>
/// <remarks>
/// Group and tag lookups index rooms enabled when this instance is created.
/// Name lookup also includes disabled rooms, such as linked secondary segments.
/// Custom pool rules can select from the full set of rooms. Vanilla layouts
/// and built-in fallbacks use <see cref="BundledRooms"/>.
/// </remarks>
public partial class PalaceRooms
{
    private readonly Dictionary<RoomGroup, List<Room>> roomsByGroup = new();
    public IReadOnlyList<Room> RoomsByGroup(RoomGroup group) => roomsByGroup.GetValueOrDefault(group, []);
    private readonly Dictionary<string, List<Room>> roomsByTag = new();
    public IReadOnlyList<Room> RoomsByTag(string tag) => roomsByTag.GetValueOrDefault(tag, []);
    private readonly Dictionary<string, Room> roomsByName = new();

    /// <summary>
    /// All definitions in input order, including disabled rooms. This sequence
    /// supports validation, merging, copying rooms for a pool, and hashing them in order.
    /// </summary>
    internal IReadOnlyList<Room> RoomsInInputOrder { get; }

    /// <summary>
    /// All rooms from the starting set of rooms (normally <c>PalaceRooms.json</c>),
    /// including enabled and disabled rooms in every group.
    /// </summary>
    /// <remarks>
    /// When files from <c>Rooms/</c> are loaded, they extend the set of rooms used.
    /// Vanilla layouts and built-in fallback lookups keep using <see cref="BundledRooms"/>.
    /// </remarks>
    internal PalaceRooms BundledRooms => bundledRooms ?? this;
    private readonly PalaceRooms? bundledRooms;

    public string Hash { get; init; }

    public static readonly string roomsMD5 = "JCa3OsnJhIe/fZ5yrx/+mA==";

    public PalaceRooms(string palaceJson, bool doValidation, VanillaRoomData? vanillaRoomData = null)
        : this(ReadDefinitions(doValidation ? "CustomRooms.json" : "PalaceRooms.json", palaceJson, doValidation, vanillaRoomData))
    {
    }

    private static IEnumerable<Room> ReadDefinitions(string filename, string json, bool validateChecksum, VanillaRoomData? vanillaRoomData)
    {
        if (validateChecksum)
        {
            var hash = MD5Hash.ComputeHash(Encoding.UTF8.GetBytes(RemoveNewLines().Replace(json, "")));
            if (roomsMD5 != Convert.ToBase64String(hash))
            {
                throw new UserFacingException("Invalid room file", $"Invalid {filename} checksum.");
            }
        }

        var inputs = new List<RoomValidationInput>();
        var diagnostics = new List<RoomDiagnostic>();
        RoomFileReader.Read(filename, json, inputs, diagnostics, vanillaRoomData);
        if (diagnostics.Count != 0)
        {
            throw new UserFacingException("Invalid room file", string.Join(Environment.NewLine, diagnostics));
        }
        return inputs.Select(input => input.Room);
    }

    public PalaceRooms(IEnumerable<Room> rooms) : this(null, rooms) { }

    // The combined set of rooms must start with the bundled definitions in
    // their original order, followed by the custom definitions.
    internal PalaceRooms(PalaceRooms? bundledRooms, IEnumerable<Room> rooms)
    {
        this.bundledRooms = bundledRooms;
        RoomsInInputOrder = rooms.ToArray();
        foreach (var room in RoomsInInputOrder)
        {
            if (room.Enabled)
            {
                if (!roomsByGroup.TryGetValue(room.Group, out var groupList))
                {
                    groupList = [];
                    roomsByGroup.Add(room.Group, groupList);
                }
                groupList.Add(room);

                if (room.Tags != null)
                {
                    foreach (var tag in room.Tags)
                    {
                        AddRoomToTagList(room, tag);
                    }
                }
            }

            roomsByName[room.Name] = room;
        }

        Hash = DeterministicHash.Combine(DeterministicHash.Seed, RoomsInInputOrder).ToString("X8").ToLower();
    }

    internal PalaceRooms CopyForRoomPool(int palaceNumber)
    {
        var roomsToCopy = new HashSet<Room>(ReferenceEqualityComparer.Instance);
        foreach (var room in RoomsInInputOrder)
        {
            bool matchesPalace = room.PalaceNumber == palaceNumber || (room.PalaceNumber == null && palaceNumber < 7);
            if (!matchesPalace) { continue; }
            roomsToCopy.Add(room);
            if (room.LinkedRoomName is { } name && GetRoomByName(name) is { } linkedRoom)
            {
                roomsToCopy.Add(linkedRoom);
            }
        }

        var copies = new List<Room>(roomsToCopy.Count);
        int bundledCount = bundledRooms?.RoomsInInputOrder.Count ?? 0;
        int bundledCopyCount = 0;
        for (int index = 0; index < RoomsInInputOrder.Count; index++)
        {
            var room = RoomsInInputOrder[index];
            if (!roomsToCopy.Contains(room)) { continue; }
            copies.Add(new Room(room));
            if (index < bundledCount) { bundledCopyCount++; }
        }

        // Reuse the same per-pool copies in both views so an explicit override to a
        // bundled room applies when that room is selected as a fallback too.
        PalaceRooms? bundledCopies = bundledRooms == null ? null : new PalaceRooms(copies.Take(bundledCopyCount));
        return new PalaceRooms(bundledCopies, copies);
    }

    private void AddRoomToTagList(Room room, string tag)
    {
        if (!roomsByTag.TryGetValue(tag, out var tagList))
        {
            tagList = [];
            roomsByTag.Add(tag, tagList);
        }
        tagList.Add(room);
    }

    public IEnumerable<Room> VanillaPalaceRoomsByPalaceNumber(int palaceNum)
    {
        int mapMin, mapMax;
        PalaceGrouping palaceGroup = Util.AsPalaceGrouping(palaceNum) ?? throw new Exception("Invalid vanilla palace room without PalaceGroup set");
        switch (palaceNum)
        {
            case 1:
                mapMin = 0;
                mapMax = 13;
                break;
            case 2:
                mapMin = 14;
                mapMax = 34;
                break;
            case 3:
                mapMin = 0;
                mapMax = 14;
                break;
            case 4:
                mapMin = 15;
                mapMax = 35;
                break;
            case 5:
                mapMin = 35;
                mapMax = 62;
                break;
            case 6:
                mapMin = 36;
                mapMax = 62;
                break;
            case 7:
                mapMin = 0;
                mapMax = 54;
                break;
            default:
                throw new ArgumentException("Invalid palace number: " + palaceNum);
        }


        var roomgroup = roomsByGroup[RoomGroup.VANILLA];

        return roomgroup.Where(
            i => //i.Group == RoomGroup.VANILLA &&
                 i.VanillaPalaceGroup == palaceGroup
                 && i.Map >= mapMin
                 && i.Map <= mapMax
                 && i is { IsEntrance: false, IsBossRoom: false, HasItem: false, IsThunderBirdRoom: false }
        );
    }

    public Room VanillaBossRoom(int palaceNum)
    {
        /*
        var map = palaceNum switch
        {
            1 => 13,
            2 => 34,
            3 => 14,
            4 => 28,
            5 => 41,
            6 => 58,
            7 => 54,
            _ => throw new ArgumentException("Invalid palace number: " + palaceNum)
        };
        */
        // might be null for test pool
        return roomsByGroup[RoomGroup.VANILLA].FirstOrDefault(i => i.IsBossRoom && i.PalaceNumber == palaceNum)!;
    }

    public Room VanillaItemRoom(int palaceNum)
    {
        var map = palaceNum switch
        {
            1 => 8,
            2 => 20,
            3 => 11,
            4 => 31,
            5 => 61,
            6 => 44,
            7 => throw new ArgumentException("GP Cannot have an item!"),
            _ => throw new ArgumentException("Invalid palace number: " + palaceNum)
        };
        return roomsByGroup[RoomGroup.VANILLA].First(i => i.HasItem && map == i.Map);
    }

    public IEnumerable<Room> ItemRoomsByDirection(RoomGroup group, Direction direction)
    {
        return ItemRoomsByDirection(roomsByGroup.GetValueOrDefault(group, []), direction);
    }

    public IEnumerable<Room> ItemRoomsByDirection(IReadOnlyList<Room> rooms, Direction direction)
    {
        if (direction == Direction.NONE)
        {
            throw new ArgumentException("Invalid Direction.NONE in ItemRoomsByDirection");
        }

        return direction switch
        {
            Direction.NORTH => rooms.Where(i => i.HasItem && i.HasUpExit),
            Direction.SOUTH => rooms.Where(i => i.HasItem && i.HasDownExit),
            Direction.WEST => rooms.Where(i => i.HasItem && i.HasLeftExit),
            Direction.EAST => rooms.Where(i => i.HasItem && i.HasRightExit),
            _ => throw new ImpossibleException("Invalid direction in ItemRoomsByDirection")
        };
    }

    public IEnumerable<Room> ItemRoomsByShape(RoomGroup group, RoomExitType shape)
    {
        return ItemRoomsByShape(roomsByGroup.GetValueOrDefault(group, []), shape);
    }

    public IEnumerable<Room> ItemRoomsByShape(IReadOnlyList<Room> rooms, RoomExitType shape)
    {
        return rooms.Where(i => i.HasItem && i.CategorizeExits() == shape);
    }

    public IEnumerable<Room> ItemRooms(RoomGroup group)
    {
        return ItemRooms(roomsByGroup.GetValueOrDefault(group, []));
    }

    public IEnumerable<Room> ItemRooms(IReadOnlyList<Room> rooms)
    {
        return rooms.Where(i => i.HasItem);
    }

    public IEnumerable<Room> NormalPalaceRoomsByGroup(RoomGroup group)
    {
        return NormalPalaceRoomsInList(roomsByGroup.GetValueOrDefault(group, []));
    }

    public IEnumerable<Room> NormalPalaceRoomsInList(IReadOnlyList<Room> rooms)
    {
        return rooms.Where(i => (i.PalaceNumber ?? 1) != 7
            && i is { IsThunderBirdRoom: false, HasItem: false, IsBossRoom: false, IsEntrance: false });
    }

    public IEnumerable<Room> GpRoomsByGroup(RoomGroup group)
    {
        return GpRoomsInList(roomsByGroup.GetValueOrDefault(group, []));
    }

    public IEnumerable<Room> GpRoomsInList(IReadOnlyList<Room> rooms)
    {
        return rooms.Where(i => (i.PalaceNumber ?? 1) == 7
            && i is { IsThunderBirdRoom: false, HasItem: false, IsBossRoom: false, IsEntrance: false });
    }

    public IEnumerable<Room> Entrances(RoomGroup group)
    {
        return Entrances(roomsByGroup.GetValueOrDefault(group, []));
    }

    public IEnumerable<Room> Entrances(IReadOnlyList<Room> rooms)
    {
        return rooms.Where(i => i.IsEntrance);
    }

    public IEnumerable<Room> ThunderBirdRooms(RoomGroup group)
    {
        return ThunderBirdRooms(roomsByGroup.GetValueOrDefault(group, []));
    }

    public IEnumerable<Room> ThunderBirdRooms(IReadOnlyList<Room> rooms)
    {
        return rooms.Where(i => i.IsThunderBirdRoom);
    }

    public IEnumerable<Room> BossRooms(RoomGroup group, int? palaceNum = null)
    {
        return BossRooms(roomsByGroup.GetValueOrDefault(group, []), palaceNum);
    }

    public IEnumerable<Room> BossRooms(IReadOnlyList<Room> rooms, int? palaceNum = null)
    {
        return rooms.Where(i => i.IsBossRoom && (palaceNum == null || palaceNum == i.PalaceNumber));
    }

    public Room? GetRoomByName(string name)
    {
        return roomsByName.GetValueOrDefault(name);
    }

    public Dictionary<string, Room> LinkedRooms(RoomGroup group, int? palaceNum = null)
    {
        return LinkedRooms(roomsByGroup.GetValueOrDefault(group, []));
    }

    public Dictionary<string, Room> LinkedRooms(IReadOnlyList<Room> rooms, int? palaceNum = null)
    {
        Dictionary<string, Room> linkedRooms = [];
        foreach (Room room in rooms)
        {
            if (room.Enabled && room.LinkedRoomName != null)
            {
                linkedRooms.Add(room.LinkedRoomName, GetRoomByName(room.LinkedRoomName)!);
                linkedRooms.Add(room.Name, GetRoomByName(room.Name)!);
            }
        }
        return linkedRooms;
    }

    [GeneratedRegex(@"[\n\r\f]")]
    private static partial Regex RemoveNewLines();
}
