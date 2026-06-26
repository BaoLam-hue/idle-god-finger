using Godot;
namespace IdleGodFinger;

public partial class Dungeon : Node
{
	[Export] public int Width = 10;
	[Export] public int Height = 10;
	[Export] public DungeonPainter Painter = null!;

	[Signal] public delegate void DungeonGeneratedEventHandler(Map map);

	public override void _Ready()
	{
		var map = new Map(Width, Height);

		// place the box code here
		for (int x = 0; x < Width; x++)
		{
			for (int y = 0; y < Height; y++)
			{
				bool isBorder = x == 0 || x == Width - 1 || y == 0 || y == Height - 1;
				map.SetTile(x, y, isBorder ? TileType.Wall : TileType.Floor);
			}
		}
		Painter.Paint(map);

		EmitSignal(SignalName.DungeonGenerated, map);
	}
}
