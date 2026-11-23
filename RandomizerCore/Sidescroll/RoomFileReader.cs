using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.Core;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.RandomizerCore.Sidescroll;

public static class RoomFileReader
{
    private enum RoomFileFormat { Unsupported, Json, Yaml }

    private static RoomFileFormat GetFormat(string filename) => Path.GetExtension(filename).ToLowerInvariant() switch
    {
        ".json" => RoomFileFormat.Json,
        ".yaml" or ".yml" => RoomFileFormat.Yaml,
        _ => RoomFileFormat.Unsupported,
    };

    public static bool IsSupported(string filename) => GetFormat(filename) != RoomFileFormat.Unsupported;

    public static void Read(string filename, string content, ICollection<RoomValidationInput> rooms,
        ICollection<RoomDiagnostic> diagnostics, VanillaRoomData? vanillaRoomData = null)
    {
        switch (GetFormat(filename))
        {
            case RoomFileFormat.Json:
                ReadJson(filename, content, rooms, diagnostics, vanillaRoomData);
                break;
            case RoomFileFormat.Yaml:
                ReadYaml(filename, content, rooms, diagnostics, vanillaRoomData);
                break;
            default:
                diagnostics.Add(new("UnsupportedRoomFile", RoomDiagnosticSeverity.Error, filename, null,
                    null, false, "Room files must use the .json, .yaml, or .yml extension."));
                break;
        }
    }

    private static void ReadYaml(string filename, string yaml, ICollection<RoomValidationInput> rooms,
        ICollection<RoomDiagnostic> diagnostics, VanillaRoomData? vanillaRoomData)
    {
        string json;
        try
        {
            // Normalize YAML to the same schema used by the generated room deserializer.
            json = RoomYamlReader.ToJson(yaml);
        }
        catch (YamlException exception)
        {
            diagnostics.Add(new("InvalidRoomFile", RoomDiagnosticSeverity.Error, filename, null, null, false,
                $"YAML line {exception.Start.Line}, column {exception.Start.Column}: {exception.Message}"));
            return;
        }
        catch (FormatException exception)
        {
            diagnostics.Add(new("InvalidRoomFile", RoomDiagnosticSeverity.Error, filename, null, null, false, exception.Message));
            return;
        }
        ReadJson(filename, json, rooms, diagnostics, vanillaRoomData, fromYaml: true);
    }

