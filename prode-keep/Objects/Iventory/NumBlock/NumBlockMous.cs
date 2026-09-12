using Godot;
using System;

public partial class NumBlockMous : Label
{
	[Export] private float _smoothSpeed = 20.0f; // Скорость сглаживания
	int bais = 6;

	public override void _Process(double delta)
	{
		Vector2 targetPosition = GetGlobalMousePosition();

		float t = 1f - Mathf.Exp(-_smoothSpeed * (float)delta);
		GlobalPosition = GlobalPosition.Lerp(targetPosition, t);

		if (!Gameplayer.isInventory && !string.IsNullOrEmpty(Text))
			Text = "";
	}
}
