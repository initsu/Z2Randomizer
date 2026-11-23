using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace Z2Randomizer.RandomizerCore.Sidescroll;

public sealed record CustomRoomLoadResult(PalaceRooms? Rooms, IReadOnlyList<RoomDiagnostic> Diagnostics)
{
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
            try { RoomFileReader.Read(filename, await readFile(filename), inputs, diagnostics, vanillaRoomData); }
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
        foreach (var changedRoom in changed.Values)
        {
            if (changedRoom.Room.LinkedRoomName is not { } linkedName || !effectiveNames.Add(linkedName) ||
                !unambiguous.TryGetValue(linkedName, out var linked)) { continue; }
            effectiveInputs.Add(externalByName.GetValueOrDefault(linkedName) ?? changedRoom with { Room = linked });
        }
        // Validate against the effective catalogue so link and duplicate checks see the generation inputs.
        diagnostics.AddRange(originalDiagnostics.Where(d => d.Severity == RoomDiagnosticSeverity.Error ||
            d.RoomName == null || !effectiveNames.Contains(d.RoomName)));
        diagnostics.AddRange(RoomValidator.Validate(effectiveInputs, unambiguous.Values.Where(room => !effectiveNames.Contains(room.Name))));

        PalaceRooms? result = diagnostics.Any(d => d.Severity == RoomDiagnosticSeverity.Error)
            ? null : new PalaceRooms(bundledRooms, definitions.Select(room => new Room(room)));
        return new(result, diagnostics.AsReadOnly());
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
