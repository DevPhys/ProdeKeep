using Godot;
using System.Collections.Generic;
using BlockId = Storage.BlockId;

public partial class WorldRenderer : TileMapLayer
{
	[Export] public Camera2D _camera;
	[Export] public Player _player;

	private ChunkStreamer _streamer;

	public override void _Ready()
	{
		_streamer = new ChunkStreamer(this);
	}

	public override void _Process(double delta)
	{
		Vector2 playerPos = _player is not null
			? _player.GlobalPosition
			: _camera.GlobalPosition;

		_streamer.Update(playerPos);

		if (Player.isBlock)
		{
			Storage.WorldMemory[Player.ChunkKey] = Player.Chunk;

			if (!Storage.SaveChunks.Contains(Player.ChunkKey))
				Storage.SaveChunks.Add(Player.ChunkKey);
		}
	}

	public void RedrawChunk(ChunkKey key) => _streamer.RedrawChunk(key);
}

public class ChunkStreamer
{
	private const int ViewDistanceChunks = 4;
	private const int RedrawHalfHeight = 50;

	private readonly ChunkPainter _painter;
	private readonly Generation _generation;
	private readonly Lighting _lighting;

	private readonly List<ChunkKey> _visible = new();
	private readonly List<ChunkKey> _previous = new();

	private readonly int _chunkWidth = Storage.ChunkW;
	private readonly int _chunkHeight = Storage.WorldH;
	private readonly int _tileSize = Storage.TileSize;

	private int _currentChunk = 0;
	private Vector2 _playerPos;

	public ChunkStreamer(TileMapLayer map)
	{
		_painter = new ChunkPainter(map);
		_generation = new Generation();
		_lighting = new Lighting(Storage.TransparentBlocks);
	}

	public void Update(Vector2 playerPos)
	{
		_playerPos = playerPos;

		int chunk = (int)(playerPos.X / (_tileSize * _chunkWidth));
		if (chunk == _currentChunk) return;

		_currentChunk = chunk;
		GenerateAndRenderWorld();
	}

	public void RedrawChunk(ChunkKey key)
	{
		int h1 = Mathf.Max(0, (int)(_playerPos.Y / _tileSize) - RedrawHalfHeight);
		int h2 = Mathf.Min(_chunkHeight, (int)(_playerPos.Y / _tileSize) + RedrawHalfHeight);

		_painter.DrawChunk(
			key, _lighting, _playerPos,
			widthLimit: (0, _chunkWidth),
			heightLimit: (h1, h2));
	}

	private void GenerateAndRenderWorld()
	{
		_previous.Clear();
		_previous.AddRange(_visible);

		UpdateVisible();

		foreach (var key in _previous)
			if (!_visible.Contains(key))
				_painter.ClearChunk(key);

		foreach (var key in _visible)
		{
			if (_previous.Contains(key)) continue;

			_generation.CreationChunk(key.ChunkIdx);
			_painter.DrawChunk(
				key, _lighting, _playerPos,
				widthLimit: (0, _chunkWidth),
				heightLimit: (0, _chunkHeight));
		}

		if (Storage.WorldMemory.Count == 0)
			GD.PrintErr("словарь пуст");
	}

	private void UpdateVisible()
	{
		_visible.Clear();

		for (int i = -ViewDistanceChunks; i <= ViewDistanceChunks; i++)
		{
			int idx = _currentChunk + i;
			if (idx < 0) continue;

			_visible.Add(new ChunkKey(0, idx));
		}
	}
}

public class ChunkPainter
{
	private readonly TileMapLayer _map;

	private readonly int _chunkWidth = Storage.ChunkW;
	private readonly int _chunkHeight = Storage.WorldH;

	private readonly int _penumbraIdForAir = (int)Storage.BlockId.PenumbraIdForAir;
	private readonly int _darknessIdForAir = (int)Storage.BlockId.DarknessIdForAir;
	private readonly int _darknessIdForBlocks = (int)Storage.BlockId.DarknessIdForBlocks;

	public ChunkPainter(TileMapLayer map) => _map = map;

	public void DrawChunk(
		ChunkKey key,
		Lighting lighting,
		Vector2 playerPos,
		(int W1, int W2) widthLimit,
		(int H1, int H2) heightLimit)
	{
		if (!Storage.WorldMemory.TryGetValue(key, out byte[] chunk))
			return;

		int[] border = lighting.Light(key, (playerPos, chunk), heightLimit);
		int offsetX = key.ChunkIdx * _chunkWidth;

		var positions = new Godot.Collections.Array<Vector2I>();
		var atlasCoordsArray = new Godot.Collections.Array<Vector2I>();

		for (int x = widthLimit.W1; x < widthLimit.W2; x++)
		{
			for (int y = heightLimit.H1; y < heightLimit.H2; y++)
			{
				int index = x * _chunkHeight + y;
				int tileId = chunk[index];
				int borderId = border[index];

				if (borderId == 1)      tileId = _penumbraIdForAir;
				else if (borderId == 2) tileId = _darknessIdForAir;
				else if (borderId == 3) tileId = _darknessIdForBlocks;

				positions.Add(new Vector2I(offsetX + x, y));
				atlasCoordsArray.Add(new Vector2I(tileId % 10, tileId / 10));
			}
		}

		FastMassUpdate(positions, atlasCoordsArray);
	}

