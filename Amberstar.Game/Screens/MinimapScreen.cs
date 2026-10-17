using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The map view of the map show spell (Minimap_Mod in MAGIC.ASM of the original).
///
/// Every map tile is shown as 2x2 pixels in its minimap color. Overlays of 2D maps
/// only cover the upper right and the lower left pixel. The view shows 88x72 tiles
/// and is scrolled in steps of 8 tiles. The party position blinks.
/// </summary>
internal sealed class MinimapScreen : ButtonGridScreen
{
	const int PixelsPerTile = 2;
	const int TilesPerStep = 8;
	const int ViewWidthInSteps = 11;
	const int ViewHeightInSteps = 9;
	const int ExitButtonIndex = 4;
	const byte BackgroundDisplayLayer = 5;
	const byte UnderlayDisplayLayer = 6;
	const byte OverlayDisplayLayer = 7;
	const byte PartyDisplayLayer = 8;
	const long TicksPerPartyColor = Game.TicksPerSecond / 15;
	static readonly Position mapOffset = new(16, 49);
	readonly List<IColoredRect> rects = [];
	IColoredRect? background;
	IColoredRect? partyMarker;
	Label? headline;
	// minimap color index per tile and overlay color index (-1 = none)
	int[] colors = [];
	int[] overlayColors = [];
	int mapWidth;
	int mapHeight;
	byte mapPalette;
	int viewX; // in steps of 8 tiles
	int viewY;
	int partyColorIndex;
	long partyColorTicks;

	public override ScreenType Type { get; } = ScreenType.Minimap;

	internal override byte ButtonGridPaletteIndex => Palette;

