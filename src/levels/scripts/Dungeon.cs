using System;
using Godot;
namespace IdleGodFinger;

public partial class Dungeon : Node
{
	[Export] public int Width = 100;
	[Export] public int Height = 100;
	[Export] public int MaxDepth = 4; // higher = more rooms, smaller rooms
	[Export] public int Seed = 0;    // 0 = random each run
	[Export] public DungeonPainter Painter = null!;

	[Signal] public delegate void DungeonGeneratedEventHandler(Map map);

	public override void _Ready()
	{
		Random rng = Seed == 0 ? new Random() : new Random(Seed);
		Map map = new Map(Width, Height);

		Rect2I bounds = new Rect2I(0, 0, Width, Height);
		BspNode root = BspSplitter.Build(bounds, rng, MaxDepth);
		BspSplitter.CarveIntoMap(root, map);

		Painter.Paint(map);
		EmitSignal(SignalName.DungeonGenerated, map);
	}
}
