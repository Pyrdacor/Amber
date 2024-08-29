using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.Screens;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;
using EventHandler = Amberstar.Game.Events.EventHandler;
using IAssetProvider = Amberstar.GameData.IAssetProvider;

namespace Amberstar.Game;

/// <summary>
/// Much of the implementation is located in the folder GameRoutines.
/// </summary>
public partial class Game : IDisposable
{
	record TimedAction(long Key, Action Action);

	const long TicksPerSecond = 60;
	const long DefaultFadeTime = 1000;
	double totalTime = 0.0;
	long lastGameTicks = 0;
	long gameTicks = 0;
	readonly ISprite portraitBackgroundSprite;
	readonly ISprite layoutSprite;

	public Game(IRenderer renderer, IAssetProvider assetProvider,
		IGraphicIndexProvider uiGraphicIndexProvider, IPaletteIndexProvider paletteIndexProvider,
		IPaletteColorProvider paletteColorProvider, IFontInfoProvider fontInfoProvider,
		Func<List<Key>> pressedKeyProvider)
	{
		Renderer = renderer;
		AssetProvider = assetProvider;
		GraphicIndexProvider = uiGraphicIndexProvider;
		PaletteIndexProvider = paletteIndexProvider;
		PaletteColorProvider = paletteColorProvider;
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
		Cursor = new(this);
		this.pressedKeyProvider = pressedKeyProvider;

		int uiPaletteIndex = paletteIndexProvider.UIPaletteIndex;

		// Show portrait area
		portraitBackgroundSprite = CreateSprite(Layer.Layout, new Position(0, 0), new Size(320, 36), 0, 14)!;
		// Show layout
		layoutSprite = CreateSprite(Layer.Layout, new Position(0, 37), new Size(320, 163), 0, 14)!;

		// Show empty char slots
		for (int i = 0; i < 6; i++)
		{
			var position = new Position(16 + i * 48, 1);
			var size = new Size(32, 34);
			CreateColoredRect(Layer.UI, position, size, Color.Black);
			CreateSprite(Layer.UI, position, size, i == 0 ? (int)UIGraphic.Skull : (int)UIGraphic.EmptyCharSlot, uiPaletteIndex);
		}

		//ScreenHandler.PushScreen(ScreenHandler.Create(ScreenType.Map2D));

		// TODO: For debugging, remove later
		// In Twinlake
		State.MapIndex = 67;
		State.PartyDirection = Direction.Down;
		//State.SetPartyPosition(7 - 1, 15 - 1);
		State.SetPartyPosition(32 - 1, 9 - 1);
		ScreenHandler.PushScreen(ScreenType.Map3D);
		// In front of crystal
		/*State.MapIndex = 21;
		State.SetPartyPosition(33, 23);
		ScreenHandler.PushScreen(ScreenType.Map2D);*/
	}

	internal IRenderer Renderer { get; }
	internal IAssetProvider AssetProvider { get; }
	internal IGraphicIndexProvider GraphicIndexProvider { get; }
	internal IPaletteIndexProvider PaletteIndexProvider { get; }
	internal IPaletteColorProvider PaletteColorProvider { get; }	
	internal ScreenHandler ScreenHandler { get; }
	internal GameState State { get; }
	internal EventHandler EventHandler { get; }
	internal TextManager TextManager { get; }
	internal Time Time { get; }
	internal event Action<bool>? CanSeeChanged;

	public void Update(double delta)
	{
		totalTime += delta;
		gameTicks = (long)Math.Round(totalTime * TicksPerSecond);

		UpdateFading();

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

	internal bool CanSee()
	{
		// TODO
		return true;
	}	

	public void Dispose()
	{
		ScreenHandler.Dispose();
	}
}
