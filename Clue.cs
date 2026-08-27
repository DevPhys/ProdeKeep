using Godot;
using System;

public partial class Clue : Label
{
	bool isInventory;
	bool isClue = false;

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
					Text = Storage.ClueInventory;
				}
				else if (!isInventory) 
				{
					Text = Storage.ClueGame;
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
