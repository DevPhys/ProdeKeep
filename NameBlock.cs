using Godot;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public partial class NameBlock : Label
{
	int currentBlock;
	Dictionary<int, string> nameBlocks = Storage.NameBlocks;

	string oldtext = "";
	string text = "";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		currentBlock = IventoryNode.currentBlock;
		text = nameBlocks[currentBlock];

		if (text != oldtext)
		{
			Text = text;
		}

		oldtext = text;
	}
}
