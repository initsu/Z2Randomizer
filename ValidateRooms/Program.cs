using System.Text;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Sidescroll;

string[] filenames = args.Length == 0 ? ["PalaceRooms.json"] : args;
var report = new StringBuilder();
var rooms = new List<RoomValidationInput>();
var readDiagnostics = new List<RoomDiagnostic>();
foreach (var filename in filenames)
{
    report.AppendLine($"Validating \"{filename}\"...");
    try { RoomFileReader.Read(filename, Util.ReadAllTextFromFile(filename), rooms, readDiagnostics); }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
        readDiagnostics.Add(new("InvalidRoomFile", RoomDiagnosticSeverity.Error, filename, null, null, false, exception.Message));
    }
}

bool hasErrors = false;
foreach (var diagnostic in readDiagnostics.Concat(RoomValidator.Validate(rooms)))
{
    hasErrors |= diagnostic.Severity == RoomDiagnosticSeverity.Error;
    report.AppendLine(diagnostic.ToString());
}

var output = report.ToString();
File.WriteAllText("ValidationOutput.txt", output);
Console.Write(output);
return hasErrors ? 1 : 0;
