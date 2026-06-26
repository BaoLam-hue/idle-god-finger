// src/levels/GridUtils.cs
using Godot;

namespace IdleGodFinger;

public static class GridUtils
{
    public const int TileSize = 24;

    /// <summary>Grid cell → world-space centre of that tile.</summary>
    public static Vector2 GridToWorld(Vector2I cell) =>
        new Vector2(cell.X * TileSize + TileSize / 2f,
                    cell.Y * TileSize + TileSize / 2f);

    /// <summary>World position → grid cell (floors correctly for negative coords too).</summary>
    public static Vector2I WorldToGrid(Vector2 world) =>
        new Vector2I(Mathf.FloorToInt(world.X / TileSize),
                     Mathf.FloorToInt(world.Y / TileSize));
}