using Godot;
using System.Collections.Generic;

public partial class InventoryDraw : Node
{
	bool oldIsInventory = false;
	bool isInventory = Gameplayer.isInventory;

    [ExportGroup("Map")]
    [Export] public TileMapLayer _map;

    [ExportGroup("Textuire Inventory")]
    [Export] public Sprite2D _texture1;
	[Export] public Sprite2D _texture2;
	[Export] public Texture2D _textureRecovery;

    [ExportGroup("Textuire Bg Inventory")]
    [Export] public Sprite2D _textureBg;
    [Export] public Texture2D _textureBgRecovery;

    List<(int IdBlock, int NumBloks)> listBlocksInventory = StorageInventory.ListBlocksInventory;

	int biasX = StorageInventory.BiasXInventory;
	int biasY = StorageInventory.BiasYInventory;

	int xW = StorageInventory.WightInventory; 
	int yH = StorageInventory.HightInventory;

	public override void _Ready()
	{
		_texture1.Texture = null;
		_texture2.Texture = null;
		_textureBg.Texture = null;
	}

	public override void _Process(double delta)
	{
		isInventory = Gameplayer.isInventory;
		listBlocksInventory = StorageInventory.ListBlocksInventory;

		if (isInventory != oldIsInventory)
		{
			if (isInventory)
				Draw();
			else
				Delete();
		}

		if (ReplacementBlocks.redrawing)
			Draw();

        oldIsInventory = isInventory;
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

		_texture1.Texture = _textureRecovery;
		_texture2.Texture = _textureRecovery;
		_textureBg.Texture = _textureBgRecovery;
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
        _textureBg.Texture = null;
    }
}
