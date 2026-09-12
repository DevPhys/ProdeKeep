using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

public class StorageInventory
{
	public static List<(int IdBlock, int NumBlocks)> ListBlocksInventory = new List<(int IdBlock, int NumBlocks)>();
	public static List<(int IdBlock, int NumBlocks)> ListBlocksHotbar = new List<(int IdBlock, int NumBlocks)>();

	public static int BiasXInventory = 13;
	public static int BiasYInventory = 9;
	public static int HightInventory = 8;
	public static int WightInventory = 20;

	public static int BiasXHotbar = 18;
	public static int BiasYHotbar = 23;
}
