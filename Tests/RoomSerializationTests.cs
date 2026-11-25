using System.Diagnostics;
using System.Text.Json;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Sidescroll;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.Tests;

[TestClass]
public class RoomSerializationTests
{
    [TestMethod]
    public void OmittedOptionalFieldsMatchExplicitDefaultsInBothFormats()
    {
        const string explicitDefaults = """
            {
                "name": "Example",
                "author": "Test",
                "connections": "00FCFC00",
                "sideviewData": "04600F08",
                "enemies": "01",
                "enabled": true,
                "group": "NONE",
                "bitmask": "0F",
                "elevatorScreen": -1,
                "hasBoss": false,
                "hasDrop": false,
                "hasDroppableElevator": false,
                "hasItem": false,
                "isBossRoom": false,
                "isDropZone": false,
                "isEntrance": false,
                "isStub": false,
                "isThunderBirdRoom": false,
                "isUpDownReversed": false,
                "priority": 0,
                "duplicateGroup": "",
                "requirements": [],
                "suppressWarning": [],
                "tags": [],
                "linkedRoomName": null,
                "mirrorSideviewData": null,
                "palaceNumber": null,
                "map": null
            }
            """;
        const string minimalJson = """
            {"name":"Example","author":"Test","palaceNumber":null,"connections":"00FCFC00","sideviewData":"04600F08","enemies":"01"}
            """;
        const string minimalYaml = """
            name: Example
            author: Test
            palaceNumber: null
            connections: 0b1001
            sideviewData: "04600F08"
            enemies: "01"
            """;

        using var document = JsonDocument.Parse(explicitDefaults);
        var fields = RoomDefinitionFields.Read(document.RootElement);
        CollectionAssert.AreEquivalent(
            new[] { "name", "author", "palaceNumber", "connections", "sideviewData", "enemies" },
            fields.Where(field => !RoomDefinitionFields.IsDefaultValue(field.Key, field.Value, vanilla: false))
                .Select(field => field.Key).ToArray());

        var rooms = new List<RoomValidationInput>();
        var diagnostics = new List<RoomDiagnostic>();
        RoomFileReader.Read("explicit.json", explicitDefaults, rooms, diagnostics);
        RoomFileReader.Read("minimal.json", minimalJson, rooms, diagnostics);
        RoomFileReader.Read("minimal.yaml", minimalYaml, rooms, diagnostics);
        Assert.IsEmpty(diagnostics, string.Join(Environment.NewLine, diagnostics));

        var expected = rooms[0].Room;
        foreach (var input in rooms.Skip(1))
        {
            Assert.AreEqual(expected.GetDeterministicHash(), input.Room.GetDeterministicHash(), input.Source);
            Assert.AreEqual(expected.Serialize(), input.Room.Serialize(), input.Source);
        }
    }

    [TestMethod]
    public void CopyPreservesRoomDataAndKeepsMutableFieldsSeparate()
    {
        var source = new Room
        {
            Name = "Room to copy",
            SideView = [4, 0x60, 0x0F, 0x08],
            Enemies = [1],
            NewEnemies = [1],
            ItemPresenceBits = [0x0F],
            IsUpDownReversed = true,
            Priority = 7,
            Tags = ["Custom"],
            SuppressWarning = ["ElevatorMissing"],
            Left = new Room(),
            LinkRequirements = new Requirements([RequirementType.JUMP]),
        };
        source.SetConnections([0, 0, 0xFC, 0xFC]);
        source.SetEffectiveRequirements(new Requirements([RequirementType.TANOOKI]), isMario: true);

        var copy = new Room(source);

        Assert.AreEqual(source.GetDeterministicHash(), copy.GetDeterministicHash());
        Assert.AreEqual(7, copy.Priority);
        Assert.IsTrue(copy.GetEffectiveRequirements(isMario: true).Contains(RequirementType.TANOOKI));
        Assert.IsTrue(copy.HasUpExit);
        Assert.IsFalse(copy.HasDownExit);
        Assert.IsNull(copy.Left);
        CollectionAssert.AreEqual(new byte[1], copy.NewEnemies);

        copy.Connections[1] = 0xFC;
        copy.Tags.Clear();
        copy.SuppressWarning.Clear();
        Assert.AreEqual((byte)0, source.Connections[1]);
        CollectionAssert.AreEqual(new[] { "Custom" }, source.Tags);
        CollectionAssert.AreEqual(new[] { "ElevatorMissing" }, source.SuppressWarning);
    }

    [TestMethod]
    public void TestSerialization()
    {
        string roomJson = """
{
    "name": "testName",
    "enabled": true,
    "group": "vanilla",
    "author": "palace1vanilla",
    "map": 4,
    "connections": "0F000214",
    "enemies": "050F0B00CB",
    "sideviewData": "3A600E08D208420022C8420022C8420022C8420022C8420022C8420022C84200D40E07F1F050B071D708420022C8420022C8420022C84200D20E",
    "bitmask": "0F",
    "isFairyBlocked": false,
    "isGloveBlocked": false,
    "isDownstabBlocked": false,
    "isUpstabBlocked": false,
    "isJumpBlocked": false,
    "hasItem": false,
    "hasBoss": false,
    "hasDrop": false,
    "elevatorScreen": 2,
    "memoryAddress": 67387,
    "IsUpDownReversed": false,
    "isDropZone": false
}
""";

        Room room = new Room(roomJson);
        String serialized = room.Serialize();
        Debug.WriteLine(serialized);
        room = new Room(serialized);
    }

