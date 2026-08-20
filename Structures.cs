using Godot;
using System;

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
