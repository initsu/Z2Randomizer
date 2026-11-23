using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Sidescroll;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace CrossPlatformUI.Services;

public class RoomLoaderService
{
    private readonly IFileSystemService fileService;
    private readonly Lazy<Task<PalaceRooms>> loadPalaceRoomsTask;

    public RoomLoaderService(IFileSystemService fileService)
    {
        this.fileService = fileService;
        loadPalaceRoomsTask = new(LoadPalaceRoomsAsync);
    }

    public Task<PalaceRooms> GetPalaceRooms() => loadPalaceRoomsTask.Value;

    public async Task<RoomPoolSpec?> GetRoomPoolSpec()
    {
        try
        {
            var yaml = await fileService.OpenFile(IFileSystemService.RandomizerPath.RoomPool, CustomRoomLoader.SpecFilename);
            return string.IsNullOrWhiteSpace(yaml) ? null : RoomPoolSpecDeserializer.FromString(yaml);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException) { return null; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new UserFacingException("Custom Room Pool Issue", $"Unable to read {CustomRoomLoader.SpecFilename}. {exception.Message}");
        }
    }

    public async Task<(RoomPoolSpec Spec, CustomRoomLoadResult Catalogue)> LoadCustomRooms(CancellationToken cancellationToken)
    {
        var spec = await GetRoomPoolSpec() ?? throw new UserFacingException("Custom Room Pool Issue", $"{CustomRoomLoader.SpecFilename} is missing or empty.");
        var path = IFileSystemService.RandomizerPath.Rooms;
        var catalogue = await CustomRoomLoader.LoadAsync(await GetPalaceRooms(), spec, "Rooms",
            () => fileService.ListLocalFiles(path), filename => fileService.OpenFile(path, filename), cancellationToken);
        return (spec, catalogue);
    }

    private async Task<PalaceRooms> LoadPalaceRoomsAsync()
    {
        var roomsJson = await fileService.OpenFile(IFileSystemService.RandomizerPath.Palaces, "PalaceRooms.json");
        return new PalaceRooms(roomsJson, false);
    }
}
