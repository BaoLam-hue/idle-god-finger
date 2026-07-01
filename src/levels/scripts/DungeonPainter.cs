using Godot;
namespace IdleGodFinger;

public partial class DungeonPainter : Node
{
    [Export] public TileMapLayer FloorLayer = null!;
    [Export] public TileMapLayer WallLayer = null!;

    public void Paint(Map map)
    {
        FloorLayer.Clear();
        WallLayer.Clear();

        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                TileType type = map.GetTile(x, y);
                Vector2I cell = new Vector2I(x, y);
                Vector2I atlasCoordinate = TileAtlas.GetAtlas(type);

                // Walls and Void go on the wall layer; everything else on the floor layer
                if (type is TileType.Wall or TileType.Void)
                    WallLayer.SetCell(cell, TileAtlas.SourceId, atlasCoordinate);
                else
                    FloorLayer.SetCell(cell, TileAtlas.SourceId, atlasCoordinate);
            }
        }
    }

    public void UpdateCell(Map map, Vector2I position)
    {
        var type = map.GetTile(position);
        var atlasCoordinate = TileAtlas.GetAtlas(type);

        FloorLayer.EraseCell(position);
        WallLayer.EraseCell(position);

        if (type is TileType.Wall or TileType.Void)
            WallLayer.SetCell(position, TileAtlas.SourceId, atlasCoordinate);
        else
            FloorLayer.SetCell(position, TileAtlas.SourceId, atlasCoordinate);
    }
}