using System.Numerics;
using Amber.Common;

namespace Amber.Renderer.Common;

public interface IDrawable
{
	bool Visible { get; set; }
	ILayer Layer { get; }
}

public interface IDrawable2D : IDrawable
{
    Position Position { get; set; }
}

public interface IDrawable3D : IDrawable
{
    Vector3 Position { get; set; }
}

public interface ISizedDrawable : IDrawable2D
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

public interface ILayeredDrawable : IDrawable2D
{
	/// <summary>
	/// Manual draw-order value, used instead of the automatic Y-based ordering
	/// for layers with <see cref="LayerFeatures.DisplayLayers"/>.
	/// </summary>
	byte DisplayLayer { get; set; }
}
