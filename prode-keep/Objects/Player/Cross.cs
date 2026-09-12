using Godot;

public partial class Cross : Sprite2D
{
	[Export] private float _followSpeed = 30.0f;
	[Export] private float _deadZone = 4.0f;

	public override void _Process(double delta)
	{
		if (Gameplayer.isInventory) return;

		Vector2 target = GetGlobalMousePosition();

		// Если прицел уже в пределах dead zone — не двигаем
		if (GlobalPosition.DistanceSquaredTo(target) < _deadZone * _deadZone)
			return;

		// Плавно догоняем (не зависит от FPS)
		float t = 1f - Mathf.Exp(-_followSpeed * (float)delta);
		GlobalPosition = GlobalPosition.Lerp(target, t);
	}
}
