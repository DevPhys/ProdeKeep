using Godot;
using System;
using System.Collections.Concurrent;
using BlockId = Storage.BlockId;

public partial class Player : CharacterBody2D
{
    [ExportGroup("Settings Player")]
    [Export] public float Speed = 200.0f;
    [Export] public float SwingSpeed = 10.0f;
    [Export] public float SwingAngleDeg = 30.0f;
    [Export] public float JumpVelocity = -400.0f;
    [Export] public float Gravity = 980.0f;

    [ExportGroup("World")]
    [Export] public WorldRenderer worldRenderer;
    [Export] public TileMapLayer _map;

    [ExportGroup("Settings Drop")]
    [Export] public PackedScene _blockScene;
    [Export] public float _scaleDrop = 0.33f;
    [Export] private Node2D _world;

    // Эти поля публичные — на них смотрят хелперы
    public Marker2D _torso;
    public Marker2D _head;
    public Marker2D _hand1;
    public Marker2D _hand2;
    public Marker2D _leg1;
    public Marker2D _leg2;

    public ConcurrentDictionary<ChunkKey, byte[]> worldMemory = new();

    public int rangeBlock = 4;
    public int sizeBlock = Storage.TileSize;

    public static byte[] Chunk;
    public static ChunkKey ChunkKey;
    public static bool isBlock = false;

    public bool isInventory;

    // Хелперы
    private PlayerAnimator _animator;
    private PlayerBlockInteractor _blockInteractor;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Hidden;

        _torso = GetNode<Marker2D>("Torso");
        _head = GetNode<Marker2D>("Head");
        _hand1 = GetNode<Marker2D>("hand1");
        _hand2 = GetNode<Marker2D>("hand2");
        _leg1 = GetNode<Marker2D>("leg1");
        _leg2 = GetNode<Marker2D>("leg2");

        Position = Storage.playerData.PlayerPos;
        worldMemory = Storage.WorldMemory;

        _animator = new PlayerAnimator(this);
        _blockInteractor = new PlayerBlockInteractor(this, worldRenderer, _map, _blockScene, _world);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector2 inputDir = Vector2.Zero;
        isInventory = Gameplayer.isInventory;

        if (!isInventory)
        {
            if (Input.IsKeyPressed(Key.A)) inputDir.X -= 1;
            else if (Input.IsKeyPressed(Key.D)) inputDir.X += 1;

            if (Input.IsKeyPressed(Key.W) && IsOnFloor())
                Velocity = new Vector2(Velocity.X, JumpVelocity);
        }

        if (!IsOnFloor())
            Velocity += new Vector2(0, Gravity * dt);

        Velocity = new Vector2(inputDir.X * Speed, Velocity.Y);

        MoveAndSlide();
        _animator.Update(dt, inputDir, isInventory);
    }

    public override void _Input(InputEvent ev)
    {
        _blockInteractor.HandleInput(ev, isInventory);
    }

    public override void _ExitTree()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
}

public class PlayerAnimator
{
    private readonly Player _player;
    private readonly Marker2D _torso, _head, _hand1, _hand2, _leg1, _leg2;

    private float _time = 0.0f;

    public PlayerAnimator(Player player)
    {
        _player = player;
        _torso = player._torso;
        _head = player._head;
        _hand1 = player._hand1;
        _hand2 = player._hand2;
        _leg1 = player._leg1;
        _leg2 = player._leg2;
    }

    public void Update(float dt, Vector2 inputDir, bool isInventory)
    {
        Vector2 mousePos = _player.GetGlobalMousePosition();

        // --- Разворот и голова за мышкой ---
        if (!isInventory)
        {
            if (mousePos.X < _player.GlobalPosition.X)
            {
                _torso.Scale = new Vector2(-1, 1);
                _head.Scale = new Vector2(-1, 1);
            }
            else
            {
                _torso.Scale = new Vector2(1, 1);
                _head.Scale = new Vector2(1, 1);
            }

            Vector2 localMousePos = _torso.ToLocal(mousePos);
            if (_head.Scale.X == -1)
                localMousePos.Y = -localMousePos.Y;

            _head.Rotation = localMousePos.Angle();
        }

        // --- Руки ---
        if (Mathf.Abs(_player.Velocity.X) > 0.1f)
        {
            _time += dt * _player.SwingSpeed;
            float swingRad = Mathf.DegToRad(_player.SwingAngleDeg);
            float swing = Mathf.Sin(_time) * swingRad;

            _hand1.Rotation = swing;
            _hand2.Rotation = -swing;
        }
        else
        {
            _time = 0.0f;
            _hand1.Rotation = 0;
            _hand2.Rotation = 0;
        }

        // --- Ноги ---
        if (Mathf.Abs(inputDir.X) > 0.1f)
        {
            float swingRad = Mathf.DegToRad(_player.SwingAngleDeg);
            float swing = Mathf.Sin(_time) * swingRad;

            _leg1.Rotation = -swing;
            _leg2.Rotation = swing;
        }
        else
        {
            _leg1.Rotation = 0;
            _leg2.Rotation = 0;
        }
    }
}

public class PlayerBlockInteractor
{
    private readonly Player _player;
    private readonly WorldRenderer _worldRenderer;
    private readonly TileMapLayer _map;
    private readonly PackedScene _blockScene;
    private readonly Node2D _world;
    private readonly Random _random = new();

