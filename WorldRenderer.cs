using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public partial class WorldRenderer : TileMapLayer
{
	// ID блоков
	enum BlockId : byte
	{
		Air = 0,
		Water = 5,

		Grass = 3,
		Snow = 7,
		Sand = 4,

		Earth = 2,
		Gravel = 6,

		Stone = 1,

		CarbonicBlock = 10,
		IronBlock = 11,

		Oak = 20,
		Spruce = 21,
		Сacti = 26,

		FoliageOaking = 30,
		FoliageSpruceing = 31
	}

	// Получаем ссылку на камеру
	[Export] public Camera2D _camera;
	[Export] public Player _player;

	private TileMapLayer _tileMapLayer;

	int darknessId = 39;
	int penumbraIdAir = 28;
	int darknessIdIdAir = 29;

	int lightSource = 10;

	Vector2 playerPos;

	Generation generation;  // Обьевляем переменную класса генераци мира
	ConcurrentDictionary<ChunkKey, byte[]> worldData = new ConcurrentDictionary<ChunkKey, byte[]>();  // Словарь чанков
	HashSet<int> permittedBlock;

	// Списки активных и неактивных чанков
	List <ChunkKey> listChunkKey = new List<ChunkKey>();
	List <ChunkKey> oldChunkKeys = new List<ChunkKey>();

	int cameraX, cameraY;   // Переменные координат камеры
	int screenWidth, screenHeight;  // Переменные размеры экрана
	int currentChunk = 0;   // Переменная для определения чанка

	private const int viewDistanceChunks = 4; // Сколько чанков видно влево/вправо
	private HashSet<ChunkKey> renderedChunks = new HashSet<ChunkKey>();  // Состояние

	public override void _Ready()
	{
		// Создаем класс генерации 
		generation = new Generation();

		// Узнаем разрешение экрана
		screenWidth = (int)DisplayServer.WindowGetSize().X;
		screenHeight = (int)DisplayServer.WindowGetSize().Y;
		
		// Создаем мир
		worldData = generation.CreationWorld();

		permittedBlock = new HashSet<int>
		{
			(int)BlockId.Air,
			(int)BlockId.Water,

			(int)BlockId.FoliageOaking,
			(int)BlockId.FoliageSpruceing,

			(int)BlockId.Сacti,
			(int)BlockId.Oak,
			(int)BlockId.Spruce,

			38,
		};
	}
	public override void _Process(double delta)
	{
		int currentChunckOld = currentChunk;
		Vector2 playerPosOld = playerPos;

		if (_player != null)
			playerPos = _player.GlobalPosition;
		else
			playerPos = _camera.GlobalPosition;

		// Узнаем позицию
		cameraX = (int)playerPos.X;
		cameraY = (int)playerPos.Y;
		
		// Узнаем на какой чанк смотрит игрок
		currentChunk = cameraX / (16 * Generation.chunkHeightX);

		if (currentChunk != currentChunckOld)
		{
			 // Рисуем только те чанки, которые видны
			GenerateAndRenderWorld(worldData);
		}
		if (Player.isBlock)
		{
			// Обнавляем чанк
			worldData[Player.ChunkKeylocal] = Player.chunk;
		}

		if (playerPos != playerPosOld)
		{
			//GenerateChunk(new ChunkKey(0, currentChunk));
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

		// Обновляем список актуальных чанков
		SpecificChunks();

		// Удаляем только те чанки, которых нет в новом списке
		foreach (var key in oldChunkKeys)
		{
			if (!listChunkKey.Contains(key))
			{
				// Удаляем только этот чанк
				ClearChunk(key);
			}
		}

		// Рисуем только новые/актуальные чанки
		foreach (var key in listChunkKey)
		{
			// Проверяем, был ли этот чанк уже нарисован
			if (!oldChunkKeys.Contains(key))
			{
				// Рисуем только если это новый чанк
				GenerateChunk(key);
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
		
		int[] border = new int[chunkHeight * chunkWidth];
		int lvl = 0;

		for (int x = 0; x < chunkWidth; x++)
		{
			lvl = 0;

			for (int y = 0; y < chunkHeight; y++)
			{
				int index = x * chunkHeight + y;
				int tileId = chunkCurrent[index];

				if (!permittedBlock.Contains(tileId))
				{
					if (lvl == 0)
						lvl = 1;
					else if (lvl == 1)
						lvl = 2;
				}

				if (tileId == (int)BlockId.Air)
				{
					if (lvl == 0)
					{
						border[index] = lightSource;
					}
					else
					{
						border[index] = lvl;
					}
				}
				else
				{
					if (lvl == 2)
					{
						if (tileId == 38)
						{
							border[index] = lightSource;
						}
						else
						{
							border[index] = 3;
						}
					}
				}
			}
		}

		lvl = 0;
		for (int step = 0; step < 7; step++)
		{
			// Создаём копию border
			int[] borderCopy = new int[border.Length];
			Buffer.BlockCopy(border, 0, borderCopy, 0, border.Length * sizeof(int));
			
			int heightUpdate = (int)(playerPos.Y / 16) + 50;
			int heightUpdate2 = (int)(playerPos.Y / 16) - 50;
			
			if (heightUpdate >= chunkHeight)
				heightUpdate = chunkHeight;
			if (heightUpdate2 < 0)
				heightUpdate2 = 0;
			
			for (int x = 0; x < chunkWidth; x++)
			{
				for (int y = heightUpdate2; y < heightUpdate; y++)
				{
					int index = x * chunkHeight + y;
					int tileId = borderCopy[index];

					if (tileId == lightSource || tileId == 0)
					{
						// Лево
						if (x > 0)
						{
							int iL = (x - 1) * chunkHeight + y;
							// Проверяем, что сосед КАСАЕТСЯ воздуха (хотя бы один из его соседей — воздух)
							if (TouchesAir(iL, chunkCurrent, chunkWidth, chunkHeight))
								border[iL] = lvl;
						}

						// Право
						if (x < chunkWidth - 1)
						{
							int iR = (x + 1) * chunkHeight + y;
							if (TouchesAir(iR, chunkCurrent, chunkWidth, chunkHeight))
								border[iR] = lvl;
						}

						// Верх
						if (y > 0)
						{
							int iUp = x * chunkHeight + (y - 1);
							if (TouchesAir(iUp, chunkCurrent, chunkWidth, chunkHeight))
								border[iUp] = lvl;
						}

						// Низ
						if (y < chunkHeight - 1)
						{
							int iDown = x * chunkHeight + (y + 1);
							if (TouchesAir(iDown, chunkCurrent, chunkWidth, chunkHeight))
								border[iDown] = lvl;
						}
					}
				}
			}
		}

		// Проходим по столбцам 
		for (int x = 0; x < chunkWidth; x++)
		{
			for (int y = 0; y < chunkHeight; y++)
			{
				int index = x * chunkHeight + y;
				int tileId = chunkCurrent[index];

				// Проверяем, ниже ли игрока этот блок
				if (border[index] == 1)
				{
					// Заменяем на блок темноты
					tileId = penumbraIdAir;
				}
				else if (border[index] == 2)
				{
					tileId = darknessIdIdAir;
				}
				else if(border[index] == 3)
				{
					tileId = darknessId;
				}

				positions.Add(new Vector2I(offsetX + x, y));
				atlasCoordsArray.Add(new Vector2I(tileId % 10, tileId / 10));
			}
		}

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

	// Функция проверки: касается ли блок воздуха
	private bool TouchesAir(int index, byte[] chunk, int chunkWidth, int chunkHeight)
	{
		int x = index / chunkHeight;
		int y = index % chunkHeight;

		// Проверяем 4 стороны
		if (x > 0 && permittedBlock.Contains(chunk[(x - 1) * chunkHeight + y]))
			return true;
		if (x < chunkWidth - 1 && permittedBlock.Contains(chunk[(x + 1) * chunkHeight + y]))
			return true;
		if (y > 0 && permittedBlock.Contains(chunk[x * chunkHeight + (y - 1)]))
			return true;
		if (y < chunkHeight - 1 && permittedBlock.Contains(chunk[x * chunkHeight + (y + 1)]))
			return true;

		return false;
	}
}
