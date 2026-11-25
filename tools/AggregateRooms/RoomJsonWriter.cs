using System.Text.Json;

internal static class RoomJsonWriter
{
    internal static void WriteValue(Utf8JsonWriter writer, JsonElement value)
    {
        if (IsScalar(value)) { value.WriteTo(writer); }
        else { writer.WriteRawValue(FormatValue(value, writer.CurrentDepth)); }
    }

    // Write each collection as a complete value because raw array entries are not indented by Utf8JsonWriter.
    private static string FormatValue(JsonElement value, int depth, bool inlineRoutes = false)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            var elements = value.EnumerateArray();
            if (IsSimpleArray(value))
            {
                return $"[{string.Join(", ", elements.Select(element => element.GetRawText()))}]";
            }
            return FormatMultiline('[', ']', depth,
                elements.Select(element => FormatValue(element, depth + 1, inlineRoutes)));
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            var properties = value.EnumerateObject();
            if (!properties.Any()) { return "{}"; }
            var fields = properties.Select(property => $"\"{JsonEncodedText.Encode(property.Name)}\": " +
                FormatValue(property.Value, depth + 1, property.Name.Equals("routes", StringComparison.OrdinalIgnoreCase)));
            if (inlineRoutes && properties.All(property => IsScalar(property.Value) || IsSimpleArray(property.Value)))
            {
                return $"{{{string.Join(", ", fields)}}}";
            }
            return FormatMultiline('{', '}', depth, fields);
        }

        return value.GetRawText();
    }

    private static string FormatMultiline(char open, char close, int depth, IEnumerable<string> values)
    {
        string indent = new(' ', depth * 2);
        string childIndent = indent + "  ";
        return $"{open}\n{childIndent}{string.Join(",\n" + childIndent, values)}\n{indent}{close}";
    }

    private static bool IsScalar(JsonElement value) => value.ValueKind is not (JsonValueKind.Array or JsonValueKind.Object);

    private static bool IsSimpleArray(JsonElement value) =>
        value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(IsScalar);
}
