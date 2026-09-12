using Godot;
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

	private const int SlotCount = 10;

	private static readonly Key[] HotbarKeys =
	{
		Key.Key1, Key.Key2, Key.Key3, Key.Key4, Key.Key5,
		Key.Key6, Key.Key7, Key.Key8, Key.Key9, Key.Key0
	};

	private List<(int IdBlock, int NumBlocks)> Hotbar => StorageInventory.ListBlocksHotbar;

	public override void _Ready()
	{
		PointerPosX = (int)pointer.Position.X;
		PointerPosY = (int)pointer.Position.Y;
		SetIndex(currentIndex);
	}

	public override void _Process(double delta)
	{
		for (int i = 0; i < HotbarKeys.Length; i++)
		{
			if (Input.IsKeyPressed(HotbarKeys[i]))
			{
				SetIndex(i);
				break;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (Gameplayer.isInventory) return;
		if (@event is not InputEventMouseButton mb || !mb.Pressed) return;

		switch (mb.ButtonIndex)
		{
			case MouseButton.WheelUp:
				SetIndex((currentIndex + 1) % SlotCount);
				break;
			case MouseButton.WheelDown:
				SetIndex((currentIndex - 1 + SlotCount) % SlotCount);
				break;
		}
	}

	private void SetIndex(int index)
	{
		currentIndex = Mathf.Clamp(index, 0, SlotCount - 1);

		// Вид — следствие модели
		pointer.Position = new Vector2(
			PointerPosX + sizePointer * currentIndex,
			PointerPosY);

		if (currentIndex < Hotbar.Count)
		{
			int id = Hotbar[currentIndex].IdBlock;
			currentBlock = id == (int)BlockId.Air ? (int)BlockId.Null : id;
		}
	}
}
