using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class FpsCounter : Label
{
	private float _fps;
	private List<float> _times = new List<float>();

	string clueInstructions = "V - hide/show instructions";
	[Export] public CharacterBody2D _player;
	
	public override void _Ready()
	{
		// Устанавливаем позицию в левый верхний угол
		Position = new Vector2(10, 20);
	}
	public override void _Process(double delta)
	{
		_fps = 1.0f / (float)delta;  // Вычисляем FPS
		
		float positionPlayerX = _player.GlobalPosition.X / Storage.TileSize;
		float positionPlayerY = _player.GlobalPosition.Y / Storage.TileSize;

		// Обновляем текст
		Text = $"FPS: {_fps:F2}\nPosition X: {positionPlayerX}\nPosition Y: {positionPlayerY}\n{clueInstructions}";

		_times.Add(_fps);
		if (_times.Count == 30 * 60)
			PrintFpsStatistics();
	}

	private void PrintFpsStatistics()
	{
		if (_times.Count <= 0)
		{
			GD.Print("Нет данных о FPS");
			return;
		}

		var frame20 = _times.Take(20).ToList();
		var sorted = _times.OrderBy(x => x).ToList();

		// Использование LINQ для всей статистики
		var stats = new
		{
			Min = _times.Min(),
			Max = _times.Max(),
			Avg = _times.Average(),
			Median = sorted[sorted.Count / 2],
			Count = _times.Count,
			Frame20Avg = frame20.Average(),
			Frame20Median = frame20.OrderBy(x => x).ElementAt(frame20.Count / 2),
			Mode = frame20.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key
		};

		// Компактный вывод
		GD.Print($"FPS Stats: Min={stats.Min:F1} Max={stats.Max:F1} Avg={stats.Avg:F1} " +
				 $"Med={stats.Median:F1} | 20帧: Avg={stats.Frame20Avg:F1} " +
				 $"Med={stats.Frame20Median:F1} Mode={stats.Mode:F1}");

		_times.Clear();
	}
}
