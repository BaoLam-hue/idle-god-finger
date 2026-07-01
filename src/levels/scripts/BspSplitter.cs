using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Godot;
using IdleGodFinger;

public class BspSplitter
{
    /// <summary>A region must be at least this many tiles wide/tall to be eligible for splitting.</summary>
    private const int MinRegionSize = 7;

    /// <summary>Smallest room that can be carved inside a leaf region.</summary>
    private const int MinRoomSize = 4;

    /// <summary>Tiles of breathing room between a room edge and its region edge.</summary>
    private const int RoomPadding = 1;

    /// <summary>
    /// Build a BSP tree, place rooms in all leaves, and return the root node.
    /// Call <see cref="CarveIntoMap"/> afterwards to write the result into a <see cref="Map"/>.
    /// </summary>
    public static BspNode Build(Rect2I bound, Random rng, int maxDepths = 5)
    {
        BspNode root = new BspNode(bound);
        Split(root, rng, depth: 0, maxDepths);
        PlaceRooms(root, rng);
        return root;
    }

    private static void Split(BspNode node, Random rng, int depth, int maxDepth)
    {
        if (depth >= maxDepth) return;

        bool canSplitV = node.Region.Size.X >= MinRegionSize * 2;
        bool canSplitH = node.Region.Size.Y >= MinRegionSize * 2;

        if (!canSplitH && !canSplitV) return;

        bool splitHorizontally = ChooseSplitAxis(node.Region, canSplitH, canSplitV, rng);

        if (splitHorizontally)
            SplitHorizontal(node, rng);
        else
            SplitVertical(node, rng);

        Split(node.Left, rng, depth + 1, maxDepth);
        Split(node.Right, rng, depth + 1, maxDepth);
    }

    private static void SplitHorizontal(BspNode node, Random rng)
    {
        int splitMin = node.Region.Position.Y + MinRegionSize;
        int splitMax = node.Region.End.Y - MinRegionSize;
        if (splitMin >= splitMax) return;

        int cut = rng.Next(splitMin, splitMax);

        node.Left = new BspNode(new Rect2I(
            node.Region.Position.X, node.Region.Position.Y,
            node.Region.Size.X, cut - node.Region.Position.Y));

        node.Right = new BspNode(new Rect2I(
            node.Region.Position.X, cut,
            node.Region.Size.X, node.Region.End.Y - cut));
    }

    private static void SplitVertical(BspNode node, Random rng)
    {
        int splitMin = node.Region.Position.X + MinRegionSize;
        int splitMax = node.Region.End.X - MinRegionSize;
        if (splitMin >= splitMax) return;

        int cut = rng.Next(splitMin, splitMax);

        node.Left = new BspNode(new Rect2I(
            node.Region.Position.X, node.Region.Position.Y,
            cut - node.Region.Position.X, node.Region.Size.Y));

        node.Right = new BspNode(new Rect2I(
            cut, node.Region.Position.Y,
            node.Region.End.X - cut, node.Region.Size.Y));
    }

    private static bool ChooseSplitAxis(Rect2I region, bool canSplitH, bool canSplitV, Random rng)
    {
        if (canSplitH && !canSplitV) return true;
        if (!canSplitH && canSplitV) return false;
        // if both is possible to split, split the longer one
        if (region.Size.Y > region.Size.X) return true;
        if (region.Size.X > region.Size.Y) return false;
        // and are equal to each other in length, chooses randomly.
        return rng.NextDouble() < 0.5;
    }

    private static void PlaceRooms(BspNode node, Random rng)
    {
        if (node.IsLeaf)
        {
            node.Room = PickRoom(node.Region, rng);
            return;
        }

        if (node.Left != null) PlaceRooms(node.Left, rng);
        if (node.Right != null) PlaceRooms(node.Right, rng);
    }

    private static Rect2I PickRoom(Rect2I region, Random rng)
    {
        int innerX = region.Position.X + RoomPadding;
        int innerY = region.Position.Y + RoomPadding;
        int innerWidth = region.Size.X - RoomPadding * 2;
        int innerHeight = region.Size.Y - RoomPadding * 2;

        // If region is barely big enough, just use the inner area.
        if (innerWidth < MinRoomSize || innerHeight < MinRoomSize)
            return new Rect2I(innerX, innerY, Math.Max(1, innerWidth), Math.Max(1, innerHeight)); // Math.Max() for safe guarding against negative width or height.

        int roomWidth = rng.Next(MinRoomSize, innerWidth + 1);
        int roomHeight = rng.Next(MinRoomSize, innerHeight + 1);

        // Randomizes the room's position within the available inner space.
        int offsetX = rng.Next(0, innerWidth - roomWidth + 1);
        int offsetY = rng.Next(0, innerHeight - roomHeight + 1);

        return new Rect2I(innerX + offsetX, innerY + offsetY, roomWidth, roomHeight);
    }

    public static void CarveIntoMap(BspNode root, Map map)
    {
        // Fill with void
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                map.SetTile(x, y, TileType.Void);
            }
        }

        // Carve rooms and corridors as floor
        foreach (var leaf in root.Leaves())
        {
            CarveRect(leaf.Room, map);
        }

        ConnectChildren(root, map);

        // Second pass — any void tile touching a floor tile becomes a wall
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (map.GetTile(x, y) == TileType.Void && IsAdjacentToFloor(x, y, map))
                {
                    map.SetTile(x, y, TileType.Wall);
                }
            }
        }
    }

    private static bool IsAdjacentToFloor(int x, int y, Map map)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (map.IsInBound(x + dx, y + dy) && map.GetTile(x + dx, y + dy) == TileType.Floor) return true;
            }

        return false;
    }

    private static void CarveRect(Rect2I rect, Map map)
    {
        for (int x = rect.Position.X; x < rect.End.X; x++)
            for (int y = rect.Position.Y; y < rect.End.Y; y++)
                if (map.IsInBound(x, y))
                    map.SetTile(x, y, TileType.Floor);
    }

    private static void ConnectChildren(BspNode node, Map map)
    {
        if (node.IsLeaf) return;

        // post order: children are connected first
        if (node.Left != null) ConnectChildren(node.Left, map);
        if (node.Right != null) ConnectChildren(node.Right, map);

        // Connect this node's two children by finding the nearest leaf centre in each.
        Vector2I start = LeafCenter(node.Left);
        Vector2I end = LeafCenter(node.Right);
        CarveCorridor(start, end, map);
    }

    /// <summary>
    /// Walk down to a leaf node and return its room centre.
    /// Prefers Left children for determinism.
    /// </summary>
    private static Vector2I LeafCenter(BspNode node)
    {
        BspNode current = node;
        while (!current.IsLeaf)
            current = current.Left ?? current.Right!;

        return new Vector2I(
            current.Room.Position.X + current.Room.Size.X / 2,
            current.Room.Position.Y + current.Room.Size.Y / 2);
    }

    private static void CarveCorridor(Vector2I start, Vector2I end, Map map)
    {
        // Horizontal leg
        int xStart = Math.Min(start.X, end.X);
        int xEnd = Math.Max(start.X, end.X);
        for (int x = xStart; x <= xEnd; x++)
        {
            if (map.IsInBound(x, start.Y))
            {
                map.SetTile(x, start.Y, TileType.Floor);
            }
        }

        // Vertical leg
        int yStart = Math.Min(start.Y, end.Y);
        int yEnd = Math.Max(start.Y, end.Y);
        for (int y = yStart; y <= yEnd; y++)
        {
            if (map.IsInBound(end.X, y))
            {
                map.SetTile(end.X, y, TileType.Floor);
            }
        }
    }
}