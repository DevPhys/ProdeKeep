using Godot;

public partial class FpsCounter : Label
{
	private float _fps;
	
	public override void _Ready()
	{
		// Устанавливаем позицию в левый верхний угол
		Position = new Vector2(10, 20);
	}
	
	public override void _Process(double delta)
	{
		// Вычисляем FPS
		_fps = 1.0f / (float)delta;
		
		// Обновляем текст
		Text = $"FPS: {_fps:F0}";
	}
}
