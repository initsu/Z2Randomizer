using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Z2Randomizer.RandomizerCore.Sidescroll;

public class ToroidalGridPalaceGenerator : ShapeCoordinatePalaceGenerator
{
    private static readonly WeightedShuffler<RoomExitType> _shapeWeights = new([
        // we place dead ends at the end instead
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

    protected override async Task<PalaceShape> CreateShape(Random r, Room entrance, int roomCount, int palaceNumber)
    {
        var shapeWeights = GetShapeWeights(palaceNumber);

        List<Coord> openCoords = new();
        Dictionary<Coord, RoomExitType> shapeGrid = [];
        Coord coord = Coord.Uninitialized;
        RoomExitType shape = entrance.CategorizeExits();
        shapeGrid[coord] = shape;
        bool xWraps = false;
        bool yWraps = false;
        int top = 0;
        int bottom = 0;

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
                if (yWraps && coord.Y == bottom) { return false; }
                enqueueCoordIfNotVisited(new Coord(coord.X, coord.Y - 1));
            }
            if (shape.ContainsUp())
            {
                if (yWraps && coord.Y == top) { return false; }
                enqueueCoordIfNotVisited(new Coord(coord.X, coord.Y + 1));
            }

            if (newCoords.Count > roomSpaceLeft)
            {
                return false;
            }

            newCoords.ForEach(c => openCoords.Add(c));

            return true;
        }

        void dequeueFromShape(Coord coord, RoomExitType shape)
        {
            void dequeueCoordIfNotVisited(Coord coord)
            {
                if (openCoords.Contains(coord))
                {
                    openCoords.Remove(coord);
                }
            }

            if (shape.ContainsLeft())
            {
                dequeueCoordIfNotVisited(new Coord(coord.X - 1, coord.Y));
            }
            if (shape.ContainsRight())
            {
                dequeueCoordIfNotVisited(new Coord(coord.X + 1, coord.Y));
            }
            if (shape.ContainsDown())
            {
                dequeueCoordIfNotVisited(new Coord(coord.X, coord.Y - 1));
            }
            if (shape.ContainsUp())
            {
                dequeueCoordIfNotVisited(new Coord(coord.X, coord.Y + 1));
            }
        }

        int? loopAtY = null;
        while (true)
        {
            await Task.Yield();

            var roomSpaceLeft = roomCount - shapeGrid.Count - openCoords.Count;
            if (roomSpaceLeft > 0)
            {
                if (openCoords.Count == 0)
                {
                    // just recurse instead of trying to fix this
                    return await CreateShape(r, entrance, roomCount, palaceNumber);
                }

                coord = openCoords[0];
                openCoords.RemoveAt(0);

                RoomExitType[] shuffled = shapeWeights.Shuffle(r);
                for (int i = 0; ; i++)
                {
                    if (i == shuffled.Length)
                    {
                        // throw new Exception("No shape matched.");
                        // just recurse instead of trying to fix this
                        return await CreateShape(r, entrance, roomCount, palaceNumber);
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
                if (!xWraps || !yWraps)
                {
                    // time to tie up some open coords
                    int minX = 0, maxX = 0;
                    top = 0;
                    bottom = 0;
                    foreach (var kvp in shapeGrid)
                    {
                        var c = kvp.Key;
                        if (c.X < minX) { minX = c.X; }
                        if (c.X > maxX) { maxX = c.X; }
                        if (c.Y < bottom) { bottom = c.Y; }
                        if (c.Y > top) { top = c.Y; }
                    }
                    var width = maxX - minX;
                    var height = top - bottom;
                    bool[] topArray = new bool[width];
                    bool[] bottomArray = new bool[width];

                    if (!yWraps)
                    {
                        loopAtY = bottom - 1;

                        // connect open coords at top and bottom
                        for (int i = 0; i < width; i++)
                        {
                            int x = i + minX;
                            var topCoord = new Coord(x, top);
                            var bottomCoord = new Coord(x, bottom);
                            var hasTopShape = shapeGrid.TryGetValue(topCoord, out var topShape);
                            //topArray[i] = hasTopShape;
                            var hasBottomShape = shapeGrid.TryGetValue(bottomCoord, out var bottomShape);
                            //bottomArray[i] = hasBottomShape;
                            if (hasTopShape && hasBottomShape)
                            {
                                var topHasUp = topShape.ContainsUp();
                                var bottomHasDown = bottomShape.ContainsDown();
                                if (topHasUp && bottomHasDown)
                                {
                                    // actual wrap!
                                    openCoords.Remove(new(x, top + 1));
                                    openCoords.Remove(new(x, bottom - 1));
                                }
                                else if (topHasUp)
                                {
                                    shapeGrid[topCoord] = topShape.RemoveUp();
                                }
                                else if (bottomHasDown)
                                {
                                    shapeGrid[bottomCoord] = bottomShape.RemoveDown();
                                }
                            }
                            else if (hasTopShape)
                            {
                                shapeGrid[topCoord] = topShape.RemoveUp();
                            }
                            else if (hasBottomShape)
                            {
                                shapeGrid[bottomCoord] = bottomShape.RemoveDown();
                            }
                        }
                        yWraps = true;
                    }

                    // this does compressing... maybe later!
                    if (1 == 0)
                    {
                        Debug.WriteLine("\n" + GetLayoutDebug(shapeGrid, false) + "\n");
                        var topRow = shapeGrid.Where(kvp => kvp.Key.Y == top);
                        var bottomRow = shapeGrid.Where(kvp => kvp.Key.Y == bottom);

                        // TODO: do compressing later
                        int rows = 0;
                        int t = top - rows;
                        int b = bottom + rows;

                        bool overlap = false;
                        while (true)
                        {
                            Array.Fill(topArray, false);
                            Array.Fill(bottomArray, false);

                            if (t - b < 2) { break; }
                            for (int i = 0; i < width; i++)
                            {
                                int x = i + minX;
                                var hasTopShape = topArray[i] || shapeGrid.TryGetValue(new(x, t), out var topShape);
                                topArray[i] = hasTopShape;
                                var hasBottomShape = bottomArray[i] || shapeGrid.TryGetValue(new (x, b), out var bottomShape);
                                bottomArray[i] = hasBottomShape;
                                overlap = hasTopShape && hasBottomShape;
                                if (overlap)
                                {
                                    break;
                                }
                            }

                            // TODO: build into coordinates: wrapTop=..., wrapBottom=... ?
                            // TODO: determine minimum room overlap to connect top+bottom. we're compressing too much now (and not connecting at all)

                            if (overlap)
                            {
                                int finalHeight = top - bottom - rows - 1;
                                int moveUp = rows + 1;
                                for (int j = 0; j <= rows; j++)
                                {
                                    int y = j + top;
                                    for (int i = 0; i < width; i++)
                                    {
                                        int x = i + minX;
                                        Coord topCoord = new(x, y);
                                        Coord bottomCoord = new(x, y - finalHeight);

                                        var hasTopShape = shapeGrid.TryGetValue(topCoord, out var topShape);
                                        var hasBottomShape = shapeGrid.TryGetValue(bottomCoord, out var bottomShape);
                                        if (hasBottomShape || hasTopShape)
                                        {
                                            RoomExitType newShape;
                                            if (hasBottomShape && hasTopShape)
                                            {
                                                newShape = topShape.Merge(bottomShape);
                                            }
                                            else if (hasTopShape)
                                            {
                                                newShape = topShape;
                                            }
                                            else
                                            {
                                                newShape = bottomShape;
                                            }
                                            if (hasTopShape)
                                            {
                                                dequeueFromShape(topCoord, topShape);
                                            }
                                            if (hasBottomShape)
                                            {
                                                dequeueFromShape(bottomCoord, bottomShape);
                                                shapeGrid.Remove(bottomCoord);
                                            }
                                            // make sure new shape can't go up
                                            // well this should only be done if we don't have a "linked up"... how do we mark a looping linked up/down?
                                            //newShape = newShape.RemoveUp();
                                            shapeGrid[topCoord] = newShape;
                                            enqueueFromShape(topCoord, newShape, 4);
                                        }
                                    }
                                }
                                break;
                            }
                            //rows++;
                            break; // making sure 1 iteration works first
                        }

                        yWraps = true;
                        Debug.WriteLine("\n" + GetLayoutDebug(shapeGrid, false) + "\n");
                    }
                }

                if (openCoords.Count == 0) { break; }
                // plug open coords
                coord = openCoords[0];
                openCoords.RemoveAt(0);

                shape = noNewOpeningsShape(coord);
                shapeGrid[coord] = shape;
            }
            Debug.WriteLine("\n" + GetLayoutDebug(shapeGrid, false) + "\n");
        }

        /*
        //Dropify graph
        foreach (Coord coord in walkGraph.Keys.OrderByDescending(i => i.Y).ThenBy(i => i.X))
        {
            await Task.Yield();
            if (!walkGraph.TryGetValue(coord, out RoomExitType exitType))
            {
                throw new ImpossibleException("Walk graph coordinate was explicitly missing");
            }
            int x = coord.X;
            int y = coord.Y;

            RoomExitType? downExitType = null;
            if (walkGraph.ContainsKey(new Coord(x, y - 1)))
            {
                downExitType = walkGraph[new Coord(x, y - 1)];
            }
            else
            {
                continue;
            }
            double dropChance = DROP_CHANCE;
            //if we dropped into this room
            if (walkGraph.TryGetValue(new Coord(x, y + 1), out RoomExitType upRoomType) && upRoomType.ContainsDrop())
            {
                //If There are no drop -> elevator conversion rooms, so if we have to keep going down, it needs to be a drop.
                //we should be roomPool agnostic
                //if (exitType.ContainsDown() && roomPool.GetNormalRoomsForExitType(RoomExitType.DROP_STUB).Any(i => i.IsDropZone))
                dropChance = 1f;
            }
            //if the path doesn't go down, or the room below doesn't exist, or the room below only goes up
            if (!exitType.ContainsDown() || downExitType == null || downExitType == RoomExitType.DEADEND_EXIT_UP)
            {
                dropChance = 0f;
            }

            if (r.NextDouble() < dropChance)
            {
                walkGraph[coord] = exitType.ConvertToDrop();
                walkGraph[new Coord(x, y - 1)] = walkGraph[new Coord(x, y - 1)].RemoveUp();
            }
        }

        //If dropification created a room with no entrance, change it
        foreach (KeyValuePair<Coord, RoomExitType> item in walkGraph.Where(i => i.Value == RoomExitType.DROP_STUB))
        {
            if (!walkGraph.ContainsKey(new Coord(item.Key.X, item.Key.Y + 1)))
            {
                walkGraph[item.Key] = RoomExitType.DEADEND_EXIT_DOWN;
                RoomExitType downRoomType = walkGraph[new Coord(item.Key.X, item.Key.Y - 1)];
                walkGraph[new Coord(item.Key.X, item.Key.Y - 1)] = downRoomType.AddUp();
            }
        }

        //If dropification created a pit, convert it to an elevator.
        //This should never happen, but it's a good safety
        foreach (KeyValuePair<Coord, RoomExitType> item in walkGraph.Where(i => i.Value == RoomExitType.NO_ESCAPE))
        {
            walkGraph[item.Key] = RoomExitType.DEADEND_EXIT_UP;
            RoomExitType upRoomType = walkGraph[new Coord(item.Key.X, item.Key.Y + 1)];
            walkGraph[new Coord(item.Key.X, item.Key.Y + 1)] = upRoomType.ConvertFromDropToDown();
        }
        */
        PalaceShape result = new(shapeGrid);
        result.Top = top;
        result.LoopAtY = loopAtY;
        return result;
    }
}
