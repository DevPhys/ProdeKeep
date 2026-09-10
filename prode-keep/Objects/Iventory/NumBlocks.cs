using Godot;
using System.Collections.Generic;

public partial class NumBlocks : Node2D
{
	[Export] private float _scaleMap = 1.61f;
	[Export] private int fontSize = 11;
	[Export] private float tileSize = 16.0f;

	private Font font;

	private int heightInventory = Storage.HightInventory; // Высота инвентаря
	List<(int IdBlock, int NumBlocks)> ListBlocksInventory = Storage.ListBlocksInventory;

	bool isDraw = false;
	bool isInventory = Gameplayer.isInventory; bool isInventoryOld = Gameplayer.isInventory;

	public override void _Ready()
	{
		var systemFont = new SystemFont();
		systemFont.FontNames = new string[] { "Arial", "Helvetica", "DejaVu Sans" };
		
		font = systemFont;
		tileSize *= _scaleMap;
	}

	public override void _Process(double delta)
	{
		ListBlocksInventory = Storage.ListBlocksInventory;
		isInventory = Gameplayer.isInventory;

		if (ListBlocksInventory.Count == 0)
		{
			GD.Print($"Список инвенторя пуст. Длина этого списка: {ListBlocksInventory.Count}");
			return;
		}

		if (isInventory != isInventoryOld)
		{
			if (isInventory)
			{
				isDraw = true;
			}
			else
			{
				isDraw = false;
			}
		}

		Refresh();
		isInventoryOld = isInventory;
	}

	public override void _Draw()
	{
		if (!isDraw) return;

		for (int i = 0; i < ListBlocksInventory.Count; i++)
		{
			var item = ListBlocksInventory[i];
			if (item.NumBlocks <= 1) continue;
			
			int x = i / heightInventory;
			int y = i % heightInventory;
			
			var text = item.NumBlocks.ToString();
			var drawPos = new Vector2(
				(x + 0.8f) * tileSize,
				(y + 0.8f) * tileSize
			);
			
			// Основной текст
			DrawString(font, drawPos, text,
				HorizontalAlignment.Right, -1, fontSize, new Color(0, 0, 0));
		}
	}
	
	// Вызов при изменении инвентаря
	public void Refresh()
	{
		QueueRedraw();
	}
}
