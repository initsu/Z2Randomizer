using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Sidescroll;
using Z2Randomizer.RandomizerCore.Sidescroll.Palace;

namespace CrossPlatformUI.Services;

public sealed record CustomRoomSnapshot(RoomPoolSpec Spec, CustomRoomLoadResult Result);

public class RoomLoaderService
{
    private readonly IFileSystemService fileService;
    private readonly Lazy<Task<string>> loadPalaceRoomsJsonTask;

    public RoomLoaderService(IFileSystemService fileService)
    {
        this.fileService = fileService;
        loadPalaceRoomsJsonTask = new(() =>
            fileService.OpenFile(IFileSystemService.RandomizerPath.Palaces, "PalaceRooms.json"));
    }

    public async Task<PalaceRooms> GetPalaceRooms(VanillaRoomData vanillaRoomData) =>
        new(await loadPalaceRoomsJsonTask.Value, doValidation: false, vanillaRoomData);

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

    public async Task<CustomRoomSnapshot> LoadCustomRooms(VanillaRoomData vanillaRoomData, CancellationToken cancellationToken)
    {
        var spec = await GetRoomPoolSpec() ?? throw new UserFacingException("Custom Room Pool Issue", $"{CustomRoomLoader.SpecFilename} is missing or empty.");
        var path = IFileSystemService.RandomizerPath.Rooms;
        var result = await CustomRoomLoader.LoadAsync(await GetPalaceRooms(vanillaRoomData), spec, "Rooms",
            () => fileService.ListLocalFiles(path), filename => fileService.OpenFile(path, filename), cancellationToken, vanillaRoomData);
        return new(spec, result);
    }
}
