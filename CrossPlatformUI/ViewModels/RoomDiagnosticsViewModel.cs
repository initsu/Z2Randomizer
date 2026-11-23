using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using ReactiveUI;
using ReactiveUI.Binding;
using RUISG = ReactiveUI.SourceGenerators;
using RxVoid = ReactiveUI.Primitives.RxVoid;
using Z2Randomizer.RandomizerCore.Sidescroll;

namespace CrossPlatformUI.ViewModels;

public sealed partial class RoomDiagnosticsViewModel : ReactiveObject
{
    public const string SuppressionHint =
        "If you've verified that each warning this line adds is a false positive, you can suppress the warnings.";

    public sealed partial class Suggestion : ReactiveObject
    {
        public string Text { get; }
        public ReactiveCommand<RxVoid, RxVoid> Copy { get; }

        [RUISG.Reactive]
        public partial string CopyButtonText { get; private set; } = "Copy";

        public Suggestion(string text)
        {
            Text = text;
            Copy = ReactiveCommand.CreateFromTask(async () => { CopyButtonText = await CopyTextAsync(Text); });
        }
    }

    public sealed record Entry(string Heading, string Message, Suggestion? Suggestion)
    {
        public bool HasHeading => Heading.Length != 0;
        public bool HasSuggestion => Suggestion != null;
    }

    public sealed record RoomEntries(string Heading, string FilePath, IReadOnlyList<Entry> Entries, Suggestion? Suppression)
    {
        public IReadOnlyList<Entry> Rows { get; } = Suppression == null
            ? Entries
            : [.. Entries, new("", SuppressionHint, Suppression)];

        public bool HasHeading => Heading.Length != 0;
        public bool HasFilePath => !string.IsNullOrWhiteSpace(FilePath);
    }

    [RUISG.Reactive]
    public partial IReadOnlyList<RoomEntries> Rooms { get; private set; } = [];

    [RUISG.Reactive]
    public partial string CopyButtonText { get; private set; } = "Copy report";

    [RUISG.Reactive]
    public partial bool IsOpen { get; private set; }

    [RUISG.Reactive]
    public partial bool AllowContinue { get; private set; }

    public ReactiveCommand<RxVoid, RxVoid> Close { get; }
    public ReactiveCommand<RxVoid, RxVoid> Continue { get; }
    public ReactiveCommand<RxVoid, RxVoid> CopyReport { get; }

    private TaskCompletionSource<bool>? pending;

    public RoomDiagnosticsViewModel()
    {
        Close = ReactiveCommand.Create(Dismiss);
        Continue = ReactiveCommand.Create(() => Complete(AllowContinue), this.WhenAnyValue(x => x.AllowContinue));
        CopyReport = ReactiveCommand.CreateFromTask(CopyReportAsync);
    }

    public Task<bool> ShowAsync(IEnumerable<RoomDiagnostic> diagnostics, bool allowContinue)
    {
        pending ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
        Rooms = diagnostics.OrderByDescending(diagnostic => diagnostic.Severity)
            .GroupBy(diagnostic => (diagnostic.Source, diagnostic.RoomIndex, diagnostic.RoomName))
            .Select(room => new RoomEntries(
                !string.IsNullOrWhiteSpace(room.Key.RoomName) ? $"Room: {room.Key.RoomName}" :
                    room.Key.RoomIndex is int index ? $"Room #{index}" : "",
                room.Key.Source,
                room.Select(diagnostic => new Entry(
                    $"{diagnostic.Severity}: {diagnostic.Id}{(diagnostic.IsMirrored ? " (mirrored)" : "")}",
                    diagnostic.Message,
                    diagnostic.SuggestedChange is { } correction ? new Suggestion(correction) : null)).ToArray(),
                room.Select(diagnostic => diagnostic.AllRoomWarningsSuppressionLine ?? diagnostic.SuppressionLine)
                    .FirstOrDefault(line => line != null) is { } suppression ? new Suggestion(suppression) : null))
            .ToArray();
        CopyButtonText = "Copy report";
        AllowContinue = allowContinue;
        IsOpen = true;
        return pending.Task;
    }

    private async Task CopyReportAsync()
    {
        var rooms = Rooms;
        var sections = new List<string>();
        foreach (var room in rooms)
        {
            var location = new List<string>();
            if (room.HasHeading) { location.Add(room.Heading); }
            if (room.HasFilePath) { location.Add(room.FilePath); }
            if (location.Count != 0) { sections.Add(string.Join('\n', location)); }
            sections.AddRange(room.Entries.Select(entry => entry.Suggestion is { } correction
                ? $"{entry.Heading}\n{entry.Message}\nSuggested change: {correction.Text}"
                : $"{entry.Heading}\n{entry.Message}"));
            if (room.Suppression is { } suppression)
            {
                sections.Add($"{SuppressionHint}\n{suppression.Text}");
            }
        }

        string status = await CopyTextAsync(string.Join("\n\n", sections));
        if (ReferenceEquals(Rooms, rooms)) { CopyButtonText = status; }
    }

    private static async Task<string> CopyTextAsync(string text)
    {
        var clipboard = App.TopLevel?.Clipboard;
        if (clipboard == null) { return "Clipboard unavailable"; }

        try
        {
            await clipboard.SetTextAsync(text);
            return "Copied";
        }
        catch (Exception exception)
        {
            NLog.LogManager.GetCurrentClassLogger().Warn(exception, "Unable to copy custom room diagnostics text.");
            return "Copy failed";
        }
    }

    internal void Dismiss() => Complete(false);

    private void Complete(bool accepted)
    {
        var review = pending;
        if (review == null) { return; }
        pending = null;
        IsOpen = false;
        review.SetResult(accepted);
    }
}