    public PlayerBlockInteractor(Player player,
                                 WorldRenderer worldRenderer,
                                 TileMapLayer map,
                                 PackedScene blockScene,
                                 Node2D world)
    {
        _player = player;
        _worldRenderer = worldRenderer;
        _map = map;
        _blockScene = blockScene;
        _world = world;
    }

    public void HandleInput(InputEvent ev, bool isInventory)
    {
        if (ev is not InputEventMouseButton mouseEvent || !mouseEvent.Pressed || isInventory)
        {
            Player.isBlock = false;
            return;
        }

        Vector2 mousePos = _player.GetGlobalMousePosition();
        Vector2 playerPos = _player.GlobalPosition;

        float distanceInBlocks = playerPos.DistanceTo(mousePos) / _player.sizeBlock;
        if (distanceInBlocks > _player.rangeBlock)
        {
            Player.isBlock = false;
            return;
        }

        int blockX = Mathf.FloorToInt(mousePos.X / 16.0f);
        int blockY = Mathf.FloorToInt(mousePos.Y / 16.0f);

        int localX = ((blockX % Storage.ChunkW) + Storage.ChunkW) % Storage.ChunkW;
        int currentChunk = Mathf.FloorToInt((float)blockX / Storage.ChunkW);

        if (blockY < 0 || blockY >= Storage.WorldH)
            return;

        int blockIndex = localX * Storage.WorldH + blockY;
        var chunkKey = new ChunkKey(worldId: 0, chunkIdx: currentChunk);
        var cellCoords = new Vector2I(blockX, blockY);

        if (mouseEvent.ButtonIndex == MouseButton.Left)
        {
            Player.isBlock = true;
            DeleteBlock(chunkKey, blockIndex, cellCoords);
        }
        else if (mouseEvent.ButtonIndex == MouseButton.Right)
        {
            Player.isBlock = true;
            NewBlock(chunkKey, blockIndex);
        }
    }

    private void DeleteBlock(ChunkKey chunkKey, int blockIndex, Vector2I cellCoords)
    {
        if (!_player.worldMemory.TryGetValue(chunkKey, out var chunk))
            return;

        if (blockIndex < 0 || blockIndex >= chunk.Length)
        {
            GD.Print($"Индекс {blockIndex} вне массива размером {chunk.Length}");
            return;
        }

        int tileId = chunk[blockIndex];

        chunk[blockIndex] = 0;
        Player.Chunk = chunk;
        Player.ChunkKey = chunkKey;

        _worldRenderer.RedrawChunk(chunkKey);

        if (!Storage.NoDropBlocks.Contains(tileId))
        {
            SetSpawnDrop(_player._scaleDrop,
                new Vector2(cellCoords.X * 16 + _random.Next(3, 13),
                            cellCoords.Y * 16 + _random.Next(0, 8)),
                tileId);
        }
    }

    private void NewBlock(ChunkKey chunkKey, int blockIndex)
    {
        if (!_player.worldMemory.TryGetValue(chunkKey, out var chunk))
            return;

        if (blockIndex < 0 || blockIndex >= chunk.Length)
        {
            GD.Print($"Индекс {blockIndex} вне массива размером {chunk.Length}");
            return;
        }

        var blockMain = StorageInventory.ListBlocksHotbar[HotbarPointer.currentIndex];

        if (chunk[blockIndex] == 0 || chunk[blockIndex] == (int)BlockId.Null)
        {
            if (blockMain.NumBlocks > 0)
            {
                chunk[blockIndex] = (byte)blockMain.IdBlock;

                var newBlockMain = (blockMain.IdBlock, blockMain.NumBlocks - 1);
                StorageInventory.ListBlocksHotbar[HotbarPointer.currentIndex] = newBlockMain;
            }
        }

        HotbarPointer.currentBlock = StorageInventory.ListBlocksHotbar[HotbarPointer.currentIndex].IdBlock;

        Player.Chunk = chunk;
        Player.ChunkKey = chunkKey;

        _worldRenderer.RedrawChunk(chunkKey);
    }

    private Texture2D GetTextureAtCell(Vector2I atlasCoords)
    {
        var source = _map.TileSet.GetSource(0) as TileSetAtlasSource;
        if (source == null)
        {
            GD.PushWarning("Источник 0 не TileSetAtlasSource");
            return null;
        }
        if (!source.HasTile(atlasCoords))
        {
            GD.PushWarning($"Нет тайла {atlasCoords}");
            return null;
        }

        Rect2I region = source.GetTileTextureRegion(atlasCoords);

        return new AtlasTexture { Atlas = source.Texture, Region = region };
    }

    private void SetSpawnDrop(float scaleDrop, Vector2 position, int tileId)
    {
        int atlasX = tileId % 10;
        int atlasY = tileId / 10;

        Texture2D texture = GetTextureAtCell(new Vector2I(atlasX, atlasY));

        var block = _blockScene.Instantiate<DropBlock>();
        var sprite = block.GetNode<Sprite2D>("Sprite2D");

        _world.AddChild(block);

        sprite.Texture = texture;
        sprite.Scale = Vector2.One * scaleDrop;
        sprite.Rotation = (float)GD.RandRange(0, Mathf.Tau);

        block.GlobalPosition = position;
        block._player = _player;
        block.idBlock = tileId;
    }
}