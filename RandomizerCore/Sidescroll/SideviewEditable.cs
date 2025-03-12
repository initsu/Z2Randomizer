﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandomizerCore.Sidescroll;

//TODO:Restructure this as a Sideview class that understands how the format actually works
//has properties that describe things about it, and can answer questions about the net result of the format
//then replace all instances of the Sideview byte[] with references to that class.
//This will allow us to ask questions of it like "does this room contain a key door"
//or "can you get from the left to right side of this room without fairy"
//these will be very useful for the future (as well as just being easier to read)

/// <summary>
/// Holds a list of <see cref="SideviewMapCommand"/>s in order to allow programmatically editing a room. 
/// 
/// <see cref="Finalize"/> must be called after editing to retrieve the updated bytes for the room.
/// </summary>
public class SideviewEditable<T> where T : Enum
{
    private byte[] Header;
    public List<SideviewMapCommand<T>> Commands { get; set; } 

    /// <summary>
    /// Create a SideViewEditable from an array of bytes for a side view.
    /// </summary>
    /// <param name="bytes"><see cref="Room.SideView"/> goes here.</param>
    public SideviewEditable(byte[] bytes)
    {
        if(bytes.Length < 4) { throw new ArgumentException("Sideview data has no header."); }
        Header = bytes[0..4];
        Commands = new List<SideviewMapCommand<T>>();
        int i = 4; // start after header
        int xcursor = 0;
        while (i < bytes.Length)
        {
            byte[] objectBytes;
            byte firstByte = bytes[i++];
            if (i == bytes.Length) { throw new ArgumentException("Sideview data contains incomplete map command."); }
            byte secondByte = bytes[i++];
            int ypos = (firstByte & 0xF0) >> 4;
            if (secondByte == 15 && ypos < 13) // 3 byte object found
            {
                if (i == bytes.Length) { throw new ArgumentException("Sideview data contains incomplete map command."); }
                byte thirdByte = bytes[i++];
                objectBytes = [firstByte, secondByte, thirdByte];
            }
            else
            {
                objectBytes = [firstByte, secondByte];
            }
            if (ypos == 14) // "x skip" command resets the cursor to the start of a page
            {
                xcursor = 16 * (firstByte & 0x0F);
            }
            else
            {
                xcursor += firstByte & 0x0F;
            }
            SideviewMapCommand<T> o = new(objectBytes);
            o.AbsX = xcursor;
            Commands.Add(o);
        }
    }

    public byte FloorHeader
    {
        get { return Header[2]; }
        set { Header[2] = value; }
    }

    public byte BackgroundMap
    {
        get { return (byte)(Header[3] & 0b00000111); }
        set { Header[3] = (byte)((Header[3] & 0b11111000) + (value & 0b00000111)); }
    }

    public SideviewMapCommand<T>? Find(Predicate<SideviewMapCommand<T>> match)
    {
        return Commands.Find(match);
    }

    public List<SideviewMapCommand<T>> FindAll(Predicate<SideviewMapCommand<T>> match)
    {
        return Commands.FindAll(match);
    }


    public void Add(SideviewMapCommand<T> command)
    {
        Commands.Add(command);
    }

    public void Remove(SideviewMapCommand<T> item)
    {
        Commands.Remove(item);
    }

    public bool HasItem()
    {
        return Find(o => o.IsItem() && !o.Extra.IsMinorItem()) != null;
    }

    /// <summary>
    /// Rebuild the side view bytes with our updated list of map commands.
    /// 
    /// We iterate through every map command and make sure all of the
    /// relative x positions are updated to match their absolute x position.
    /// </summary>
    /// <returns>The final room side view bytes</returns>
    public byte[] Finalize()
    {
        int i = 0;
        // remove "x skip" commands before sorting
        // they are easily re-created if needed
        while (i < Commands.Count)
        {
            SideviewMapCommand<T> o = Commands[i];
            if (o.Y == 0xE)
            {
                Commands.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
        // map commands must be sorted left-to-right
        Commands.Sort((a, b) =>
            {
                if (a.AbsX != b.AbsX)
                {
                    return a.AbsX.CompareTo(b.AbsX);
                }
                else
                {
                    int aYOffset = (a.Y + 3) % 16;
                    int bYOffset = (b.Y + 3) % 16;
                    return aYOffset.CompareTo(bYOffset);
                }
            });
        i = 0;
        int xCursor = 0;
        while (i < Commands.Count)
        {
            SideviewMapCommand<T> o = Commands[i];
            if (xCursor + o.RelX != o.AbsX)
            {
                int xDiff = o.AbsX - xCursor;
                if (xDiff > 15) // create new "x skip" command
                {
                    var xSkip = SideviewMapCommand<T>.CreateXSkip(xDiff / 16);
                    Commands.Insert(i, xSkip);
                    i++;
                    xDiff = o.AbsX & 0xF;
                }
                o.RelX = xDiff;
            }
            xCursor = o.AbsX;
            i++;
        }
        byte[] bytes = [
            .. Header, 
            .. Commands.SelectMany(o => o.Bytes)
        ];
        bytes[0] = (byte)bytes.Length;
        return bytes;
    }

    public void Mirror()
    {
        List<SideviewMapCommand<T>> floors = FindAll(o => o.IsNewFloor());
        if (floors.Count > 0)
        {
            // shift floor param to the next object (since they go left->right)
            var startFloor = SideviewMapCommand<T>.CreateNewFloor(0, FloorHeader);
            byte nextFloorParam = startFloor.Bytes[1];
            for (int i = 0; i < floors.Count; i++)
            {
                var floor = floors[i];
                byte oldFloorParam = floor.Bytes[1];
                floor.Bytes[1] = nextFloorParam;
                nextFloorParam = oldFloorParam;
            }
            FloorHeader = (byte)((FloorHeader & 0b01110000) + (nextFloorParam & 0b10001111));
        }

        foreach (var cmd in Commands)
        {
            cmd.AbsX = 63 - cmd.AbsX - (cmd.Width - 1);
        }
    }

    public String DebugString()
    {
        StringBuilder sb = new StringBuilder("");
        var headerBytes = Convert.ToHexString(Header);
        var floor = SideviewMapCommand<T>.CreateNewFloor(0, FloorHeader);
        sb.AppendLine($"{headerBytes}       {"Floor",-26}{FloorHeader}");

        foreach (var c in Commands)
        {
            sb.AppendLine(c.DebugString());
        }
        return sb.ToString();
    }
}
