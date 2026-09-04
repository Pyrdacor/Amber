using Amber.Common;

namespace Amber.Renderer.Common;

public interface IRenderer
{
	/// <summary>
	/// Renders all added layers.
	/// </summary>
	void Render();
	/// <summary>
	/// Resizes the renderer to the given virtual size.
	/// </summary>
	void Resize(Size size);
	/// <summary>
	/// Converts a virtual position to actual screen coordinates.
	/// </summary>
	Position ToScreen(Position position);
	/// <summary>
	/// Converts a virtual size to actual screen coordinates.
	/// </summary>
	Size ToScreen(Size size);
	/// <summary>
	/// Converts an actual screen position to virtual coordinates.
	/// </summary>
	Position FromScreen(Position position);
	/// <summary>
	/// Converts an actual screen size to virtual coordinates.
	/// </summary>
	Size FromScreen(Size size);
	/// <summary>
	/// Adds a layer so it is included in <see cref="Render"/>.
	/// </summary>
	void AddLayer(ILayer layer);
	/// <summary>
	/// Removes a layer so it is no longer rendered.
	/// </summary>
	void RemoveLayer(ILayer layer);

	/// <summary>
	/// Current virtual size of the renderer.
	/// </summary>
	Size Size { get; }
	/// <summary>
	/// All layers currently added to the renderer.
	/// </summary>
	IReadOnlyList<ILayer> Layers { get; }
	/// <summary>
	/// Factory for creating new layers for this renderer.
	/// </summary>
	ILayerFactory LayerFactory { get; }
	/// <summary>
	/// Factory for creating textures for this renderer.
	/// </summary>
	ITextureFactory TextureFactory { get; }
	/// <summary>
	/// The 3D camera used by 3D layers.
	/// </summary>
	ICamera3D Camera { get; }
}
