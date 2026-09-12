using Godot;

public partial class CameraPlayer : Camera2D
{
	[Export] private float _followSpeed = 5.0f;
	[Export] public CharacterBody2D _player;

	public override void _Ready()
	{
		LimitLeft = 0;
		LimitTop = 0;
		LimitRight = Storage.WorldSizeBlocks * 16;
		LimitBottom = Storage.WorldH * 16;

		if (_player != null)
			Position = _player.GlobalPosition.Round();
	}

	public override void _Process(double delta)
	{
		if (_player == null) return;

		// Экспоненциальное сглаживание — не зависит от FPS
		float t = 1f - Mathf.Exp(-_followSpeed * (float)delta);
		Position = Position.Lerp(_player.GlobalPosition, t).Round();
	}
}
