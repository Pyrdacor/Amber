using Amber.Common;
using Amber.Renderer.Common;

namespace Amber.Renderer.Headless;

internal class Layer : ILayer
{
	public Layer(int index, LayerType type, LayerConfig config)
	{
		Type = type;
		Config = config;
		Index = index;

		if (type != LayerType.Texture3D && type != LayerType.Billboard3D)
		{
			if (type.UsesTextures())
				SpriteFactory = new SpriteFactory(this);

			if (type == LayerType.Color2D || type == LayerType.ColorAndTexture2D)
				ColoredRectFactory = new ColoredRectFactory(this);
		}
	}

	public int Index { get; }
	public bool Visible { get; set; } = false;
	public LayerType Type { get; }
	public LayerConfig Config { get; }
	public IColoredRectFactory? ColoredRectFactory { get; }
	public ISpriteFactory? SpriteFactory { get; }

	public void Render(IRenderer renderer)
	{
		// nothing to draw
	}
}

internal sealed class Layer3D : Layer, ILayer3D
{
	public Layer3D(int index, LayerType type, LayerConfig config)
		: base(index, type, config)
	{
		Surface3DFactory = new Surface3DFactory(this);
	}

	public Color? FogColor { get; set; }
	public float FogStartDistance { get; set; } = 10.0f;
	public float FogEndDistance { get; set; } = 15.0f;
	public Color[]? ReplacementColors { get; set; }
	public Color? TintColor { get; set; }
	public int? SkyColorIndex { get; set; }
	public Color? SkyReplacementColor { get; set; }
	public float LightIntensity { get; set; } = 1.0f;
	public float FadeFactor { get; set; } = 1.0f;
	public ISurface3DFactory? Surface3DFactory { get; }
}

internal sealed class LayerFactory : ILayerFactory
{
	int nextIndex = 0;

	public ILayer Create(LayerType type, LayerConfig config)
	{
		return type switch
		{
			LayerType.Texture3D or LayerType.Billboard3D => new Layer3D(nextIndex++, type, config),
			_ => new Layer(nextIndex++, type, config),
		};
	}
}
