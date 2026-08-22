using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Godot;
using BlockId = Storage.BlockId;

public class Generation
{
	// Настройки мира
	(int Upper, int Lower) LimitWater = (263, 280);  // Границы по высоте появления воды
	(int Upper, int Lower) LimitCarbonic = (260, 410);  // Границы появления угольной руды
	(int Upper, int Lower) LimitIron = (290, 650);  // Границы появления железной руды

	// Настройка пещер
	(int Upper, int Lower) upperLimitCave = (31, 53);
	(int Upper, int Lower) lowerLimitCave = (200, 225);

	// Настройка линии рельефа
	static (int Upper, int Lower) upperLimitRelief = (15 + 250, 30 + 250);
	static int lowerLimitRelief = 3 + 250;

	// Дополнительные настройки генерации
	int layerThicknessEarth = 4;  // Толщина слоя земли
	int numOctave1D = 10; // Количество октав 1д шума

	public static ConcurrentDictionary<ChunkKey, byte[]> worldMemory;

	// Списки шаблонов структур деревьев
	private static List<byte> listTreesOaks = new List<byte>();  // Дуб
	private static List<byte> listTreesСacti = new List<byte>();  // Кактус
	private static List<byte> listTreesSpruce = new List<byte>();  // Ель

	public static string seed; // Сид мира
	public int worldSizeBlocks; // Длина мира
	public int numWorld; // Кол-во миров
	public int worldHeightY; // глубина
	public int chunkHeightX;  // Длина 1 чанка

	private static int upperLimitBlocks;  // Высота блока
	private static int lowerLimitBlocks;  // минимальный блок
	private static double midBlocksH;  // Средняя высота 
	private static double amplitudeBlocks;  // Амплитуда

	Noise noise = new Noise();

