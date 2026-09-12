using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using BlockId = Storage.BlockId;

public class FileGame
{
	private static string nameFile = "Chunk";
	private static string folderName = "WorldData";
	private static string playerDataFile = "save_data.json";

	private static string folderNameCopy = "WorldDataCopyPlayer";
	private static string playerDataFileCopy = "save_data_copy.json";

	private static readonly byte[] Key = Encoding.UTF8.GetBytes("Rkwpfkstekstj284"); // 16 байт для AES-128
	private static readonly byte[] IV = Encoding.UTF8.GetBytes("MksyEJ4936205739");   // 16 байт

	private static int xW = StorageInventory.WightInventory; 
	private static int yH = StorageInventory.HightInventory;

	// Получаем все доступные блоки (кроме Air и Null)
	private static List<BlockId> availableBlocks = new List<BlockId>
		{
			BlockId.Stone, BlockId.Earth, BlockId.Grass, BlockId.Sand, BlockId.Water,
			BlockId.Gravel, BlockId.Snow, BlockId.Bush, BlockId.DryBush,
			BlockId.CarbonicOre, BlockId.IronOre, BlockId.GoldOre, BlockId.CopperOre,
			BlockId.AluminumOre, BlockId.RubyOre, BlockId.DiamondOre,
			BlockId.Oak, BlockId.Spruce, BlockId.Сacti,
			BlockId.FoliageOaking, BlockId.FoliageSpruceing,
			BlockId.Torch
		};

	private static AdditionalGeneration additional = new AdditionalGeneration();

	private static Encryption encryption;
	static FileGame()
	{
		encryption = new Encryption(Key, IV);
	}


	public static void SaveChunk(ChunkKey key)
	{
		byte[] chunk = Storage.WorldMemory[key];

		int worldId = key.WorldId;
		int chunkId = key.ChunkIdx;

		if (!Directory.Exists(folderName))
		{
			Directory.CreateDirectory(folderName);
		}

		string filePath = Path.Combine(folderName, $"{nameFile}_{worldId}_{chunkId}.bin");
		File.WriteAllBytes(filePath, chunk);
	}
	public static byte[] LoadChunk(ChunkKey key)
	{
		int worldId = key.WorldId;
		int chunkId = key.ChunkIdx;

		string filePath = Path.Combine(folderName, $"{nameFile}_{worldId}_{chunkId}.bin");

		if (!File.Exists(filePath))
		{
			GD.Print($"Файл не найден: {filePath}");
			return null;
		}
		
		return File.ReadAllBytes(filePath);
	}

	public static List<ChunkKey> GetAllSavedChunks()
	{
		List<ChunkKey> chunks = new List<ChunkKey>();

		if (!Directory.Exists(folderName))
			return chunks;

		string[] files = Directory.GetFiles(folderName, "*.bin");

		foreach (string file in files)
		{
			string fileName = Path.GetFileNameWithoutExtension(file);

			string[] parts = fileName.Split('_');
			if (parts.Length == 3)
			{
				int worldId = int.Parse(parts[1]);
				int chunkId = int.Parse(parts[2]);

				chunks.Add(new ChunkKey(worldId, chunkId));
			}
		}
		
		return chunks;
	}

	public static void SaveData(Vector2 PlayerPos)
	{		
		var playerData = new PlayerData
		{
			PlayerPos = PlayerPos,

			Seed = Storage.playerData.Seed,

			Inventory = StorageInventory.ListBlocksInventory,
			Hotbar = StorageInventory.ListBlocksHotbar,
		};

		Save(playerData, folderName, playerDataFile);
		Save(playerData, folderNameCopy, playerDataFileCopy);
	}
	public static PlayerData LoadData()
	{
		PlayerData main = Load(folderName, playerDataFile);
		if (main == null)
		{
			main = Load(folderNameCopy, playerDataFileCopy);
			if (main == null)
			{
				main = CreateDefaultPlayerData();
			}
			Save(main, folderName, playerDataFile);
			Save(main, folderNameCopy, playerDataFileCopy);
		}

		return main;
	}

