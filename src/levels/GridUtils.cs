using Godot;

public static class GridUtils
{
    public static Vector2I WorldToGrid(Vector2 worldPos, Vector2 origin)
    {
        int x = Mathf.FloorToInt((worldPos.X - origin.X) / GameConstants.TileSize);
        int y = Mathf.FloorToInt((worldPos.Y - origin.Y) / GameConstants.TileSize);
        return new Vector2I(x, y);
    }

    public static Vector2 GridToWorld(Vector2I tile, Vector2 origin)
    {
        float x = origin.X + tile.X * GameConstants.TileSize + GameConstants.TileSize / 2f;
        float y = origin.Y + tile.Y * GameConstants.TileSize + GameConstants.TileSize / 2f;
        return new Vector2(x, y);
    }
}