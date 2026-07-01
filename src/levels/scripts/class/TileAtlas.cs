using System.Collections.Generic;
using Godot;

namespace IdleGodFinger;

public static class TileAtlas
{
    public const int SourceId = 0;

    private static readonly Dictionary<TileType, Vector2I> _map = new()
    {
        [TileType.Void] = new Vector2I(6, 0),
        [TileType.Wall] = new Vector2I(0, 0),
        [TileType.Floor] = new Vector2I(3, 0)
    };

    public static Vector2I GetAtlas(TileType type) => _map[type];
}