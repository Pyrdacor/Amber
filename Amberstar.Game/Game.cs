using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Screens;
using Amberstar.GameData.Serialization;
using IAssetProvider = Amberstar.GameData.IAssetProvider;

namespace Amberstar.Game
{
	public class Game : IDisposable
	{
		public Game(IRenderer renderer, IAssetProvider assetProvider)
		{
			Renderer = renderer;
			AssetProvider = assetProvider;
			ScreenHandler = new(this);

			// Show portrait area
			AddSprite(Layer.Layout, new Position(0, 0), new Size(320, 36), 0);
			// Show layout
			AddSprite(Layer.Layout, new Position(0, 37), new Size(320, 163), 2);

			// Show empty char slots
			for (int i = 0; i < 6; i++)
			{
				var position = new Position(16 + i * 48, 1);
				var size = new Size(32, 36);
				// if (notEmpty) // TODO
					AddColoredRect(Layer.UI, position, size, Color.Black);
				AddSprite(Layer.UI, position, size, i == 0 ? (int)UIGraphic.Skull : (int)UIGraphic.EmptyCharSlot);
			}

			ScreenHandler.PushScreen(ScreenHandler.Create(ScreenType.Map2D));
		}

		internal IRenderer Renderer { get; }
		internal IAssetProvider AssetProvider { get; }
		internal ScreenHandler ScreenHandler { get; }

		public void Update(double delta)
		{
			ScreenHandler.ActiveScreen?.Update(this, delta);
		}

		public void Render(double delta)
		{
			ScreenHandler.ActiveScreen?.Render(this, delta);
		}

		// TODO: Move somewhere else
		ILayer GetRenderLayer(Layer layer) => Renderer.Layers[(int)layer];

		internal ISprite? AddSprite(Layer layer, Position position, Size size, int textureIndex)
		{
			var renderLayer = GetRenderLayer(layer);
			var textureAtlas = renderLayer.Config.Texture!;
			var sprite = renderLayer.SpriteFactory?.Create();

			if (sprite != null)
			{
				sprite.TextureOffset = textureAtlas.GetOffset(textureIndex);
				sprite.Position = position;
				sprite.Size = size;
				sprite.Visible = true;
			}

			return sprite;
		}

		internal IColoredRect? AddColoredRect(Layer layer, Position position, Size size, Color color)
		{
			var renderLayer = GetRenderLayer(layer);
			var coloredRect = renderLayer.ColoredRectFactory?.Create();

			if (coloredRect != null)
			{
				coloredRect.Color = color;
				coloredRect.Position = position;
				coloredRect.Size = size;
				coloredRect.Visible = true;
			}

			return coloredRect;
		}

		public void Dispose()
		{
			ScreenHandler.Dispose();
		}
	}
}
