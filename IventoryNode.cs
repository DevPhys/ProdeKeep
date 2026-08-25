using Godot;
using System;

using System.Collections.Concurrent;
using System.Collections.Generic;

public partial class IventoryNode : Node2D
{
	[Export] public Sprite2D pointer;

	int iventoryWidth = 144;
	int sizePointer = 16;

	public static int currentBlock;
	public static List<int> listBlocks = new List<int>();

	public static int spawnX;
	public static int spawnY;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		int screenWidth = (int)DisplayServer.WindowGetSize().X;
		int screenHeight = (int)DisplayServer.WindowGetSize().Y;

		spawnX = (int)(screenWidth / 4 - iventoryWidth / 2);
		spawnY = 600;

		Position = new Vector2(spawnX, spawnY);

		for (int i = 0;i < 9; i++)
		{
			listBlocks.Add(i + 1);
		}

		currentBlock = listBlocks[0];
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Key1))
		{
			currentBlock = listBlocks[0];
			pointer.Position = new Vector2(0, 0);
		}
		else if (Input.IsKeyPressed(Key.Key2))
		{
			currentBlock = listBlocks[1];
			pointer.Position = new Vector2(sizePointer * 1, 0);
		}
		else if (Input.IsKeyPressed(Key.Key3))
		{
			currentBlock = listBlocks[2];
			pointer.Position = new Vector2(sizePointer * 2, 0);
		}
		else if (Input.IsKeyPressed(Key.Key4))
		{
			currentBlock = listBlocks[3];
			pointer.Position = new Vector2(sizePointer * 3, 0);
		}
		else if (Input.IsKeyPressed(Key.Key5))
		{
			currentBlock = listBlocks[4];
			pointer.Position = new Vector2(sizePointer * 4, 0);
		}
		else if (Input.IsKeyPressed(Key.Key6))
		{
			currentBlock = listBlocks[5];
			pointer.Position = new Vector2(sizePointer * 5, 0);
		}
		else if (Input.IsKeyPressed(Key.Key7))
		{
			currentBlock = listBlocks[6];
			pointer.Position = new Vector2(sizePointer * 6, 0);
		}
		else if (Input.IsKeyPressed(Key.Key8))
		{
			currentBlock = listBlocks[7];
			pointer.Position = new Vector2(sizePointer * 7, 0);
		}
		else if (Input.IsKeyPressed(Key.Key9))
		{
			currentBlock = listBlocks[8];
			pointer.Position = new Vector2(sizePointer * 8, 0);
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Проверяем, является ли событие событием кнопки мыши
		if (@event is InputEventMouseButton mouseButton && !Gameplayer.isInventory)
		{
			int posPointerX = (int)pointer.Position.X;
			int index = 0;

			if (posPointerX != 0)
				index = posPointerX / sizePointer;

			// Проверяем, нажата ли кнопка
			if (mouseButton.Pressed)
			{
				// Проверяем индекс кнопки
				if (mouseButton.ButtonIndex == MouseButton.WheelUp && index < 8)  // Прокрутка вверх
				{
					pointer.Position = new Vector2(posPointerX + sizePointer, 0);
					currentBlock = listBlocks[index + 1];
				}
				else if (mouseButton.ButtonIndex == MouseButton.WheelDown && index > 0)  // Прокрутка вниз
				{
					pointer.Position = new Vector2(posPointerX - sizePointer, 0);
					currentBlock = listBlocks[index - 1];
				}
			}
		}
	}
}
