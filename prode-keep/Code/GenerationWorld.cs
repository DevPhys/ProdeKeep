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
	(int Upper, int Lower) LimitIron = (290, 520);  // Границы появления железной руды
	(int Upper, int Lower) LimitGold = (340, 590); // Границы появления золотой руды
	(int Upper, int Lower) LimitCopper = (260, 456); // Границы появления медной руды

	(int Upper, int Lower) LimitAluminum = (280, 500); // Границы появления алюминиевой руды
	(int Upper, int Lower) LimitRuby = (450, 600); // Границы появления рубиновой руды
	(int Upper, int Lower) LimitDiamond = (590, 650); // Границы появления алмазной руды

	// Настройка пещер
	(int Upper, int Lower) upperLimitCave = (31, 53);
	(int Upper, int Lower) lowerLimitCave = (200, 225);

	// Настройка линии рельефа
	(int Upper, int Lower) upperLimitRelief = (15 + 250, 30 + 250);
	int lowerLimitRelief = 3 + 250;

	// Дополнительные настройки генерации
	int layerThicknessEarth = 4;  // Толщина слоя земли
	int numOctave1D = 10; // Количество октав 1д шума

	ConcurrentDictionary<ChunkKey, byte[]> worldMemory = Storage.WorldMemory;

	// Списки шаблонов структур деревьев
	List<byte> listTreesOaks = new List<byte>();  // Дуб
	List<byte> listTreesСacti = new List<byte>();  // Кактус
	List<byte> listTreesSpruce = new List<byte>();  // Ель

	string seed;
	int worldSizeBlocks = Storage.WorldSizeBlocks;
	int numWorld = Storage.NumWorld;
	int worldHeightY = Storage.WorldH;
	int chunkHeightX = Storage.ChunkW;

	int upperLimitBlocks;  // Высота блока
	int lowerLimitBlocks;  // минимальный блок
	double midBlocksH;  // Средняя высота 
	double amplitudeBlocks;  // Амплитуда

	Noise noise = new Noise();
	FileGame fileGame = new FileGame();
	AdditionalGeneration additionalGeneration;

	double[] waterNoise, carbonicNoise;
	double[] ironNoise, goldNoise, copperNoise;
	double[] aluminumNoise, rubyNoise, diamondNoise;
	double[] caveMap, caveMapLower, temperatureMap;

	double seedModifier;

	int[] pChunk; int[] worldHeightsBlocks;
	int[] permOaks, permCacti, permSpruce;
	int[] p;

	public Generation()
	{
		seed = Storage.playerData.Seed;

		additionalGeneration = new AdditionalGeneration(worldSizeBlocks, seed);
		NoiseMapGenerator mapGenerator = new NoiseMapGenerator(additionalGeneration, seed);

		listTreesOaks = mapGenerator.ListTreesOaks;
		listTreesСacti = mapGenerator.ListTreesСacti;
		listTreesSpruce = mapGenerator.ListTreesSpruce;

		p = mapGenerator.P;

		waterNoise = mapGenerator.WaterNoise; carbonicNoise = mapGenerator.CarbonicNoise;
		ironNoise = mapGenerator.IronNoise; goldNoise = mapGenerator.GoldNoise; copperNoise = mapGenerator.CopperNoise;
		aluminumNoise = mapGenerator.AluminumNoise; rubyNoise = mapGenerator.RubyNoise; diamondNoise = mapGenerator.DiamondNoise;
		permOaks = mapGenerator.PermOaks; permCacti = mapGenerator.PermCacti; permSpruce = mapGenerator.PermSpruce;
		caveMap = mapGenerator.CaveMap; caveMapLower = mapGenerator.CaveMapLower; temperatureMap = mapGenerator.TemperatureMap;

		int basicUpperLimitblocks = additionalGeneration.NumberFromSeed(BasicNumber: upperLimitRelief.Lower - 5, Renge: (upperLimitRelief.Upper, upperLimitRelief.Lower));

		// Присваеваем 
		upperLimitBlocks = basicUpperLimitblocks;
		lowerLimitBlocks = lowerLimitRelief;
		midBlocksH = (upperLimitBlocks + lowerLimitBlocks) / 2.0;
		amplitudeBlocks = upperLimitBlocks - lowerLimitBlocks;

		long seedNumber = long.Parse(seed);
		Random rand = new Random((int)((seedNumber) % int.MaxValue));
		seedModifier = char.GetNumericValue(seed[4]) / 100.0;
		if (seedModifier <= 0.02) seedModifier = 0.03;
		if (seedModifier >= 0.06) seedModifier = 0.05;

		pChunk = additionalGeneration.GeneratePermutationTable(rand); // Таблица перестановок
		worldHeightsBlocks = additionalGeneration.GenerateHeightMap(
			permutationTable: pChunk,
			LimitBlocks: (upperLimitBlocks, lowerLimitBlocks),
			Main: (midBlocksH, amplitudeBlocks, numOctave1D)); // Карта высот
	}
	public void CreationChunk(int NumChunk)
	{
		var watch = System.Diagnostics.Stopwatch.StartNew();

		// Локальные переменные для каждого потока
		ChunkKey localKey = new ChunkKey(0, NumChunk);

		if (Storage.WorldMemory.ContainsKey(localKey))
		{
			return;
		}

		// Создаем базовый рельеф с пещерами и биомами
		GenerationChunk(NumChunk);

		if (!worldMemory.TryGetValue(localKey, out byte[] chunk))
		{
			GD.Print($"Чанк {NumChunk} пуст или не обнаружен");
			return;
		}

		// Работаем с локальной копией
		byte[] localChunk = chunk;
		(ChunkKey Key, byte[] Chunk) main = (localKey, localChunk);

		int i = NumChunk;

		GenerationWater(waterNoise, i, main);  // Создаем озера
		GenerationWaterSand(i, main);  // Создаем речной песок на дне озер
		GenerationOre(
			mapNoise1D: carbonicNoise,
			Permutation: p, i,
			Frequency: (0.18, 0.68),
			IDblock: (byte)BlockId.CarbonicOre,
			Limit: LimitCarbonic,
			Main: main);  // Создаем жалежы уголя

		// Создаем металл
		GenerationOre(mapNoise1D: ironNoise,
			Permutation: p, i,
			Frequency: (0.20, 0.75),
			IDblock: (byte)BlockId.IronOre,
			Limit: LimitIron,
			Main: main);  // Создаем жалежы железа
		GenerationOre(mapNoise1D: copperNoise,
			Permutation: p, i,
			Frequency: (0.20, 0.65),
			IDblock: (byte)BlockId.CopperOre,
			Limit: LimitCopper,
			Main: main);  // Создаем жалежы меди
		GenerationOre(
			mapNoise1D: aluminumNoise,
			Permutation: p, i,
			Frequency: (0.20, 0.50),
			IDblock: (byte)BlockId.AluminumOre,
			Limit: LimitAluminum,
			Main: main);  // Создаем жалежы алюминия

		// Создаем драгоценные камни
		GenerationOre(mapNoise1D: rubyNoise,
			Permutation: p, i,
			Frequency: (0.15, 0.98),
			IDblock: (byte)BlockId.RubyOre,
			Limit: LimitRuby,
			Main: main);  // Создаем жалежы рубина
		GenerationOre(mapNoise1D: diamondNoise,
			Permutation: p, i,
			Frequency: (0.16, 0.91),
			IDblock: (byte)BlockId.DiamondOre,
			Limit: LimitDiamond,
			Main: main);  // Создаем жалежы алмазов
		GenerationOre(mapNoise1D: goldNoise,
			Permutation: p, i,
			Frequency: (0.17, 0.88),
			IDblock: (byte)BlockId.GoldOre,
			Limit: LimitGold,
			Main: main);  // Создаем жалежы золота

		// Создаем растительность
		GenerationStructures(Index: i, Step: 5,
			AllowedBlocks: (
				[(byte)BlockId.Air, (byte)BlockId.Grass, (byte)BlockId.Earth],
				[(byte)BlockId.Air],
				[(byte)BlockId.Grass, (byte)BlockId.Earth]),
			SizeStructure: (2, 5),
			ListStructure: listTreesOaks,
			Permutation: permOaks,
			Main: main);  // Размещаем дубы
		GenerationStructures(Index: i, Step: 5,
			AllowedBlocks: (
				[(byte)BlockId.Air],
				[(byte)BlockId.Air],
				[(byte)BlockId.Sand]),
			SizeStructure: (2, 5),
			ListStructure: listTreesСacti,
			Permutation: permCacti,
			Main: main);  // Размещаем кактусы
		GenerationStructures(Index: i, Step: 7,
			AllowedBlocks: (
				[(byte)BlockId.Air, (byte)BlockId.Snow],
				[(byte)BlockId.Air],
				[(byte)BlockId.Snow, (byte)BlockId.Earth]),
			SizeStructure: (4, 7),
			ListStructure: listTreesSpruce,
			Permutation: permSpruce,
			Main: main);  // Размещаем ели

		watch.Stop();

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

	private void GenerationOre(double[] mapNoise1D, int[] Permutation, int Index, int IDblock, (double f, double p) Frequency, (int Upper, int Lower) Limit, (ChunkKey Key, byte[] Chunk) Main)
	{
		int chunkOffsetX = Index * chunkHeightX;

		for (int x = 0; x < chunkHeightX; x++)
		{
			int worldX = chunkOffsetX + x;
			int carbonicUpperLimit = Limit.Upper + (int)(mapNoise1D[worldX] * 5);
			int carbonicLowerLimit = Limit.Lower + (int)(mapNoise1D[worldX] * 4);

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

	private void GenerationChunk(int NumChunk)
	{
		// Заполнение чанк байтами
		byte[] chunk = new byte[chunkHeightX * worldHeightY];

		Parallel.For(NumChunk * chunkHeightX, NumChunk * chunkHeightX + chunkHeightX, x =>
		{
			// x - это мировая координата
			int localX = x - (NumChunk * chunkHeightX); // Локальная координата для записи в chunk
			int surfaceY = worldHeightsBlocks[x];
			int xOffset = localX * worldHeightY;

			for (int y = 0; y < worldHeightY; y++)
			{
				int blockIdx = xOffset + y;
				int caveStartY = surfaceY + upperLimitCave.Upper + (int)(caveMap[x] * upperLimitCave.Lower);
				int caveStartY2 = surfaceY + lowerLimitCave.Upper + (int)(caveMapLower[x] * lowerLimitCave.Lower);

				if (y < surfaceY)
					chunk[blockIdx] = 0;
				else if (y <= surfaceY + layerThicknessEarth)  // Пропускаем слой земли
				{
					// Ничего не делаем — уже покрашено
				}
				else if (y <= caveStartY)
					chunk[blockIdx] = 1;
				else if (y >= caveStartY2)
					chunk[blockIdx] = 1;
				else
				{
					double sampleX = x;  // Используем мировую координату x для шума
					double sampleY = y;
					double noise1 = noise.PerlinNoise2D(sampleX / 180.0, sampleY / 90.0, p) * 1.0;
					double noise2 = noise.PerlinNoise2D(sampleX / 60.0, sampleY / 40.0, p) * 0.4;
					double noise3 = noise.PerlinNoise2D(sampleX / 15.0, sampleY / 10.0, p) * 0.1;
					double finalNoise = (noise1 + noise2 + noise3) / 1.65;

					if (finalNoise > seedModifier * -1 && finalNoise < seedModifier)
						chunk[blockIdx] = 0;
					else
						chunk[blockIdx] = 1;
				}

				if (y == surfaceY)
				{
					double temperature = temperatureMap[x];  // Используем мировую координату x

					if (temperature >= 0.8)
						chunk[blockIdx] = (byte)BlockId.Sand;
					else if (temperature <= 0.2)
						chunk[blockIdx] = (byte)BlockId.Snow;
					else
						chunk[blockIdx] = (byte)BlockId.Grass;

					for (int i = 1; i <= layerThicknessEarth; i++)
					{
						int dirtIndex = xOffset + (y + i);  // xOffset уже локальный

						// Проверка границ
						if (dirtIndex < chunk.Length)
						{
							if (temperature >= 0.8)
							{
								chunk[dirtIndex] = (byte)BlockId.Sand;
							}
							else
							{
								chunk[dirtIndex] = (byte)BlockId.Earth;
							}
						}
					}
				}
			}
		});

		ChunkKey key = new ChunkKey(0, NumChunk);
		worldMemory.TryAdd(key, chunk);
	}
}

public class AdditionalGeneration (int WorldSizeBlocks = 1000, string Seed = "")
{
	string seed = Seed;
	Noise noise = new Noise();

	public string GenerationSeed(int Length = 9)
	{
		// Запуск цикла генерации сида
		System.Random random = new System.Random();
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		for (int i = 0; i < Length; i++)
		{
			sb.Append(random.Next(0, 10));
		}
		seed = sb.ToString(); // Присвоение сида

		return seed;
	}
	public int NumberFromSeed((int MinNum, int MaxNum) Renge, int BasicNumber = 0, int Factor = 1)
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

	public double[] GenerateNoiseMap1D(int Seed = 42)
	{
		double[] noiseMap = new double[WorldSizeBlocks];
		double frequency = 0.005;

		int[] p = Perm(Seed: Seed);

		// Создаем карту
		for (int i = 0; i < WorldSizeBlocks; i++)
		{
			double rawNoise = noise.PerlinNoise1D(i * frequency, p);
			noiseMap[i] = (rawNoise + 1.0) / 2.0;
		}

		return noiseMap;
	}
	public int[] Perm(int Seed = 8)
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

	public int[] GeneratePermutationTable(Random rand)
	{
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
	public int[] GenerateHeightMap(int[] permutationTable, (int Upper, int Lower) LimitBlocks, (double MidBlocksH, double AmplitudeBlocks, int NumOctave1D) Main)
	{
		int[] worldHeightsBlocks = new int[WorldSizeBlocks];

		for (int x = 0; x < WorldSizeBlocks; x++)
		{
			double totalNoise = 0.0;
			double totalAmplitude = 0.0;

			double frequency = 0.008;
			double amplitude = 1.0;

			for (int octave = 0; octave < Main.NumOctave1D; octave++)
			{
				double sampleX = (x * frequency) + 563; // WorldOffset
				double noiseVal = noise.PerlinNoise1D(sampleX, permutationTable);

				totalNoise += noiseVal * amplitude;
				totalAmplitude += amplitude;

				frequency *= 2.0;
				amplitude *= 0.5;
			}

			double finalNoise = totalNoise / totalAmplitude;

			int heightInBlocks = (int)(Main.MidBlocksH + (finalNoise * Main.AmplitudeBlocks));
			if (heightInBlocks < LimitBlocks.Lower) heightInBlocks = LimitBlocks.Lower;
			if (heightInBlocks > LimitBlocks.Upper) heightInBlocks = LimitBlocks.Upper;

			worldHeightsBlocks[x] = heightInBlocks;
		}

		return worldHeightsBlocks;
	}
}
public class NoiseMapGenerator
{
	public double[] WaterNoise { get; private set; }
	public double[] CarbonicNoise { get; private set; }
	public double[] IronNoise { get; private set; }
	public double[] GoldNoise { get; private set; }
	public double[] CopperNoise { get; private set; }
	public double[] AluminumNoise { get; private set; }
	public double[] RubyNoise { get; private set; }
	public double[] DiamondNoise { get; private set; }

	public int[] PermOaks { get; private set; }
	public int[] PermCacti { get; private set; }
	public int[] PermSpruce { get; private set; }

	public double[] CaveMap { get; private set; }
	public double[] CaveMapLower { get; private set; }
	public double[] TemperatureMap { get; private set; }

	public int SeedMap { get; private set; }
	public int[] P { get; private set; }

	public List<byte> ListTreesOaks { get; private set; }
	public List<byte> ListTreesСacti { get; private set; }
	public List<byte> ListTreesSpruce { get; private set; }

	public string Seed { get; private set; }
	AdditionalGeneration additionalGeneration;

	public NoiseMapGenerator(AdditionalGeneration AdditionalGeneration, string SeedWorld)
	{
		additionalGeneration = AdditionalGeneration;
		Seed = SeedWorld;

		InitializeOreNoiseMaps();
		InitializePermutations();
		InitializeCaveAndTemperatureMaps();
		InitializeStructures();
	}

	private void InitializeOreNoiseMaps()
	{
		SeedMap = additionalGeneration.NumberFromSeed(BasicNumber: 92, Factor: 3, Renge: (10, 99));
		P = additionalGeneration.Perm(Seed: SeedMap * 10);

		WaterNoise = additionalGeneration.GenerateNoiseMap1D(Seed: SeedMap * 5);
		CarbonicNoise = additionalGeneration.GenerateNoiseMap1D(Seed: SeedMap + 100);
		IronNoise = additionalGeneration.GenerateNoiseMap1D(Seed: SeedMap + 200);
		GoldNoise = additionalGeneration.GenerateNoiseMap1D(Seed: SeedMap + 300);
		CopperNoise = additionalGeneration.GenerateNoiseMap1D(Seed: SeedMap + 400);
		AluminumNoise = additionalGeneration.GenerateNoiseMap1D(Seed: SeedMap + 500);
		RubyNoise = additionalGeneration.GenerateNoiseMap1D(Seed: SeedMap + 600);
		DiamondNoise = additionalGeneration.GenerateNoiseMap1D(Seed: SeedMap + 700);
	}
	private void InitializePermutations()
	{
		PermOaks = additionalGeneration.Perm(Seed: 10);
		PermCacti = additionalGeneration.Perm(Seed: 11);
		PermSpruce = additionalGeneration.Perm(Seed: 12);
	}
	private void InitializeCaveAndTemperatureMaps()
	{
		int seedMapLocal = additionalGeneration.NumberFromSeed(BasicNumber: 33, Factor: 7, Renge: (10, 99));
		CaveMap = additionalGeneration.GenerateNoiseMap1D(seedMapLocal);
		CaveMapLower = additionalGeneration.GenerateNoiseMap1D(seedMapLocal + 100);
		TemperatureMap = additionalGeneration.GenerateNoiseMap1D(Seed: seedMapLocal + 200);
	}
	private void InitializeStructures()
	{
		// Заполняем списки
		byte FO = (byte)BlockId.FoliageOaking;
		byte O = (byte)BlockId.Oak;
		ListTreesOaks = [
		0, 0, FO, FO, 0,
		0, 0, FO, FO, FO,
		O, O, O, O, FO,
		0, 0, FO, FO, FO,
		0, 0, FO, FO, 0];

		byte C = (byte)BlockId.Сacti;
		ListTreesСacti = [
		0, 0, 0, 0, 0,
		0, 0, C, 0, 0,
		C, C, C, C, C,
		0, 0, 0, C, 0,
		0, 0, 0, 0, 0];

		byte S = (byte)BlockId.Spruce;
		byte FS = (byte)BlockId.FoliageSpruceing;
		ListTreesSpruce = [
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
}
