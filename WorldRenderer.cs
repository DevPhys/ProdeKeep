using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public partial class WorldRenderer : TileMapLayer
{
	// Получаем ссылку на камеру
	//[Export] public Camera2D Camera2D; 

	private Player _player;
	private Camera _camera;

	Generation generation;  // Обьевляем переменную класса генераци мира
	ConcurrentDictionary<ChunkKey, byte[]> worldData = new ConcurrentDictionary<ChunkKey, byte[]>();  // Словарь чанков

	// Списки активных и неактивных чанков
	List<ChunkKey> listChunkKey = new List<ChunkKey>();
	List<ChunkKey> oldChunkKeys = new List<ChunkKey>();

	int cameraX, cameraY;   // Переменные координат камеры
	int screenWidth, screenHeight;  // Переменные размеры экрана
	int currentChunk = 0;   // Переменная для определения чанка

	// Константы
	private const int viewDistanceChunks = 4; // Сколько чанков видно влево/вправо

	// Состояние
	private HashSet<ChunkKey> renderedChunks = new HashSet<ChunkKey>();

	public override void _Ready()
	{
		// Создаем класс генерации 
		generation = new Generation();

		// Узнаем разрешение экрана
		screenWidth = (int)DisplayServer.WindowGetSize().X;
		screenHeight = (int)DisplayServer.WindowGetSize().Y;

		_player = GetNode<Player>("/root/Gameplay/Player");
		_camera = GetNode<Camera>("/root/Gameplay/Camera2D");

		GD.Print($"Разрешение экрана: {screenWidth}х{screenHeight}");  // Выводим разрешение в консоль
		worldData = generation.CreationWorld();  // Создаем мир
	}
	public override void _Process(double delta)
	{
		int currentChunckOld = currentChunk;

		// Узнаем позицию игрока
		Vector2 playerPos;
		if (_player != null)
			playerPos = _player.GlobalPosition;
		else
			playerPos = _camera.GlobalPosition;

		cameraX = (int)playerPos.X;
		cameraY = (int)playerPos.Y;

		currentChunk = cameraX / (16 * Generation.chunkHeightX);  // Узнаем на какой чанк смотрит игрок

		//GD.Print($"Чанк {currentChunk}. Положение игрока по Х: {cameraX} Положение игрока по Y: {cameraY}");

		if (currentChunk != currentChunckOld)
		{
			GenerateAndRenderWorld(worldData);  // Рисуем только те чанки, которые видны
			//GD.Print("Рисуем новые чанки!");
		}
		if (Player.isBlock)
		{
			worldData[Player.ChunkKeylocal] = Player.chunk;

			//GD.Print("Перерисовали!");
		}
	}

	private void GenerateAndRenderWorld(ConcurrentDictionary<ChunkKey, byte[]> worldData)
	{
		// Проверяем на наличее чанков
		if (worldData.Count == 0)
		{
			GD.PrintErr("словарь пуст");
			return;
		}

		// Сохраняем старые чанки
		oldChunkKeys.Clear();
		oldChunkKeys.AddRange(listChunkKey);

		// 2Обновляем список актуальных чанков
		SpecificChunks();

		// Удаляем только те чанки, которых нет в новом списке
		foreach (var key in oldChunkKeys)
		{
			if (!listChunkKey.Contains(key))
			{
				ClearChunk(key); // Удаляем только этот чанк
			}
		}

		// Рисуем только новые/актуальные чанки
		foreach (var key in listChunkKey)
		{
			// Проверяем, был ли этот чанк уже нарисован
			if (!oldChunkKeys.Contains(key))
			{
				GenerateChunk(key); // Рисуем только если это новый чанк
			}
		}
	}
	private void SpecificChunks()
	{
		// Очищаем список актуальных чанков
		listChunkKey.Clear();

		// Добавляем чанки слева и справа
		for (int i = viewDistanceChunks * -1; i <= viewDistanceChunks; i++)
		{
			int chunkIndex = currentChunk + i;
			if (chunkIndex < 0) continue;

			var key = new ChunkKey(0, chunkIndex);
			if (worldData.ContainsKey(key))
				listChunkKey.Add(key);
		}
	}

	private void GenerateChunk(ChunkKey key)
	{
		// Рисуем видимые чанки
		if (!worldData.TryGetValue(key, out byte[]? chunkCurrent))
			return;

		int chunkWidth = Generation.chunkHeightX;
		int chunkHeight = Generation.worldHeightY;

		int offsetX = key.ChunkIdx * chunkWidth;

		// Создаем массивы для массовой установки
		var positions = new Godot.Collections.Array<Vector2I>();
		var atlasCoordsArray = new Godot.Collections.Array<Vector2I>();

		// Проходим по столбцам 
		for (int x = 0; x < chunkWidth; x++)
		{
			for (int y = 0; y < chunkHeight; y++)
			{
				int index = x * chunkHeight + y;
				int tileId = chunkCurrent[index];

				if (tileId != 0)
				{
					positions.Add(new Vector2I(offsetX + x, y));
					atlasCoordsArray.Add(new Vector2I(tileId % 10, tileId / 10));
				}
			}
		}

		//GD.Print($"Рисуем чанк! Всего тайлов установиться {positions.Count}");

		for (int i = 0; i < positions.Count; i++)
		{
			SetCell(positions[i], 0, atlasCoordsArray[i]);
		}
	}
	private void ClearChunk(ChunkKey key)
	{
		int chunkWidth = Generation.chunkHeightX;
		int chunkHeight = Generation.worldHeightY;
		int chunksPerWorld = Generation.worldSizeBlocks / chunkWidth;

		int globalIndex = key.WorldId * chunksPerWorld + key.ChunkIdx;
		int offsetX = key.ChunkIdx * chunkWidth;

		// Очищаем чанк
		for (int x = 0; x < chunkWidth; x++)
		{
			for (int y = 0; y < chunkHeight; y++)
			{
				// полностью удаляем тайл
				EraseCell(new Vector2I(offsetX + x, y));
			}
		}
	}

	public void RedrawChunk(ChunkKey key)
	{
		ClearChunk(key);
		GenerateChunk(key);
	}
}

public struct ChunkKey
{
	public int WorldId;
	public int ChunkIdx;

	public ChunkKey(int worldId, int chunkIdx)
	{
		WorldId = worldId;
		ChunkIdx = chunkIdx;
	}
}
