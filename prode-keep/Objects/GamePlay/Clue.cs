using Godot;
using System;

public partial class Clue : Label
{
	bool isInventory;
	bool isClue = false;

	string clueGame = "" +
		"E - open/close inventory\n" +
		"A - running left\n" +
		"D - running right\n" +
		"W - Jump\n" +
		"ESC / Сross / Alt + F4 - exit\n" +
		"Wheel scroll / keys 1-9 - \n" +
		"replacing a block in the \n" +
		"Hotbar\n\n" +
		"RMB - place a block" +
		"\nLMB - destroy the block";
	string clueInventory = "" +
		"RMB - replace a block\n" +
		"in the Hotbar / \n" +
		"select a block\n" +
		"in the inventory";


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Visible = false;
	}

	public override void _Input(InputEvent ev)
	{
		if (ev is InputEventKey keyEvent && keyEvent.Pressed)
		{
			isInventory = Gameplayer.isInventory;

			if (keyEvent.Keycode == Key.V && !isClue)
			{
				if (isInventory)
				{
					Text = clueInventory;
				}
				else if (!isInventory) 
				{
					Text = clueGame;
				}

				Visible = true;
				isClue = true;
			}
			else if (keyEvent.Keycode == Key.V && isClue)
			{
				Visible = false;
				isClue = false;
			}
		}
	}
}
