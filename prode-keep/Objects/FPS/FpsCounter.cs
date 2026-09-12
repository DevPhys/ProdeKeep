using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class FpsCounter : Label
{
	private const string ClueInstructions = "V - hide/show instructions";
	private const float ReportIntervalSec = 30f;

	[Export] public CharacterBody2D _player;

	private readonly List<float> _times = new(1800);
	private float _accum;
	private int _frameSkip;

	public override void _Ready()
	{
		Position = new Vector2(10, 20);
	}

	public override void _Process(double delta)
	{
		if (_player is null) return;

		float fps = (float)Engine.GetFramesPerSecond();

		// текст обновляем реже, чем каждый кадр
		if (_frameSkip++ % 6 == 0)
		{
			float px = _player.GlobalPosition.X / Storage.TileSize;
			float py = _player.GlobalPosition.Y / Storage.TileSize;
			Text = $"FPS: {fps:F2}\nPosition X: {px}\nPosition Y: {py}\n{ClueInstructions}";
		}

		_times.Add(fps);
		_accum += (float)delta;

		if (_accum >= ReportIntervalSec)
		{
			PrintFpsStatistics();
			_accum = 0f;
		}
	}

	private void PrintFpsStatistics()
	{
		if (_times.Count == 0)
		{
			GD.Print("Нет данных о FPS");
			return;
		}

		var sorted = _times.OrderBy(x => x).ToList();

		float min = sorted[0];
		float max = sorted[^1];
		float avg = _times.Average();
		float median = sorted.Count % 2 == 0
			? (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) * 0.5f
			: sorted[sorted.Count / 2];

		// 1% low — среднее по худшему 1% кадров. Полезнее моды.
		int worstCount = Mathf.Max(1, sorted.Count / 100);
		float low1 = sorted.Take(worstCount).Average();

		GD.Print($"FPS Stats ({_times.Count} кадров): " +
				 $"Min={min:F1} Max={max:F1} Avg={avg:F1} Med={median:F1} 1%Low={low1:F1}");

		_times.Clear();
	}
}
