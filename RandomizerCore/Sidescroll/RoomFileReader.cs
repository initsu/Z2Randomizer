using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
        ICollection<RoomDiagnostic> diagnostics)
    {
        switch (GetFormat(filename))
        {
            case RoomFileFormat.Json:
                ReadJson(filename, content, rooms, diagnostics);
                break;
            case RoomFileFormat.Yaml:
                ReadYaml(filename, content, rooms, diagnostics);
                break;
            default:
                diagnostics.Add(new("UnsupportedRoomFile", RoomDiagnosticSeverity.Error, filename, null,
                    null, false, "Room files must use the .json, .yaml, or .yml extension."));
                break;
        }
    }

    private static void ReadYaml(string filename, string yaml, ICollection<RoomValidationInput> rooms,
        ICollection<RoomDiagnostic> diagnostics)
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
        ReadJson(filename, json, rooms, diagnostics);
    }

    private static void ReadJson(string filename, string json, ICollection<RoomValidationInput> rooms,
        ICollection<RoomDiagnostic> diagnostics)
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
                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                JsonElement connections = default;
                foreach (var property in definition.EnumerateObject())
                {
                    if (!keys.Add(property.Name)) { throw new FormatException($"Duplicate room field '{property.Name}'."); }
                    switch (property.Name.ToLowerInvariant())
                    {
                        case "name" when property.Value.ValueKind == JsonValueKind.String:
                            name = property.Value.GetString();
                            break;
                        case "connections":
                            connections = property.Value;
                            break;
                        case "requirements":
                            CheckRequirements(property.Value, allowComplex: true);
                            break;
                    }
                }
                // Exit flags are derived during deserialization, so check this field before invoking it.
                if (connections.ValueKind != JsonValueKind.String || connections.GetString()!.Length != 8)
                {
                    Error("InvalidConnections", "Room must have four connection bytes encoded as a hexadecimal string.", index, name);
                    return;
                }
                var room = definition.Deserialize(RoomSerializationContext.Default.Room)!;
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
