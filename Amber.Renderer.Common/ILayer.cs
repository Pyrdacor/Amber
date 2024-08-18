using Amber.Renderer.Common;

namespace Amber.Renderer;

[Flags]
public enum LayerFeatures
{
	ColoredRects,
	Sprites,
	Palette
}

public struct LayerConfig
{
	public float BaseZ { get; init; }
	public bool UseVirtualScreen { get; init; }
	public LayerFeatures LayerFeatures { get; init; }
	public ITexture? Texture { get; init; }
	public ITexture? Palette { get; init; }
}

public interface ILayer
{
	int Index { get; }
	bool Visible { get; set; }
	LayerConfig Config { get; }
	ISpriteFactory SpriteFactory { get; }

	void Render(IRenderer renderer);
}

public interface ILayerFactory
{
	ILayer Create(LayerConfig config);
}