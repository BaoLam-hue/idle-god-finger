using Godot;

namespace IdleGodFinger;

public partial class Map : RefCounted
{
    public int Width { get; }
    public int Height { get; }
    private readonly TileType[,] _tileGrid;
    public Map(int width, int height)
    {
        Width = width;
        Height = height;
        _tileGrid = new TileType[width, height];
    }
    public TileType GetTile(int x, int y) => _tileGrid[x, y];
    public TileType GetTile(Vector2I position) => _tileGrid[position.X, position.Y];
    public void SetTile(int x, int y, TileType type) => _tileGrid[x, y] = type;
    public void SetTile(Vector2I position, TileType type) => _tileGrid[position.X, position.Y] = type;

    public bool IsInBound(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height;

    public bool IsWalkAble(Vector2I position) =>
        GetTile(position) is TileType.Floor;
}