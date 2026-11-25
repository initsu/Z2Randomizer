using System.Text.Json.Nodes;
using Z2Randomizer.RandomizerCore.Sidescroll;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.Tests;

[TestClass]
public class YamlRoomMaskTests
{
    [TestMethod]
    [DataRow("0b1110", "0b1010")]
    [DataRow("14", "10")]
    public void MasksMatchTheSameJsonConnectionSlots(string itemBits, string connections)
    {
        var rooms = new List<RoomValidationInput>();
        var diagnostics = new List<RoomDiagnostic>();
        RoomFileReader.Read("room.yaml", $"""
            name: Example
            author: Test
            palaceNumber: null
            itembits: {itemBits}
            connections: {connections}
            enemies: "01"
            sideviewData: "04600000"
            """, rooms, diagnostics);
        RoomFileReader.Read("room.json", """
            {"name":"Example","author":"Test","group":"NONE","palaceNumber":null,"bitmask":"0E","connections":"00FC00FC","enemies":"01","sideviewData":"04600000"}
            """, rooms, diagnostics);

        Assert.IsEmpty(diagnostics, string.Join(Environment.NewLine, diagnostics));
        Assert.AreEqual(rooms[1].Room.Serialize(), rooms[0].Room.Serialize());
        Assert.AreEqual(RoomGroup.NONE, rooms[0].Room.Group);
    }

    [TestMethod]
    [DataRow("name", "InvalidRoomDefinition", "name")]
    [DataRow("author", "InvalidRoomDefinition", "author")]
    [DataRow("palaceNumber", "InvalidRoomDefinition", "palaceNumber")]
    [DataRow("sideviewData", "InvalidRoomDefinition", "sideviewData")]
    [DataRow("enemies", "InvalidRoomDefinition", "enemies")]
    [DataRow("connections", "InvalidConnections", "connection")]
    public void MissingRequiredFieldsReportAnError(string field, string errorId, string message)
    {
        var definition = JsonNode.Parse("""
            {"name":"Example","author":"Test","palaceNumber":null,"connections":"00FCFC00","enemies":"01","sideviewData":"04600F08"}
            """)!.AsObject();
        definition.Remove(field);
        var rooms = new List<RoomValidationInput>();
        var diagnostics = new List<RoomDiagnostic>();

        RoomFileReader.Read("room.json", definition.ToJsonString(), rooms, diagnostics);

        Assert.IsEmpty(rooms);
        var error = diagnostics.Single();
        Assert.AreEqual(errorId, error.Id);
        StringAssert.Contains(error.Message, message);
    }

    [TestMethod]
    [DataRow("itembits: 0b10000", "itembits")]
    [DataRow("itembits: 0b0120", "itembits")]
    [DataRow("itembits: 16", "itembits")]
    [DataRow("itembits: 0b0000\nbitmask: '00'", "itembits or bitmask")]
    [DataRow("group: VANILLA", "destination maps")]
    public void InvalidYamlMasksReportAnError(string roomFields, string expectedMessage)
    {
        var rooms = new List<RoomValidationInput>();
        var diagnostics = new List<RoomDiagnostic>();
        RoomFileReader.Read("room.yaml", $"connections: 0b1010\n{roomFields}", rooms, diagnostics);

        Assert.IsEmpty(rooms);
        StringAssert.Contains(diagnostics.Single().Message, expectedMessage);
    }
}
