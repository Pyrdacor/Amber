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
	/// <summary>
	/// Normally the baseline is Y + Height. For render layers which do not
	/// support display layers, this is used to determine the rendering order.
	/// This offset is added to Y + Height to determine the actual baseline.
	/// </summary>
	int BaseLineOffset { get; set; }
	Rect? ClipRect { get; set; }
}

public interface ILayeredDrawable : IDrawable
{
	byte DisplayLayer { get; set; }
}