	public Generation()
	{
		// Запуск цикла генерации сида
		System.Random random = new System.Random();
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		for (int i = 0; i < 10; i++)
		{
			sb.Append(random.Next(0, 9));
		}
		seed = sb.ToString(); // Присвоение сида

		int basicUpperLimitblocks = NumberFromSeed(BasicNumber: upperLimitRelief.Lower - 5, Renge: (upperLimitRelief.Upper, upperLimitRelief.Lower));

		// Присваеваем 
		upperLimitBlocks = basicUpperLimitblocks;
		lowerLimitBlocks = lowerLimitRelief;
		midBlocksH = (upperLimitBlocks + lowerLimitBlocks) / 2.0;
		amplitudeBlocks = upperLimitBlocks - lowerLimitBlocks;

		worldMemory = Storage.WorldMemory;
		worldSizeBlocks = Storage.WorldSizeBlocks;
		numWorld = Storage.NumWorld;
		worldHeightY = Storage.WorldH;
		chunkHeightX = Storage.ChunkW;

		// Заполняем списки
		byte FO = (byte)BlockId.FoliageOaking;
		byte O = (byte)BlockId.Oak;
		listTreesOaks = [
		0, 0, FO, FO, 0,
		0, 0, FO, FO, FO,
		O, O, O, O, FO,
		0, 0, FO, FO, FO,
		0, 0, FO, FO, 0];
 
		byte C = (byte)BlockId.Сacti;
		listTreesСacti = [
		0, 0, 0, 0, 0,
		0, 0, C, 0, 0,
		C, C, C, C, C,
		0, 0, 0, C, 0,
		0, 0, 0, 0, 0];

		byte S = (byte)BlockId.Spruce;
		byte FS = (byte)BlockId.FoliageSpruceing;
		listTreesSpruce = [
			0, 0, 0, 0, 0, 0, 0,
			0, FS, 0, 0, 0, 0, 0,
			0, FS, FS, 0, 0, 0, 0,
			0, FS, FS, FS, 0, FS, 0,
			S,  S,  S,  S,  S,  S, FS,
			0, FS, FS, FS, 0, FS, 0,
			0, FS, FS, 0, 0, 0, 0,
			0, FS, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0, 0,
			];
	}
	public void CreationWorld()
	{
		var watch = System.Diagnostics.Stopwatch.StartNew();
		
		// Создаем мир из 0 и 1
		GenerationWorld();

		int SeedMap = NumberFromSeed(BasicNumber: 92, Factor: 3, Renge: (10, 99));
		int[] p = Perm(Seed: 2);

		int numsChunk = worldSizeBlocks / chunkHeightX;  // Находим количество чанков

		double[] waterNoise = GenerateNoiseMap(Seed: SeedMap * 5);  // Создаем карту вод
		double[] carbonicNoise = GenerateNoiseMap(Seed: SeedMap + 100);  // Создаем карту высот появления угольной руды
		double[] ironNoise = GenerateNoiseMap(Seed: SeedMap + 200);  // Создаем карту появления железной руды
		double[] carbonicNoiseX = GenerateNoiseMap(Seed: SeedMap - 100);  // Создаем карту по Х появления угольной руды

		int[] permOaks = Perm(Seed: 10);
		int[] permCacti = Perm(Seed: 11);
		int[] permSpruce = Perm(Seed: 12);

		// Циклом перерисовываем мир, добавляя траву, руды, структуры и т.д.
		Parallel.For(0, numsChunk, i =>
		{
			// Локальные переменные для каждого потока
			ChunkKey localKey = new ChunkKey(0, i);

			if (!worldMemory.TryGetValue(localKey, out byte[]? chunk))
			{
				GD.Print($"Чанк {i} пуст или не обнаружен");
				return;
			}

			// Работаем с локальной копией
			byte[] localChunk = chunk;

			GenerationWater(waterNoise, i, (localKey, localChunk));  // Создаем озера
			GenerationWaterSand(i, (localKey, localChunk));  // Создаем речной песок на дне озер

			GenerationOre(carbonicNoise, p, i,
				Frequency: (0.18, 0.68),
				IDblock: (byte)BlockId.CarbonicBlock,
				Limit: (LimitCarbonic.Upper, LimitCarbonic.Lower), 
				Main: (localKey, localChunk));  // Создаем уголь
			GenerationOre(ironNoise, p, i,
				Frequency: (0.20, 0.78),
				IDblock: (byte)BlockId.IronBlock,
				Limit: (LimitIron.Upper, LimitIron.Lower),
				Main: (localKey, localChunk));  // Создаем железо

			GenerationStructures(Index: i, Step: 5,
				AllowedBlocks: (
					[(byte)BlockId.Air, (byte)BlockId.Grass, (byte)BlockId.Earth],
					[(byte)BlockId.Air],
					[(byte)BlockId.Grass, (byte)BlockId.Earth]),
				SizeStructure: (2, 5),
				ListStructure: listTreesOaks,
				Permutation: permOaks,
				Main: (localKey, localChunk));  // Размещаем дубы
			GenerationStructures(Index: i, Step: 5,
				AllowedBlocks: (
					[(byte)BlockId.Air],
					[(byte)BlockId.Air],
					[(byte)BlockId.Sand]),
				SizeStructure: (2, 5),
				ListStructure: listTreesСacti,
				Permutation: permCacti,
				Main: (localKey, localChunk));  // Размещаем кактусы
			GenerationStructures(Index: i, Step: 7,
				AllowedBlocks: (
					[(byte)BlockId.Air, (byte)BlockId.Snow],
					[(byte)BlockId.Air],
					[(byte)BlockId.Snow, (byte)BlockId.Earth]),
				SizeStructure: (4, 7),
				ListStructure: listTreesSpruce,
				Permutation: permSpruce,
				Main: (localKey, localChunk));  // Размещаем ели
		});
		
		watch.Stop();
		GD.Print($"\nГотово! Время полной генерации: {watch.ElapsedMilliseconds / 1000.0} секунд");

		// Обновляем
		Storage.WorldMemory = worldMemory;
	}


