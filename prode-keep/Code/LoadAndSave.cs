using Godot;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;

public static class LoadAndSave
{
	public static void Load()
	{
		List<ChunkKey> chunks = FileGame.GetAllSavedChunks();
		for (int i = 0; i < chunks.Count; i++)
		{
			byte[] chunk = FileGame.LoadChunk(chunks[i]);

			if (chunk != null)
				Storage.WorldMemory[chunks[i]] = chunk;
		}

		PlayerData playerData = FileGame.LoadData();
		Storage.playerData = playerData;

		Storage.ListBlocksInventory = playerData.Inventory;
		Storage.ListBlocksHotbar = playerData.Hotbar;
	}

	public static void Save(Vector2 Position)
	{
		List<ChunkKey> chunksSave = Storage.SaveChunks;
		for (int i = 0; i < chunksSave.Count; i++)
		{
			FileGame.SaveChunk(chunksSave[i]);
		}

		FileGame.SaveData(Position);
	}
}
