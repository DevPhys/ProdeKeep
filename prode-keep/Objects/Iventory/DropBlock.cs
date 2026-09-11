using Godot;
using System.Collections.Generic;
using System.Collections.Concurrent;

using BlockId = Storage.BlockId;

public partial class DropBlock : CharacterBody2D
{
	// Сила гравитации (пикселей в секунду^2)
	private float _gravity = 980f;

	// Скорость вращения (радиан в секунду)
	private float _rotationSpeed = 3f;

	private Sprite2D _sprite;
	private bool _landed = false;

	public Player _player;
	private int range = 16;

	public int idBlock;
	private const float PickupOffsetY = 32f;

	int xW = Storage.WightInventory;
	int yH = Storage.HightInventory;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Гравитация
		Velocity += new Vector2(0, _gravity * dt);

		var collision = MoveAndCollide(Velocity * dt);

		if (collision != null)
		{
			Velocity = Velocity.Slide(collision.GetNormal());
			if (!_landed) _landed = true;
		}
		else
		{
			_landed = false;
		}

		if (!_landed)
			_sprite.Rotation += _rotationSpeed * dt;

		// Проверка дистанции до игрока
		if (_player != null)
		{
			if (GlobalPosition.DistanceSquaredTo(new Vector2(_player.GlobalPosition.X, _player.GlobalPosition.Y + PickupOffsetY)) < range * range)
			{
				GD.Print("Игрок в радиусе подбора!");

				List<(int IdBlock, int NumBlocks)> listBlocksHotbar = Storage.ListBlocksHotbar;
				bool isHotbar = false;

				for (int i = 0; i < listBlocksHotbar.Count; i++)
				{
					int tileId = listBlocksHotbar[i].IdBlock;
					if (tileId == idBlock || tileId == (int)BlockId.Null)
					{
						int numBlocks = listBlocksHotbar[i].NumBlocks;
						if (numBlocks < 64)
						{
							(int IdBlock, int NumBlocks) mainBlock = (idBlock, numBlocks + 1);
							listBlocksHotbar[i] = mainBlock;

							isHotbar = true;
							break;
						}
					}
				}

				List<(int IdBlock, int NumBlocks)> listBlocksInventory = Storage.ListBlocksInventory;
				bool isPlacedInInventory = false;

				if (!isHotbar)
				{
					for (int y = 0; y < yH; y++)
					{
						for (int x = 0; x < xW; x++)
						{
							int i = x * yH + y;
							int tileId = listBlocksInventory[i].IdBlock;
							if (tileId == idBlock || tileId == (int)BlockId.Null)
							{
								int numBlocks = listBlocksInventory[i].NumBlocks;
								if (numBlocks < 64)
								{
									(int IdBlock, int NumBlocks) mainBlock = (idBlock, numBlocks + 1);
									listBlocksInventory[i] = mainBlock;

									isPlacedInInventory = true;
									break;
								}
							}
						}

						if (isPlacedInInventory) break;
					}
				}

				if (isHotbar || isPlacedInInventory)
					QueueFree();
			}
		}
	}
}