	private void GenerationWater(double[] WaterNoise, int Index, (ChunkKey Key, byte[] Chunk) Main)
	{
		int chunkOffsetX = Index * chunkHeightX;

		// Проходим по длине чанка
		for (int x = 0; x < chunkHeightX; x++)
		{
			int worldX = chunkOffsetX + x;  // Вычисляем индекс для массива карты температуры
			int waterUpperLimit = LimitWater.Upper + (int)(WaterNoise[worldX] * 5);  // Меняем верхнию границу появлении воды

			// Проходимся в диапазоне появленя озер
			for (int y = waterUpperLimit; y <= LimitWater.Lower; y++)
			{
				int index = x * worldHeightY + y;  // Вычисляем индекс блока

				// Если воздух — заливаем водой
				if (Main.Chunk[index] == (byte)BlockId.Air)
				{
					Main.Chunk[index] = (byte)BlockId.Water; // вода
				}
			}
		}

		// Проходим по длине чанка
		for (int x = 0; x < chunkHeightX; x++)
		{
			int worldX = chunkOffsetX + x;  // Вычисляем индекс для массива карты температуры
			int waterUpperLimit = LimitWater.Upper + (int)(WaterNoise[worldX] * 5);  // Меняем верхнию границу появлении воды

			// Проходимся в диапазоне появленя озер
			for (int y = waterUpperLimit; y <= LimitWater.Lower; y++)
			{
				int index = x * worldHeightY + y;  // Вычисляем индекс блока

				if (Main.Chunk[index] == (byte)BlockId.Water)
				{
					if (x - 1 >= 0 && x + 1 < chunkHeightX && y - 1 >= 0 && y + 1 < worldHeightY)
					{
						if (Main.Chunk[(x + 1) * worldHeightY + y] != (byte)BlockId.Water &&
							Main.Chunk[(x - 1) * worldHeightY + y] != (byte)BlockId.Water &&
							Main.Chunk[x * worldHeightY + (y + 1)] != (byte)BlockId.Water &&
							Main.Chunk[x * worldHeightY + (y - 1)] != (byte)BlockId.Water)
						{
							Main.Chunk[index] = (byte)BlockId.Air;
						}
					}
				}
			}
		}

		// Обновляем чанк
		worldMemory[Main.Key] = Main.Chunk;
	}
	private void GenerationWaterSand(int Index, (ChunkKey Key, byte[] Chunk) Main)
	{
		// Проходим по длине чанка
		for (int x = 0; x < chunkHeightX; x++)
		{
			// Проходимя по высоте чанка
			for (int y = 0; y < worldHeightY - 1; y++)
			{
				int currentIndex = x * worldHeightY + y;  // Вычисляем индекс блока
				int belowIndex = x * worldHeightY + (y + 1);  // // Вычисляем индекс блока ниже

				// Проверяем
				if (Main.Chunk[currentIndex] == (byte)BlockId.Water && 
					Main.Chunk[belowIndex] != (byte)BlockId.Water)
				{
					// Заменяем траву на песок (нижний блок)
					Main.Chunk[belowIndex] = (byte)BlockId.Sand;
				}
			}
		}

		// Обновляем чанк
		worldMemory[Main.Key] = Main.Chunk;
	}

