using Godot;
using System;

using System.Collections.Concurrent;
using System.Collections.Generic;

public partial class InventoryGlobal : TileMapLayer
{
	List<byte> listInventory = Storage.BlocksForInventory;
	int linNumX = 0;
	int linNumY = 1;

	int linNumXLimit = 9;
	int linNumYLimit = 8;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		linNumX = 0;
		linNumY = 1;

		for (int i = 0; i < listInventory.Count; i++)
		{
			if (linNumX > linNumXLimit)
			{
				linNumX = 0;
				linNumY++;
			}

			int tileId = listInventory[i];
			SetCell(new Vector2I(linNumX, linNumY), 0, new Vector2I(tileId % 10, tileId / 10));

			linNumX++;
		}
	}
}
