using Amber.Common;

namespace Amber.Renderer;

public interface IDrawable
{
	Position Position { get; set; }
	bool Visible { get; set; }
	int Layer { get; set; }
}