    [TestMethod]
    public void TestDeserialization()
    {
        //string roomJson = "{\"name\": \"testName\", \"enabled\": true, \"group\": \"palace1vanilla\", \"map\": 4, \"connections\": \"0F000214\", \"enemies\": \"050F0B00CB\", \"sideviewData\": \"3A600E08D208420022C8420022C8420022C8420022C8420022C8420022C84200D40E07F1F050B071D708420022C8420022C8420022C84200D20E\", \"bitmask\": \"0F\", \"isFairyBlocked\": false, \"isGloveBlocked\": false, \"isDownstabBlocked\": false, \"isUpstabBlocked\": false, \"isJumpBlocked\": false, \"hasItem\": false, \"hasBoss\": false, \"hasDrop\": false, \"elevatorScreen\": 2, \"memoryAddress\": \"01073B\", \"IsUpDownReversed\": false, \"isDropZone\": false}";
        string roomJson = """
            {
            "bitmask": "0F",
            "connections": "0F000214",
            "elevatorScreen": 2,
            "enabled": true,
            "enemies": "050F0B00CB",
            "group": "VANILLA", 
            "author": "vanilla",
            "hasBoss": false,
            "hasDrop": false,
            "hasItem": false,
            "isThunderBirdRoom": false,
            "isEntrance": false,
            "palaceNumber": null,
            "linkedRoomName": null,
            "isDropZone": false,
            "isEntrance": false,
            "IsUpDownReversed": false,
            "map": 4,
            "memoryAddress": 67387,
            "name": "testName",
            "requirements":
                [
                ],
            "sideviewData": "3A600E08D208420022C8420022C8420022C8420022C8420022C8420022C84200D40E07F1F050B071D708420022C8420022C8420022C84200D20E"
            }
            """;

        Room room = new Room(roomJson);
        Assert.AreEqual("testName", room.Name);
    }

    [TestMethod]
    public void TestBulkDeserialization()
    {
        //Deserialization has changed and so this test is now outdated.
        //TODO: Replace this with a proper test.
        /*
        Dictionary<RoomGroup, List<Room>> roomsByGroup = new();
        string roomsJson = File.ReadAllText("PalaceRooms.json");
        dynamic rooms = JsonConvert.DeserializeObject(roomsJson)!;
        foreach (var obj in rooms)
        {
            Room room = new(obj.ToString());
            if (!roomsByGroup.ContainsKey(room.Group))
            {
                roomsByGroup.Add(room.Group, new List<Room>());
            }
            roomsByGroup[room.Group].Add(room);
        }
        */
    }

    public class StandardByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x == null)
            {
                return y == null;
            }
            if (y == null)
            {
                return false;
            }
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(byte[] room)
        {
            int result = 17;
            for (int i = 0; i < room.Length; i++)
            {
                unchecked
                {
                    result = result * 23 + room[i];
                }
            }
            return result;
        }
    }

    [TestMethod]
    public void RoomsContainsNoDuplcateRooms()
    {
        //TODO: REIMPLEMENT THIS
        /*
        bool useCustomRooms = false;
        List<Room> rooms = new List<Room>();
        rooms.AddRange(PalaceRooms.Palace1Vanilla(useCustomRooms));
        rooms.AddRange(PalaceRooms.Palace2Vanilla(useCustomRooms));
        rooms.AddRange(PalaceRooms.Palace3Vanilla(useCustomRooms));
        rooms.AddRange(PalaceRooms.Palace4Vanilla(useCustomRooms));
        rooms.AddRange(PalaceRooms.Palace5Vanilla(useCustomRooms));
        rooms.AddRange(PalaceRooms.Palace6Vanilla(useCustomRooms));
        rooms.AddRange(PalaceRooms.RoomJamGTM(useCustomRooms));
        rooms.AddRange(PalaceRooms.DMInPalaces(useCustomRooms));
        rooms.AddRange(PalaceRooms.WinterSolstice(useCustomRooms));
        rooms.AddRange(PalaceRooms.MaxRoomJam(useCustomRooms));
        rooms.AddRange(PalaceRooms.DusterRoomJam(useCustomRooms));
        rooms.AddRange(PalaceRooms.AaronRoomJam(useCustomRooms));
        rooms.AddRange(PalaceRooms.KnightcrawlerRoomJam(useCustomRooms));
        rooms.AddRange(PalaceRooms.TriforceOfCourage(useCustomRooms));
        rooms.AddRange(PalaceRooms.BenthicKing(useCustomRooms));
        rooms.AddRange(PalaceRooms.EasternShadow(useCustomRooms));
        rooms.AddRange(PalaceRooms.EunosRooms(useCustomRooms));

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = 0; j < rooms.Count; j++)
            {
                if(i != j)
                {
                    Assert.IsFalse(Util.byteArrayEqualityComparer.Equals(rooms[i].SideView, rooms[j].SideView)
                        && Util.byteArrayEqualityComparer.Equals(rooms[i].Enemies, rooms[j].Enemies)
                        && !(rooms[i].Group.EndsWith("vanilla") && rooms[j].Group.EndsWith("vanilla"))
                        , "Room# " + i + " (" + Util.ByteArrayToHexString(rooms[i].SideView) + "/" + Util.ByteArrayToHexString(rooms[i].Enemies) + ") " +
                        " is a duplicate of Room# " + j + " (" + Util.ByteArrayToHexString(rooms[j].SideView) + "/" + Util.ByteArrayToHexString(rooms[j].Enemies) + ")");
                }
            }
        }
        */
    }
}
