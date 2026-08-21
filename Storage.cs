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

		CarbonicBlock = 10,
		IronBlock = 11,

		Oak = 20,
		Spruce = 21,
		Сacti = 26,

		FoliageOaking = 30,
		FoliageSpruceing = 31,

		DarknessIdForBlocks = 39,
		PenumbraIdForAir = 28,
		DarknessIdForAir = 29,
		
		Torch = 38,

		LightSource = 10,
		TileSize = 16,
	}

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

			(int)BlockId.Torch,
		};
	}
}
