using System.Numerics;

namespace Amber.Renderer.OpenGL
{
	internal static class MatrixExtensions
	{
		public static float[] ToArray(this Matrix4x4 matrix)
		{
			float[] values = new float[4 * 4];
			int i = 0;

			for (int y = 0; y < 4; y++)
			{
				for (int x = 0; x < 4; x++)
				{
					values[i++] = matrix[x, y];
				}
			}

			return values;
		}
	}
}
