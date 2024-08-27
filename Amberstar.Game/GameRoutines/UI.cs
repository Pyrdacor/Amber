using Amberstar.GameData;

namespace Amberstar.Game
{
	partial class Game
	{
		internal void SetLayout(Layout layout, byte? paletteIndex = null)
		{
			var renderLayer = GetRenderLayer(Layer.Layout);
			var textureAtlas = renderLayer.Config.Texture!;
			layoutSprite.TextureOffset = textureAtlas.GetOffset((int)layout);

			if (paletteIndex != null)
				layoutSprite.PaletteIndex = paletteIndex.Value;
		}
	}
}
