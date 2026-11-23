using FluentAssertions;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Sidescroll;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;
using Random = Z2Randomizer.RandomizerCore.Primitives.Random;

namespace Z2Randomizer.Tests;

[TestClass]
public class RoomPoolTests
{
    Room CreateMockRoom(string name, RoomGroup group, bool enabled = true, bool hasItem = false,
        bool isBossRoom = false, bool isEntrance = false, bool isThunderBirdRoom = false,
        int? palaceNumber = null, string? linkedRoomName = null, string[]? tags = null,
        bool hasUpExit = false, bool hasDownExit = false, bool hasLeftExit = false,
        bool hasRightExit = false, bool hasDrop = false, bool isDropZone = false,
        byte map = 0, string duplicateGroup = "")
    {
        var connections = new byte[] {
            hasLeftExit ? (byte)0 : (byte)0xFC,
            hasDownExit ? (byte)0 : (byte)0xFC,
            hasUpExit ? (byte)0 : (byte)0xFC,
            hasRightExit ? (byte)0 : (byte)0xFC
        };

        var room = new Room
        {
            Name = name,
            Group = group,
            Enabled = enabled,
            HasItem = hasItem,
            IsBossRoom = isBossRoom,
            IsEntrance = isEntrance,
            IsThunderBirdRoom = isThunderBirdRoom,
            PalaceNumber = palaceNumber,
            LinkedRoomName = linkedRoomName,
            Tags = tags?.ToList() ?? [],
            HasDrop = hasDrop,
            IsDropZone = isDropZone,
            DuplicateGroup = duplicateGroup,
            SideView = [0x04, 0x60, 0x00, 0x08],
            Enemies = [0x01],
            ItemPresenceBits = [0x0F],
            Connections = connections,
            Map = map
        };
        room.OnDeserialized();
        return room;
    }

    RandomizerProperties CreateMockProps(bool allowVanilla = true, bool allowV4 = false,
        bool allowV5 = false, bool removeLongDeadEnds = false, bool includeExpert = true,
        bool blockersAnywhere = true, bool replaceFireWithDash = true,
        HashSet<Collectable>? removeItems = null)
    {
        return new RandomizerProperties
        {
            AllowVanillaRooms = allowVanilla,
            AllowV4Rooms = allowV4,
            AllowV5_0Rooms = allowV5,
            RemoveLongDeadEnds = removeLongDeadEnds,
            IncludeExpertRooms = includeExpert,
            BlockersAnywhere = blockersAnywhere,
            ReplaceFireWithDash = replaceFireWithDash,
            NoDuplicateRooms = false,
            NoDuplicateRoomsBySideview = false,
            KeyLogic = KeyLogicOption.FAIRY_OR_MAGICKEY,
            MarioMode = false,
            RoomPoolOption = RoomPoolOption.VANILLA,
            PalaceStyles = Enumerable.Repeat(PalaceStyle.VANILLA, 7).ToArray(),
            PalaceLengths = new int[7],
            RemoveItems = removeItems ?? []
        };
    }

    /// <summary>
    /// Creates PalaceRooms as required for FinalizePool
    /// </summary>
    PalaceRooms CreateMockPalaceRooms(List<Room>? vanillaRooms = null, List<Room>? v4Rooms = null, List<Room>? v5Rooms = null, int palaceNumber = 1)
    {
        var allRooms = new List<Room>();
        allRooms.AddRange(vanillaRooms ?? []);
        allRooms.AddRange([
            CreateMockRoom("VanillaDownEntrance", RoomGroup.VANILLA, isEntrance: true, palaceNumber: palaceNumber, hasDownExit: true),
            CreateMockRoom("VanillaP1Boss", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1, map: 13, hasLeftExit: true),
            CreateMockRoom("VanillaP1Item", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasRightExit: true),
        ]);
        allRooms.AddRange([
            CreateMockRoom("V4DownBoss", RoomGroup.V4_0, isBossRoom: true, hasDownExit: true, palaceNumber: palaceNumber >= 6 ? palaceNumber : null)
        ]);
        allRooms.AddRange(v4Rooms ?? []);
        allRooms.AddRange([
            CreateMockRoom("V5UpEntrance", RoomGroup.V5_0, isEntrance: true, palaceNumber: palaceNumber, hasUpExit: true)
        ]);
        allRooms.AddRange(v5Rooms ?? []);
        allRooms.AddRange([
            CreateMockRoom("StubDown", RoomGroup.STUBS, hasDownExit: true),
            CreateMockRoom("StubUp", RoomGroup.STUBS, hasUpExit: true)
        ]);
        return new PalaceRooms(allRooms);
    }

    [TestMethod]
    public void ShapeFirstPlacementTriesCustomGroupWithHigherPriorityFirst()
    {
        var builtIn = CreateMockRoom("Built-in", RoomGroup.V4_0, hasLeftExit: true);
        var custom = CreateMockRoom("Custom", RoomGroup.CUSTOM, hasLeftExit: true);
        var bundled = CreateMockPalaceRooms(v4Rooms: [builtIn]);
        var combined = new PalaceRooms(bundled, [.. bundled.RoomsInInputOrder, custom]);
        var spec = RoomPoolSpecDeserializer.FromString("""
            groups_include: [VANILLA, V4_0]
            groups:
            - name: CUSTOM
              priority: 1
            """);
        var props = CreateMockProps();
        var pool = new RoomPool(combined, 1, props, spec);

        var generator = new RoomPriorityTestGenerator();
        for (int seed = 0; seed < 8; seed++)
        {
            Assert.AreEqual("Custom", generator.PickLeftDeadEnd(pool, props, new Random(seed))?.Name);
        }
    }

