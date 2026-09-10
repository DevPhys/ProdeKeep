using Godot;
using System;

using BlockId = Storage.BlockId;

public partial class ReplacementBlocks : Node
{
	[Export] public TileMapLayer _map;
	[Export] public Sprite2D _block;
	[Export] public Label _numBlock;

	[Export] public int startXInventory = 13;
	[Export] public int endXInventory = 33;
	[Export] public int startYInventory = 9;
	[Export] public int endYInventory = 17;

	[Export] public int startXHotbar = 18;
	[Export] public int endXHotbar = 28;
	[Export] public int yHotbar = 23;

	int idBlock; int numBlocks;

	int biasX = Storage.BiasXInventory; 
	int biasY = Storage.BiasYInventory;
	int hightInventory = Storage.HightInventory;

	public static bool redrawing = false;

	int timeRedrawing = 2;
	int storageDevice = 0;

	Vector2I tileCoordsOld;

	public override void _Process(double delta)
	{
		if (!redrawing) return;

		if (storageDevice == timeRedrawing)
			redrawing = false;
		else
			storageDevice++;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && Gameplayer.isInventory)
		{
			Vector2 mouseScreenPos = GetViewport().GetMousePosition();
			Vector2I tileCoords = _map.LocalToMap(_map.ToLocal(mouseScreenPos));

			if (mouseButton.ButtonIndex == MouseButton.Left && _block.Texture == null)
			{
				var main = GetTextureAtCell(tileCoords);

				if (main.blockId == (int)BlockId.Null)
				{
					_block.Texture = null;
					idBlock = (int)BlockId.Null;

					return; // клик по пустой клетке
				}

				Re_recording(tileCoords, (int)BlockId.Null, 0);

				redrawing = true;
				storageDevice = 0;

				_block.Texture = main.Texture;
				if (numBlocks > 1)
					_numBlock.Text = $"{numBlocks}";
				idBlock = main.blockId;

				tileCoordsOld = tileCoords;
			}

			else if (mouseButton.ButtonIndex == MouseButton.Left && _block.Texture != null)
			{
				Vector2I atlasCoords = _map.GetCellAtlasCoords(tileCoords);
				int blockId = atlasCoords.Y * 10 + atlasCoords.X;

				if (blockId == (int)BlockId.Null)
				{
					Re_recording(tileCoords, idBlock, numBlocks);

					_block.Texture = null;
					_numBlock.Text = "";
					idBlock = (int)BlockId.Null;
				}
				else
				{
					var main = GetTextureAtCell(tileCoords);
					Re_recording(tileCoords, idBlock, numBlocks);

					_block.Texture = main.Texture;
					if (numBlocks > 1)
						_numBlock.Text = $"{numBlocks}";
					idBlock = main.blockId;
				}

				redrawing = true;
				storageDevice = 0;
			}
		}
	}

	private (Texture2D Texture, int blockId) GetTextureAtCell(Vector2I cellCoords)
	{
		TileData tileData = _map.GetCellTileData(cellCoords);

		if (tileData == null) return (null, (int)BlockId.Null);

		int sourceId = _map.GetCellSourceId(cellCoords);
		if (sourceId == (int)BlockId.Null) return (null, (int)BlockId.Null);

		TileSetSource source = _map.TileSet.GetSource(sourceId);

		if (source is TileSetAtlasSource atlasSource)
		{
			Vector2I atlasCoords = _map.GetCellAtlasCoords(cellCoords);

			// Получаем регион в атласе (Rect2I)
			Rect2I region = atlasSource.GetTileTextureRegion(atlasCoords);

			// Создаем AtlasTexture с нужным регионом
			AtlasTexture atlasTexture = new AtlasTexture();
			atlasTexture.Atlas = atlasSource.Texture;
			atlasTexture.Region = region;

			int blockId = atlasCoords.Y * 10 + atlasCoords.X;
			return (atlasTexture, blockId);
		}

		return (null, (int)BlockId.Null);
	}
	private void Re_recording(Vector2I cellCoords, int BlockId, int NumBlocks)
	{
		int localX = cellCoords.X;
		int localY = cellCoords.Y;

		if (localX >= startXInventory &&
			localX <= endXInventory &&
			localY >= startYInventory &&
			localY <= endYInventory)
		{
			int index = (localX - biasX) * hightInventory + (localY - biasY);
			numBlocks = Storage.ListBlocksInventory[index].NumBlocks;

			(int IdBlock, int NumBloks) mainBlock = (BlockId, NumBlocks);
			Storage.ListBlocksInventory[index] = mainBlock;

			return;
		}
		else if (localX >= startXHotbar &&
				 localX <= endXHotbar &&
				 localY == yHotbar)
		{
			int index = localX - startXHotbar;
			numBlocks = Storage.ListBlocksHotbar[index].NumBlocks;

			(int IdBlock, int NumBloks) mainBlock = (BlockId, NumBlocks);
			Storage.ListBlocksHotbar[index] = mainBlock;
			HotbarPointer.currentBlock = Storage.ListBlocksHotbar[HotbarPointer.currentIndex].IdBlock;

			return;
		}

		numBlocks = 0;
	}
}
