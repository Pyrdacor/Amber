using Amber.Common;

namespace Amber.Renderer;

public interface IRenderer
{
	void Render();
	void Resize(Size size);
	Position ToScreen(Position position);
	Size ToScreen(Size size);
	Position FromScreen(Position position);
	Size FromScreen(Size size);
	void AddLayer(ILayer layer);
	void RemoveLayer(ILayer layer);

	Size Size { get; }
	IReadOnlyList<ILayer> Layers { get; }
	ILayerFactory LayerFactory { get; }
}
