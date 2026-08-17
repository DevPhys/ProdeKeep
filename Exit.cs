using Godot;
using System;

public partial class Exit : Node2D
{

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
}
