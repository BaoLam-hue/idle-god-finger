using Godot;

public partial class DungeonGenerator : Node
{
	public enum TileType
	{
		Wall = 0,   // impassable; default for uninitialised cells
		Floor = 1,  // walkable
		Trap = 2,   // walkable; damages hero once then disappears
	}

	private TileType[,] _grid;
	private int _width;
	private int _height;


	// ── Grid access ──────────────────────────────────────────
	public bool IsInBounds(Vector2I tile) =>
		tile.X >= 0 && tile.X < _width &&
		tile.Y >= 0 && tile.Y < _height;

	public TileType GetTile(Vector2I tile)
	{
		if (!IsInBounds(tile)) return TileType.Wall;
		return _grid[tile.X, tile.Y];
	}

	public void SetTile(Vector2I tile, TileType type)
	{
		if (!IsInBounds(tile)) return;
		_grid[tile.X, tile.Y] = type;
	}

	// ── Occupancy query helpers ───────────────────────────────
	public bool IsWalkable(Vector2I tile) =>
		IsInBounds(tile) &&
		GetTile(tile) != TileType.Wall;

	public bool IsWall(Vector2I tile) =>
		!IsInBounds(tile) ||
		GetTile(tile) == TileType.Wall;

	public bool IsTrap(Vector2I tile) =>
		IsInBounds(tile) &&
		GetTile(tile) == TileType.Trap;
}
