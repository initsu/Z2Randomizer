using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Z2Randomizer.RandomizerCore.Sidescroll;

// Convert the supported YAML subset to the same schema consumed by generated JSON metadata.
internal static class RoomYamlReader
{
    internal static string ToJson(string yaml)
    {
        var parser = new Parser(new StringReader(yaml));
        parser.Consume<StreamStart>();
        parser.Consume<DocumentStart>();
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output))
        {
            WriteNode(parser, writer, 0);
        }
        parser.Consume<DocumentEnd>();
        if (!parser.Accept<StreamEnd>(out _))
        {
            throw new FormatException("Room files must contain exactly one YAML document.");
        }
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static void WriteNode(IParser parser, Utf8JsonWriter writer, int depth)
    {
        if (depth > 64)
        {
            throw new FormatException("Room YAML is nested too deeply.");
        }
        if (parser.Current is NodeEvent node && (!node.Anchor.IsEmpty || !node.Tag.IsEmpty))
        {
            throw new FormatException("Room YAML does not support anchors or explicit tags.");
        }

        switch (parser.Current)
        {
            case MappingStart:
                parser.Consume<MappingStart>();
                writer.WriteStartObject();
                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (!parser.Accept<MappingEnd>(out _))
                {
                    var key = parser.Consume<Scalar>();
                    if (!key.Anchor.IsEmpty || !key.Tag.IsEmpty)
                    {
                        throw new FormatException("Room YAML does not support anchors or explicit tags.");
                    }
                    if (!keys.Add(key.Value) || key.Value == "<<")
                    {
                        throw new FormatException("Room YAML requires unique keys and does not support merge keys.");
                    }
                    writer.WritePropertyName(key.Value);
                    WriteNode(parser, writer, depth + 1);
                }
                parser.Consume<MappingEnd>();
                writer.WriteEndObject();
                break;
            case SequenceStart:
                parser.Consume<SequenceStart>();
                writer.WriteStartArray();
                while (!parser.Accept<SequenceEnd>(out _))
                {
                    WriteNode(parser, writer, depth + 1);
                }
                parser.Consume<SequenceEnd>();
                writer.WriteEndArray();
                break;
            case Scalar scalar:
                parser.Consume<Scalar>();
                WriteScalar(writer, scalar);
                break;
            default:
                throw new FormatException("Expected a room YAML mapping, sequence, or scalar; aliases are unsupported.");
        }
    }

    private static void WriteScalar(Utf8JsonWriter writer, Scalar scalar)
    {
        // Only unquoted null/boolean/integer scalars are typed. Everything else stays a string,
        // including hex data, enum names, and YAML 1.1 words such as 'on'.
        if (scalar.Style == ScalarStyle.Plain)
        {
            switch (scalar.Value)
            {
                case "null":
                case "~":
                case "":
                    writer.WriteNullValue();
                    return;
                case "true":
                    writer.WriteBooleanValue(true);
                    return;
                case "false":
                    writer.WriteBooleanValue(false);
                    return;
            }
            if (long.TryParse(scalar.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
            {
                writer.WriteNumberValue(integer);
                return;
            }
        }
        writer.WriteStringValue(scalar.Value);
    }
}
