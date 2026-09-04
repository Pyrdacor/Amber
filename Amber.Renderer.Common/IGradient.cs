using Amber.Common;

namespace Amber.Renderer.Common;

public interface IGradient : IDrawable2D
{
	Size Size { get; }
	/// <summary>
	/// Key: Start X or Y
	/// Value: Color to draw from the given start coordinate until the next
	/// </summary>
	Dictionary<int, Color> Colors { get; }
	/// <summary>
	/// If true, colors are laid out top to bottom (Y). Otherwise left to right (X).
	/// </summary>
	bool Vertical { get; set; }
}
