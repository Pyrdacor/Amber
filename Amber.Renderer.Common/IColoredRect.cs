using Amber.Common;

namespace Amber.Renderer.Common;

public interface IColoredRect : ILayeredDrawable, ISizedDrawable
{
	Color Color { get; set; }
}

public interface IColoredRectFactory
{
	IColoredRect Create();
}
