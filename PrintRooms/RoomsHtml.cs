
using RandomizerCore;
using RandomizerCore.Sidescroll;
using System.Text;

namespace PrintRooms;

class RoomsHtml
{
    private List<string> roomHtmlList = new();

    public void Add(Room room, Room? linkedRoom, string imageName)
    {
        StringBuilder tagsSB = new();
        if (room.LinkedRoomName != null) { tagsSB.Append("#SegmentedRoom "); }
        if (room.IsEntrance) { tagsSB.Append("#Entrance "); }
        if (room.IsBossRoom) { tagsSB.Append("#BossRoom "); }
        if (room.HasItem) { tagsSB.Append("#ItemRoom "); }
        string tagsStr = tagsSB.Length > 0 ? $"<p class=\"room-tags\">{tagsSB.ToString()}</p>" : "";

        StringBuilder connSB = new();
        connSB.Append("<p class=\"room-connections\">Connections: ");
        if (room.IsDropZone) { connSB.Append("#DropZone "); }
        if (room.HasLeftExit) { connSB.Append("&#x2B05; "); }
        if (room.HasRightExit) { connSB.Append("&#x27A1; "); }
        if (room.HasUpExit) { connSB.Append("&#x2B06; "); }
        if (room.HasDownExit) { connSB.Append("&#x2B07; "); }
        if (room.ElevatorScreen != -1) { connSB.Append("#Elevator "); }
        if (room.HasDrop) { connSB.Append("#HasDrop "); }
        else if (!room.IsEntrance && !room.IsBossRoom && room.CountExits() < 2) { connSB.Append("#DeadEnd "); }
        connSB.Append("</p>");
        string connectionsStr = connSB.ToString();

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

        string roomHtml = $"""
<div class="room">
    <img class="room-img" src="{imageName}" />
    <p>
      <span class="room-name">{PrintRooms.GetName(room)}</span>
      <span class="room-author">by {room.Author}</span>
    </p>
    {tagsStr}
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
<html>
<head>
    <meta name="viewport" content="width=device-width, initial-scale=1, user-scalable=yes">
    <title>Zelda II Randomizer 4.4 Beta Rooms</title>
</head>
<style>
    body {
        margin: 4px;
        font-family: monospace;
    }
    @media (prefers-color-scheme: dark) {
      body {
        color: #fff;
        background: #181818;
      }
    }

    .rooms {
        display: flex;
        flex-wrap: wrap;
    }

    .room {
        flex: 400px;
        margin: 4px 4px 20px;

        @media screen and (max-width:540.9px) {
            height: 78.24vh;
        }
    }

    .room p {
        margin: 0;
        font-size: 1.2em;
        @media screen and (max-width:800px) {
            font-size: 0.70em;
        }
        @media screen and (max-width:600px) {
            font-size: 0.60em;
        }
        @media screen and (max-width:540.9px) {
            font-size: 0.33em;
        }
    }

    .room-name {
        display: inline-block;
        font-style: italic;
    }

    .room-author {
        display: inline-block;
        opacity: 0.4;
    }

    .room-img {
        width: 100%;
        @media screen and (max-width:800px) {
            image-rendering: pixelated;
        }
    }

    .room-tags {
        display: block;
    }

    .room-connections {
        display: block;
    }

    .room-requirements {
        display: block;
    }

    .requirement-img {
        height: 1.6em;
        vertical-align: middle;
        image-rendering: pixelated;
    }
</style>

<body>

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
