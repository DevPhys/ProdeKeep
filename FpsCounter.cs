using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class FpsCounter : Label
{
	private float _fps;
	private List<float> _times = new List<float>();

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

		// Обновляем текст
		Text = $"FPS: {_fps:F0}\nPosition X: {positionPlayerX}\nPosition Y: {positionPlayerY}";

		_times.Add(_fps);
		if (_times.Count == 60 * 60)
			PrintFpsStatistics();
	}

	private void PrintFpsStatistics()
	{
		if (_times.Count == 0)
		{
			GD.Print("Нет данных о FPS");
			return;
		}

		// Вычисляем статистику
		float minFps = _times.Min();
		float maxFps = _times.Max();
		float avgFps = _times.Average();

		// Вычисляем медиану
		var sortedTimes = _times.OrderBy(x => x).ToList();
		float medianFps;
		int middleIndex = sortedTimes.Count / 2;

		if (sortedTimes.Count % 2 == 0)
		{
			medianFps = (sortedTimes[middleIndex - 1] + sortedTimes[middleIndex]) / 2;
		}
		else
		{
			medianFps = sortedTimes[middleIndex];
		}

		// Выводим статистику
		GD.Print("=== Статистика FPS ===");
		GD.Print($"Минимальный FPS: {minFps:F2}");
		GD.Print($"Максимальный FPS: {maxFps:F2}");
		GD.Print($"Средний FPS: {avgFps:F2}");
		GD.Print($"Медианный FPS: {medianFps:F2}");
		GD.Print($"Всего кадров: {_times.Count}");
		GD.Print("=====================");
	}
}
