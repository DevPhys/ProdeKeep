using Godot;
using System;

using System.Collections.Concurrent;
using System.Collections.Generic;

public partial class Iventory : TileMapLayer
{
	List<int> listBlocks = new List<int>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		listBlocks = IventoryNode.listBlocks;

		for (int i = 0; i < 9; i++)
		{
			SetCell(new Vector2I(i, 0), 0, new Vector2I(listBlocks[i] % 10, listBlocks[i] / 10));
		}
	}
}