	private void GenerationOre(double[] CarbonicNoise, int[] Permutation, int Index, int IDblock, (double f, double p) Frequency, (int Upper, int Lower) Limit, (ChunkKey Key, byte[] Chunk) Main)
	{
		int chunkOffsetX = Index * chunkHeightX;

		for (int x = 0; x < chunkHeightX; x++)
		{
			int worldX = chunkOffsetX + x;
			int carbonicUpperLimit = Limit.Upper + (int)(CarbonicNoise[worldX] * 5);
			int carbonicLowerLimit = Limit.Lower + (int)(CarbonicNoise[worldX] * 4);

			// Защита от выхода за границы мира
			carbonicUpperLimit = Math.Max(carbonicUpperLimit, 0);
			carbonicLowerLimit = Math.Min(carbonicLowerLimit, worldHeightY - 1);

			for (int y = carbonicUpperLimit; y <= carbonicLowerLimit; y++)
			{
				int index = x * worldHeightY + y;

				if (Main.Chunk[index] == (byte)BlockId.Stone)
				{
					double carbonicNoiseHere = noise.PerlinNoise2D(worldX * Frequency.f, y * Frequency.f, Permutation);

					if (carbonicNoiseHere >= Frequency.p)
						Main.Chunk[index] = (byte)IDblock;
				}
			}
		}

		worldMemory[Main.Key] = Main.Chunk;
	}
	private void GenerationStructures(int Index, int Step,
	List<byte> ListStructure, (List<byte> LeftAndRight, List<byte> Top, List<byte> Bottom) AllowedBlocks,
	(int W, int H) SizeStructure, int[] Permutation, (ChunkKey Key, byte[] Chunk) Main)
	{
		int chunkOffsetX = Index * chunkHeightX;

		// Проходимся по всей длине
		for (int x = SizeStructure.W - 1; x < chunkHeightX - SizeStructure.W + 1; x++)
		{
			// Проходимся по столбцу
			for (int y = SizeStructure.W; y < worldHeightY - SizeStructure.W; y++)
			{
				int currentIndex = x * worldHeightY + y;  // Определяем индекс блока
				int belowIndex = x * worldHeightY + (y - 1);  // Определяем индекс блока над текущим блоком

				if (AllowedBlocks.Bottom.Contains(Main.Chunk[currentIndex]) &&
					AllowedBlocks.Top.Contains(Main.Chunk[belowIndex]))
				{
					double noiseNum = noise.PerlinNoise1D((chunkOffsetX + x) * 0.1, Permutation);
					if (noiseNum >= 0.02)
					{
						// Проверяем, можно ли построить
						bool canBuild = true;

						// Проверяем все столбцы и все уровни появления
						for (int offsetX = SizeStructure.W * -1; offsetX <= SizeStructure.W && canBuild; offsetX++)
						{
							int checkX = x + offsetX;
							if (checkX < 0 || checkX >= chunkHeightX) continue;

							for (int d = 1; d <= SizeStructure.H; d++)
							{
								int checkY = y - d;
								if (checkY < 0)
								{
									canBuild = false;
									break;
								}

								int checkIndex = checkX * worldHeightY + checkY;
								if (checkIndex >= Main.Chunk.Length || !AllowedBlocks.LeftAndRight.Contains(Main.Chunk[checkIndex]))
								{
									canBuild = false;
									break;
								}
							}
						}

						if (canBuild)
						{
							// Строим
							for (int offsetX = SizeStructure.W * -1; offsetX <= SizeStructure.W; offsetX++)
							{
								int placeX = x + offsetX;
								if (placeX < 0 || placeX >= chunkHeightX) continue;

								int columnStartIndex = (offsetX + SizeStructure.W) * Step;

								// Строим столбец снизу вверх
								for (int d = 1; d <= SizeStructure.H; d++)
								{
									int placeY = y - d;
									int placeIndex = placeX * worldHeightY + placeY;

									if (placeIndex >= 0 && placeIndex < Main.Chunk.Length)
									{
										int listIndex = columnStartIndex + (d - 1);

										if (ListStructure[listIndex] != (byte)BlockId.Air)
											Main.Chunk[placeIndex] = ListStructure[listIndex];
									}
								}
							}
						}
					}
					break;
				}
			}
		}
		worldMemory[Main.Key] = Main.Chunk;
	}

