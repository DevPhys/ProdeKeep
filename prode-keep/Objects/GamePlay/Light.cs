using Godot;
using System;

public partial class Light : CanvasModulate
{
	[Export] public CharacterBody2D _player;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Position = _player.GlobalPosition;
	}
}
