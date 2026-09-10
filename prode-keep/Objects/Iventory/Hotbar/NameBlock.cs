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

	public override void _Process(double delta)
	{
		currentBlock = HotbarPointer.currentBlock;
		text = nameBlocks[currentBlock];

		if (text != oldtext)
			Text = text;

		oldtext = text;
	}
}
