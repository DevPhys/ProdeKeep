using Godot;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Gameplayer : Node2D  // Корневой узел сцены
{
	[Export] public Player _player;

	[Export] public int _fpsLimit = 60;
	[Export] public bool _isDisplayServer = false;

	public static bool isInventory = false;

	public override void _Ready()
	{
		if (_isDisplayServer)
			DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
		Engine.MaxFps = _fpsLimit;
	}

	// Этот метод автоматически вызывается для необработанных событий ввода
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey eventKey && eventKey.Pressed)
		{
			bool isEscape = eventKey.Keycode == Key.Escape;
			bool isAltF4 = eventKey.Keycode == Key.F4 && eventKey.AltPressed;

			if (isEscape || isAltF4)
			{
				ExitGame();
				GetViewport().SetInputAsHandled();
			}
		}
	}
	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			ExitGame();
		}
	}
	private void ExitGame()
	{
		LoadAndSave.Save(_player.GlobalPosition);
		GetTree().Quit();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			// Клавиша E - открыть инвентарь
			if (keyEvent.Keycode == Key.E && !isInventory)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				isInventory = true;
			}

			// Клавиша E/Escape - закрыть инвентарь
			else if (keyEvent.Keycode == Key.E && isInventory)
			{
				Input.MouseMode = Input.MouseModeEnum.Hidden;
				isInventory = false;
			}
		}
	}
}
