using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using BlockId = Storage.BlockId;

using System.Threading;
using System.Threading.Tasks;

public partial class WorldRenderer : TileMapLayer
{
	// Получаем ссылки
	[Export] public Camera2D _camera;
	[Export] public Player _player;

	Generation generation;
	Lighting lighting;

	HashSet<int> transparentBlocks;
	HashSet<ChunkKey> renderedChunks = new HashSet<ChunkKey>();

	List <ChunkKey> listChunkKey = new List<ChunkKey>();
	List <ChunkKey> oldChunkKeys = new List<ChunkKey>();

	Vector2 playerPos;

	(int W1, int W2) chunkWidthLimit = (0, Storage.ChunkW);
	(int H1, int H2) chunkHeightLimit = (0, Storage.WorldH);

	int cameraX, cameraY;
	int screenWidth, screenHeight;
	int currentChunk = 0; 

	const int viewDistanceChunks = 4;

	int _penumbraIdForAir = (int)BlockId.PenumbraIdForAir;
	int _darknessIdForAir = (int) BlockId.DarknessIdForAir;
	int _darknessIdForBlocks = (int)BlockId.DarknessIdForBlocks;

	int _chunkWidth = Storage.ChunkW;
	int _chunkHeight = Storage.WorldH;
	int _tileSize = Storage.TileSize;

	public override void _Ready()
	{
		screenWidth = (int)DisplayServer.WindowGetSize().X;
		screenHeight = (int)DisplayServer.WindowGetSize().Y;

		transparentBlocks = Storage.TransparentBlocks;

		generation = new Generation();
		lighting = new Lighting(transparentBlocks);
	}
	public override void _Process(double delta)
	{
		int currentChunckOld = currentChunk;
		Vector2 playerPosOld = playerPos;

		if (_player != null)
			playerPos = _player.GlobalPosition;
		else
			playerPos = _camera.GlobalPosition;

		cameraX = (int)playerPos.X;
		cameraY = (int)playerPos.Y;
		
		currentChunk = cameraX / (_tileSize * _chunkWidth);

		if (currentChunk != currentChunckOld)
		{
			GenerateAndRenderWorld(Storage.WorldMemory);
		}

		if (Player.isBlock)
		{
			// Обнавляем чанк
			Storage.WorldMemory[Player.ChunkKey] = Player.Chunk;

			// Сохраняем ключ чанка, чтобы потом сохранить
			if (!Storage.SaveChunks.Contains(Player.ChunkKey))
			{
				Storage.SaveChunks.Add(Player.ChunkKey);
			}
		}
	}

	private void GenerateAndRenderWorld(ConcurrentDictionary<ChunkKey, byte[]> worldData)
	{
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
				generation.CreationChunk(key.ChunkIdx);
				worldData = Storage.WorldMemory;

				GenerateChunk(key, chunkWidthLimit, chunkHeightLimit);
			}
		}

		// Проверяем на наличее чанков
		if (worldData.Count == 0)
		{
			GD.PrintErr("словарь пуст");
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
			listChunkKey.Add(key);
		}
	}

	private void GenerateChunk(ChunkKey key, (int W1, int W2) ChunkWidthLimit, (int H1, int H2) ChunkHeightLimit)
	{
		// Рисуем видимые чанки
		if (!Storage.WorldMemory.TryGetValue(key, out byte[] chunkCurrent))
			return;

		int[] border = lighting.Light(key, (playerPos, chunkCurrent), ChunkHeightLimit);
		int offsetX = key.ChunkIdx * _chunkWidth;

		// Создаем массивы для массовой установки
		var positions = new Godot.Collections.Array<Vector2I>();
		var atlasCoordsArray = new Godot.Collections.Array<Vector2I>();

		// Проходим по столбцам 
		for (int x = ChunkWidthLimit.W1; x < ChunkWidthLimit.W2; x++)
		{
			for (int y = ChunkHeightLimit.H1; y < ChunkHeightLimit.H2; y++)
			{
				int index = x * _chunkHeight + y;

				int tileId = chunkCurrent[index];
				int borderId = border[index];

				if (borderId == 1)
				{
					tileId = _penumbraIdForAir;
				}
				else if (borderId == 2)
				{
					tileId = _darknessIdForAir;
				}
				else if(borderId == 3)
				{
					tileId = _darknessIdForBlocks;
				}

				positions.Add(new Vector2I(offsetX + x, y));
				atlasCoordsArray.Add(new Vector2I(tileId % 10, tileId / 10));
			}
		}

		FastMassUpdate(positions, atlasCoordsArray);
	}

	private void ClearChunk(ChunkKey key)
	{
		int chunksPerWorld = Storage.WorldSizeBlocks / _chunkWidth;
		int globalIndex = key.WorldId * chunksPerWorld + key.ChunkIdx;
		int offsetX = key.ChunkIdx * _chunkWidth;

		// Очищаем чанк
		for (int x = 0; x < _chunkWidth; x++)
		{
			for (int y = 0; y < _chunkHeight; y++)
			{
				// полностью удаляем тайл
				EraseCell(new Vector2I(offsetX + x, y));
			}
		}
	}
	public void RedrawChunk(ChunkKey key)
	{
		int heightUpdate2 = (int)(playerPos.Y / _tileSize) + 50;
		int heightUpdate1 = (int)(playerPos.Y / _tileSize) - 50;

		if (heightUpdate2 >= _chunkHeight)
			heightUpdate2 = _chunkHeight;
		if (heightUpdate1 < 0)
			heightUpdate1 = 0;

		GenerateChunk(key, chunkWidthLimit, (heightUpdate1, heightUpdate2));
	}

	public void FastMassUpdate(Godot.Collections.Array<Vector2I> positions, Godot.Collections.Array<Vector2I> atlasCoordsArray)
	{
		for (int i = 0; i < positions.Count; i++)
		{
			// Прямая и точечная установка без посредников
			SetCell(positions[i], sourceId: 0, atlasCoordsArray[i]);
		}
	}
}

