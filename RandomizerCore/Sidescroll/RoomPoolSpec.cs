using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Z2Randomizer.RandomizerCore.Sidescroll;

public static class RoomPoolConfigs
{
    public static RoomPoolSpec Vanilla = new()
    {
        Description = "Only Vanilla rooms.",
        GroupsInclude = ["VANILLA"],
    };

    public static RoomPoolSpec Beginner = new()
    {
        Description = "All vanilla and community rooms suitable for beginners.",
        GroupsInclude = ["VANILLA", "V4_0", "V5_0"],
        TagsExclude = ["Expert"],
    };

    public static RoomPoolSpec All = new()
    {
        Description = "All vanilla and community rooms are included.",
        GroupsInclude = ["VANILLA", "V4_0", "V5_0"],
    };

    public static RoomPoolSpec V4_0 = new()
    {
        Description = "Vanilla and v4.0 rooms only.",
        GroupsInclude = ["VANILLA", "V4_0"],
    };
}

public class RoomPoolSpec
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "Custom Room Pool";

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "version")]
    public int? Version { get; set; }

    [YamlMember(Alias = "groups-include")]
    public List<string>? GroupsInclude { get; set; }

    [YamlMember(Alias = "groups")]
    public List<GroupOverride>? Groups { get; set; }

    [YamlMember(Alias = "tags-include")]
    public List<string>? TagsInclude { get; set; }

    [YamlMember(Alias = "tags-exclude")]
    public List<string>? TagsExclude { get; set; }

    [YamlMember(Alias = "tags")]
    public List<GroupOverride>? Tags { get; set; }

    [YamlMember(Alias = "rooms")]
    public List<RoomOverride>? Rooms { get; set; }

    [YamlMember(Alias = "rooms-exclude")]
    public List<string>? RoomsExclude { get; set; }

    [YamlIgnore]
    private string? hash = null;
    [YamlIgnore]
    public string Hash {
        get {
            if (hash == null) { hash = GetHashCode().ToString("X8").ToLower(); }
            return hash;
        }
    }

    public static RoomPoolSpec FromString(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

        try
        {
            return deserializer.Deserialize<RoomPoolSpec>(yaml);
        }
        catch (YamlException ex)
        {
            throw new UserFacingException("Custom Room Pool Issue", $"YAML Parsing Error at Line {ex.Start.Line}, Column {ex.Start.Column}:\n{ex.Message}");
        }
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Groups, TagsInclude, TagsExclude, Rooms, RoomsExclude);
    }
}

public class GroupOverride
{
    [YamlMember(Alias = "name")]
    public required string Name { get; set; }

    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "priority")]
    public int? Priority { get; set; }

    [YamlMember(Alias = "condition")]
    public OverrideCondition? Condition { get; set; }

    public bool ConditionMatches(int palaceNumber, PalaceStyle palaceStyle)
    {
        if (Condition == null) { return true; }
        return Condition.Matches(palaceNumber, palaceStyle);
    }
}

public class OverrideCondition
{
    [YamlMember(Alias = "palace")]
    public List<int>? Palace { get; set; }

    [YamlMember(Alias = "not-palace")]
    public List<int>? NotPalace { get; set; }

    [YamlMember(Alias = "style")]
    public List<string>? Style { get; set; }

    [YamlMember(Alias = "not-style")]
    public List<string>? NotStyle { get; set; }

    /// <summary>
    /// Determines whether a palace matches the predefined `condition`.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the palace satisfies all configured filters;
    /// otherwise, <see langword="false"/>. If a filter is not configured, it is ignored.
    /// </returns>
    public bool Matches(int palaceNumber, PalaceStyle palaceStyle)
    {
        bool palaceNumberMatches = true;
        if (Palace is List<int> listMustIncludePalace)
        {
            palaceNumberMatches = listMustIncludePalace.Contains(palaceNumber);
        }
        else if (NotPalace is List<int> listMustNotIncludePalace)
        {
            palaceNumberMatches = !listMustNotIncludePalace.Contains(palaceNumber);
        }

        bool palaceStyleMatches = true;
        if (Style is List<string> listMustIncludeStyle)
        {
            palaceStyleMatches = listMustIncludeStyle.Any(s => String.Equals(palaceStyle.ToString(), s, StringComparison.OrdinalIgnoreCase));
        }
        else if (NotStyle is List<string> listMustNotIncludeStyle)
        {
            palaceStyleMatches = !listMustNotIncludeStyle.Any(s => String.Equals(palaceStyle.ToString(), s, StringComparison.OrdinalIgnoreCase));
        }

        return palaceNumberMatches && palaceStyleMatches;
    }
}

public class RoomOverride
{
    [YamlMember(Alias = "name")]
    public required string Name { get; set; }

    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "sideview")]
    public string? Sideview { get; set; }

    [YamlMember(Alias = "enemies")]
    public string? Enemies { get; set; }

    [YamlMember(Alias = "itembits")]
    public int? ItemBits { get; set; }

    [YamlMember(Alias = "priority")]
    public int? Priority { get; set; }

    //[YamlMember(Alias = "weight")]
    //public int? Weight { get; set; }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Enabled, Sideview, Enemies, ItemBits, Priority);
    }
}
