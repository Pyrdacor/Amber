using Amber.Audio;
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

	public const int MaxPartyMembers = 6;
	const long TicksPerSecond = 60;
	const long DefaultFadeTime = 1000;
	double totalTime = 0.0;
	long lastGameTicks = 0;
	long gameTicks = 0;
    readonly ISprite portraitBackgroundSprite;
    readonly ISprite?[] portraitSprites = new ISprite?[MaxPartyMembers];
    readonly ISprite layoutSprite;
	readonly IColoredRect?[] partyMemberNameBackgrounds = new IColoredRect?[MaxPartyMembers];
	readonly IRenderText?[] partyMemberNames = new IRenderText?[MaxPartyMembers];
    readonly IAudioOutput audioOutput;

    public Game(IRenderer renderer, IAssetProvider assetProvider, IAudioOutput audioOutput,
		IGraphicIndexProvider uiGraphicIndexProvider, IPaletteIndexProvider paletteIndexProvider,
		IPaletteColorProvider paletteColorProvider, IFontInfoProvider fontInfoProvider,
		Func<List<Key>> pressedKeyProvider, Action<Position> setMousePosition)
	{
		Renderer = renderer;
		AssetProvider = assetProvider;
		GraphicIndexProvider = uiGraphicIndexProvider;
		PaletteIndexProvider = paletteIndexProvider;
		PaletteColorProvider = paletteColorProvider;
		this.audioOutput = audioOutput;
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
		this.setMousePosition = setMousePosition;

        int uiPaletteIndex = paletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

		// Show portrait area
		portraitBackgroundSprite = CreateSprite(Layer.Layout, new Position(0, 0), new Size(320, 36), 0, uiPaletteIndex)!;
		// Show layout
		layoutSprite = CreateSprite(Layer.Layout, new Position(0, 37), new Size(320, 163), 0, 14)!;

		// Show party member (and empty) slots
		var partyMemberSlots = State.GetPartyMembersWithSlot(this);

		foreach (var partyMemberSlot in partyMemberSlots)
		{
			(int i, IPartyMember? partyMember) = partyMemberSlot;

            var position = new Position(16 + i * 48, 1);
            var size = new Size(32, 34);

            if (partyMember != null)
            {
                var sprite = portraitSprites[i] = CreateSprite(Layer.UI, position, size, GraphicIndexProvider.GetPersonPortraitIndex(1), uiPaletteIndex);
                sprite!.DisplayLayer = 0;

                string name = partyMember.Name;

                if (name.Length > 5)
                    name = name[..5];

                var namePosition = position + new Position(2, size.Height - 4);
                var nameSize = new Size(TextManager.GetTextRenderWidth(name), 6);

                var nameBackground = partyMemberNameBackgrounds[i] = CreateColoredRect(Layer.UI, namePosition, nameSize, Color.Black);
                nameBackground!.DisplayLayer = 5;

                var nameText = partyMemberNames[i] = TextManager.Create(name, 8);
                nameText.ShowInArea(new Rect(namePosition, nameSize), 10);
            }
            else
            {
                portraitSprites[i] = CreateSprite(Layer.UI, position, size, (int)UIGraphic.EmptyCharSlot, uiPaletteIndex);

                Destroy(partyMemberNameBackgrounds[i]);
                partyMemberNameBackgrounds[i] = null;

                Destroy(partyMemberNames[i]);
                partyMemberNames[i] = null;

            }
        }

        ScreenHandler.PushScreen(ScreenType.Map2D);

        // TODO: For debugging, remove later
        // In Twinlake
        //State.MapIndex = 67;
		//State.PartyDirection = Direction.Down;
		//State.SetPartyPosition(7 - 1, 15 - 1);
		//State.SetPartyPosition(32 - 1, 9 - 1);
		//ScreenHandler.PushScreen(ScreenType.Map3D);
		// In front of crystal
		//State.MapIndex = 21;
		//State.SetPartyPosition(33, 23);
		//ScreenHandler.PushScreen(ScreenType.Map2D);
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
		UpdateMusic(delta);

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

		long elapsed = Paused ? 0 : gameTicks - lastGameTicks;
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
