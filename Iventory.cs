using Godot;
using System;

using System.Collections.Concurrent;
using System.Collections.Generic;

using BlockId = Storage.BlockId;

public partial class Iventory : TileMapLayer
{
	List<int> listBlocks = new List<int>();
	List<int> listBlocksOld = new List<int>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		for (int i = 0; i < 9; i++)
		{
			listBlocksOld.Add((int)BlockId.Null);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		listBlocks = IventoryNode.listBlocks;

		for (int i = 0; i < 9; i++)
		{
			if (listBlocks[i] != listBlocksOld[i])
			{
				SetCell(new Vector2I(i, 0), 0, new Vector2I(listBlocks[i] % 10, listBlocks[i] / 10));
			}
		}

		listBlocksOld.Clear();
		listBlocksOld.AddRange(listBlocks);
	}
}
