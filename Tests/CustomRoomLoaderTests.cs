using System.Text.Json.Nodes;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Sidescroll;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.Tests;

[TestClass]
public class CustomRoomLoaderTests
{
    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Rooms", name));

    private static PalaceRooms Bundled() =>
        TestRoomFiles.LoadBundledWithSyntheticRom();

    private static Task<CustomRoomLoadResult> Load(PalaceRooms bundled, RoomPoolSpec spec,
        Dictionary<string, string> files)
        => CustomRoomLoader.LoadAsync(bundled, spec, "Rooms", () => Task.FromResult<IEnumerable<string>>(files.Keys),
            name => Task.FromResult(files[name]));

    [TestMethod]
    public async Task MixedFormatsEnterTheSameCatalogueAndFollowConditionalPoolRules()
    {
        var spec = RoomPoolSpecDeserializer.FromString("""
            groups_include: [VANILLA]
            tags_include: [Local]
            tags:
              - name: Local
                enabled: false
                condition:
                  palace: [2]
            rooms:
              - name: Disabled local
                enabled: false
            """);
        var regular = JsonNode.Parse(Fixture("regular.json"))!.AsObject();
        regular["name"] = "Disabled local";
        regular["tags"] = JsonNode.Parse("""["Local"]""");
        var yaml = Fixture("regular.yaml")
            .Replace("aaron jam 4_4 P1_M1", "Selected local", StringComparison.Ordinal) + "\ntags: [Local]";

        var result = await Load(Bundled(), spec, new()
        {
            ["a.json"] = regular.ToJsonString(),
            ["z.YmL"] = yaml,
            ["preview.png"] = "ignored",
            ["README.md"] = "ignored",
        });
        var rooms = result.RequireRooms();
        Assert.IsNotNull(rooms.GetRoomByName("Disabled local"));
        Assert.IsNotNull(rooms.GetRoomByName("Selected local"));
        var props = new RandomizerProperties
        {
            PalaceStyles = Enumerable.Repeat(PalaceStyle.RECONSTRUCTED, 7).ToArray(),
            RemoveItems = [],
            BlockersAnywhere = true,
            ReplaceFireWithDash = true,
            KeyLogic = KeyLogicOption.FAIRY_OR_MAGICKEY,
            MarioMode = false,
        };
        var p1 = new RoomPool(rooms, 1, props, spec);
        var p2 = new RoomPool(rooms, 2, props, spec);
        Assert.IsTrue(p1.NormalRooms.Any(room => room.Name == "Selected local"));
        Assert.IsFalse(p1.NormalRooms.Any(room => room.Name == "Disabled local"));
        Assert.IsFalse(p2.NormalRooms.Any(room => room.Name == "Selected local"));
        Assert.IsTrue(rooms.GetRoomByName("Disabled local")!.Enabled);
    }

    [TestMethod]
    public async Task AggregatesInputAndOverrideErrorsBeforePublishingCatalogue()
    {
        var bundled = Bundled();
        var result = await Load(bundled, RoomPoolSpecDeserializer.FromString("""
            groups_include: [FakeGroup]
            rooms:
              - name: triforceOfCourage M5
                enemies: "03"
              - name: Missing room
            """), new()
        {
            ["bad.json"] = "!",
            ["malformed.json"] = Fixture("malformed-connections.json"),
            ["collision.yaml"] = Fixture("regular.yaml"),
        });
        var errors = result.Diagnostics.Where(d => d.Severity == RoomDiagnosticSeverity.Error).ToArray();

        Assert.IsNull(result.Rooms);
        CollectionAssert.AreEquivalent(new[] { "InvalidRoomFile", "InvalidConnections", "DuplicateRoomName",
            "UnknownRoomGroup", "UnknownRoomOverride", "InvalidEnemies" }, errors.Select(d => d.Id).ToArray());
        var effective = errors.Single(d => d.Id == "InvalidEnemies");
        Assert.AreEqual("CustomRoomPool.yaml", effective.Source);
        Assert.AreEqual("triforceOfCourage M5", effective.RoomName);
        Assert.AreNotEqual(1, bundled.GetRoomByName("triforceOfCourage M5")!.Enemies.Length);
    }

    [TestMethod]
    public async Task EmptyFolderPreservesBundledIdentityAndSuppressesBundledAdvisories()
    {
        var bundled = Bundled();
        var result = await CustomRoomLoader.LoadAsync(bundled, new RoomPoolSpec(), "Rooms",
            () => throw new DirectoryNotFoundException(), _ => throw new InvalidOperationException());
        Assert.AreEqual(bundled.Hash, result.RequireRooms().Hash);
        Assert.AreEqual(0, result.Diagnostics.Count);
    }

    [TestMethod]
    public async Task CustomPoolOverridesStayWithinTheMatchingPalaceAndPool()
    {
        var bundled = Bundled();
        var room = bundled.GetRoomByName("triforceOfCourage M5")!;
        var original = room.Serialize();
        int initialPriority = room.Priority;
        var spec = RoomPoolSpecDeserializer.FromString("""
            groups_include: [VANILLA, V4_0, V5_0]
            groups:
              - name: V5_0
                priority: 9
                condition:
                  palace: [1]
            rooms:
              - name: triforceOfCourage M5
                itembits: 3
            """);
        var result = await Load(bundled, spec, []);
        var catalogue = result.RequireRooms();
        var props = new RandomizerProperties
        {
            PalaceStyles = Enumerable.Repeat(PalaceStyle.RECONSTRUCTED, 7).ToArray(),
            RemoveItems = [],
            BlockersAnywhere = true,
            ReplaceFireWithDash = true,
            KeyLogic = KeyLogicOption.FAIRY_OR_MAGICKEY,
            MarioMode = false,
        };
        var first = new RoomPool(catalogue, 1, props, spec);
        var second = new RoomPool(catalogue, 2, props, spec);
        var firstRoom = first.NormalRooms.Concat(first.ItemRooms).Single(r => r.Name == room.Name);
        var secondRoom = second.NormalRooms.Concat(second.ItemRooms).Single(r => r.Name == room.Name);
        var firstPrioritized = first.NormalRooms.First(r => r.Group == RoomGroup.V5_0 && r.PalaceNumber == null);
        var secondEquivalent = second.NormalRooms.Single(r => r.Name == firstPrioritized.Name);
        Assert.AreEqual(9, firstPrioritized.Priority);
        Assert.AreEqual(catalogue.GetRoomByName(firstPrioritized.Name)!.Priority, secondEquivalent.Priority);
        Assert.AreEqual((byte)3, firstRoom.ItemPresenceBits[0]);
        firstRoom.ItemPresenceBits[0] = 0;
        Assert.AreEqual((byte)3, secondRoom.ItemPresenceBits[0]);
        Assert.AreEqual(initialPriority, catalogue.GetRoomByName(room.Name)!.Priority);
        Assert.AreEqual(original, catalogue.GetRoomByName(room.Name)!.Serialize());
        Assert.AreEqual(original, room.Serialize());
    }
}
