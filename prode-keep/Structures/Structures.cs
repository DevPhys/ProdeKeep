using Godot;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;

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

public class PlayerData
{
	public Vector2 PlayerPos { get; set; }

	public string Seed { get; set; }

	public List<(int IdBlock, int NumBloks)> Hotbar { get; set; }
	public List<(int IdBlock, int NumBloks)> Inventory { get; set; }
}
