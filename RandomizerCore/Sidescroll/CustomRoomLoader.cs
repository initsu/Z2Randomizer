using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.RandomizerCore.Sidescroll;

public sealed record CustomRoomLoadResult(PalaceRooms? Rooms, IReadOnlyList<RoomDiagnostic> Diagnostics)
{
    public string? LoadedRoomsHash { get; } = Rooms is { } rooms
        ? DeterministicHash.Combine(DeterministicHash.Seed,
            rooms.RoomsInInputOrder.OrderBy(room => room.Name, StringComparer.Ordinal)).ToString("x8")
        : null;

    public PalaceRooms RequireRooms() => Rooms ?? throw new UserFacingException("Custom Room Pool Issue",
        string.Join(Environment.NewLine, Diagnostics.Where(d => d.Severity == RoomDiagnosticSeverity.Error)));
}

public static class CustomRoomLoader
{
    public const string SpecFilename = "CustomRoomPool.yaml";

    public static async Task<CustomRoomLoadResult> LoadAsync(PalaceRooms bundledRooms, RoomPoolSpec spec,
        string folder, Func<Task<IEnumerable<string>>> listFiles, Func<string, Task<string>> readFile,
        CancellationToken cancellationToken = default, VanillaRoomData? vanillaRoomData = null)
    {
        var inputs = new List<RoomValidationInput>();
        var diagnostics = new List<RoomDiagnostic>();
        string[] filenames;
        try
        {
            filenames = (await listFiles()).Where(RoomFileReader.IsSupported).Order(StringComparer.Ordinal).ToArray();
        }
        catch (DirectoryNotFoundException) { filenames = []; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            diagnostics.Add(new("UnreadableRoomFolder", RoomDiagnosticSeverity.Error, folder, null, null, false, exception.Message));
            filenames = [];
        }
        foreach (var filename in filenames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                RoomFileReader.Read(filename, await readFile(filename), inputs, diagnostics, vanillaRoomData, defaultGroup: RoomGroup.CUSTOM);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                diagnostics.Add(new("UnreadableRoomFile", RoomDiagnosticSeverity.Error, filename, null, null, false, exception.Message));
            }
        }
        cancellationToken.ThrowIfCancellationRequested();

