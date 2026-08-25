using Godot;
using System;

public partial class Gameplayer : Node2D
{
	[Export] public PackedScene inventoryScene;
	[Export] public Node inventoryContainer;
	private Node inventoryInstance;

	public static bool isInventory = false;

	public override void _Ready()
	{
	}

	// Этот метод автоматически вызывается для необработанных событий ввода
	public override void _UnhandledInput(InputEvent @event)
	{
		// Проверяем, является ли событие нажатием клавиши
		if (@event is InputEventKey eventKey)
		{
			// Проверяем, была ли нажата клавиша Escape
			if (eventKey.Pressed && eventKey.Keycode == Key.Escape)
			{
				// Выходим из игры
				GetTree().Quit();
				// Помечаем событие как обработанное, чтобы оно не пошло дальше
				GetViewport().SetInputAsHandled();
			}
		}
	}
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			// Клавиша E - открыть инвентарь
			if (keyEvent.Keycode == Key.E)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				OpenInventory();
				isInventory = true;
			}

			// Клавиша Escape - закрыть инвентарь
			if (keyEvent.Keycode == Key.R)
			{
				Input.MouseMode = Input.MouseModeEnum.Hidden;
				CloseInventory();
				isInventory = false;
			}
		}
	}

	private void OpenInventory()
	{
		// Проверяем, что контейнер назначен
		if (inventoryContainer == null)
		{
			GD.PrintErr("Контейнер для инвентаря не назначен в инспекторе!");
			return;
		}

		// Проверяем, что инвентарь еще не открыт
		if (inventoryInstance != null && IsInstanceValid(inventoryInstance))
		{
			GD.Print("Инвентарь уже открыт");
			return;
		}

		// Создаем экземпляр сцены
		inventoryInstance = inventoryScene.Instantiate();

		// Добавляем в контейнер
		inventoryContainer.AddChild(inventoryInstance);

		GD.Print($"Инвентарь добавлен в {inventoryContainer.Name}");
	}

	private void CloseInventory()
	{
		if (inventoryInstance != null && IsInstanceValid(inventoryInstance))
		{
			inventoryInstance.QueueFree();
			inventoryInstance = null;
			GD.Print("Инвентарь закрыт");
		}
		else
		{
			GD.Print("Инвентарь не открыт");
		}
	}
}
