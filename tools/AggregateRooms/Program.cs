using System.Text.Json;
using YamlDotNet.Core;
using Z2Randomizer.RandomizerCore.Sidescroll;

if (args.Length is < 2 or > 4)
{
    Console.Error.WriteLine("Usage: AggregateRooms <bundled-room-directory> <output-json> [field-order-file] [file-order-file]");
    return 1;
}

try
{
    var fieldOrder = ReadOrder(args.Length >= 3 ? args[2] : null, "RoomFieldOrder");
    var fileOrder = ReadOrder(args.Length == 4 ? args[3] : null, "RoomFileOrder");
    string[] sources = Directory.EnumerateFiles(args[0])
        .Where(path => Path.GetExtension(path).ToLowerInvariant() is ".json" or ".yaml" or ".yml")
        .OrderBy(path => fileOrder.GetValueOrDefault(Path.GetFileName(path), int.MaxValue))
        .ThenBy(path => path, StringComparer.Ordinal).ToArray();
    if (sources.Length == 0) { throw new FormatException("No bundled room files found."); }

    using var output = new MemoryStream();
    using (var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true }))
    {
        writer.WriteStartArray();
        // Keep each file's room order. Sorting by room name changes generation order and the room hash.
        foreach (string source in sources) { AppendFile(writer, source, fieldOrder); }
        writer.WriteEndArray();
    }

    string destination = Path.GetFullPath(args[1]);
    byte[] bytes = output.ToArray();
    if (File.Exists(destination) && File.ReadAllBytes(destination).AsSpan().SequenceEqual(bytes)) { return 0; }

    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
    try
    {
        File.WriteAllBytes(temporary, bytes);
        File.Move(temporary, destination, overwrite: true);
    }
    finally { File.Delete(temporary); }
    return 0;
}
catch (Exception exception) when (exception is FormatException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static Dictionary<string, int> ReadOrder(string? path, string resourceName)
{
    using var input = path == null
        ? typeof(JsonNodeContext).Assembly.GetManifestResourceStream(resourceName)!
        : File.OpenRead(path);
    using var reader = new StreamReader(input);
    var order = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    while (reader.ReadLine() is { } line)
    {
        string entry = line.Trim();
        if (entry.Length == 0) { continue; }
        if (!order.TryAdd(entry, order.Count))
        {
            throw new FormatException($"Duplicate entry '{entry}' in {path ?? resourceName}.");
        }
    }
    return order;
}

static void AppendFile(Utf8JsonWriter writer, string source, Dictionary<string, int> fieldOrder)
{
    int? roomIndex = null;
    try
    {
        bool yaml = Path.GetExtension(source).ToLowerInvariant() is ".yaml" or ".yml";
        string text = File.ReadAllText(source);
        using var document = JsonDocument.Parse(yaml ? RoomYamlReader.ToJson(text) : text);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            AppendRoom(writer, document.RootElement, yaml, fieldOrder);
            return;
        }
        roomIndex = 0;
        foreach (var definition in document.RootElement.EnumerateArray())
        {
            roomIndex++;
            AppendRoom(writer, definition, yaml, fieldOrder);
        }
    }
    catch (Exception exception) when (exception is FormatException or JsonException or YamlException)
    {
        string location = roomIndex.HasValue ? $"{source}, room {roomIndex}" : source;
        throw new FormatException($"{location}: {exception.Message}", exception);
    }
}

static void AppendRoom(Utf8JsonWriter writer, JsonElement definition, bool yaml, Dictionary<string, int> fieldOrder)
{
    var fields = RoomDefinitionFields.Read(definition);
    bool vanilla = RoomDefinitionFields.IsVanilla(fields);
    bool usesConnectionMask = false;
    var normalized = yaml ? RoomDefinitionFields.NormalizeYaml(definition, fields, out usesConnectionMask) : null;
    RoomDefinitionFields.CheckRequired(fields, vanilla);
    RoomDefinitionFields.ReadPalaceNumber(fields, vanilla);
    if (usesConnectionMask && vanilla)
    {
        throw new FormatException("Vanilla rooms need four connection bytes with destination maps.");
    }

    // Preserve the original field values, including vanilla ROM references, without materializing a Room.
    if (normalized != null) { fields = RoomDefinitionFields.Read(JsonSerializer.SerializeToElement(normalized, JsonNodeContext.Default.JsonObject)); }
    var connections = fields.GetValueOrDefault("connections");
    if ((!vanilla || connections.ValueKind != JsonValueKind.Undefined) &&
        (connections.ValueKind != JsonValueKind.String || connections.GetString() is not { Length: 8 }))
    {
        throw new FormatException("Room must have four connection bytes encoded as a hexadecimal string.");
    }

    if (fields.GetValueOrDefault("enabled").ValueKind == JsonValueKind.False)
    {
        var linkedRoom = fields.GetValueOrDefault("linkedRoomName");
        if (linkedRoom.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(linkedRoom.GetString())) { return; }
    }

    writer.WriteStartObject();
    foreach (var (field, value) in fields.OrderBy(field => fieldOrder.GetValueOrDefault(field.Key, int.MaxValue))
        .ThenBy(field => field.Key, StringComparer.OrdinalIgnoreCase))
    {
        // Non-vanilla definitions do not use ROM indexes.
        if (field.Equals("palaceGroup", StringComparison.OrdinalIgnoreCase) ||
            (!vanilla && field.Equals("map", StringComparison.OrdinalIgnoreCase))) { continue; }
        if (RoomDefinitionFields.IsDefaultValue(field, value, vanilla)) { continue; }
        writer.WritePropertyName(field);
        RoomJsonWriter.WriteValue(writer, value);
    }
    writer.WriteEndObject();
}

[System.Text.Json.Serialization.JsonSerializable(typeof(System.Text.Json.Nodes.JsonObject))]
internal partial class JsonNodeContext : System.Text.Json.Serialization.JsonSerializerContext;