        var originalDiagnostics = RoomValidator.Validate(inputs, bundledRooms.RoomsInInputOrder);
        var invalidNames = originalDiagnostics.Where(d => d.Severity == RoomDiagnosticSeverity.Error)
            .Select(d => d.RoomName).ToHashSet(StringComparer.Ordinal);
        var external = inputs.OrderBy(input => input.Room.Name, StringComparer.Ordinal).ToArray();
        var definitions = bundledRooms.RoomsInInputOrder.Concat(external.Select(input => input.Room)).ToArray();
        // An ambiguous external name cannot be used as an override target.
        var unambiguous = definitions.Where(room => room.Name != null && !invalidNames.Contains(room.Name))
            .ToDictionary(room => room.Name, StringComparer.Ordinal);
        var changed = ApplyOverrides(spec, unambiguous, diagnostics);
        var effectiveInputs = changed.Values.ToList();
        var externalByName = external.Where(input => input.Room.Name != null && !invalidNames.Contains(input.Room.Name))
            .ToDictionary(input => input.Room.Name, StringComparer.Ordinal);
        var effectiveNames = changed.Keys.ToHashSet(StringComparer.Ordinal);
        var bundledCompanions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var changedRoom in changed.Values)
        {
            if (changedRoom.Room.LinkedRoomName is not { } linkedName || !effectiveNames.Add(linkedName) ||
                !unambiguous.TryGetValue(linkedName, out var linked)) { continue; }
            if (externalByName.TryGetValue(linkedName, out var externalCompanion))
            {
                effectiveInputs.Add(externalCompanion);
            }
            else
            {
                bundledCompanions.Add(linkedName);
                effectiveInputs.Add(changedRoom with { Room = linked });
            }
        }
        // Validate against the effective catalogue so link and duplicate checks see the generation inputs.
        diagnostics.AddRange(originalDiagnostics.Where(d => d.Severity == RoomDiagnosticSeverity.Error || d.RoomName == null || !effectiveNames.Contains(d.RoomName)));
        var originalSuggestions = originalDiagnostics.Where(d => d.SuggestedField != null)
            .Select(d => (d.Source, d.RoomIndex, d.RoomName, d.Id, d.SuggestedField)).ToHashSet();
        diagnostics.AddRange(RoomValidator.Validate(effectiveInputs, unambiguous.Values.Where(room => !effectiveNames.Contains(room.Name)))
            .Where(d => d.Severity == RoomDiagnosticSeverity.Error || d.RoomName == null || !bundledCompanions.Contains(d.RoomName))
            .Select(d =>
            {
                if (d.Severity != RoomDiagnosticSeverity.Warning) { return d; }
                if (d.RoomName is not { } name || !externalByName.TryGetValue(name, out var source))
                {
                    return d with { SuggestedField = null };
                }
                // A correction to a room file must also be valid for the definition in that file.
                var suggestion = originalSuggestions.Contains((source.Source, source.RoomIndex, name, d.Id, d.SuggestedField))
                    ? d.SuggestedField : null;
                return d with { Source = source.Source, RoomIndex = source.RoomIndex, SuggestedField = suggestion };
            }));
        AddSuppressionLines(diagnostics, external);

        PalaceRooms? result = diagnostics.Any(d => d.Severity == RoomDiagnosticSeverity.Error)
            ? null : new PalaceRooms(bundledRooms, definitions.Select(room => new Room(room)));
        return new(result, diagnostics.AsReadOnly());
    }

    private static void AddSuppressionLines(List<RoomDiagnostic> diagnostics, IEnumerable<RoomValidationInput> inputs)
    {
        var sourceRooms = inputs.ToLookup(input => (input.Source, input.RoomIndex, RoomName: (string?)input.Room.Name));
        var warningsByRoom = diagnostics.Where(d => d.Severity == RoomDiagnosticSeverity.Warning)
            .ToLookup(d => (d.Source, d.RoomIndex, d.RoomName));
        for (int index = 0; index < diagnostics.Count; index++)
        {
            var diagnostic = diagnostics[index];
            if (diagnostic.Severity != RoomDiagnosticSeverity.Warning) { continue; }
            var key = (diagnostic.Source, diagnostic.RoomIndex, diagnostic.RoomName);
            var input = sourceRooms[key].FirstOrDefault();
            if (input == null) { continue; }
            string field = Path.GetExtension(diagnostic.Source).Equals(".json", StringComparison.OrdinalIgnoreCase)
                ? "\"suppressWarning\"" : "suppressWarning";
            var existing = input.Room.SuppressWarning ?? [];
            string FormatLine(IEnumerable<string> ids)
            {
                var entries = existing.Concat(ids).Distinct(StringComparer.Ordinal)
                    .Select(id => $"\"{JsonEncodedText.Encode(id)}\"");
                return $"{field}: [{string.Join(", ", entries)}]";
            }
            var individual = FormatLine([diagnostic.Id]);
            var all = FormatLine(warningsByRoom[key].Select(d => d.Id));
            diagnostics[index] = diagnostic with
            {
                SuppressionLine = individual,
                AllRoomWarningsSuppressionLine = all == individual ? null : all,
            };
        }
    }

    private static Dictionary<string, RoomValidationInput> ApplyOverrides(RoomPoolSpec spec,
        Dictionary<string, Room> rooms, List<RoomDiagnostic> diagnostics)
    {
        var changed = new Dictionary<string, RoomValidationInput>(StringComparer.Ordinal);
        var groupNames = (spec.GroupsInclude ?? []).Concat((spec.Groups ?? []).Select(group => group?.Name));
        foreach (var name in groupNames.Distinct(StringComparer.Ordinal))
        {
            if (!Enum.TryParse<RoomGroup>(name, out var group) || !Enum.IsDefined(group) || name != group.ToString())
            {
                Error("UnknownRoomGroup", $"Unrecognized room group '{name}'.");
            }
        }
        if ((spec.Tags ?? []).Any(tag => string.IsNullOrWhiteSpace(tag?.Name)))
        {
            Error("InvalidTagOverride", "Each tag override must name a tag.");
        }
        int index = 0;
        foreach (var rule in spec.Rooms ?? [])
        {
            index++;
            if (string.IsNullOrWhiteSpace(rule?.Name) || !rooms.TryGetValue(rule.Name, out var original))
            {
                Error("UnknownRoomOverride", "Override must name a room that exists exactly once and is structurally valid.", index, rule?.Name);
                continue;
            }

            var room = changed.TryGetValue(rule.Name, out var prior) ? prior.Room : new Room(original);
            try
            {
                rule.ApplyTo(room);
                // Priority-only changes cannot affect layout. Record all explicit changes so later
                // overrides see the same enabled state and bytes that RoomPool will use.
                if (prior != null || rule.Enabled != original.Enabled || !string.IsNullOrEmpty(rule.Sideview) ||
                    !string.IsNullOrEmpty(rule.Enemies) || rule.ItemPresenceBits.HasValue)
                {
                    changed[rule.Name] = new(room, SpecFilename, index);
                }
            }
            catch (Exception exception) when (exception is FormatException or OverflowException)
            {
                Error("InvalidRoomOverride", $"Invalid sideview, enemy, or itembits override. {exception.Message}", index, rule.Name);
            }
        }
        return changed;

        void Error(string id, string message, int? roomIndex = null, string? name = null) =>
            diagnostics.Add(new(id, RoomDiagnosticSeverity.Error, SpecFilename, roomIndex, name, false, message));
    }
}
