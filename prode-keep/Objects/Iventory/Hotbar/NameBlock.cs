using Godot;
using System.Collections.Generic;

public partial class NameBlock : Label
{
    private static readonly Dictionary<int, string> NameBlocks = Storage.NameBlocks;

    private string _shown = "";

    public override void _Process(double delta)
    {
        if (!NameBlocks.TryGetValue(HotbarPointer.currentBlock, out string name))
            name = "";

        if (name == _shown)
            return;

        Text = name;
        _shown = name;
    }
}