	public void ClearChunk(ChunkKey key)
	{
		int offsetX = key.ChunkIdx * _chunkWidth;

		for (int x = 0; x < _chunkWidth; x++)
			for (int y = 0; y < _chunkHeight; y++)
				_map.EraseCell(new Vector2I(offsetX + x, y));
	}

	private void FastMassUpdate(
		Godot.Collections.Array<Vector2I> positions,
		Godot.Collections.Array<Vector2I> atlasCoordsArray)
	{
		for (int i = 0; i < positions.Count; i++)
		{
			_map.SetCell(positions[i], sourceId: 0, atlasCoordsArray[i]);
		}
	}
}

public class Lighting
{
	private readonly HashSet<int> transparentBlocks;

	private readonly int chunkWidth = Storage.ChunkW;
	private readonly int chunkHeight = Storage.WorldH;

	public Lighting(HashSet<int> transparentBlocks)
	{
		this.transparentBlocks = transparentBlocks;
	}

	public int[] Light(ChunkKey key, (Vector2 PlayerPos, byte[] ChunkCurrent) Main, (int H1, int H2) ChunkHeightLimit)
	{
		int[] border = new int[chunkHeight * chunkWidth];
		byte[] chunkCurrent = Main.ChunkCurrent;

		bool isLighting = Storage.IsLighting;
		int offsetX = key.ChunkIdx * chunkWidth; // ← не используется, оставил как было

		if (isLighting)
		{
			int lvl = 0;
			for (int x = 0; x < chunkWidth; x++)
			{
				lvl = 0;
				for (int y = 0; y < chunkHeight; y++)
				{
					int index = x * chunkHeight + y;
					int tileId = chunkCurrent[index];

					if (!transparentBlocks.Contains(tileId))
					{
						if (lvl == 0)
							lvl = 1;
						else if (lvl == 1)
						{
							if (!TouchesAir(index, chunkCurrent, chunkWidth, chunkHeight))
								lvl = 2;
						}
					}

					if (tileId == (int)BlockId.Air)
					{
						if (lvl == 0)
							border[index] = (int)BlockId.LightSource;
						else
							border[index] = lvl;
					}
					else
					{
						if (lvl == 2)
						{
							if (tileId == (int)BlockId.Torch)
								border[index] = (int)BlockId.LightSource;
							else
								border[index] = 3;
						}
					}
				}
			}

			lvl = (int)BlockId.LightSource;
			for (int step = 0; step < 10; step++)
			{
				int[] borderCopy = new int[border.Length];
				System.Buffer.BlockCopy(border, 0, borderCopy, 0, border.Length * sizeof(int));

				for (int x = 0; x < chunkWidth; x++)
				{
					for (int y = ChunkHeightLimit.H1; y < ChunkHeightLimit.H2; y++)
					{
						int index = x * chunkHeight + y;
						int tileId = borderCopy[index];

						if (tileId != (int)BlockId.LightSource) continue;

						if (x > 0)
						{
							int iL = (x - 1) * chunkHeight + y;
							if (TouchesAir(iL, chunkCurrent, chunkWidth, chunkHeight))
								border[iL] = lvl;
						}
						if (x < chunkWidth - 1)
						{
							int iR = (x + 1) * chunkHeight + y;
							if (TouchesAir(iR, chunkCurrent, chunkWidth, chunkHeight))
								border[iR] = lvl;
						}
						if (y > 0)
						{
							int iUp = x * chunkHeight + (y - 1);
							if (TouchesAir(iUp, chunkCurrent, chunkWidth, chunkHeight))
								border[iUp] = lvl;
						}
						if (y < chunkHeight - 1)
						{
							int iDown = x * chunkHeight + (y + 1);
							if (TouchesAir(iDown, chunkCurrent, chunkWidth, chunkHeight))
								border[iDown] = lvl;
						}
					}
				}
			}
		}

		return border;
	}

	private bool TouchesAir(int index, byte[] chunk, int chunkWidth, int chunkHeight)
	{
		int x = index / chunkHeight;
		int y = index % chunkHeight;

		if (x > 0 && transparentBlocks.Contains(chunk[(x - 1) * chunkHeight + y]))
			return true;
		if (x < chunkWidth - 1 && transparentBlocks.Contains(chunk[(x + 1) * chunkHeight + y]))
			return true;
		if (y > 0 && transparentBlocks.Contains(chunk[x * chunkHeight + (y - 1)]))
			return true;
		if (y < chunkHeight - 1 && transparentBlocks.Contains(chunk[x * chunkHeight + (y + 1)]))
			return true;

		return false;
	}
}