public class Lighting (HashSet<int> transparentBlocks)
{
	int chunkWidth = Storage.ChunkW;
	int chunkHeight = Storage.WorldH;

	public int[] Light(ChunkKey key, (Vector2 PlayerPos, byte[] ChunkCurrent) Main, (int H1, int H2) ChunkHeightLimit)
	{
		int[] border = new int[chunkHeight * chunkWidth];
		byte[] chunkCurrent = Main.ChunkCurrent;

		bool isLighting = Storage.IsLighting;
		int offsetX = key.ChunkIdx * chunkWidth;

		if (isLighting)
		{
			int lvl = 0;
			for (int x = 0; x < chunkWidth; x++)
			{
				lvl = 0;
				for (int y = 0; y < chunkHeight; y++)
				{
					int index = x * chunkHeight + y;
					int tileId = chunkCurrent[index];

					if (!transparentBlocks.Contains(tileId))
					{
						if (lvl == 0)
							lvl = 1;
						else if (lvl == 1)
						{
							if (!TouchesAir(index, chunkCurrent, chunkWidth, chunkHeight))
								lvl = 2;
						}
					}

					if (tileId == (int)BlockId.Air)
					{
						if (lvl == 0)
						{
							border[index] = (int)BlockId.LightSource;
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
							if (tileId == (int)BlockId.Torch)
							{
								border[index] = (int)BlockId.LightSource;
							}
							else
							{
								border[index] = 3;
							}
						}
					}
				}
			}

			lvl = (int)BlockId.LightSource;
			for (int step = 0; step < 10; step++)
			{
				// Создаём копию border
				int[] borderCopy = new int[border.Length];
				Buffer.BlockCopy(border, 0, borderCopy, 0, border.Length * sizeof(int));

				for (int x = 0; x < chunkWidth; x++)
				{
					for (int y = ChunkHeightLimit.H1; y < ChunkHeightLimit.H2; y++)
					{
						int index = x * chunkHeight + y;
						int tileId = borderCopy[index];

						if (tileId == (int)BlockId.LightSource)
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
		}

		return border;
	}
	private bool TouchesAir(int index, byte[] chunk, int chunkWidth, int chunkHeight)
	{
		int x = index / chunkHeight;
		int y = index % chunkHeight;

		// Проверяем 4 стороны
		if (x > 0 && transparentBlocks.Contains(chunk[(x - 1) * chunkHeight + y]))
			return true;
		if (x < chunkWidth - 1 && transparentBlocks.Contains(chunk[(x + 1) * chunkHeight + y]))
			return true;
		if (y > 0 && transparentBlocks.Contains(chunk[x * chunkHeight + (y - 1)]))
			return true;
		if (y < chunkHeight - 1 && transparentBlocks.Contains(chunk[x * chunkHeight + (y + 1)]))
			return true;

		return false;
	}
}
