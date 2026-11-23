using Z2Randomizer.RandomizerCore.Sidescroll;

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
            group: NONE
            itembits: {itemBits}
            connections: {connections}
            enemies: "01"
            sideviewData: "04600000"
            """, rooms, diagnostics);
        RoomFileReader.Read("room.json", """
            {"group":"NONE","bitmask":"0E","connections":"00FC00FC","enemies":"01","sideviewData":"04600000"}
            """, rooms, diagnostics);

        Assert.IsEmpty(diagnostics, string.Join(Environment.NewLine, diagnostics));
        Assert.AreEqual(rooms[1].Room.Serialize(), rooms[0].Room.Serialize());
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
