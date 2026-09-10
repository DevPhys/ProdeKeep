using Godot;
using System;

public partial class BlockMous : Sprite2D
{
	[Export] private float _smoothSpeed = 20.0f; // Скорость сглаживания

	public override void _Process(double delta)
	{
		Vector2 targetPosition = GetGlobalMousePosition();

		// Сглаживание с учётом delta (не зависит от FPS)
		GlobalPosition = GlobalPosition.Lerp(targetPosition, _smoothSpeed * (float)delta);
	}
}
