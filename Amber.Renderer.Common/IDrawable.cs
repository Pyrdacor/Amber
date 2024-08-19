using Amber.Common;

namespace Amber.Renderer;

public interface IDrawable
{
	Position Position { get; set; }
	bool Visible { get; set; }
	ILayer Layer { get; }
}

public interface ISizedDrawable : IDrawable
{
	Size Size { get; set; }
}

public interface ILayeredDrawable : IDrawable
{
	byte DisplayLayer { get; set; }
}
