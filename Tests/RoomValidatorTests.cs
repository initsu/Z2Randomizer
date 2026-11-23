using System.Text.Json;
using Z2Randomizer.RandomizerCore.Sidescroll;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.Tests;

[TestClass]
public class RoomValidatorTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Rooms", name + ".json"));

    private static Room ReadRoom(string name) =>
        JsonSerializer.Deserialize(ReadFixture(name), RoomSerializationContext.Default.Room)!;

    [TestMethod]
    public void ReportsMalformedRoomsAndContinuesWithValidRooms()
    {
        var badConnections = ReadRoom("regular");
        badConnections.Connections = [0, 0xFC];
        badConnections.Enabled = false;
        badConnections.SuppressWarning.Add("InvalidConnections");
        var laterRoom = ReadRoom("suppressed");
        laterRoom.SuppressWarning.Clear();

        var diagnostics = RoomValidator.Validate([
            new(badConnections, "connections.yaml", 2),
            new(ReadRoom("malformed-sideview"), "sideview.json"),
            new(ReadRoom("malformed-enemies"), "enemies.json"),
            new(laterRoom, "later.json"),
        ]);

        CollectionAssert.AreEqual(new[] { "InvalidConnections", "InvalidSideview", "InvalidEnemies" },
            diagnostics.Where(d => d.Severity == RoomDiagnosticSeverity.Error).Select(d => d.Id).ToArray());
        var connection = diagnostics.Single(d => d.Id == "InvalidConnections");
        Assert.AreEqual("connections.yaml", connection.Source);
        Assert.AreEqual(2, connection.RoomIndex);
        Assert.AreEqual(badConnections.Name, connection.RoomName);
        Assert.IsTrue(diagnostics.Any(d => d.Source == "later.json" && d.Id == "MultipleExitElevators"));
    }

    [TestMethod]
    public void MirrorWarningsUseOriginalSourceAndSuppression()
    {
        var room = ReadRoom("regular");
        room.SideView = [4, 0x40, 0x0F, 0x08]; // Three pages.
        room.Enemies = [1];
        room.ElevatorScreen = -1;
        room.SetConnections([0, 0xFC, 0xFC, 0]); // Both sides: the warning also applies after mirroring.
        RoomValidationInput[] inputs = [new(room, "room.yml", 1)];

        var warnings = RoomValidator.Validate(inputs).Where(d => d.Id == "ThreePageRightExit").ToArray();
        CollectionAssert.AreEqual(new[] { false, true }, warnings.Select(d => d.IsMirrored).ToArray());
        Assert.IsTrue(warnings.All(d => d.Source == "room.yml" && d.RoomIndex == 1 && d.RoomName == room.Name));

        room.SuppressWarning.Add("ThreePageRightExit");
        Assert.IsFalse(RoomValidator.Validate(inputs).Any(d => d.Id == "ThreePageRightExit"));
    }

    [TestMethod]
    public void DisabledSecondaryIsCheckedWhenItsPrimaryIsEnabled()
    {
        var rooms = JsonSerializer.Deserialize(ReadFixture("segmented"), RoomSerializationContext.Default.ListRoom)!;
        var primary = rooms[0];
        var secondary = rooms[1];
        secondary.SetConnections([0xFC, 0xFC, 0, 0xFC]);
        secondary.ElevatorScreen = -1;
        RoomValidationInput[] inputs = [new(secondary, "secondary.json")];

        Assert.IsTrue(RoomValidator.Validate(inputs).Any(d => d.Id == "InvalidLinkedRoom"));
        var diagnostics = RoomValidator.Validate(inputs, [primary]);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == RoomDiagnosticSeverity.Error));
        Assert.IsTrue(diagnostics.Any(d => d.Id == "ElevatorMissing" && d.RoomName == secondary.Name));
    }

    [TestMethod]
    public void CatalogueContextResolvesNamesAndLayoutsWithoutReportingItsWarnings()
    {
        var room = ReadRoom("regular");
        var bundled = ReadRoom("regular");
        RoomValidationInput[] inputs = [new(room, "custom.json")];
        Assert.IsTrue(RoomValidator.Validate(inputs, [bundled]).Any(d => d.Id == "DuplicateRoomName"));

        bundled.Name = "Bundled room";
        bundled.DuplicateGroup = "";
        var context = RoomValidator.Validate(inputs, [bundled]);
        Assert.IsFalse(context.Any(d => d.Id == "DuplicateGroupMismatch"));
        Assert.IsTrue(context.All(d => d.Source == "custom.json" && d.RoomName == room.Name));

        room.DuplicateGroup = "";
        bundled.DuplicateGroup = "Shared layout";
        var first = RoomValidator.Validate(inputs, [bundled]);
        Assert.IsTrue(first.Any(d => d.Id == "DuplicateGroupMismatch"));
        CollectionAssert.AreEqual(first.ToArray(), RoomValidator.Validate(inputs, [bundled]).ToArray());
    }

    [TestMethod]
    public void SuggestedRepairLeavesLaterChecksAndInputsUnchanged()
    {
        var room = ReadRoom("regular");
        room.SideView = [8, 0x60, 0x0F, 0x08, 0xE4, 0, 0xF0, 0x50]; // Elevator starts at x=64.
        room.Tags.Add("NoMirror");
        var before = room.Serialize();
        RoomValidationInput[] inputs = [new(room, "overflow.json")];

        var first = RoomValidator.Validate(inputs);
        Assert.IsTrue(first.Any(d => d.Id == "PageOverflow" && d.Message.Contains("Fixed bytes:")));
        Assert.IsTrue(first.Any(d => d.Id == "ElevatorOutsideRoom"));
        Assert.IsFalse(first.Any(d => d.Severity == RoomDiagnosticSeverity.Error));
        Assert.AreEqual(before, room.Serialize());
        CollectionAssert.AreEqual(first.ToArray(), RoomValidator.Validate(inputs).ToArray());
    }

    [TestMethod]
    public void GreatPalaceChecksRetainLockedDoorRequirementWarning()
    {
        var room = ReadRoom("great-palace");
        room.SideView = [6, 0x60, 0x0F, 0x48, 0x85, (byte)PalaceObjectShared.LOCKED_DOOR];
        room.Enemies = [1];

        Assert.IsTrue(RoomValidator.Validate([new(room, "gp.json")])
            .Any(d => d.Id == "LockedDoorNotInRequirements" && !d.IsMirrored));
    }

    [TestMethod]
    public void UnsafeEnemyCountBlocksWhenTheRoomCanBeUsed()
    {
        var room = ReadRoom("regular");
        room.Enemies = new byte[25]; // Length header and twelve enemy records.
        room.Enemies[0] = 25;
        room.Enabled = false;
        room.SuppressWarning.Add("UnsafeEnemyCount");
        RoomValidationInput[] inputs = [new(room, "enemies.json")];

        Assert.AreEqual(0, RoomValidator.Validate(inputs).Count);
        room.Enabled = true;
        var diagnostic = RoomValidator.Validate(inputs).Single();
        Assert.AreEqual("UnsafeEnemyCount", diagnostic.Id);
        Assert.AreEqual(RoomDiagnosticSeverity.Error, diagnostic.Severity);
    }
}
