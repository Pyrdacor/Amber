using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Collections;
using Amberstar.Game.Screens;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;
using EventHandler = Amberstar.Game.Events.EventHandler;
using IAssetProvider = Amberstar.GameData.IAssetProvider;

namespace Amberstar.Game;

public class Game : IDisposable
{
	record TimedAction(long Key, Action Action);

	const long TicksPerSecond = 60;
	double totalTime = 0.0;
	long lastGameTicks = 0;
	long gameTicks = 0;
	readonly ISprite portraitBackgroundSprite;
	readonly ISprite layoutSprite;
	readonly Func<List<Key>> pressedKeyProvider;
	List<Key>? pressedKeys = null;
	readonly SortedStack<long, TimedAction> timedActions = new();
	long lastTimedActionKey = -1;
	static Random random = new();

	internal static int Random(int min, int max) => random.Next(min, max);
	internal static int Random(int max) => Random(0, max);
	internal static bool Random() => Random(0, 1) == 1;

	public Game(IRenderer renderer, IAssetProvider assetProvider,
		IGraphicIndexProvider uiGraphicIndexProvider, IPaletteIndexProvider paletteIndexProvider,
		IFontInfoProvider fontInfoProvider, Func<List<Key>> pressedKeyProvider)
	{
		Renderer = renderer;
		AssetProvider = assetProvider;
		GraphicIndexProvider = uiGraphicIndexProvider;
		PaletteIndexProvider = paletteIndexProvider;
		ScreenHandler = new(this);
		try
		{
			State = new(assetProvider.SavegameLoader.LoadSavegame());
		}
		catch
		{
			State = new();
		}
		EventHandler = new(this);
		TextManager = new(this, AssetProvider.FontLoader.LoadFont(), fontInfoProvider);
		Time = new(this);
		this.pressedKeyProvider = pressedKeyProvider;

		int uiPaletteIndex = paletteIndexProvider.UIPaletteIndex;

		// Show portrait area
		portraitBackgroundSprite = AddSprite(Layer.Layout, new Position(0, 0), new Size(320, 36), 0, 14)!;
		// Show layout
		layoutSprite = AddSprite(Layer.Layout, new Position(0, 37), new Size(320, 163), 0, 14)!;

		// Show empty char slots
		for (int i = 0; i < 6; i++)
		{
			var position = new Position(16 + i * 48, 1);
			var size = new Size(32, 34);
			AddColoredRect(Layer.UI, position, size, Color.Black);
			AddSprite(Layer.UI, position, size, i == 0 ? (int)UIGraphic.Skull : (int)UIGraphic.EmptyCharSlot, uiPaletteIndex);
		}

		//ScreenHandler.PushScreen(ScreenHandler.Create(ScreenType.Map2D));

		// TODO: For debugging, remove later
		State.MapIndex = 67;
		State.PartyDirection = Direction.Down;
		State.SetPartyPosition(7 - 1, 15 - 1);
		ScreenHandler.PushScreen(ScreenHandler.Create(ScreenType.Map3D));
	}

	internal IRenderer Renderer { get; }
	internal IAssetProvider AssetProvider { get; }
	internal IGraphicIndexProvider GraphicIndexProvider { get; }
	internal IPaletteIndexProvider PaletteIndexProvider { get; }
	internal ScreenHandler ScreenHandler { get; }
	internal GameState State { get; }
	internal EventHandler EventHandler { get; }
	internal TextManager TextManager { get; }
	internal Time Time { get; }

	public void Update(double delta)
	{
		totalTime += delta;
		gameTicks = (long)Math.Round(totalTime * TicksPerSecond);

		// Execute timed actions which are ready.
		var readyTimedAction = timedActions.Pop(gameTicks);

		if (readyTimedAction != null)
		{
			readyTimedAction.Action();
			return;
		}

		if (gameTicks == lastGameTicks)
			return;

		long elapsed = gameTicks - lastGameTicks;
		lastGameTicks = gameTicks;

		Time.Update(elapsed);
		ScreenHandler.ActiveScreen?.Update(this, elapsed);
		pressedKeys = null; // reset
	}

	public void Render(double delta)
	{
		// TODO: needed?
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

	internal void Teleport(int x, int y, Direction direction, int mapIndex, bool fade)
	{
		if (State.MapIndex != mapIndex)
			fade = true; // always fade when switching maps

		// TODO: fade

		var map = AssetProvider.MapLoader.LoadMap(mapIndex);

		State.SetPartyPosition(x - 1, y - 1);
		State.PartyDirection = direction;
		State.MapIndex = mapIndex;

		if (map.Type == MapType.Map2D)
		{
			if (ScreenHandler.ActiveScreen?.Type == ScreenType.Map2D)
				(ScreenHandler.ActiveScreen as Map2DScreen)!.MapChanged();
			else
			{
				ScreenHandler.ClearAllScreens();
				ScreenHandler.PushScreen(ScreenHandler.Create(ScreenType.Map2D));
			}
		}
		else // 3D
		{
			if (ScreenHandler.ActiveScreen?.Type == ScreenType.Map3D)
				(ScreenHandler.ActiveScreen as Map3DScreen)!.MapChanged();
			else
			{
				ScreenHandler.ClearAllScreens();
				ScreenHandler.PushScreen(ScreenHandler.Create(ScreenType.Map3D));
			}
		}
	}

	internal void ShowText(Action? nextAction = null)
	{
		// TODO
	}

	internal void ShowPictureWithText()
	{
		ScreenHandler.PushScreen(ScreenHandler.Create(ScreenType.PictureText));
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

	internal void SetLayout(Layout layout, byte? paletteIndex = null)
	{
		var renderLayer = GetRenderLayer(Layer.Layout);
		var textureAtlas = renderLayer.Config.Texture!;
		layoutSprite.TextureOffset = textureAtlas.GetOffset((int)layout);

		if (paletteIndex != null)
			layoutSprite.PaletteIndex = paletteIndex.Value;
	}

	internal long AddDelayedAction(long delay, Action action)
	{
		timedActions.Push(gameTicks + delay, new(++lastTimedActionKey, action));
		return lastTimedActionKey;
	}

	internal void DeleteDelayedActions(params long[] keys)
	{
		var lookup = new HashSet<long>(keys);
		timedActions.Remove(timedAction => lookup.Contains(timedAction.Key));
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
