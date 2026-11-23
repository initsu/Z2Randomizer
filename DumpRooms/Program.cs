// See https://aka.ms/new-console-template for more information
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

using System.Text;
using System.Text.Json;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Sidescroll;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

var vanillaRoomData = new VanillaRoomData(File.ReadAllBytes(args[0]));
StringBuilder sb = new StringBuilder("[");
foreach (var palaceGroup in Enum.GetValues<PalaceGrouping>())
{
    for (byte map = 0; map <= VanillaRoomData.LastMap; map++)
    {
        byte[] sideView;
        byte[] enemies;
        try
        {
            sideView = vanillaRoomData.ReadSideview(palaceGroup, map);
            enemies = vanillaRoomData.ReadEnemies(palaceGroup, map);
        }
        catch (FormatException exception)
        {
            Console.WriteLine($"Skipping {palaceGroup} map {map}. {exception.Message}");
            continue;
        }

        bool hasItem = false;
        int elevatorScreen = -1;
        try
        {
            SideviewEditable<PalaceObject> sideviewEditable = new(sideView);
            hasItem = sideviewEditable.HasItem();
            var elevator = sideviewEditable.Find(o => o.IsElevator());
            if (elevator != null) { elevatorScreen = elevator.AbsX / 16; }
        }
        catch (Exception)
        {
            Console.WriteLine("Malformed room breaks parsing. Group: " + palaceGroup + " Map: " + map);
        }

        var room = new Room
        {
            ItemPresenceBits = [vanillaRoomData.ReadItemBits(palaceGroup, map)],
            Connections = vanillaRoomData.ReadConnections(palaceGroup, map),
            ElevatorScreen = elevatorScreen,
            Enabled = true,
            Enemies = enemies,
            Group = RoomGroup.V5_0,
            Author = "",
            HasBoss = false,
            IsBossRoom = false,
            HasDrop = false,
            HasItem = hasItem,
            IsThunderBirdRoom = false,
            PalaceNumber = palaceGroup == PalaceGrouping.PalaceGp ? 7 : null,
            LinkedRoomName = null,
            IsDropZone = false,
            IsEntrance = false,
            Name = "",
            SideView = sideView,
        };
        sb.Append(room.Serialize() + ",");
    }
}
sb[sb.Length - 1] = ']';
var jsonElement = JsonSerializer.Deserialize<JsonElement>(sb.ToString());
JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true, };
string prettyPrintedJson = JsonSerializer.Serialize(jsonElement, options);
File.WriteAllText("Rooms.dump", prettyPrintedJson);

#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
