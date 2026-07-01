using System;
using Godot;

namespace IdleGodFinger;

public static class BspSplitter
{
    /// <summary>A region must be at least this many tiles wide/tall to be eligible for splitting.</summary>
    private const int MinRegionSize = 7;

    /// <summary>
    /// Build a BSP tree, place rooms in all leaves, and return the root node.
    /// Call <see cref="RoomPlacer.CarveIntoMap"/> afterwards to write the result into a <see cref="Map"/>.
    /// </summary>
    public static BspNode Build(Rect2I bound, Random rng, int maxDepths = 5)
    {
        BspNode root = new BspNode(bound);
        Split(root, rng, depth: 0, maxDepths);
        RoomPlacer.PlaceRooms(root, rng);
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

        // If both are possible, split the longer axis.
        if (region.Size.Y > region.Size.X) return true;
        if (region.Size.X > region.Size.Y) return false;

        // Equal length — choose randomly.
        return rng.NextDouble() < 0.5;
    }
}