using Godot;
using System.Collections.Generic;
using BlockId = Storage.BlockId;

public partial class HotbarDraw : Node
{
    [Export] public TileMapLayer _map;

    private const int Slots = 10;
    private const int AtlasColumns = 10;

    private readonly List<(int IdBlock, int NumBlocks)> _old = new();

    public override void _Ready()
    {
        for (int i = 0; i < Slots; i++)
            _old.Add((-1, 0));
    }

    public override void _Process(double delta)
    {
        var list = StorageInventory.ListBlocksHotbar;

        int count = Mathf.Min(Slots, list.Count);

        for (int i = 0; i < count; i++)
        {
            var cur = list[i];
            var prev = _old[i];

            // Текстура зависит только от IdBlock — сравниваем только его.
            if (cur.IdBlock == prev.IdBlock)
                continue;

            int cellX = i + StorageInventory.BiasXHotbar;
            int cellY = StorageInventory.BiasYHotbar;

            if (cur.IdBlock == (int)BlockId.Air)
            {
                _map.EraseCell(new Vector2I(cellX, cellY));
            }
            else
            {
                var atlas = new Vector2I(cur.IdBlock % AtlasColumns, cur.IdBlock / AtlasColumns);
                _map.SetCell(new Vector2I(cellX, cellY), 0, atlas);
            }

            _old[i] = cur;
        }
    }
}