    private static void ReadJson(string filename, string json, ICollection<RoomValidationInput> rooms,
        ICollection<RoomDiagnostic> diagnostics, VanillaRoomData? vanillaRoomData, bool fromYaml = false)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (var definition in root.EnumerateArray()) { ReadRoom(definition, ++index); }
            }
            else { ReadRoom(root, null); }
        }
        catch (JsonException exception)
        {
            Error("InvalidRoomFile", exception.Message);
        }

        void ReadRoom(JsonElement definition, int? index)
        {
            string? name = null;
            try
            {
                if (definition.ValueKind != JsonValueKind.Object) { throw new FormatException("Expected a room object."); }
                var fields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                JsonObject? normalized = null;
                string? connections = null;
                byte? itemBits = null;
                bool usesConnectionMask = false;
                JsonObject Normalize() => normalized ??= JsonNode.Parse(definition.GetRawText())!.AsObject();
                foreach (var property in definition.EnumerateObject())
                {
                    if (!fields.TryAdd(property.Name, property.Value)) { throw new FormatException($"Duplicate room field '{property.Name}'."); }
                    switch (property.Name.ToLowerInvariant())
                    {
                        case "name" when property.Value.ValueKind == JsonValueKind.String:
                            name = property.Value.GetString();
                            break;
                        case "connections":
                            if (fromYaml && IsConnectionMask(property.Value))
                            {
                                usesConnectionMask = true;
                                byte mask = ReadFourBits(property.Value, "connections");
                                var exits = new byte[4];
                                for (int i = 0; i < exits.Length; i++)
                                {
                                    exits[i] = (mask & (0b1000 >> i)) == 0 ? (byte)0xFC : (byte)0x00;
                                }
                                connections = Convert.ToHexString(exits);
                                Normalize()[property.Name] = connections;
                            }
                            else if (property.Value.ValueKind == JsonValueKind.String)
                            {
                                connections = property.Value.GetString();
                            }
                            break;
                        case "itembits" when fromYaml:
                            itemBits = ReadFourBits(property.Value, "itembits");
                            Normalize().Remove(property.Name);
                            break;
                        case "requirements":
                            CheckRequirements(property.Value, allowComplex: true);
                            break;
                    }
                }
                if (itemBits is byte itemMask)
                {
                    if (fields.ContainsKey("bitmask")) { throw new FormatException("Specify itembits or bitmask once per room."); }
                    Normalize()["bitmask"] = itemMask.ToString("X2");
                }
                JsonObject? resolved = ResolveVanillaFields(definition, fields, vanillaRoomData, normalized, usesConnectionMask);
                if (resolved != null && !fields.ContainsKey("connections"))
                {
                    connections = resolved["connections"]!.GetValue<string>();
                }
                // Exit flags are derived during deserialization, so check this field before invoking it.
                if (connections is not { Length: 8 })
                {
                    Error("InvalidConnections", fromYaml
                        ? "Room connections must be a four-bit mask such as 0b1010 or a four-byte hexadecimal string."
                        : "Room must have four connection bytes encoded as a hexadecimal string.", index, name);
                    return;
                }
                var room = resolved == null
                    ? definition.Deserialize(RoomSerializationContext.Default.Room)!
                    : resolved.Deserialize(RoomSerializationContext.Default.Room)!;
                rooms.Add(new(room, filename, index));
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException or FormatException
                or InvalidOperationException or KeyNotFoundException)
            {
                Error("InvalidRoomDefinition", exception.Message, index, name);
            }
        }

        void Error(string id, string message, int? index = null, string? name = null) =>
            diagnostics.Add(new(id, RoomDiagnosticSeverity.Error, filename, index, name, false, message));
    }

    private static JsonObject? ResolveVanillaFields(JsonElement definition, Dictionary<string, JsonElement> fields,
        VanillaRoomData? vanillaRoomData, JsonObject? normalized, bool usesConnectionMask)
    {
        var group = fields.GetValueOrDefault("group");
        bool isVanilla = group.ValueKind switch
        {
            JsonValueKind.String => string.Equals(group.GetString(), nameof(RoomGroup.VANILLA), StringComparison.OrdinalIgnoreCase),
            JsonValueKind.Number => group.TryGetInt32(out int value) && value == (int)RoomGroup.VANILLA,
            JsonValueKind.Undefined => fields.ContainsKey("palaceGroup") && fields.ContainsKey("map"),
            _ => false,
        };
        if (!isVanilla) { return normalized; }
        if (usesConnectionMask) { throw new FormatException("Vanilla rooms need four connection bytes with destination maps."); }
        bool hasItemBits = fields.ContainsKey("bitmask") || normalized?.ContainsKey("bitmask") == true;
        if (fields.ContainsKey("connections") && fields.ContainsKey("sideviewData") &&
            fields.ContainsKey("enemies") && hasItemBits) { return normalized; }

        var groupNumber = fields.GetValueOrDefault("palaceGroup");
        var mapNumber = fields.GetValueOrDefault("map");
        if (groupNumber.ValueKind != JsonValueKind.Number || !groupNumber.TryGetInt32(out int number) || number is < 1 or > 3 ||
            mapNumber.ValueKind != JsonValueKind.Number || !mapNumber.TryGetByte(out byte map) || map > VanillaRoomData.LastMap)
        {
            throw new FormatException($"A vanilla ROM reference needs palaceGroup 1–3 and map 0–{VanillaRoomData.LastMap}.");
        }
        if (vanillaRoomData == null) { throw new FormatException("A base ROM is required to load this vanilla room."); }

        var sourceGroup = (PalaceGrouping)(number - 1);
        var resolved = normalized ?? JsonNode.Parse(definition.GetRawText())!.AsObject();
        if (!fields.ContainsKey("connections")) { resolved["connections"] = Convert.ToHexString(vanillaRoomData.ReadConnections(sourceGroup, map)); }
        if (!fields.ContainsKey("sideviewData")) { resolved["sideviewData"] = Convert.ToHexString(vanillaRoomData.ReadSideview(sourceGroup, map)); }
        if (!fields.ContainsKey("enemies")) { resolved["enemies"] = Convert.ToHexString(vanillaRoomData.ReadEnemies(sourceGroup, map)); }
        if (!hasItemBits) { resolved["bitmask"] = vanillaRoomData.ReadItemBits(sourceGroup, map).ToString("X2"); }
        return resolved;
    }

    private static bool IsConnectionMask(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number) { return true; }
        if (value.ValueKind != JsonValueKind.String) { return false; }
        string text = value.GetString()!;
        // Legacy four-byte hex can start with 0B. Binary masks use the 0b prefix.
        return text.StartsWith("0b", StringComparison.Ordinal);
    }

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

    private static void CheckRequirements(JsonElement requirements, bool allowComplex)
    {
        if (requirements.ValueKind != JsonValueKind.Array) { throw new FormatException("Room requirements must be an array."); }
        foreach (var requirement in requirements.EnumerateArray())
        {
            switch (requirement.ValueKind)
            {
                case JsonValueKind.String:
                    CheckRequirementName(requirement);
                    break;
                case JsonValueKind.Array:
                    foreach (var member in requirement.EnumerateArray()) { CheckRequirementName(member); }
                    break;
                case JsonValueKind.Object when allowComplex:
                    if (!requirement.TryGetProperty("requires", out var alternatives))
                    {
                        throw new FormatException("A complex requirement must contain a 'requires' array.");
                    }
                    CheckRequirements(alternatives, allowComplex: false);
                    break;
                default:
                    throw new FormatException("Expected a requirement name, a list of names, or a complex requirement.");
            }
        }
    }

    private static void CheckRequirementName(JsonElement name)
    {
        if (name.ValueKind != JsonValueKind.String || !Enum.TryParse<RequirementType>(name.GetString(), true, out var type)
            || !Enum.IsDefined(type) || !string.Equals(name.GetString(), type.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException($"Unrecognized requirement {name.GetRawText()}.");
        }
    }
}
