using Godot;

public partial class Clue : Label
{
	private const string ClueGame =
		"E - open/close inventory\n" +
		"A - running left\n" +
		"D - running right\n" +
		"W - Jump\n" +
		"ESC / Cross / Alt + F4 - exit\n" +
		"Wheel scroll / keys 1-9 - \n" +
		"replacing a block in the \n" +
		"Hotbar\n\n" +
		"RMB - place a block" +
		"\nLMB - destroy the block";

	private const string ClueInventory =
		"RMB - replace a block\n" +
		"in the Hotbar / \n" +
		"select a block\n" +
		"in the inventory";

	private bool _lastInventoryMode;

	public override void _Ready()
	{
		Visible = false;
		_lastInventoryMode = Gameplayer.isInventory;
	}

	public override void _UnhandledInput(InputEvent ev)
	{
		if (ev is not InputEventKey keyEvent || !keyEvent.Pressed) return;

		// Переключение подсказки
		if (keyEvent.Keycode == Key.V)
		{
			Visible = !Visible;
			_lastInventoryMode = Gameplayer.isInventory;

			if (Visible)
				Text = _lastInventoryMode ? ClueInventory : ClueGame;

			return;
		}

		// Если подсказка видна и режим сменился — обновляем текст
		if (Visible && _lastInventoryMode != Gameplayer.isInventory)
		{
			_lastInventoryMode = Gameplayer.isInventory;
			Text = _lastInventoryMode ? ClueInventory : ClueGame;
		}
	}
}
