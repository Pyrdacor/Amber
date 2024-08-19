namespace Amber.Common;

public static class MathUtil
{
	public static bool FloatEqual(float left, float right)
	{
		return Math.Abs(right - left) < 0.00001f;
	}

	public static int Floor(float f)
	{
		return (int)Math.Floor(f);
	}

	public static int Ceiling(float f)
	{
		return (int)Math.Ceiling(f);
	}

	public static int Round(float f)
	{
		return (int)Math.Round(f);
	}

	public static int Floor(double f)
	{
		return (int)Math.Floor(f);
	}

	public static int Ceiling(double f)
	{
		return (int)Math.Ceiling(f);
	}

	public static int Round(double f)
	{
		return (int)Math.Round(f);
	}

	public static float Limit(float minValue, float value, float maxValue)
	{
		return Math.Max(minValue, Math.Min(value, maxValue));
	}

	public static long Limit(long minValue, long value, long maxValue)
	{
		return Math.Max(minValue, Math.Min(value, maxValue));
	}

	public static uint Limit(uint minValue, uint value, uint maxValue)
	{
		return Math.Max(minValue, Math.Min(value, maxValue));
	}

	public static int Limit(int minValue, int value, int maxValue)
	{
		return Math.Max(minValue, Math.Min(value, maxValue));
	}

	public static short LimitToShort(int value)
	{
		return (short)Limit(short.MinValue, value, short.MaxValue);
	}

	public static float Min(float firstValue, float secondValue, params float[] values)
	{
		float min = Math.Min(firstValue, secondValue);

		foreach (var value in values)
		{
			if (value < min)
				min = value;
		}

		return min;
	}

	public static float Max(float firstValue, float secondValue, params float[] values)
	{
		float max = Math.Max(firstValue, secondValue);

		foreach (var value in values)
		{
			if (value > max)
				max = value;
		}

		return max;
	}

	public static int Min(int firstValue, int secondValue, params int[] values)
	{
		int min = Math.Min(firstValue, secondValue);

		foreach (var value in values)
		{
			if (value < min)
				min = value;
		}

		return min;
	}

	public static int Max(int firstValue, int secondValue, params int[] values)
	{
		int max = Math.Max(firstValue, secondValue);

		foreach (var value in values)
		{
			if (value > max)
				max = value;
		}

		return max;
	}

	public static uint Min(uint firstValue, uint secondValue, params uint[] values)
	{
		uint min = Math.Min(firstValue, secondValue);

		foreach (var value in values)
		{
			if (value < min)
				min = value;
		}

		return min;
	}

	public static uint Max(uint firstValue, uint secondValue, params uint[] values)
	{
		uint max = Math.Max(firstValue, secondValue);

		foreach (var value in values)
		{
			if (value > max)
				max = value;
		}

		return max;
	}
}
