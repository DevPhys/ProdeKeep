using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 200.0f;
	[Export] public float SwingSpeed = 10.0f;
	[Export] public float SwingAngleDeg = 30.0f;
	[Export] public float JumpVelocity = -400.0f;
	[Export] public float Gravity = 980.0f;

	[Export] public WorldRenderer worldRenderer;

	private float _time = 0.0f;

	private Marker2D _torso;
	private Marker2D _head;
	private Marker2D _hand1;
	private Marker2D _hand2;
	private Marker2D _leg1;
	private Marker2D _leg2;

	public static ConcurrentDictionary<ChunkKey, byte[]> worldMemory = new ConcurrentDictionary<ChunkKey, byte[]>();
	public static byte[] chunk;
	public static ChunkKey ChunkKeylocal;
	public static bool isBlock = false;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;

		_torso = GetNode<Marker2D>("Torso");
		_head = GetNode<Marker2D>("Head");
		_hand1 = GetNode<Marker2D>("hand1");
		_hand2 = GetNode<Marker2D>("hand2");
		_leg1 = GetNode<Marker2D>("leg1");
		_leg2 = GetNode<Marker2D>("leg2");

		Position = new Vector2(16 * 200000 / 2, 100 * 16);
		worldMemory = Generation.worldMemory;
	}
	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		// Горизонтальное движение
		float horizontalSpeed = inputDir.X * Speed;

		// Вертикальная скорость с гравитацией
		float verticalSpeed = Velocity.Y;
		
		if (IsOnFloor())
		{
			verticalSpeed = 0;
			
			if (Input.IsActionPressed("ui_up"))  // Стрелка вверх = прыжок
			{
				verticalSpeed = JumpVelocity;
			}
		}
		else
		{
			verticalSpeed += Gravity * dt;
		}

		Velocity = new Vector2(horizontalSpeed, verticalSpeed);
		MoveAndSlide();

		// --- Разворот персонажа ---
		if (Velocity.X < -0.1f)
		{
			_torso.Scale = new Vector2(-1, 1);
			_head.Scale = new Vector2(-1, 1);
		}
		else if (Velocity.X > 0.1f)
		{
			_torso.Scale = new Vector2(1, 1);
			_head.Scale = new Vector2(1, 1);
		}

		// --- Анимация рук (всегда, если двигаемся) ---
		if (Mathf.Abs(Velocity.X) > 0.1f)
		{
			_time += dt * SwingSpeed;
			float swingRad = Mathf.DegToRad(SwingAngleDeg);
			float swing = Mathf.Sin(_time) * swingRad;

			_hand1.Rotation = swing;
			_hand2.Rotation = -swing;
		}
		else
		{
			_time = 0.0f;
			_hand1.Rotation = 0;
			_hand2.Rotation = 0;
		}

		// --- Анимация ног (когда жмём влево/вправо) ---
		if (Mathf.Abs(inputDir.X) > 0.1f)
		{
			float swingRad = Mathf.DegToRad(SwingAngleDeg);
			float swing = Mathf.Sin(_time) * swingRad;

			_leg1.Rotation = -swing;
			_leg2.Rotation = swing;
		}
		else
		{
			_leg1.Rotation = 0;
			_leg2.Rotation = 0;
		}

		// --- Голова за мышкой ---
		Vector2 mousePos = GetGlobalMousePosition();
		Vector2 headPos = _head.GlobalPosition;

		// Преобразуем позицию мыши в локальные координаты персонажа
		Vector2 localMousePos = _torso.ToLocal(mousePos);

		// Инвертируем Y, если нужно
		if (_head.Scale.X == -1)
		{
			localMousePos.Y = -localMousePos.Y;
		}
		else
		{
			localMousePos.Y = localMousePos.Y;
		}

		float targetAngle = localMousePos.Angle();
		_head.Rotation = targetAngle;
	}

	public override void _Input(InputEvent ev)
	{
		if (ev is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
		{
			isBlock = true;

			// Получаем позицию мыши
			Vector2 mousePos = GetGlobalMousePosition();

			int currentChunk = (int)(mousePos.X / (16 * Generation.chunkHeightX));  // Получаем текущий чанк

			// Координаты блока в мире
			int blockX = (int)(mousePos.X / 16);
			int blockY = (int)(mousePos.Y / 16);

			int localX = blockX - (currentChunk * Generation.chunkHeightX);

			int blockIndex = localX * Generation.worldHeightY + blockY;  // Индекс в массиве чанка
			var chunkKey = new ChunkKey(worldId: 0, chunkIdx: currentChunk);  // Проверяем, что индекс в пределах массива

			GD.Print($"Координаты блока Х: {blockX} Y: {blockY} blockIndex: {blockIndex} размер массива: {worldMemory[chunkKey].Length}");
			GD.Print($"Координаты мыши X: {mousePos.X} Y: {mousePos.Y} текущий чанк {currentChunk}\n");

			if (worldMemory.ContainsKey(chunkKey))
			{
				// Удаляем блок (ставим 0)
				worldMemory[chunkKey][blockIndex] = 0;
				chunk = worldMemory[chunkKey];
				ChunkKeylocal = chunkKey;

				worldRenderer.RedrawChunk(chunkKey);

				GD.Print($"✅ Блок удален! X: {blockX}, Y: {blockY}, Чанк: {currentChunk}, Индекс: {blockIndex}");
			}
			else
			{
				GD.Print($"❌ Блок не найден! X: {blockX}, Y: {blockY}, Чанк: {currentChunk}");
			}
		}
		else
		{
			isBlock = false;
		}
	}
	public override void _ExitTree()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}
}
