using System.Collections.Generic;
using System.Linq;

namespace Z2Randomizer.RandomizerCore.Sidescroll.Palace;

internal static class RoomSelection
{
    internal static bool HasMixedPriorities(this IReadOnlyList<Room> rooms) =>
        rooms.Count > 1 && rooms.Any(room => room.Priority != rooms[0].Priority);

    /// <summary>Choose randomly among the highest-priority candidates that have not failed in this layout.</summary>
    internal static Room? SampleByPriority(this IReadOnlyList<Room> rooms, Random random, HashSet<Room>? failed = null)
    {
        int bestPriority = int.MinValue;
        int count = 0;
        foreach (var room in rooms)
        {
            if (failed?.Contains(room) == true || room.Priority < bestPriority) { continue; }
            if (room.Priority > bestPriority)
            {
                bestPriority = room.Priority;
                count = 0;
            }
            count++;
        }
        if (count == 0) { return null; }
        if (count == rooms.Count) { return rooms[random.Next(count)]; }

        int index = random.Next(count);
        return rooms.First(room => room.Priority == bestPriority && failed?.Contains(room) != true && index-- == 0);
    }

    internal static List<Room> ShuffleByPriority(this List<Room> rooms, Random random)
    {
        rooms.FisherYatesShuffle(random);
        if (!rooms.HasMixedPriorities()) { return rooms; }
        return rooms.OrderByDescending(room => room.Priority).ToList();
    }

    /// <summary>Try replacing lower-priority rooms first so preferred rooms stay placed.</summary>
    internal static List<Room> ShuffleReplacementTargets(this List<Room> rooms, Random random)
    {
        rooms.FisherYatesShuffle(random);
        if (!rooms.HasMixedPriorities()) { return rooms; }
        return rooms.OrderBy(room => room.Priority).ToList();
    }
}
