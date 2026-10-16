using Amber.Common;
using Amber.Renderer.Common;

namespace Amber.Renderer.Headless;

/// <summary>
/// Renderer which only keeps the state of the drawables and never draws anything.
///
/// It is used to run the game without a window or graphics hardware,
/// e.g. for automated tests and scenario scripts.
/// </summary>
public sealed class Renderer(Size size) : IRenderer
{
	readonly List<ILayer> layers = [];
	readonly LayerFactory layerFactory = new();
	readonly TextureFactory textureFactory = new();
	readonly Camera3D camera = new();

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public Size Size { get; private set; } = size;

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public IReadOnlyList<ILayer> Layers => layers.AsReadOnly();

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public ILayerFactory LayerFactory => layerFactory;

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public ITextureFactory TextureFactory => textureFactory;

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public ICamera3D Camera => camera;

	/// <summary>
	/// Counts the render calls, so tests can check that the game loop rendered.
	/// </summary>
	public long RenderCount { get; private set; } = 0;

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public void Render()
	{
		foreach (var layer in layers)
			layer.Render(this);

		RenderCount++;
	}

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public void Resize(Size size) => Size = size;

	// The virtual screen is the screen as there is no window.
	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public Position ToScreen(Position position) => position;

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public Size ToScreen(Size size) => size;

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public Position FromScreen(Position position) => position;

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public Size FromScreen(Size size) => size;

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public void AddLayer(ILayer layer)
	{
		layers.Remove(layer);
		layers.Add(layer);
	}

	/// <summary>
	/// <inheritdoc/>
	/// </summary>
	public void RemoveLayer(ILayer layer) => layers.Remove(layer);
}
