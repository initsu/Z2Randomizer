using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using Z2Randomizer.RandomizerCore;

namespace Z2Randomizer.RandomizerCore.Sidescroll.Palace;

internal class ByEntranceDirectionItemRoomSelectionStrategy : ItemRoomSelectionStrategy
{
    private const int MAX_ATTEMPTS = 200;

    private const bool BREAK_EARLY_ON_FIRST_FAILED_DIRECTION = false;
    public override Room[] SelectItemRooms(Palace palace, RoomPool roomPool, int itemRoomCount, bool avoidDuplicates, Random r)
    {
        if (roomPool.ItemRooms.HasMixedPriorities())
        {
            return SelectByPriority(roomPool, itemRoomCount, avoidDuplicates, r);
        }
        List<Room> itemRooms = [], originalItemRooms = [];
        int attemptNumber = 0;
        Direction itemRoomDirection;
        List<Direction> remainingDirections = new(DirectionExtensions.ITEM_ROOM_ORIENTATIONS);
        while (itemRooms.Count < itemRoomCount && attemptNumber++ < MAX_ATTEMPTS && remainingDirections.Count > 0)
        {
            itemRoomDirection = remainingDirections.Sample(r);
            if (!roomPool.ItemRoomsByDirection.TryGetValue(itemRoomDirection, out var value))
            {
                remainingDirections.Remove(itemRoomDirection);
#pragma warning disable CS0162 // Unreachable code detected
                if (BREAK_EARLY_ON_FIRST_FAILED_DIRECTION)
                {
                    return [];
                }
#pragma warning restore CS0162 // Unreachable code detected
                continue;
            }
            Room itemRoom = value.Next(r);

            if (!avoidDuplicates || !originalItemRooms.Contains(itemRoom))
            {
                originalItemRooms.Add(itemRoom);
                itemRooms.Add(new(itemRoom));
            }

        }


        return itemRooms.ToArray();
    }

    private static Room[] SelectByPriority(RoomPool pool, int count, bool avoidDuplicates, Random random)
    {
        var tables = DirectionExtensions.ITEM_ROOM_ORIENTATIONS.Where(pool.ItemRoomsByDirection.ContainsKey)
            .Select(direction => pool.ItemRoomsByDirection[direction]).ToList();
        var candidates = tables.SelectMany(table => table.Keys()).Distinct().ToArray();
        var chosen = new HashSet<Room>();
        var result = new List<Room>();
        while (result.Count < count)
        {
            var eligible = candidates.Where(room => !avoidDuplicates || !chosen.Contains(room)).ToArray();
            if (eligible.Length == 0) { break; }
            int priority = eligible.Max(room => room.Priority);
            var eligibleTier = eligible.Where(room => room.Priority == priority).ToHashSet();
            var table = tables.Where(table => table.Keys().Any(eligibleTier.Contains)).ToList().Sample(random);
            if (table == null) { break; }

            var weights = table.Keys().Where(eligibleTier.Contains).Select(room => (room, table.Weight(room))).ToList();
            var selected = new TableWeightedRandom<Room>(weights).Next(random);
            chosen.Add(selected);
            result.Add(new Room(selected));
        }
        return result.ToArray();
    }
}
