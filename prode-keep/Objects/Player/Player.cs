using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BlockId = Storage.BlockId;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 200.0f;
	[Export] public float SwingSpeed = 10.0f;
	[Export] public float SwingAngleDeg = 30.0f;
	[Export] public float JumpVelocity = -400.0f;
	[Export] public float Gravity = 980.0f;

	private Marker2D _torso;
	private Marker2D _head;
	private Marker2D _hand1;
	private Marker2D _hand2;
	private Marker2D _leg1;
	private Marker2D _leg2;

	[Export] public WorldRenderer worldRenderer;
	ConcurrentDictionary<ChunkKey, byte[]> worldMemory = new ConcurrentDictionary<ChunkKey, byte[]>();

	private float _time = 0.0f;
	private int rangeBlock = 4;
	private int sizeBlock = Storage.TileSize;

	public static byte[] Chunk;
	public static ChunkKey ChunkKey;

	public static bool isBlock = false;
	private bool isInventory;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;

		_torso = GetNode<Marker2D>("Torso");
		_head = GetNode<Marker2D>("Head");
		_hand1 = GetNode<Marker2D>("hand1");
		_hand2 = GetNode<Marker2D>("hand2");
		_leg1 = GetNode<Marker2D>("leg1");
		_leg2 = GetNode<Marker2D>("leg2");

		Position = Storage.playerData.PlayerPos;
		worldMemory = Storage.WorldMemory;
	}
	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Vector2 inputDir = Vector2.Zero;

		isInventory = Gameplayer.isInventory;

		if (!isInventory)
		{
			if (Input.IsKeyPressed(Key.A))
				inputDir.X -= 1;
			else if (Input.IsKeyPressed(Key.D))
				inputDir.X += 1;

			// Прыжок
			if (Input.IsKeyPressed(Key.W) && IsOnFloor())
			{
				Velocity = new Vector2(Velocity.X, JumpVelocity);
			}
		}

		// Гравитация
		if (!IsOnFloor())
			Velocity += new Vector2(0, Gravity * dt);

		// Движение
		Velocity = new Vector2(inputDir.X * Speed, Velocity.Y);

		MoveAndSlide();
		AnimationPlayers(dt, inputDir);
	}
	public override void _Input(InputEvent ev)
	{
		if (ev is InputEventMouseButton mouseEvent && mouseEvent.Pressed && !isInventory)
		{
			bool isRange = false;

			// Получаем позицию мыши
			Vector2 mousePos = GetGlobalMousePosition();
			Vector2 playerPos = GlobalPosition;

			float distance = playerPos.DistanceTo(mousePos);
			float distanceInBlocks = distance / sizeBlock;

			int blockX = 0, blockY = 0, localX = 0, currentChunk = 0, blockIndex = 0;
			ChunkKey chunkKey;

			if (distanceInBlocks <= rangeBlock)
			{
				isRange = true;
			}
			else
			{
				isRange = false;
			}

			if (isRange)
			{
				// Координаты блока в мире (используем FloorToInt вместо RoundToInt)
				blockX = Mathf.FloorToInt(mousePos.X / 16.0f);
				blockY = Mathf.FloorToInt(mousePos.Y / 16.0f);

				// Учитываем отрицательные координаты
				localX = ((blockX % Storage.ChunkW) + Storage.ChunkW) % Storage.ChunkW;
				currentChunk = Mathf.FloorToInt((float)blockX / Storage.ChunkW);

				// Проверяем, что blockY в допустимых пределах
				if (blockY >= 0 && blockY < Storage.WorldH)
				{
					blockIndex = localX * Storage.WorldH + blockY;

					chunkKey = new ChunkKey(worldId: 0, chunkIdx: currentChunk);

					if (mouseEvent.ButtonIndex == MouseButton.Left)
					{
						isBlock = true;
						DeleteBlock(chunkKey, blockIndex);
					}
					else if (mouseEvent.ButtonIndex == MouseButton.Right)
					{
						isBlock = true;
						NewBlock(chunkKey, blockIndex);
					}
				}
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

	private void DeleteBlock(ChunkKey chunkKey, int blockIndex)
	{
		if (worldMemory.ContainsKey(chunkKey))
		{
			// Проверяем, что индекс в пределах массива
			if (blockIndex < 0 || blockIndex >= worldMemory[chunkKey].Length)
			{
				GD.Print($"Индекс {blockIndex} вне массива размером {worldMemory[chunkKey].Length}");
				return;
			}

			worldMemory[chunkKey][blockIndex] = 0;
			Chunk = worldMemory[chunkKey];
			ChunkKey = chunkKey;

			worldRenderer.RedrawChunk(chunkKey);
		}
	}
	private void NewBlock(ChunkKey chunkKey, int blockIndex)
	{
		if (worldMemory.ContainsKey(chunkKey))
		{
			// Проверяем, что индекс в пределах массива
			if (blockIndex < 0 || blockIndex >= worldMemory[chunkKey].Length)
			{
				GD.Print($"Индекс {blockIndex} вне массива размером {worldMemory[chunkKey].Length}");
				return;
			}

			if (worldMemory[chunkKey][blockIndex] == (int)BlockId.Null)
				worldMemory[chunkKey][blockIndex] = 0;
			else if (worldMemory[chunkKey][blockIndex] == 0)
				worldMemory[chunkKey][blockIndex] = (byte)HotbarPointer.currentBlock;

			Chunk = worldMemory[chunkKey];
			ChunkKey = chunkKey;

			worldRenderer.RedrawChunk(chunkKey);
		}
	}

	private void AnimationPlayers(float dt, Vector2 inputDir)
	{
		Vector2 mousePos = GetGlobalMousePosition();
		Vector2 headPos = _head.GlobalPosition;
		
		isInventory = Gameplayer.isInventory;
		if (!isInventory)
		{
			// --- Разворот персонажа к мышке ---
			if (mousePos.X < GlobalPosition.X)
			{
				_torso.Scale = new Vector2(-1, 1);
				_head.Scale = new Vector2(-1, 1);
			}
			else
			{
				_torso.Scale = new Vector2(1, 1);
				_head.Scale = new Vector2(1, 1);
			}
			
			// --- Голова за мышкой ---
			Vector2 localMousePos = _torso.ToLocal(mousePos);

			// Инвертируем Y, если нужно
			if (_head.Scale.X == -1)
			{
				localMousePos.Y = -localMousePos.Y;
			}
			
			float targetAngle = localMousePos.Angle();
			_head.Rotation = targetAngle;
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
	}
}