	private void GenerationWorld()
	{
		var watch = System.Diagnostics.Stopwatch.StartNew();

		int seedMap = NumberFromSeed(BasicNumber: 33, Factor: 7, Renge: (10, 99));
		double[] caveMap = GenerateNoiseMap(seedMap);
		double[] caveMapLower = GenerateNoiseMap(seedMap + 100);
		double[] temperatureMap = GenerateNoiseMap(Seed: seedMap * 100);  // Создаем карту температуры

		Parallel.For(0, numWorld, worldId =>
		{
			long seedNumber = long.Parse(seed);
			Random rand = new Random((int)((seedNumber + worldId) % int.MaxValue));
			double seedModifier = char.GetNumericValue(seed[4]) / 100.0;
			if (seedModifier <= 0.02) seedModifier = 0.03;
			if (seedModifier >= 0.06) seedModifier = 0.05;

			double offsetX = rand.NextDouble() * 1000.0;
			double offsetY = rand.NextDouble() * 1000.0;
			double worldOffset = worldId * 5000.0;

			int[] p = GenerateNoiseMap2(rand);
			int[] worldHeightsBlocks = GenerateH((worldOffset, p));

			// Заполнение мира байтами
			byte[] entireWorldBytes = new byte[worldSizeBlocks * worldHeightY];

			Parallel.For(0, worldSizeBlocks, x =>
			{
				int surfaceY = worldHeightsBlocks[x];
				int xOffset = x * worldHeightY;

				for (int y = 0; y < worldHeightY; y++)
				{
					int blockIdx = xOffset + y;
					int caveStartY = surfaceY + upperLimitCave.Upper + (int)(caveMap[x] * upperLimitCave.Lower); 
					int caveStartY2 = surfaceY + lowerLimitCave.Upper + (int)(caveMapLower[x] * lowerLimitCave.Lower);


					if (y < surfaceY)
						entireWorldBytes[blockIdx] = 0;
					else if (y <= surfaceY + layerThicknessEarth)  // Пропускаем слой земли
					{
						// Ничего не делаем — уже покрашено
					}
					else if (y <= caveStartY)
						entireWorldBytes[blockIdx] = 1;
					else if (y >= caveStartY2)
						entireWorldBytes[blockIdx] = 1;
					else
					{
						double sampleX = x + offsetX;
						double sampleY = y + offsetY;
						double noise1 = noise.PerlinNoise2D(sampleX / 180.0, sampleY / 90.0, p) * 1.0;
						double noise2 = noise.PerlinNoise2D(sampleX / 60.0, sampleY / 40.0, p) * 0.4;
						double noise3 = noise.PerlinNoise2D(sampleX / 15.0, sampleY / 10.0, p) * 0.1;
						double finalNoise = (noise1 + noise2 + noise3) / 1.65;

						if (finalNoise > seedModifier * -1 && finalNoise < seedModifier)
							entireWorldBytes[blockIdx] = 0;
						else
							entireWorldBytes[blockIdx] = 1;
					}

					if (y == surfaceY)
					{
						double temperature = temperatureMap[x];

						if (temperature >= 0.8)
							entireWorldBytes[blockIdx] = (byte)BlockId.Sand;
						else if (temperature <= 0.2)
							entireWorldBytes[blockIdx] = (byte)BlockId.Snow;
						else
							entireWorldBytes[blockIdx] = (byte)BlockId.Grass;


						for (int i = 1; i <= layerThicknessEarth; i++)
						{
							int dirtIndex = xOffset + (y + i);

							if (temperature >= 0.8)
							{
								entireWorldBytes[dirtIndex] = (byte)BlockId.Sand;
							}
							else
							{
								entireWorldBytes[dirtIndex] = (byte)BlockId.Earth;
							}
						}
					}

					//entireWorldBytes = GenerationBlocks(temperatureMap, Indexs: (blockIdx, xOffset + y + 1), entireWorldBytes, Pos: (x, y));
				}
			});

			// ========== Нарезка на чанки ==========
			int totalChunksToSave = worldSizeBlocks / chunkHeightX;
			int bytesPerChunk = chunkHeightX * worldHeightY;

			Parallel.For(0, totalChunksToSave, chunkIdx =>
			{
				byte[] binaryChunk = new byte[bytesPerChunk];
				int sourceOffset = chunkIdx * bytesPerChunk;
				Buffer.BlockCopy(entireWorldBytes, sourceOffset, binaryChunk, 0, bytesPerChunk);

				ChunkKey key = new ChunkKey(worldId, chunkIdx);
				worldMemory.TryAdd(key, binaryChunk);
			});
		});

		watch.Stop();
		GD.Print($"\nГотово! Время генериции №1: {watch.ElapsedMilliseconds / 1000.0} секунд");
	}

	private static int NumberFromSeed((int MinNum, int MaxNum) Renge, int BasicNumber = 0, int Factor = 1)
	{
		if (string.IsNullOrEmpty(seed))
			return BasicNumber * Factor;

		// Суммируем все цифры сида для получения числа
		int sum = 0;
		foreach (char c in seed)
		{
			if (char.IsDigit(c))
				sum += (int)char.GetNumericValue(c);
		}

		// Если сумма меньше Renge.MinNum, добавляем Renge.MinNum
		if (sum < Renge.MinNum)
			sum += Renge.MinNum;

		// Ограничиваем 
		sum = sum % (Renge.MaxNum - Renge.MinNum + 1) + Renge.MinNum;

		return sum * Factor;
	}
	private int[] Perm(int Seed = 8)
	{
		int[] p = new int[512];
		int[] perm = new int[256];

		for (int i = 0; i < 256; i++)
			perm[i] = i;

		// Перемешиваем
		Random rng = new Random(Seed);
		for (int i = 255; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			(perm[i], perm[j]) = (perm[j], perm[i]);
		}

		// Удваиваем
		for (int i = 0; i < 512; i++)
			p[i] = perm[i % 256];

		return p;
	}

