using Godot;
using System;

public partial class Cross : Sprite2D
{	
	public override void _Process(double delta)
	{
		GlobalPosition = GetGlobalMousePosition();
	}
}
