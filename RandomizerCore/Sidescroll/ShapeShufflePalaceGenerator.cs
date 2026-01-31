using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Z2Randomizer.RandomizerCore.Sidescroll;

public class ShapeShufflePalaceGenerator : ShapeFirstCoordinatePalaceGenerator
{
    private static readonly WeightedShuffler<RoomExitType> _shapeWeights = new([
        // we place dead ends last, so they are not included in the shuffle
        //(RoomExitType.DEADEND_EXIT_RIGHT, 1),
        //(RoomExitType.DEADEND_EXIT_UP, 1),
        //(RoomExitType.DEADEND_EXIT_LEFT, 1),
        //(RoomExitType.DEADEND_EXIT_DOWN, 1),

        (RoomExitType.HORIZONTAL_PASSTHROUGH, 20),

        (RoomExitType.NE_L, 4),
        (RoomExitType.NW_L, 4),
        (RoomExitType.SE_L, 4),
        (RoomExitType.SW_L, 4),

        (RoomExitType.INVERSE_T, 4),
        (RoomExitType.T,         4),
        (RoomExitType.RIGHT_T,   2),
        (RoomExitType.LEFT_T,    2),

        (RoomExitType.FOUR_WAY, 2),

        (RoomExitType.VERTICAL_PASSTHROUGH, 1),

        // we don't think about drop rooms just yet
    ]);

    protected virtual WeightedShuffler<RoomExitType> GetShapeWeights(int palaceNumber)
    {
        return _shapeWeights;
    }

    readonly record struct Entry(Coord coord, RoomExitType shape);

    protected override async Task<Dictionary<Coord, RoomExitType>> GetPalaceShape(RandomizerProperties props, Palace palace, RoomPool roomPool, Random r, int roomCount)
    {
        var shapeWeights = GetShapeWeights(palace.Number);

        List<Coord> openCoords = new();
        Dictionary<Coord, RoomExitType> shapeGrid = [];
        Coord coord = Coord.Uninitialized;
        Room entrance = new(roomPool.Entrances[r.Next(roomPool.Entrances.Count)])
        {
            IsRoot = true,
        };
        Debug.Assert(palace.AllRooms.Count == 0);
        palace.AllRooms.Add(entrance); // not sure I agree that rooms should be added to the palace in a "Get" method
        palace.Entrance = entrance;

        RoomExitType shape = entrance.CategorizeExits();
        shapeGrid[coord] = shape;

        enqueueFromShape(coord, shape, 4);

        bool coordCanHoldShape(Coord coord, RoomExitType shape)
        {
            bool fitNeighbor(bool hasConnection, Coord neighborCoord, Func<RoomExitType, bool> neighborHasConnection)
            {
                if (!shapeGrid.TryGetValue(neighborCoord, out var neighborShape)) { return true; }
                bool mustHaveConnection = neighborHasConnection(neighborShape);
                return hasConnection == mustHaveConnection;
            }

            return
                fitNeighbor(shape.ContainsLeft(), new Coord(coord.X - 1, coord.Y), other => other.ContainsRight()) &&
                fitNeighbor(shape.ContainsRight(), new Coord(coord.X + 1, coord.Y), other => other.ContainsLeft()) &&
                fitNeighbor(shape.ContainsDown(), new Coord(coord.X, coord.Y - 1), other => other.ContainsUp()) &&
                fitNeighbor(shape.ContainsUp(), new Coord(coord.X, coord.Y + 1), other => other.ContainsDown());
        }

        RoomExitType noNewOpeningsShape(Coord coord)
        {
            RoomExitType shape = RoomExitType.NO_ESCAPE;

            void fitNeighbor(Coord neighborCoord, Func<RoomExitType, bool> neighborHasConnection, Func<RoomExitType> addConnection)
            {
                if (!shapeGrid.TryGetValue(neighborCoord, out var neighborShape)) { return; }
                bool mustHaveConnection = neighborHasConnection(neighborShape);
                if (mustHaveConnection)
                {
                    addConnection();
                }
            }

            fitNeighbor(new Coord(coord.X - 1, coord.Y), other => other.ContainsRight(), () => shape = shape.AddLeft());
            fitNeighbor(new Coord(coord.X + 1, coord.Y), other => other.ContainsLeft(), () => shape = shape.AddRight());
            fitNeighbor(new Coord(coord.X, coord.Y - 1), other => other.ContainsUp(), () => shape = shape.AddDown());
            fitNeighbor(new Coord(coord.X, coord.Y + 1), other => other.ContainsDown(), () => shape = shape.AddUp());

            return shape;
        }

        bool enqueueFromShape(Coord coord, RoomExitType shape, int roomSpaceLeft)
        {
            List<Coord> newCoords = new();
            void enqueueCoordIfNotVisited(Coord coord)
            {
                if (!shapeGrid.ContainsKey(coord))
                {
                    if (!openCoords.Contains(coord))
                    {
                        newCoords.Add(coord);
                    }
                }
            }

            if (shape.ContainsLeft())
            {
                enqueueCoordIfNotVisited(new Coord(coord.X - 1, coord.Y));
            }
            if (shape.ContainsRight())
            {
                enqueueCoordIfNotVisited(new Coord(coord.X + 1, coord.Y));
            }
            if (shape.ContainsDown())
            {
                enqueueCoordIfNotVisited(new Coord(coord.X, coord.Y - 1));
            }
            if (shape.ContainsUp())
            {
                enqueueCoordIfNotVisited(new Coord(coord.X, coord.Y + 1));
            }

            if (newCoords.Count > roomSpaceLeft)
            {
                return false;
            }

            newCoords.ForEach(c => openCoords.Add(c));

            return true;
        }

        while (true)
        {
            await Task.Yield();

            var roomSpaceLeft = roomCount - shapeGrid.Count - openCoords.Count;
            if (roomSpaceLeft > 0)
            {
                if (openCoords.Count == 0)
                {
                    // recurse instead of trying to fix this
                    return await GetPalaceShape(props, palace, roomPool, r, roomCount);
                }

                coord = openCoords[0];
                openCoords.RemoveAt(0);

                RoomExitType[] shuffled = shapeWeights.Shuffle(r);
                for (int i = 0; ; i++)
                {
                    if (i == shuffled.Length)
                    {
                        // recurse instead of trying to fix this
                        return await GetPalaceShape(props, palace, roomPool, r, roomCount);
                    }
                    shape = shuffled[i];
                    if (coordCanHoldShape(coord, shape))
                    {
                        if (enqueueFromShape(coord, shape, roomSpaceLeft))
                        {
                            shapeGrid[coord] = shape;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (openCoords.Count == 0) { break; }
                // plug open coords
                coord = openCoords[0];
                openCoords.RemoveAt(0);

                shape = noNewOpeningsShape(coord);
                shapeGrid[coord] = shape;
            }
            Debug.WriteLine("\n" + GetLayoutDebug(shapeGrid, false) + "\n");
        }

        // TODO: dropify

        return shapeGrid;
    }
}
