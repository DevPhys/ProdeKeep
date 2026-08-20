using Godot;

public partial class FpsCounter : Label
{
	private float _fps;
	[Export] public CharacterBody2D _player;
	
	public override void _Ready()
	{
		// Устанавливаем позицию в левый верхний угол
		Position = new Vector2(10, 20);
	}
	
	public override void _Process(double delta)
	{
		_fps = 1.0f / (float)delta;  // Вычисляем FPS
		
		float positionPlayerX = _player.GlobalPosition.X / 16;
		float positionPlayerY = _player.GlobalPosition.Y / 16;
		
		Text = $"FPS: {_fps:F0}\nPosition X: {positionPlayerX}\nPosition Y: {positionPlayerY}";  // Обновляем текст
	}
}
