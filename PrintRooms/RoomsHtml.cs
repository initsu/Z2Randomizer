
using System.Text;
using Z2Randomizer.RandomizerCore;
using Z2Randomizer.RandomizerCore.Sidescroll;

namespace PrintRooms;

class RoomsHtml
{
    private List<string> roomHtmlList = new();

    public void Add(Room room, Room? linkedRoom, string imageName)
    {
        List<string> roomTypes = [];
        if (room.IsEntrance) { roomTypes.Add("Entrance"); }
        if (room.HasItem) { roomTypes.Add("ItemRoom"); }
        if (room.IsBossRoom) { roomTypes.Add("BossRoom"); }
        if (room.IsDropZone) { roomTypes.Add("DropZone"); }
        if (room.LinkedRoomName != null) { roomTypes.Add("SegmentedRoom"); }
        string roomTypesJoined = roomTypes.Count > 0 ? ("#" + string.Join(" #", roomTypes)) : "";
        string roomTypesStr = roomTypesJoined.Length > 0 ? $"<p class=\"room-tags\">{roomTypesJoined}</p>" : "";

        StringBuilder connSB = new();
        connSB.Append("<p class=\"room-connections\">Connections: ");
        // connSB.Append(room.CategorizeExits().ToArrowString());
        if (room.HasLeftExit) { connSB.Append("&#8678; "); }
        if (room.HasRightExit) { connSB.Append("&#8680; "); }
        if (room.HasUpExit) { connSB.Append("&#8679; "); }
        if (room.HasDownExit && !room.HasDrop) { connSB.Append("&#8681; "); }
        if (room.HasDownExit && room.HasDrop) { connSB.Append("&#8675; "); }
        connSB.Append("</p>");
        string connectionsStr = connSB.ToString();

        var tags = room.Tags ?? [];

        StringBuilder reqSB = new();
        var req = room.Requirements;
        if (req.ToString() != "[]") // IsEmpty()?
        {
            reqSB.Append("<p class=\"room-requirements\">Requirements:");
            bool firstOr = true;
            foreach (var indReq in req.IndividualRequirements)
            {
                if (!firstOr) { reqSB.AppendLine("OR"); }
                firstOr = false;
                AddRequirementIcon(reqSB, indReq);
            }
            foreach (var compReq in req.CompositeRequirements)
            {
                if (!firstOr) { reqSB.AppendLine("OR"); }
                firstOr = false;
                bool firstAnd = true;
                foreach (var innerReq in compReq)
                {
                    if (!firstAnd) { reqSB.AppendLine("+"); }
                    firstAnd = false;
                    AddRequirementIcon(reqSB, innerReq);
                }
            }
            reqSB.AppendLine("</p>");
        }
        string requirementsStr = reqSB.ToString();

        string roomTypeClasses = string.Join(" ", roomTypes.Select(tag => $"RoomType-{tag}"));
        string tagClasses = string.Join(" ", tags.Select(tag => $"Tag-{tag}"));
        string roomHtml = $"""
    <div class="room RoomGroup-{room.Group.ToString()} Palace-{(room.PalaceNumber != 7 ? "RegularPalace" : "GreatPalace")} ExitType-{room.CategorizeExits()} {roomTypeClasses} {tagClasses}">
      <img class="room-img" src="{imageName}" loading="lazy" />
      <p>
        <span class="room-name">{PrintRooms.GetName(room)}</span>
        <span class="room-author">by {room.Author}</span>
      </p>
      {roomTypesStr}
      {connectionsStr}
      {requirementsStr}
    </div>
""";
        roomHtmlList.Add(roomHtml);

        if (linkedRoom != null)
        {
            Add(linkedRoom, null, imageName);
        }
    }

    private static void AddRequirementIcon(StringBuilder sb, RequirementType ireq)
    {
        switch (ireq)
        {
            case RequirementType.JUMP:
            case RequirementType.FAIRY:
            case RequirementType.UPSTAB:
            case RequirementType.DOWNSTAB:
            case RequirementType.KEY:
            case RequirementType.DASH:
            case RequirementType.GLOVE:
                sb.AppendLine($"<img class=\"requirement-img\" src=\"requirements/{ireq.ToString().ToLower()}.png\"/>");
                break;
            default:
                sb.AppendLine(ireq.ToString());
                break;
        }
    }

    public string Finalize()
    {
        StringBuilder sb = new StringBuilder("");
        sb.AppendLine("""
<!DOCTYPE html>
<html lang="en">

<head>
  <meta name="viewport" content="width=device-width, initial-scale=1, user-scalable=yes">
  <title>Zelda II Randomizer 4.4 Beta Rooms</title>
  <link rel="stylesheet" href="index.css" />
</head>
<style id="room-group-dynamic-style"></style>
<body>
  <div id="room-group-toggles"></div>
  <script src="index.js" charset="UTF-8"></script>
  <div class="rooms">
""");
        foreach (var roomHtml in roomHtmlList)
        {
            sb.AppendLine(roomHtml);
        }
        sb.AppendLine("""
  </div>
</body>

</html>
""");
        return sb.ToString();
    }
}