    [TestMethod]
    public void PrioritySamplingFallsBackAfterHigherPriorityRoomsFail()
    {
        Room preferred = new() { Priority = 1 };
        Room fallback = new();
        Room[] candidates = [fallback, preferred];
        var random = new Random(42);

        Assert.AreSame(preferred, candidates.SampleByPriority(random));
        Assert.AreSame(fallback, candidates.SampleByPriority(random, [preferred]));
        Assert.IsNull(candidates.SampleByPriority(random, [preferred, fallback]));
    }

    [TestMethod]
    public void EntranceDirectionItemSelectionUsesPriorityAcrossDirections()
    {
        var preferred = CreateMockRoom("Preferred item", RoomGroup.CUSTOM, hasItem: true, hasLeftExit: true);
        var bundled = CreateMockPalaceRooms();
        var combined = new PalaceRooms(bundled, [.. bundled.RoomsInInputOrder, preferred]);
        var props = CreateMockProps();
        var pool = new RoomPool(combined, 1, props, new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA"],
            Groups = [new GroupOverride { Name = "CUSTOM", Priority = 1 }],
        });

        var strategy = new ByEntranceDirectionItemRoomSelectionStrategy();
        var items = strategy.SelectItemRooms(new Palace(1, false), pool, 2, true, new Random(42));
        CollectionAssert.AreEqual(new[] { "Preferred item", "VanillaP1Item" }, items.Select(room => room.Name).ToArray());
    }

    [TestMethod]
    [DataRow(true, "Preferred miniboss")]
    [DataRow(false, "Ordinary room")]
    public void ShapeFirstPlacementAppliesMinibossPreferenceBeforePriority(bool preferMiniBoss, string expectedRoom)
    {
        var ordinary = CreateMockRoom("Ordinary room", RoomGroup.V4_0, hasLeftExit: true);
        ordinary.Priority = 10;
        var miniboss = CreateMockRoom("Miniboss", RoomGroup.V4_0, hasLeftExit: true);
        miniboss.HasBoss = true;
        var preferredMiniboss = CreateMockRoom("Preferred miniboss", RoomGroup.V4_0, hasLeftExit: true);
        preferredMiniboss.HasBoss = true;
        preferredMiniboss.Priority = 1;
        var rooms = CreateMockPalaceRooms(v4Rooms: [ordinary, miniboss, preferredMiniboss]);
        var props = CreateMockProps();
        var pool = new RoomPool(rooms, 1, props, new RoomPoolSpec { GroupsInclude = ["VANILLA", "V4_0"] });
        var generator = new RoomPriorityTestGenerator(preferMiniBoss ? 1f : 0f);

        for (int seed = 0; seed < 8; seed++)
        {
            Assert.AreEqual(expectedRoom, generator.PickLeftDeadEnd(pool, props, new Random(seed))?.Name);
        }
    }

    private sealed class RoomPriorityTestGenerator(float? miniBossChance = null) : RandomWalkCoordinatePalaceGenerator
    {
        protected override float? GetMiniBossChance() => miniBossChance;

        internal Room? PickLeftDeadEnd(RoomPool pool, RandomizerProperties props, Random random) =>
            PickNonStubRoom(props, new Palace(1, false), pool, pool, random, [], Coord.Origin, RoomExitType.DEADEND_EXIT_LEFT, false);
    }

    #region Test Spec Presets
    [TestMethod]
    [DataRow(RoomPoolOption.VANILLA, 1)]
    [DataRow(RoomPoolOption.V5_0_NO_EXPERT, 3)]
    [DataRow(RoomPoolOption.V5_0, 3)]
    [DataRow(RoomPoolOption.V4_0, 2)]
    [DataRow(RoomPoolOption.SGL2026, 2)]
    public void BuiltInRoomPoolSurvivesFlagRoundTrip(RoomPoolOption option, int groupCount)
    {
        var config = new RandomizerConfiguration { NormalPalaceStyle = PalaceStyle.RECONSTRUCTED, RoomPoolOption = option };
        var decoded = new RandomizerConfiguration(config.SerializeFlags());

        Assert.AreEqual(option, decoded.RoomPoolOption);
        Assert.AreEqual(groupCount, RoomPoolConfigs.PotentialRoomPools(decoded.RoomPoolOption));
    }

    [TestMethod]
    public void Test_Presets()
    {
        var roomsJson = Util.ReadAllTextFromFile("PalaceRooms.json");
        var palaceRooms = new PalaceRooms(roomsJson, false);
        var roomPoolYaml = Util.ReadAllTextFromFile("CustomRoomPool.yaml");
        var roomPoolSpec = RoomPoolSpecDeserializer.FromString(roomPoolYaml);

        var props = CreateMockProps(blockersAnywhere: true);

        var specV4 = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA", "V4_0"]
        };
        var poolV4 = new RoomPool(palaceRooms, 1, props, specV4);

        poolV4.NormalRooms.Select(room => room.Name).Distinct().Count().Should().Be(poolV4.NormalRooms.Count);
        poolV4.ItemRooms.Should().Contain(r => r.Name == "rightOpenItemRooms M102");
        poolV4.ItemRoomsByDirection[Direction.NORTH].Keys().Should().Contain(r => r.Name == "rightOpenItemRooms M102");
        poolV4.ItemRoomsByDirection[Direction.EAST].Keys().Should().Contain(r => r.Name == "rightOpenItemRooms M102");
        poolV4.GetItemRoomsForShape(RoomExitType.NE_L).Should().Contain(r => r.Name == "rightOpenItemRooms M102");

        var specV4Minus = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA", "V4_0"],
            RoomsExclude = ["rightOpenItemRooms M102"],
        };
        var poolV4Minus = new RoomPool(palaceRooms, 1, props, specV4Minus);

        poolV4Minus.NormalRooms.Select(room => room.Name).Distinct().Count().Should().Be(poolV4.NormalRooms.Count);
        poolV4Minus.ItemRooms.Should().NotContain(r => r.Name == "rightOpenItemRooms M102");
        poolV4Minus.ItemRoomsByDirection[Direction.NORTH].Keys().Should().NotContain(r => r.Name == "rightOpenItemRooms M102");
        poolV4Minus.ItemRoomsByDirection[Direction.EAST].Keys().Should().NotContain(r => r.Name == "rightOpenItemRooms M102");
        poolV4Minus.GetItemRoomsForShape(RoomExitType.NE_L).Should().NotContain(r => r.Name == "rightOpenItemRooms M102");

        var specConditionalGroup = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA"],
            Groups = [
                new GroupOverride() { Name = "V4_0" },
                new GroupOverride() { Name = "V5_0" },
            ],
        };
        var poolConditionalGroup = new RoomPool(palaceRooms, 4, props, specConditionalGroup);
        poolConditionalGroup.NormalRooms.Select(room => room.Name).Distinct().Count().Should().Be(poolConditionalGroup.NormalRooms.Count);

        var specRoomsOverride = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA"],
            Groups = [
                new GroupOverride() { Name = "V4_0" },
            ],
            Rooms = [
                new RoomOverride() { Name = "triforceOfCourage M5", ItemPresenceBits = 0b1110 },
            ],
        };
        var poolRoomsOverride = new RoomPool(palaceRooms, 4, props, specRoomsOverride);
        poolRoomsOverride.NormalRooms.Select(room => room.Name).Distinct().Count().Should().Be(poolRoomsOverride.NormalRooms.Count);

        // try actual current cfg
        var poolForSpec = new RoomPool(palaceRooms, 4, props, roomPoolSpec);
        poolForSpec.NormalRooms.Select(room => room.Name).Distinct().Count().Should().Be(poolForSpec.NormalRooms.Count);
    }

    #endregion

    #region Constructor - Props-based

    [TestMethod]
    public void Constructor_Props_SplitsRoomsIntoCorrectCategories()
    {
        var normalRoom = CreateMockRoom("Normal1", RoomGroup.V4_0, palaceNumber: 1);
        var entranceRoom = CreateMockRoom("Entrance1", RoomGroup.V4_0, isEntrance: true, palaceNumber: 1);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.V4_0, isBossRoom: true, palaceNumber: 1);
        var itemRoom = CreateMockRoom("Item1", RoomGroup.V4_0, hasItem: true, palaceNumber: 1, hasUpExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([normalRoom, entranceRoom, bossRoom, itemRoom]);
        var props = CreateMockProps(allowVanilla: false, allowV4: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.NormalRooms.Should().Contain(r => r.Name == "Normal1");
        pool.Entrances.Should().Contain(r => r.Name == "Entrance1");
        pool.BossRooms.Should().Contain(r => r.Name == "Boss1");
        pool.ItemRooms.Should().Contain(r => r.Name == "Item1");
    }

    [TestMethod]
    public void Constructor_Props_IncludesTbirdRoomsForPalace7()
    {
        var tbirdRoom = CreateMockRoom("Tbird1", RoomGroup.VANILLA, isThunderBirdRoom: true, palaceNumber: 7);
        var bossRoom = CreateMockRoom("Boss7", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 7);

        var mockPalaceRooms = CreateMockPalaceRooms([tbirdRoom, bossRoom], palaceNumber: 7);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 7, props);

        pool.TbirdRooms.Should().ContainSingle().Which.Name.Should().Be("Tbird1");
    }

    [TestMethod]
    public void Constructor_Props_OnlyIncludesRoomsForMatchingPalace()
    {
        var p1Room = CreateMockRoom("P1Room", RoomGroup.VANILLA, palaceNumber: 1);
        var p2Room = CreateMockRoom("P2Room", RoomGroup.VANILLA, palaceNumber: 2);
        var genericRoom = CreateMockRoom("GenericRoom", RoomGroup.VANILLA);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([p1Room, p2Room, genericRoom, bossRoom]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.NormalRooms.Should().Contain(r => r.Name == "P1Room");
        pool.NormalRooms.Should().Contain(r => r.Name == "GenericRoom");
        pool.NormalRooms.Should().NotContain(r => r.Name == "P2Room");
    }

    [TestMethod]
    public void Constructor_Props_RespectsAllowV4Flag()
    {
        var v4Room = CreateMockRoom("V4Room", RoomGroup.V4_0, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([], [v4Room]);

        var propsWithV4 = CreateMockProps(allowVanilla: true, allowV4: true);
        var poolWithV4 = new RoomPool(mockPalaceRooms, 1, propsWithV4);
        poolWithV4.NormalRooms.Should().Contain(r => r.Name == "V4Room");

        var propsWithoutV4 = CreateMockProps(allowVanilla: true, allowV4: false);
        var poolWithoutV4 = new RoomPool(mockPalaceRooms, 1, propsWithoutV4);
        poolWithoutV4.NormalRooms.Should().NotContain(r => r.Name == "V4Room");
    }

    [TestMethod]
    public void Constructor_Props_RespectsAllowV5Flag()
    {
        var v5Room = CreateMockRoom("V5Room", RoomGroup.V5_0, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([], v5Rooms: [v5Room]);

        var propsWithV5 = CreateMockProps(allowVanilla: true, allowV5: true);
        var poolWithV5 = new RoomPool(mockPalaceRooms, 1, propsWithV5);
        poolWithV5.NormalRooms.Should().Contain(r => r.Name == "V5Room");

        var propsWithoutV5 = CreateMockProps(allowVanilla: true, allowV5: false);
        var poolWithoutV5 = new RoomPool(mockPalaceRooms, 1, propsWithoutV5);
        poolWithoutV5.NormalRooms.Should().NotContain(r => r.Name == "V5Room");
    }

    [TestMethod]
    public void Constructor_Props_RemovesLongDeadEndsWhenConfigured()
    {
        var longDeadEndRoom = CreateMockRoom("LongDeadEnd", RoomGroup.VANILLA, palaceNumber: 1, tags: ["LongDeadEnd"]);
        var normalRoom = CreateMockRoom("Normal1", RoomGroup.VANILLA, palaceNumber: 1);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([longDeadEndRoom, normalRoom, bossRoom]);
        var props = CreateMockProps(allowVanilla: true, removeLongDeadEnds: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.NormalRooms.Should().NotContain(r => r.Name == "LongDeadEnd");
        pool.NormalRooms.Should().Contain(r => r.Name == "Normal1");
    }

    [TestMethod]
    public void Constructor_Props_RemovesExpertRoomsWhenConfigured()
    {
        var expertRoom = CreateMockRoom("Expert1", RoomGroup.VANILLA, palaceNumber: 1, tags: ["Expert"]);
        var normalRoom = CreateMockRoom("Normal1", RoomGroup.VANILLA, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([expertRoom, normalRoom]);
        var props = CreateMockProps(allowVanilla: true, includeExpert: false);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.NormalRooms.Should().NotContain(r => r.Name == "Expert1");
        pool.NormalRooms.Should().Contain(r => r.Name == "Normal1");
    }

    [TestMethod]
    public void Constructor_Props_FallsBackToVanillaEntrancesWhenNoneInPool()
    {
        var entranceRoom = CreateMockRoom("VanillaEntrance", RoomGroup.VANILLA, isEntrance: true, palaceNumber: 1, hasDownExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([entranceRoom]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.Entrances.Should().Contain(r => r.Name == "VanillaEntrance");
    }

    #endregion

    #region Constructor - Spec-based

    [TestMethod]
    public void Constructor_Spec_IncludesSpecifiedGroups()
    {
        var vanillaRoom = CreateMockRoom("Vanilla1", RoomGroup.VANILLA, palaceNumber: 1);
        var v4Room = CreateMockRoom("V4Room", RoomGroup.V4_0, palaceNumber: 1);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([vanillaRoom, bossRoom], [v4Room]);
        var props = CreateMockProps();
        var spec = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA", "V4_0"]
        };
        var pool = new RoomPool(mockPalaceRooms, 1, props, spec);

        pool.NormalRooms.Should().Contain(r => r.Name == "Vanilla1");
        pool.NormalRooms.Should().Contain(r => r.Name == "V4Room");
    }

    [TestMethod]
    public void Constructor_Spec_ExcludesRoomsByTag()
    {
        var taggedRoom = CreateMockRoom("Tagged1", RoomGroup.VANILLA, palaceNumber: 1, tags: ["ExcludeMe"]);
        var normalRoom = CreateMockRoom("Normal1", RoomGroup.VANILLA, palaceNumber: 1);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([taggedRoom, normalRoom, bossRoom]);
        var props = CreateMockProps();
        var spec = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA"],
            TagsExclude = ["ExcludeMe"]
        };
        var pool = new RoomPool(mockPalaceRooms, 1, props, spec);

        pool.NormalRooms.Should().NotContain(r => r.Name == "Tagged1");
        pool.NormalRooms.Should().Contain(r => r.Name == "Normal1");
    }

    [TestMethod]
    public void Constructor_Spec_ExcludesRoomsByName()
    {
        var excludedRoom = CreateMockRoom("ExcludedRoom", RoomGroup.VANILLA, palaceNumber: 1);
        var normalRoom = CreateMockRoom("Normal1", RoomGroup.VANILLA, palaceNumber: 1);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([excludedRoom, normalRoom, bossRoom]);
        var props = CreateMockProps();
        var spec = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA"],
            RoomsExclude = ["ExcludedRoom"]
        };
        var pool = new RoomPool(mockPalaceRooms, 1, props, spec);

        pool.NormalRooms.Should().NotContain(r => r.Name == "ExcludedRoom");
        pool.NormalRooms.Should().Contain(r => r.Name == "Normal1");
    }

    [TestMethod]
    public void Constructor_Spec_IncludesRoomsByTag()
    {
        var taggedRoom = CreateMockRoom("Tagged1", RoomGroup.VANILLA, palaceNumber: 1, tags: ["IncludeMe"]);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([taggedRoom, bossRoom]);
        var props = CreateMockProps();
        var spec = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA"],
            TagsInclude = ["IncludeMe"]
        };
        var pool = new RoomPool(mockPalaceRooms, 1, props, spec);

        pool.NormalRooms.Should().Contain(r => r.Name == "Tagged1");
    }

    [TestMethod]
    public void Constructor_Spec_ThrowsOnUnrecognizedGroup()
    {
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([bossRoom]);
        var props = CreateMockProps();
        var spec = new RoomPoolSpec
        {
            GroupsInclude = ["INVALID_GROUP"]
        };
        var act = () => new RoomPool(mockPalaceRooms, 1, props, spec);
        act.Should().Throw<UserFacingException>()
            .WithMessage("*unrecognized group*");
    }

    [TestMethod]
    public void CopyForRoomPool_KeepsGreatPalaceRoomsAndLinkedSegments()
    {
        var regular = CreateMockRoom("Regular", RoomGroup.V4_0);
        var stub = CreateMockRoom("Stub", RoomGroup.STUBS, hasUpExit: true);
        var primary = CreateMockRoom("GP primary", RoomGroup.V4_0, palaceNumber: 7,
            linkedRoomName: "Secondary");
        var bundled = new PalaceRooms([regular, stub, primary]);
        var secondary = CreateMockRoom("Secondary", RoomGroup.NONE, enabled: false, linkedRoomName: primary.Name);
        var gpRoom = CreateMockRoom("GP room", RoomGroup.NONE, palaceNumber: 7);
        var combined = new PalaceRooms(bundled, [.. bundled.RoomsInInputOrder, secondary, gpRoom]);

        var copy = combined.CopyForRoomPool(7);

        copy.RoomsInInputOrder.Select(room => room.Name).Should().Equal("GP primary", "Secondary", "GP room");
        copy.BundledRooms.RoomsInInputOrder.Select(room => room.Name).Should().Equal("GP primary");
        copy.GetRoomByName(primary.Name).Should().BeSameAs(copy.BundledRooms.GetRoomByName(primary.Name));
        copy.GetRoomByName(primary.Name).Should().NotBeSameAs(primary);
        copy.GetRoomByName(secondary.Name)!.Enabled.Should().BeFalse();
    }

    [TestMethod]
    public void GeneratedPalacesUseStubsForTheirOwnSideviewFormat()
    {
        var rooms = new PalaceRooms(Util.ReadAllTextFromFile("PalaceRooms.json"), false);
        var props = CreateMockProps();
        var spec = RoomPoolConfigs.V5_0;

        var regularStubs = new RoomPool(rooms, 1, props, spec).DefaultStubsByDirection;
        var greatPalaceStubs = new RoomPool(rooms, 7, props, spec).DefaultStubsByDirection;
        regularStubs.Values.Select(stub => stub.Name).Should().BeEquivalentTo("Stub up elevator", "Stub down elevator");
        greatPalaceStubs.Values.Select(stub => stub.Name).Should().BeEquivalentTo("Stub up elevator GP", "Stub down elevator GP");
        greatPalaceStubs.Values.Should().OnlyContain(stub => stub.PalaceNumber == 7 && stub.IsStub);
        new VanillaRoomPool(rooms, 1, props).DefaultStubsByDirection.Should().HaveCount(2);
        new VanillaRoomPool(rooms, 7, props).DefaultStubsByDirection.Should().BeEmpty();
    }

    [TestMethod]
    public void Constructor_Spec_AcceptsNamedOverridesForOtherPalaces()
    {
        var palaceTwoRoom = CreateMockRoom("Palace 2 room", RoomGroup.VANILLA, palaceNumber: 2);
        var rooms = CreateMockPalaceRooms([palaceTwoRoom]);
        var spec = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA"],
            Rooms = [new RoomOverride { Name = palaceTwoRoom.Name, Priority = 5 }],
        };

        var pool = new RoomPool(rooms, 1, CreateMockProps(), spec);

        pool.NormalRooms.Should().NotContain(room => room.Name == palaceTwoRoom.Name);
        palaceTwoRoom.Priority.Should().Be(0);
    }

    [TestMethod]
    public void Constructor_Spec_ThrowsOnMissingRoomName()
    {
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([bossRoom]);
        var props = CreateMockProps();
        var spec = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA"],
            Rooms =
            [
                new RoomOverride
                {
                    Name = "NonExistentRoom",
                    Enabled = true,
                    Sideview = "",
                    Enemies = "",
                    ItemPresenceBits = null
                }
            ]
        };
        var act = () => new RoomPool(mockPalaceRooms, 1, props, spec);
        act.Should().Throw<UserFacingException>()
            .WithMessage("*not found*");
    }

    [TestMethod]
    public void Constructor_Spec_AddsIndividualRooms()
    {
        var addedRoom = CreateMockRoom("AddedRoom", RoomGroup.NONE, palaceNumber: 1);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([addedRoom, bossRoom]);
        var props = CreateMockProps();
        var spec = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA"],
            Rooms =
            [
                new RoomOverride
                {
                    Name = "AddedRoom",
                    Enabled = true,
                    Sideview = "",
                    Enemies = "",
                    ItemPresenceBits = null
                }
            ]
        };
        var pool = new RoomPool(mockPalaceRooms, 1, props, spec);

        pool.NormalRooms.Should().Contain(r => r.Name == "AddedRoom");
    }

    [TestMethod]
    public void Constructor_Spec_CanOverrideRoomProperties()
    {
        var overridableRoom = CreateMockRoom("Overridable", RoomGroup.NONE, palaceNumber: 1);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([overridableRoom, bossRoom]);
        var props = CreateMockProps();
        var spec = new RoomPoolSpec
        {
            GroupsInclude = ["VANILLA"],
            Rooms =
            [
                new RoomOverride
                {
                    Name = "Overridable",
                    Enabled = true,
                    Sideview = "",
                    Enemies = "00",
                    ItemPresenceBits = 5
                }
            ]
        };
        var pool = new RoomPool(mockPalaceRooms, 1, props, spec);

        var room = pool.NormalRooms.Find(r => r.Name == "Overridable");
        room.Should().NotBeNull();
        room!.ItemPresenceBits.Should().Equal([5]);
    }

    #endregion

    #region Copy constructor

    [TestMethod]
    public void CopyConstructor_CopiesAllCollections()
    {
        var normalRoom = CreateMockRoom("Normal1", RoomGroup.VANILLA, palaceNumber: 1);
        var entranceRoom = CreateMockRoom("Entrance1", RoomGroup.VANILLA, isEntrance: true, palaceNumber: 1);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);
        var tbirdRoom = CreateMockRoom("Tbird1", RoomGroup.VANILLA, isThunderBirdRoom: true, palaceNumber: 7);
        var itemRoom = CreateMockRoom("Item1", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasUpExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([normalRoom, entranceRoom, bossRoom, tbirdRoom, itemRoom]);
        var props = CreateMockProps(allowVanilla: true);
        var original = new RoomPool(mockPalaceRooms, 1, props);

        var copy = new RoomPool(original);

        copy.TbirdRooms.Should().BeEmpty();
        copy.LinkedRooms.Should().BeEmpty();
        copy.NormalRooms.Should().BeEquivalentTo(original.NormalRooms);
        copy.Entrances.Should().BeEquivalentTo(original.Entrances);
        copy.BossRooms.Should().BeEquivalentTo(original.BossRooms);
        copy.TbirdRooms.Should().BeEquivalentTo(original.TbirdRooms);
        copy.ItemRooms.Should().BeEquivalentTo(original.ItemRooms);
        copy.VanillaBossRoom.Should().Be(original.VanillaBossRoom);
        copy.LinkedRooms.Should().BeEquivalentTo(original.LinkedRooms);
    }

    [TestMethod]
    public void CopyConstructor_ShallowClonesCollections()
    {
        var normalRoom = CreateMockRoom("Normal1", RoomGroup.VANILLA, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([normalRoom]);
        var props = CreateMockProps(allowVanilla: true);
        var original = new RoomPool(mockPalaceRooms, 1, props);
        var copy = new RoomPool(original);

        copy.NormalRooms.Should().NotBeSameAs(original.NormalRooms);
        copy.Entrances.Should().NotBeSameAs(original.Entrances);
        copy.BossRooms.Should().NotBeSameAs(original.BossRooms);
        copy.NormalRooms.First().Should().BeSameAs(original.NormalRooms.First());
        copy.BossRooms.First().Should().BeSameAs(original.BossRooms.First());
    }

    #endregion

    #region RemoveRoom / RemoveRooms

    [TestMethod]
    public void RemoveRoom_RemovesFromCorrectCategory()
    {
        var normalRoom = CreateMockRoom("Normal1", RoomGroup.VANILLA, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([normalRoom]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.RemoveRoom(normalRoom);

        pool.NormalRooms.Should().NotContain(normalRoom);
    }

    [TestMethod]
    public void RemoveRoom_RemovesItemRoomFromAllCollections()
    {
        var itemRoom = CreateMockRoom("Item1", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasUpExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([itemRoom]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.ItemRooms.Should().Contain(itemRoom);
        pool.RemoveRoom(itemRoom);
        pool.ItemRooms.Should().NotContain(itemRoom);
    }

    [TestMethod]
    public void RemoveRooms_WithPredicate_RemovesMatchingRooms()
    {
        var room1 = CreateMockRoom("Room1", RoomGroup.VANILLA, palaceNumber: 1, tags: ["Remove"]);
        var room2 = CreateMockRoom("Room2", RoomGroup.VANILLA, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([room1, room2]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.RemoveRooms(r => r.HasTag("Remove"));

        pool.NormalRooms.Should().NotContain(r => r.Name == "Room1");
        pool.NormalRooms.Should().Contain(r => r.Name == "Room2");
    }

    #endregion

    #region RemoveDuplicates

    [TestMethod]
    public void RemoveDuplicates_NoDuplicateRooms_RemovesSingleRoom()
    {
        var itemRoom1 = CreateMockRoom("Item1", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasUpExit: true);
        var itemRoom2 = CreateMockRoom("Item2", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasUpExit: true);
        var bossRoom = CreateMockRoom("Boss1", RoomGroup.VANILLA, isBossRoom: true, palaceNumber: 1);

        var mockPalaceRooms = CreateMockPalaceRooms([itemRoom1, itemRoom2, bossRoom]);
        var props = CreateMockProps(allowVanilla: true);
        props.NoDuplicateRooms = true;
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.ItemRooms.Should().Contain(itemRoom1);
        pool.RemoveDuplicates(props, itemRoom1);
        pool.ItemRooms.Should().NotContain(itemRoom1);
        pool.ItemRooms.Should().Contain(itemRoom2);
    }

    [TestMethod]
    public void RemoveDuplicates_NoDuplicateRoomsBySideview_RemovesMatchingSideview()
    {
        var itemRoom1 = CreateMockRoom("ItemVariant1", RoomGroup.V4_0, duplicateGroup: "ItemVariantGroup1", hasItem: true, palaceNumber: 1, hasUpExit: true);
        var itemRoom2 = CreateMockRoom("ItemVariant2", RoomGroup.V4_0, duplicateGroup: "ItemVariantGroup1", hasItem: true, palaceNumber: 1, hasUpExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([itemRoom1, itemRoom2]);
        var props = CreateMockProps(allowVanilla: false, allowV4: true);
        props.NoDuplicateRoomsBySideview = true;
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.ItemRooms.Should().HaveCount(2);
        pool.RemoveDuplicates(props, itemRoom1);
        pool.ItemRooms.Should().BeEmpty();
    }

    #endregion

    #region LinkedRooms

    [TestMethod]
    public void Constructor_GathersLinkedRooms()
    {
        var primaryRoom = CreateMockRoom("Primary", RoomGroup.VANILLA, palaceNumber: 1,
            linkedRoomName: "Secondary");
        var secondaryRoom = CreateMockRoom("Secondary", RoomGroup.VANILLA, palaceNumber: 1,
            linkedRoomName: "Primary", enabled: false);

        var mockPalaceRooms = CreateMockPalaceRooms([primaryRoom, secondaryRoom]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.LinkedRooms.Should().ContainKey("Primary");
        pool.LinkedRooms.Should().ContainKey("Secondary");
        pool.NormalRooms.Should().Contain(primaryRoom);
        pool.NormalRooms.Should().NotContain(secondaryRoom);
    }

    #endregion

    #region CategorizeNormalRoomExits / GetNormalRoomsForExitType

    [TestMethod]
    public void CategorizeNormalRoomExits_ReturnsCategorizedRooms()
    {
        var deadendRight = CreateMockRoom("DeadendRight", RoomGroup.VANILLA, palaceNumber: 1, hasRightExit: true);
        var deadendLeft = CreateMockRoom("DeadendLeft", RoomGroup.VANILLA, palaceNumber: 1, hasLeftExit: true);
        var passThrough = CreateMockRoom("PassThrough", RoomGroup.VANILLA, palaceNumber: 1, hasLeftExit: true, hasRightExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([deadendRight, deadendLeft, passThrough]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        var categorized = pool.CategorizeNormalRoomExits();

        categorized[RoomExitType.DEADEND_EXIT_RIGHT].Should().ContainSingle().Which.Name.Should().Be("DeadendRight");
        categorized[RoomExitType.DEADEND_EXIT_LEFT].Should().ContainSingle().Which.Name.Should().Be("DeadendLeft");
        categorized[RoomExitType.HORIZONTAL_PASSTHROUGH].Should().ContainSingle().Which.Name.Should().Be("PassThrough");
    }

    [TestMethod]
    public void GetNormalRoomsForExitType_ReturnsMatchingRooms()
    {
        var deadendRight = CreateMockRoom("DeadendRight", RoomGroup.VANILLA, palaceNumber: 1, hasRightExit: true);
        var deadendLeft = CreateMockRoom("DeadendLeft", RoomGroup.VANILLA, palaceNumber: 1, hasLeftExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([deadendRight, deadendLeft]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        var rightRooms = pool.GetNormalRoomsForExitType(RoomExitType.DEADEND_EXIT_RIGHT);
        rightRooms.Should().ContainSingle().Which.Name.Should().Be("DeadendRight");

        var leftRooms = pool.GetNormalRoomsForExitType(RoomExitType.DEADEND_EXIT_LEFT);
        leftRooms.Should().ContainSingle().Which.Name.Should().Be("DeadendLeft");
    }

    #endregion

    #region GetItemRoomShapes / GetItemRoomsForShape

    [TestMethod]
    public void GetItemRoomShapes_ReturnsDistinctShapes()
    {
        var itemRight = CreateMockRoom("ItemRight", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasRightExit: true);
        var itemUp = CreateMockRoom("ItemUp", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasUpExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([itemRight, itemUp]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        var shapes = pool.GetItemRoomShapes().ToList();
        shapes.Should().Contain(RoomExitType.DEADEND_EXIT_RIGHT);
        shapes.Should().Contain(RoomExitType.DEADEND_EXIT_UP);
    }

    [TestMethod]
    public void GetItemRoomsForShape_ReturnsMatchingRooms()
    {
        var itemRight = CreateMockRoom("ItemRight", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasRightExit: true);
        var itemUp = CreateMockRoom("ItemUp", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasUpExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([itemRight, itemUp]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        var rightRooms = pool.GetItemRoomsForShape(RoomExitType.DEADEND_EXIT_RIGHT);
        rightRooms.Should().Contain(r => r.Name == "ItemRight");

        var upRooms = pool.GetItemRoomsForShape(RoomExitType.DEADEND_EXIT_UP);
        upRooms.Should().Contain(r => r.Name == "ItemUp");
    }

    #endregion

    #region GetMergedExitType

    [TestMethod]
    public void GetMergedExitType_ReturnsMergedTypeForLinkedRoom()
    {
        var primaryRoom = CreateMockRoom("Primary", RoomGroup.VANILLA, palaceNumber: 1,
            hasRightExit: true, linkedRoomName: "Secondary");
        var secondaryRoom = CreateMockRoom("Secondary", RoomGroup.VANILLA, palaceNumber: 1,
            hasLeftExit: true, linkedRoomName: "Primary", enabled: false);

        var mockPalaceRooms = CreateMockPalaceRooms([primaryRoom, secondaryRoom]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        var mergedType = pool.GetMergedExitType(primaryRoom);
        mergedType.Should().Be(RoomExitType.HORIZONTAL_PASSTHROUGH);
    }

    [TestMethod]
    public void GetMergedExitType_ReturnsOwnTypeWhenNoLinkedRoom()
    {
        var normalRoom = CreateMockRoom("Normal1", RoomGroup.VANILLA, palaceNumber: 1, hasRightExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([normalRoom]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        var exitType = pool.GetMergedExitType(normalRoom);
        exitType.Should().Be(RoomExitType.DEADEND_EXIT_RIGHT);
    }

    #endregion

    #region RefillNormalRoomsForExitType

    [TestMethod]
    public void RefillNormalRoomsForExitType_AddsRoomsFromSourcePool()
    {
        var room1 = CreateMockRoom("Room1", RoomGroup.VANILLA, palaceNumber: 1, hasRightExit: true);
        var room2 = CreateMockRoom("Room2", RoomGroup.VANILLA, palaceNumber: 1, hasRightExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([room1, room2]);
        var props = CreateMockProps(allowVanilla: true);
        var sourcePool = new RoomPool(mockPalaceRooms, 1, props);
        var targetPool = new RoomPool(mockPalaceRooms, 1, props);

        targetPool.NormalRooms.Clear();

        targetPool.RefillNormalRoomsForExitType(sourcePool, RoomExitType.DEADEND_EXIT_RIGHT);

        targetPool.NormalRooms.Should().HaveCount(2);
        targetPool.NormalRooms.Should().Contain(r => r.Name == "Room1");
        targetPool.NormalRooms.Should().Contain(r => r.Name == "Room2");
    }

    // Expose the real shape-filling path without generating a random layout or ROM.
    private sealed class RefillPlacementGenerator : TowerCoordinatePalaceGenerator
    {
        public Task<bool> FillForTest(RandomizerProperties props, Palace palace, RoomPool source, RoomPool working)
        {
            Dictionary<Coord, RoomExitType> shape = new()
            {
                [new Coord(1, 0)] = RoomExitType.HORIZONTAL_PASSTHROUGH,
                [new Coord(2, 0)] = RoomExitType.HORIZONTAL_PASSTHROUGH,
                [new Coord(3, 0)] = RoomExitType.HORIZONTAL_PASSTHROUGH,
            };
            return FillShape(props, palace, source, working, new Random(0), shape, []);
        }
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task RefilledTemplatesProduceIndependentPlacedRooms(bool transmute, bool preventBySideview)
    {
        var template = CreateMockRoom("Repeated", RoomGroup.VANILLA, palaceNumber: 1,
            hasLeftExit: true, hasRightExit: true);
        if (transmute)
        {
            template.SideView = [0x07, 0x60, 0x00, 0x08, 0x82, 0x0F, (byte)Collectable.BLUE_JAR];
        }
        else
        {
            template.ItemPresenceBits = [0];
        }
        var source = new RoomPool(CreateMockPalaceRooms([template]), 1, CreateMockProps());
        var working = new RoomPool(source);
        var props = CreateMockProps();
        props.NoDuplicateRooms = !preventBySideview;
        props.NoDuplicateRoomsBySideview = preventBySideview;
        var palace = new Palace(1, false) { Entrance = new Room(source.Entrances[0]) };
        var originalSideView = template.SideView.ToArray();
        var originalBits = template.ItemPresenceBits.ToArray();
        var originalEnemies = template.Enemies.ToArray();

        // Only one passthrough template exists. Duplicate prevention removes it
        // after each placement, so placing three requires two actual refills.
        Assert.IsTrue(await new RefillPlacementGenerator().FillForTest(props, palace, source, working));
        Assert.AreEqual(3, palace.AllRooms.Count);
        Assert.AreEqual(0, working.NormalRooms.Count);
        for (int i = 0; i < palace.AllRooms.Count; i++)
        {
            var placed = palace.AllRooms[i];
            Assert.AreEqual(template.Name, placed.Name);
            Assert.AreNotSame(template, placed);
            Assert.AreNotSame(template.SideView, placed.SideView);
            Assert.AreNotSame(template.ItemPresenceBits, placed.ItemPresenceBits);
            Assert.AreNotSame(template.Enemies, placed.Enemies);
            for (int j = 0; j < i; j++)
            {
                Assert.AreNotSame(palace.AllRooms[j], placed);
                Assert.AreNotSame(palace.AllRooms[j].SideView, placed.SideView);
                Assert.AreNotSame(palace.AllRooms[j].ItemPresenceBits, placed.ItemPresenceBits);
                Assert.AreNotSame(palace.AllRooms[j].Enemies, placed.Enemies);
            }
        }

        var edited = palace.AllRooms[0];
        Assert.IsTrue(transmute ? edited.TryTransmuteMinorItemToKey(new Random(0)) : edited.TryAddKeyOnFloor());
        edited.SetItemBit(0, !edited.GetItemBit(0));
        edited.Enemies[0] = 0;
        foreach (var untouched in palace.AllRooms.Skip(1).Append(template))
        {
            CollectionAssert.AreEqual(originalSideView, untouched.SideView);
            CollectionAssert.AreEqual(originalBits, untouched.ItemPresenceBits);
            CollectionAssert.AreEqual(originalEnemies, untouched.Enemies);
        }
    }

    #endregion

    #region ItemRoomsByDirection

    [TestMethod]
    public void FinalizePool_PopulatesItemRoomsByDirection()
    {
        var itemUp = CreateMockRoom("ItemUp", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasUpExit: true);
        var itemDown = CreateMockRoom("ItemDown", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasDownExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([itemUp, itemDown]);
        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.ItemRoomsByDirection.Should().ContainKey(Direction.NORTH);
        pool.ItemRoomsByDirection.Should().ContainKey(Direction.SOUTH);
        pool.ItemRoomsByDirection[Direction.NORTH].Keys().Should().Contain(itemUp);
        pool.ItemRoomsByDirection[Direction.SOUTH].Keys().Should().Contain(itemDown);
    }

    [TestMethod]
    public void RemoveRoom_RemovesFromItemRoomsByDirection()
    {
        var itemUp = CreateMockRoom("ItemUp", RoomGroup.VANILLA, hasItem: true, palaceNumber: 1, hasUpExit: true);

        var mockPalaceRooms = CreateMockPalaceRooms([itemUp]);

        var props = CreateMockProps(allowVanilla: true);
        var pool = new RoomPool(mockPalaceRooms, 1, props);

        pool.ItemRoomsByDirection.Should().ContainKey(Direction.NORTH);
        pool.ItemRoomsByDirection[Direction.NORTH].Keys().Should().Contain(itemUp);
        pool.RemoveRoom(itemUp);
        pool.ItemRoomsByDirection.Should().NotContainKey(Direction.NORTH);
    }

    #endregion
}