	private void PrintChunkToConsole(int worldId, int chunkIdx, int startY = 0, int endY = 150)
	{
		// Создаем ключ для чанка
		ChunkKey key = new ChunkKey(worldId, chunkIdx);

		// Достаем массив байт из словаря
		if (!worldMemory.TryGetValue(key, out byte[] chunkBytes))
		{
			GD.Print($"Чанк [{worldId}, {chunkIdx}] не найден в памяти.");
			return;
		}

		GD.Print($"Визуализация чанка {chunkIdx} (Мир {worldId})");

		// Идем по строкам сверху вниз (от неба к земле)
		for (int y = endY; y >= startY; y--)
		{
			string rowText = "";

			// Идем слева направо по ширине чанка 
			for (int localX = 0; localX < chunkHeightX; localX++)
			{
				// Формула индекса такая же, как при генерации
				int blockIdx = (localX * worldHeightY) + y;
				byte blockType = chunkBytes[blockIdx];

				// Подменяем ID блока на красивый символ для консоли
				switch (blockType)
				{
					case 0: rowText += "."; break; // Небо / Воздух
					case 1: rowText += "#"; break; // Земля / Камень
					default: rowText += "?"; break;
				}
			}
			GD.Print(rowText);
		}
	}
	private void PrintWorldSizeInRAM()
	{
		long totalBytes = 0;

		// Считаем размер всех массивов внутри словаря
		foreach (var chunk in worldMemory.Values)
		{
			totalBytes += chunk.Length;
		}

		// Переводим в удобные единицы измерения
		double kilobytes = totalBytes / 1024.0;
		double megabytes = kilobytes / 1024.0;

		GD.Print("\n=========================================");
		GD.Print($"Общий вес мира в ОЗУ:");
		GD.Print($"В байтах: {totalBytes:N0} B");
		GD.Print($"В килобайтах: {kilobytes:F2} KB");
		GD.Print($"В мегабайтах: {megabytes:F2} MB");
		GD.Print($"Всего чанков в памяти: {worldMemory.Count}");
		GD.Print("=========================================");
	}

	private double[] GenerateNoiseMap(int Seed = 42)
	{
		double[] noiseMap = new double[worldSizeBlocks];
		double frequency = 0.005;

		int[] p = Perm(Seed: Seed);

		// Создаем карту
		for (int i = 0; i < worldSizeBlocks; i++)
		{
			double rawNoise = noise.PerlinNoise1D(i * frequency, p);
			noiseMap[i] = (rawNoise + 1.0) / 2.0;
		}

		return noiseMap;
	}

	public int[] GenerateNoiseMap2(Random rand)
	{
		// Перемешиваем таблицу для Perlin
		int[] pTable = Enumerable.Range(0, 256).ToArray();
		for (int i = 255; i > 0; i--)
		{
			int j = rand.Next(i + 1);
			int temp = pTable[i];
			pTable[i] = pTable[j];
			pTable[j] = temp;
		}
		int[] p = new int[1024];
		for (int i = 0; i < 256; i++)
			p[i] = p[i + 256] = p[i + 512] = p[i + 768] = pTable[i];

		return p;
	}
	public int[] GenerateH((double WorldOffset, int[] P) Main)
	{
		// Генерация высот
		int[] worldHeightsBlocks = new int[worldSizeBlocks];

		for (int x = 0; x < worldSizeBlocks; x++)
		{
			double totalNoise = 0.0;
			double totalAmplitude = 0.0;

			double frequency = 0.008;
			double amplitude = 1.0;

			// Скрещиваем октавы
			for (int octave = 0; octave < numOctave1D; octave++)
			{
				double sampleX = (x * frequency) + Main.WorldOffset;
				double noiseVal = noise.PerlinNoise1D(sampleX, Main.P);

				totalNoise += noiseVal * amplitude;
				totalAmplitude += amplitude;

				frequency *= 2.0;
				amplitude *= 0.5;
			}

			// Нормализация
			double finalNoise = totalNoise / totalAmplitude;

			// Преобразование в высоту
			int heightInBlocks = (int)(midBlocksH + (finalNoise * amplitudeBlocks));
			if (heightInBlocks < lowerLimitBlocks) heightInBlocks = lowerLimitBlocks;
			if (heightInBlocks > upperLimitBlocks) heightInBlocks = upperLimitBlocks;

			worldHeightsBlocks[x] = heightInBlocks;
		}

		return worldHeightsBlocks;
	}
}
