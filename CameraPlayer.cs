using Godot;
using System;

public partial class CameraPlayer : Camera2D
{
	[Export] private float _followSpeed = 5.0f;
	
	private CharacterBody2D _player;
	
	private float _worldLeft;
	private float _worldRight;
	private float _worldTop;
	private float _worldBottom;

	public override void _Ready()
	{
		Zoom = Vector2.One;
		
		int tileSize = 16;
		_worldLeft = 0;
		_worldRight = Generation.worldSizeBlocks * tileSize;
		_worldTop = 0;
		_worldBottom = Generation.worldHeightY * tileSize - 1;
		
		// Ищем игрока
		_player = GetParent().GetNodeOrNull<CharacterBody2D>("Player");
		
		if (_player != null)
		{
			Position = _player.GlobalPosition;
			GD.Print($"Камера следит за игроком: {_player.Name}");
		}
		else
		{
			GD.PrintErr("Ошибка: Узел 'Player' не найден!");
		}
		
		Position = Position.Round();
	}

	public override void _Process(double delta)
	{
		if (_player == null) return;
		
		float deltaF = (float)delta;
		
		Vector2 target = _player.GlobalPosition;
		Position = Position.Lerp(target, _followSpeed * deltaF);
		Position = ClampPositionToWorld(Position).Round();
	}
	
	private Vector2 ClampPositionToWorld(Vector2 targetPosition)
	{
		Vector2 viewportSize = GetViewportRect().Size;
		
		float halfVisibleWidth = viewportSize.X / 2;
		float halfVisibleHeight = viewportSize.Y / 2;
		
		float minX = _worldLeft + halfVisibleWidth;
		float maxX = _worldRight - halfVisibleWidth;
		float minY = _worldTop + halfVisibleHeight;
		float maxY = _worldBottom - halfVisibleHeight;
		
		if (minX > maxX) minX = maxX = (_worldLeft + _worldRight) / 2;
		if (minY > maxY) minY = maxY = (_worldTop + _worldBottom) / 2;
		
		float clampedX = Mathf.Clamp(targetPosition.X, minX, maxX);
		float clampedY = Mathf.Clamp(targetPosition.Y, minY, maxY);
		
		return new Vector2(clampedX, clampedY);
	}
}
