using Godot;
using System;

using System.Collections.Concurrent;
using System.Collections.Generic;

using BlockId = Storage.BlockId;

public partial class HotbarDraw : Node
{
	List<(int IdBlock, int NumBlocks)> listBlocks = new List<(int IdBlock, int NumBlocks)>();
	List<(int IdBlock, int NumBlocks)> listBlocksOld = new List<(int IdBlock, int NumBlocks)>();

	[Export] public TileMapLayer _map;

	int biasX = Storage.BiasXHotbar;
	int biasY = Storage.BiasYHotbar;

	int numSlots = 10;

	public override void _Ready()
	{
		for (int i = 0; i < numSlots; i++)
		{
			listBlocksOld.Add((-1, 0));
		}
	}

	public override void _Process(double delta)
	{
		listBlocks = Storage.ListBlocksHotbar;

		for (int i = 0; i < numSlots; i++)
		{
			if (listBlocks[i].IdBlock != listBlocksOld[i].IdBlock)
			{
				_map.SetCell(new Vector2I(i + biasX, biasY), 0, new Vector2I(listBlocks[i].IdBlock % 10, listBlocks[i].IdBlock / 10));
			}
			listBlocksOld[i] = listBlocks[i];
		}
	}
}
