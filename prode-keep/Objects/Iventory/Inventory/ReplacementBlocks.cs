using Godot;

using Godot;
using System;

using BlockId = Storage.BlockId;

public partial class ReplacementBlocks : Node
{
    [Export] public TileMapLayer _map;
    [Export] public Sprite2D _block;
    [Export] public Label _numBlock;
    [Export] public Label _nameBlock;

    [Export] public int startXInventory = InventoryLayout.InvStartX;
    [Export] public int endXInventory = InventoryLayout.InvEndX;
    [Export] public int startYInventory = InventoryLayout.InvStartY;
    [Export] public int endYInventory = InventoryLayout.InvEndY;

    [Export] public int startXHotbar = InventoryLayout.HotStartX;
    [Export] public int endXHotbar = InventoryLayout.HotEndX;
    [Export] public int yHotbar = InventoryLayout.HotY;

    int idBlock;
    int numBlocks;

    int biasX = StorageInventory.BiasXInventory;
    int biasY = StorageInventory.BiasYInventory;
    int hightInventory = StorageInventory.HightInventory;

    public static bool redrawing = false;

    int timeRedrawing = 2;
    int storageDevice = 0;

    Vector2I tileCoordsOld;

    public override void _Process(double delta)
    {
        if (redrawing)
        {
            for (int i = 0; i < StorageInventory.ListBlocksHotbar.Count; i++)
            {
                var slot = StorageInventory.ListBlocksHotbar[i];
                if (slot.NumBlocks <= 0 && slot.IdBlock != (int)BlockId.Null)
                    StorageInventory.ListBlocksHotbar[i] = ((int)BlockId.Null, 0);
            }

            if (storageDevice >= timeRedrawing)
            {
                redrawing = false;
                storageDevice = 0;
            }
            else
            {
                storageDevice++;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton ||
            !mouseButton.Pressed ||
            !Gameplayer.isInventory)
            return;

        Vector2 mouseScreenPos = GetViewport().GetMousePosition();
        Vector2I tileCoords = _map.LocalToMap(_map.ToLocal(mouseScreenPos));

        if (mouseButton.ButtonIndex == MouseButton.Left && _block.Texture == null)
        {
            var main = TileTextureReader.Read(_map, tileCoords);

            if (main.BlockId == (int)BlockId.Null)
            {
                _block.Texture = null;
                idBlock = (int)BlockId.Null;
                return;
            }

            bool bl = Re_recording(tileCoords, (int)BlockId.Null, 0);
            if (!bl)
            {
                _block.Texture = null;
                idBlock = (int)BlockId.Null;
                _numBlock.Text = "";
            }

            redrawing = true;
            storageDevice = 0;

            _block.Texture = main.Texture;
            if (numBlocks > 1)
                _numBlock.Text = $"{numBlocks}";
            idBlock = main.BlockId;

            tileCoordsOld = tileCoords;
        }
        else if (mouseButton.ButtonIndex == MouseButton.Left && _block.Texture != null)
        {
            Vector2I atlasCoords = _map.GetCellAtlasCoords(tileCoords);
            int blockId = atlasCoords.Y * TileTextureReader.AtlasColumns + atlasCoords.X;

            if (blockId == (int)BlockId.Null)
            {
                Re_recording(tileCoords, idBlock, numBlocks);

                _block.Texture = null;
                _numBlock.Text = "";
                idBlock = (int)BlockId.Null;
            }
            else
            {
                var main = TileTextureReader.Read(_map, tileCoords);
                bool bl = Re_recording(tileCoords, idBlock, numBlocks);

                if (!bl)
                {
                    _block.Texture = null;
                    idBlock = (int)BlockId.Null;
                    _numBlock.Text = "";
                }

                _block.Texture = main.Texture;
                if (numBlocks > 1)
                    _numBlock.Text = $"{numBlocks}";
                idBlock = main.BlockId;
            }

            redrawing = true;
            storageDevice = 0;
        }
    }

    private bool Re_recording(Vector2I cellCoords, int blockId, int num)
    {
        if (InventorySlotGrid.IsInventoryCell(
                cellCoords,
                startXInventory, endXInventory,
                startYInventory, endYInventory))
        {
            int index = InventorySlotGrid.InventoryIndex(
                cellCoords, biasX, biasY, hightInventory);

            numBlocks = StorageInventory.ListBlocksInventory[index].NumBlocks;

            (int IdBlock, int NumBloks) mainBlock = (blockId, num);
            StorageInventory.ListBlocksInventory[index] = mainBlock;

            return true;
        }
        else if (InventorySlotGrid.IsHotbarCell(
                     cellCoords,
                     startXHotbar, endXHotbar,
                     yHotbar))
        {
            int index = InventorySlotGrid.HotbarIndex(cellCoords, startXHotbar);

            numBlocks = StorageInventory.ListBlocksHotbar[index].NumBlocks;

            (int IdBlock, int NumBloks) mainBlock = (blockId, num);
            StorageInventory.ListBlocksHotbar[index] = mainBlock;
            HotbarPointer.currentBlock = StorageInventory.ListBlocksHotbar[HotbarPointer.currentIndex].IdBlock;

            return true;
        }

        numBlocks = 0;
        return false;
    }
}

public static class TileTextureReader
{
    public const int AtlasColumns = 10;

    /// <summary>
    /// Возвращает текстуру и id блока для клетки тайлмапа.
    /// (null, Null) — если клетка пуста или тайлсет не AtlasSource.
    /// </summary>
    public static (Texture2D Texture, int BlockId) Read(TileMapLayer map, Vector2I cell)
    {
        TileData tileData = map.GetCellTileData(cell);
        if (tileData == null)
            return (null, (int)Storage.BlockId.Null);

        int sourceId = map.GetCellSourceId(cell);
        if (sourceId == (int)Storage.BlockId.Null)
            return (null, (int)Storage.BlockId.Null);

        if (map.TileSet.GetSource(sourceId) is TileSetAtlasSource atlasSource)
        {
            Vector2I atlasCoords = map.GetCellAtlasCoords(cell);
            Rect2I region = atlasSource.GetTileTextureRegion(atlasCoords);

            var tex = new AtlasTexture
            {
                Atlas = atlasSource.Texture,
                Region = region
            };

            int blockId = atlasCoords.Y * AtlasColumns + atlasCoords.X;
            return (tex, blockId);
        }

        return (null, (int)Storage.BlockId.Null);
    }
}

public static class InventoryLayout
{
    // Инвентарь
    public const int InvStartX = 13;
    public const int InvEndX = 33;
    public const int InvStartY = 9;
    public const int InvEndY = 17;

    // Хотбар
    public const int HotStartX = 18;
    public const int HotEndX = 28;
    public const int HotY = 23;

    public const int HotbarSlots = 10;
    public const int AtlasColumns = 10;
}

public static class InventorySlotGrid
{
    public static bool IsInventoryCell(Vector2I cell, int startX, int endX, int startY, int endY)
        => cell.X >= startX && cell.X <= endX &&
           cell.Y >= startY && cell.Y <= endY;

    public static bool IsHotbarCell(Vector2I cell, int startX, int endX, int y)
        => cell.X >= startX && cell.X <= endX && cell.Y == y;

    public static int InventoryIndex(Vector2I cell, int biasX, int biasY, int height)
        => (cell.X - biasX) * height + (cell.Y - biasY);

    public static int HotbarIndex(Vector2I cell, int startX)
        => cell.X - startX;
}