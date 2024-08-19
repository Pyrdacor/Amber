using Amber.Renderer.Common;

namespace Amber.Renderer;

[Flags]
public enum LayerFeatures
{
	Transparency,
	DisplayLayers
}

public enum LayerType
{
	// All palette layers
	Color2D,
	Texture2D,
	ColorAndTexture2D,
	Texture3D,
	Billboard3D,
	Sky,
	Fog,
	Text,
	// Non-palette layers
	Images
	// TODO ...
}

public enum LayerRenderTarget
{
	VirtualScreen2D,
	Map3D,
	Window,
}

public static class LayerTypeExtensions
{
	public static bool UsesTextures(this LayerType type) => type switch
	{
		LayerType.Color2D => false,
		LayerType.Sky => false,
		LayerType.Fog => false,
		_ => true
	};

	public static bool UsesPalette(this LayerType type) => type switch
	{
		LayerType.Color2D => false,
		LayerType.Images => false,
		_ => true
	};
}

public readonly struct LayerConfig
{
	public float BaseZ { get; init; }
	public bool UseVirtualScreen { get; init; }
	public LayerRenderTarget RenderTarget { get; init; }
	public LayerFeatures LayerFeatures { get; init; }
	public ITexture? Texture { get; init; }
	public ITexture? Palette { get; init; }
}

public interface ILayer
{
	int Index { get; }
	bool Visible { get; set; }
	LayerType Type { get; }
	LayerConfig Config { get; }
	IColoredRectFactory? ColoredRectFactory { get; }
	ISpriteFactory? SpriteFactory { get; }

	void Render(IRenderer renderer);
}

public interface ILayerFactory
{
	ILayer Create(LayerType type, LayerConfig config);
}