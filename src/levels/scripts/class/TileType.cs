namespace IdleGodFinger;

public enum TileType
{
    Void = 0,   // outside dungeon bounds — impassable
    Wall = 1,   // solid — impassable
    Floor = 2,   // walkable
    Entrance = 3,
    Exit = 4,
}