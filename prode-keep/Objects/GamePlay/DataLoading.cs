using Godot;
using System;

public partial class DataLoading: Node
{
	public override void _Ready()
	{
		LoadAndSave.Load();
	}
}
