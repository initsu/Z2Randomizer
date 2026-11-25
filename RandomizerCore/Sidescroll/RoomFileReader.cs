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

    public static void Read(string filename, string content, ICollection<RoomValidationInput> rooms, ICollection<RoomDiagnostic> diagnostics, VanillaRoomData? vanillaRoomData = null, RoomGroup? defaultGroup = null)
    {
        switch (GetFormat(filename))
        {
            case RoomFileFormat.Json:
                ReadJson(filename, content, rooms, diagnostics, vanillaRoomData, defaultGroup);
                break;
            case RoomFileFormat.Yaml:
                ReadYaml(filename, content, rooms, diagnostics, vanillaRoomData, defaultGroup);
                break;
            default:
                diagnostics.Add(new("UnsupportedRoomFile", RoomDiagnosticSeverity.Error, filename, null,
                    null, false, "Room files must use the .json, .yaml, or .yml extension."));
                break;
        }
    }

    private static void ReadYaml(string filename, string yaml, ICollection<RoomValidationInput> rooms, ICollection<RoomDiagnostic> diagnostics, VanillaRoomData? vanillaRoomData, RoomGroup? defaultGroup)
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
        ReadJson(filename, json, rooms, diagnostics, vanillaRoomData, defaultGroup, fromYaml: true);
    }

    private static void ReadJson(string filename, string json, ICollection<RoomValidationInput> rooms, ICollection<RoomDiagnostic> diagnostics, VanillaRoomData? vanillaRoomData, RoomGroup? defaultGroup, bool fromYaml = false)
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
                var fields = RoomDefinitionFields.Read(definition);
                var nameField = fields.GetValueOrDefault("name");
                if (nameField.ValueKind == JsonValueKind.String) { name = nameField.GetString(); }
                if (fields.TryGetValue("requirements", out var requirements)) { CheckRequirements(requirements, allowComplex: true); }

                bool usesConnectionMask = false;
                JsonObject? normalized = fromYaml
                    ? RoomDefinitionFields.NormalizeYaml(definition, fields, out usesConnectionMask)
                    : null;
                bool isVanilla = RoomDefinitionFields.IsVanilla(fields, defaultGroup);
                if (!fields.ContainsKey("group") && defaultGroup is { } fallback)
                {
                    normalized ??= RoomDefinitionFields.ParseObject(definition);
                    normalized["group"] = fallback.ToString();
                }
                if (isVanilla && usesConnectionMask) { throw new FormatException("Vanilla rooms need four connection bytes with destination maps."); }
                var resolved = isVanilla
                    ? ResolveVanillaFields(definition, fields, vanillaRoomData, normalized,
                        RoomDefinitionFields.ReadPalaceNumber(fields, vanilla: true)!.Value)
                    : normalized;
                string? connections = null;
                if (resolved?["connections"] is JsonValue resolvedConnections)
                {
                    resolvedConnections.TryGetValue(out connections);
                }
                else if (fields.GetValueOrDefault("connections") is { ValueKind: JsonValueKind.String } value)
                {
                    connections = value.GetString();
                }
                // Exit flags are derived during deserialization, so check this field before invoking it.
                if (connections is not { Length: 8 })
                {
                    Error("InvalidConnections", fromYaml
                        ? "Room connections must be a four-bit mask such as 0b1010 or a four-byte hexadecimal string."
                        : "Room must have four connection bytes encoded as a hexadecimal string.", index, name);
                    return;
                }
                if (!isVanilla) { RoomDefinitionFields.ReadPalaceNumber(fields, vanilla: false); }
                RoomDefinitionFields.CheckRequired(fields, isVanilla);
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
        VanillaRoomData? vanillaRoomData, JsonObject? normalized, int palaceNumber)
    {
        bool hasItemBits = fields.ContainsKey("bitmask") || normalized?.ContainsKey("bitmask") == true;
        if (fields.ContainsKey("connections") && fields.ContainsKey("sideviewData") &&
            fields.ContainsKey("enemies") && hasItemBits) { return normalized; }

        var mapNumber = fields.GetValueOrDefault("map");
        if (mapNumber.ValueKind != JsonValueKind.Number || !mapNumber.TryGetByte(out byte map) || map > VanillaRoomData.LastMap)
        {
            throw new FormatException($"A vanilla ROM reference needs a map from 0–{VanillaRoomData.LastMap}.");
        }
        if (vanillaRoomData == null) { throw new FormatException("A base ROM is required to load this vanilla room."); }

        var sourceGroup = Util.AsPalaceGrouping(palaceNumber)!.Value;
        var resolved = normalized ?? RoomDefinitionFields.ParseObject(definition);
        if (!fields.ContainsKey("connections")) { resolved["connections"] = Convert.ToHexString(vanillaRoomData.ReadConnections(sourceGroup, map)); }
        if (!fields.ContainsKey("sideviewData")) { resolved["sideviewData"] = Convert.ToHexString(vanillaRoomData.ReadSideview(sourceGroup, map)); }
        if (!fields.ContainsKey("enemies")) { resolved["enemies"] = Convert.ToHexString(vanillaRoomData.ReadEnemies(sourceGroup, map)); }
        if (!hasItemBits) { resolved["bitmask"] = vanillaRoomData.ReadItemBits(sourceGroup, map).ToString("X2"); }
        return resolved;
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
