using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.RandomizerCore.Sidescroll;

internal static class RoomDefinitionFields
{
    internal const byte DefaultItemBits = 0xF;
    internal const int DefaultElevatorScreen = -1;
    internal const bool DefaultEnabled = true;
    internal const RoomGroup DefaultGroup = RoomGroup.NONE;
    internal const int DefaultPriority = 0;
    internal const string DefaultDuplicateGroup = "";

    // These are the values the Room loader restores when a bundled field is absent.
    // Vanilla item bits can come from the ROM.
    internal static bool IsDefaultValue(string field, JsonElement value, bool vanilla) =>
        field.ToLowerInvariant() switch
        {
            "enabled" => value.ValueKind == (DefaultEnabled ? JsonValueKind.True : JsonValueKind.False),
            "bitmask" => !vanilla && value.ValueKind == JsonValueKind.String &&
                string.Equals(value.GetString(), DefaultItemBits.ToString("X2"), StringComparison.OrdinalIgnoreCase),
            "group" => value.ValueKind switch
            {
                JsonValueKind.String => string.Equals(value.GetString(), DefaultGroup.ToString(), StringComparison.OrdinalIgnoreCase),
                JsonValueKind.Number => value.TryGetInt32(out int group) && group == (int)DefaultGroup,
                _ => false,
            },
            "elevatorscreen" => value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out int screen) && screen == DefaultElevatorScreen,
            "hasboss" or "hasdrop" or "hasdroppableelevator" or "hasitem" or "isbossroom" or "isdropzone" or
                "isentrance" or "isstub" or "isthunderbirdroom" or "isupdownreversed" => value.ValueKind == JsonValueKind.False,
            "linkedroomname" or "mirrorsideviewdata" or "map" =>
                value.ValueKind == JsonValueKind.Null,
            "requirements" or "mariorequirements" or "suppresswarning" or "tags" =>
                value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0,
            "duplicategroup" => value.ValueKind == JsonValueKind.String && value.GetString() == DefaultDuplicateGroup,
            "priority" => value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out int priority) && priority == DefaultPriority,
            _ => false,
        };

    internal static Dictionary<string, JsonElement> Read(JsonElement definition)
    {
        if (definition.ValueKind != JsonValueKind.Object) { throw new FormatException("Expected a room object."); }
        var fields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in definition.EnumerateObject())
        {
            if (!fields.TryAdd(property.Name, property.Value)) { throw new FormatException($"Duplicate room field '{property.Name}'."); }
        }
        return fields;
    }

    internal static bool IsVanilla(Dictionary<string, JsonElement> fields, RoomGroup? defaultGroup = null)
    {
        var group = fields.GetValueOrDefault("group");
        return group.ValueKind switch
        {
            JsonValueKind.String => string.Equals(group.GetString(), nameof(RoomGroup.VANILLA), StringComparison.OrdinalIgnoreCase),
            JsonValueKind.Number => group.TryGetInt32(out int value) && value == (int)RoomGroup.VANILLA,
            JsonValueKind.Undefined => defaultGroup == RoomGroup.VANILLA,
            _ => false,
        };
    }

    internal static int? ReadPalaceNumber(Dictionary<string, JsonElement> fields, bool vanilla)
    {
        var value = fields.GetValueOrDefault("palaceNumber");
        if (value.ValueKind == JsonValueKind.Null && !vanilla) { return null; }
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) && number is >= 1 and <= 7)
        {
            return number;
        }
        throw new FormatException(vanilla
            ? "A vanilla room needs its source palaceNumber (1–7)."
            : "Room needs a palaceNumber field. Use null for a shared regular-palace room or a number from 1–7.");
    }

    internal static void CheckRequired(Dictionary<string, JsonElement> fields, bool vanilla)
    {
        if (fields.GetValueOrDefault("name") is not { ValueKind: JsonValueKind.String } name ||
            string.IsNullOrWhiteSpace(name.GetString()))
        {
            throw new FormatException("Room needs a name.");
        }
        // Some existing rooms have no credited author and use an explicit empty string.
        if (fields.GetValueOrDefault("author").ValueKind != JsonValueKind.String)
        {
            throw new FormatException("Room needs an author field.");
        }
        CheckData("sideviewData");
        CheckData("enemies");

        void CheckData(string field)
        {
            var value = fields.GetValueOrDefault(field);
            if (vanilla && value.ValueKind == JsonValueKind.Undefined) { return; }
            if (value.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(value.GetString()))
            {
                throw new FormatException($"Room needs a {field} hexadecimal string.");
            }
        }
    }

    internal static JsonObject? NormalizeYaml(JsonElement definition, Dictionary<string, JsonElement> fields, out bool usesConnectionMask)
    {
        JsonObject? normalized = null;
        JsonObject Normalize() => normalized ??= ParseObject(definition);
        var connections = fields.GetValueOrDefault("connections");
        usesConnectionMask = connections.ValueKind == JsonValueKind.Number ||
            (connections.ValueKind == JsonValueKind.String && connections.GetString()!.StartsWith("0b", StringComparison.Ordinal));
        if (usesConnectionMask)
        {
            byte mask = ReadFourBits(connections, "connections");
            var exits = new byte[4];
            for (int index = 0; index < exits.Length; index++)
            {
                exits[index] = (mask & (0b1000 >> index)) == 0 ? (byte)0xFC : (byte)0x00;
            }
            Normalize()["connections"] = Convert.ToHexString(exits);
        }
        if (fields.TryGetValue("itembits", out var itemBits))
        {
            if (fields.ContainsKey("bitmask")) { throw new FormatException("Specify itembits or bitmask once per room."); }
            Normalize().Remove("itembits");
            Normalize()["bitmask"] = ReadFourBits(itemBits, "itembits").ToString("X2");
        }
        return normalized;
    }

    internal static JsonObject ParseObject(JsonElement definition) =>
        JsonNode.Parse(definition.GetRawText(), new JsonNodeOptions { PropertyNameCaseInsensitive = true })!.AsObject();

    private static byte ReadFourBits(JsonElement value, string field)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetByte(out byte bits) && bits <= 0xF)
        {
            return bits;
        }
        if (value.ValueKind == JsonValueKind.String && value.GetString() is { Length: 6 } binary &&
            binary.StartsWith("0b", StringComparison.Ordinal))
        {
            byte mask = 0;
            foreach (char digit in binary.AsSpan(2))
            {
                if (digit is not ('0' or '1')) { throw InvalidMask(); }
                mask = (byte)((mask << 1) | (digit - '0'));
            }
            return mask;
        }
        throw InvalidMask();

        FormatException InvalidMask() => new($"{field} must be a four-bit mask written as 0b0000 through 0b1111 or an integer from 0 to 15.");
    }
}
