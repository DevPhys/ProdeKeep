using Godot;
using System;

using BlockId = Storage.BlockId;

public partial class InventoryGlobalNode : Node2D
{
	[Export] public TileMapLayer tileMapLayer;
	[Export] public PackedScene inventory;
	[Export] public Sprite2D _block;

	TileMapLayer inventoryMap;

	int widthInventory = 300, heightInventory = 180;
	int spawnX, spawnY;

	int idBlock = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		int screenWidth = (int)DisplayServer.WindowGetSize().X;
		int screenHeight = (int)DisplayServer.WindowGetSize().Y;

		spawnX = (int)(screenWidth / 4 - widthInventory / 2);
		spawnY = 100;

		Position = new Vector2(spawnX, spawnY);

		Node inventoryInstance = inventory.Instantiate();
		inventoryMap = inventoryInstance.GetNode<TileMapLayer>("TileMapLayer");

		inventoryMap.Position = new Vector2(IventoryNode.spawnX, IventoryNode.spawnY);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			// Получаем глобальную позицию мыши
			Vector2 globalMousePos = GetGlobalMousePosition();

			if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				// Получаем родительский узел инвентаря (саму сцену инвентаря)
				Node2D inventoryParent = inventoryMap.GetParent() as Node2D;

				if (inventoryParent != null)
				{
					// Преобразуем глобальные координаты мыши в локальные координаты inventoryMap
					Vector2 localMousePos = inventoryMap.ToLocal(globalMousePos);

					// Определяем ширину одного слота (замените на вашу реальную ширину слота)
					float slotWidth = 32.0f; // или любое другое значение

					// Вычисляем индекс слота
					int slotIndex = Mathf.FloorToInt(localMousePos.X / slotWidth);

					GD.Print($"Slot Index: {slotIndex}, Local Mouse: {localMousePos}");

					// Проверяем, что индекс в допустимом диапазоне
					if (slotIndex >= 0 && slotIndex <= 8 && localMousePos.Y >= 0 && localMousePos.Y < slotWidth)
					{
						Re_recording(new Vector2I(slotIndex, 0));
						_block.Texture = null;
						idBlock = (int)BlockId.Null;
					}
					else
					{
						GD.Print($"Slot out of range: {slotIndex}");
					}
				}
			}

			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				// Преобразуем в локальные координаты TileMapLayer
				Vector2 localMousePos = tileMapLayer.ToLocal(globalMousePos);

				// Получаем координаты тайла
				Vector2I cellCoords = tileMapLayer.LocalToMap(localMousePos);

				//GD.Print($"TileMapLayer Position: {tileMapLayer.Position}");
				//GD.Print($"TileMapLayer GlobalPosition: {tileMapLayer.GlobalPosition}");
				//GD.Print($"TileSize: {tileMapLayer.TileSet.TileSize}");

				var main = GetTextureAtCell(cellCoords);
				if (main.Texture != null && _block != null)
				{
					// Устанавливаем текстуру спрайту
					_block.Texture = main.Texture;
					idBlock = main.blockId;

					//GD.Print($"ID Блока: {idBlock}");
				}
				else
				{
					_block.Texture = null;
					idBlock = (int)BlockId.Null;
				}
			}
		}
	}

	private (Texture2D Texture, int blockId) GetTextureAtCell(Vector2I cellCoords)
	{
		TileData tileData = tileMapLayer.GetCellTileData(cellCoords);

		if (tileData == null) return (null, (int)BlockId.Null);

		int sourceId = tileMapLayer.GetCellSourceId(cellCoords);
		if (sourceId == (int)BlockId.Null) return (null, (int)BlockId.Null);

		TileSetSource source = tileMapLayer.TileSet.GetSource(sourceId);

		if (source is TileSetAtlasSource atlasSource)
		{
			Vector2I atlasCoords = tileMapLayer.GetCellAtlasCoords(cellCoords);

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
	private void Re_recording(Vector2I cellCoords)
	{
		IventoryNode.listBlocks[cellCoords.X] = idBlock;
	}
}
