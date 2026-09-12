using Godot;

public partial class Gameplayer : Node2D  // Корневой узел сцены
{
	[Export] public Player _player;

	[Export] public int _maxFps = 60;
	[Export] public bool _disableVsync = false;

	public static bool isInventory = false;

	public override void _Ready()
	{
		if (_disableVsync)
			DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);

		Engine.MaxFps = Mathf.Max(0, _maxFps);
	}

	// Этот метод автоматически вызывается для необработанных событий ввода
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			bool isEscape = keyEvent.Keycode == Key.Escape;
			bool isAltF4 = keyEvent.Keycode == Key.F4 && keyEvent.AltPressed;

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
		if (_player is not null)
			LoadAndSave.Save(_player.GlobalPosition);
		else
			GD.PushError("ExitGame: _player не назначен, позиция игрока не сохранена");

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

			// Клавиша E - закрыть инвентарь
			else if (keyEvent.Keycode == Key.E && isInventory)
			{
				Input.MouseMode = Input.MouseModeEnum.Hidden;
				isInventory = false;
			}
		}
	}
}
