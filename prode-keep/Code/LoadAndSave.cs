using Godot;
using System.Collections.Generic;
using System;

public static class LoadAndSave
{
	public static void Load()
	{
		List<ChunkKey> chunks = FileGame.GetAllSavedChunks();
        foreach (var key in FileGame.GetAllSavedChunks())
        {
            byte[] chunk = FileGame.LoadChunk(key);
            if (chunk is not null)
                Storage.WorldMemory[key] = chunk;
        }

        PlayerData playerData = FileGame.LoadData();
		Storage.playerData = playerData;

        StorageInventory.ListBlocksInventory = playerData.Inventory;
        StorageInventory.ListBlocksHotbar = playerData.Hotbar;
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
