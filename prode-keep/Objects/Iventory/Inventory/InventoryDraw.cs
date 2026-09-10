using Godot;
using System.Collections.Generic;

public partial class InventoryDraw : Node
{
	bool oldIsInventory = false;
	bool isInventory = Gameplayer.isInventory;

	[Export] public TileMapLayer _map;
	[Export] public Sprite2D _texture1;
	[Export] public Sprite2D _texture2;
	[Export] public Texture2D _textureBg;

	List<(int IdBlock, int NumBloks)> listBlocksInventory = Storage.ListBlocksInventory;

	int biasX = Storage.BiasXInventory;
	int biasY = Storage.BiasYInventory;

	int xW = Storage.WightInventory; 
	int yH = Storage.HightInventory;

	public override void _Ready()
	{
		_texture1.Texture = null;
		_texture2.Texture = null;
	}

	public override void _Process(double delta)
	{
		isInventory = Gameplayer.isInventory;
		listBlocksInventory = Storage.ListBlocksInventory;

		if (isInventory != oldIsInventory)
		{
			if (isInventory)
				Draw();
			else
				Delete();
		}

		if (ReplacementBlocks.redrawing)
			Draw();

		oldIsInventory = Gameplayer.isInventory;
	}

	private void Draw()
	{
		for (int x = 0; x < xW; x++)
		{
			for (int y = 0; y < yH; y++)
			{
				int i = x * yH + y;
				_map.SetCell(new Vector2I(x + biasX, y + biasY), 0, new Vector2I(listBlocksInventory[i].IdBlock % 10, listBlocksInventory[i].IdBlock / 10));
			}
		}

		_texture1.Texture = _textureBg;
		_texture2.Texture = _textureBg;
	}
	private void Delete()
	{
		for (int x = 0; x < xW; x++)
		{
			for (int y = 0; y < yH; y++)
			{
				int i = x * yH + y;
				_map.EraseCell(new Vector2I(x + biasX, y + biasY));
			}
		}

		_texture1.Texture = null;
		_texture2.Texture = null;
	}
}
