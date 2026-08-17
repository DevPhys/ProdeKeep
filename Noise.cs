using Godot;
using System;

public class Noise
{
	// Математика шума Перлина (1D и 2D)
	public double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
	public double Lerp(double a, double b, double t) => a + (b - a) * t;

	public double PerlinNoise1D(double x, int[] p)
	{
		int xLeft = (int)Math.Floor(x);
		int xRight = xLeft + 1;
		double t = x - xLeft;
		return Lerp((p[Math.Abs(xLeft) % 256] % 2 == 0 ? -1.0 : 1.0) * t,
					(p[Math.Abs(xRight) % 256] % 2 == 0 ? -1.0 : 1.0) * (t - 1.0), Fade(t)) * 2.0;
	}
	public double PerlinNoise2D(double x, double y, int[] p)
	{
		int X = (int)Math.Floor(x) & 255;
		int Y = (int)Math.Floor(y) & 255;
		double xf = x - Math.Floor(x);
		double yf = y - Math.Floor(y);

		double u = Fade(xf);
		double v = Fade(yf);

		int aa = p[p[X] + Y];
		int ab = p[p[X] + Y + 1];
		int ba = p[p[X + 1] + Y];
		int bb = p[p[X + 1] + Y + 1];

		double grad2d(int hash, double xVal, double yVal)
		{
			int h = hash & 7;
			double uVal = h < 4 ? xVal : yVal;
			double vVal = h < 4 ? yVal : xVal;
			return ((h & 1) == 0 ? uVal : -uVal) + ((h & 2) == 0 ? vVal : -vVal);
		}

		return Lerp(Lerp(grad2d(aa, xf, yf), grad2d(ba, xf - 1, yf), u),
					Lerp(grad2d(ab, xf, yf - 1), grad2d(bb, xf - 1, yf - 1), u), v);
	}
}
