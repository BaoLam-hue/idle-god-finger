using Godot;

public partial class DungeonGenerator : Node
{
	public enum TileType
	{
		Wall = 0,   // impassable; default for uninitialised cells
		Floor = 1,  // walkable
		Trap = 2,   // walkable; damages hero once then disappears
	}
}
