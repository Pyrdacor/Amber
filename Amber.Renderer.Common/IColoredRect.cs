using Amber.Common;

namespace Amber.Renderer;

public interface IColoredRect : ILayeredDrawable, ISizedDrawable
{
	Color Color { get; set; }
}

public interface IColoredRectFactory
{
	IColoredRect Create();
}
