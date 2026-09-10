using Godot;
using System;

using System.Collections.Concurrent;
using System.Collections.Generic;

using BlockId = Storage.BlockId;

public partial class HotbarPointer : Node
{
	[Export] public Sprite2D pointer;
	[Export] public int sizePointer = 26;

	[Export] public int PointerPosY = 592;
	[Export] public int PointerPosX = 488;

	public static int currentBlock;
	public static int currentIndex = 0;

	List<(int IdBlock, int NumBloks)> listBlocksHotbar = Storage.ListBlocksHotbar;

	private static readonly Dictionary<Key, int> HotbarKeys = new()
	{
		{ Key.Key1, 0 }, { Key.Key2, 1 }, { Key.Key3, 2 }, { Key.Key4, 3 }, { Key.Key5, 4 },
		{ Key.Key6, 5 }, { Key.Key7, 6 }, { Key.Key8, 7 }, { Key.Key9, 8 }, { Key.Key0, 9 }
	};

	public override void _Ready()
	{
		PointerPosX = (int)pointer.Position.X;
		PointerPosY = (int)pointer.Position.Y;
	}

	public override void _Process(double delta)
	{
		listBlocksHotbar = Storage.ListBlocksHotbar;
		if (currentBlock == null)
			currentBlock = listBlocksHotbar[0].IdBlock;
		if (currentBlock == (int)BlockId.Air)
			currentBlock = (int)BlockId.Null;

		foreach (var pair in HotbarKeys)
		{
			if (Input.IsKeyPressed(pair.Key))
			{
				currentIndex = pair.Value;

				currentBlock = listBlocksHotbar[currentIndex].IdBlock;
				pointer.Position = new Vector2(sizePointer * pair.Value + PointerPosX, PointerPosY);
				break;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Проверяем, является ли событие событием кнопки мыши
		if (@event is InputEventMouseButton mouseButton && !Gameplayer.isInventory)
		{
			int posPointerX = (int)pointer.Position.X;
			int maxIndex = 9; // максимальный индекс (0-9)
			int basePointerX = PointerPosX;
			int index = 0;

			// Вычитаем базовую позицию
			int relativeX = posPointerX - basePointerX;

			if (relativeX < 0)
				index = 0;
			else
				index = Math.Clamp(relativeX / sizePointer, 0, maxIndex);

			currentIndex = index;

			// Проверяем, нажата ли кнопка
			if (mouseButton.Pressed)
			{
				// Проверяем индекс кнопки
				if (mouseButton.ButtonIndex == MouseButton.WheelUp && index < 9 && index < listBlocksHotbar.Count)  // Прокрутка вверх
				{
					pointer.Position = new Vector2(posPointerX + sizePointer, PointerPosY);
					currentBlock = listBlocksHotbar[index + 1].IdBlock;
				}
				else if (mouseButton.ButtonIndex == MouseButton.WheelUp && index == 9)
				{
					pointer.Position = new Vector2(basePointerX, PointerPosY);
					currentBlock = listBlocksHotbar[0].IdBlock;
				}

				else if (mouseButton.ButtonIndex == MouseButton.WheelDown && index > 0)  // Прокрутка вниз
				{
					pointer.Position = new Vector2(posPointerX - sizePointer, PointerPosY);
					currentBlock = listBlocksHotbar[index - 1].IdBlock;  //
				}
				else if (mouseButton.ButtonIndex == MouseButton.WheelDown && index == 0)
				{
					pointer.Position = new Vector2(basePointerX + sizePointer * 9, PointerPosY);
					currentBlock = listBlocksHotbar[9].IdBlock;  //
				}
			}
		}
	}
}
