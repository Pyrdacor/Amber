using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Screens;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;
using IAssetProvider = Amberstar.GameData.IAssetProvider;

namespace Amberstar.Game
{
	public class Game : IDisposable
	{
		const long TicksPerSecond = 60;
		double totalTicks = 0.0;
		long lastGameTicks = 0;
		long gameTicks = 0;
		ISprite portraitBackgroundSprite;
		ISprite layoutSprite;
		readonly Func<List<Key>> pressedKeyProvider;
		List<Key>? pressedKeys = null;

		public Game(IRenderer renderer, IAssetProvider assetProvider,
			IUIGraphicIndexProvider uiGraphicIndexProvider, IPaletteIndexProvider paletteIndexProvider,
			Func<List<Key>> pressedKeyProvider)
		{
			Renderer = renderer;
			AssetProvider = assetProvider;
			UIGraphicIndexProvider = uiGraphicIndexProvider;
			PaletteIndexProvider = paletteIndexProvider;
			ScreenHandler = new(this);
			State = new();
			this.pressedKeyProvider = pressedKeyProvider;

			int uiPaletteIndex = paletteIndexProvider.UIPaletteIndex;

			// Show portrait area
			portraitBackgroundSprite = AddSprite(Layer.Layout, new Position(0, 0), new Size(320, 36), 0, uiPaletteIndex)!;
			// Show layout
			layoutSprite = AddSprite(Layer.Layout, new Position(0, 37), new Size(320, 163), 0, uiPaletteIndex)!;

			// Show empty char slots
			for (int i = 0; i < 6; i++)
			{
				var position = new Position(16 + i * 48, 1);
				var size = new Size(32, 34);
				AddColoredRect(Layer.UI, position, size, Color.Black);
				AddSprite(Layer.UI, position, size, i == 0 ? (int)UIGraphic.Skull : (int)UIGraphic.EmptyCharSlot, uiPaletteIndex);
			}

			/*AddSprite(Layer.UI, new Position(100, 100), new Size(80, 80),
				UIGraphicIndexProvider.Get80x80ImageIndex(Image80x80.Castle),
				PaletteIndexProvider.Get80x80ImagePaletteIndex(Image80x80.Castle), true);*/

			ScreenHandler.PushScreen(ScreenHandler.Create(ScreenType.Map2D));
		}

		internal IRenderer Renderer { get; }
		internal IAssetProvider AssetProvider { get; }
		internal IUIGraphicIndexProvider UIGraphicIndexProvider { get; }
		internal IPaletteIndexProvider PaletteIndexProvider { get; }
		internal ScreenHandler ScreenHandler { get; }
		internal GameState State { get; }

		public void Update(double delta)
		{
			totalTicks += delta;
			gameTicks = (long)Math.Round(totalTicks * TicksPerSecond);

			if (gameTicks == lastGameTicks)
				return;

			long elapsed = gameTicks - lastGameTicks;
			lastGameTicks = gameTicks;

			ScreenHandler.ActiveScreen?.Update(this, elapsed);
			pressedKeys = null; // reset
		}

		public void Render(double delta)
		{
			ScreenHandler.ActiveScreen?.Render(this);
		}

		public void KeyDown(Key key, KeyModifiers keyModifiers)
		{
			ScreenHandler.ActiveScreen?.KeyDown(key, keyModifiers);
		}

		public void KeyUp(Key key, KeyModifiers keyModifiers)
		{
			ScreenHandler.ActiveScreen?.KeyUp(key, keyModifiers);
		}

		public void KeyChar(char ch, KeyModifiers keyModifiers)
		{
			ScreenHandler.ActiveScreen?.KeyChar(ch, keyModifiers);
		}

		public void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
		{
			ScreenHandler.ActiveScreen?.MouseDown(position, buttons, keyModifiers);
		}

		public void MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
		{
			ScreenHandler.ActiveScreen?.MouseUp(position, buttons, keyModifiers);
		}

		public void MouseMove(Position position, MouseButtons buttons)
		{
			ScreenHandler.ActiveScreen?.MouseMove(position, buttons);
		}

		public void MouseWheel(Position position, float scrollX, float scrollY, MouseButtons buttons)
		{
			ScreenHandler.ActiveScreen?.MouseWheel(position, scrollX, scrollY, buttons);
		}

		// TODO: Move somewhere else
		ILayer GetRenderLayer(Layer layer) => Renderer.Layers[(int)layer];

		internal ISprite? AddSprite(Layer layer, Position position, Size size, int textureIndex, int paletteIndex, bool opaque = false)
		{
			var renderLayer = GetRenderLayer(layer);
			var textureAtlas = renderLayer.Config.Texture!;
			var sprite = renderLayer.SpriteFactory?.Create();

			if (sprite != null)
			{
				sprite.TextureOffset = textureAtlas.GetOffset(textureIndex);
				sprite.Position = position;
				sprite.Size = size;
				sprite.PaletteIndex = (byte)paletteIndex;
				sprite.Opaque = opaque;
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

		internal void SetLayout(Layout layout)
		{
			var renderLayer = GetRenderLayer(Layer.Layout);
			var textureAtlas = renderLayer.Config.Texture!;
			layoutSprite.TextureOffset = textureAtlas.GetOffset((int)layout);
		}

		internal bool IsKeyDown(Key key) => (pressedKeys ?? pressedKeyProvider()).Contains(key);

		private static Key KeyByChar(char ch)
		{
			if (ch >= '0' && ch <= '9')
				return Key.Number0 + ch - '0';
			else if (ch >= 'A' && ch <= 'Z')
				return Key.LetterA + ch - 'A';
			else if (ch >= 'a' && ch <= 'z')
				return Key.LetterA + ch - 'a';
			else if (ch == '\n')
				return Key.Enter;
			else if (ch == ' ')
				return Key.Space;
			else
				return Key.Invalid;
		}

		internal bool IsKeyDown(char ch) => (pressedKeys ?? pressedKeyProvider()).Contains(KeyByChar(ch));

		public void Dispose()
		{
			ScreenHandler.Dispose();
		}
	}
}
