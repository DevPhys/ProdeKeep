using Godot;
using System.Collections.Generic;
using BlockId = Storage.BlockId;

public partial class DropBlock : CharacterBody2D
{
	public Player _player;
	public int idBlock;

	private Sprite2D _sprite;
	private DropPhysics _physics;
	private DropPickupLogic _pickup;

	private bool _landed = false;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_physics = new DropPhysics(this, _sprite);
		_pickup = new DropPickupLogic(this);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		_landed = _physics.Update(dt, _landed);

		if (_pickup.TryPickup(_player, idBlock))
			QueueFree();
	}
}

/// <summary>
/// Отвечает только за движение дропа: гравитация, столкновения и вращение в полёте.
/// </summary>
public class DropPhysics
{
	private const float Gravity = 980f;       // пикс/сек²
	private const float RotationSpeed = 3f;   // рад/сек

	private readonly CharacterBody2D _body;
	private readonly Sprite2D _sprite;

	public DropPhysics(CharacterBody2D body, Sprite2D sprite)
	{
		_body = body;
		_sprite = sprite;
	}

	/// <summary>
	/// Возвращает новое состояние "на земле".
	/// </summary>
	public bool Update(float dt, bool wasLanded)
	{
		_body.Velocity += new Vector2(0, Gravity * dt);

		var collision = _body.MoveAndCollide(_body.Velocity * dt);

		bool landed;
		if (collision != null)
		{
			_body.Velocity = _body.Velocity.Slide(collision.GetNormal());
			landed = true;
		}
		else
		{
			landed = false;
		}

		// Крутится, только пока летит
		if (!landed)
			_sprite.Rotation += RotationSpeed * dt;

		return landed;
	}
}

/// <summary>
/// Отвечает только за подбор дропа: проверка дистанции до игрока,
/// попытка положить в хотбар, потом в инвентарь.
/// </summary>
public class DropPickupLogic
{
	private const float Range = 16f;
	private const float PickupOffsetY = 32f;
	private const int MaxStack = 64;

	private readonly Node2D _owner;

	public DropPickupLogic(Node2D owner)
	{
		_owner = owner;
	}

	/// <summary>
	/// Пытается подобрать блок. Возвращает true, если дроп нужно удалить.
	/// </summary>
	public bool TryPickup(Player player, int idBlock)
	{
		if (player == null)
			return false;

		Vector2 target = new Vector2(
			player.GlobalPosition.X,
			player.GlobalPosition.Y + PickupOffsetY);

		if (_owner.GlobalPosition.DistanceSquaredTo(target) >= Range * Range)
			return false;

		if (TryAddToHotbar(idBlock))
			return true;

		return TryAddToInventory(idBlock);
	}

	private static bool TryAddToHotbar(int idBlock)
	{
		List<(int IdBlock, int NumBlocks)> hotbar = StorageInventory.ListBlocksHotbar;

		for (int i = 0; i < hotbar.Count; i++)
		{
			int tileId = hotbar[i].IdBlock;
			if (tileId != idBlock && tileId != (int)BlockId.Null)
				continue;

			int num = hotbar[i].NumBlocks;
			if (num >= MaxStack)
				continue;

			hotbar[i] = (idBlock, num + 1);
			return true;
		}

		return false;
	}

	private static bool TryAddToInventory(int idBlock)
	{
		List<(int IdBlock, int NumBlocks)> inv = StorageInventory.ListBlocksInventory;

		int xW = StorageInventory.WightInventory;
		int yH = StorageInventory.HightInventory;

		for (int y = 0; y < yH; y++)
		{
			for (int x = 0; x < xW; x++)
			{
				int i = x * yH + y;
				int tileId = inv[i].IdBlock;

				if (tileId != idBlock && tileId != (int)BlockId.Null)
					continue;

				int num = inv[i].NumBlocks;
				if (num >= MaxStack)
					continue;

				inv[i] = (idBlock, num + 1);
				return true;
			}
		}

		return false;
	}
}
