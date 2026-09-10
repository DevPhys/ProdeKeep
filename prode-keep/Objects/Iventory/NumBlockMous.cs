using Godot;
using System;

public partial class NumBlockMous : Label
{
	[Export] private float _smoothSpeed = 20.0f; // Скорость сглаживания
	int bais = 6;

	public override void _Process(double delta)
	{
		Vector2 targetPosition = GetGlobalMousePosition() + new Vector2(bais, bais);

		// Сглаживание с учётом delta
		GlobalPosition = GlobalPosition.Lerp(targetPosition, _smoothSpeed * (float)delta);

		if (!Gameplayer.isInventory)
			Text = "";
	}
}
