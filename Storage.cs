using Godot;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Storage
{
	// ID блоков
	public enum BlockId : byte
	{
		Air = 0,
		Water = 5,

		Grass = 3,
		Snow = 7,
		Sand = 4,

		Earth = 2,
		Gravel = 6,

		Stone = 1,

		CarbonicOre = 10,
		IronOre = 11,
		GoldOre = 12,
		CopperOre = 13,
		AluminumOre = 14,
		RubyOre = 15,
		DiamondOre = 16,

		Oak = 20,
		Spruce = 21,
		Сacti = 26,

		FoliageOaking = 30,
		FoliageSpruceing = 31,

		Bush = 8,
		DryBush = 9,

		Torch = 38,

		LightSource = 10,
		TileSize = 16,

		DarknessIdForBlocks = 39,
		PenumbraIdForAir = 28,
		DarknessIdForAir = 29,

		Null = 19,
	}

	public static Dictionary<int, string> NameBlocks = new Dictionary<int, string> {
		{ (int)BlockId.Air, "Air" },
		{ (int)BlockId.Null, ""  },
		{ (int)BlockId.Water, "Water" },
		{ (int)BlockId.Grass, "Grass" },
		{ (int)BlockId.Snow, "Snow" },
		{ (int)BlockId.Sand, "Sand" },
		{ (int)BlockId.Earth, "Earth" },
		{ (int)BlockId.Gravel, "Gravel" },
		{ (int)BlockId.Stone, "Stone" },
		{ (int)BlockId.CarbonicOre, "Carbonic Ore" },
		{ (int)BlockId.IronOre, "Iron Ore" },
		{ (int)BlockId.GoldOre, "Gold Ore" },
		{ (int)BlockId.CopperOre, "Copper Ore" },
		{ (int)BlockId.AluminumOre, "Aluminum Ore" },
		{ (int)BlockId.RubyOre, "Ruby Ore" },
		{ (int)BlockId.DiamondOre, "Diamond Ore" },
		{ (int)BlockId.Oak, "Oak" },
		{ (int)BlockId.Spruce, "Spruce" },
		{ (int)BlockId.Сacti, "Cacti" },
		{ (int)BlockId.FoliageOaking, "Foliage Oaking" },
		{ (int)BlockId.FoliageSpruceing, "Foliage Spruceing" },
		{ (int)BlockId.Bush, "Bush" },
		{ (int)BlockId.DryBush, "Dry Bush" },
		{ (int)BlockId.Torch, "Torch" },
	};

	// Ключ — (ID мира, Индекс чанка), Значение — массив байт этого чанка
	public static ConcurrentDictionary<ChunkKey, byte[]> WorldMemory = new ConcurrentDictionary<ChunkKey, byte[]>();

	public HashSet<int> TransparentBlocks;

	public static int WorldSizeBlocks = 200000; // Длина мира
	public static int NumWorld = 1; // Кол-во миров
	public static int WorldH = 650; // глубина
	public static int ChunkW = 25;  // Длина 1 чанка

	public Storage ()
	{
		TransparentBlocks = new HashSet<int>
		{
			(int)BlockId.Air,
			(int)BlockId.Water,

			(int)BlockId.FoliageOaking,
			(int)BlockId.FoliageSpruceing,

			(int)BlockId.Сacti,
			(int)BlockId.Oak,
			(int)BlockId.Spruce,

			(int)BlockId.Bush,
			(int)BlockId.DryBush,

			(int)BlockId.Torch,
			(int)BlockId.Null,
		};
	}
}