	private static void Save(PlayerData PlayerData, string FolderName, string NameFile)
	{
		if (!Directory.Exists(FolderName))
		{
			Directory.CreateDirectory(FolderName);
		}

		var options = new JsonSerializerOptions
		{
			WriteIndented = false, // Компактный JSON для экономии места
			IncludeFields = true   // Включать поля, не только свойства
		};

		string jsonString = JsonSerializer.Serialize(PlayerData, options);
		string checksum = encryption.ComputeChecksum(jsonString);

		string dataWithChecksum = $"{checksum}:{jsonString}";
		string encryptedData = encryption.Encrypt(dataWithChecksum);
		string filePath = Path.Combine(FolderName, NameFile);

		// Сохраняем зашифрованные данные в файл
		File.WriteAllText(filePath, encryptedData);
		GD.Print($"Данные сохранены в {filePath}");
	}
	private static PlayerData Load(string FolderName, string NameFile)
	{
		// Полный путь к файлу в папке
		string filePath = Path.Combine(FolderName, NameFile);

		if (!File.Exists(filePath))
		{
			GD.Print($"Ошибка, данные не загружены");
			return null;
		}

		// Читаем зашифрованные данные из файла
		string encryptedData = File.ReadAllText(filePath);

		try
		{
			// Дешифруем данные
			string dataWithChecksum = encryption.Decrypt(encryptedData);

			// Проверяем контрольную сумму
			string[] parts = dataWithChecksum.Split(new[] { ':' }, 2);

			if (parts.Length != 2)
			{
				GD.Print($"Ошибка при загрузки данных");
				return null;
			}

			string expectedChecksum = parts[0];
			string jsonString = parts[1];

			if (encryption.ComputeChecksum(jsonString) != expectedChecksum)
			{
				GD.Print($"Ошибка при загрузки данных");
				return null;
			}

			// Десериализуем обратно в объект
			var options = new JsonSerializerOptions
			{
				IncludeFields = true
			};

			PlayerData playerData = JsonSerializer.Deserialize<PlayerData>(jsonString, options);
			return playerData;
		}
		catch (Exception ex)
		{
			GD.Print($"Ошибка {ex}, данные не загружены");
			return null;
		}
	}

	private static PlayerData CreateDefaultPlayerData()
	{
		List<(int IdBlock, int NumBloks)> listBlocksHotbar = new List<(int IdBlock, int NumBloks)>();
		List<(int IdBlock, int NumBloks)> ListBlocksInventory = new List<(int IdBlock, int NumBloks)>();

		string seed = additional.GenerationSeed();

		for (int i = 0; i < 10; i++)
		{
			listBlocksHotbar.Add(((int)BlockId.Null, 0));
		}

		for (int i = 0; i < xW * yH; i++)
		{
			ListBlocksInventory.Add(((int)BlockId.Null, 0));
		}

		// Заполняем инвентарь по строкам
		for (int y = 0; y < yH; y++)
		{
			for (int x = 0; x < xW; x++)
			{
				int index = y * xW + x;
				int blockIndex = y * xW + x;

				if (blockIndex < availableBlocks.Count)
				{
					(int IdBlock, int NumBloks) mainBlock = ((int)availableBlocks[blockIndex], index + 10);
					ListBlocksInventory[index] = mainBlock;
				}
				// остальные останутся Null
			}
		}

		// Создаём объект с данными
		var playerDataStart = new PlayerData
		{
			PlayerPos = new Vector2(Storage.WorldSizeBlocks / 2 * Storage.TileSize, 100 * Storage.TileSize),

			Seed = seed,

			Hotbar = listBlocksHotbar,
			Inventory = ListBlocksInventory
		};

		return playerDataStart;
	}
}

class Encryption (byte[] key, byte[] iv)
{
	byte[] Key = key;
	byte[] IV = iv;

	public string Encrypt(string plainText)
	{
		using (Aes aes = Aes.Create())
		{
			aes.Key = Key;
			aes.IV = IV;

			ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

			using (MemoryStream ms = new MemoryStream())
			{
				using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
				{
					using (StreamWriter sw = new StreamWriter(cs))
					{
						sw.Write(plainText);
					}
				}
				return Convert.ToBase64String(ms.ToArray());
			}
		}
	}
	public string Decrypt(string cipherText)
	{
		using (Aes aes = Aes.Create())
		{
			aes.Key = Key;
			aes.IV = IV;

			ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

			using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
			{
				using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
				{
					using (StreamReader sr = new StreamReader(cs))
					{
						return sr.ReadToEnd();
					}
				}
			}
		}
	}
	public string ComputeChecksum(string data)
	{
		using (SHA256 sha256 = SHA256.Create())
		{
			byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
			return Convert.ToBase64String(hash);
		}
	}
}