	byte Palette => Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Automap];

	int WidthInSteps => (mapWidth + TilesPerStep - 1) / TilesPerStep;

	int HeightInSteps => (mapHeight + TilesPerStep - 1) / TilesPerStep;

	public override void Open(Action? closeAction)
	{
		LoadMapColors();

		// Note: The original always starts at the upper left corner. We start at the party.
		var partyPosition = Game.State.PartyPosition;
		viewX = Math.Clamp(partyPosition.X / TilesPerStep - ViewWidthInSteps / 2, 0, Math.Max(0, WidthInSteps - ViewWidthInSteps));
		viewY = Math.Clamp(partyPosition.Y / TilesPerStep - ViewHeightInSteps / 2, 0, Math.Max(0, HeightInSteps - ViewHeightInSteps));

		base.Open(closeAction);

		Game.SetLayout(Layout.Automap, Palette);
		Game.Cursor.CursorType = CursorType.Sword;

		var map = Game.ScreenHandler.FindMapScreen() switch
		{
			Map2DScreen map2DScreen => (IMap)map2DScreen.Map,
			Map3DScreen map3DScreen => map3DScreen.Map,
			_ => null
		};

		headline = AddLabel(mapOffset.X, 39, ViewWidthInSteps * TilesPerStep * PixelsPerTile, 7, 20);
		headline.SetText(map?.Name ?? "", 15, TextManager.TransparentPaper, Palette);
		headline.Alignment = TextAlignment.Center;

		background = Game.CreateColoredRect(Layer.UI, mapOffset, new Size(ViewWidthInSteps * TilesPerStep * PixelsPerTile, ViewHeightInSteps * TilesPerStep * PixelsPerTile), GetColor(0));
		background!.DisplayLayer = BackgroundDisplayLayer;

		ShowMap();
	}

	public override void Close()
	{
		ClearMap();
		Game.Destroy(background);
		background = null;

		base.Close();
	}

	private void LoadMapColors()
	{
		switch (Game.ScreenHandler.FindMapScreen())
		{
			case Map2DScreen map2DScreen:
			{
				var map = map2DScreen.Map;
				mapWidth = map.Width;
				mapHeight = map.Height;
				mapPalette = map2DScreen.Palette;
				colors = new int[mapWidth * mapHeight];
				overlayColors = new int[mapWidth * mapHeight];

				for (int i = 0; i < colors.Length; i++)
				{
					var tile = map.Tiles[i];
					colors[i] = tile.Underlay == 0 ? 0 : map2DScreen.GetMinimapColorIndex(tile.Underlay);
					overlayColors[i] = tile.Overlay == 0 ? -1 : map2DScreen.GetMinimapColorIndex(tile.Overlay);
				}
				break;
			}
			case Map3DScreen map3DScreen:
			{
				var map = map3DScreen.Map;
				mapWidth = map.Width;
				mapHeight = map.Height;
				mapPalette = map3DScreen.Palette;
				colors = map.Tiles.Select(tile => tile.LabTileIndex == 0 ? 0 : map.LabTiles[tile.LabTileIndex - 1].MinimapColorIndex).ToArray();
				overlayColors = [.. Enumerable.Repeat(-1, colors.Length)];
				break;
			}
			default:
				throw new AmberException(ExceptionScope.Application, "The minimap needs a map.");
		}
	}

	private Color GetColor(int colorIndex) => Game.PaletteColorProvider.GetPaletteColor(mapPalette, colorIndex & 0xf);

	private void AddRect(int x, int y, int width, int height, int colorIndex, byte displayLayer)
	{
		var rect = Game.CreateColoredRect(Layer.UI, mapOffset + new Position(x, y), new Size(width, height), GetColor(colorIndex));

		if (rect != null)
		{
			rect.DisplayLayer = displayLayer;
			rects.Add(rect);
		}
	}

	private void ClearMap()
	{
		rects.ForEach(rect => rect.Visible = false);
		rects.Clear();
		Game.Destroy(partyMarker);
		partyMarker = null;
	}

	private void ShowMap()
	{
		ClearMap();

		int startX = viewX * TilesPerStep;
		int startY = viewY * TilesPerStep;
		int width = Math.Min(ViewWidthInSteps * TilesPerStep, mapWidth - startX);
		int height = Math.Min(ViewHeightInSteps * TilesPerStep, mapHeight - startY);

		for (int y = 0; y < height; y++)
		{
			int rowOffset = (startY + y) * mapWidth + startX;
			int runStart = 0;

			// tiles with the same color are drawn as one rectangle
			for (int x = 1; x <= width; x++)
			{
				if (x == width || colors[rowOffset + x] != colors[rowOffset + runStart])
				{
					AddRect(runStart * PixelsPerTile, y * PixelsPerTile, (x - runStart) * PixelsPerTile, PixelsPerTile, colors[rowOffset + runStart], UnderlayDisplayLayer);
					runStart = x;
				}
			}

			for (int x = 0; x < width; x++)
			{
				int overlayColor = overlayColors[rowOffset + x];

				if (overlayColor == -1)
					continue;

				AddRect(x * PixelsPerTile + 1, y * PixelsPerTile, 1, 1, overlayColor, OverlayDisplayLayer);
				AddRect(x * PixelsPerTile, y * PixelsPerTile + 1, 1, 1, overlayColor, OverlayDisplayLayer);
			}
		}

		var partyPosition = Game.State.PartyPosition;
		int partyX = partyPosition.X - startX;
		int partyY = partyPosition.Y - startY;

		if (partyX >= 0 && partyY >= 0 && partyX < width && partyY < height)
		{
			partyMarker = Game.CreateColoredRect(Layer.UI, mapOffset + new Position(partyX * PixelsPerTile, partyY * PixelsPerTile),
				new Size(PixelsPerTile, PixelsPerTile), GetColor(partyColorIndex));
			partyMarker!.DisplayLayer = PartyDisplayLayer;
		}

		RequestButtonSetup();
	}

	public override void Update(long elapsedTicks)
	{
		base.Update(elapsedTicks);

		partyColorTicks += elapsedTicks;

		if (partyColorTicks >= TicksPerPartyColor)
		{
			partyColorTicks %= TicksPerPartyColor;
			partyColorIndex = (partyColorIndex + 1) & 0xf;

			if (partyMarker != null)
				partyMarker.Color = GetColor(partyColorIndex);
		}
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		buttonGrid.SetButton(0, ButtonType.ArrowUpLeft);
		buttonGrid.SetButton(1, ButtonType.ArrowUp);
		buttonGrid.SetButton(2, ButtonType.ArrowUpRight);
		buttonGrid.SetButton(3, ButtonType.ArrowLeft);
		buttonGrid.SetButton(ExitButtonIndex, ButtonType.Exit);
		buttonGrid.SetButton(5, ButtonType.ArrowRight);
		buttonGrid.SetButton(6, ButtonType.ArrowDownLeft);
		buttonGrid.SetButton(7, ButtonType.ArrowDown);
		buttonGrid.SetButton(8, ButtonType.ArrowDownRight);

		// Scrolling is only possible if the map is larger than the view.
		bool horizontal = WidthInSteps > ViewWidthInSteps;
		bool vertical = HeightInSteps > ViewHeightInSteps;

		foreach (int index in new[] { 0, 2, 3, 5, 6, 8 })
			buttonGrid.EnableButton(index, horizontal);

		foreach (int index in new[] { 0, 1, 2, 6, 7, 8 })
		{
			if (!vertical)
				buttonGrid.EnableButton(index, false);
		}
	}

	internal int ScriptViewX => viewX * TilesPerStep + 1;

	internal int ScriptViewY => viewY * TilesPerStep + 1;

	/// <summary>
	/// Scrolls the view by the given steps of 8 tiles (like the arrow buttons).
	/// </summary>
	internal void Scroll(int x, int y)
	{
		viewX = Math.Clamp(viewX + x, 0, Math.Max(0, WidthInSteps - ViewWidthInSteps));
		viewY = Math.Clamp(viewY + y, 0, Math.Max(0, HeightInSteps - ViewHeightInSteps));
		ShowMap();
	}

	protected override void ButtonClicked(int index)
	{
		if (index == ExitButtonIndex)
		{
			Game.ScreenHandler.PopScreen();
			return;
		}

		Scroll(index % 3 - 1, index / 3 - 1);
	}
}
