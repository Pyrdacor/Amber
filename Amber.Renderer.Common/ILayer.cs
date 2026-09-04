using Amber.Common;

namespace Amber.Renderer.Common;

[Flags]
public enum LayerFeatures
{
	None = 0,
	Transparency = 0x1,
	DisplayLayers = 0x2,
	Alpha = 0x4,
	Fog = 0x8,
	Fading = 0x10,
	Tinting = 0x20,
	ColorReplacement = 0x40,
	SkyColorReplacemnet = 0x80,
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
	/// <summary>
	/// Base Z value used to order this layer against other layers.
	/// </summary>
	public float BaseZ { get; init; }
	public LayerRenderTarget RenderTarget { get; init; }
	public LayerFeatures LayerFeatures { get; init; }
	/// <summary>
	/// Texture atlas used by the layer, if any (see <see cref="LayerType.UsesTextures"/>).
	/// </summary>
	public ITextureAtlas? Texture { get; init; }
	/// <summary>
	/// Palette texture used by the layer, if any (see <see cref="LayerType.UsesPalette"/>).
	/// </summary>
	public ITexture? Palette { get; init; }
}

public interface ILayer
{
	/// <summary>
	/// Index used to order this layer's draw calls against other layers.
	/// </summary>
	int Index { get; }
	bool Visible { get; set; }
	LayerType Type { get; }
	LayerConfig Config { get; }
	/// <summary>
	/// Factory for colored rects on this layer, or null if the layer type doesn't support them.
	/// </summary>
	IColoredRectFactory? ColoredRectFactory { get; }
	/// <summary>
	/// Factory for sprites on this layer, or null if the layer type doesn't support them.
	/// </summary>
	ISpriteFactory? SpriteFactory { get; }

	/// <summary>
	/// Renders all drawables on this layer.
	/// </summary>
	void Render(IRenderer renderer);
}

public interface ILayer3D : ILayer
{
	Color? FogColor { get; set; }
	float FogStartDistance { get; set; }
	float FogEndDistance { get; set; }
	Color[]? ReplacementColors { get; set; }
	Color? TintColor { get; set; }
	int? SkyColorIndex { get; set; }
    Color? SkyReplacementColor { get; set; }
	float LightIntensity { get; set; }
    /// <summary>
    /// 0 is fully faded out, 1 is fully visible.
    /// </summary>
    float FadeFactor { get; set; }
    /// <summary>
    /// Factory for 3D surfaces on this layer, or null if the layer type doesn't support them.
    /// </summary>
    ISurface3DFactory? Surface3DFactory { get; }
}

public interface ILayerFactory
{
	/// <summary>
	/// Creates a new layer of the given type with the given configuration.
	/// </summary>
	ILayer Create(LayerType type, LayerConfig config);
}