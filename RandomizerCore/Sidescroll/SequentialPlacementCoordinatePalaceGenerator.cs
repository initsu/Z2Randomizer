﻿using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace RandomizerCore.Sidescroll;

public class SequentialPlacementCoordinatePalaceGenerator() : CoordinatePalaceGenerator()
{
    private const int STALL_LIMIT = 5000;
    internal override Palace GeneratePalace(RandomizerProperties props, RoomPool rooms, Random r, int roomCount, int palaceNumber)
    {
        Palace palace = new(palaceNumber);
        List<(int, int)> openCoords = new();
        Dictionary<RoomExitType, List<Room>> roomsByExitType;
        RoomPool roomPool = new(rooms);
        int palaceGroup = palaceNumber switch
        {
            1 => 1,
            2 => 1,
            3 => 2,
            4 => 2,
            5 => 1,
            6 => 2,
            7 => 3,
            _ => throw new ImpossibleException("Invalid palace number: " + palaceNumber)
        };
        Room entrance = new(roomPool.Entrances[r.Next(roomPool.Entrances.Count)])
        {
            IsRoot = true,
            PalaceGroup = palaceGroup
        };
        openCoords.AddRange(entrance.GetOpenExitCoords());
        palace.AllRooms.Add(entrance);
        palace.Entrance = entrance;

        int stallCount = 0;
        int openJunctionsCount = 0;
        while (palace.AllRooms.Count + openCoords.Count < roomCount || openJunctionsCount > 0)
        {
            //Stalled out, try again from the start
            if(openCoords.Count == 0 || stallCount++ > STALL_LIMIT)
            {
                palace.IsValid = false;
                return palace;
            }
            Room? newRoom = roomPool.NormalRooms.Sample(r);
            if (newRoom == null)
            {
                palace.IsValid = false;
                return palace;
            }
            else
            {
                newRoom = new(newRoom);
                if (newRoom.LinkedRoomName != null)
                {
                    Room linkedRoom = rooms.LinkedRooms[newRoom.LinkedRoomName];
                    newRoom = newRoom.Merge(new(linkedRoom));
                }
            }

            openCoords.FisherYatesShuffle(r);
            (int, int) bestFit = (0,0);
            int bestFitExitCount = 0;
            foreach((int, int) openCoord in openCoords) 
            {
                newRoom.coords = openCoord;
                int fitExitCount = newRoom.FitsWithLeft(palace.AllRooms.FirstOrDefault(i => i.coords == (openCoord.Item1 - 1, openCoord.Item2)))
                    + newRoom.FitsWithDown(palace.AllRooms.FirstOrDefault(i => i.coords == (openCoord.Item1, openCoord.Item2 - 1)))
                    + newRoom.FitsWithUp(palace.AllRooms.FirstOrDefault(i => i.coords == (openCoord.Item1, openCoord.Item2 + 1)))
                    + newRoom.FitsWithRight(palace.AllRooms.FirstOrDefault(i => i.coords == (openCoord.Item1 + 1, openCoord.Item2)));

                if(fitExitCount > bestFitExitCount)
                {
                    bestFitExitCount = fitExitCount;
                    bestFit = openCoord;
                }
            }

            if(bestFitExitCount > 0 && bestFit != (0, 0))
            {
                newRoom.coords = bestFit;
                
                List<(int, int)> newOpenCoords = newRoom.GetOpenExitCoords();
                foreach ((int, int) coord in newOpenCoords.ToList())
                {
                    if (openCoords.Contains(coord) || palace.AllRooms.Any(i => i.coords == coord))
                    {
                        newOpenCoords.Remove(coord);
                    }
                }
                //If adding this room would cause the number of open coordinates to be too large,
                //keep searching for a room that more precisely fits the available space.
                if (newOpenCoords.Count + openCoords.Count + palace.AllRooms.Count > roomCount)
                {
                    continue;
                }
                if (props.NoDuplicateRoomsBySideview && AllowDuplicatePrevention(props, palaceNumber))
                {
                    if (palace.AllRooms.Any(i => byteArrayEqualityComparer.Equals(i.SideView, newRoom.SideView)))
                    {
                        continue;
                    }
                }
                if (props.NoDuplicateRooms && AllowDuplicatePrevention(props, palaceNumber))
                {
                    roomPool.NormalRooms.Remove(newRoom);
                }
                Room left = palace.AllRooms.FirstOrDefault(i => i.coords == (bestFit.Item1 - 1, bestFit.Item2))!;
                Room down = palace.AllRooms.FirstOrDefault(i => i.coords == (bestFit.Item1, bestFit.Item2 - 1))!;
                Room up = palace.AllRooms.FirstOrDefault(i => i.coords == (bestFit.Item1, bestFit.Item2 + 1))!;
                Room right = palace.AllRooms.FirstOrDefault(i => i.coords == (bestFit.Item1 + 1, bestFit.Item2))!;
                if(newRoom.FitsWithLeft(left) > 0)
                {
                    newRoom.Left = left;
                    left.Right = newRoom;
                }
                if (newRoom.FitsWithDown(down) > 0)
                {
                    newRoom.Down = down;
                    if(!newRoom.HasDrop)
                    {
                        down.Up = newRoom;
                    }
                }
                if (newRoom.FitsWithUp(up) > 0)
                {
                    if(!up.HasDrop)
                    {
                        newRoom.Up = up;
                    }
                    up.Down = newRoom;
                }
                if (newRoom.FitsWithRight(right) > 0)
                {
                    newRoom.Right = right;
                    right.Left = newRoom;
                }
                openCoords.AddRange(newOpenCoords);
                openCoords.Remove(bestFit);
                palace.AllRooms.Add(newRoom);
                stallCount = 0;

                //Count the number of open coordinates that are junction coordinates, i.e. they have
                //more than 1 room they need to connect to. We want to fill all of these in before capping paths
                //to prevent the capping logic from getting dumb.
                //I considered just categorizing all the rooms by type and doing logic to determine the appropriate cap,
                //but that logic tree to find what shape the hole is to fill with the appropriate peg got messy.
                openJunctionsCount = 0;
                foreach((int, int) coord in openCoords)
                {
                    //debug++;
                    Room? coordLeft = palace.AllRooms.FirstOrDefault(i => i.coords == (coord.Item1 - 1, coord.Item2));
                    Room? coordRight = palace.AllRooms.FirstOrDefault(i => i.coords == (coord.Item1 + 1, coord.Item2));
                    Room? coordUp = palace.AllRooms.FirstOrDefault(i => i.coords == (coord.Item1, coord.Item2 + 1));
                    Room? coordDown = palace.AllRooms.FirstOrDefault(i => i.coords == (coord.Item1, coord.Item2 - 1));

                    if((coordLeft != null && coordLeft.HasRightExit ? 1 : 0)
                        + (coordRight != null && coordRight.HasLeftExit ? 1 : 0)
                        + (coordUp != null && (coordUp.HasDownExit || coordUp.HasDrop) ? 1 : 0)
                        + (coordDown != null && coordDown.HasUpExit ? 1 : 0) >= 2)
                    {
                        openJunctionsCount++;
                    }
                }

                //Debug.WriteLine("Added Room at (" + newRoom.coords.Item1 + ", " + newRoom.coords.Item2 + ")");
            }
        }
        //close stubs
        if(openCoords.Count > 0)
        {
            roomsByExitType = roomPool.CategorizeNormalRoomExits();

            foreach ((int, int) openCoord in openCoords.ToList())
            {
                Room? left = palace.AllRooms.FirstOrDefault(i => i.coords == (openCoord.Item1 - 1, openCoord.Item2));
                Room? right = palace.AllRooms.FirstOrDefault(i => i.coords == (openCoord.Item1 + 1, openCoord.Item2));
                Room? up = palace.AllRooms.FirstOrDefault(i => i.coords == (openCoord.Item1, openCoord.Item2 + 1));
                Room? down = palace.AllRooms.FirstOrDefault(i => i.coords == (openCoord.Item1, openCoord.Item2 - 1));
                if ((left != null && left.HasRightExit ? 1 : 0)
                    + (right != null && right.HasLeftExit ? 1 : 0)
                    + (up != null && (up.HasDownExit || up.HasDrop) ? 1 : 0)
                    + (down != null && down.HasUpExit ? 1 : 0) >= 2)
                {
                    throw new Exception("Junction remains in stub closing that should have been cleaned up");
                }

                RoomExitType exitType;
                if (left != null && left.HasRightExit)
                {
                    exitType = RoomExitType.DEADEND_EXIT_LEFT;
                }
                else if (right != null && right.HasLeftExit)
                {
                    exitType = RoomExitType.DEADEND_EXIT_RIGHT;
                }
                else if (up != null && up.HasDownExit)
                {
                    if (up.HasDrop)
                    {
                        exitType = RoomExitType.NO_ESCAPE;
                        logger.Debug("Drop stubs are currently unsupported. Ask discord how we feel about these");
                        palace.IsValid = false;
                        return palace;
                    }
                    else
                    {
                        exitType = RoomExitType.DEADEND_EXIT_UP;
                    }
                }
                else if (down != null && down.HasUpExit)
                {
                    exitType = RoomExitType.DEADEND_EXIT_DOWN;
                }
                else
                {
                    throw new ImpossibleException("Open coordinate has no adjacent exits");
                }
                roomsByExitType.TryGetValue(exitType, out var possibleStubs);

                bool placed = false;
                do //while (placed == false)
                {
                    Room? newRoom = possibleStubs?.Sample(r);
                    if (newRoom == null)
                    {
                        roomPool.DefaultStubsByDirection.TryGetValue(exitType, out newRoom);
                    }
                    //This should no longer be possible since default stubs aren't removable
                    if (newRoom == null)
                    {
                        palace.IsValid = false;
                        return palace;
                    }
                    else
                    {
                        newRoom = new(newRoom);
                        //If the stub is a drop zone, pretend it isn't, otherwise junctions can appear
                        //as a result of adding the stub.
                        if (newRoom.IsDropZone)
                        {
                            newRoom.IsDropZone = false;
                        }
                    }
                    newRoom.coords = openCoord;
                    palace.AllRooms.Add(newRoom);
                    openCoords.Remove(openCoord);
                    placed = true;

                    if (left != null && newRoom.HasLeftExit)
                    {
                        newRoom.Left = left;
                        left.Right = newRoom;
                    }
                    if (down != null && newRoom.HasDownExit)
                    {
                        newRoom.Down = down;
                        down.Up = newRoom;
                    }
                    if (up != null && newRoom.HasUpExit)
                    {
                        newRoom.Up = up;
                        up.Down = newRoom;
                    }
                    if (right != null && newRoom.HasRightExit)
                    {
                        newRoom.Right = right;
                        right.Left = newRoom;
                    }

                    if (props.NoDuplicateRooms && AllowDuplicatePrevention(props, palaceNumber) && newRoom.Group != RoomGroup.STUBS)
                    {
                        roomsByExitType[exitType].Remove(newRoom);
                    }
                    if (props.NoDuplicateRoomsBySideview && AllowDuplicatePrevention(props, palaceNumber))
                    {
                        List<Room> duplicateSideviewStubs = roomPool.NormalRooms.Where(i => byteArrayEqualityComparer.Equals(i.SideView, newRoom.SideView)
                            && i.Group != RoomGroup.STUBS).ToList();
                        roomPool.NormalRooms.RemoveAll(i => byteArrayEqualityComparer.Equals(i.SideView, newRoom.SideView)
                            && i.Group != RoomGroup.STUBS);
                    }
                } while (placed == false);
            }
        }

        if (palace.AllRooms.Count > roomCount)
        {
            throw new ImpossibleException("Palace Room count exceeds maximum room count.");
        }
        if(openCoords.Count != 0)
        {
            throw new ImpossibleException("Stray open coordinate after palace is generated");
        }

        //Recategorize the remaining rooms after stubbing out.
        roomsByExitType = roomPool.CategorizeNormalRoomExits();

        if (!AddSpecialRoomsByReplacement(palace, roomPool, r, props))
        {
            palace.IsValid = false;
            return palace;
        }

        if (palace.AllRooms.Count != roomCount)
        {
            throw new Exception("Generated palace has the incorrect number of rooms");
        }

        
        palace.AllRooms.ForEach(i => i.PalaceNumber = palaceNumber);
        palace.IsValid = true;
        return palace;
    }
}
