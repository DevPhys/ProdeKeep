using Godot;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;

public readonly struct ChunkKey : System.IEquatable<ChunkKey>
{
	public readonly int WorldId;
	public readonly int ChunkIdx;

	public ChunkKey(int worldId, int chunkIdx)
	{
		WorldId = worldId;
		ChunkIdx = chunkIdx;
	}

	public bool Equals(ChunkKey other) => WorldId == other.WorldId && ChunkIdx == other.ChunkIdx;
	public override bool Equals(object obj) => obj is ChunkKey k && Equals(k);
	public override int GetHashCode() => System.HashCode.Combine(WorldId, ChunkIdx);
}

public class PlayerData
{
	public Vector2 PlayerPos { get; set; }

	public string Seed { get; set; }

	public List<(int IdBlock, int NumBloks)> Hotbar { get; set; }
	public List<(int IdBlock, int NumBloks)> Inventory { get; set; }
}
