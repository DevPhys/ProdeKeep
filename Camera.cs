using Godot;
using System;

public partial class Camera : Camera2D
{
	[Export] private float _speed = 500.0f;
	[Export] private float _zoomSpeed = 0.1f;
	[Export] private float _minZoom = 0.5f;
	[Export] private float _maxZoom = 2.0f;

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

		Position = new Vector2(_worldRight / 2, _worldBottom / 2);
		Position = Position.Round();
	}

	public override void _Process(double delta)
	{
		float deltaF = (float)delta;
		Vector2 input = Vector2.Zero;

		// Управление WASD или стрелками
		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
			input.Y -= 1;
		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
			input.Y += 1;
		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
			input.X -= 1;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
			input.X += 1;

		// Движение камеры
		Position += input.Normalized() * _speed / Zoom.X * deltaF;
		Position = ClampPositionToWorld(Position).Round();

		// Зум колесом мыши
		if (Input.IsActionJustPressed("ui_accept")) // Enter для сброса зума
		{
			Zoom = Vector2.One;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Зум колесом мыши
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				Zoom *= (1 + _zoomSpeed);
			}
			else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				Zoom *= (1 - _zoomSpeed);
			}

			Zoom = new Vector2(
				Mathf.Clamp(Zoom.X, _minZoom, _maxZoom),
				Mathf.Clamp(Zoom.Y, _minZoom, _maxZoom)
			);
		}
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
